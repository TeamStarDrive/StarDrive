using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;

namespace UnitTests.Ships;

[TestClass]
public class WeaponDamagePerSecondTests : StarDriveTest
{
    public WeaponDamagePerSecondTests()
    {
        LoadStarterShips("Vulcan Scout");
        CreateUniverseAndPlayerEmpire();
    }

    static IWeaponTemplate Weapon(string uid)
    {
        Assert.IsTrue(ResourceManager.GetWeaponTemplate(uid, out IWeaponTemplate t), $"setup: no weapon {uid}");
        return t;
    }

    [TestMethod]
    [DataRow("Railgun")]
    [DataRow("LaserBeam")]
    [DataRow("ClusterMissiles")]
    public void WithNoBonusesAWeaponDealsItsBaseDamagePerSecond(string uid)
    {
        IWeaponTemplate w = Weapon(uid);
        AssertEqual(0.01f, w.DamagePerSecond, WeaponTemplate.DamagePerSecondWithTechs(w, Player.data),
                    "an empire with no weapon bonuses deals the weapon's own damage per second");
    }

    [TestMethod]
    public void AWeaponTypeDamageBonusRaisesTheDamagePerSecondOfThatType()
    {
        IWeaponTemplate railgun = Weapon("Railgun");
        IWeaponTemplate laser = Weapon("LaserBeam");
        Player.data.WeaponTags[WeaponTag.Kinetic].Damage = 0.25f;

        AssertEqual(0.01f, railgun.DamagePerSecond * 1.25f, WeaponTemplate.DamagePerSecondWithTechs(railgun, Player.data),
                    "a kinetic weapon deals 25% more with a 25% kinetic damage bonus");
        AssertEqual(0.01f, laser.DamagePerSecond, WeaponTemplate.DamagePerSecondWithTechs(laser, Player.data),
                    "a beam takes no kinetic bonus");
    }

    [TestMethod]
    public void AMirvWeaponTakesTheBonusesOfItsWarheads()
    {
        IWeaponTemplate missile = Weapon("ClusterMissiles");
        Player.data.WeaponTags[WeaponTag.Kinetic].Damage = 0.25f;

        AssertEqual(0.01f, missile.DamagePerSecond * 1.25f, WeaponTemplate.DamagePerSecondWithTechs(missile, Player.data),
                    "the cluster missile's kinetic warheads take the kinetic bonus, though the missile is not kinetic");
    }

    [TestMethod]
    public void OrdnanceEffectivenessRaisesEveryWeaponThatUsesOrdnance()
    {
        IWeaponTemplate railgun = Weapon("Railgun");
        IWeaponTemplate laser = Weapon("LaserBeam");
        Player.data.OrdnanceEffectivenessBonus = 0.2f;

        AssertEqual(0.01f, railgun.DamagePerSecond * 1.2f, WeaponTemplate.DamagePerSecondWithTechs(railgun, Player.data),
                    "a railgun uses ordnance and does not explode, and still takes the ordnance bonus, as in combat");
        AssertEqual(0.01f, laser.DamagePerSecond, WeaponTemplate.DamagePerSecondWithTechs(laser, Player.data),
                    "a beam that uses no ordnance takes no ordnance bonus");
    }
}
