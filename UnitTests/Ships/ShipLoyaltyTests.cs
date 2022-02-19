using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game;
using Ship_Game.Ships;
using Microsoft.Xna.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.ShipDesign;

namespace UnitTests.Ships
{
    [TestClass]
    public class ShipLoyaltyTests : StarDriveTest
    {
        public ShipLoyaltyTests()
        {
            CreateUniverseAndPlayerEmpire();
        }

        GameplayObject[] FindNearbyShips(Empire loyalty)
        {
            return loyalty.Universum.Spatial.FindNearby(GameObjectType.Ship, Vector2.Zero,
                                                        1000, 10, onlyLoyalty: loyalty);
        }

        void EnsureSpawnedLoyaltyAndSpatialCoherence(Ship playerShip)
        {
            // NOTE: This covers the LoyaltyChangeAtSpawn case
            Assert.AreEqual(1, UState.Objects.NumShips);
            Assert.IsFalse(Player.OwnedShips.Contains(playerShip), "Player.OwnedShips must NOT contain the ship");
            Assert.IsFalse(Enemy.OwnedShips.Contains(playerShip), "Enemy.OwnedShips must NOT contain the ship");
            UState.Objects.Update(TestSimStep);
            
            Assert.AreEqual(1, UState.Objects.NumShips);
            Assert.IsTrue(Player.OwnedShips.Contains(playerShip), "Player.OwnedShips MUST contain the ship, the ship was not added to Empire?");
            Assert.IsFalse(Enemy.OwnedShips.Contains(playerShip), "Enemy.OwnedShips must NOT contain the ship");

            Assert.AreEqual(Player, playerShip.Loyalty, "LoyaltyChangeAtSpawn is broken");

            var nearbyPlayerShips = FindNearbyShips(Player);
            Assert.AreEqual(1, nearbyPlayerShips.Length, "There should be 1 Player ship nearby");
            Assert.AreEqual(playerShip.Id, nearbyPlayerShips[0].Id);

            var nearbyEnemyShips = FindNearbyShips(Enemy);
            Assert.AreEqual(0, nearbyEnemyShips.Length, "There should be 0 Enemy ships nearby");
        }

        void EnsureLoyaltyTransferAndSpatialCoherence(Ship transferredShip)
        {
            Assert.AreEqual(1, UState.Objects.NumShips);
            Assert.IsTrue(Player.OwnedShips.Contains(transferredShip), "Player.OwnedShips MUST contain the ship before update");
            Assert.IsFalse(Enemy.OwnedShips.Contains(transferredShip), "Enemy.OwnedShips must NOT contain the ship before update");
            RunObjectsSim(TestSimStep);
            RunObjectsSim(TestSimStep);
            Assert.AreEqual(1, UState.Objects.NumShips);
            Assert.IsFalse(Player.OwnedShips.Contains(transferredShip), "Player.OwnedShips must NOT contain the ship AFTER transfer and update");
            Assert.IsTrue(Enemy.OwnedShips.Contains(transferredShip), "Enemy.OwnedShips MUST contain the ship  AFTER transfer and update");

            Assert.AreEqual(Enemy, transferredShip.Loyalty, "LoyaltyChange failed! Incorrect loyalty!");

            var nearbyPlayerShips = FindNearbyShips(Player);
            Assert.AreEqual(0, nearbyPlayerShips.Length, "There should be no Player ships nearby");

            var nearbyEnemyShips = FindNearbyShips(Enemy);
            Assert.AreEqual(1, nearbyEnemyShips.Length, "There should be 1 Enemy ship nearby");
            Assert.AreEqual(transferredShip.Id, nearbyEnemyShips[0].Id);
        }

        [TestMethod]
        public void LoyaltyChangeFromBoarding()
        {
            var ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            EnsureSpawnedLoyaltyAndSpatialCoherence(ship);
            ship.LoyaltyChangeFromBoarding(Enemy, addNotification:false);
            EnsureLoyaltyTransferAndSpatialCoherence(ship);
        }

        [TestMethod]
        public void LoyaltyChangeByGift()
        {
            var ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            EnsureSpawnedLoyaltyAndSpatialCoherence(ship);
            ship.LoyaltyChangeByGift(Enemy, addNotification:false);
            EnsureLoyaltyTransferAndSpatialCoherence(ship);
        }
    }
}
