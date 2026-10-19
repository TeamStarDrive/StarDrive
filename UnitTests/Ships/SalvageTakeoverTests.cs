using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.CombatTactics;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using Block = Ship_Game.Commands.Goals.SalvageDerelict.Block;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A salvage platform cannot be boarded: a troop ship sent to board it drops the order with its troop aboard, a
/// right-click with a troop ship on it is refused, a carrier sends no shuttles at it, a planet sends no boarders at it,
/// a transporter passes it over for the next ship, and pirates never pick it for a raid; each check has a ship that
/// can be boarded beside it. An absorbed empire's built platforms pass to the absorber with their orders and claims, at
/// work or coming apart, wait for their change of hands a step later, and then work for the station that passed with
/// them or, where the absorber has one there, for the absorber's own, keeping their derelicts' techs, production, name
/// and icon and starting their points for those techs, and the production they delivered, again from none.
/// Its orders not yet built end with their claims and their constructors, and the absorber's own order on a derelict a
/// passed platform works ends, its constructor flying home, with no notification. A defeated empire's platforms work
/// for its rebels. Ships changing hands in a system show its derelicts to their new owner, as entering it would, and
/// the player is told; applied off the sim thread, it lands on it. A merger called off the sim thread runs on it, and
/// one a save catches before the platforms change hands loads with their orders.
/// </summary>
[TestClass]
public class SalvageTakeoverTests : StarDriveTest
{
    static readonly Vector2 SystemSite = new(600_000f, 600_000f);
    static readonly Vector2 Wreck = SystemSite + new Vector2(30_000f, 0);
    static readonly Vector2 PlayerStation = SystemSite + new Vector2(33_000f, 0);
    static readonly Vector2 EnemyStation = SystemSite + new Vector2(30_000f, 10_000f);
    const string Constructor = "Terran Constructor";
    const string Carrier = "TEST_Excalibur-Class Supercarrier";
    const string OtherPlatform = "Platform Base mk1-a";
    readonly SolarSystem WreckSystem;
    readonly IShipDesign Station;

    public SalvageTakeoverTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Terran Basic Salvage Station", ShipDesign.SalvagePlatformName, Carrier,
                         "Assault Shuttle", "Terran Assault Shuttle", OtherPlatform);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        WreckSystem = AddDummyPlanet(SystemSite, 1f, 1f, 1f, SystemSite + new Vector2(0, 60_000f), explored: true).System;
        AddHomeWorldToEmpire(new Vector2(200_000f), Player);
        Assert.IsTrue(ResourceManager.Ships.GetDesign("Terran Basic Salvage Station", out Station), "setup: the station is loaded");
        Player.data.CurrentConstructor = Constructor;
        Enemy.AddBuildableShip(ResourceManager.Ships.GetDesign(Constructor));
        Player.GetRelations(Enemy).AtWar = Enemy.GetRelations(Player).AtWar = false; // no fight, so only what a test does touches the ships
    }

    [TestCleanup]
    public override void Cleanup()
    {
        UState.Derelicts.Clear();
        base.Cleanup();
    }

    // a salvageable derelict of the player's ship the enemy destroyed, so both know of it; the ship itself is gone
    Derelict Forms(Vector2 at)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", Player, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false, Enemy), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        return UState.Derelicts.Current.Last();
    }

    Ship BuiltStation(Empire owner, Vector2 at)
    {
        var order = new SalvageOps(owner, WreckSystem, at, Station);
        owner.AI.AddGoal(order);
        order.AdvanceToNextStep(); // past the queueing, which needs a port
        order.TargetShip = SpawnShip(Station.Name, owner, at);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the station is built");
        return order.TargetShip;
    }

    // the empire's order with its platform built beside the derelict, as its constructor leaves it, about to strip
    SalvageDerelict AtWork(Empire owner, Derelict d, float hold = 100f)
    {
        Assert.IsTrue(SalvageDerelict.Order(owner, d), "setup: ordered");
        SalvageDerelict order = SalvageDerelict.OrderOf(owner, d);
        Ship constructor = order.FinishedShip;
        Assert.IsNotNull(constructor, "setup: a constructor launches");
        Assert.IsTrue(d.TryClaim(owner), "setup: the constructor claims the derelict as it starts to build");

        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, owner, order.BuildPosition);
        platform.CargoSpaceMax = hold;
        constructor.QueueTotalRemoval();
        order.OnPlatformBuilt(platform, constructor);
        order.Evaluate();
        AssertEqual("Salvage", order.StepName, "setup: the platform is built");
        return order;
    }

    // new ships join their empires' lists, which a merger and a defeat hand over
    void Settle(params Ship[] ships)
    {
        UState.Objects.UpdateLists();
        foreach (Ship ship in ships)
            Assert.IsTrue(ship.Loyalty.OwnedShips.Contains(ship), "setup: a ship among its empire's ships");
    }

    static SalvageDerelict OrderOf(Empire empire) => (SalvageDerelict)empire.AI.FindGoal(g => g is SalvageDerelict);

    Ship TroopShip(Empire owner, Vector2 at)
    {
        Ship troopShip = SpawnShip(owner.data.DefaultTroopShip, owner, at);
        ResourceManager.CreateTroop("Wyvern", owner).LandOnShip(troopShip);
        return troopShip;
    }

    static bool Boards(Ship ship, Ship target) => ship.AI.FindGoal(ShipAI.Plan.BoardShip, out _) && ship.AI.EscortTarget == target;

    [TestMethod]
    public void ATroopShipSentToBoardAPlatformDropsTheOrderWithItsTroopAboard()
    {
        LoadStarterShips(Enemy.data.DefaultTroopShip);
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation(Player, PlayerStation);
        Ship platform = AtWork(Player, d).Platform;
        Ship toStation = TroopShip(Enemy, station.Position - new Vector2(LandShip.BoardingRange(station) + 1000f, 0));
        Ship toPlatform = TroopShip(Enemy, platform.Position - new Vector2(LandShip.BoardingRange(platform) + 1000f, 0));
        toStation.AI.OrderTroopToBoardShip(station);
        toPlatform.AI.OrderTroopToBoardShip(platform);

        RunSimWhile((simTimeout: 30, fatal: true), () => !station.TroopsAreBoardingShip);
        RunObjectsSim(2f);
        Assert.IsFalse(platform.TroopsAreBoardingShip, "while the same order puts a troop aboard the station, none goes aboard the platform");
        AssertEqual(1, toPlatform.TroopCount, "its troop ship keeps its troop");
        Assert.IsFalse(toPlatform.AI.FindGoal(ShipAI.Plan.BoardShip, out _), "and drops the order");
        AssertEqual(Player, platform.Loyalty, "the platform stays the player's");
    }

    [TestMethod]
    public void ARightClickWithATroopShipOnAPlatformIsRefused()
    {
        LoadStarterShips(Player.data.DefaultTroopShip);
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation(Enemy, EnemyStation);
        Ship platform = AtWork(Enemy, d).Platform;
        var commands = new Ship_Game.Universe.ShipMoveCommands(Universe);
        Ship alone = TroopShip(Player, new Vector2(300_000f));
        Ship inGroup = TroopShip(Player, new Vector2(305_000f));
        Assert.IsTrue(alone.ShipData.Role == RoleName.troop && alone.DesignRole == RoleName.troop, "setup: troop ships");

        Assert.IsTrue(commands.AttackSpecificShip(alone, station) && Boards(alone, station), "setup: a right-click sends a troop ship to board the enemy's station");
        Assert.IsTrue(commands.RightClickOnShip(inGroup, station) && Boards(inGroup, station), "setup: and one of a group");

        Assert.IsFalse(commands.AttackSpecificShip(alone, platform), "a right-click on the enemy's platform is refused");
        Assert.IsTrue(Boards(alone, station), "leaving the troop ship's order as it was");
        Assert.IsFalse(commands.RightClickOnShip(inGroup, platform), "and for a troop ship of a group too");
        Assert.IsTrue(Boards(inGroup, station), "which keeps its order");
    }

    [TestMethod]
    public void ACarrierSendsNoShuttlesAtAPlatformThoughPiratesBoardAnyShip()
    {
        CreateAMinorFaction("Corsairs");
        Assert.IsTrue(Faction.WeArePirates, "setup: pirates, whose carriers board ships of any strength");
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, new Vector2(300_000f));
        Ship scout = SpawnShip("Vulcan Scout", Player, platform.Position + new Vector2(0, 600f));
        Ship carrier = SpawnShip(Carrier, Faction, platform.Position + new Vector2(0, -600f));
        for (int i = 0; i < 6; ++i)
            ResourceManager.CreateTroop("Wyvern", Faction).LandOnShip(carrier);
        typeof(Ship).GetProperty(nameof(Ship.SecondsAlive)).SetValue(carrier, 10); // past its first moments, when it sends no troops
        object tactics = typeof(CarrierBays).GetField("TroopTactics", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(carrier.Carrier);
        MethodInfo assault = typeof(AssaultShipCombat).GetMethod("AssaultTargetShip", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsFalse((bool)assault.Invoke(tactics, new object[] { platform }), "a carrier sends no shuttles at a platform");
        Assert.IsFalse(carrier.Carrier.AllTroopBays.Any(b => b.HangarShip != null), "and launches none");
        Assert.IsTrue((bool)assault.Invoke(tactics, new object[] { scout }), "setup: the same carrier sends them at a ship beside it");
    }

    [TestMethod]
    public void APlanetSendsNoBoardersAtAPlatform()
    {
        LoadStarterShips(Enemy.data.DefaultTroopShip);
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        Planet enemyWorld = AddHomeWorldToEmpire(new Vector2(400_000f), Enemy, new Vector2(405_000f), explored: true);
        var troops = new Troop[3];
        for (int i = 0; i < troops.Length; ++i)
        {
            troops[i] = ResourceManager.CreateTroop("Wyvern", Enemy);
            Assert.IsTrue(troops[i].TryLandTroop(enemyWorld), "setup: the planet must hold troops");
            troops[i].UpdateMoveActions(troops[i].MaxStoredActions);
        }
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, enemyWorld.Position + new Vector2(enemyWorld.Radius + 500f, 0));
        Ship inPlay = SpawnShip("Vulcan Scout", Player, enemyWorld.Position + new Vector2(0, enemyWorld.Radius + 5000f));
        inPlay.AI.OrderHoldPosition(MoveOrder.HoldPosition);
        RunSimWhile((simTimeout: 1.5, fatal: false));

        new AssaultBombers(enemyWorld, Enemy, Player).Evaluate();
        Ship[] boarders = troops.Select(t => t.HostShip).Where(s => s != null).ToArray();
        Assert.IsTrue(boarders.Length > 0, "setup: the planet must send boarders");
        foreach (Ship boarder in boarders)
            Assert.AreNotSame(platform, boarder.AI.EscortTarget, "no boarders go after the closer platform");
        Assert.IsTrue(boarders.Any(b => b.AI.EscortTarget == inPlay), "the boarders go after the ship beside it");
    }

    [TestMethod]
    public void ATransporterPassesOverAPlatformForTheNextShip()
    {
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, new Vector2(300_000f));
        Ship scout = SpawnShip("Vulcan Scout", Player, platform.Position + new Vector2(0, 300f));
        Ship boarder = SpawnShip("Vulcan Scout", Enemy, platform.Position + new Vector2(0, -100f));
        ResourceManager.CreateTroop("Wyvern", Enemy).LandOnShip(boarder);
        AssertEqual(0f, scout.ShieldPower, "setup: a ship with its shields down, which a transporter reaches");
        boarder.AI.NearByShips = new() { new(platform), new(scout) };

        MethodInfo beam = typeof(ShipAI).GetMethod("DoAssaultTransporterLogic", BindingFlags.Instance | BindingFlags.NonPublic);
        beam.Invoke(boarder.AI, new object[] { boarder.Modules[0] });
        Assert.IsFalse(platform.TroopsAreBoardingShip, "a transporter beams no troop onto the platform, the nearest ship");
        Assert.IsTrue(scout.TroopsAreBoardingShip, "but onto the next one in its reach");
    }

    [TestMethod]
    public void PiratesNeverPickASalvagePlatformForARaid()
    {
        CreateAMinorFaction("Corsairs");
        Assert.IsTrue(Faction.WeArePirates, "setup: the Corsairs are pirates");
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, new Vector2(300_000f));
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(platform.IsPlatform, "setup: a salvage platform has the platform role");
        Assert.IsFalse(Faction.Pirates.GetTarget(Player, Pirates.TargetType.Platform, out _), "pirates never pick a salvage platform for a raid");

        Ship other = SpawnShip(OtherPlatform, Player, new Vector2(310_000f));
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(Faction.Pirates.GetTarget(Player, Pirates.TargetType.Platform, out Ship target) && target == other,
                      "setup: they pick another platform");
    }

    [TestMethod]
    public void AnAbsorbedEmpiresPlatformPassesWithItsOrderAndClaimAndWorksOnOnceItChangesHands()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation(Enemy, EnemyStation);
        SalvageDerelict theirs = AtWork(Enemy, d);
        Ship platform = theirs.Platform;
        theirs.Evaluate();
        AssertEqual(0.01f, 12f, platform.GetProduction(), "setup: the platform holds a turn's work");
        AssertGreaterThan(theirs.Techs.Length, 0, "setup: its order keeps its derelict's techs");
        theirs.Gained[0] = 40f; // as its loads would have paid the merged empire
        Settle(station, platform);

        Player.AbsorbEmpire(Enemy);
        SalvageDerelict ours = OrderOf(Player);
        AssertEqual(platform, ours?.Platform, "the platform's order passes to the absorber");
        AssertEqual("Salvage", ours.StepName, "at work");
        AssertEqual(Player, d.Claimant, "with the claim");
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(Player, d), "so the derelict is the absorber's to call off");
        CollectionAssert.AreEqual(theirs.Techs, ours.Techs, "and its derelict's techs");
        AssertEqual(theirs.DerelictProduction, ours.DerelictProduction, "and whole production");
        AssertEqual("Dreadnought mk1-a", ours.DerelictName, "and name");
        AssertEqual(d.Design.BaseHull.IconPath, ours.DerelictIcon, "and hull icon");
        CollectionAssert.AreEqual(new float[theirs.Techs.Length], ours.Gained, "its points start again from none, as they were the merged empire's");

        AssertEqual(Enemy, platform.Loyalty, "setup: the platform changes hands a step after the merger");
        ours.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(ours), "the order waits for it");
        AssertEqual(0.01f, 12f, platform.GetProduction(), "stripping nothing meanwhile");

        RunObjectsSim(TestSimStep);
        AssertEqual(Player, platform.Loyalty, "setup: the platform changes hands");
        AssertEqual(station, SalvageOps.StationIn(Player, WreckSystem), "setup: and so does the station");
        ours.Evaluate();
        AssertEqual(0.01f, 24f, platform.GetProduction(), "then it strips for the absorber");
    }

    [TestMethod]
    public void WhereTheAbsorberHasAStationItsStationTakesOverTheRemovedOnesPlatforms()
    {
        Derelict d = Forms(Wreck);
        Ship theirStation = BuiltStation(Enemy, EnemyStation);
        Ship ourStation = BuiltStation(Player, PlayerStation);
        SalvageDerelict theirs = AtWork(Enemy, d, hold: 12f);
        Ship platform = theirs.Platform;
        theirs.Evaluate();
        Assert.IsTrue(SalvageDerelict.HoldFull(platform), "setup: the platform's hold is full");
        Settle(theirStation, platform);

        Player.AbsorbEmpire(Enemy);
        RunObjectsSim(TestSimStep);
        Assert.IsFalse(theirStation.Active, "setup: the absorbed station is removed where the absorber has one");

        SalvageDerelict ours = OrderOf(Player);
        ours.Evaluate();
        Assert.IsTrue(platform.Active && Player.AI.HasGoal(ours), "its platform works on");
        AssertEqual(0.01f, 12f, ourStation.GetProduction(), "for the absorber's station, which takes its load");
    }

    [TestMethod]
    public void APlatformComingApartPassesAndFinishesForTheAbsorber()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation(Enemy, EnemyStation);
        SalvageDerelict theirs = AtWork(Enemy, d);
        Ship platform = theirs.Platform;
        theirs.Evaluate();
        SalvageDerelict.Cancel(Enemy, d);
        theirs.Evaluate();
        Assert.IsTrue(theirs.Dismantling, "setup: the platform comes apart");
        AssertEqual(0.01f, 12f, theirs.Delivered, "setup: having sent its station a load of 12");
        Settle(station, platform);

        Player.AbsorbEmpire(Enemy);
        SalvageDerelict ours = OrderOf(Player);
        Assert.IsTrue(ours.CalledOff && ours.Dismantling, "it passes called off and coming apart");
        Assert.IsNull(d.Claimant, "with no claim to pass");
        AssertEqual(0f, ours.Delivered, "and its production delivered starts again from none, as it was the merged empire's");

        ours.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(ours) && platform.Dismantling, "it waits for its change of hands, still coming apart");
        RunObjectsSim(TestSimStep);
        ours.Evaluate();
        Assert.IsTrue(platform.Active, "then stands one turn");
        ours.Evaluate();
        Assert.IsFalse(platform.Active, "and is gone after its second");
        Assert.IsFalse(Player.AI.HasGoal(ours), "its order done");
    }

    [TestMethod]
    public void AnAbsorbedEmpiresOrderNotYetBuiltEndsWithItsClaimAndConstructor()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation(Enemy, EnemyStation);
        Assert.IsTrue(SalvageDerelict.Order(Enemy, d), "setup: ordered");
        Ship constructor = SalvageDerelict.OrderOf(Enemy, d).FinishedShip;
        Assert.IsTrue(d.TryClaim(Enemy), "setup: its constructor starts to build");
        Settle(station, constructor);

        Player.AbsorbEmpire(Enemy);
        Assert.IsNull(OrderOf(Player), "an order not yet built does not pass");
        Assert.IsNull(d.Claimant, "and its claim is given up");
        Assert.IsFalse(constructor.Active, "its constructor is lost in the merger");
    }

    [TestMethod]
    public void TheAbsorbersOwnOrderOnADerelictAPassedPlatformWorksEndsAndItsConstructorFliesHome()
    {
        Derelict d = Forms(Wreck);
        BuiltStation(Player, PlayerStation);
        Ship theirStation = BuiltStation(Enemy, EnemyStation);
        Assert.IsTrue(SalvageDerelict.Order(Player, d), "setup: the player orders the derelict");
        SalvageDerelict ownOrder = SalvageDerelict.OrderOf(Player, d);
        Ship ownConstructor = ownOrder.FinishedShip;
        RunSimWhile((simTimeout: 5, fatal: true), () => ownConstructor.IsLaunching);
        Ship platform = AtWork(Enemy, d).Platform;
        Settle(theirStation, platform);
        var notes = Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);

        Player.AbsorbEmpire(Enemy);
        RunObjectsSim(TestSimStep);
        int told = notes.NumberOfNotifications;
        ownOrder.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(ownOrder), "the absorber's own order on the derelict ends");
        AssertEqual(told, notes.NumberOfNotifications, "telling the player nothing, as the derelict stays the player's");
        AssertEqual(platform, SalvageDerelict.OrderOf(Player, d)?.Platform, "the passed platform's order is the derelict's");
        AssertEqual(Player, d.Claimant, "and keeps its claim");

        RunObjectsSim(TestSimStep);
        AssertEqual(AIState.ReturnToHangar, ownConstructor.AI.State, "the ended order's constructor flies home");
    }

    [TestMethod]
    public void ADefeatedEmpiresPlatformWorksForItsRebels()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation(Enemy, EnemyStation);
        SalvageDerelict theirs = AtWork(Enemy, d);
        Ship platform = theirs.Platform;
        theirs.Evaluate();
        Settle(station, platform);

        Enemy.SetAsDefeated();
        RunObjectsSim(TestSimStep);
        Empire rebels = platform.Loyalty;
        Assert.IsTrue(platform.Active && rebels != Enemy && rebels.data.IsRebelFaction, "the platform goes to the defeated empire's rebels");
        SalvageDerelict ours = OrderOf(rebels);
        AssertEqual(platform, ours?.Platform, "with its order");
        AssertEqual(rebels, d.Claimant, "and its claim");

        ours.Evaluate();
        AssertEqual(0.01f, 24f, platform.GetProduction(), "it strips on for the rebels' station");
    }

    // a salvageable derelict of the enemy's own ship that no one destroyed and the player never saw, so only the enemy knows of it
    Derelict FormsKnownToTheEnemyOnly(Vector2 at)
    {
        TestShip ship = SpawnShip("Dreadnought mk1-a", Enemy, at);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        Derelict d = UState.Derelicts.Current.Last();
        Assert.IsTrue(d.IsKnownBy(Enemy) && !d.IsKnownBy(Player), "setup: only the enemy knows of it");
        return d;
    }

    // two steps of the objects place new ships in their systems
    void PlaceInSystem(Ship ship)
    {
        RunObjectsSim(TestSimStep);
        RunObjectsSim(TestSimStep);
        AssertEqual(WreckSystem, ship.System, "setup: the ship is in its system");
    }

    [TestMethod]
    public void ShipsChangingHandsInASystemShowItsDerelictsToTheirNewOwnerAsEnteringItWould()
    {
        Derelict d = FormsKnownToTheEnemyOnly(Wreck);
        Ship station = BuiltStation(Enemy, EnemyStation);
        SalvageDerelict theirs = AtWork(Enemy, d);
        PlaceInSystem(theirs.Platform);
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        Assert.IsFalse(Player.DerelictSystems.Contains(WreckSystem), "setup: the player lists no derelicts there");

        Player.AbsorbEmpire(Enemy);
        Assert.IsFalse(d.IsKnownBy(Player), "setup: the absorber knows nothing of them until its ships there change hands");
        RunObjectsSim(TestSimStep);
        AssertEqual(Player, station.Loyalty, "setup: the ships there change hands");
        Assert.IsTrue(Player.DerelictSystems.Contains(WreckSystem) && d.IsKnownBy(Player), "the absorber learns of the system's derelicts");
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(Player, d), "so the passed order is its to call off");
        Assert.IsTrue(Universe.NotificationManager.IsNotificationPresent(
                          string.Format(Localizer.Token(GameText.NewSalvageableDerelict), WreckSystem.Name, d.Design.Name)),
                      "and the player is told, as on entering the system");
    }

    [TestMethod]
    public void AChangeOfHandsAppliedOffTheSimThreadShowsTheDerelictsOnIt()
    {
        Derelict d = FormsKnownToTheEnemyOnly(Wreck);
        Ship scout = SpawnShip("Vulcan Scout", Enemy, Wreck + new Vector2(0, 5_000f));
        PlaceInSystem(scout);

        scout.LoyaltyChangeByGift(Player, addNotification: false);
        FieldInfo simThread = typeof(UniverseScreen).GetField("SimThread", BindingFlags.Instance | BindingFlags.NonPublic);
        simThread.SetValue(Universe, new Thread(() => { }));
        try
        {
            UState.Objects.UpdateLists(); // as a save or the UI applies it
        }
        finally
        {
            simThread.SetValue(Universe, null);
        }
        AssertEqual(Player, scout.Loyalty, "setup: the scout changes hands off the sim thread");
        Assert.IsFalse(d.IsKnownBy(Player), "its new owner learns of the derelicts there only on the sim thread");

        Universe.InvokePendingSimThreadActions();
        Assert.IsTrue(Player.DerelictSystems.Contains(WreckSystem) && d.IsKnownBy(Player), "where it does");
    }

    [TestMethod]
    public void AMergerCalledOffTheSimThreadRunsOnIt()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation(Enemy, EnemyStation);
        Ship platform = AtWork(Enemy, d).Platform;
        Settle(station, platform);

        FieldInfo simThread = typeof(UniverseScreen).GetField("SimThread", BindingFlags.Instance | BindingFlags.NonPublic);
        simThread.SetValue(Universe, new Thread(() => { }));
        try
        {
            Player.AbsorbEmpire(Enemy); // as a federation accepted in the diplomacy screen
        }
        finally
        {
            simThread.SetValue(Universe, null);
        }
        Assert.IsFalse(Enemy.IsDefeated, "the merger waits for the sim thread");
        Assert.IsNotNull(OrderOf(Enemy), "its platform's order still the absorbed empire's");

        Universe.InvokePendingSimThreadActions();
        Assert.IsTrue(Enemy.IsDefeated, "and runs there");
        AssertEqual(platform, OrderOf(Player)?.Platform, "passing the platform's order on");
    }

    // as the game saves: started on the sim thread, written by a worker, then loaded
    UniverseScreen SaveAsTheGameDoesAndLoad(string name)
    {
        FieldInfo simThread = typeof(UniverseScreen).GetField("SimThread", BindingFlags.Instance | BindingFlags.NonPublic);
        simThread.SetValue(Universe, Thread.CurrentThread);
        try
        {
            UState.StarDate = 1001f; // the loader needs a stardate past 1000
            Universe.SaveAsync(name);
            for (int waited = 0; Universe.IsSaving && waited < 30_000; waited += 10)
                Thread.Sleep(10);
            Assert.IsFalse(Universe.IsSaving, "setup: the save is written");
        }
        finally
        {
            simThread.SetValue(Universe, null);
        }
        Universe.InvokePendingSimThreadActions();
        UniverseScreen loaded = LoadGame.Load(new FileInfo($"{SavedGame.DefaultSaveGameFolder}{name}.sav"), noErrorDialogs: true, startSimThread: false);
        Assert.IsNotNull(loaded, "setup: the save loads");
        return loaded;
    }

    [TestMethod]
    public void AMergedEmpiresPlatformStillChangingHandsWhenASaveStartsLoadsWithItsOrder()
    {
        Derelict d = Forms(Wreck);
        Ship station = BuiltStation(Enemy, EnemyStation);
        SalvageDerelict theirs = AtWork(Enemy, d);
        Ship platform = theirs.Platform;
        theirs.Evaluate();
        Settle(station, platform);

        Player.AbsorbEmpire(Enemy);
        UniverseScreen loaded = SaveAsTheGameDoesAndLoad("UnitTest.SalvagePlatformMergerSave");
        Empire player = loaded.UState.Player;
        SalvageDerelict ours = OrderOf(player);
        Assert.IsTrue(ours?.Platform is { Active: true, IsSalvagePlatform: true } && ours.Platform.Loyalty == player,
                      "the platform the merger brought loads with the absorber's order for it");
        AssertEqual(player, ours.Derelict?.Claimant, "and its claim");

        ours.Evaluate();
        AssertEqual(0.01f, 24f, ours.Platform.GetProduction(), "and works on in the loaded game");
    }
}
