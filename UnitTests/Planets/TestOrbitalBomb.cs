using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;

namespace UnitTests.Planets
{
    [TestClass]
    public class TestOrbitalBomb : StarDriveTest
    {
        public TestOrbitalBomb()
        {
            CreateGameInstance();
            LoadPlanetContent();
            ResourceManager.LoadProjectileMeshes();
            CreateTestEnv();
        }

        private Planet P;
        private Empire TestEmpire;
        private Bomb B;

        void CreateTestEnv()
        {
            CreateUniverseAndPlayerEmpire(out TestEmpire);
            AddDummyPlanetToEmpire(TestEmpire);
            AddHomeWorldToEmpire(TestEmpire, out P);
            B = new Bomb(Vector3.Zero, TestEmpire, "NuclearBomb");
        }

        PlanetGridSquare FindHabitableTargetTile(SolarSystemBody p)
            => p.TilesList.Find(tile => tile.Habitable && !tile.Biosphere);
        PlanetGridSquare FindUnhabitableTargetTile(SolarSystemBody p) 
            => p.TilesList.Find(tile => !tile.Habitable);
        PlanetGridSquare FindBiospheres(SolarSystemBody p)
            => p.TilesList.Find(tile => tile.Biosphere);

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
            float expectedPop = 14 * TestEmpire.data.Traits.HomeworldSizeMultiplier;
            float actualPop   = P.MaxPopulationBillion;
            Assert.That.Equal(0.1f, expectedPop, actualPop);
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
            float expectedPop = P.Population - B.PopKilled * 100; // 0.1 of pop killed potential. the usual is * 1000
            orbitalDrop.DamageColonySurface(B);
            Assert.IsTrue(P.Population < expectedPop + 10 && P.Population > expectedPop - 10);
        }

        [TestMethod]
        public void TestTileDestruction()
        {
            float expectedMaxPop = P.MaxPopulation - P.BasePopPerTile * Empire.RacialEnvModifer(P.Category, TestEmpire);
            P.DestroyTile(FindHabitableTargetTile(P));
            Assert.That.Equal(expectedMaxPop, P.MaxPopulation);
        }

        [TestMethod]
        public void TestBiospheresDestruction()
        {
            PlanetGridSquare bioTile = FindUnhabitableTargetTile(P);
            ResourceManager.GetBuilding(Building.BiospheresId, out Building bioSpheres);
            bioTile.PlaceBuilding(bioSpheres, P);
            P.UpdateMaxPopulation();
            float expectedMaxPop = P.MaxPopulation - P.PopPerBiosphere(TestEmpire);
            P.DestroyTile(bioTile);
            Assert.That.Equal(expectedMaxPop, P.MaxPopulation);
            Assert.IsFalse(bioTile.Biosphere);
            Assert.IsFalse(bioTile.Habitable);
        }
    }
}
