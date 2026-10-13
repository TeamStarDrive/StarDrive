using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class PointDefenseTargetTests : StarDriveTest
{
    static readonly Vector2 Gun = new(200_000, 200_000);

    public PointDefenseTargetTests()
    {
        LoadStarterShips("Rocket Scout");
        CreateUniverseAndPlayerEmpire();
    }

    WeaponTestWrapper PointDefenseGun(Ship ship)
    {
        var gun = (WeaponTestWrapper)ship.Weapons[0];
        gun.TestTag_PD = true;
        gun.TestSalvoCount = 1;
        gun.TestProjectileCount = 1;
        gun.TestOrdinanceRequiredToFire = 0f;
        gun.TestPowerRequiredToFire = 0f;
        gun.Module.FieldOfFire = 45f.ToRadians();
        return gun;
    }

    // a PD gun aimed at a ship, then given shots with one missile at missileOffset from it
    (WeaponTestWrapper gun, Ship enemy, Projectile missile) GunOnAShipThenAMissile(Vector2 missileOffset)
    {
        Ship us = SpawnShip("TEST_Vulcan Scout", Player, Gun);
        WeaponTestWrapper gun = PointDefenseGun(us);
        Vector2 ahead = Gun + new Vector2(0, -1000);
        Ship enemy = SpawnShip("Rocket Scout", Enemy, ahead, ahead.DirectionToTarget(Gun));
        Ship[] noShips = Empty<Ship>.Array;

        Assert.IsTrue(gun.UpdateAndFireAtTarget(enemy, Empty<Projectile>.Array, noShips), "setup: with no missile about, the gun fires at the ship");
        Assert.AreEqual(enemy, (gun.FireTarget as ShipModule)?.GetParent(), "setup: the gun is aimed at the ship");

        Weapon rockets = enemy.Weapons.Find(w => w.UID == "Rocket");
        Projectile missile = Projectile.Create(rockets, enemy, Gun + missileOffset, Vectors.Down, us.Modules[0], playSound: false);
        var missiles = new[] { missile };
        for (int i = 0; i < 20 && gun.FireTarget is not Projectile; ++i)
        {
            gun.CooldownTimer = 0f;
            gun.UpdateAndFireAtTarget(enemy, missiles, noShips);
        }
        return (gun, enemy, missile);
    }

    [TestMethod]
    public void APointDefenseGunOnAShipTurnsToAMissileThatComesIntoReach()
    {
        (WeaponTestWrapper gun, _, Projectile missile) = GunOnAShipThenAMissile(new Vector2(0, -500));

        Assert.AreEqual(missile, gun.FireTarget, "a point defense gun aimed at a ship turns to a missile in its arc and range");
    }

    [TestMethod]
    public void APointDefenseGunStaysOnItsShipWhileTheMissileIsOutsideItsArc()
    {
        (WeaponTestWrapper gun, Ship enemy, _) = GunOnAShipThenAMissile(new Vector2(0, 500));

        Assert.AreEqual(enemy, (gun.FireTarget as ShipModule)?.GetParent(), "a missile behind the gun's arc does not pull it off its ship");
    }

    [TestMethod]
    public void AShipTracksTheNearestProjectilesFirst()
    {
        Ship us = SpawnShip("TEST_Vulcan Scout", Player, Gun);
        us.TrackingPower = 10;
        us.AI.TargetProjectiles = true;
        Ship enemy = SpawnShip("Rocket Scout", Enemy, Gun + new Vector2(0, -50_000));
        Weapon rockets = enemy.Weapons.Find(w => w.UID == "Rocket");
        float reach = us.WeaponsMaxRange;
        AssertGreaterThan(reach, 400f, "setup: the ship needs a weapon reach to scan for projectiles in");

        foreach (float share in new[] { 0.9f, 0.7f, 0.5f, 0.3f }) // farthest first, so the spatial ids run far to near
            Projectile.Create(rockets, enemy, Gun + new Vector2(0, -reach * share), Vectors.Down, us.Modules[0],
                              playSound: false, inheritedVelocity: Vector2.Zero);
        RunObjectsSim(TestSimStep);
        typeof(ShipAI).GetField("ProjectileScanTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(us.AI, 0f);
        us.AI.ScanForTargets(TestSimStep);

        Projectile[] tracked = us.AI.TrackProjectiles;
        AssertEqual(4, tracked.Length, "setup: every missile in reach is tracked");
        for (int i = 1; i < tracked.Length; ++i)
            AssertLessThanOrEqual(tracked[i - 1].Position.SqDist(us.Position), tracked[i].Position.SqDist(us.Position),
                                  "the tracked projectiles run nearest first");
    }
}
