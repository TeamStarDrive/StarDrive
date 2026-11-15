using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A colony with a space port stays blockaded while a hostile ship is near it, also when its guns find nothing in reach.
/// A freighter heading to a blockaded colony from outside its system stops once the blockade has lasted a moment, waits
/// 25 seconds for it to lift, then gives up the trip; a blockade that blinks on for a frame does not stop it.
/// </summary>
[TestClass]
public class TradeBlockadeTests : StarDriveTest
{
    const string Freighter = "Owlwok Freighter S";
    const string EnemyShip = "Dodaving mk1-a";
    readonly Planet Exporter;
    readonly Planet Importer;
    static readonly PropertyInfo SpaceCombat = typeof(Planet).GetProperty(nameof(Planet.SpaceCombatNearPlanet))!;

    public TradeBlockadeTests()
    {
        LoadStarterShips(Freighter, EnemyShip);
        CreateUniverseAndPlayerEmpire();
        UState.Objects.EnableParallelUpdate = false;
        Exporter = AddHomeWorldToEmpire(new Vector2(200_000), Player, new Vector2(205_000), explored: true);
        Importer = AddDummyPlanet(new Vector2(300_000), 1f, 1f, 4f, new Vector2(305_000), explored: true);
        Importer.SetOwner(Player);
        Player.UpdateRallyPoints();
    }

    // a hostile ship near the colony, which the colony's system knows about
    void HostileNearExporter()
    {
        Assert.IsTrue(Exporter.HasSpacePort, "setup: a colony with a space port is blockaded by space combat near it");
        SpawnShip(EnemyShip, Enemy, Exporter.Position + new Vector2(Exporter.Radius + 5000f, 0f));
        RunSimWhile((simTimeout: 1.5, fatal: false));
        Assert.IsTrue(Exporter.System.HostileForcesPresent(Player) && Exporter.System.DangerousForcesPresent(Player),
                      "setup: the colony's system sees the enemy ship");
    }

    Ship FreighterOnItsWay()
    {
        Ship freighter = SpawnShip(Freighter, Player, Exporter.Position + new Vector2(8000f, 0f));
        freighter.LoadFood(5f);
        freighter.AI.SetTradePlan(ShipAI.Plan.DropOffGoods, Exporter, Importer, Goods.Food);
        Assert.AreNotEqual(Importer.System, freighter.System, "setup: the freighter is outside the colony's system");
        return freighter;
    }

    void Run(Ship freighter, float seconds, bool blockaded)
    {
        for (float time = 0f; time < seconds; time += TestSimStep.FixedTime)
        {
            SpaceCombat.SetValue(Importer, blockaded);
            freighter.AI.Update(TestSimStep);
        }
        Assert.AreEqual(blockaded, Importer.TradeBlocked, "setup: the colony is blockaded or not, as the test wants");
    }

    static bool OnItsTrip(Ship freighter)
        => freighter.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal) && goal.Plan == ShipAI.Plan.DropOffGoods;

    [TestMethod]
    public void AHostileShipNearAColonyKeepsItBlockadedThroughAGunScanThatFindsNothingInReach()
    {
        HostileNearExporter();
        for (int i = 0; i < 180 && !Exporter.SpaceCombatNearPlanet; ++i)
            Exporter.UpdateSpaceCombatBuildings(TestSimStep);
        Assert.IsTrue(Exporter.TradeBlocked, "setup: with a hostile ship near, the colony is blockaded");

        Exporter.ScanForSpaceCombatTargets(null, weaponRange: 100f, canLaunchShips: false);

        Assert.IsTrue(Exporter.TradeBlocked, "a gun scan that finds nothing in its reach does not lift the blockade");
    }

    [TestMethod]
    public void AColonysGunsThatFindNothingInReachDoNotLiftTheBlockade()
    {
        Building template = ResourceManager.BuildingsDict.Values.First(b => b.IsWeapon);
        Building gun = ResourceManager.CreateBuilding(Exporter, template);
        Exporter.TilesList.First(t => t.Habitable && t.NoBuildingOnTile).PlaceBuilding(gun, Exporter);
        gun.SpaceRange = 1000f;
        HostileNearExporter();

        bool blockaded = false, lifted = false;
        for (int i = 0; i < 180; ++i)
        {
            gun.WeaponTimer = 0f; // the gun looks for a target every frame
            Exporter.UpdateSpaceCombatBuildings(TestSimStep);
            if (Exporter.TradeBlocked) blockaded = true;
            else if (blockaded) lifted = true;
        }

        Assert.IsTrue(blockaded, "setup: with a hostile ship near, the colony is blockaded");
        Assert.IsFalse(lifted, "the colony's guns finding nothing in their reach do not lift the blockade");
    }

    static readonly FieldInfo LaunchWait = typeof(Building).GetField("DefenseShipRetryTimer", BindingFlags.Instance | BindingFlags.NonPublic)!;

    // the colony's capital, whose hangar holds its defense ships; the colony has no guns
    Building Capital()
    {
        Building capital = null;
        foreach (Building b in Exporter.Buildings)
        {
            Assert.IsFalse(b.IsWeapon, "setup: the colony has no guns");
            if (b.IsCapital) capital = b;
        }
        Assert.IsNotNull(capital, "setup: the colony has its capital");
        return capital;
    }

    // the capital's hangar full but its empire too poor for any of its ships, and an enemy ship in the hangar's reach, farther than a colony counts a threat from
    Building HangarThatCannotAffordItsShips(out Ship enemy)
    {
        Building capital = Capital();
        capital.UpdateCurrentDefenseShips(capital.DefenseShipsCapacity); // its hangar full, as the colony fills it over time
        Player.Money = 101f; // enough for the hangar to try, too little for any of its ships
        enemy = SpawnShip(EnemyShip, Enemy, Exporter.Position + new Vector2(18_000f, 0f));
        enemy.AI.OrderHoldPosition();
        RunSimWhile((simTimeout: 1.5, fatal: false));

        Assert.IsTrue(capital.DefenseShipsCapacity > 0 && !capital.HasLaunchedAllDefenseShips, "setup: the capital's hangar has ships to launch");
        Assert.IsNull(ShipBuilder.PickCostEffectiveShipToBuild(capital.DefenseShipsRole, Player, Player.Money - 90, Player.Money / 10),
                      "setup: but the empire can afford no design to launch them as");
        Assert.IsTrue(Exporter.System.DangerousForcesPresent(Player), "setup: the colony's system sees the enemy ship");
        Assert.IsFalse(enemy.Position.InRadius(Exporter.Position, 15_000f), "setup: farther than a colony counts a threat from");
        Assert.IsNotNull(Exporter.ScanForSpaceCombatTargets(null, capital.SpaceRange, canLaunchShips: true), "setup: but in the hangar's reach");
        return capital;
    }

    [TestMethod]
    public void AHangarWithNoShipItCanAffordDoesNotBlockadeItsColonyOverAShipItCannotFight()
    {
        Building capital = HangarThatCannotAffordItsShips(out _);

        bool blockaded = false;
        for (float time = 0f; time < 7f; time += TestSimStep.FixedTime) // past the hangar's wait after a failed launch, so it tries again
        {
            Exporter.UpdateSpaceCombatBuildings(TestSimStep);
            blockaded |= Exporter.TradeBlocked;
        }

        Assert.IsFalse(blockaded, "a hangar with no ship it can afford to launch does not blockade its colony over a ship it cannot fight");
        Assert.IsTrue((float)LaunchWait.GetValue(capital) > 0f, "setup: the hangar tried again in that time, and waits once more");
    }

    [TestMethod]
    public void AHangarsWaitAfterAFailedLaunchEndsWithTheFight()
    {
        Building capital = HangarThatCannotAffordItsShips(out Ship enemy);
        LaunchWait.SetValue(capital, 5f); // as just after a failed launch
        enemy.QueueTotalRemoval();
        RunSimWhile((simTimeout: 4, fatal: false)); // the system's 1 s check and the colony's 2 s recheck end the fight in about 3 s, before the 5 s wait runs out
        Assert.IsFalse(Exporter.System.DangerousForcesPresent(Player), "setup: the enemy ship is gone");

        Assert.AreEqual(0f, (float)LaunchWait.GetValue(capital), "the fight over, the hangar does not carry its wait into the next one");
    }

    [TestMethod]
    public void AHangarsWaitAfterAFailedLaunchEndsWithANewOwner()
    {
        Building capital = Capital();
        LaunchWait.SetValue(capital, 5f); // as just after a failed launch

        Exporter.SetOwner(Enemy);

        Assert.AreEqual(0f, (float)LaunchWait.GetValue(capital), "a new owner does not wait out the last owner's failed launch");
    }

    [TestMethod]
    public void AFreighterOutsideABlockadedColonysSystemWaitsAShortWhileThenGivesUpTheTrip()
    {
        Ship freighter = FreighterOnItsWay();

        Run(freighter, 24f, blockaded: true);
        Assert.IsTrue(OnItsTrip(freighter), "the freighter waits for the blockade to lift");

        Run(freighter, 2f, blockaded: true);
        Assert.IsFalse(OnItsTrip(freighter), "after 25 seconds it gives up the trip");
    }

    [TestMethod]
    public void AFreighterSavedWithTheOldLongerWaitWaitsNoLongerThanTheOthers()
    {
        Ship freighter = FreighterOnItsWay();
        Assert.IsTrue(freighter.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal), "setup: the freighter has its trip");
        goal.Trade.BlockadeTimer = 120f; // as a trip saved before the wait was shortened

        Run(freighter, 26f, blockaded: true);

        Assert.IsFalse(OnItsTrip(freighter), "after 25 seconds it gives up the trip");
    }

    [TestMethod]
    public void AFreighterWhoseBlockadeLiftsWaitsTheFullTimeForTheNextOne()
    {
        Ship freighter = FreighterOnItsWay();
        Run(freighter, 10f, blockaded: true);
        Run(freighter, TestSimStep.FixedTime, blockaded: false);

        Run(freighter, 20f, blockaded: true);

        Assert.IsTrue(OnItsTrip(freighter), "a blockade that lifted and came back starts the wait over");
    }

    // a freighter flying on sets where it thrusts each update; one braking for a blockade does not
    static bool FliesOn(Ship freighter) => freighter.AI.ThrustTarget != Vector2.Zero;

    [TestMethod]
    public void AFreighterFliesOnThroughTheBlockadeAGunShotLeavesButStopsForOneThatLasts()
    {
        Ship freighter = FreighterOnItsWay();
        Run(freighter, TestSimStep.FixedTime, blockaded: false);
        Assert.IsTrue(FliesOn(freighter), "setup: the freighter flies on toward the colony");

        Run(freighter, Planet.SpaceCombatRecheckSeconds, blockaded: true);
        Assert.IsTrue(FliesOn(freighter), "a blockade that lasts only until the colony's next check, as after a gun shot, does not stop it");

        Run(freighter, 1f, blockaded: true);
        Assert.IsFalse(FliesOn(freighter), "a blockade that lasts longer stops it");
        Assert.IsTrue(OnItsTrip(freighter), "and it waits for the blockade to lift");
    }
}
