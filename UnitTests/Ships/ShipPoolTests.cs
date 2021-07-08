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

namespace UnitTests.Ships
{
    [TestClass]
    public class ShipPoolTests : StarDriveTest
    {
        Planet Homeworld;

        public ShipPoolTests()
        {
            CreateGameInstance();
            LoadStarterShips(starterShips:new[]{ "Vulcan Scout", "Excalibur-Class Supercarrier" },
                             savedDesigns:new[]{ "Prototype Frigate" },
                             ResourceManager.TestOptions.LoadPlanets);
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
            Assert.AreEqual(ship.loyalty, ship.Pool.OwnerEmpire);
            Assert.IsTrue(ship.Pool is AO, "Ship should be assigned to an AO");
        }
        
        [TestMethod]
        public void CarrierIsAddedToCoreShipPool()
        {
            Ship ship = SpawnShip("Excalibur-Class Supercarrier", Enemy, Vector2.Zero);
            Assert.AreEqual(null, ship.Pool);
            Universe.Objects.Update(TestSimStep);

            Assert.AreNotEqual(null, ship.Pool, "Ship was not added to empire ShipPool !");
            Assert.AreEqual(ship.loyalty, ship.Pool.OwnerEmpire);
            Assert.IsTrue(ship.Pool is ShipPool, "Ship should be assigned to Empire's Core pool");
        }

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
    }
}
