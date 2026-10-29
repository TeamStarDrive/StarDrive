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
/// can be boarded beside it. A salvage ship boarded on its way is its boarder's once it changes hands, its order ending
/// then, and a ship like any other, which can be scrapped. An absorbed empire's standing platforms pass to the absorber
/// with their orders, claims and what they stripped, at work or turning back, wait for their change of hands a step
/// later, and then work for the absorber, keeping their derelicts' techs, production, name and icon and starting their
/// points for those techs again from none. Its orders not yet at work end with their claims, their salvage ships lost
/// in the merger, a called-off order passing without the claim of one of those on its derelict, and the absorber's own
/// order on a derelict a passed platform works ends, its salvage ship flying home, with no notification. A defeated
/// empire's platforms work for its rebels, the claims passed with them holding at the turn, while the turn lets go of a
/// defeated faction's claims though it keeps its goals. Ships changing hands in a system show its derelicts to their new
/// owner, as entering it would, and the player is told; applied off the sim thread, it lands on it. A merger called off
/// the sim thread runs on it, and one a save catches before the platforms change hands loads with their orders.
/// </summary>
[TestClass]
public class SalvageTakeoverTests : SalvageOrderTest
{
    const string Carrier = "TEST_Excalibur-Class Supercarrier";
    const string OtherPlatform = "Platform Base mk1-a";

    public SalvageTakeoverTests()
    {
        LoadStarterShips(Carrier, "Assault Shuttle", "Terran Assault Shuttle", OtherPlatform);
        Player.GetRelations(Enemy).AtWar = Enemy.GetRelations(Player).AtWar = false; // no fight, so only what a test does touches the ships
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

    // far from its derelict, on its way
    Vector2 OnItsWay => Homeworld.Position + new Vector2(2_000f, 0);

    [TestMethod]
    public void ATroopShipSentToBoardAPlatformDropsTheOrderWithItsTroopAboard()
    {
        LoadStarterShips(Enemy.data.DefaultTroopShip);
        Derelict d = Forms(Wreck);
        Ship platform = AtWork(d).Platform;
        Ship other = SpawnShip(OtherPlatform, Player, Wreck + new Vector2(0, 8_000f));
        Ship toOther = TroopShip(Enemy, other.Position - new Vector2(LandShip.BoardingRange(other) + 1000f, 0));
        Ship toPlatform = TroopShip(Enemy, platform.Position - new Vector2(LandShip.BoardingRange(platform) + 1000f, 0));
        toOther.AI.OrderTroopToBoardShip(other);
        toPlatform.AI.OrderTroopToBoardShip(platform);

        RunSimWhile((simTimeout: 30, fatal: true), () => !other.TroopsAreBoardingShip);
        RunObjectsSim(2f);
        Assert.IsFalse(platform.TroopsAreBoardingShip, "while the same order puts a troop aboard another platform, none goes aboard the salvage platform");
        AssertEqual(1, toPlatform.TroopCount, "its troop ship keeps its troop");
        Assert.IsFalse(toPlatform.AI.FindGoal(ShipAI.Plan.BoardShip, out _), "and drops the order");
        AssertEqual(Player, platform.Loyalty, "the platform stays the player's");
    }

    [TestMethod]
    public void ARightClickWithATroopShipOnAPlatformIsRefused()
    {
        LoadStarterShips(Player.data.DefaultTroopShip);
        Derelict d = Forms(Wreck);
        Ship platform = AtWork(d, empire: Enemy).Platform;
        Ship other = SpawnShip(OtherPlatform, Enemy, Wreck + new Vector2(0, 8_000f));
        var commands = new Ship_Game.Universe.ShipMoveCommands(Universe);
        Ship alone = TroopShip(Player, new Vector2(300_000f));
        Ship inGroup = TroopShip(Player, new Vector2(305_000f));
        Assert.IsTrue(alone.ShipData.Role == RoleName.troop && alone.DesignRole == RoleName.troop, "setup: troop ships");

        Assert.IsTrue(commands.AttackSpecificShip(alone, other) && Boards(alone, other), "setup: a right-click sends a troop ship to board the enemy's other platform");
        Assert.IsTrue(commands.RightClickOnShip(inGroup, other) && Boards(inGroup, other), "setup: and one of a group");

        Assert.IsFalse(commands.AttackSpecificShip(alone, platform), "a right-click on the enemy's salvage platform is refused");
        Assert.IsTrue(Boards(alone, other), "leaving the troop ship's order as it was");
        Assert.IsFalse(commands.RightClickOnShip(inGroup, platform), "and for a troop ship of a group too");
        Assert.IsTrue(Boards(inGroup, other), "which keeps its order");
    }

    [TestMethod]
    public void ASalvageShipBoardedOnItsWayIsItsBoardersOnceItChangesHandsAndCanBeScrapped()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order, at: OnItsWay);
        order.Evaluate();
        Assert.IsTrue(ship.WorksForSalvage && !ship.CanBeScrapped, "setup: on its way, it takes no orders");

        ship.LoyaltyChangeByGift(Enemy, addNotification: false); // as boarders take it
        order.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(order), "its order waits for the change of hands");
        UState.Objects.UpdateLists();
        AssertEqual(Enemy, ship.Loyalty, "setup: the ship changes hands");
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(order), "then the order ends");
        Assert.IsTrue(ship.Active, "and its boarder keeps the ship");
        Assert.IsTrue(!ship.WorksForSalvage && ship.CanBeScrapped, "a ship like any other, which can be scrapped");
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
    public void AnAbsorbedEmpiresPlatformPassesWithItsOrderClaimAndWhatItStrippedAndWorksOnOnceItChangesHands()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict theirs = AtWork(d, empire: Enemy);
        Ship platform = theirs.Platform;
        theirs.Evaluate();
        AssertEqual(0.01f, 12f, theirs.Load, "setup: the platform stripped a turn's work");
        AssertGreaterThan(theirs.Techs.Length, 0, "setup: its order keeps its derelict's techs");
        theirs.Gained[0] = 40f; // as its stripping would have paid the merged empire
        Settle(platform);

        Player.AbsorbEmpire(Enemy);
        SalvageDerelict ours = OrderOf(Player);
        AssertEqual(platform, ours?.Platform, "the platform's order passes to the absorber");
        AssertEqual("Salvage", ours.StepName, "at work");
        AssertEqual(Player, d.Claimant, "with the claim");
        AssertEqual(Block.Ordered, SalvageDerelict.WhyNoOrder(Player, d), "so the derelict is the absorber's to call off");
        AssertEqual(0.01f, 12f, ours.Load, "and what the platform stripped");
        CollectionAssert.AreEqual(theirs.Techs, ours.Techs, "and its derelict's techs");
        AssertEqual(theirs.DerelictProduction, ours.DerelictProduction, "and whole production");
        AssertEqual("Dreadnought mk1-a", ours.DerelictName, "and name");
        AssertEqual(d.Design.BaseHull.IconPath, ours.DerelictIcon, "and hull icon");
        CollectionAssert.AreEqual(new float[theirs.Techs.Length], ours.Gained, "its points start again from none, as they were the merged empire's");

        AssertEqual(Enemy, platform.Loyalty, "setup: the platform changes hands a step after the merger");
        ours.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(ours), "the order waits for it");
        AssertEqual(0.01f, 12f, ours.Load, "stripping nothing meanwhile");

        RunObjectsSim(TestSimStep);
        AssertEqual(Player, platform.Loyalty, "setup: the platform changes hands");
        ours.Evaluate();
        AssertEqual(0.01f, 24f, ours.Load, "then it strips for the absorber");
    }

    [TestMethod]
    public void APlatformTurningBackPassesAndBringsWhatItStrippedToTheAbsorber()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict theirs = AtWork(d, empire: Enemy);
        Ship platform = theirs.Platform;
        theirs.Evaluate();
        SalvageDerelict.Cancel(Enemy, d);
        theirs.Evaluate();
        Assert.IsTrue(theirs.Dismantling, "setup: the platform turns back into the ship with 12 aboard");
        Settle(platform);

        Player.AbsorbEmpire(Enemy);
        SalvageDerelict ours = OrderOf(Player);
        Assert.IsTrue(ours.CalledOff && ours.Dismantling, "it passes called off and turning back");
        Assert.IsNull(d.Claimant, "with no claim to pass");
        AssertEqual(0.01f, 12f, ours.Load, "and with what it stripped");

        ours.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(ours) && platform.Dismantling, "it waits for its change of hands, still turning back");
        RunObjectsSim(TestSimStep);
        ours.Evaluate();
        Assert.IsTrue(platform.Active, "then stands one turn");
        ours.Evaluate();
        Assert.IsFalse(platform.Active || Player.AI.HasGoal(ours), "and is gone after its second, its order done");
        DeliverRefund delivery = Player.AI.FindGoals<DeliverRefund>().Single();
        Assert.IsTrue(delivery.OldShip.IsSalvageShip && delivery.OldShip.Loyalty == Player, "the absorber's salvage ship is back");
        AssertEqual(0.01f, 12f, delivery.Load, "bringing the 12 home");
    }

    [TestMethod]
    public void ACalledOffOrderPassesWithoutTheClaimOfAnOrderNotYetAtWorkOnItsDerelict()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict theirs = AtWork(d, empire: Enemy);
        Ship platform = theirs.Platform;
        SalvageDerelict.Cancel(Enemy, d);
        Assert.IsTrue(theirs.CalledOff && d.Claimant == null, "setup: the order is called off, its platform winding down");
        SalvageDerelict again = Ordered(d, empire: Enemy);
        Ship ship = ShipBuilt(again);
        again.Evaluate();
        Assert.IsTrue(again.Deploying && d.Claimant == Enemy, "setup: the empire orders the derelict again, its salvage ship deploying");
        Settle(platform, ship);

        Player.AbsorbEmpire(Enemy);
        Assert.IsTrue(OrderOf(Player) is { CalledOff: true }, "the called-off order passes, its platform winding down");
        Assert.IsNull(d.Claimant, "without the claim of the order not yet at work, which ends and gives it up");
    }

    [TestMethod]
    public void AnAbsorbedEmpiresOrderNotAtWorkEndsWithItsClaimItsSalvageShipLostInTheMerger()
    {
        Derelict deploying = Forms(Wreck);
        Derelict away = Forms(Wreck + new Vector2(0, 5_000f));
        SalvageDerelict atSite = Ordered(deploying, empire: Enemy);
        Ship deployer = ShipBuilt(atSite);
        atSite.Evaluate();
        Assert.IsTrue(atSite.Deploying && deploying.Claimant == Enemy, "setup: one salvage ship deploys, claiming its derelict");
        SalvageDerelict flying = Ordered(away, empire: Enemy);
        Ship flyer = ShipBuilt(flying, at: OnItsWay);
        flying.Evaluate();
        AssertEqual("FlyToDerelict", flying.StepName, "setup: another flies out");
        Settle(deployer, flyer);

        Player.AbsorbEmpire(Enemy);
        Assert.IsNull(OrderOf(Player), "an order not yet at work does not pass");
        Assert.IsNull(deploying.Claimant, "and its claim is given up");
        Assert.IsFalse(deployer.Active || flyer.Active, "its salvage ships are lost in the merger");
    }

    [TestMethod]
    public void TheAbsorbersOwnOrderOnADerelictAPassedPlatformWorksEndsAndItsSalvageShipFliesHome()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict ownOrder = Ordered(d);
        Ship ownShip = ShipBuilt(ownOrder, at: OnItsWay);
        ownOrder.Evaluate();
        AssertEqual("FlyToDerelict", ownOrder.StepName, "setup: the player's salvage ship flies out");
        Ship platform = AtWork(d, empire: Enemy).Platform;
        Settle(platform);
        var notes = Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);

        Player.AbsorbEmpire(Enemy);
        RunObjectsSim(TestSimStep);
        int told = notes.NumberOfNotifications;
        ownOrder.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(ownOrder), "the absorber's own order on the derelict ends");
        AssertEqual(told, notes.NumberOfNotifications, "telling the player nothing, as the derelict stays the player's");
        AssertEqual(platform, SalvageDerelict.OrderOf(Player, d)?.Platform, "the passed platform's order is the derelict's");
        AssertEqual(Player, d.Claimant, "and keeps its claim");
        Assert.IsTrue(Player.AI.HasGoal(g => g is DeliverRefund delivery && delivery.OldShip == ownShip), "the ended order's salvage ship flies home");
    }

    [TestMethod]
    public void ADefeatedEmpiresPlatformWorksForItsRebels()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict theirs = AtWork(d, empire: Enemy);
        Ship platform = theirs.Platform;
        theirs.Evaluate();
        Settle(platform);

        Enemy.SetAsDefeated();
        RunObjectsSim(TestSimStep);
        Empire rebels = platform.Loyalty;
        Assert.IsTrue(platform.Active && rebels != Enemy && rebels.data.IsRebelFaction, "the platform goes to the defeated empire's rebels");
        SalvageDerelict ours = OrderOf(rebels);
        AssertEqual(platform, ours?.Platform, "with its order");
        AssertEqual(rebels, d.Claimant, "and its claim");

        ours.Evaluate();
        AssertEqual(0.01f, 24f, ours.Load, "it strips on for the rebels");
    }

    [TestMethod]
    public void AClaimPassedToTheRebelsHoldsAtTheTurnAndADefeatedFactionsIsLetGo()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict theirs = AtWork(d, empire: Enemy);
        Settle(theirs.Platform);
        Enemy.SetAsDefeated();
        Empire rebels = d.Claimant;
        Assert.IsTrue(rebels != Enemy && rebels.data.IsRebelFaction, "setup: the claim passes to the defeated empire's rebels");

        int turns = d.TurnsLeft;
        UState.Derelicts.UpdateTurn();
        AssertEqual(rebels, d.Claimant, "with its order, so it holds at the turn, before the platform changes hands");
        AssertEqual(turns, d.TurnsLeft, "and the timer stands");

        rebels.SetAsDefeated();
        Assert.IsNotNull(OrderOf(rebels), "setup: a defeated faction keeps its goals");
        UState.Derelicts.UpdateTurn();
        Assert.IsNull(d.Claimant, "but the turn lets its claim go");
        AssertEqual(turns - 1, d.TurnsLeft, "so the timer runs on");
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
        Ship platform = AtWork(d, empire: Enemy).Platform;
        PlaceInSystem(platform);
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        Assert.IsFalse(Player.DerelictSystems.Contains(WreckSystem), "setup: the player lists no derelicts there");

        Player.AbsorbEmpire(Enemy);
        Assert.IsFalse(d.IsKnownBy(Player), "setup: the absorber knows nothing of them until its ships there change hands");
        RunObjectsSim(TestSimStep);
        AssertEqual(Player, platform.Loyalty, "setup: the ships there change hands");
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
        Ship platform = AtWork(d, empire: Enemy).Platform;
        Settle(platform);

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
        SalvageDerelict theirs = AtWork(d, empire: Enemy);
        Ship platform = theirs.Platform;
        theirs.Evaluate();
        Settle(platform);

        Player.AbsorbEmpire(Enemy);
        UniverseScreen loaded = SaveAsTheGameDoesAndLoad("UnitTest.SalvagePlatformMergerSave");
        Empire player = loaded.UState.Player;
        SalvageDerelict ours = OrderOf(player);
        Assert.IsTrue(ours?.Platform is { Active: true, IsSalvagePlatform: true } && ours.Platform.Loyalty == player,
                      "the platform the merger brought loads with the absorber's order for it");
        AssertEqual(player, ours.Derelict?.Claimant, "and its claim");

        ours.Evaluate();
        AssertEqual(0.01f, 24f, ours.Load, "and works on in the loaded game");
    }
}
