using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.ShipDesign;
using Ship_Game.Ships;

namespace UnitTests.Universe
{
    [TestClass]
    public class UniverseObjectManagerTests : StarDriveTest
    {
        public UniverseObjectManagerTests()
        {
            CreateUniverseAndPlayerEmpire();
        }

        [TestMethod]
        public void SpawnedShipIsAddedToEmpireAndUniverse()
        {
            var spawnedShip = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            RunObjectsSim(TestSimStep);
            Assert.AreEqual(Player.OwnedShips.Count, 1, "Ship was not added to Player's Empire OwnedShips");
            Assert.IsTrue(Universe.Objects.Ships.Contains(spawnedShip), "Ship was not added to UniverseObjectManager");
        }

        [TestMethod]
        public void DesignShipIsNotAddedToEmpireAndUniverse()
        {
            IShipDesign design = ResourceManager.Ships.GetDesign("Vulcan Scout");
            var mustNotBeAddedToEmpireOrUniverse = new DesignShip(Universe, design as ShipDesign);
            RunObjectsSim(TestSimStep);
            Assert.AreEqual(0, Player.OwnedShips.Count, "DesignShip should not be added to Player's Empire OwnedShips");
            Assert.AreEqual(0, Universe.Objects.Ships.Count, "DesignShip should not be added to UniverseObjectManager");
        }

        [TestMethod]
        public void ShipsWithNoModulesShouldNotBeAddedToEmpire()
        {
            ShipDesign design = ResourceManager.Ships.GetDesign("Vulcan Scout").GetClone(null);
            design.SetDesignSlots(Empty<DesignSlot>.Array);

            // somehow we manage to create one
            var emptyTemplate = new DesignShip(Universe, design);
            var mustNotBeAddedToEmpireOrUniverse = Ship.CreateShipAtPoint(Universe, emptyTemplate, Player, Vector2.Zero);
            RunObjectsSim(TestSimStep);
            Assert.AreEqual(0, Player.OwnedShips.Count, "Ship with empty modules should not be added to Player's Empire OwnedShips");
            Assert.AreEqual(0, Universe.Objects.Ships.Count, "Ship with empty modules should not be added to UniverseObjectManager");
        }
    }
}
