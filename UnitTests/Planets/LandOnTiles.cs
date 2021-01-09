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
            CreateGameInstance();
            LoadPlanetContent();
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
            Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
            AddDummyPlanetToEmpire(TestEmpire);
            AddHomeWorldToEmpire(TestEmpire, out P);
            Enemy1   = CreateEnemyTroop;
            Enemy2   = CreateEnemyTroop;
            Friendly = CreateFriendlyTroop;
        }

        Troop CreateEnemyTroop    => ResourceManager.CreateTroop("Wyvern", Enemy);
        Troop CreateFriendlyTroop => ResourceManager.CreateTroop("Wyvern", TestEmpire);

        bool GetTroopTile(Troop troop, out PlanetGridSquare troopTile)
        {
            troopTile = null;
            foreach (PlanetGridSquare tile in P.TilesList)
            {
                if (tile.LockOnOurTroop(troop.Loyalty, out Troop troopToCheck) 
                    && troop == troopToCheck)
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
        public void LandEnemiesAndFriends()
        {
            Assert.IsTrue(Enemy1.TryLandTroop(P));
            Assert.IsTrue(P.TroopsHere.Contains(Enemy1));
            Assert.IsTrue(GetCapitalTile(out PlanetGridSquare capitalTile));
            Assert.IsTrue(GetTroopTile(Enemy1, out PlanetGridSquare enemy1Tile));

            // Enemy should land out of capital's reach so it wont get hit on landing
            Assert.IsFalse(capitalTile.InRangeOf(enemy1Tile, 1), "Enemy1 Too Close to Capital");

            // land a second enemy
            Assert.IsTrue(Enemy2.TryLandTroop(P));
            Assert.IsTrue(P.TroopsHere.Contains(Enemy2));
            Assert.IsTrue(GetTroopTile(Enemy2, out PlanetGridSquare enemy2Tile));
            Assert.IsTrue(enemy1Tile != enemy2Tile, "Enemies are on the same tile!");

            // Enemy should land out of capital's reach so it wont get hit on landing
            // and close to enemy1, as reinforcements
            Assert.IsFalse(capitalTile.InRangeOf(enemy2Tile, 1), "Enemy2 Too Close to Capital");
            Assert.IsTrue(enemy1Tile.InRangeOf(enemy2Tile, 1), "Enemy2 Too Far From Enemy1");

            Enemy1.UpdateAttackActions(Enemy1.MaxStoredActions);
            Enemy2.UpdateAttackActions(Enemy2.MaxStoredActions);
            Assert.IsTrue(Enemy1.CanAttack);
            Assert.IsTrue(Enemy2.CanAttack);

            // Friendly troop should be in range 1 of the capital and not in range
            // of enemy troops

            Assert.IsTrue(Friendly.TryLandTroop(P));
            Assert.IsTrue(GetTroopTile(Friendly, out PlanetGridSquare friendlyTile));

            string positions = $"Capital : {capitalTile.x},{capitalTile.y}\n" +
                               $"Friendly: {friendlyTile.x},{ friendlyTile.y}\n" +
                               $"Enemy1  : {enemy1Tile.x},{ enemy1Tile.y}\n" +
                               $"Enemy2  : {enemy2Tile.x},{ enemy2Tile.y}\n"; 

            Assert.IsTrue(friendlyTile.InRangeOf(capitalTile, 1), $"Friendly Too Far From Capital\n{positions}");
            Assert.IsFalse(friendlyTile.InRangeOf(enemy1Tile, 1), $"Friendly Too Close to Enemy1\n{positions}");
            Assert.IsFalse(friendlyTile.InRangeOf(enemy2Tile, 1), $"Friendly Too Close to Enemy2\n{positions}");
        }
    }
}