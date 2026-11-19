using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Gameplay;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

[TestClass]
public class WeaponFanTests : StarDriveTest
{
    const string FanUid = "ZzFanCannon";

    public WeaponFanTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    static Map<string, IWeaponTemplate> Weapons => (Map<string, IWeaponTemplate>)typeof(ResourceManager)
        .GetField("WeaponsDict", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);

    [TestCleanup]
    public override void Cleanup()
    {
        Weapons.Remove(FanUid);
        base.Cleanup();
    }

    // each shot's angle from the aim, in degrees
    float[] FanAngles(int shots, int arc)
    {
        Assert.IsTrue(ResourceManager.GetWeaponTemplate("MassDriver", out IWeaponTemplate t), "setup: no MassDriver");
        var fan = (WeaponTemplate)typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(t, null);
        fan.UID = FanUid;
        fan.ProjectileCount = shots;
        fan.FireDispersionArc = arc;
        fan.FireImprecisionAngle = 0;
        Weapons[FanUid] = fan;

        Vector2 aim = new(1, 0);
        float aimDegrees = aim.ToDegrees();
        Weapon gun = ResourceManager.CreateWeapon(UState, FanUid, null, null);
        return gun.SalvoDirections(aim).Select(d => ((d.ToDegrees() - aimDegrees + 540f) % 360f) - 180f).OrderBy(a => a).ToArray();
    }

    [TestMethod]
    public void AnOddFanSendsItsMiddleShotStraightAtTheAim()
    {
        float[] angles = FanAngles(shots: 3, arc: 10);
        Assert.AreEqual(3, angles.Length);
        Assert.AreEqual(-10f / 3f, angles[0], 0.01f, "the shots stay a third of the arc apart");
        Assert.AreEqual(0f, angles[1], 0.01f, "the middle shot goes at the aim");
        Assert.AreEqual(10f / 3f, angles[2], 0.01f);
    }

    [TestMethod]
    public void AnEvenFanIsCenteredOnTheAim()
    {
        float[] angles = FanAngles(shots: 10, arc: 25);
        Assert.AreEqual(10, angles.Length);
        Assert.AreEqual(-11.25f, angles.First(), 0.01f, "the outer shots sit half a step inside the arc");
        Assert.AreEqual(11.25f, angles.Last(), 0.01f, "on both sides");
        Assert.AreEqual(0f, angles.Average(), 0.01f, "so the fan leans to neither side");
    }
}
