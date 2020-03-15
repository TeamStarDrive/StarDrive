using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;

namespace UnitTests.AITests.Ships
{
    [TestClass]
    public class TestShipMove : StarDriveTest
    {
        public TestShipMove()
        {
            LoadStarterShipVulcan();
            CreateGameInstance();
            CreateUniverseAndPlayerEmpire(out _);
        }

        Ship CreateTestShip()
        {
            Ship ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            ship.FTLSpoolTime = 3f;
            return ship;
        }

        Ship CreateEnemyTestShip(Vector2 location)
        {
            Ship ship = SpawnShip("Vulcan Scout", Enemy, location);
            ship.FTLSpoolTime = 3f;
            return ship;
        }


        [TestMethod]
        public void MoveShip()
        {
            Ship ship              = CreateTestShip();
            var enemySpawnLocation = new Vector2(30000, 0);
            Ship enemy             = CreateEnemyTestShip(enemySpawnLocation);
            var movePosition       = new Vector2(60000, 0);

            Player.GetEmpireAI().DeclareWarOn(Enemy, WarType.BorderConflict);
            ship.AI.OrderMoveDirectlyTo(movePosition, new Vector2(1,0),true
                                        , Ship_Game.AI.AIState.AwaitingOrders, 0, false);

            // wait for ship to enter warp
            while (ship.engineState != Ship.MoveState.Warp)
            {
                ship.Update(0.01666666f);
            }
            bool sawEnemyShip       = false;

            // wait for ship to exit warp
            while (ship.engineState == Ship.MoveState.Warp)
            {
                UniverseScreen.SpaceManager.Update(0.0166666f);
                ship.Update(0.01666666f);
                enemy.Update(0.01666666f);
                sawEnemyShip |= ship.AI.BadGuysNear;
            }
            Assert.IsTrue(sawEnemyShip, "Did not see an enemy while at warp");
            Assert.IsTrue(ship.AI.BadGuysNear, "Bad guys near was not set");
            Assert.IsTrue(ship.Center.InRadius(movePosition, 6000), "final move failed");

            // fly back with a combat move. 
            movePosition = Vector2.Zero;
            ship.AI.OrderMoveDirectlyTo(movePosition, new Vector2(1, 0), true
                            , Ship_Game.AI.AIState.AwaitingOrders, 0, true);

            sawEnemyShip       = false;

            // wait for ship to enter warp
            while (ship.engineState != Ship.MoveState.Warp)
            {
                ship.Update(0.01666666f);
            }

            // wait for ship to exit warp
            while (ship.engineState == Ship.MoveState.Warp)
            {
                UniverseScreen.SpaceManager.Update(0.0166666f);
                ship.Update(0.01666666f);
                enemy.Update(0.01666666f);
                sawEnemyShip |= ship.AI.BadGuysNear;
            }

            Assert.IsTrue(ship.Center.InRadius(enemySpawnLocation, 7500), "combat move failed");
        }
    }
}
