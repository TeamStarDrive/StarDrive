using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Ships
{
    [TestClass]
    public class TestShipMove : StarDriveTest
    {
        public TestShipMove()
        {
            CreateUniverseAndPlayerEmpire();
        }

        void WaitForEngineChangeTo(Ship.MoveState state, Ship ship, Action update)
        {
            RunSimWhile((simTimeout:15, fatal:true), () => ship.engineState != state, update);
        }

        void SpawnShips(out Ship ship, out Ship enemy)
        {
            ship  = SpawnShip("Fang Strafer", Player, Vector2.Zero);
            enemy = SpawnShip("Fang Strafer", Enemy, new Vector2(30000, 0));
            ship.SensorRange = 40000;
            enemy.SensorRange = 40000;
        }

        [TestMethod]
        public void MoveShipIgnoringHostiles()
        {
            SpawnShips(out Ship ship, out Ship enemy);
            enemy.AI.OrderHoldPosition(new Vector2(30000, 0), new Vector2(0,1));

            // order ship to move, ignoring enemies
            var movePosition = new Vector2(60000, 0);
            ship.AI.OrderMoveTo(movePosition, new Vector2(1,0), AIState.AwaitingOrders);

            // wait for ship to enter warp
            WaitForEngineChangeTo(Ship.MoveState.Warp, ship, () =>
            {
            });

            bool sawEnemyShip = false;

            // wait for ship to exit warp
            WaitForEngineChangeTo(Ship.MoveState.Sublight, ship, () =>
            {
                sawEnemyShip |= ship.AI.BadGuysNear;
            });

            // now wait a bit more to allow our ship to finish final approach
            RunObjectsSim(5.0f);

            Assert.IsTrue(sawEnemyShip, "Did not see an enemy while at warp");
            Assert.IsTrue(ship.AI.BadGuysNear, "Bad guys near was not set");
            Assert.IsTrue(ship.Position.InRadius(movePosition, 6000), "final move failed");
        }

        [TestMethod]
        public void MoveShipWithCombatMoveEngagingHostiles()
        {
            SpawnShips(out Ship ship, out Ship enemy);
            enemy.AI.OrderHoldPosition(enemy.Position, new Vector2(0,1));

            // order ship to move, CombatMove
            var movePosition = new Vector2(60000, 0);
            ship.AI.OrderMoveTo(movePosition, new Vector2(1,0), AIState.AwaitingOrders, MoveOrder.Aggressive);

            // wait for ship to enter warp
            WaitForEngineChangeTo(Ship.MoveState.Warp, ship, () =>
            {
                ship.Update(TestSimStep);
            });

            bool sawEnemyShip = false;

            // wait for ship to exit warp
            WaitForEngineChangeTo(Ship.MoveState.Sublight, ship, () =>
            {
                sawEnemyShip |= ship.AI.BadGuysNear;
            });

            // now wait a bit more to allow our ship to approach the enemy
            RunObjectsSim(10.0f);

            Assert.IsTrue(sawEnemyShip, "Did not see an enemy while at warp");
            Assert.IsTrue(ship.AI.BadGuysNear, "Bad guys near was not set");
            Assert.IsTrue(ship.Position.InRadius(enemy.Position, 7500),
                          $"CombatMove failed distance={ship.Position.Distance(enemy.Position)} Ship={ship} Enemy={enemy}");
        }

        [TestMethod]
        public void ShipYRotation()
        {
            Ship ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            AssertEqual(0, ship.YRotation, "Ship's Y rotation should be 0 when spawned");
            Vector2 newPos = new Vector2(2000, 2000);
            RunObjectsSim(TestSimStep);

            ship.AI.OrderMoveTo(newPos, Vector2.Zero);
            RunObjectsSim(TestSimStep * 10);
            AssertGreaterThan(Math.Abs(ship.YRotation), 0);

            float maxYBank = ship.GetMaxBank();
            float yBankReached = 0;
            RunSimWhile((simTimeout: 25, fatal: true), () => ship.Position.OutsideRadius(newPos, 100), () =>
            {
                yBankReached = Math.Max(Math.Abs(ship.YRotation), yBankReached);
            });

            // Allow 10% tolerance in max bank (saves performance in game since not using lower/higher bounds)
            AssertLessThan(yBankReached, maxYBank * 1.1f);
            AssertGreaterThan(yBankReached, maxYBank * 0.95f);
            AssertEqual(0, ship.YRotation, "Ship should reach 0 Y rotation at this point");
        }
    }
}
