using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

// a repair drone's beam mends a ship out of combat at its full rate, and one fighting or just hit at a fifth of it, and is drawn where the drone is
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

    // the universe draws only the beams its spatial search finds in view
    [TestMethod]
    public void ADroneBeamIsFoundInViewWhereItsDroneIs()
    {
        var at = new Vector2(600_000f);
        Ship target = SpawnShip("Vulcan Scout", Player, at);
        foreach (ShipModule m in target.Modules)
            m.SetHealth(m.ActualMaxHealth * 0.5f, "Test");
        Ship carrier = SpawnShip("Vulcan Scout", Player, at + new Vector2(1_000f, 0));
        carrier.AI.FriendliesNearby = new[] { target };
        Weapon drones = ResourceManager.CreateWeapon(UState, "RepairDrone", carrier, carrier.Modules.First(m => m.InstalledWeapon != null));
        Projectile drone = Projectile.Create(drones, carrier, at + new Vector2(500f, 0), new Vector2(0f, -1f), null, playSound: false);
        RunObjectsSim(0.5f);

        DroneBeam beam = UState.Objects.GetAllProjectilesAndBeams().OfType<DroneBeam>().FirstOrDefault();
        Assert.IsNotNull(beam, "setup: the drone beams the damaged ship");
        Assert.IsTrue(beam.Source.InRadius(drone.Position, 100f), "setup: the beam starts at the drone");
        SpatialObjectBase[] inView = UState.Spatial.FindNearby(GameObjectType.Beam, new AABoundingBox2D(drone.Position, 2_000f), 16);
        Assert.IsTrue(inView.Contains(beam), $"the view around the drone finds its beam, at {beam.Position} radius {beam.RadiusX},{beam.RadiusY}");

        target.Position += new Vector2(0f, 3_000f);
        RunObjectsSim(TestSimStep.FixedTime);
        Assert.IsTrue(beam.Active, "setup: the beam still mends the ship");
        SpatialObjectBase[] atTheFarEnd = UState.Spatial.FindNearby(GameObjectType.Beam, new AABoundingBox2D(beam.ActualHitDestination, 100f), 16);
        Assert.IsTrue(atTheFarEnd.Contains(beam), $"and finds it out to where it reaches as the ship moves, at {beam.Position} radius {beam.RadiusX},{beam.RadiusY}");
    }
}
