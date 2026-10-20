using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Block = Ship_Game.Commands.Goals.SalvageDerelict.Block;
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

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.SalvageRecovery", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");
        SalvageDerelict loadedOrder = loaded.UState.Player.AI.FindGoals<SalvageDerelict>().First();
        Assert.IsTrue(loadedOrder.Recovering, "a recovery stays one through a save");
        AssertEqual(0.01f, 24f, loadedOrder.RecoveryWork, "with its work done");
        AssertEqual(10, loadedOrder.BreakInDone, "and its break-in");
        AssertEqual(0.001f, 0.4f, loaded.UState.Derelicts.Current.First().HealthAtDeath, "the derelict keeps the health its ship died with");
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
        AssertEqual(10 + workTurns, DerelictRecovery.TurnsLeft(Player, d, 0, 0f), "the break-in's turns, then the work's");
        AssertEqual(6 + workTurns, DerelictRecovery.TurnsLeft(Player, d, 4, 0f), "four turns into the break-in");
        AssertEqual((int)Math.Ceiling((work - 24f) / 24f), DerelictRecovery.TurnsLeft(Player, d, 10, 24f), "a turn's work done after it");
        AssertEqual(0, DerelictRecovery.TurnsLeft(Player, d, 10, work), "none once all is done");
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
}
