using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Planets
{
    // a planet weapon fires every FireDelay / planet level, the same rate its offense rating assumes
    [TestClass]
    public class PlanetWeaponFireDelayTests : StarDriveTest
    {
        readonly Planet P;
        readonly Building Gun;
        readonly float FireDelay;

        public PlanetWeaponFireDelayTests()
        {
            CreateUniverseAndPlayerEmpire();
            P = AddHomeWorldToEmpire(new Vector2(1000), Player);

            Building template = ResourceManager.BuildingsDict.Values.First(b => b.IsWeapon
                && ResourceManager.GetWeaponTemplate(b.Weapon, out IWeaponTemplate w) && w.FireDelay > 2);
            ResourceManager.GetWeaponTemplate(template.Weapon, out IWeaponTemplate weapon);
            FireDelay = weapon.FireDelay;

            Gun = ResourceManager.CreateBuilding(P, template);
            P.TilesList.First(t => t.Habitable && t.NoBuildingOnTile).PlaceBuilding(Gun, P);
        }

        void SetLevel(int level)
        {
            float[] billions = { 0, 0.3f, 1f, 3f, 7f, 12f };
            P.Population = billions[level] * 1000f;
            P.UpdateDevelopmentLevel();
            Assert.AreEqual(level, P.Level, "The test population did not give the planet the level it needs");
        }

        [TestMethod]
        public void AtLevelOneTheGunFiresAtItsOwnDelay()
        {
            SetLevel(1);
            AssertEqual(0.001f, FireDelay, Gun.ActualFireDelay(P.Level),
                $"A level 1 colony fires {Gun.Name} on a flat 1 s instead of its own {FireDelay} s fire delay");
        }

        [TestMethod]
        public void OffenseFollowsTheRealFireRateAtEveryLevel()
        {
            SetLevel(1);
            float offensePerShot = Gun.Offense * Gun.ActualFireDelay(P.Level);
            for (int level = 2; level <= 5; ++level)
            {
                SetLevel(level);
                AssertEqual(0.001f * offensePerShot, offensePerShot, Gun.Offense * Gun.ActualFireDelay(P.Level),
                    $"At level {level} the offense of {Gun.Name} no longer matches how often it really fires");
            }
        }
    }
}
