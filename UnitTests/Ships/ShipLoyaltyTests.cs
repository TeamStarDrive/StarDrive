using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Ships
{
    [TestClass]
    public class ShipLoyaltyTests : StarDriveTest
    {
        public ShipLoyaltyTests()
        {
            CreateGameInstance();
            LoadStarterShipVulcan();
            CreateUniverseAndPlayerEmpire();
        }

        GameplayObject[] FindNearbyShips(Empire loyalty)
        {
            return UniverseScreen.Spatial.FindNearby(GameObjectType.Ship, Vector2.Zero,
                                                     1000, 10, onlyLoyalty: loyalty);
        }

        void EnsureSpatialCoherence(Ship playerShip)
        {
            // NOTE: This covers the LoyaltyChangeAtSpawn case
            Assert.AreEqual(0, Universe.Objects.Ships.Count);
            Universe.Objects.Update(TestSimStep);
            Assert.AreEqual(1, Universe.Objects.Ships.Count);

            Assert.AreEqual(Player, playerShip.loyalty, "LoyaltyChangeAtSpawn is broken");

            var nearbyPlayerShips = FindNearbyShips(Player);
            Assert.AreEqual(1, nearbyPlayerShips.Length, "There should be 1 Player ship nearby");
            Assert.AreEqual(playerShip.Id, nearbyPlayerShips[0].Id);

            var nearbyEnemyShips = FindNearbyShips(Enemy);
            Assert.AreEqual(0, nearbyEnemyShips.Length, "There should be 0 Enemy ships nearby");
        }

        void EnsureLoyaltyTransferAndSpatialCoherence(Ship transferredShip)
        {
            Universe.Objects.Update(TestSimStep);
            Assert.AreEqual(Enemy, transferredShip.loyalty);

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
            EnsureSpatialCoherence(ship);
            ship.LoyaltyChangeFromBoarding(Enemy, addNotification:false);
            EnsureLoyaltyTransferAndSpatialCoherence(ship);
        }

        [TestMethod]
        public void LoyaltyChangeByGift()
        {
            var ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            EnsureSpatialCoherence(ship);
            ship.LoyaltyChangeByGift(Enemy, addNotification:false);
            EnsureLoyaltyTransferAndSpatialCoherence(ship);
        }
    }
}
