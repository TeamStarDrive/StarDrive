using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Ship_Game.Utils;
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
/// slots at the production pace, plus a tenth, times 1 minus the engineers, and holds once all is done. Turning the
/// order to salvage keeps the work but a later recovery breaks in again; saves and passing on keep both. A rival's
/// recovery reads Recovering throughout, its break-in unshown.
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
    Derelict Forms(Vector2 at, string design = "Dreadnought mk1-a", float health = 1f)
    {
        TestShip ship = SpawnShip(design, Player, at);
        ship.Health = ship.HealthMax * health;
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

    // each roll is the next of these, from 0 to 1, the last one again and again: a dice roll passes under its percent
    sealed class FixedRandom : RandomBase
    {
        readonly Rolls Source;
        public FixedRandom(params double[] rolls) : base(1) { Source = new(rolls); }
        protected override Random Rand => Source;

        sealed class Rolls : Random
        {
            readonly double[] Values;
            int At;
            public Rolls(double[] values) { Values = values; }
            protected override double Sample() => Values[Math.Min(At++, Values.Length - 1)];
            public override double NextDouble() => Sample();
        }
    }

    static void RollsWith(Empire empire, params double[] rolls)
        => typeof(Empire).GetField(nameof(Empire.Random))!.SetValue(empire, new FixedRandom(rolls));

    // the player's recovery with all its work done, so its next turn rolls
    SalvageDerelict AllWorkDone(Derelict d)
    {
        BuiltStation();
        Player.data.SelectedTraitSet = "";
        SalvageDerelict order = AtWork(d, recover: true);
        Turns(order, 10 + (int)Math.Ceiling(DerelictRecovery.Work(Player, d) / 24f));
        AssertEqual(1f, DerelictRecovery.Progress(Player, d, order.RecoveryWork), "setup: all the work done");
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
        Turns(order, 1);
        AssertEqual(0.001f, work, order.RecoveryWork, "and the platform holds there");
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
        AssertEqual(10 + workTurns + 1, DerelictRecovery.TurnsLeft(Player, d, 0, 0f), "the break-in's turns, then the work's, then the roll's");
        AssertEqual(6 + workTurns + 1, DerelictRecovery.TurnsLeft(Player, d, 4, 0f), "four turns into the break-in");
        AssertEqual((int)Math.Ceiling((work - 24f) / 24f) + 1, DerelictRecovery.TurnsLeft(Player, d, 10, 24f), "a turn's work done after it");
        AssertEqual(1, DerelictRecovery.TurnsLeft(Player, d, 10, work), "only the roll's once all the work is done");
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
        AssertEqual(Outcome.Recovered, DerelictRecovery.Roll(Player, d, new FixedRandom(0.19)), "a roll under the chance recovers the ship");
        AssertEqual(Outcome.BrokenApart, DerelictRecovery.Roll(Player, d, new FixedRandom(0.21, 0.11)),
                    "one over it fails, and a second roll over a tenth breaks the derelict apart");
        AssertEqual(Outcome.Exploded, DerelictRecovery.Roll(Player, d, new FixedRandom(0.21, 0.09)), "while one under a tenth blows it up");
    }

    [TestMethod]
    public void TheRollComesTheTurnAfterAllTheWorkIsDoneAndASuccessHolds()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        RollsWith(Player, 0.0);
        SalvageDerelict order = AllWorkDone(d);
        Assert.IsFalse(order.RecoverySucceeded, "the full bar shows for a turn before the roll");

        float depth = d.Z;
        AssertGreaterThan(depth, 0f, "setup: the derelict rests below a ship's depth");
        Turns(order, 1);
        Assert.IsTrue(order.RecoverySucceeded, "the turn after, a roll under the chance succeeds");
        d.Update(Seconds(Derelict.RiseSeconds * 0.5f));
        AssertEqual(0.01f, depth * 0.5f, d.Z, "and the derelict rises evenly");
        d.Update(Seconds(Derelict.RiseSeconds * 0.5f));
        AssertEqual(0f, d.Z, "to a ship's depth over a few seconds");
        RollsWith(Player, 0.99, 0.99);
        Turns(order, 1);
        d.Update(Seconds(1f));
        Assert.IsTrue(d.CanBeSalvaged && d.Claimant == Player && d.Z == 0f, "where the recovery holds, rolling no more");

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageRecovery", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsTrue(loaded.UState.Player.AI.FindGoals<SalvageDerelict>().First().RecoverySucceeded, "through a save");
        Assert.IsTrue(loaded.UState.Derelicts.Current.First().Rising, "the derelict still up");
    }

    [TestMethod]
    public void AFailedRollBreaksTheDerelictApartAndItsPlatformComesApart()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = AllWorkDone(d);
        RollsWith(Player, 0.99, 0.99);
        var notes = Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);

        Turns(order, 1);
        Assert.IsTrue(d.BrokenApart, "a roll over the chance, and a second one over a tenth, break the derelict apart");
        AssertEqual(0, notes.NumberOfNotifications, "which does not tell of a salvage's end");
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
        SalvageDerelict order = AllWorkDone(d);
        RollsWith(Player, 0.99, 0.99);
        Turns(order, 1);
        Assert.IsTrue(order.RecoveryFailed, "setup: the roll failed");

        SalvageDerelict.PassOn(Player, Enemy);
        SalvageDerelict passed = Enemy.AI.FindGoals<SalvageDerelict>().First();
        Assert.IsTrue(passed.RecoveryFailed && passed.Finished, "the order passed on keeps its platform's work over");
    }

    [TestMethod]
    public void ACatastropheBlowsUpTheDerelictAndItsPlatformAndHitsTheShipsNearItBlamingNobody()
    {
        Derelict d = Forms(Wreck, health: 0.4f);
        SalvageDerelict order = AllWorkDone(d);
        Ship platform = order.Platform;
        Ship near = SpawnShip("Dreadnought mk1-a", Enemy, d.Position + new Vector2(0f, d.Radius + 200f));
        RunObjectsSim(TestSimStep);
        float health = near.Health;
        near.SetLastDamagedBy(platform); // as if a shot had just hit it

        RollsWith(Player, 0.99, 0.0);
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
}
