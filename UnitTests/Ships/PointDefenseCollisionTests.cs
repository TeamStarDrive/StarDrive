using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class PointDefenseCollisionTests : StarDriveTest
{
    public PointDefenseCollisionTests()
    {
        LoadStarterShips("Rocket Scout", "Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
    }

    [TestMethod]
    public void APointDefenseShellHitsAMissileThatIsListedBeforeIt()
    {
        var gun = new Vector2(200_000, 200_000);
        Ship defender = SpawnShip("Vulcan Scout", Player, gun);
        Ship attacker = SpawnShip("Rocket Scout", Enemy, gun + new Vector2(0, 5000));
        Vector2 at = gun + new Vector2(0, 2000);
        Projectile missile = Projectile.Create(ResourceManager.CreateWeapon(UState, "Missile", attacker, null), attacker,
                                               at, Vectors.Up, null, playSound: false);
        Projectile flak = Projectile.Create(ResourceManager.CreateWeapon(UState, "FlakAA Cannon", defender, null), defender,
                                            at, Vectors.Up, null, playSound: false);

        RunObjectsSim(TestSimStep);
        AssertLessThan(missile.SpatialIndex, flak.SpatialIndex, "setup: the missile must be listed before the shell");
        RunObjectsSim(TestSimStep);

        AssertLessThanOrEqual(missile.Health, 0f, "a point defense shell must hit a missile it overlaps, whichever comes first");
    }
}
