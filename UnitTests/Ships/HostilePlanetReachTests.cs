using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.Empires;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

/// <summary>
/// A planet's guns aim at a ship's module and their shots hit its modules. A ship within the reach of the guns of a
/// planet whose owner may fire on it keeps its modules where its hull is, on screen or off, as one near enemy ships does.
/// </summary>
[TestClass]
public class HostilePlanetReachTests : StarDriveTest
{
    const float GunReach = 5000f;
    readonly Planet EnemyPlanet;
    readonly FixedSimTime EnemyScanInterval = new(EmpireConstants.EnemyScanInterval);

    public HostilePlanetReachTests()
    {
        CreateUniverseAndPlayerEmpire();
        LoadStarterShips(Player.data.DefaultTroopShip);
        EnemyPlanet = AddHomeWorldToEmpire(new Vector2(400_000), Enemy, new Vector2(405_000), explored: true);
        EnemyPlanet.System.UpdateOwnerList(); // as a colony made in the game lists its owner in its system
        Assert.IsTrue(EnemyPlanet.System.OwnerList.Contains(Enemy), "setup: the enemy owns a planet in the system");
        Assert.IsTrue(EnemyPlanet.Radius + 1000 < GunReach && GunReach + 2000 < EnemyPlanet.SensorRange,
                      "setup: the planet sees farther than its guns reach, and they reach past its surface");
    }

    void GiveThePlanetAGun()
    {
        Building template = ResourceManager.BuildingsDict.Values.First(b => b.IsWeapon);
        Building gun = ResourceManager.CreateBuilding(EnemyPlanet, template);
        EnemyPlanet.TilesList.First(t => t.Habitable && t.NoBuildingOnTile).PlaceBuilding(gun, EnemyPlanet);
        gun.SpaceRange = GunReach;
        AssertEqual(1f, GunReach, EnemyPlanet.GunReach(), "setup: the planet's gun");
    }

    // a troop ship of ours this far from the enemy planet, past its first sensor scan in the planet's system
    Ship TroopShipAt(float distance)
    {
        Ship ship = SpawnShip(Player.data.DefaultTroopShip, Player, EnemyPlanet.Position + new Vector2(distance, 0));
        ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(ship);
        RunObjectsSim(EnemyScanInterval);
        RunObjectsSim(EnemyScanInterval);
        Assert.AreEqual(EnemyPlanet.System, ship.System, "setup: the ship is in the planet's system");
        Assert.IsTrue(ship.AI.IsNonCombatant, "setup: a troop ship picks no targets, so no planet counts as an enemy for it");
        Assert.IsFalse(ship.AI.BadGuysNear, "setup: no enemy ship is near");
        Assert.IsFalse(ship.IsVisibleToPlayer, "setup: off screen");
        return ship;
    }

    // how far its first module lies from where its hull now is, after the ship moved 2000 and its modules were updated
    float ModuleLagAfterAMove(Ship ship)
    {
        ShipModule module = ship.Modules[0];
        Vector2 offset = module.Position - ship.Position;
        ship.Position += new Vector2(0, 2000);
        ship.UpdateModulePositions(TestSimStep);
        return module.Position.Distance(ship.Position + offset);
    }

    [TestMethod]
    public void AShipOffScreenInReachOfAnEnemyPlanetsGunsKeepsItsModulesOnItsHull()
    {
        GiveThePlanetAGun();
        Ship ship = TroopShipAt(EnemyPlanet.Radius + 1000);

        Assert.IsTrue(ship.AI.BadPlanetsNear, "the enemy planet's gun may fire on it");
        AssertEqual(1f, 0f, ModuleLagAfterAMove(ship), "its modules move with it, where the planet's guns aim and hit");
    }

    [TestMethod]
    public void AShipOffScreenBeyondTheReachOfAnEnemyPlanetsGunsLeavesItsModulesAlone()
    {
        GiveThePlanetAGun();
        Ship ship = TroopShipAt(GunReach + 2000);

        Assert.IsFalse(ship.AI.BadPlanetsNear, "the planet sees it, but its gun does not reach it");
        AssertEqual(1f, 2000f, ModuleLagAfterAMove(ship), "off screen with nothing near that may fire on it, its modules wait for a need");
    }

    [TestMethod]
    public void AShipOffScreenNearAnEnemyPlanetWithoutGunsLeavesItsModulesAlone()
    {
        Ship ship = TroopShipAt(EnemyPlanet.Radius + 1000);

        Assert.IsFalse(ship.AI.BadPlanetsNear, "a planet without guns does not fire on it");
        AssertEqual(1f, 2000f, ModuleLagAfterAMove(ship), "its modules wait for a need");
    }
}
