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
            Ship vulcan = SpawnShip("TEST_Vulcan Scout", Player, Vector2.Zero);
            Assert.AreEqual(4, vulcan.GridWidth);
            Assert.AreEqual(4, vulcan.GridHeight);

            Ship prototype = SpawnShip("Terran-Prototype", Player, Vector2.Zero);
            Assert.AreEqual(18, prototype.GridWidth);
            Assert.AreEqual(30, prototype.GridHeight);
        }

        static Vector2 Vec(float x, float y) => new(x,y);
        static Point Pos(int x, int y) => new(x,y);

        [TestMethod]
        public void WorldToGridLocalCoords()
        {
            var c = Vec(1000, 1000);
            Ship ship = SpawnShip("TEST_Vulcan Scout", Player, c);
            Assert.AreEqual(4, ship.GridWidth);

            //  0  16  32  48    center = 32,32
            // x0  x1  x2  x3
            // ___|IO_|IO_|___ y0 0
            // ___|I__|I__|___ y1 16
            // IO_|I__|IC_|IO_ y2 32
            // IO_|E__|E__|IO_ y3 48

            Assert.AreEqual(Vec(32,32),   ship.WorldToGridLocal( c ));
            Assert.AreEqual(Vec(0,0),     ship.WorldToGridLocal( c - Vec(32,32) ));
            Assert.AreEqual(Vec(-32,-32), ship.WorldToGridLocal( c - Vec(64,64) ));
            Assert.AreEqual(Vec(64,64),   ship.WorldToGridLocal( c + Vec(32,32) ));

            Assert.AreEqual(Pos(2,2),   ship.WorldToGridLocalPoint( c ));
            Assert.AreEqual(Pos(0,0),   ship.WorldToGridLocalPoint( c - Vec(32,32) ));
            Assert.AreEqual(Pos(-2,-2), ship.WorldToGridLocalPoint( c - Vec(64,64) ));
            Assert.AreEqual(Pos(4,4),   ship.WorldToGridLocalPoint( c + Vec(32,32) ));

            Assert.AreEqual(Pos(0,0), ship.WorldToGridLocalPointClipped( c - Vec(64,64)  )); // outside TopLeft
            Assert.AreEqual(Pos(3,0), ship.WorldToGridLocalPointClipped( c + Vec(48,-48) )); // outside TopRight
            Assert.AreEqual(Pos(3,3), ship.WorldToGridLocalPointClipped( c + Vec(32,32)  )); // outside BotRight
            Assert.AreEqual(Pos(0,3), ship.WorldToGridLocalPointClipped( c + Vec(-48,48) )); // outside BotLeft

            // one coordinate in range, other out of bounds
            Assert.AreEqual(Pos(0,2), ship.WorldToGridLocalPointClipped( c - Vec(48,0) ));
            Assert.AreEqual(Pos(2,0), ship.WorldToGridLocalPointClipped( c - Vec(0,48) ));
            Assert.AreEqual(Pos(3,2), ship.WorldToGridLocalPointClipped( c + Vec(32,0) ));
            Assert.AreEqual(Pos(3,2), ship.WorldToGridLocalPointClipped( c + Vec(48,0) ));
            Assert.AreEqual(Pos(2,3), ship.WorldToGridLocalPointClipped( c + Vec(0,32) ));
            Assert.AreEqual(Pos(2,3), ship.WorldToGridLocalPointClipped( c + Vec(0,48) ));
        }

        [TestMethod]
        public void WorldToGridLocalCoords_RotatedToLeft()
        {
            var c = Vec(1000, 1000);
            Ship ship = SpawnShip("TEST_Vulcan Scout", Player, c);
            Assert.AreEqual(4, ship.GridWidth);
            
            ship.RotationDegrees = -90; // facing towards left
            ship.UpdateModulePositions(TestSimStep, true, forceUpdate:true);

            // pick a 1x1 module which should be at [0,2]
            Assert.AreEqual(Pos(1,1), ship.GetModuleAt(0,2).GetSize());
            // and now check the expected CENTER of the module
            Assert.That.Equal(0.1f, c + Vec(8,24), ship.GetModuleAt(0,2).Position);

            //  0  16  32  48            0  16  32  48
            // x0  x1  x2  x3           y0  y1  y2  y3
            // ___|IO_|IO_|___ y0 0     ___|___|IO_|IO_ x3 48
            // ___|I__|I__|___ y1 16    IO_|I__|IC_|E__ x2 32
            // IO_|I__|IC_|IO_ y2 32    IO_|I__|I__|E__ x1 16
            // IO_|E__|E__|IO_ y3 48    ___|___|IO_|IO_ x0 0

            Assert.AreEqual(Vec(32,32),  ship.WorldToGridLocal( c ));
            Assert.AreEqual(Vec(64,0),   ship.WorldToGridLocal( c - Vec(32,32) ));
            Assert.AreEqual(Vec(96,-32), ship.WorldToGridLocal( c - Vec(64,64) ));
            Assert.AreEqual(Vec(0,64),   ship.WorldToGridLocal( c + Vec(32,32) ));

            Assert.AreEqual(Pos(2,2),  ship.WorldToGridLocalPoint( c ));
            Assert.AreEqual(Pos(4,0),  ship.WorldToGridLocalPoint( c - Vec(32,32) ));
            Assert.AreEqual(Pos(6,-2), ship.WorldToGridLocalPoint( c - Vec(64,64) ));
            Assert.AreEqual(Pos(0,4),  ship.WorldToGridLocalPoint( c + Vec(32,32) ));

            Assert.AreEqual(Pos(3,0), ship.WorldToGridLocalPointClipped( c - Vec(64,64)  )); // outside TopLeft
            Assert.AreEqual(Pos(3,3), ship.WorldToGridLocalPointClipped( c + Vec(48,-48) )); // outside TopRight
            Assert.AreEqual(Pos(0,3), ship.WorldToGridLocalPointClipped( c + Vec(32,32)  )); // outside BotRight
            Assert.AreEqual(Pos(0,0), ship.WorldToGridLocalPointClipped( c + Vec(-48,48) )); // outside BotLeft
        }

        [TestMethod]
        public void WorldToGridLocalCoords_Rotated180()
        {
            var c = Vec(1000, 1000);
            Ship ship = SpawnShip("TEST_Vulcan Scout", Player, c);
            Assert.AreEqual(4, ship.GridWidth);

            ship.RotationDegrees = 180; // facing towards down
            ship.UpdateModulePositions(TestSimStep, true, forceUpdate:true);

            // pick a 1x1 module which should be at [0,2]
            Assert.AreEqual(Pos(1,1), ship.GetModuleAt(0,2).GetSize());
            // and now check the expected CENTER of the module
            Assert.That.Equal(0.1f, c + Vec(24,-8), ship.GetModuleAt(0,2).Position);

            //  0  16  32  48           48  32  16   0
            // x0  x1  x2  x3           x3  x2  x1  x0
            // ___|IO_|IO_|___ y0 0     IO_|E__|E__|IO_ y3 48
            // ___|I__|I__|___ y1 16    IO_|IC_|I__|IO_ y2 32
            // IO_|I__|IC_|IO_ y2 32    ___|I__|I__|___ y1 16
            // IO_|E__|E__|IO_ y3 48    ___|IO_|IO_|___ y0 0

            Assert.AreEqual(Vec(32,32), ship.WorldToGridLocal( c ));
            Assert.AreEqual(Vec(64,64), ship.WorldToGridLocal( c - Vec(32,32) ));
            Assert.AreEqual(Vec(96,96), ship.WorldToGridLocal( c - Vec(64,64) ));
            Assert.AreEqual(Vec(0,0),   ship.WorldToGridLocal( c + Vec(32,32) ));

            Assert.AreEqual(Pos(2,2), ship.WorldToGridLocalPoint( c ));
            Assert.AreEqual(Pos(4,4), ship.WorldToGridLocalPoint( c - Vec(32,32) ));
            Assert.AreEqual(Pos(6,6), ship.WorldToGridLocalPoint( c - Vec(64,64) ));
            Assert.AreEqual(Pos(0,0), ship.WorldToGridLocalPoint( c + Vec(32,32) ));

            Assert.AreEqual(Pos(3,3), ship.WorldToGridLocalPointClipped( c - Vec(64,64)  )); // outside TopLeft
            Assert.AreEqual(Pos(0,3), ship.WorldToGridLocalPointClipped( c + Vec(48,-48) )); // outside TopRight
            Assert.AreEqual(Pos(0,0), ship.WorldToGridLocalPointClipped( c + Vec(32,32)  )); // outside BotRight
            Assert.AreEqual(Pos(3,0), ship.WorldToGridLocalPointClipped( c + Vec(-48,48) )); // outside BotLeft
        }

        [TestMethod]
        public void GridLocalToWorldCoords()
        {
            var c = Vec(1000, 1000);
            Ship ship = SpawnShip("TEST_Vulcan Scout", Player, c);
            Assert.AreEqual(4, ship.GridWidth);

            Assert.AreEqual(c + Vec(0,0),   ship.GridLocalToWorld( Vec(32,32)   ));
            Assert.AreEqual(c - Vec(32,32), ship.GridLocalToWorld( Vec(0,0)     ));
            Assert.AreEqual(c - Vec(64,64), ship.GridLocalToWorld( Vec(-32,-32) ));
            Assert.AreEqual(c + Vec(32,32), ship.GridLocalToWorld( Vec(64,64)   ));

            Assert.AreEqual(c + Vec(0,0),   ship.GridLocalPointToWorld( Pos(2,2)   ));
            Assert.AreEqual(c - Vec(32,32), ship.GridLocalPointToWorld( Pos(0,0)   ));
            Assert.AreEqual(c - Vec(64,64), ship.GridLocalPointToWorld( Pos(-2,-2) ));
            Assert.AreEqual(c + Vec(32,32), ship.GridLocalPointToWorld( Pos(4,4)   ));
        }

        [TestMethod]
        public void DesignModuleGrid_WorldToGridLocalCoords()
        {
            Vector2 c = Vector2.Zero;
            Ship ship = SpawnShip("TEST_Vulcan Scout", Player, c);
            var grid = new DesignModuleGrid(null, ship.ShipData as ShipDesign);

            Assert.AreEqual(4, grid.Width);
            Assert.AreEqual(4, grid.Height);

            Assert.AreEqual(Pos(2,2),   grid.WorldToGridPos( c ));
            Assert.AreEqual(Pos(0,0),   grid.WorldToGridPos( c - Vec(32,32) ));
            Assert.AreEqual(Pos(-2,-2), grid.WorldToGridPos( c - Vec(64,64) ));
            Assert.AreEqual(Pos(4,4),   grid.WorldToGridPos( c + Vec(32,32) ));
        }
    }
}
