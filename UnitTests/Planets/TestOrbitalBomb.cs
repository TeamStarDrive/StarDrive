using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Vector3 = SDGraphics.Vector3;

namespace UnitTests.Planets
{
    [TestClass]
    public class TestOrbitalBomb : StarDriveTest
    {
        readonly Planet P; // Player.Capital
        readonly Bomb B;

        public TestOrbitalBomb()
        {
            CreateUniverseAndPlayerEmpire();
            AddDummyPlanetToEmpire(new Vector2(2000), Player);
            P = AddHomeWorldToEmpire(new Vector2(2000), Player);
            B = new Bomb(Vector3.Zero, Player, "NuclearBomb", shipLevel: 15, shipHealthPercent: 1);
        }

        PlanetGridSquare FindHabitableTargetTile(SolarSystemBody p)
            => p.TilesList.Find(tile => tile.Habitable && !tile.Biosphere);

        PlanetGridSquare FindUnhabitableTargetTile(SolarSystemBody p)
            => p.TilesList.Find(tile => !tile.Habitable);

        void CreateOrbitalDrop(out OrbitalDrop ob, PlanetGridSquare tile)
        {
            ob = new OrbitalDrop
            {
                TargetTile = tile,
                Surface    = P
            };
        }

        [TestMethod]
        public void TestPopulationCreated() // FB: Should be moved to other planet test class once i create them.
        {
            float expectedPop = 14 * Player.data.Traits.HomeworldSizeMultiplier;
            float actualPop   = P.MaxPopulationBillion;
            AssertEqual(0.1f, expectedPop, actualPop);
        }

        [TestMethod]
        public void TestPopKilledHabitable()
        {
            for (int i = 0; i < 100; ++i)
            {
                var tile = FindHabitableTargetTile(P);
                //do until bombs have made the planet tile uninhabitable
                //but make sure it dropped at least 1 bomb.
                if (tile == null && i > 0)
                    break;

                CreateOrbitalDrop(out OrbitalDrop orbitalDrop, tile);
                float expectedPop = P.Population - B.PopKilled * 1000;
                orbitalDrop.DamageColonySurface(B);
                Assert.IsTrue(P.Population < expectedPop + 10 && P.Population > expectedPop - 10, $"At index {i}");
            }
        }

        [TestMethod]
        public void TestPopKilledUnhabitable()
        {
            CreateOrbitalDrop(out OrbitalDrop orbitalDrop, FindUnhabitableTargetTile(P));
            float expectedPop = P.Population - B.PopKilled * 250; // 0.25 of pop killed potential. the usual is * 1000
            orbitalDrop.DamageColonySurface(B);
            Assert.IsTrue(P.Population < expectedPop + 10 && P.Population > expectedPop - 10);
        }

        [TestMethod]
        public void TestTileDestruction()
        {
            float expectedMaxPop = P.MaxPopulation - P.BasePopPerTile * Empire.RacialEnvModifer(P.Category, Player);
            P.DestroyTile(FindHabitableTargetTile(P));
            AssertEqual(expectedMaxPop, P.MaxPopulation);
        }

        [TestMethod]
        public void TestBiospheresDestruction()
        {
            PlanetGridSquare bioTile = FindUnhabitableTargetTile(P);
            ResourceManager.GetBuilding(Building.BiospheresId, out Building bioSpheres);
            bioTile.PlaceBuilding(bioSpheres, P);
            P.UpdateMaxPopulation();
            float expectedMaxPop = P.MaxPopulation - P.PopPerBiosphere(Player);
            P.DestroyTile(bioTile);
            AssertEqual(expectedMaxPop, P.MaxPopulation);
            Assert.IsFalse(bioTile.Biosphere);
            Assert.IsFalse(bioTile.Habitable);
        }
    }
}
