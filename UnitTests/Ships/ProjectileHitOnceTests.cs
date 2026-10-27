using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class ProjectileHitOnceTests : StarDriveTest
{
    public ProjectileHitOnceTests()
    {
        LoadStarterShips("Rocket Scout", "Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
    }

    // one collision step in which the shot overlaps two enemy ships at the same spot
    unsafe int HitsOnTwoOverlappingShips(string attackerDesign, out Ship first, out Ship second, float speed = 0f)
    {
        var at = new Vector2(300_000, 0);
        first = SpawnShip("Vulcan Scout", Player, at);
        second = SpawnShip("Vulcan Scout", Player, at);
        Ship attacker = SpawnShip(attackerDesign, Enemy, at + new Vector2(0, 3000));
        Projectile shot = Projectile.Create(attacker.Weapons[0], attacker, at, Vectors.Up, null, playSound: false);
        if (speed > 0f)
            shot.Velocity = Vectors.Up * speed;

        var objects = new SpatialObjectBase[] { shot, first, second };
        CollisionPair* pairs = stackalloc CollisionPair[2];
        pairs[0] = new CollisionPair(0, 1);
        pairs[1] = new CollisionPair(0, 2);
        return NarrowPhase.Collide(TestSimStep, pairs, 2, objects);
    }

    [TestMethod]
    public void AShellOverlappingTwoShipsInOneStepHitsOnlyOne()
    {
        // slow enough that the hit test looks at where it is, not along its path
        int hits = HitsOnTwoOverlappingShips("Vulcan Scout", out Ship first, out Ship second, speed: 300f);
        Assert.IsTrue(hits > 0, "setup: the shell must hit a ship it overlaps");
        AssertEqual(1, hits, $"one shell, one hit (health {first.Health:0}/{first.HealthMax:0} and {second.Health:0}/{second.HealthMax:0})");
    }

    [TestMethod]
    public void AMissileOverlappingTwoShipsInOneStepHitsOnlyOne()
    {
        int hits = HitsOnTwoOverlappingShips("Rocket Scout", out Ship first, out Ship second);
        Assert.IsTrue(hits > 0, "setup: the missile must hit a ship it overlaps");
        AssertEqual(1, hits, $"one missile, one hit (health {first.Health:0}/{first.HealthMax:0} and {second.Health:0}/{second.HealthMax:0})");
    }
}
