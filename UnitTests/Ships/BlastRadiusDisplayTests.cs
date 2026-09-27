using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships;

[TestClass]
public class BlastRadiusDisplayTests : StarDriveTest
{
    public BlastRadiusDisplayTests()
    {
        LoadStarterShips("Rocket Scout");
        CreateUniverseAndPlayerEmpire();
    }

    static Weapon ExplosiveOrdnanceWeapon(Ship ship)
    {
        foreach (Weapon w in ship.Weapons)
            if (w.Explodes && w.OrdinanceRequiredToFire > 0f)
                return w;
        return null;
    }

    [TestMethod]
    public void TheDesignScreenShowsTheBlastRadiusCombatUses()
    {
        Ship launcher = SpawnShip("Rocket Scout", Player, Vector2.Zero);
        Weapon rocket = ExplosiveOrdnanceWeapon(launcher);
        Assert.IsNotNull(rocket, "setup: Rocket Scout must carry an explosive weapon that uses ordnance");
        Assert.IsTrue(rocket.Tag_Missile && rocket.Tag_Guided, "setup: the rocket must carry both tags given a bonus");

        WeaponTagModifier missileTag = Player.data.WeaponTags[WeaponTag.Missile];
        WeaponTagModifier guidedTag = Player.data.WeaponTags[WeaponTag.Guided];
        float missileRadius = missileTag.ExplosionRadius;
        float guidedRadius = guidedTag.ExplosionRadius;
        try
        {
            missileTag.ExplosionRadius = 0.25f;
            guidedTag.ExplosionRadius = 0.1f;
            Player.data.OrdnanceEffectivenessBonus = 0.5f;

            Projectile missile = Projectile.Create(rocket, launcher, launcher.Position, Vectors.Up, null, playSound: false);
            missile.Die(null, cleanupOnly: true);

            float expected = rocket.ExplosionRadius * (1f + 0.25f + 0.1f) * 1.5f;
            AssertEqual(0.001f, expected, missile.DamageRadius, "combat adds each tag's bonus against the base radius, then the ordnance bonus");
            AssertEqual(0.001f, missile.DamageRadius, ModuleSelection.BlastRadius(rocket, Player.data),
                        "the design screen must show the blast radius the missile explodes with");
        }
        finally
        {
            missileTag.ExplosionRadius = missileRadius;
            guidedTag.ExplosionRadius = guidedRadius;
        }
    }
}
