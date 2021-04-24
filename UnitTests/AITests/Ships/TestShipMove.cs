using System;
using System.Diagnostics;
using System.IO;
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
            CreateGameInstance();
            LoadStarterShipVulcan();
        }

        void CreateTestEnv()
        {
            CreateUniverseAndPlayerEmpire(out _);
        }
        Ship CreateTestShip()
        {
            Ship ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            ship.Stats.FTLSpoolTime = 3f;
            return ship;
        }

        Ship CreateEnemyTestShip(Vector2 location)
        {
            Ship ship = SpawnShip("Vulcan Scout", Enemy, location);
            ship.Stats.FTLSpoolTime = 3f;
            return ship;
        }

        void WaitForEngineChangeTo(Ship.MoveState state, Ship ship, Action update)
        {
            var sw = Stopwatch.StartNew();
            while (ship.engineState != state)
            {
                update();
                if (sw.Elapsed.TotalSeconds > 5.0)
                    throw new TimeoutException($"Timed out while waiting for engine state change");
            }
        }

        [TestMethod]
        public void MoveShip()
        {
            CreateTestEnv();
            
            var enemySpawnLocation = new Vector2(30000, 0);
            var movePosition       = new Vector2(60000, 0);
            Ship ship  = CreateTestShip();
            Ship enemy = CreateEnemyTestShip(enemySpawnLocation);
            enemy.AI.OrderHoldPosition(enemySpawnLocation, new Vector2(0,1));

            Player.GetEmpireAI().DeclareWarOn(Enemy, WarType.BorderConflict);
            ship.AI.OrderMoveDirectlyTo(movePosition, new Vector2(1,0),true
                                        , Ship_Game.AI.AIState.AwaitingOrders, 0, false);

            // wait for ship to enter warp
            WaitForEngineChangeTo(Ship.MoveState.Warp, ship, () =>
            {
                ship.AI.DoManualSensorScan(new FixedSimTime(10f));
                ship.Update(TestSimStep);
            });

            bool sawEnemyShip = false;

            // wait for ship to exit warp
            WaitForEngineChangeTo(Ship.MoveState.Sublight, ship, () =>
            {
                Universe.Objects.Update(TestSimStep); // update ships
                ship.AI.DoManualSensorScan(new FixedSimTime(10f));
                enemy.AI.DoManualSensorScan(new FixedSimTime(10f));
                sawEnemyShip |= ship.AI.BadGuysNear;
            });
            Assert.IsTrue(sawEnemyShip, "Did not see an enemy while at warp");
            Assert.IsTrue(ship.AI.BadGuysNear, "Bad guys near was not set");
            Assert.IsTrue(ship.Center.InRadius(movePosition, 6000), "final move failed");

            // fly back with a combat move. 
            movePosition = Vector2.Zero;
            ship.AI.OrderMoveDirectlyTo(movePosition, new Vector2(1, 0), true,
                                        Ship_Game.AI.AIState.AwaitingOrders, 0, true);

            sawEnemyShip = false;

            // wait for ship to enter warp
            WaitForEngineChangeTo(Ship.MoveState.Warp, ship, () =>
            {
                ship.Update(TestSimStep);
            });

            // wait for ship to exit warp
            WaitForEngineChangeTo(Ship.MoveState.Sublight, ship, () =>
            {
                Universe.Objects.Update(TestSimStep); // update ships           
                ship.AI.StartSensorScan(new FixedSimTime(10f));
                enemy.AI.StartSensorScan(new FixedSimTime(10f));
                sawEnemyShip |= ship.AI.BadGuysNear;
            });

            Assert.IsTrue(ship.Center.InRadius(enemySpawnLocation, 7500), "combat move failed");
        }
    }
}
