using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Ships
{
    [TestClass]
    public class ModuleResistTests : StarDriveTest
    {
        public ModuleResistTests()
        {
            LoadStarterShips("TEST_ShipResist", "Vulcan Scout");
            CreateUniverseAndPlayerEmpire();
        }

        static ShipModule FindResistModule(Ship ship)
        {
            foreach (ShipModule m in ship.Modules)
                if (m.UID == "TEST_ModuleResist")
                    return m;
            return null;
        }

        [TestMethod]
        public void OutOfRangeResistancesReadBackAsFullResistance()
        {
            Ship ship = SpawnShip("TEST_ShipResist", Player, Vector2.Zero);
            ShipModule armor = FindResistModule(ship);
            Assert.IsNotNull(armor, "setup: TEST_ShipResist must carry TEST_ModuleResist");

            AssertEqual(0.001f, 1f, armor.KineticResist, "KineticResist 2 must clamp to 1");
            AssertEqual(0.001f, 1f, armor.BeamResist, "BeamResist 3 must clamp to 1");
            AssertEqual(0.001f, 1f, armor.ExplosiveResist, "ExplosiveResist 2 must clamp to 1");
        }

        [TestMethod]
        public void AFullyResistantPlateContributesNothingToTheDeathBlast()
        {
            Ship ship = SpawnShip("TEST_ShipResist", Player, Vector2.Zero);
            ShipModule armor = FindResistModule(ship);
            Assert.IsNotNull(armor, "setup: TEST_ShipResist must carry TEST_ModuleResist");
            Assert.IsTrue(armor.Health > 0, "setup: the plate must be alive to be counted");

            AssertEqual(0.001f, 0f, armor.GetExplosionDamageOnShipExplode(),
                "an out of range explosive resistance must neither add to nor cancel the blast");
        }

        [TestMethod]
        public void FullResistanceAbsorbsTheShotInsteadOfPassingItOn()
        {
            Ship ship = SpawnShip("TEST_ShipResist", Player, Vector2.Zero);
            Ship attacker = SpawnShip("Vulcan Scout", Enemy, new Vector2(1000, 0));
            ShipModule armor = FindResistModule(ship);
            Assert.IsNotNull(armor, "setup: TEST_ShipResist must carry TEST_ModuleResist");

            Projectile p = Projectile.Create(attacker.Weapons[0], attacker, Vector2.Zero, Vectors.Up, null, false);
            Assert.IsTrue(p.Weapon.Tag_Kinetic, "setup: the attacker's weapon must be kinetic");

            float healthBefore = armor.Health;
            armor.Damage(p, 100f, out float remainder);

            AssertEqual(0.001f, healthBefore, armor.Health, "a fully resistant module takes no damage");
            AssertEqual(0.001f, 0f, remainder, "and nothing carries on past it");
        }

        [TestMethod]
        public void AClampedResistanceZeroesTheDamageModifierAndNeverInvertsIt()
        {
            Ship ship = SpawnShip("TEST_ShipResist", Player, Vector2.Zero);
            Ship attacker = SpawnShip("Vulcan Scout", Enemy, new Vector2(1000, 0));
            ShipModule armor = FindResistModule(ship);
            Assert.IsNotNull(armor, "setup: TEST_ShipResist must carry TEST_ModuleResist");

            Weapon kinetic = attacker.Weapons[0];
            Assert.IsTrue(kinetic.Tag_Kinetic, "setup: the attacker's weapon must be kinetic");

            AssertEqual(0.001f, 0f, kinetic.GetArmorDamageMod(armor),
                "KineticResist 2 must give a modifier of 0, never a negative one that would heal");
        }
    }
}
