using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class PlanetRepairTests : StarDriveTest
{
    Planet OurWorld, EnemyWorld, ForeignWorld;

    public PlanetRepairTests()
    {
        CreateUniverseAndPlayerEmpire();
        CreateThirdMajorEmpire();
        OurWorld = AddHomeWorldToEmpire(new Vector2(200_000, 0), Player);
        EnemyWorld = AddHomeWorldToEmpire(new Vector2(0, 200_000), Enemy);
        ForeignWorld = AddHomeWorldToEmpire(new Vector2(-200_000, 0), ThirdMajor);
    }

    float RepairPerSecond(Planet orbit, Vector2 spawnAt)
    {
        TestShip ship = SpawnShip("Vulcan Scout", Player, spawnAt);
        if (orbit != null)
        {
            orbit.GeodeticManager.AffectNearbyShips();
            ship.OrderToOrbit(orbit, clearOrders: true);
            RunSimWhile((60, fatal: true), () => !ship.AI.IsOrbiting(orbit));
        }

        foreach (ShipModule m in ship.Modules)
            m.SetHealth(m.ActualMaxHealth * 0.5f, "Test");
        RunObjectsSim(3f);
        return ship.CurrentRepairPerSecond;
    }

    // A hit stops self-repair for 5 seconds of play; real time going by while paused does not count.
    [TestMethod]
    public void AHitStopsRepairForFiveSecondsOfPlayNotOfRealTime()
    {
        TestShip ship = SpawnShip("Vulcan Scout", Player, new Vector2(0, -300_000));
        ship.SetLastDamagedBy(EnemyWorld);
        Assert.IsFalse(ship.CanRepair, "just hit, it does not repair");

        PropertyInfo realTime = typeof(GameBase).GetProperty(nameof(GameBase.TotalElapsed));
        float realTimeNow = GameBase.Base.TotalElapsed;
        try
        {
            realTime!.SetValue(GameBase.Base, realTimeNow + 600f);
            Assert.IsFalse(ship.CanRepair, "ten minutes paused do not count");
        }
        finally
        {
            realTime!.SetValue(GameBase.Base, realTimeNow);
        }

        RunObjectsSim(4f);
        Assert.IsFalse(ship.CanRepair, "4 seconds of play on, it is still recently hit");
        RunObjectsSim(1.5f);
        Assert.IsTrue(ship.CanRepair, "after 5 seconds of play it repairs again");
    }

    float SelfRepair() => RepairPerSecond(null, new Vector2(0, -300_000));
    float OrbitRepair(Planet p) => RepairPerSecond(p, p.Position + new Vector2(p.Radius + 500, 0));

    [TestMethod]
    public void OurOwnPlanetRepairsAShipInOrbit()
    {
        float self = SelfRepair();
        float orbiting = OrbitRepair(OurWorld);
        AssertGreaterThan(OurWorld.GeodeticManager.RepairRatePerSecond, 0f);
        AssertEqual(1f, self + OurWorld.GeodeticManager.RepairRatePerSecond, orbiting,
                    "orbiting our own world should add its repair rate");
    }

    [TestMethod]
    public void AnAllysPlanetRepairsAShipInOrbit()
    {
        Player.SignAllianceWith(ThirdMajor);
        AssertTrue(Player.IsAlliedWith(ThirdMajor));
        float self = SelfRepair();
        float orbiting = OrbitRepair(ForeignWorld);
        AssertGreaterThan(ForeignWorld.GeodeticManager.RepairRatePerSecond, 0f);
        AssertEqual(1f, self + ForeignWorld.GeodeticManager.RepairRatePerSecond, orbiting,
                    "orbiting an ally's world should add its repair rate");
    }

    [TestMethod]
    public void AnEnemyPlanetDoesNotRepairAShipInOrbit()
    {
        AssertTrue(Player.IsAtWarWith(Enemy));
        float self = SelfRepair();
        float orbiting = OrbitRepair(EnemyWorld);
        AssertGreaterThan(EnemyWorld.GeodeticManager.RepairRatePerSecond, 0f);
        AssertEqual(0.01f, self, orbiting, "an enemy world must not repair our ship");
    }

    [TestMethod]
    public void AForeignPlanetAtPeaceDoesNotRepairAShipInOrbit()
    {
        AssertFalse(Player.IsAtWarWith(ThirdMajor));
        AssertFalse(Player.IsAlliedWith(ThirdMajor));
        float self = SelfRepair();
        float orbiting = OrbitRepair(ForeignWorld);
        AssertGreaterThan(ForeignWorld.GeodeticManager.RepairRatePerSecond, 0f);
        AssertEqual(0.01f, self, orbiting, "a foreign world we are not allied with must not repair our ship");
    }

    [TestMethod]
    public void APlanetThatLostItsOwnerDoesNotRepairAShipInOrbit()
    {
        EnemyWorld.GeodeticManager.AffectNearbyShips();
        EnemyWorld.WipeOutColony(Player);
        Assert.IsNull(EnemyWorld.Owner, "setup: the colony must be gone");
        AssertGreaterThan(EnemyWorld.GeodeticManager.RepairRatePerSecond, 0f,
                          "setup: the dead colony must still hold its last repair rate");
        float self = SelfRepair();
        float orbiting = OrbitRepair(EnemyWorld);
        AssertEqual(0.01f, self, orbiting, "a world nobody owns must not repair our ship");
    }
}
