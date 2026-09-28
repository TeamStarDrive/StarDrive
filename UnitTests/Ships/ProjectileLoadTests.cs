using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace UnitTests.Ships;

[TestClass]
public class ProjectileLoadTests : StarDriveTest
{
    public ProjectileLoadTests()
    {
        LoadStarterShips("Dreadnought mk1-a", "Corsair");
        CreateUniverseAndPlayerEmpire();
    }

    Projectile Fire(string weaponUid)
    {
        Ship target = SpawnShip("Dreadnought mk1-a", Player, new Vector2(0, 10000));
        Ship attacker = SpawnShip("Corsair", Enemy, Vector2.Zero);
        Weapon w = ResourceManager.CreateWeapon(UState, weaponUid, attacker, null);
        return Projectile.Create(w, attacker, attacker.Position,
                                 (target.Position - attacker.Position).Normalized(), target, playSound: false);
    }

    [TestMethod]
    public void AShotInFlightKeepsItsSpeedWhenLoaded()
    {
        Projectile shot = Fire("MassDriver");
        Assert.IsFalse(shot.Weapon.Tag_Guided, "setup: MassDriver must fire unguided shots");
        Vector2 inFlight = shot.Velocity;

        shot.OnDeserialized(UState);

        AssertEqual(0.01f, inFlight, shot.Velocity, "a loaded shot must fly on at the speed it was saved with");
    }

    [TestMethod]
    public void ADeflectedShotKeepsItsLostMomentumWhenLoaded()
    {
        Projectile shot = Fire("MassDriver");
        float fullSpeed = shot.Velocity.Length();
        shot.Deflect(Player, new Vector3(shot.Position, 0f));
        Vector2 deflected = shot.Velocity;
        AssertLessThan(deflected.Length(), fullSpeed, "setup: a deflected shot must lose speed");

        shot.OnDeserialized(UState);

        AssertEqual(0.01f, deflected, shot.Velocity, "a loaded deflected shot must keep its slower flight");
    }

    [TestMethod]
    public void AGuidedMissileKeepsItsFlightWhenLoaded()
    {
        Projectile missile = Fire("Missile");
        Assert.IsTrue(missile.Weapon.Tag_Guided, "setup: Missile must be guided");
        AssertEqual(0f, missile.Weapon.DelayedIgnition, "setup: Missile must ignite at launch");
        for (int i = 0; i < 20; ++i)
            missile.Update(TestSimStep);
        Vector2 inFlight = missile.Velocity;
        float heading = missile.Rotation;
        AssertGreaterThan(inFlight.Length(), 100f, "setup: the missile must be under way; its launcher is at rest");
        AssertGreaterThan(Vectors.AngleDifference(heading.RadiansToDirection(), missile.Owner.Rotation.RadiansToDirection()),
                          0.1f, "setup: the missile must have turned off its launcher's heading");

        missile.OnDeserialized(UState);

        AssertEqual(0.01f, inFlight, missile.Velocity, "a loaded missile must not drop back to its launcher's speed");
        AssertEqual(0.01f, heading, missile.Rotation, "a loaded missile must not snap back to its launcher's heading");
    }
}
