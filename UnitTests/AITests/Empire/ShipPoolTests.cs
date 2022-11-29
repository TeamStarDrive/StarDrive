using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Empires.ShipPools;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.AITests.Empire
{
    [TestClass]
    public class ShipPoolTests : StarDriveTest
    {
        Planet Homeworld;

        public ShipPoolTests()
        {
            LoadStarterShips("Heavy Carrier mk5-b", "Medium Freighter", 
                             "Vulcan Scout", "Unarmed Scout",
                             "Vingscout", "Owlwok Constructor", "Owlwok Freighter S");
            CreateUniverseAndPlayerEmpire();
            Homeworld = AddHomeWorldToEmpire(new Vector2(2000), Enemy);
            Enemy.Update(UState, TestSimStep); // need to update the empire first to create AO's
            RunObjectsSim(TestSimStep);
        }
        
        [TestMethod]
        public void FighterIsAddedToGeneralAO()
        {
            // Only AI ships will be auto-added to Pools
            Ship ship = SpawnShip("Vulcan Scout", Enemy, Vector2.Zero);
            AssertEqual(null, ship.Pool);
            RunObjectsSim(TestSimStep);

            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");
            AssertEqual(ship.Loyalty, ship.Pool.Owner);
            AssertTrue(ship.Pool is AO, "Ship should be assigned to an AO");
        }
        
        [TestMethod]
        public void CarrierIsAddedToCoreShipPool()
        {
            Ship ship = SpawnShip("Heavy Carrier mk5-b", Enemy, Vector2.Zero);
            AssertEqual(null, ship.Pool);
            RunObjectsSim(TestSimStep);

            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");
            AssertEqual(ship.Loyalty, ship.Pool.Owner);
            AssertTrue(ship.Pool is ShipPool, "Ship should be assigned to Empire's Core pool");
        }
        
        [TestMethod]
        public void ColonyShipIsNotAddedToForcePools()
        {
            Ship ship = SpawnShip("Colony Ship", Enemy, Vector2.Zero);
            RunObjectsSim(TestSimStep);
            AssertEqual(null, ship.Pool, "Colony Ship should not be added to any Force Pools");
        }
        
        [TestMethod]
        public void FreighterIsNotAddedToForcePools()
        {
            Ship ship = SpawnShip("Medium Freighter", Enemy, Vector2.Zero);
            RunObjectsSim(TestSimStep);
            AssertEqual(null, ship.Pool, "Freighter should not be added to any Force Pools");
        }

        // AIState.Scrap
        [TestMethod]
        public void ScrappingShipMustNotBeInAPool()
        {
            Ship ship = SpawnShip("Vulcan Scout", Enemy, Vector2.Zero);
            RunObjectsSim(TestSimStep);
            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");

            ship.AI.OrderScrapShip();
            RunObjectsSim(TestSimStep);
            AssertEqual(null, ship.Pool, "Ship must be removed from ShipPools after OrderScrap");
        }

        // AIState.Scuttle
        [TestMethod]
        public void ScuttlingShipMustNotBeInAPool()
        {
            Ship ship = SpawnShip("Vulcan Scout", Enemy, Vector2.Zero);
            RunObjectsSim(TestSimStep);
            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");

            ship.AI.OrderScuttleShip();
            RunObjectsSim(TestSimStep);
            AssertEqual(null, ship.Pool, "Ship must be removed from ShipPools after OrderScuttle");
        }

        // AIState.Refit
        [TestMethod]
        public void RefittingShipMustNotBeInAPool()
        {
            Ship ship = SpawnShip("Vulcan Scout", Enemy, Vector2.Zero);
            RunObjectsSim(TestSimStep);
            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");

            IShipDesign refitTo = ResourceManager.Ships.GetDesign("Rocket Scout");
            ship.AI.OrderRefitTo(Homeworld, new RefitShip(ship, refitTo, Enemy));
            RunObjectsSim(TestSimStep);
            AssertEqual(null, ship.Pool, "Ship must be removed from ShipPools after OrderRefitTo");
        }

        // AIState.Resupply
        [TestMethod]
        public void ResupplyingShipMustRemainInItsPool()
        {
            Ship ship = SpawnShip("Vulcan Scout", Enemy, new Vector2(4000, 4000));
            RunObjectsSim(TestSimStep);

            Homeworld.ParentSystem.ShipList.Add(ship);
            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");
            IShipPool originalPool = ship.Pool;
            
            ship.ChangeOrdnance(-25);
            ship.AI.OrderResupply(Homeworld, clearOrders:true);
            RunSimWhile((simTimeout:90, fatal:true), () => ship.AI.State == AIState.Resupply, () =>
            {
                Homeworld.GeodeticManager.AffectNearbyShips();
                AssertEqual(originalPool, ship.Pool, "Ship must remain in the same pool during Resupply");
            });
        }

        [TestMethod]
        public void ShipIsRemovedFromPoolsAfterDeath()
        {
            Ship ship = SpawnShip("Vulcan Scout", Enemy, Vector2.Zero);
            RunObjectsSim(TestSimStep);
            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");

            ship.Die(ship, cleanupOnly: true);
            RunObjectsSim(TestSimStep);
            AssertEqual(null, ship.Pool, "Ship must be removed from ShipPools after Death");
        }

        [TestMethod]
        public void ShipIsMovedToNewPoolAfterLoyaltyChange()
        {
            CreateThirdMajorEmpire();
            Enemy.AI.AreasOfOperations.Add(new AO(UState, Enemy));
            Ship ship = SpawnShip("Vulcan Scout", Enemy, Vector2.Zero);
            RunObjectsSim(TestSimStep);
            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");

            // Third major empire boards the ship
            var oldPool = ship.Pool;
            ship.LoyaltyChangeFromBoarding(ThirdMajor, false);
            RunObjectsSim(TestSimStep);
            Assert.AreNotEqual(oldPool, ship.Pool, "Ship must be moved to new pool");
        }
    }
}
