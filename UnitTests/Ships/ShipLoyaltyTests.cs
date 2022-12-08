using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game;
using Ship_Game.Ships;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.ShipDesign;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.Spatial;

namespace UnitTests.Ships
{
    [TestClass]
    public class ShipLoyaltyTests : StarDriveTest
    {
        public ShipLoyaltyTests()
        {
            CreateUniverseAndPlayerEmpire();
        }

        SpatialObjectBase[] FindNearbyShips(Empire loyalty)
        {
            return loyalty.Universe.Spatial.FindNearby(GameObjectType.Ship, Vector2.Zero,
                                                        1000, 10, onlyLoyalty: loyalty);
        }

        void EnsureSpawnedLoyaltyAndSpatialCoherence(Ship playerShip)
        {
            // NOTE: This covers the LoyaltyChangeAtSpawn case
            AssertEqual(1, UState.Objects.NumShips);
            Assert.IsFalse(Player.OwnedShips.Contains(playerShip), "Player.OwnedShips must NOT contain the ship");
            Assert.IsFalse(Enemy.OwnedShips.Contains(playerShip), "Enemy.OwnedShips must NOT contain the ship");
            UState.Objects.Update(TestSimStep);
            
            AssertEqual(1, UState.Objects.NumShips);
            Assert.IsTrue(Player.OwnedShips.Contains(playerShip), "Player.OwnedShips MUST contain the ship, the ship was not added to Empire?");
            Assert.IsFalse(Enemy.OwnedShips.Contains(playerShip), "Enemy.OwnedShips must NOT contain the ship");

            AssertEqual(Player, playerShip.Loyalty, "LoyaltyChangeAtSpawn is broken");

            var nearbyPlayerShips = FindNearbyShips(Player);
            AssertEqual(1, nearbyPlayerShips.Length, "There should be 1 Player ship nearby");
            AssertEqual(playerShip.Id, ((Ship)nearbyPlayerShips[0]).Id);

            var nearbyEnemyShips = FindNearbyShips(Enemy);
            AssertEqual(0, nearbyEnemyShips.Length, "There should be 0 Enemy ships nearby");
        }

        void EnsureLoyaltyTransferAndSpatialCoherence(Ship transferredShip)
        {
            AssertEqual(1, UState.Objects.NumShips);
            Assert.IsTrue(Player.OwnedShips.Contains(transferredShip), "Player.OwnedShips MUST contain the ship before update");
            Assert.IsFalse(Enemy.OwnedShips.Contains(transferredShip), "Enemy.OwnedShips must NOT contain the ship before update");
            RunObjectsSim(TestSimStep);
            RunObjectsSim(TestSimStep);
            AssertEqual(1, UState.Objects.NumShips);
            Assert.IsFalse(Player.OwnedShips.Contains(transferredShip), "Player.OwnedShips must NOT contain the ship AFTER transfer and update");
            Assert.IsTrue(Enemy.OwnedShips.Contains(transferredShip), "Enemy.OwnedShips MUST contain the ship  AFTER transfer and update");

            AssertEqual(Enemy, transferredShip.Loyalty, "LoyaltyChange failed! Incorrect loyalty!");

            var nearbyPlayerShips = FindNearbyShips(Player);
            AssertEqual(0, nearbyPlayerShips.Length, "There should be no Player ships nearby");

            var nearbyEnemyShips = FindNearbyShips(Enemy);
            AssertEqual(1, nearbyEnemyShips.Length, "There should be 1 Enemy ship nearby");
            AssertEqual(transferredShip.Id, ((Ship)nearbyEnemyShips[0]).Id);
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
