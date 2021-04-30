using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    /// <summary>
    /// This test suite ensures the optimized Ship ModuleGrid
    /// does not have regressions due to refactoring.
    /// </summary>
    [TestClass]
    public class ShipModuleGridTests : StarDriveTest
    {
        public ShipModuleGridTests()
        {
            CreateGameInstance();
            LoadStarterShips(starterShips:new[]{ "Vulcan Scout" }, 
                             savedDesigns:new[]{ "Prototype Frigate" });
            CreateUniverseAndPlayerEmpire(out Empire empire);
        }

        /// <summary>
        /// If any of these fail, the ModuleGrid is broken!
        /// Fix the bug inside ModuleGrid
        /// </summary>
        [TestMethod]
        public void Regression_StarterShips_ModuleGrid()
        {
            Ship vulcan = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            Assert.AreEqual(4, vulcan.GridWidth);
            Assert.AreEqual(4, vulcan.GridHeight);

            Ship prototype = SpawnShip("Prototype Frigate", Player, Vector2.Zero);
            Assert.AreEqual(6, prototype.GridWidth);
            Assert.AreEqual(16, prototype.GridHeight);
        }

        [TestMethod]
        public void Regression_LoadSavedShip_ModuleGrid()
        {
            Ship toSave = SpawnShip("Prototype Frigate", Player, Vector2.Zero);
            SavedGame.ShipSaveData saved = SavedGame.ShipSaveFromShip(toSave);

            Ship prototype = Ship.CreateShipFromSave(Player, saved);
            Assert.AreEqual(6, prototype.GridWidth);
            Assert.AreEqual(16, prototype.GridHeight);
        }
    }
}
