﻿using System;
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
            CreateUniverseAndPlayerEmpire();
        }

        void WaitForEngineChangeTo(Ship.MoveState state, Ship ship, Action update)
        {
            LoopWhile(5, () => ship.engineState != state, update);
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
            Assert.IsTrue(ship.Center.InRadius(movePosition, 6000), "final move failed");
        }
        

        [TestMethod]
        public void MoveShipWithCombatMoveEngagingHostiles()
        {
            Ship ship  = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            Ship enemy  = SpawnShip("Vulcan Scout", Enemy, new Vector2(30000, 0));
            enemy.AI.OrderHoldPosition(enemy.Center, new Vector2(0,1));

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
            Assert.IsTrue(ship.Center.InRadius(enemy.Center, 7500), $"CombatMove failed: {ship} not at {enemy}");
        }
    }
}
