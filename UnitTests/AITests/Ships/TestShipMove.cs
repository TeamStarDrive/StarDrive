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
            CreateUniverseAndPlayerEmpire();
        }

        void WaitForEngineChangeTo(Ship.MoveState state, Ship ship, Action update)
        {
            LoopWhile((timeout:5, fatal:true), () => ship.engineState != state, update);
        }

        [TestMethod]
        public void MoveShipIgnoringHostiles()
        {
            Ship ship  = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            Ship enemy  = SpawnShip("Vulcan Scout", Enemy, new Vector2(30000, 0));
            enemy.AI.OrderHoldPosition(new Vector2(30000, 0), new Vector2(0,1));

            // order ship to move, ignoring enemies
            var movePosition = new Vector2(60000, 0);
            ship.AI.OrderMoveDirectlyTo(movePosition, new Vector2(1,0), true, 
                                        Ship_Game.AI.AIState.AwaitingOrders, 0, offensiveMove:false);

            // wait for ship to enter warp
            WaitForEngineChangeTo(Ship.MoveState.Warp, ship, () =>
            {
                Universe.Objects.Update(TestSimStep); // update ships and do scans
                ship.Update(TestSimStep);
            });

            bool sawEnemyShip = false;

            // wait for ship to exit warp
            WaitForEngineChangeTo(Ship.MoveState.Sublight, ship, () =>
            {
                Universe.Objects.Update(TestSimStep); // update ships and do scans
                sawEnemyShip |= ship.AI.BadGuysNear;
            });
            Assert.IsTrue(sawEnemyShip, "Did not see an enemy while at warp");
            Assert.IsTrue(ship.AI.BadGuysNear, "Bad guys near was not set");
            Assert.IsTrue(ship.Position.InRadius(movePosition, 6000), "final move failed");
        }
        

        [TestMethod]
        public void MoveShipWithCombatMoveEngagingHostiles()
        {
            Ship ship  = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            Ship enemy  = SpawnShip("Vulcan Scout", Enemy, new Vector2(30000, 0));
            enemy.AI.OrderHoldPosition(enemy.Position, new Vector2(0,1));

            // order ship to move, CombatMove
            var movePosition = new Vector2(60000, 0);
            ship.AI.OrderMoveDirectlyTo(movePosition, new Vector2(1,0), true, 
                                        Ship_Game.AI.AIState.AwaitingOrders, 0, offensiveMove:true);

            // wait for ship to enter warp
            WaitForEngineChangeTo(Ship.MoveState.Warp, ship, () =>
            {
                Universe.Objects.Update(TestSimStep); // update ships and do scans
                ship.Update(TestSimStep);
            });

            bool sawEnemyShip = false;

            // wait for ship to exit warp
            WaitForEngineChangeTo(Ship.MoveState.Sublight, ship, () =>
            {
                Universe.Objects.Update(TestSimStep); // update ships and do scans
                sawEnemyShip |= ship.AI.BadGuysNear;
            });

            Assert.IsTrue(sawEnemyShip, "Did not see an enemy while at warp");
            Assert.IsTrue(ship.AI.BadGuysNear, "Bad guys near was not set");
            Assert.IsTrue(ship.Position.InRadius(enemy.Position, 7500), $"CombatMove failed: {ship} not at {enemy}");
        }

        [TestMethod]
        public void ShipYRotation()
        {
            Ship ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            ship.AI.OrderMoveTo(new Vector2(10000, 10000), Vector2.Zero, false, Ship_Game.AI.AIState.MoveTo);
            Universe.Objects.Update(TestSimStep);
            Assert.IsTrue(ship.yRotation.NotZero(), "Ship's Y rotation should chance as it rotates");
        }
    }
}
