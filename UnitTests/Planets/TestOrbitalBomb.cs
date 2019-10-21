using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Planets
{
    [TestClass]
    public class TestOrbitalBomb : StarDriveTest
    {
        public TestOrbitalBomb()
        {
            LoadPlanetContent();
            CreateGameInstance();
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

        PlanetGridSquare FindHabitableTargetTile   => P.TilesList.Find(tile => tile.Habitable && !tile.Biosphere);
        PlanetGridSquare FindUnhabitableTargetTile => P.TilesList.Find(tile => !tile.Habitable);
        PlanetGridSquare FindBiospheres            => P.TilesList.Find(tile => tile.Biosphere);

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
            float actualPop   = (float)Math.Round(P.MaxPopulationBillion, 1);
            Assert.That.Equal(expectedPop, actualPop);
        }

        [TestMethod]
        public void TestPopKilledHabitable()
        {
            CreateOrbitalDrop(out OrbitalDrop orbitalDrop, FindHabitableTargetTile);
            float expectedPop = P.Population - B.PopKilled*1000;
            orbitalDrop.DamageColonySurface(B);
            Assert.That.Equal(expectedPop, P.Population);
        }

        [TestMethod]
        public void TestPopKilledUnhabitable()
        {
            CreateOrbitalDrop(out OrbitalDrop orbitalDrop, FindUnhabitableTargetTile);
            float expectedPop = P.Population - B.PopKilled*100; // 0.1 of pop killed potential. the usual is * 1000
            orbitalDrop.DamageColonySurface(B);
            Assert.That.Equal(expectedPop, P.Population);
        }

        [TestMethod]
        public void TestTileDestruction()
        {
            float expectedMaxPop = P.MaxPopulation - P.BasePopPerTile * TestEmpire.RacialEnvModifer(P.Category);
            P.DestroyTile(FindHabitableTargetTile);
            Assert.That.Equal(expectedMaxPop, P.MaxPopulation);
        }

        [TestMethod]
        public void TestBiospheresDestruction()
        {
            PlanetGridSquare bioTile = FindUnhabitableTargetTile;
            ResourceManager.GetBuilding(Building.BiospheresId, out Building bioSpheres);
            bioTile.PlaceBuilding(bioSpheres, P);
            P.UpdateMaxPopulation();
            float expectedMaxPop = P.MaxPopulation - P.BasePopPerTile;
            P.DestroyTile(bioTile);
            Assert.That.Equal(expectedMaxPop, P.MaxPopulation);
        }
    }
}
