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
        bool combatRepair = GlobalStats.Defaults.UseCombatRepair;
        GlobalStats.Defaults.UseCombatRepair = true; // the harness never advances TotalElapsed, so every ship reads as recently hit
        try
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
        finally
        {
            GlobalStats.Defaults.UseCombatRepair = combatRepair;
        }
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
