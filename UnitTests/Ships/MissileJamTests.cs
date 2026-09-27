using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class MissileJamTests : StarDriveTest
{
    public MissileJamTests()
    {
        LoadStarterShips("Rocket Scout", "Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
    }

    static Weapon GuidedWeapon(Ship ship)
    {
        foreach (Weapon w in ship.Weapons)
            if (w.Tag_Guided)
                return w;
        return null;
    }

    [TestMethod]
    public void AJammedMissileKeepsItsLockAndDiesAtItsDecoyPoint()
    {
        Ship launcher = SpawnShip("Rocket Scout", Player, Vector2.Zero);
        Ship target = SpawnShip("Vulcan Scout", Enemy, new Vector2(0, -2500));
        Weapon rocket = GuidedWeapon(launcher);
        Assert.IsNotNull(rocket, "setup: Rocket Scout must carry a guided weapon");
        RunObjectsSim(TestSimStep.FixedTime);

        target.ECMValue = 2f; // no content has ECM; above 1 it beats any resistance plus the roll
        float healthBefore = target.Health;
        Projectile missile = Projectile.Create(rocket, launcher, launcher.Position, Vectors.Up, target.Modules[0], playSound: false);
        MissileAI ai = missile.MissileAI;
        Assert.IsNotNull(ai, "setup: a guided weapon must launch a missile with a MissileAI");

        Vector2 lastPosition = missile.Position;
        RunSimWhile((15, fatal: true), () => missile.Active, body: () =>
        {
            if (ai.Jammed)
                Assert.IsNotNull(ai.Target, "a jammed missile must keep its lock and keep steering for its decoy point");
            lastPosition = missile.Position;
        });

        Assert.IsTrue(ai.Jammed, "setup: the missile must have been jammed");
        AssertLessThan(lastPosition.Distance(missile.FixedError), 400f, "a jammed missile must self-destruct at its decoy point");
        AssertEqual(0.001f, healthBefore, target.Health, "a jammed missile must not hit its target");
    }
}
