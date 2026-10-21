using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Rendering;
using Block = Ship_Game.Commands.Goals.SalvageDerelict.Block;
using Outcome = Ship_Game.Ships.DerelictRecovery.Outcome;
using Kind = Ship_Game.Ships.DerelictSalvageButton.Kind;
using Stage = Ship_Game.Commands.Goals.SalvageDerelict.Stage;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// The Recover button orders a derelict ship recovered: a saved mode of the salvage order, passed on with it, in which
/// its platform strips nothing and first sends what it holds to its station. While an order stands, the other button
/// turns it to that button's work, and its own lit button calls it off; a station can only be salvaged. The buttons
/// show while the player has a salvage station built in the derelict's system or an order on it. The chance to recover
/// a ship is its hull's, 60 for a frigate down to 20 for a capital, times 1 plus its race's engineers, read from its
/// race's traits, which research leaves alone and rebels and saves keep, times the share of the derelict not yet
/// stripped; of the failures, 10% times 1 minus the engineers explode. A recovering platform breaks in for 10 turns at
/// any pace, then works at twice the salvage speed through the share of health the ship lost at its death times its
/// slots at the production pace, plus a tenth, times 1 minus the engineers, and rolls as the last of it is done, or the
/// turn after the break-in with none to do. A success raises the derelict, its panel showing the work done with no
/// buttons, and at a ship's depth its ship takes its place for the empire that recovered it, under its name, with a new
/// crew, its repair modules, engines and the armor on its edge dead, its other armor at 2% and all else at 4%, with no
/// ordnance, troops, power or shields, dark until its lights flicker on, and heads for its empire's nearest planet that
/// repairs; the selection passes to it, and with that empire gone the derelict sinks away instead. Turning the
/// order to salvage keeps the work but a later recovery breaks in again; saves and passing on keep both. A rival's
/// recovery reads Recovering throughout, its break-in unshown. The player is told of each outcome of theirs, the ship
/// as it appears and a failure or a blast at the roll, but not of a rival's ship, a recovery called off, or a platform
/// or station lost during the rise; the Salvage alerts switch silences them all. A derelict's row leaves the derelicts
/// window once it can no longer be selected.
/// </summary>
[TestClass]
public class SalvageRecoveryTests : StarDriveTest
{
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    static readonly Vector2 Wreck = SystemSite + new Vector2(30_000f, 0);
    static readonly Vector2 PlayerStation = SystemSite + new Vector2(33_000f, 0);
    readonly SolarSystem WreckSystem;
    readonly IShipDesign Station;

    public SalvageRecoveryTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Terran Basic Salvage Station", ShipDesign.SalvagePlatformName);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        WreckSystem = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true).System;
        AddHomeWorldToEmpire(new Vector2(200_000f), Player);
        Assert.IsTrue(ResourceManager.Ships.GetDesign("Terran Basic Salvage Station", out Station), "setup: the station is loaded");
        Player.data.CurrentConstructor = "Terran Constructor";
    }

    [TestCleanup]
    public override void Cleanup()
    {
        UState.Derelicts.Clear();
        base.Cleanup();
    }

    // a salvageable derelict of the player's, so the player knows of it, its ship dead with this share of its health; the ship itself is gone
    Derelict Forms(Vector2 at, string design = "Dreadnought mk1-a", float health = 1f, string name = null)
    {
        TestShip ship = SpawnShip(design, Player, at);
        ship.Health = ship.HealthMax * health;
        if (name != null)
            ship.VanityName = name;
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        return UState.Derelicts.Current.Last();
    }

    SalvageOps StationOrder()
    {
        var order = new SalvageOps(Player, WreckSystem, PlayerStation, Station);
        Player.AI.AddGoal(order);
        order.AdvanceToNextStep(); // past the queueing, which needs a port
        return order;
    }

    Ship BuiltStation(SalvageOps order = null)
    {
        order ??= StationOrder();
        order.TargetShip = SpawnShip(Station.Name, Player, PlayerStation);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the station is built");
        return order.TargetShip;
    }

    // the player's order with its platform built beside the derelict, as its constructor leaves it
    SalvageDerelict AtWork(Derelict d, bool recover = false)
    {
        Assert.IsTrue(SalvageDerelict.Order(Player, d, recover), "setup: ordered");
        SalvageDerelict order = SalvageDerelict.OrderOf(Player, d);
        Ship constructor = order.FinishedShip;
        Assert.IsNotNull(constructor, "setup: a constructor launches");
        Assert.IsTrue(d.TryClaim(Player), "setup: the constructor claims the derelict as it starts to build");

        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, order.BuildPosition);
        platform.CargoSpaceMax = 100f;
        constructor.QueueTotalRemoval();
        order.OnPlatformBuilt(platform, constructor);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the platform is built");
        return order;
    }

    static float Chance(Empire empire, Derelict d) => DerelictRecovery.Chance(empire, d);

    // the platform's turns of work, one evaluation of its order each
    static void Turns(SalvageDerelict order, int turns)
    {
        for (int i = 0; i < turns; ++i)
            order.Evaluate();
    }

    Stage StageOf(Derelict d) => SalvageDerelict.StageOf(Player, d, out _, out _);

    // the empire's luck seed set to the first whose draws for the derelict, each from 0 to 1, are as wanted: a draw passes under its percent, as a dice roll does
    static void Draws(Derelict d, Empire empire, Func<float, float, bool> wanted)
    {
        PropertyInfo seed = typeof(Empire).GetProperty(nameof(Empire.LuckSeed))!;
        for (int s = 1; s < 1_000_000; ++s)
        {
            seed.SetValue(empire, s);
            (float recovery, float catastrophe) = DerelictRecovery.Draws(empire, d);
            if (wanted(recovery, catastrophe))
                return;
        }
        Assert.Fail("setup: no seed draws as wanted");
    }

    void Succeeds(Derelict d) => Draws(d, Player, (recovery, _) => recovery < 0.01f);
    void BreaksApart(Derelict d) => Draws(d, Player, (recovery, catastrophe) => recovery > 0.99f && catastrophe > 0.5f);
    void Explodes(Derelict d) => Draws(d, Player, (recovery, catastrophe) => recovery > 0.99f && catastrophe < 0.01f);

    // the player's recovery with a turn of work left, so its next turn finishes the work and rolls
    SalvageDerelict OneTurnOfWorkLeft(Derelict d)
    {
        BuiltStation();
        Player.data.SelectedTraitSet = "";
        SalvageDerelict order = AtWork(d, recover: true);
        int workTurns = (int)Math.Ceiling(DerelictRecovery.Work(Player, d) / 24f);
        Assert.IsTrue(workTurns > 1, "setup: work for more than a turn");
        Turns(order, 10 + workTurns - 1);
        AssertLessThan(DerelictRecovery.Progress(Player, d, order.RecoveryWork), 1f, "setup: a turn's work left");
        return order;
    }

    static FixedSimTime Seconds(float seconds) => new(seconds);

    Block BlockOf(Derelict d, Kind kind) => DerelictSalvageButton.BlockOf(kind, SalvageDerelict.WhyNoOrder(Player, d), d);

    [TestMethod]
    public void EachHullHasItsChanceAsReverseEngineeringsTable()
    {
        AssertEqual(60f, DerelictRecovery.HullChance(RoleName.frigate), "a frigate");
        AssertEqual(50f, DerelictRecovery.HullChance(RoleName.destroyer), "a destroyer");
        AssertEqual(40f, DerelictRecovery.HullChance(RoleName.cruiser), "a cruiser");
        AssertEqual(30f, DerelictRecovery.HullChance(RoleName.battleship), "a battleship");
        AssertEqual(20f, DerelictRecovery.HullChance(RoleName.capital), "a capital");
    }

    [TestMethod]
    public void TheChanceIsTheHullsRaisedByTheRacesEngineersAndLoweredByWhatIsStripped()
    {
        Derelict d = Forms(Wreck);
        AssertEqual(RoleName.capital, d.Design.HullRole, "setup: a capital's derelict");
        Player.data.SelectedTraitSet = "Savage";
        AssertEqual(0.01f, 20f, Chance(Player, d), "a capital's 20");

        Player.data.SelectedTraitSet = "Savage, Skilled Engineers";
        Player.data.Traits.ModHpModifier = 2.1f;
        AssertEqual(0.01f, 24f, Chance(Player, d), "a fifth more for Skilled Engineers, whatever research adds to the ships' health");

        UState.Derelicts.Strip(d, null, d.Production * 0.25f);
        AssertEqual(0.01f, 18f, Chance(Player, d), "lowered by the quarter of it stripped");

        Player.data.SelectedTraitSet = "Haphazard Engineers";
        AssertEqual(0.01f, 12f, Chance(Player, d), "and a fifth less for Haphazard Engineers");
    }

    [TestMethod]
    public void OneFailureInTenExplodesFewerForSkilledEngineersAndMoreForHaphazardOnes()
    {
        Player.data.SelectedTraitSet = "";
        AssertEqual(0.01f, 10f, DerelictRecovery.CatastropheChanceOf(Player), "one in ten");
        Player.data.SelectedTraitSet = "Skilled Engineers";
        AssertEqual(0.01f, 8f, DerelictRecovery.CatastropheChanceOf(Player), "8% for Skilled Engineers");
        Player.data.SelectedTraitSet = "Haphazard Engineers";
        AssertEqual(0.01f, 12f, DerelictRecovery.CatastropheChanceOf(Player), "12% for Haphazard Engineers");
    }

    [TestMethod]
    public void AnEmpiresEngineersAreItsRacesTraitsWhichResearchLeavesAlone()
    {
        UState.P.DisableAlternateAITraits = true; // the race's first trait set
        IEmpireData skilled = ResourceManager.MajorRaces.First(r => UState.GetEmpireByName(r.Name) == null
                                                                  && r.CreateInstance().Traits.PlayerTraitOptions.Contains("Skilled Engineers"));
        Empire e = UState.CreateEmpire(skilled, isPlayer: false);
        AssertEqual(0.01f, 0.2f, e.RaceEngineers, "a race made with Skilled Engineers");

        e.data.Traits.ModHpModifier += 0.5f; // as research, an artifact or the AI's difficulty adds
        AssertEqual(0.01f, 0.2f, e.RaceEngineers, "which nothing it adds to its ships' health changes");
    }

    [TestMethod]
    public void RebelsHaveTheEngineersOfTheRaceTheyRiseFrom()
    {
        Player.data.SelectedTraitSet = "Skilled Engineers";
        Player.data.Traits.ModHpModifier = 1f;
        Empire rebels = UState.CreateRebelsFromEmpireData(Player.data, Player);
        AssertEqual(0.01f, 0.2f, rebels.RaceEngineers, "the race's Skilled Engineers, not the empire's health research");
    }

    [TestMethod]
    public void ARecoveringPlatformStripsNothingAndFirstSendsWhatItHolds()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d);
        order.Evaluate();
        AssertEqual(0.01f, 12f, order.Platform.GetProduction(), "setup: the platform stripped a turn's work");

        SalvageDerelict.Switch(Player, d, recover: true);
        Assert.IsTrue(order.Recovering, "the Recover button turns the order to a recovery");
        float left = d.ProductionLeft;
        float stored = station.GetProduction();
        order.Evaluate();
        AssertEqual(0.01f, 0f, order.Platform.GetProduction(), "the platform first sends what it holds");
        AssertEqual(0.01f, stored + 12f, station.GetProduction(), "to its station");
        order.Evaluate();
        AssertEqual(0.01f, left, d.ProductionLeft, "and strips nothing");

        SalvageDerelict.Switch(Player, d, recover: false);
        Assert.IsFalse(order.Recovering, "the Salvage button turns it back");
        order.Evaluate();
        AssertEqual(0.01f, left - 12f, d.ProductionLeft, "and the platform strips again");
    }

    [TestMethod]
    public void ARecoveringPlatformsLoadWaitsForRoomInItsStation()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation();
        SalvageDerelict order = AtWork(d);
        order.Evaluate();
        station.LoadProduction(station.CargoRoom);
        Assert.IsFalse(order.LoadWaitsForStation(order.Platform), "setup: with room in its hold, a stripping platform's load does not wait");

        SalvageDerelict.Switch(Player, d, recover: true);
        Assert.IsTrue(order.LoadWaitsForStation(order.Platform), "a recovering one's does, as its station is full");
        order.Evaluate();
        AssertEqual(0.01f, 12f, order.Platform.GetProduction(), "so it keeps its load");
    }

    [TestMethod]
    public void TheButtonsOrderTurnAndCallOffTheOrder()
    {
        Derelict d = Forms(Wreck);
        BuiltStation();
        Block recover = BlockOf(d, Kind.Recover);
        AssertEqual(Block.None, recover, "setup: the player can order it");
        Assert.IsFalse(DerelictSalvageButton.Lit(Kind.Recover, recover, null), "both buttons are grey");

        DerelictSalvageButton.Click(Player, d, Kind.Recover, recover, lit: false);
        Block ordered = BlockOf(d, Kind.Salvage);
        SalvageDerelict order = DerelictSalvageButton.StandingOrder(Player, d, ordered);
        Assert.IsTrue(order is { Recovering: true }, "the Recover button orders a recovery");
        AssertEqual(Block.Ordered, ordered, "which stands");
        Assert.IsTrue(DerelictSalvageButton.Lit(Kind.Recover, ordered, order), "its button lit");
        Assert.IsFalse(DerelictSalvageButton.Lit(Kind.Salvage, ordered, order), "the other grey");

        DerelictSalvageButton.Click(Player, d, Kind.Salvage, ordered, lit: false);
        Assert.IsFalse(order.Recovering, "the other button turns the order to its work");
        Assert.IsTrue(DerelictSalvageButton.Lit(Kind.Salvage, ordered, order), "and lights up");
        Assert.IsFalse(DerelictSalvageButton.Lit(Kind.Recover, ordered, order), "the Recover button grey again");

        DerelictSalvageButton.Click(Player, d, Kind.Salvage, ordered, lit: true);
        Assert.IsNull(SalvageDerelict.OrderOf(Player, d), "the lit button calls the order off");
        AssertEqual(Block.None, BlockOf(d, Kind.Salvage), "so both are grey and can order again");
    }

    [TestMethod]
    public void AStationCanOnlyBeSalvaged()
    {
        Derelict d = Forms(Wreck, design: "Terran Basic Salvage Station");
        BuiltStation();
        Assert.IsFalse(DerelictRecovery.CanRecover(d), "a station's derelict");
        AssertEqual(Block.NotAShip, SalvageDerelict.WhyNoRecovery(Player, d), "greys the Recover button");
        Assert.IsFalse(SalvageDerelict.Order(Player, d, recover: true), "which orders nothing");
        AssertEqual(Block.None, SalvageDerelict.WhyNoOrder(Player, d), "the Salvage button can");

        Assert.IsTrue(SalvageDerelict.Order(Player, d), "setup: ordered salvaged");
        AssertEqual(Block.NotAShip, SalvageDerelict.WhyNoRecovery(Player, d), "the Recover button stays grey");
        SalvageDerelict.Switch(Player, d, recover: true);
        Assert.IsFalse(SalvageDerelict.OrderOf(Player, d).Recovering, "and turns nothing to a recovery");
    }

    [TestMethod]
    public void TheButtonsShowOnlyWithASalvageStationBuiltInTheSystemOrAnOrderStanding()
    {
        Derelict d = Forms(Wreck);
        bool Shown() => DerelictSalvageButton.Shown(Player, d, SalvageDerelict.WhyNoOrder(Player, d));
        Assert.IsFalse(Shown(), "no station there");

        SalvageOps stationOrder = StationOrder();
        Assert.IsFalse(Shown(), "nor one on order");
        d.TryClaim(Enemy);
        Assert.IsFalse(Shown(), "nor while another empire salvages it");

        d.ReleaseClaim();
        d.TryClaim(Player); // as a save can catch a claim without its order
        Assert.IsTrue(Shown(), "a claim of the player's shows them, to give it up");
        d.ReleaseClaim();

        BuiltStation(stationOrder);
        Assert.IsTrue(Shown(), "a built station shows them");
        d.TryClaim(Enemy);
        Assert.IsTrue(Shown(), "greyed while another empire salvages it");

        UState.P.DisableSalvageOps = true;
        Assert.IsFalse(Shown(), "but not with salvage ops off");
    }

    [TestMethod]
    public void TheOtherButtonGivesUpAClaimKeptWithoutItsOrder()
    {
        Derelict d = Forms(Wreck);
        BuiltStation();
        d.TryClaim(Player); // as a save can catch a claim without its order
        Block block = BlockOf(d, Kind.Recover);
        AssertEqual(Block.Ordered, block, "setup: the claim reads as an order");
        SalvageDerelict order = DerelictSalvageButton.StandingOrder(Player, d, block);
        Assert.IsNull(order, "setup: with none standing");
        bool lit = DerelictSalvageButton.Lit(Kind.Recover, block, order);
        Assert.IsFalse(lit, "setup: as a salvage, the Recover button grey");
        Assert.IsTrue(DerelictSalvageButton.Lit(Kind.Salvage, block, order), "setup: and the Salvage button lit");

        DerelictSalvageButton.Click(Player, d, Kind.Recover, block, lit);
        Assert.IsNull(d.Claimant, "its click gives the claim up, as a call-off would");
    }

    [TestMethod]
    public void AnOrdersRecoverySurvivesSaveAndPassingOn()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        BuiltStation();
        SalvageDerelict order = AtWork(d, recover: true);
        Player.data.SelectedTraitSet = "Skilled Engineers";
        Turns(order, 11);
        AssertEqual(0.01f, 24f, order.RecoveryWork, "setup: broken in, and a turn's work done");
        d.Update(Seconds(UState.P.TurnTimer * 3f));
        AssertEqual(0.001f, 0.3f, d.Steadiness, "setup: steadied for three turns");

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageRecovery", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");
        SalvageDerelict loadedOrder = loaded.UState.Player.AI.FindGoals<SalvageDerelict>().First();
        Assert.IsTrue(loadedOrder.Recovering, "a recovery stays one through a save");
        AssertEqual(0.01f, 24f, loadedOrder.RecoveryWork, "with its work done");
        AssertEqual(10, loadedOrder.BreakInDone, "and its break-in");
        Derelict loadedDerelict = loaded.UState.Derelicts.Current.First();
        AssertEqual(0.001f, 0.4f, loadedDerelict.HealthAtDeath, "the derelict keeps the health its ship died with");
        AssertEqual(0.001f, 0.3f, loadedDerelict.Steadiness, "and its steadying");
        loadedDerelict.Update(Seconds(UState.P.TurnTimer * 10f));
        AssertEqual(1f, loadedDerelict.Steadiness, "which goes on to where the break-in took it");
        AssertEqual(0.01f, 0.2f, loaded.UState.Player.RaceEngineers, "and the race's engineers with its traits");

        SalvageDerelict.PassOn(Player, Enemy);
        SalvageDerelict passed = Enemy.AI.FindGoals<SalvageDerelict>().First();
        Assert.IsTrue(passed.Recovering, "and passed on with its platform");
        AssertEqual(0.01f, 24f, passed.RecoveryWork, "with its work done");
        AssertEqual(10, passed.BreakInDone, "and its break-in");
    }

    [TestMethod]
    public void TheWorkIsTheHealthLostAtDeathTimesTheSlotsAtThePaceLessTheEngineers()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        AssertEqual(0.001f, 0.4f, d.HealthAtDeath, "the derelict keeps the share of health its ship died with");
        int slots = d.Design.SurfaceArea;
        Player.data.SelectedTraitSet = "";
        AssertEqual(0.01f, 0.6f * slots * 1.1f, DerelictRecovery.Work(Player, d), "the share lost times the ship's slots, and a tenth more");

        Player.data.SelectedTraitSet = "Skilled Engineers";
        AssertEqual(0.01f, 0.6f * slots * 1.1f * 0.8f, DerelictRecovery.Work(Player, d), "a fifth less for Skilled Engineers");
        Player.data.SelectedTraitSet = "Haphazard Engineers";
        AssertEqual(0.01f, 0.6f * slots * 1.1f * 1.2f, DerelictRecovery.Work(Player, d), "a fifth more for Haphazard ones");

        Player.data.SelectedTraitSet = "";
        UState.P.Pace = 3f; // a production pace of 2
        AssertEqual(0.01f, 0.6f * slots * 1.1f * 2f, DerelictRecovery.Work(Player, d), "twice the work at twice the production pace");
        AssertEqual(0.01f, 24f, DerelictRecovery.Speed, "worked at twice the salvage speed, at any pace");
        AssertEqual(10, DerelictRecovery.BreakInTurns, "after a break-in of 10 turns, at any pace too");
    }

    [TestMethod]
    public void ARecoveringPlatformBreaksInThenWorksAtTwiceTheSalvageSpeedUntilAllIsDone()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        Succeeds(d);
        BuiltStation();
        Player.data.SelectedTraitSet = "";
        SalvageDerelict order = AtWork(d, recover: true);
        float work = DerelictRecovery.Work(Player, d);
        Assert.IsTrue(work > 48f, "setup: work for more than two turns");

        Turns(order, 9);
        Assert.IsTrue(order.BreakingIn, "nine turns in, its platform still breaks in");
        AssertEqual(1, order.BreakInTurnsLeft, "with a turn left");
        AssertEqual(0f, order.RecoveryWork, "and no work done");
        AssertEqual(Stage.BreakingIn, StageOf(d), "the derelicts window says so");
        AssertEqual(string.Format(Localizer.Token(GameText.DerelictBreakingIn), 1), ShipInfoUIElement.SalvageStatus(order.Platform, order),
                    "and the platform's status, with the turns left");

        Turns(order, 1);
        Assert.IsFalse(order.BreakingIn, "after ten it has broken in");
        AssertEqual(0f, order.RecoveryWork, "and the work starts the next turn");
        Turns(order, 1);
        AssertEqual(0.01f, 24f, order.RecoveryWork, "at twice the salvage speed");
        AssertEqual(Stage.Recovering, StageOf(d), "the window's status reads Recovering");
        AssertEqual(string.Format(Localizer.Token(GameText.RecoveringDerelict), d.Design.Name), ShipInfoUIElement.SalvageStatus(order.Platform, order),
                    "and the platform's names the ship");

        Turns(order, (int)Math.Ceiling(work / 24f) - 1);
        AssertEqual(1f, DerelictRecovery.Progress(Player, d, order.RecoveryWork), "all of the work is done");
        Assert.IsTrue(order.RecoverySucceeded, "and rolled for the same turn");
        Turns(order, 1);
        AssertEqual(0.001f, work, order.RecoveryWork, "the platform holding there");
    }

    [TestMethod]
    public void ARecoveryWithNoWorkRollsTheTurnAfterItsBreakIn()
    {
        Derelict d = Forms(Wreck);
        Succeeds(d);
        BuiltStation();
        Player.data.SelectedTraitSet = "";
        SalvageDerelict order = AtWork(d, recover: true);
        AssertEqual(0f, DerelictRecovery.Work(Player, d), "setup: a ship that died at full health leaves no work");
        AssertEqual(11, DerelictRecovery.TurnsLeft(Player, d, 0, 0f), "its tooltip counts the break-in's turns and the roll's");

        Turns(order, 10);
        Assert.IsFalse(order.BreakingIn || order.RecoverySucceeded, "broken in, no roll yet");
        AssertEqual(1, DerelictRecovery.TurnsLeft(Player, d, order.BreakInDone, order.RecoveryWork), "the roll's turn left");
        Turns(order, 1);
        Assert.IsTrue(order.RecoverySucceeded, "the turn after the break-in it rolls");
    }

    [TestMethod]
    public void TurningToSalvageKeepsTheWorkButARecoveryBreaksInAgain()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        BuiltStation();
        Player.data.SelectedTraitSet = "";
        SalvageDerelict order = AtWork(d, recover: true);
        Turns(order, 12);
        AssertEqual(0.01f, 48f, order.RecoveryWork, "setup: broken in, and two turns' work");

        SalvageDerelict.Switch(Player, d, recover: false);
        Turns(order, 1);
        AssertEqual(0.01f, 48f, order.RecoveryWork, "the work done stays while the platform strips");

        SalvageDerelict.Switch(Player, d, recover: true);
        Assert.IsTrue(order.BreakingIn, "a recovery taken up again breaks in again");
        AssertEqual(10, order.BreakInTurnsLeft, "for all its turns");
        Turns(order, 1);
        AssertEqual(10, order.BreakInTurnsLeft, "which start once its platform has sent what it holds");
        Turns(order, 10);
        AssertEqual(0.01f, 48f, order.RecoveryWork, "the work waiting meanwhile");
        Turns(order, 1);
        AssertEqual(0.01f, 72f, order.RecoveryWork, "then going on from where it was");
    }

    [TestMethod]
    public void TheBarShowsTheWorkDoneAndItsTooltipTheTurnsLeftWithTheBreakIn()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        Player.data.SelectedTraitSet = "";
        float work = DerelictRecovery.Work(Player, d);
        AssertEqual(0.001f, 0.25f, DerelictRecovery.Progress(Player, d, work * 0.25f), "a quarter of the work done");
        int workTurns = (int)Math.Ceiling(work / 24f);
        AssertEqual(10 + workTurns, DerelictRecovery.TurnsLeft(Player, d, 0, 0f), "the break-in's turns, then the work's, the last of which rolls");
        AssertEqual(6 + workTurns, DerelictRecovery.TurnsLeft(Player, d, 4, 0f), "four turns into the break-in");
        AssertEqual((int)Math.Ceiling((work - 24f) / 24f), DerelictRecovery.TurnsLeft(Player, d, 10, 24f), "a turn's work done after it");
        AssertEqual(1, DerelictRecovery.TurnsLeft(Player, d, 10, work), "the roll's own with no work left");
    }

    [TestMethod]
    public void ARecoveryTurnSendsAShuttleForEverySixSlotsOfWork()
    {
        AssertEqual(4, CargoShuttles.RecoveryShuttlesFor(24f), "four for a turn's 24");
        AssertEqual(2, CargoShuttles.RecoveryShuttlesFor(7f), "rounded up");
        AssertEqual(1, CargoShuttles.RecoveryShuttlesFor(0.5f), "and at least one");
    }

    [TestMethod]
    public void PassingOnAnOrderMidBreakInKeepsItsTurns()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        BuiltStation();
        SalvageDerelict order = AtWork(d, recover: true);
        Turns(order, 4);
        AssertEqual(6, order.BreakInTurnsLeft, "setup: four turns into the break-in");

        SalvageDerelict.PassOn(Player, Enemy);
        SalvageDerelict passed = Enemy.AI.FindGoals<SalvageDerelict>().First();
        Assert.IsTrue(passed.BreakingIn, "the order passed on still breaks in");
        AssertEqual(6, passed.BreakInTurnsLeft, "with the turns it had left");
    }

    [TestMethod]
    public void ARivalsRecoveryReadsRecoveringThroughoutWithoutItsBreakIn()
    {
        Derelict d = Forms(Wreck);
        var theirs = new SalvageDerelict(Enemy, d, recover: true);
        Enemy.AI.AddGoal(theirs);
        Assert.IsTrue(d.TryClaim(Enemy), "setup: the rival's constructor starts to build");
        AssertEqual(Stage.Recovering, StageOf(d), "a rival's recovery reads Recovering while its platform is built");
        AssertEqual(string.Format(Localizer.Token(GameText.DerelictRecoveredBy), Enemy.Name), DerelictSalvageButton.ClaimTip(d, Enemy),
                    "and its flag's tooltip says it recovers the derelict");

        theirs.OnPlatformBuilt(SpawnShip(ShipDesign.SalvagePlatformName, Enemy, theirs.BuildPosition), SpawnShip("Terran Constructor", Enemy, theirs.BuildPosition));
        Assert.IsTrue(theirs.BreakingIn, "setup: its platform breaks in");
        AssertEqual(Stage.Recovering, SalvageDerelict.StageOf(Player, d, out Empire claimant, out SalvageDerelict order), "the window still reads Recovering");
        AssertEqual(Enemy, claimant, "in its colour");
        Assert.IsNull(order, "with no order of the player's, so no bar");
        AssertEqual(string.Format(Localizer.Token(GameText.RecoveringDerelict), d.Design.Name), ShipInfoUIElement.SalvageStatus(theirs.Platform, theirs),
                    "and so does its platform's status");

        SalvageDerelict.Switch(Enemy, d, recover: false);
        AssertEqual(Stage.Salvaging, StageOf(d), "its salvage reads Salvaging");
        AssertEqual(string.Format(Localizer.Token(GameText.DerelictSalvagedBy), Enemy.Name), DerelictSalvageButton.ClaimTip(d, Enemy),
                    "and its flag's tooltip says it salvages the derelict");
    }

    [TestMethod]
    public void TheRollRecoversAtTheChanceAndATenthOfTheFailuresExplode()
    {
        Derelict d = Forms(Wreck);
        Player.data.SelectedTraitSet = "";
        AssertEqual(0.01f, 20f, Chance(Player, d), "setup: a capital's 20%");
        Draws(d, Player, (recovery, _) => recovery is > 0.15f and < 0.19f);
        AssertEqual(Outcome.Recovered, DerelictRecovery.Roll(Player, d), "a draw under the chance recovers the ship");
        Draws(d, Player, (recovery, catastrophe) => recovery is > 0.21f and < 0.25f && catastrophe is > 0.11f and < 0.15f);
        AssertEqual(Outcome.BrokenApart, DerelictRecovery.Roll(Player, d), "one over it fails, and a second draw over a tenth breaks the derelict apart");
        Draws(d, Player, (recovery, catastrophe) => recovery is > 0.21f and < 0.25f && catastrophe < 0.09f);
        AssertEqual(Outcome.Exploded, DerelictRecovery.Roll(Player, d), "while one under a tenth blows it up");
    }

    [TestMethod]
    public void EachEmpireHasItsOwnDrawsForADerelictTheSameAfterAReload()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        Derelict other = Forms(Wreck + new Vector2(0f, 5_000f), health: 0.4f);
        Assert.AreNotEqual(Player.LuckSeed, Enemy.LuckSeed, "each empire picks a luck seed of its own as it is made");
        (float recovery, float catastrophe) = DerelictRecovery.Draws(Player, d);
        Assert.IsTrue(recovery is >= 0f and < 1f && catastrophe is >= 0f and < 1f, "from which it draws from 0 to 1 for a derelict");
        Assert.AreNotEqual(DerelictRecovery.Draws(Player, other), (recovery, catastrophe), "other draws for another derelict");
        Assert.AreNotEqual(DerelictRecovery.Draws(Enemy, d), (recovery, catastrophe), "and another empire its own for the same one");
        PropertyInfo seed = typeof(Empire).GetProperty(nameof(Empire.LuckSeed))!;
        seed.SetValue(Player, 0);
        seed.SetValue(Enemy, 0);
        Assert.AreNotEqual(DerelictRecovery.Draws(Enemy, d), DerelictRecovery.Draws(Player, d), "even with no seed, as in a save made before it, by the empires' ids");
        seed.SetValue(Player, 1234);
        Player.data.SelectedTraitSet = "";

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageRecovery", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Derelict again = loaded.UState.Derelicts.Current.First(x => x.Id == d.Id);
        AssertEqual(1234, loaded.UState.Player.LuckSeed, "a save keeps the empire's luck seed");
        Assert.AreEqual(DerelictRecovery.Draws(Player, d), DerelictRecovery.Draws(loaded.UState.Player, again), "so the player's draws are the same after a reload");
        AssertEqual(DerelictRecovery.Roll(Player, d), DerelictRecovery.Roll(loaded.UState.Player, again), "so its roll comes out the same after a reload");
    }

    [TestMethod]
    public void TheRollComesWithTheLastOfTheWorkAndASuccessRaisesTheDerelict()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        Succeeds(d);
        SalvageDerelict order = OneTurnOfWorkLeft(d);
        Assert.IsFalse(order.RecoverySucceeded, "no roll while work is left");

        float depth = d.Z;
        AssertGreaterThan(depth, 0f, "setup: the derelict rests below a ship's depth");
        Turns(order, 1);
        AssertEqual(1f, DerelictRecovery.Progress(Player, d, order.RecoveryWork), "the last turn's work done");
        Assert.IsTrue(order.RecoverySucceeded, "and the same turn a roll under the chance succeeds");
        Assert.IsFalse(d.CanBeSalvaged, "so the derelict can no longer be salvaged");
        Assert.IsFalse(WreckSystem.Derelicts.ContainsRef(d), "and leaves its system");
        d.Update(Seconds(Derelict.RiseSeconds * 0.5f));
        AssertEqual(0.01f, depth * 0.5f, d.Z, "it rises evenly");
        BreaksApart(d);
        Turns(order, 1);
        Assert.IsTrue(order.RecoverySucceeded && !order.Finished && Player.AI.HasGoal(g => g == order), "rolling no more, its order standing as it rises");
        d.Update(Seconds(Derelict.RiseSeconds * 0.5f));
        AssertEqual(0f, d.Z, "to a ship's depth over a few seconds");
    }

    // the player's recovery whose last turn of work rolled a success, the derelict now rising for its ship
    SalvageDerelict RolledASuccess(Derelict d)
    {
        Succeeds(d);
        SalvageDerelict order = OneTurnOfWorkLeft(d);
        Turns(order, 1);
        Assert.IsTrue(order.RecoverySucceeded, "setup: the roll succeeds");
        return order;
    }

    // the derelict rises the rest of the way, and its ship takes its place
    Ship RisesForItsShip(Derelict d)
    {
        UState.Derelicts.Update(Seconds(Derelict.RiseSeconds));
        Assert.IsNotNull(d.RecoveredAs, "setup: the ship takes the derelict's place");
        return d.RecoveredAs;
    }

    [TestMethod]
    public void TheShipTakesTheDerelictsPlaceAsItReachesAShipsDepth()
    {
        Derelict d = Forms(Wreck, health: 0.4f, name: "Vigilant Dawn");
        typeof(Derelict).GetProperty(nameof(Derelict.RotationZ))!.SetValue(d, 2f);
        Player.data.BaseShipLevel = 3;
        SalvageDerelict order = RolledASuccess(d);
        var notes = Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);

        UState.Derelicts.Update(Seconds(1.5f));
        Assert.IsNull(d.RecoveredAs, "no ship while the derelict rises");
        Assert.IsTrue(UState.Derelicts.Current.ContainsRef(d) && !order.Finished, "which stays, its order standing");
        UState.Derelicts.Update(Seconds(1.5f));
        Ship ship = d.RecoveredAs;
        Assert.IsNotNull(ship, "as it reaches a ship's depth, 3 seconds after the roll, its ship takes its place");
        Assert.IsFalse(UState.Derelicts.Current.ContainsRef(d), "and the derelict is gone");
        AssertEqual("Dreadnought mk1-a", ship.Name, "the ship of its design");
        AssertEqual(Player, ship.Loyalty, "the recovering empire's");
        AssertEqual(d.Position, ship.Position, "where it lay");
        AssertEqual(2f, ship.Rotation, "facing its way");
        AssertEqual("Vigilant Dawn", ship.VanityName, "under its name");
        AssertEqual(0, ship.Level, "with a new crew");
        Assert.IsNull(d.Claimant, "the derelict given up");

        Assert.IsTrue(order.Finished, "the platform's work is over");
        Turns(order, 1);
        Assert.IsTrue(order.Dismantling, "and it takes itself apart");
        AssertEqual($"{WreckSystem.Name}: the Dreadnought mk1-a is recovered", notes.GetNotificationsAtomic().Single().Message,
                    "which tells only of the ship, not of a salvage's end");
    }

    [TestMethod]
    public void TheShipComesBackDamagedWithNoOrdnanceTroopsPowerOrShields()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        RolledASuccess(d);
        Ship ship = RisesForItsShip(d);
        Assert.IsTrue(ResourceManager.GetShipTemplate("Dreadnought mk1-a", out Ship designed), "setup: the design as built");

        float Share(ShipModule m) => m.Health / m.ActualMaxHealth;
        ShipModule[] modules = ship.Modules;
        int engines = 0, edge = 0, inner = 0, exposed = 0;
        for (int i = 0; i < modules.Length; ++i)
        {
            ShipModule m = modules[i];
            if (m.ModuleType == ShipModuleType.Engine)
            {
                ++engines;
                Assert.IsTrue(!m.Active && m.Health == 0f, $"its engines dead: {m.UID}");
            }
            else if (m.ModuleType == ShipModuleType.Armor && designed.Modules[i].IsExternal)
            {
                ++edge;
                Assert.IsTrue(!m.Active && m.Health == 0f, $"its armor on the hull's edge as designed dead: {m.UID}");
            }
            else if (m.ModuleType == ShipModuleType.Armor)
            {
                ++inner;
                AssertEqual(0.0001f, 0.02f, Share(m), $"its other armor at 2%: {m.UID}");
                if (m.IsExternal)
                    ++exposed;
            }
        }
        Assert.IsTrue(engines > 0 && edge > 0 && inner > 0, "setup: the design has engines and armor on and inside its edge");
        AssertGreaterThan(exposed, 0, "the armor under its dead edge now on the outside");
        AssertEqual(0f, ship.MaxSTLSpeed, "it cannot move until its engines are repaired");
        ShipModule bridge = modules.First(m => m.UID == "Bridge");
        Assert.IsTrue(bridge.Active, "its command module alive");
        AssertEqual(0.0001f, 0.04f, Share(bridge), "at 4%");
        AssertEqual(0.0001f, 0.04f, Share(modules.First(m => m.UID == "Extreme Fusion Reactor")), "as every other module");
        Assert.IsTrue(ship.Active, "the ship alive");

        AssertGreaterThan(ship.OrdinanceMax, 0f, "setup: it holds ordnance");
        AssertEqual(0f, ship.Ordinance, "with none aboard");
        AssertGreaterThan(ship.TroopCapacity, 0, "setup: it holds troops");
        AssertEqual(0, ship.TroopCount, "with none aboard");
        AssertGreaterThan(ship.PowerStoreMax, 0f, "setup: it stores power");
        AssertEqual(0f, ship.PowerCurrent, "with none stored");
        AssertGreaterThan(ship.ShieldMax, 0f, "setup: it has shields");
        AssertEqual(0f, ship.ShieldPower, "empty");
        AssertLessThan(ship.GetStrength(), ship.BaseStrength, "its strength that of the damaged ship");
    }

    [TestMethod]
    public void AModuleThatComesBackAliveKeepsAtLeastOneHealth()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        RolledASuccess(d);
        Ship ship = RisesForItsShip(d);
        ShipModule reactor = ship.Modules.First(m => m.UID == "Extreme Fusion Reactor");
        reactor.SetRecoveredHealth(0.5f / reactor.ActualMaxHealth);
        AssertEqual(1f, reactor.Health, "a module whose share would leave it under 1 health keeps 1");
        reactor.Update(Seconds(1f));
        Assert.IsTrue(reactor.Active, "so it stays alive past its next update");
    }

    [TestMethod]
    public void RepairModulesComeBackDeadAndADesignWithNoRepairGetsItsEnginesBarelyAlive()
    {
        ShipModule engineering = ResourceManager.GetModuleTemplate("Main Engineering");
        ShipModule engine = ResourceManager.GetModuleTemplate("CapEngines");
        ShipModule bridge = ResourceManager.GetModuleTemplate("Bridge");
        AssertEqual(0f, DerelictRecovery.HealthOnRecovery(engineering, designRepairs: true), "a repair module other than a command module comes back dead");
        AssertEqual(0.04f, DerelictRecovery.HealthOnRecovery(bridge, designRepairs: true), "a command module at 4%, whatever its repair");
        AssertEqual(0f, DerelictRecovery.HealthOnRecovery(engine, designRepairs: true), "the engines dead while the design repairs");
        AssertEqual(0.01f, DerelictRecovery.HealthOnRecovery(engine, designRepairs: false), "and at 1% when it has no repair at all");

        ShipModule reactor = ResourceManager.GetModuleTemplate("Extreme Fusion Reactor");
        Assert.IsFalse(DerelictRecovery.DesignRepairs(new[] { engine, reactor }), "a design of modules with no repair value has no repair");
        Assert.IsTrue(DerelictRecovery.DesignRepairs(new[] { engine, reactor, bridge }), "while a command module's repair counts");
        Assert.IsTrue(DerelictRecovery.DesignRepairs(new[] { engine, engineering }), "as does a repair module's");
    }

    [TestMethod]
    public void TheShipHeadsForItsEmpiresNearestPlanetThatRepairs()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        Planet near = AddDummyPlanetToEmpire(Wreck + new Vector2(40_000f, 0f), Player);
        Planet far = AddDummyPlanetToEmpire(Wreck + new Vector2(120_000f, 0f), Player);
        foreach (Planet p in Player.GetPlanets())
            p.TotalRepair = 0f;
        far.TotalRepair = 20f;
        Player.UpdateRallyPoints();
        Assert.IsTrue(near.Position.Distance(Wreck) < far.Position.Distance(Wreck), "setup: the planet with no repair is nearer");

        RolledASuccess(d);
        Ship ship = RisesForItsShip(d);
        AssertEqual(AIState.Resupply, ship.AI.State, "the ship heads off to be repaired");
        AssertEqual(far, ship.AI.OrbitTarget, "at the nearest planet that repairs");
    }

    [TestMethod]
    public void AsItRisesThePanelShowsTheWorkDoneWithNoButtonsAndNothingTurnsOrCallsOffTheOrder()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = RolledASuccess(d);
        Universe.SetSelectedDerelict(d);
        Universe.UpdateSelectedDerelict();
        Assert.AreSame(d, Universe.SelectedDerelict, "it stays selected as it rises");

        Block block = SalvageDerelict.WhyNoOrder(Player, d);
        Assert.IsFalse(DerelictSalvageButton.Shown(Player, d, block), "with no buttons");
        Assert.AreSame(order, DerelictSalvageButton.StandingOrder(Player, d, block), "its panel showing the recovery");
        AssertEqual(1f, DerelictRecovery.Progress(Player, d, order.RecoveryWork), "all its work done");
        AssertEqual(Stage.Recovering, StageOf(d), "still recovering");
        SalvageDerelict.Switch(Player, d, recover: false);
        Assert.IsTrue(order.Recovering, "and its order can no longer be turned to salvage");
        SalvageDerelict.Cancel(Player, d);
        Universe.InvokePendingSimThreadActions();
        Assert.IsTrue(!order.CalledOff && !order.Finished && d.Claimant == Player, "nor called off, as a click queued in the frame of the roll would");
    }

    [TestMethod]
    public void TheSelectionPassesFromTheDerelictToItsShip()
    {
        Universe.ShipInfoUIElement ??= new(new SDGraphics.Rectangle(0, 0, 407, 242), Universe.ScreenManager, Universe);
        Derelict d = Forms(Wreck, health: 0.4f);
        RolledASuccess(d);
        Universe.SetSelectedDerelict(d);
        Ship ship = RisesForItsShip(d);
        Universe.UpdateSelectedDerelict();
        Assert.AreSame(ship, Universe.SelectedShip, "the ship taking its place is selected");
        Assert.IsNull(Universe.SelectedDerelict, "and the derelict no more");
    }

    [TestMethod]
    public void TheSelectionDoesNotPassToARivalsShip()
    {
        Universe.ShipInfoUIElement ??= new(new SDGraphics.Rectangle(0, 0, 407, 242), Universe.ScreenManager, Universe);
        Derelict d = Forms(Wreck, health: 0.4f);
        RolledASuccess(d);
        SalvageDerelict.PassOn(Player, Enemy);
        Universe.SetSelectedDerelict(d);
        Ship ship = RisesForItsShip(d);
        AssertEqual(Enemy, ship.Loyalty, "setup: a rival's ship takes its place");
        Universe.UpdateSelectedDerelict();
        Assert.IsNull(Universe.SelectedShip, "which is not selected");
        Assert.IsNull(Universe.SelectedDerelict, "and the derelict no more");
    }

    [TestMethod]
    public void AnOrderSavedAfterItsShipAppearedStillComesApartUntold()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        RolledASuccess(d);
        RisesForItsShip(d);

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageRecovery", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        var notes = loaded.NotificationManager = new NotificationManager(loaded.ScreenManager, loaded);
        SalvageDerelict order = loaded.UState.Player.AI.FindGoals<SalvageDerelict>().First();
        Assert.IsTrue(order.Recovered && order.Finished, "its derelict gone, its platform's work is over");
        order.Evaluate();
        Assert.IsTrue(order.Dismantling, "so the platform takes itself apart");
        AssertEqual(0, notes.NumberOfNotifications, "without telling of a salvage's end");
    }

    [TestMethod]
    public void ADerelictRisingForItsShipLeavesTheDriftingCapFree()
    {
        float cap = GlobalStats.Defaults.DriftingDerelictCapMultiplier;
        try
        {
            GlobalStats.Defaults.DriftingDerelictCapMultiplier = 0.1f; // a cap of 1 in a galaxy of up to 10 systems
            Derelict d = Forms(Wreck, health: 0.4f);
            Assert.IsTrue(UState.Derelicts.TryAttachModel(d, new SceneObject("rising")), "setup: the derelict has a model");
            RolledASuccess(d);
            Assert.IsTrue(d.RisingForItsShip && d.InScene, "setup: it rises for its ship");

            TestShip dying = SpawnShip("Dreadnought mk1-a", Player, Wreck + new Vector2(0f, 20_000f));
            typeof(Ship).GetField("ShipSO", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(dying, new SceneObject("dying"));
            Assert.IsTrue(UState.Derelicts.TryAdd(dying, salvageable: false, seen: true), "a ship dying on screen meanwhile still leaves a drifting derelict");
        }
        finally
        {
            GlobalStats.Defaults.DriftingDerelictCapMultiplier = cap;
        }
    }

    [TestMethod]
    public void ADerelictThatMayYetComeBackAsAShipKeepsItsDesignFromBeingOverwrittenOrDeleted()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        Assert.IsTrue(UState.Derelicts.AnyMayComeBackAs("Dreadnought mk1-a"), "a salvageable derelict of the design");
        Assert.IsFalse(UState.Derelicts.AnyMayComeBackAs("Vulcan Scout"), "only of its own design");
        Forms(Wreck + new Vector2(0f, 5_000f), design: "Terran Basic Salvage Station");
        Assert.IsFalse(UState.Derelicts.AnyMayComeBackAs("Terran Basic Salvage Station"), "not a station, which can only be salvaged");

        RolledASuccess(d);
        Assert.IsTrue(UState.Derelicts.AnyMayComeBackAs("Dreadnought mk1-a"), "one rising for its ship too");
        RisesForItsShip(d);
        Assert.IsFalse(UState.Derelicts.AnyMayComeBackAs("Dreadnought mk1-a"), "but no more once its ship has taken its place");
    }

    [TestMethod]
    public void AFailedRecoveryNoLongerKeepsTheDesign()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = OneTurnOfWorkLeft(d);
        BreaksApart(d);
        Turns(order, 1);
        Assert.IsTrue(d.BrokenApart, "setup: the roll broke it apart");
        Assert.IsFalse(UState.Derelicts.AnyMayComeBackAs("Dreadnought mk1-a"), "a derelict that will never come back keeps no design");
    }

    [TestMethod]
    public void ADerelictWhoseShipCannotBeMadeIsGone()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = RolledASuccess(d);
        ShipDesign foreign = ((ShipDesign)d.Design).GetClone(d.Design.Name);
        foreign.ModName = "No Such Mod";
        typeof(Derelict).GetProperty(nameof(Derelict.Design))!.SetValue(d, foreign);
        Universe.SetSelectedDerelict(d);
        var notes = Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);

        UState.Derelicts.Update(Seconds(3f));
        Assert.IsNull(d.RecoveredAs, "a design of another mod makes no ship");
        AssertEqual(0, notes.NumberOfNotifications, "and tells nothing");
        Assert.IsFalse(UState.Derelicts.Current.ContainsRef(d), "the derelict has left the list");
        Assert.IsFalse(d.SelectableByPlayer, "and is gone, no longer to be selected");
        Universe.UpdateSelectedDerelict();
        Assert.IsNull(Universe.SelectedDerelict, "so its panel closes");
        Assert.IsTrue(order.Finished, "its platform's work is over");
    }

    [TestMethod]
    public void ADerelictRisingForAnEmpireGoneSinksAwayWithNoShip()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        RolledASuccess(d);
        typeof(Empire).GetProperty(nameof(Empire.IsDefeated))!.SetValue(Player, true);
        UState.Derelicts.Update(Seconds(Derelict.RiseSeconds));
        Assert.IsNull(d.RecoveredAs, "no ship comes back for an empire gone");
        Assert.IsTrue(d.Sinking, "and the derelict sinks away");
        Assert.IsFalse(d.SelectableByPlayer, "no longer to be selected");
    }

    [TestMethod]
    public void ARecoveryPassedOnAsTheDerelictRisesBringsItsShipToTheEmpireItPassedTo()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        RolledASuccess(d);
        SalvageDerelict.PassOn(Player, Enemy);
        AssertEqual(Enemy, RisesForItsShip(d).Loyalty, "the ship comes back for the empire the order passed to");
    }

    [TestMethod]
    public void ThroughASaveTheDerelictStillRisesForItsShipUnderItsName()
    {
        Derelict d = Forms(Wreck, health: 0.4f, name: "Vigilant Dawn");
        RolledASuccess(d);
        UState.Derelicts.Update(Seconds(Derelict.RiseSeconds * 0.5f));

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageRecovery", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsTrue(loaded.UState.Player.AI.FindGoals<SalvageDerelict>().First().RecoverySucceeded, "the success survives a save");
        Derelict risen = loaded.UState.Derelicts.Current.First();
        Assert.IsTrue(risen.Rising, "and the derelict still rises");
        loaded.UState.Derelicts.Update(Seconds(Derelict.RiseSeconds));
        Assert.IsNotNull(risen.RecoveredAs, "its ship taking its place");
        AssertEqual(loaded.UState.Player, risen.RecoveredAs.Loyalty, "for the empire that recovered it");
        AssertEqual("Vigilant Dawn", risen.RecoveredAs.VanityName, "under its name");
    }

    [TestMethod]
    public void TheShipAppearsDarkAndItsLightsFlickerOnOnceItHasPower()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        RolledASuccess(d);
        Ship ship = RisesForItsShip(d);
        AssertEqual(0f, ship.LightLevel(0f), "dark as it appears");

        var update = typeof(Ship).GetMethod("UpdateLights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        void Powered(float seconds) => update.Invoke(ship, new object[] { new FixedSimTime(seconds), true });
        Powered(1f);
        Assert.IsFalse(ship.LightsOut, "its lights come on after a second of power");
        bool dark = false, lit = false;
        for (int i = 0; i < 19; ++i)
        {
            Powered(0.1f);
            dark |= ship.LightLevel(0f) == 0f;
            lit |= ship.LightLevel(0f) == 1f;
        }
        Assert.IsTrue(dark && lit, "flickering over two seconds");
        Powered(0.2f);
        AssertEqual(1f, ship.LightLevel(0f), "then on for good");
        Powered(1f);
        AssertEqual(1f, ship.LightLevel(0f), "and steady");
    }

    [TestMethod]
    public void AFailedRollBreaksTheDerelictApartAndItsPlatformComesApart()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = OneTurnOfWorkLeft(d);
        BreaksApart(d);
        var notes = Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);

        Turns(order, 1);
        Assert.IsTrue(d.BrokenApart, "a roll over the chance, and a second one over a tenth, break the derelict apart");
        AssertEqual($"{WreckSystem.Name}: your recovery of the Dreadnought mk1-a failed, and it broke apart", notes.GetNotificationsAtomic().Single().Message,
                    "which tells only of the failure, not of a salvage's end");
        Assert.IsFalse(d.CanBeSalvaged, "so it can no longer be salvaged");
        Assert.IsFalse(WreckSystem.Derelicts.ContainsRef(d), "and leaves its system");
        Assert.IsTrue(order.RecoveryFailed && order.Dismantling, "its platform takes itself apart");
        Assert.IsFalse(d.Sinking, "the derelict breaks up first");
        d.Update(Seconds(Derelict.MaxBreakupSeconds + 0.1f));
        Assert.IsTrue(d.Sinking, "then sinks away");

        UState.StarDate = 1001f;
        SavedGame save = Universe.Save("UnitTest.SalvageRecovery", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsTrue(loaded.UState.Player.AI.FindGoals<SalvageDerelict>().First().RecoveryFailed, "the failure survives a save");
        Assert.IsTrue(loaded.UState.Derelicts.Current.First().BrokenApart, "and so does the broken derelict");

        Turns(order, SalvageDerelict.DismantleTurns);
        Assert.IsFalse(Player.AI.HasGoal(g => g == order), "the order ends with its platform");
        Assert.IsNull(d.Claimant, "and gives up the derelict");
    }

    [TestMethod]
    public void AFailedRecoveryPassedOnStaysFailed()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = OneTurnOfWorkLeft(d);
        BreaksApart(d);
        Turns(order, 1);
        Assert.IsTrue(order.RecoveryFailed, "setup: the roll failed");

        SalvageDerelict.PassOn(Player, Enemy);
        SalvageDerelict passed = Enemy.AI.FindGoals<SalvageDerelict>().First();
        Assert.IsTrue(passed.RecoveryFailed && passed.Finished, "the order passed on keeps its platform's work over");
    }

    // the player sees the platform at work from planet view, where the work's effect shows
    void SeesTheWork(SalvageDerelict order, Derelict d)
    {
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
        RunSimWhile((simTimeout: 5, fatal: true), () => !order.Platform.InPlayerSensorRange);
        Assert.IsFalse(order.RecoverySucceeded || order.RecoveryFailed, "setup: no roll yet");
        Assert.IsTrue(d.RecoveryEffectsShown, "setup: the last turn's work shows");
    }

    [TestMethod]
    public void ASuccessfulRollEndsTheWorksEffect()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        Succeeds(d);
        SalvageDerelict order = OneTurnOfWorkLeft(d);
        SeesTheWork(order, d);
        Turns(order, 1);
        Assert.IsTrue(order.RecoverySucceeded, "setup: the roll succeeds");
        Assert.IsFalse(d.RecoveryEffectsShown, "and the work's effect ends with it, its parts rising away with the derelict");
    }

    [TestMethod]
    public void TheTurnThatFinishesTheWorkSendsNoShuttles()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        Succeeds(d);
        SalvageDerelict order = OneTurnOfWorkLeft(d);
        Ship platform = order.Platform;
        SeesTheWork(order, d);
        platform.InFrustum = true;
        Universe.CargoShuttles.Update(Universe, TestSimStep);
        AssertEqual(0, Universe.CargoShuttles.InFlight(null, platform), "setup: no shuttles out");

        Turns(order, 1);
        Universe.CargoShuttles.Update(Universe, TestSimStep);
        Assert.IsTrue(order.RecoverySucceeded, "setup: the last of the work rolls");
        AssertEqual(0, Universe.CargoShuttles.InFlight(null, platform), "and sends no shuttles, its outcome showing at once");
    }

    [TestMethod]
    public void AFailedRollEndsTheWorksEffect()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = OneTurnOfWorkLeft(d);
        SeesTheWork(order, d);
        BreaksApart(d);
        Turns(order, 1);
        Assert.IsTrue(order.RecoveryFailed, "setup: the roll fails");
        Assert.IsFalse(d.RecoveryEffectsShown, "and the work's effect ends with it");
    }

    [TestMethod]
    public void ACatastropheBlowsUpTheDerelictAndItsPlatformAndHitsTheShipsNearItBlamingNobody()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = OneTurnOfWorkLeft(d);
        Ship platform = order.Platform;
        Ship near = SpawnShip("Dreadnought mk1-a", Enemy, d.Position + new Vector2(0f, d.Radius + 200f));
        RunObjectsSim(TestSimStep);
        float health = near.Health;
        near.SetLastDamagedBy(platform); // as if a shot had just hit it

        Explodes(d);
        Turns(order, 1);
        Assert.IsFalse(UState.Derelicts.Current.ContainsRef(d), "a roll over the chance, and a second one under a tenth, blow the derelict up");
        Assert.IsFalse(d.CanBeSalvaged, "so a panel still holding it shows it no more");
        Assert.IsFalse(platform.Active, "and its platform with it");
        Assert.IsFalse(Player.AI.HasGoal(g => g == order), "which ends the order");
        AssertLessThan(near.Health, health, "the blast hits a ship near it");
        Assert.IsNull(near.LastDamagedBy, "and blames nobody, not even the ship that hit it last");
    }

    [TestMethod]
    public void TheBreakInSteadiesTheDerelictEvenlyToAShipsLevelRestForGood()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        float tumbled = d.RotationZ;
        d.Tumble(1f);
        Assert.AreNotEqual(tumbled, d.RotationZ, "setup: it tumbles as it did at its death");
        typeof(Derelict).GetProperty(nameof(Derelict.RotationX))!.SetValue(d, 1f);
        typeof(Derelict).GetProperty(nameof(Derelict.RotationY))!.SetValue(d, -0.5f);
        typeof(Derelict).GetProperty(nameof(Derelict.RotationZ))!.SetValue(d, 2f);
        float heading = d.RotationZ;
        BuiltStation();
        SalvageDerelict order = AtWork(d, recover: true);
        float turn = UState.P.TurnTimer;

        Turns(order, 1);
        d.Update(Seconds(turn * 0.5f));
        AssertEqual(0.001f, 0.05f, d.Steadiness, "half a turn into the break-in's first turn, a twentieth of the way");
        AssertEqual(0.001f, 0.95f, d.RotationX, "its pitch coming level at the same pace");
        AssertEqual(0.001f, -0.475f, d.RotationY, "and its roll");
        d.Update(Seconds(turn));
        AssertEqual(0.001f, 0.1f, d.Steadiness, "no further than the break-in's turns so far take it");

        for (int i = 1; i < 10; ++i)
        {
            Turns(order, 1);
            d.Update(Seconds(turn));
        }
        AssertEqual(1f, d.Steadiness, "still once the break-in is over");
        AssertEqual(0f, d.RotationX, "and level");
        AssertEqual(0f, d.RotationY, "in both ways");
        AssertEqual(heading, d.RotationZ, "its heading kept");

        SalvageDerelict.Switch(Player, d, recover: false);
        Turns(order, 1);
        d.Update(Seconds(turn));
        d.Tumble(turn);
        AssertEqual(1f, d.Steadiness, "and it stays still once its platform turns to salvage");
        Assert.IsTrue(d.RotationX == 0f && d.RotationY == 0f && d.RotationZ == heading, "tumbling no more");
    }

    [TestMethod]
    public void WithNoBreakInTheWorkStartsAtOnceAndTheDerelictSteadiesAtOnce()
    {
        int breakIn = GlobalStats.Defaults.SalvageRecoveryBreakInTurns;
        try
        {
            GlobalStats.Defaults.SalvageRecoveryBreakInTurns = 0;
            Derelict d = Forms(Wreck, health: 0.4f);
            typeof(Derelict).GetProperty(nameof(Derelict.RotationX))!.SetValue(d, 1f);
            BuiltStation();
            SalvageDerelict order = AtWork(d, recover: true);
            Assert.IsFalse(order.BreakingIn, "with no break-in");
            Turns(order, 1);
            AssertEqual(0.01f, 24f, order.RecoveryWork, "the work starts the first turn");
            d.Update(Seconds(0.1f));
            AssertEqual(1f, d.Steadiness, "and the derelict steadies at once");
            AssertEqual(0f, d.RotationX, "level");
        }
        finally
        {
            GlobalStats.Defaults.SalvageRecoveryBreakInTurns = breakIn;
        }
    }

    NotificationManager TrackNotifications() => Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);

    [TestMethod]
    public void ThePlayerIsToldAsTheShipAppearsAndAClickSelectsItOrOnceItIsGoneShowsTheSystem()
    {
        Universe.ShipInfoUIElement ??= new(new SDGraphics.Rectangle(0, 0, 407, 242), Universe.ScreenManager, Universe);
        Universe.SystemInfoOverlay ??= new(new SDGraphics.Rectangle(0, 0, 407, 242), Universe.ScreenManager, Universe);
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = RolledASuccess(d);
        NotificationManager notes = TrackNotifications();
        UState.Derelicts.Update(Seconds(Derelict.RiseSeconds * 0.5f));
        AssertEqual(0, notes.NumberOfNotifications, "nothing is told while the derelict rises");

        Ship ship = RisesForItsShip(d);
        Notification told = notes.GetNotificationsAtomic().Single();
        AssertEqual($"{WreckSystem.Name}: the Dreadnought mk1-a is recovered", told.Message, "the player is told as its ship appears");
        AssertEqual(d.Design.BaseHull.IconPath, told.IconPath, "under the derelict's hull icon");
        AssertEqual("NewUI/icon_recovered_notification", told.SymbolPath, "with the green hammers");
        Assert.IsFalse(told.Pause, "it does not pause the game");
        AssertEqual("SnapToShipOrSystem", told.Action);
        Assert.AreSame(ship, told.ReferencedItem1, "a click selects the ship");
        Assert.AreSame(WreckSystem, told.ReferencedItem2, "or shows the system once it is gone");

        Turns(order, 1);
        Assert.IsTrue(order.Dismantling, "setup: the platform comes apart");
        AssertEqual(1, notes.NumberOfNotifications, "telling nothing more");

        notes.SnapToShipOrSystem(ship, WreckSystem);
        Assert.AreSame(ship, Universe.SelectedShip, "the click selects the ship");
        ship.LoyaltyChangeFromBoarding(Enemy, addNotification: false);
        RunObjectsSim(TestSimStep);
        AssertEqual(Enemy, ship.Loyalty, "setup: the ship is boarded");
        notes.SnapToShipOrSystem(ship, WreckSystem);
        Assert.AreSame(WreckSystem, Universe.SelectedSystem, "in other hands it shows the system");
        Assert.IsNull(Universe.SelectedShip, "selecting the ship no more");

        Universe.ClearSelectedItems();
        ship.Die(null, cleanupOnly: true);
        notes.SnapToShipOrSystem(ship, WreckSystem);
        Assert.AreSame(WreckSystem, Universe.SelectedSystem, "and once it is gone shows the system too");
    }

    [TestMethod]
    public void ARivalsRecoveredShipIsNotTold()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        RolledASuccess(d);
        SalvageDerelict.PassOn(Player, Enemy);
        NotificationManager notes = TrackNotifications();
        AssertEqual(Enemy, RisesForItsShip(d).Loyalty, "setup: a rival's ship takes the derelict's place");
        AssertEqual(0, notes.NumberOfNotifications, "which the player is not told of");
    }

    [TestMethod]
    public void AFailedRollIsToldAtOnceAndAClickShowsTheSystem()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = OneTurnOfWorkLeft(d);
        BreaksApart(d);
        NotificationManager notes = TrackNotifications();

        Turns(order, 1);
        Assert.IsTrue(d.BrokenApart, "setup: the roll breaks the derelict apart");
        Notification told = notes.GetNotificationsAtomic().Single();
        AssertEqual($"{WreckSystem.Name}: your recovery of the Dreadnought mk1-a failed, and it broke apart", told.Message, "the player is told at once");
        AssertEqual(d.Design.BaseHull.IconPath, told.IconPath, "under the derelict's hull icon");
        AssertEqual("NewUI/icon_recovery_failed_notification", told.SymbolPath, "with the yellow hammers");
        Assert.IsFalse(told.Pause, "it does not pause the game");
        AssertEqual("SnapToSystem", told.Action, "a click shows the system, as the derelict breaks up and sinks away");
        Assert.AreSame(WreckSystem, told.ReferencedItem1);

        Turns(order, SalvageDerelict.DismantleTurns);
        Assert.IsFalse(Player.AI.HasGoal(g => g == order), "setup: the platform came apart");
        AssertEqual(1, notes.NumberOfNotifications, "telling nothing more");
    }

    [TestMethod]
    public void ACatastropheIsToldAtOnceAndNotAsAPlatformLost()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = OneTurnOfWorkLeft(d);
        Explodes(d);
        NotificationManager notes = TrackNotifications();

        Turns(order, 1);
        Assert.IsFalse(Player.AI.HasGoal(g => g == order), "setup: the blast ends the order with its platform");
        Notification told = notes.GetNotificationsAtomic().Single();
        AssertEqual($"{WreckSystem.Name}: the Dreadnought mk1-a exploded as your platform worked to recover it, destroying the platform", told.Message,
                    "the player is told once, of the blast");
        AssertEqual(d.Design.BaseHull.IconPath, told.IconPath, "under the derelict's hull icon");
        AssertEqual("NewUI/icon_recovery_exploded_notification", told.SymbolPath, "with the red hammers");
        Assert.IsFalse(told.Pause, "it does not pause the game");
        AssertEqual("SnapToSystem", told.Action, "a click shows the system, as the derelict is gone");
        Assert.AreSame(WreckSystem, told.ReferencedItem1);
    }

    [TestMethod]
    public void ACalledOffRecoveryIsNotToldEvenAfterItsSalvageBroughtProduction()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        BuiltStation();
        SalvageDerelict order = AtWork(d);
        Turns(order, 1);
        AssertGreaterThan(order.Platform.GetProduction(), 0f, "setup: the platform stripped a load");
        SalvageDerelict.Switch(Player, d, recover: true);
        Universe.InvokePendingSimThreadActions();
        Turns(order, 1);
        AssertGreaterThan(order.Delivered, 0f, "setup: turned to recovery, it sent its load to the station");

        NotificationManager notes = TrackNotifications();
        SalvageDerelict.Cancel(Player, d);
        Universe.InvokePendingSimThreadActions();
        Turns(order, 1);
        Assert.IsTrue(order.CalledOff && order.Dismantling, "setup: the called-off platform comes apart");
        AssertEqual(0, notes.NumberOfNotifications, "and the player, who called the recovery off, is told nothing");
    }

    [TestMethod]
    public void APlatformLostAsTheDerelictRisesIsNotToldAndTheShipStillComes()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = RolledASuccess(d);
        NotificationManager notes = TrackNotifications();
        order.Platform.Die(null, cleanupOnly: true);

        Turns(order, 1);
        Assert.IsFalse(Player.AI.HasGoal(g => g == order), "setup: losing its platform as the derelict rises ends the order");
        AssertEqual(0, notes.NumberOfNotifications, "untold");
        RisesForItsShip(d);
        AssertEqual($"{WreckSystem.Name}: the Dreadnought mk1-a is recovered", notes.GetNotificationsAtomic().Single().Message,
                    "as the ship still comes, which is told");
    }

    [TestMethod]
    public void AStationLostAsTheDerelictRisesIsNotToldAndTheShipStillComes()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = RolledASuccess(d);
        Ship station = SalvageOps.StationIn(Player, WreckSystem);
        NotificationManager notes = TrackNotifications();
        station.Die(null, cleanupOnly: true);

        Turns(order, 1);
        Assert.IsFalse(Player.AI.HasGoal(g => g == order), "setup: losing the station as the derelict rises ends the order");
        AssertEqual(0, notes.NumberOfNotifications, "untold");
        RisesForItsShip(d);
        AssertEqual($"{WreckSystem.Name}: the Dreadnought mk1-a is recovered", notes.GetNotificationsAtomic().Single().Message,
                    "as the ship still comes, which is told");
    }

    [TestMethod]
    public void TheSalvageAlertsSwitchSilencesTheRecoveryNotifications()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = RolledASuccess(d);
        NotificationManager notes = TrackNotifications();
        UState.P.DisableSalvageWarning = true;

        Ship ship = RisesForItsShip(d);
        notes.AddRecoveryFailedNotification(order, exploded: false);
        notes.AddRecoveryFailedNotification(order, exploded: true);
        AssertEqual(0, notes.NumberOfNotifications, "with Salvage alerts off, no recovery's outcome is told");

        UState.P.DisableSalvageWarning = false;
        notes.AddDerelictRecoveredNotification(d, ship);
        notes.AddRecoveryFailedNotification(order, exploded: false);
        notes.AddRecoveryFailedNotification(order, exploded: true);
        AssertEqual(3, notes.NumberOfNotifications, "and with them on, each is");
    }

    [TestMethod]
    public void TheDerelictsWindowDropsTheRowOfADerelictThatCanNoLongerBeSelected()
    {
        Derelict recovered = Forms(Wreck, health: 0.4f);
        Derelict stripped = Forms(Wreck + new Vector2(0f, 5_000f), health: 0.4f);
        RolledASuccess(recovered);
        var window = new SystemDerelictsScreen(Universe, WreckSystem);
        window.LoadContent();
        Assert.IsTrue(window.Lists(recovered) && window.Lists(stripped), "setup: the window lists a derelict rising for its ship, and another");

        UState.Derelicts.Strip(stripped, null, stripped.ProductionLeft);
        window.Update(0f);
        Assert.IsFalse(window.Lists(stripped), "a derelict stripped empty leaves it");
        Assert.IsTrue(window.Lists(recovered), "while the one rising stays");
        RisesForItsShip(recovered);
        window.Update(0f);
        Assert.IsFalse(window.Lists(recovered), "until its ship takes its place");
    }
}
