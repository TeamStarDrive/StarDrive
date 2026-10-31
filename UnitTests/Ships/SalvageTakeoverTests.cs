using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.AI.CombatTactics;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// Only pirates board a salvage platform: another empire's troop ship sent to board it drops the order with its troop
/// aboard, a right-click with a troop ship on it is refused, only a pirate carrier sends shuttles at it, a planet sends
/// no boarders at it, and a transporter passes it over for the next ship; each check has a ship that can be boarded
/// beside it. Pirates pick one at work for a raid as a salvage platform, never as a platform. A salvage ship boarded is
/// its boarder's once it changes hands, its mission and orders ending then, and a ship like any other, which can be
/// scrapped. An absorbed or defeated empire's salvage ends: its missions and orders end with their claims, its platforms
/// go, and its salvage ships pass to the absorber or its rebels as ships like any other, a constructor in flight
/// scuttled rather than called home. The turn lets go of a defeated faction's claims though it keeps its goals. Ships
/// changing hands in a system show its derelicts to their new owner, as entering it would, and the player is told;
/// applied off the sim thread, it lands on it. A merger called off the sim thread runs on it.
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
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, Wreck);
        Ship other = SpawnShip(OtherPlatform, Player, Wreck + new Vector2(0, 8_000f));
        // within boarding range, so the armed platform cannot disable the troop ship before it lands
        Ship toOther = TroopShip(Enemy, other.Position - new Vector2(LandShip.BoardingRange(other) - 50f, 0));
        Ship toPlatform = TroopShip(Enemy, platform.Position - new Vector2(LandShip.BoardingRange(platform) - 50f, 0));
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
    public void ASalvageShipBoardedIsItsBoardersOnceItChangesHandsEndingItsMissionAndOrder()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict order = Ordered(d);
        Ship ship = ShipBuilt(order, at: OnItsWay);
        SalvageMission mission = MissionOf(order);
        mission.Evaluate();
        Assert.IsTrue(ship.WorksForSalvage && !ship.CanBeScrapped, "setup: on its way, it takes no orders");

        ship.LoyaltyChangeByGift(Enemy, addNotification: false); // as boarders take it
        mission.Evaluate();
        Assert.IsTrue(Player.AI.HasGoal(mission), "its mission waits for the change of hands");
        UState.Objects.UpdateLists();
        AssertEqual(Enemy, ship.Loyalty, "setup: the ship changes hands");
        mission.Evaluate();
        order.Evaluate();
        Assert.IsFalse(Player.AI.HasGoal(mission) || Player.AI.HasGoal(order), "then its mission ends, and its order with it");
        Assert.IsTrue(ship.Active, "and its boarder keeps the ship");
        Assert.IsTrue(!ship.WorksForSalvage && ship.CanBeScrapped, "a ship like any other, which can be scrapped");
    }

    // a carrier of the empire's with troops aboard, past its first moments, when it sends none
    Ship CarrierOf(Empire empire, Vector2 at)
    {
        Ship carrier = SpawnShip(Carrier, empire, at);
        for (int i = 0; i < 6; ++i)
            ResourceManager.CreateTroop("Wyvern", empire).LandOnShip(carrier);
        typeof(Ship).GetProperty(nameof(Ship.SecondsAlive)).SetValue(carrier, 10);
        return carrier;
    }

    // true when the carrier sends its shuttles to board the target
    static bool Assaults(Ship carrier, Ship target)
    {
        object tactics = typeof(CarrierBays).GetField("TroopTactics", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(carrier.Carrier);
        MethodInfo assault = typeof(AssaultShipCombat).GetMethod("AssaultTargetShip", BindingFlags.Instance | BindingFlags.NonPublic);
        return (bool)assault.Invoke(tactics, new object[] { target });
    }

    [TestMethod]
    public void OnlyAPirateCarrierSendsShuttlesAtAPlatform()
    {
        Empire rebels = UState.CreateRebelsFromEmpireData(Enemy.data, Enemy);
        CreateAMinorFaction("Corsairs");
        Assert.IsTrue(rebels.IsFaction && !rebels.WeArePirates && Faction.WeArePirates, "setup: rebels and pirates, factions whose carriers board ships of any strength");
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, new Vector2(300_000f));
        Ship scout = SpawnShip("Vulcan Scout", Player, platform.Position + new Vector2(0, 600f));
        Ship rebel = CarrierOf(rebels, platform.Position + new Vector2(0, -600f));

        Assert.IsFalse(Assaults(rebel, platform), "a rebel carrier sends no shuttles at a platform");
        Assert.IsFalse(rebel.Carrier.AllTroopBays.Any(b => b.HangarShip != null), "and launches none");
        Assert.IsTrue(Assaults(rebel, scout), "setup: the same carrier sends them at a ship beside it");

        Ship pirate = CarrierOf(Faction, platform.Position + new Vector2(600f, 0));
        Assert.IsTrue(Assaults(pirate, platform), "but a pirate carrier sends them at the platform");
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
    public void PiratesPickASalvagePlatformAtWorkForARaidAsOneButNeverAsAPlatform()
    {
        CreateAMinorFaction("Corsairs");
        Assert.IsTrue(Faction.WeArePirates, "setup: the Corsairs are pirates");
        Ship platform = SpawnShip(ShipDesign.SalvagePlatformName, Player, new Vector2(300_000f));
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(platform.IsPlatform, "setup: a salvage platform has the platform role");
        Assert.IsFalse(Faction.Pirates.GetTarget(Player, Pirates.TargetType.Platform, out _), "pirates never pick a salvage platform as a platform");
        Assert.IsTrue(Faction.Pirates.GetTarget(Player, Pirates.TargetType.SalvagePlatform, out Ship target) && target == platform, "but as a salvage platform");

        Ship other = SpawnShip(OtherPlatform, Player, new Vector2(310_000f));
        RunObjectsSim(TestSimStep);
        Assert.IsTrue(Faction.Pirates.GetTarget(Player, Pirates.TargetType.Platform, out target) && target == other,
                      "setup: they pick another platform");
        for (int i = 0; i < 10; ++i) // a pick at random among those that qualify
            Assert.IsTrue(Faction.Pirates.GetTarget(Player, Pirates.TargetType.SalvagePlatform, out target) && target == platform, "which is no salvage platform");

        platform.StartDismantling(SalvageDerelict.DismantleTurns);
        Assert.IsFalse(Faction.Pirates.GetTarget(Player, Pirates.TargetType.SalvagePlatform, out _), "nor one taking itself apart");
    }

    static bool HasSalvage(Empire empire) => empire.AI.HasGoal(g => g is SalvageDerelict or SalvageMission);

    [TestMethod]
    public void AnAbsorbedEmpiresSalvageEndsItsPlatformGoesAndItsSalvageShipsPassAsShipsLikeAnyOther()
    {
        Derelict worked = Forms(Wreck);
        Derelict away = Forms(Wreck + new Vector2(0, 5_000f));
        SalvageDerelict atWork = AtWork(worked, empire: Enemy);
        Ship platform = atWork.Platform;
        Ship parked = MissionOf(atWork).SalvageShip;
        SalvageDerelict flying = Ordered(away, empire: Enemy);
        Ship flyer = ShipBuilt(flying, at: OnItsWay);
        MissionOf(flying).Evaluate();
        Assert.IsTrue(flyer.WorksForSalvage && AIState.MoveTo == flyer.AI.State, "setup: another salvage ship flies out");
        Settle(platform, parked, flyer);

        Player.AbsorbEmpire(Enemy);
        Assert.IsFalse(HasSalvage(Enemy) || HasSalvage(Player), "the absorbed empire's missions and orders end, none passing to the absorber");
        Assert.IsFalse(platform.Active, "its platform goes");
        Assert.IsNull(worked.Claimant, "with its claim");

        RunObjectsSim(TestSimStep);
        Assert.IsTrue(parked.Active && flyer.Active && parked.Loyalty == Player && flyer.Loyalty == Player, "its salvage ships pass to the absorber");
        Assert.IsTrue(!parked.WorksForSalvage && parked.CanBeScrapped && !flyer.WorksForSalvage, "as ships like any other");
    }

    [TestMethod]
    public void ADefeatedEmpiresSalvageEndsItsPlatformGoesAndItsSalvageShipGoesToItsRebels()
    {
        Derelict d = Forms(Wreck);
        SalvageDerelict theirs = AtWork(d, empire: Enemy);
        Ship platform = theirs.Platform;
        Ship ship = MissionOf(theirs).SalvageShip;
        Settle(platform, ship);

        Enemy.SetAsDefeated();
        RunObjectsSim(TestSimStep);
        Assert.IsFalse(platform.Active, "its platform goes");
        Assert.IsNull(d.Claimant, "with its claim");
        Empire rebels = ship.Loyalty;
        Assert.IsTrue(ship.Active && rebels != Enemy && rebels.data.IsRebelFaction, "its salvage ship goes to its rebels");
        Assert.IsFalse(HasSalvage(rebels) || HasSalvage(Enemy), "with no salvage to work");
        Assert.IsFalse(ship.WorksForSalvage, "a ship like any other");
    }

    [TestMethod]
    public void ADefeatedEmpiresConstructorInFlightIsScuttledNotCalledHome()
    {
        SalvageDerelict order = Ordered(Forms(Wreck), empire: Enemy);
        ShipBuilt(order);
        Ship constructor = Launched(order);

        Enemy.SetAsDefeated();
        Assert.IsFalse(HasSalvage(Enemy), "setup: the defeated empire's salvage ends");
        Assert.IsFalse(constructor.Active, "its constructor in flight is scuttled at once, not called home");
    }

    [TestMethod]
    public void ADefeatedFactionsClaimIsLetGoAtTheTurnThoughItKeepsItsGoals()
    {
        CreateAMinorFaction("Corsairs");
        Empire pirates = Faction;
        Derelict d = Forms(Wreck);
        var order = new SalvageDerelict(pirates, d);
        pirates.AI.AddGoal(order);
        Assert.IsTrue(d.TryClaim(pirates), "setup: the pirates claim the derelict");

        int turns = d.TurnsLeft;
        UState.Derelicts.UpdateTurn();
        AssertEqual(pirates, d.Claimant, "an order of theirs holds the claim at the turn");
        AssertEqual(turns, d.TurnsLeft, "and the timer stands");

        pirates.SetAsDefeated();
        Assert.IsTrue(pirates.AI.HasGoal(order), "setup: a defeated faction keeps its goals");
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
        Ship scout = SpawnShip("Vulcan Scout", Enemy, Wreck + new Vector2(0, 5_000f));
        PlaceInSystem(scout);
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
        Assert.IsFalse(Player.DerelictSystems.Contains(WreckSystem), "setup: the player lists no derelicts there");

        Player.AbsorbEmpire(Enemy);
        Assert.IsFalse(d.IsKnownBy(Player), "setup: the absorber knows nothing of them until its ships there change hands");
        RunObjectsSim(TestSimStep);
        AssertEqual(Player, scout.Loyalty, "setup: the ships there change hands");
        Assert.IsTrue(Player.DerelictSystems.Contains(WreckSystem) && d.IsKnownBy(Player), "the absorber learns of the system's derelicts");
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
        SalvageDerelict theirs = AtWork(d, empire: Enemy);
        Ship platform = theirs.Platform;
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
        Assert.IsTrue(Enemy.AI.HasGoal(theirs) && platform.Active, "its salvage still the absorbed empire's");

        Universe.InvokePendingSimThreadActions();
        Assert.IsTrue(Enemy.IsDefeated, "and runs there");
        Assert.IsFalse(Enemy.AI.HasGoal(theirs) || platform.Active, "ending its salvage");
    }
}
