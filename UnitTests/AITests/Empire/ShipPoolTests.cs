using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Empires.ShipPools;
using Ship_Game.Ships;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class ShipPoolTests : StarDriveTest
    {
        Planet Homeworld;

        public ShipPoolTests()
        {
            LoadStarterShips("Heavy Carrier mk5-b", "Medium Freighter");
            CreateUniverseAndPlayerEmpire();
            AddHomeWorldToEmpire(Enemy, out Homeworld);
            Enemy.Update(TestSimStep); // need to update the empire first to create AO's
            Universe.Objects.Update(TestSimStep);
        }
        
        [TestMethod]
        public void FighterIsAddedToGeneralAO()
        {
            // Only AI ships will be auto-added to Pools
            Ship ship = SpawnShip("Vulcan Scout", Enemy, Vector2.Zero);
            Assert.AreEqual(null, ship.Pool);
            Universe.Objects.Update(TestSimStep);

            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");
            Assert.AreEqual(ship.Loyalty, ship.Pool.OwnerEmpire);
            Assert.IsTrue(ship.Pool is AO, "Ship should be assigned to an AO");
        }
        
        [TestMethod]
        public void CarrierIsAddedToCoreShipPool()
        {
            Ship ship = SpawnShip("Heavy Carrier mk5-b", Enemy, Vector2.Zero);
            Assert.AreEqual(null, ship.Pool);
            Universe.Objects.Update(TestSimStep);

            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");
            Assert.AreEqual(ship.Loyalty, ship.Pool.OwnerEmpire);
            Assert.IsTrue(ship.Pool is ShipPool, "Ship should be assigned to Empire's Core pool");
        }
        
        [TestMethod]
        public void ColonyShipIsNotAddedToForcePools()
        {
            Ship ship = SpawnShip("Colony Ship", Enemy, Vector2.Zero);
            Universe.Objects.Update(TestSimStep);
            Assert.AreEqual(null, ship.Pool, "Colony Ship should not be added to any Force Pools");
        }
        
        [TestMethod]
        public void FreighterIsNotAddedToForcePools()
        {
            Ship ship = SpawnShip("Medium Freighter", Enemy, Vector2.Zero);
            Universe.Objects.Update(TestSimStep);
            Assert.AreEqual(null, ship.Pool, "Freighter should not be added to any Force Pools");
        }

        // AIState.Scrap
        [TestMethod]
        public void ScrappingShipMustNotBeInAPool()
        {
            Ship ship = SpawnShip("Vulcan Scout", Enemy, Vector2.Zero);
            Universe.Objects.Update(TestSimStep);
            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");

            ship.AI.OrderScrapShip();
            Universe.Objects.Update(TestSimStep);
            Assert.AreEqual(null, ship.Pool, "Ship must be removed from ShipPools after OrderScrap");
        }

        // AIState.Scuttle
        [TestMethod]
        public void ScuttlingShipMustNotBeInAPool()
        {
            Ship ship = SpawnShip("Vulcan Scout", Enemy, Vector2.Zero);
            Universe.Objects.Update(TestSimStep);
            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");

            ship.AI.OrderScuttleShip();
            Universe.Objects.Update(TestSimStep);
            Assert.AreEqual(null, ship.Pool, "Ship must be removed from ShipPools after OrderScuttle");
        }

        // AIState.Refit
        [TestMethod]
        public void RefittingShipMustNotBeInAPool()
        {
            Ship ship = SpawnShip("Vulcan Scout", Enemy, Vector2.Zero);
            Universe.Objects.Update(TestSimStep);
            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");

            ship.AI.OrderRefitTo(Homeworld, new RefitShip(ship, "Rocket Scout", Enemy));
            Universe.Objects.Update(TestSimStep);
            Assert.AreEqual(null, ship.Pool, "Ship must be removed from ShipPools after OrderScrap");
        }

        // AIState.Resupply
        [TestMethod]
        public void ResupplyingShipMustNotBeInAPool()
        {
            Ship ship = SpawnShip("Vulcan Scout", Enemy, Vector2.Zero);
            ship.Position = new Vector2(10000, 10000);
            Universe.Objects.Update(TestSimStep);
            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");
            IShipPool originalPool = ship.Pool;

            ship.AI.OrderResupply(Homeworld, true);
            while (ship.AI.State == AIState.Resupply)
            {
                Universe.Objects.Update(TestSimStep);
                Assert.AreEqual(originalPool, ship.Pool, "Ship must remain in the same pool during Resupply");
            }
        }

        [TestMethod]
        public void ShipIsRemovedFromPoolsAfterDeath()
        {
            Ship ship = SpawnShip("Vulcan Scout", Enemy, Vector2.Zero);
            Universe.Objects.Update(TestSimStep);
            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");

            ship.Die(ship, cleanupOnly: true);
            Universe.Objects.Update(TestSimStep);
            Assert.AreEqual(null, ship.Pool, "Ship must be removed from ShipPools after Death");
        }

        [TestMethod]
        public void ShipIsMovedToNewPoolAfterLoyaltyChange()
        {
            CreateThirdMajorEmpire();
            Enemy.GetEmpireAI().AreasOfOperations.Add(new AO());
            Ship ship = SpawnShip("Vulcan Scout", Enemy, Vector2.Zero);
            Universe.Objects.Update(TestSimStep);
            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");

            // Third major empire boards the ship
            var oldPool = ship.Pool;
            ship.LoyaltyChangeFromBoarding(ThirdMajor, false);
            Universe.Objects.Update(TestSimStep);
            Assert.AreNotEqual(oldPool, ship.Pool, "Ship must be moved to new pool");
        }
    }
}
