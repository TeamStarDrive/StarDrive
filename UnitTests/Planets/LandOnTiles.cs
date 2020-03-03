using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;

namespace UnitTests.Planets
{
    [TestClass]
    public class TestLandOnTile : StarDriveTest
    {
        public TestLandOnTile()
        {
            LoadPlanetContent();
            CreateGameInstance();
            ResourceManager.LoadProjectileMeshes();
            CreateTestEnv();
        }

        private Planet P;
        private Empire TestEmpire;
        private Troop Enemy1;
        private Troop Enemy2;
        private Troop Friendly;


        void CreateTestEnv()
        {
            CreateUniverseAndPlayerEmpire(out TestEmpire);
            AddDummyPlanetToEmpire(TestEmpire);
            AddHomeWorldToEmpire(TestEmpire, out P);
            Enemy1   = CreateEnemyTroop;
            Enemy2   = CreateEnemyTroop;
            Friendly = CreateFriendlyTroop;
        }

        Troop CreateEnemyTroop    => ResourceManager.CreateTroop("Wyvern", EmpireManager.Remnants);
        Troop CreateFriendlyTroop => ResourceManager.CreateTroop("Wyvern", TestEmpire);

        bool GetTroopTile(Troop troop, out PlanetGridSquare troopTile)
        {
            troopTile = null;
            foreach (PlanetGridSquare tile in P.TilesList)
            {
                if (tile.LockOnOurTroop(troop.Loyalty, out _))
                {
                    troopTile = tile;
                    break;
                }
            }

            return troopTile != null;
        }

        bool GetCapitalTile(out PlanetGridSquare capitalTile)
        {
            capitalTile = null;
            foreach (PlanetGridSquare tile in P.TilesList)
            {
                if (tile.BuildingOnTile && tile.building.IsCapital)
                {
                    capitalTile = tile;
                    break;
                }
            }

            return capitalTile != null;
        }

        [TestMethod]
        public void CheckTroops()
        {
            Assert.IsNotNull(Enemy1);
            Assert.IsNotNull(Enemy2);
            Assert.IsNotNull(Friendly);
        }

        [TestMethod]
        public void LandFirstEnemy()
        {
            Assert.IsTrue(Enemy1.TryLandTroop(P));
            Assert.IsTrue(P.TroopsHere.Contains(Enemy1));
            Assert.IsTrue(GetCapitalTile(out PlanetGridSquare capitalTile));
            Assert.IsTrue(GetTroopTile(Enemy1, out PlanetGridSquare troopTile));

            // Enemy should land out of capital's reach so it wont get hit on landing
            Assert.IsFalse(capitalTile.InRangeOf(troopTile, 1));
        }

        [TestMethod]
        public void LandSecondEnemy()
        {
            // Land enemy1 if this test is run individually
            if (!P.TroopsHere.Contains(Enemy1))
                LandFirstEnemy();

            Assert.IsTrue(Enemy2.TryLandTroop(P));
            Assert.IsTrue(P.TroopsHere.Contains(Enemy1));
            Assert.IsTrue(P.TroopsHere.Contains(Enemy2));
            Assert.IsTrue(GetCapitalTile(out PlanetGridSquare capitalTile));
            Assert.IsTrue(GetTroopTile(Enemy1, out PlanetGridSquare enemy1Tile));
            Assert.IsTrue(GetTroopTile(Enemy2, out PlanetGridSquare enemy2Tile));

            // Enemy should land out of capital's reach so it wont get hit on landing
            // and close to enemy1, as reinforcements
            Assert.IsFalse(capitalTile.InRangeOf(enemy1Tile, 1));
            Assert.IsFalse(capitalTile.InRangeOf(enemy2Tile, 1));
            Assert.IsTrue(enemy1Tile.InRangeOf(enemy2Tile, 1));
        }

        [TestMethod]
        public void ThenLandFriendly()
        {
            // Land enemy1 if this test is run individually
            if (!P.TroopsHere.Contains(Enemy1))
                LandFirstEnemy();

            // Land enemy2 if this test is run individually
            if (!P.TroopsHere.Contains(Enemy2))
                LandSecondEnemy();

            Assert.IsTrue(P.TroopsHere.Contains(Enemy1));
            Assert.IsTrue(P.TroopsHere.Contains(Enemy2));
            Assert.IsTrue(GetCapitalTile(out PlanetGridSquare capitalTile));
            Assert.IsTrue(GetTroopTile(Enemy1, out PlanetGridSquare enemy1Tile));
            Assert.IsTrue(GetTroopTile(Enemy2, out PlanetGridSquare enemy2Tile));

            Assert.IsTrue(Friendly.TryLandTroop(P));
            Assert.IsTrue(GetTroopTile(Friendly, out PlanetGridSquare friendlyTile));

            // Friendly troop should be in range 1 of the capital and not in range
            // of enemy troops
            Enemy1.UpdateAttackActions(Enemy1.MaxStoredActions);
            Enemy2.UpdateAttackActions(Enemy2.MaxStoredActions);
            Assert.IsTrue(Enemy1.CanAttack);
            Assert.IsTrue(Enemy2.CanAttack);
            Assert.IsTrue(friendlyTile.InRangeOf(capitalTile, 1));
            Assert.IsFalse(friendlyTile.InRangeOf(enemy1Tile, 1));
            Assert.IsFalse(friendlyTile.InRangeOf(enemy2Tile, 1));
        }
    }
}