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

        Ship CreateEnemyTestShip()
        {
            Ship ship = SpawnShip("Vulcan Scout", Enemy, new Vector2(30000,0));
            ship.FTLSpoolTime = 3f;
            return ship;
        }


        [TestMethod]
        public void MoveShip()
        {
            Ship ship = CreateTestShip();
            Ship enemy = CreateEnemyTestShip();
            var movePosition = new Vector2(60000, 0);
            Player.GetEmpireAI().DeclareWarOn(Enemy, WarType.BorderConflict);
            ship.AI.OrderMoveDirectlyTo(
                movePosition, 
                new Vector2(1,0),
                true,
                Ship_Game.AI.AIState.AwaitingOrders);

            while (ship.engineState != Ship.MoveState.Warp)
            {
                ship.Update(0.01666666f);
            }
            bool sawEnemyShip = false;
            bool enemyWithinSensors = false;
            while (ship.engineState == Ship.MoveState.Warp)
            {
                UniverseScreen.SpaceManager.Update(0.0166666f);
                ship.Update(0.01666666f);
                enemy.Update(0.01666666f);
                sawEnemyShip |= ship.AI.BadGuysNear;
                enemyWithinSensors |= ship.Center.Distance(enemy.Center) <= ship.SensorRange;
            }
            Assert.IsTrue(sawEnemyShip, "Did not see an enemy while at warp");
            Assert.IsFalse(ship.AI.BadGuysNear, "Came out of warp seeing an enemy");
            Assert.IsTrue(ship.Center.InRadius(movePosition, 6000), "final move failed");
        }
    }
}
