using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

// a repair drone's beam mends a ship out of combat at its full rate, and one fighting or just hit at a fifth of it
[TestClass]
public class RepairDroneBeamTests : StarDriveTest
{
    public RepairDroneBeamTests()
    {
        LoadStarterShips("Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
    }

    // the health a drone's beam gives a ship at half health in a second
    float HealedInASecond(Vector2 at, bool justHit)
    {
        Ship target = SpawnShip("Vulcan Scout", Player, at);
        foreach (ShipModule m in target.Modules)
            m.SetHealth(m.ActualMaxHealth * 0.5f, "Test");
        Ship carrier = SpawnShip("Vulcan Scout", Player, at + new Vector2(1_000f, 0));
        carrier.AI.FriendliesNearby = new[] { target };
        Weapon drones = ResourceManager.CreateWeapon(UState, "RepairDrone", carrier, carrier.Modules.First(m => m.InstalledWeapon != null));
        Projectile drone = Projectile.Create(drones, carrier, at, new Vector2(0f, -1f), null, playSound: false);
        drone.DroneAI.ChooseTarget();
        AssertEqual(target, drone.DroneAI.DroneTarget, "setup: the drone goes for the damaged ship");
        if (justHit)
            target.SetLastDamagedBy(carrier);
        AssertEqual(justHit, target.IsInRecentCombat, "setup: the ship counts as in combat only once hit");

        var beam = new DroneBeam(UState.CreateId(), drone.DroneAI);
        float before = target.Health;
        for (int i = 0; i < 60; ++i)
            beam.Update(TestSimStep);
        return target.Health - before;
    }

    [TestMethod]
    public void ADroneBeamMendsAShipJustHitAtAFifthOfItsRate()
    {
        float outOfCombat = HealedInASecond(new Vector2(600_000f), justHit: false);
        AssertGreaterThan(outOfCombat, 0f, "setup: the beam mends a ship out of combat");
        AssertEqual(0.01f, outOfCombat * 0.2f, HealedInASecond(new Vector2(-600_000f), justHit: true), "a ship just hit gets a fifth of it");
    }
}
