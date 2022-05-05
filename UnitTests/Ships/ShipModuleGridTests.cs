using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Point = SDGraphics.Point;

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
            CreateUniverseAndPlayerEmpire();
        }

        /// <summary>
        /// If any of these fail, the ModuleGrid is broken!
        /// Fix the bug inside ModuleGrid
        /// </summary>
        [TestMethod]
        public void Regression_LoadSavedShip_ModuleGrid()
        {
            Ship toSave = SpawnShip("Terran-Prototype", Player, Vector2.Zero);
            SavedGame.ShipSaveData saved = SavedGame.ShipSaveFromShip(new ShipDesignWriter(), toSave);

            Ship prototype = Ship.CreateShipFromSave(Universe.UState, Player, saved);
            Assert.AreEqual(18, prototype.GridWidth);
            Assert.AreEqual(30, prototype.GridHeight);
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

            Ship prototype = SpawnShip("Terran-Prototype", Player, Vector2.Zero);
            Assert.AreEqual(18, prototype.GridWidth);
            Assert.AreEqual(30, prototype.GridHeight);
        }

        [TestMethod]
        public void WorldToGridLocalCoords()
        {
            var c = new Vector2(1000, 1000);
            Ship ship = SpawnShip("Vulcan Scout", Player, c);
            Assert.AreEqual(4, ship.GridWidth);

            Assert.AreEqual(new Vector2(32,32),   ship.WorldToGridLocal( c ));
            Assert.AreEqual(new Vector2(0,0),     ship.WorldToGridLocal( c - new Vector2(32,32) ));
            Assert.AreEqual(new Vector2(-32,-32), ship.WorldToGridLocal( c - new Vector2(64,64) ));
            Assert.AreEqual(new Vector2(64,64),   ship.WorldToGridLocal( c + new Vector2(32,32) ));

            Assert.AreEqual(new Point(2,2),   ship.WorldToGridLocalPoint( c ));
            Assert.AreEqual(new Point(0,0),   ship.WorldToGridLocalPoint( c - new Vector2(32,32) ));
            Assert.AreEqual(new Point(-2,-2), ship.WorldToGridLocalPoint( c - new Vector2(64,64) ));
            Assert.AreEqual(new Point(4,4),   ship.WorldToGridLocalPoint( c + new Vector2(32,32) ));

            Assert.AreEqual(new Point(0,0), ship.WorldToGridLocalPointClipped( c - new Vector2(64,64) ));
            Assert.AreEqual(new Point(3,3), ship.WorldToGridLocalPointClipped( c + new Vector2(32,32) ));
        }

        [TestMethod]
        public void GridLocalToWorldCoords()
        {
            var c = new Vector2(1000, 1000);
            Ship ship = SpawnShip("Vulcan Scout", Player, c);
            Assert.AreEqual(4, ship.GridWidth);

            Assert.AreEqual(c + new Vector2(0,0),   ship.GridLocalToWorld( new Vector2(32,32)   ));
            Assert.AreEqual(c - new Vector2(32,32), ship.GridLocalToWorld( new Vector2(0,0)     ));
            Assert.AreEqual(c - new Vector2(64,64), ship.GridLocalToWorld( new Vector2(-32,-32) ));
            Assert.AreEqual(c + new Vector2(32,32), ship.GridLocalToWorld( new Vector2(64,64)   ));

            Assert.AreEqual(c + new Vector2(0,0),   ship.GridLocalPointToWorld( new Point(2,2)   ));
            Assert.AreEqual(c - new Vector2(32,32), ship.GridLocalPointToWorld( new Point(0,0)   ));
            Assert.AreEqual(c - new Vector2(64,64), ship.GridLocalPointToWorld( new Point(-2,-2) ));
            Assert.AreEqual(c + new Vector2(32,32), ship.GridLocalPointToWorld( new Point(4,4)   ));
        }

        [TestMethod]
        public void DesignModuleGrid_WorldToGridLocalCoords()
        {
            Vector2 c = Vector2.Zero;
            Ship ship = SpawnShip("Vulcan Scout", Player, c);
            var grid = new DesignModuleGrid(null, ship.ShipData as ShipDesign);

            Assert.AreEqual(4, grid.Width);
            Assert.AreEqual(4, grid.Height);

            Assert.AreEqual(new Point(2,2),   grid.WorldToGridPos( c ));
            Assert.AreEqual(new Point(0,0),   grid.WorldToGridPos( c - new Vector2(32,32) ));
            Assert.AreEqual(new Point(-2,-2), grid.WorldToGridPos( c - new Vector2(64,64) ));
            Assert.AreEqual(new Point(4,4),   grid.WorldToGridPos( c + new Vector2(32,32) ));
        }
    }
}
