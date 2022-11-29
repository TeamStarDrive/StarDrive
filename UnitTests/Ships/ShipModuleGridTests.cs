using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;
using Ship_Game.Data.Binary;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Point = SDGraphics.Point;
using Ship_Game.Universe;
using UnitTests.Serialization;

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
        /// </summary>
        [TestMethod]
        public void Regression_LoadSavedShip()
        {
            Ship toSave = SpawnShip("Terran-Prototype", Player, Vector2.Zero);
            UniverseState us = BinarySerializerTests.SerDes(UState);
            
            Ship deserialized = us.Player.OwnedShips[0];
            AssertEqual(new Point(20,28), deserialized.GridSize);
            AssertEqual(toSave.Modules.Length, deserialized.Modules.Length);

            // if the UniverseSaveData design is exactly equal to an existing design,
            // then the existing one will be used and the one from savegame is discarded
            AssertEqual(toSave.ShipData, deserialized.ShipData, "Expected matching ShipData to be replaced from ShipsManager");
        }

        [TestMethod]
        public void Regression_LoadSavedShip_NoExistingDesign()
        {
            ShipDesign unknownDesign = ResourceManager.Ships.GetDesign("Terran-Prototype")
                                       .GetClone("Unknown-Ship");
            Ship unknownTemplate = Ship.CreateNewShipTemplate(Empire.Void, unknownDesign);

            Ship toSave = SpawnShip(unknownTemplate, Player, Vector2.Zero);
            UniverseState us = BinarySerializerTests.SerDes(UState);

            Ship deserialized = us.Player.OwnedShips[0];
            AssertEqual(new Point(20,28), deserialized.GridSize);
            AssertEqual(toSave.Modules.Length, deserialized.Modules.Length);

            // the design only exists in the savegame
            Assert.IsTrue(deserialized.ShipData.IsFromSave, "IsFromSave must be true");
        }

        /// <summary>
        /// If any of these fail, the ModuleGrid is broken!
        /// </summary>
        [TestMethod]
        public void Regression_StarterShips_ModuleGrid()
        {
            Ship vulcan = SpawnShip("TEST_Vulcan Scout", Player, Vector2.Zero);
            AssertEqual(new Point(4,4), vulcan.GridSize);

            Ship prototype = SpawnShip("Terran-Prototype", Player, Vector2.Zero);
            AssertEqual(new Point(20,28), prototype.GridSize);
        }

        static Vector2 Vec(float x, float y) => new(x,y);
        static Point Pos(int x, int y) => new(x,y);

        [TestMethod]
        public void WorldToGridLocalCoords()
        {
            var c = Vec(1000, 1000);
            Ship ship = SpawnShip("TEST_Vulcan Scout", Player, c);
            AssertEqual(4, ship.GridWidth);

            //  0  16  32  48    center = 32,32
            // x0  x1  x2  x3
            // ___|IO_|IO_|___ y0 0
            // ___|I__|I__|___ y1 16
            // IO_|I__|IC_|IO_ y2 32
            // IO_|E__|E__|IO_ y3 48

            AssertEqual(Vec(32,32),   ship.WorldToGridLocal( c ));
            AssertEqual(Vec(0,0),     ship.WorldToGridLocal( c - Vec(32,32) ));
            AssertEqual(Vec(-32,-32), ship.WorldToGridLocal( c - Vec(64,64) ));
            AssertEqual(Vec(64,64),   ship.WorldToGridLocal( c + Vec(32,32) ));

            AssertEqual(Pos(2,2),   ship.WorldToGridLocalPoint( c ));
            AssertEqual(Pos(0,0),   ship.WorldToGridLocalPoint( c - Vec(32,32) ));
            AssertEqual(Pos(-2,-2), ship.WorldToGridLocalPoint( c - Vec(64,64) ));
            AssertEqual(Pos(4,4),   ship.WorldToGridLocalPoint( c + Vec(32,32) ));

            AssertEqual(Pos(0,0), ship.WorldToGridLocalPointClipped( c - Vec(64,64)  )); // outside TopLeft
            AssertEqual(Pos(3,0), ship.WorldToGridLocalPointClipped( c + Vec(48,-48) )); // outside TopRight
            AssertEqual(Pos(3,3), ship.WorldToGridLocalPointClipped( c + Vec(32,32)  )); // outside BotRight
            AssertEqual(Pos(0,3), ship.WorldToGridLocalPointClipped( c + Vec(-48,48) )); // outside BotLeft

            AssertEqual(Pos(0,0), ship.GridLocalToPointClipped( Vec(-16,-16) )); // outside TopLeft
            AssertEqual(Pos(3,0), ship.GridLocalToPointClipped( Vec(64,-16)  )); // outside TopRight
            AssertEqual(Pos(3,3), ship.GridLocalToPointClipped( Vec(64,64)   )); // outside BotRight
            AssertEqual(Pos(0,3), ship.GridLocalToPointClipped( Vec(-16,64)  )); // outside BotLeft

            // one coordinate in range, other out of bounds
            AssertEqual(Pos(0,2), ship.WorldToGridLocalPointClipped( c - Vec(48,0) ));
            AssertEqual(Pos(2,0), ship.WorldToGridLocalPointClipped( c - Vec(0,48) ));
            AssertEqual(Pos(3,2), ship.WorldToGridLocalPointClipped( c + Vec(32,0) ));
            AssertEqual(Pos(3,2), ship.WorldToGridLocalPointClipped( c + Vec(48,0) ));
            AssertEqual(Pos(2,3), ship.WorldToGridLocalPointClipped( c + Vec(0,32) ));
            AssertEqual(Pos(2,3), ship.WorldToGridLocalPointClipped( c + Vec(0,48) ));
        }

        [TestMethod]
        public void WorldToGridLocalCoords_RotatedToLeft()
        {
            var c = Vec(1000, 1000);
            Ship ship = SpawnShip("TEST_Vulcan Scout", Player, c);
            AssertEqual(4, ship.GridWidth);
            
            ship.RotationDegrees = -90; // facing towards left
            ship.UpdateModulePositions(TestSimStep, true, forceUpdate:true);

            // pick a 1x1 module which should be at [0,2]
            AssertEqual(Pos(1,1), ship.GetModuleAt(0,2).GetSize());
            // and now check the expected CENTER of the module
            AssertEqual(0.1f, c + Vec(8,24), ship.GetModuleAt(0,2).Position);

            //  0  16  32  48            0  16  32  48
            // x0  x1  x2  x3           y0  y1  y2  y3
            // ___|IO_|IO_|___ y0 0     ___|___|IO_|IO_ x3 48
            // ___|I__|I__|___ y1 16    IO_|I__|IC_|E__ x2 32
            // IO_|I__|IC_|IO_ y2 32    IO_|I__|I__|E__ x1 16
            // IO_|E__|E__|IO_ y3 48    ___|___|IO_|IO_ x0 0

            AssertEqual(Vec(32,32),  ship.WorldToGridLocal( c ));
            AssertEqual(Vec(64,0),   ship.WorldToGridLocal( c - Vec(32,32) ));
            AssertEqual(Vec(96,-32), ship.WorldToGridLocal( c - Vec(64,64) ));
            AssertEqual(Vec(0,64),   ship.WorldToGridLocal( c + Vec(32,32) ));

            AssertEqual(Pos(2,2),  ship.WorldToGridLocalPoint( c ));
            AssertEqual(Pos(4,0),  ship.WorldToGridLocalPoint( c - Vec(32,32) ));
            AssertEqual(Pos(6,-2), ship.WorldToGridLocalPoint( c - Vec(64,64) ));
            AssertEqual(Pos(0,4),  ship.WorldToGridLocalPoint( c + Vec(32,32) ));

            AssertEqual(Pos(3,0), ship.WorldToGridLocalPointClipped( c - Vec(64,64)  )); // outside TopLeft
            AssertEqual(Pos(3,3), ship.WorldToGridLocalPointClipped( c + Vec(48,-48) )); // outside TopRight
            AssertEqual(Pos(0,3), ship.WorldToGridLocalPointClipped( c + Vec(32,32)  )); // outside BotRight
            AssertEqual(Pos(0,0), ship.WorldToGridLocalPointClipped( c + Vec(-48,48) )); // outside BotLeft
        }

        [TestMethod]
        public void WorldToGridLocalCoords_Rotated180()
        {
            var c = Vec(1000, 1000);
            Ship ship = SpawnShip("TEST_Vulcan Scout", Player, c);
            AssertEqual(4, ship.GridWidth);

            ship.RotationDegrees = 180; // facing towards down
            ship.UpdateModulePositions(TestSimStep, true, forceUpdate:true);

            // pick a 1x1 module which should be at [0,2]
            AssertEqual(Pos(1,1), ship.GetModuleAt(0,2).GetSize());
            // and now check the expected CENTER of the module
            AssertEqual(0.1f, c + Vec(24,-8), ship.GetModuleAt(0,2).Position);

            //  0  16  32  48           48  32  16   0
            // x0  x1  x2  x3           x3  x2  x1  x0
            // ___|IO_|IO_|___ y0 0     IO_|E__|E__|IO_ y3 48
            // ___|I__|I__|___ y1 16    IO_|IC_|I__|IO_ y2 32
            // IO_|I__|IC_|IO_ y2 32    ___|I__|I__|___ y1 16
            // IO_|E__|E__|IO_ y3 48    ___|IO_|IO_|___ y0 0

            AssertEqual(Vec(32,32), ship.WorldToGridLocal( c ));
            AssertEqual(Vec(64,64), ship.WorldToGridLocal( c - Vec(32,32) ));
            AssertEqual(Vec(96,96), ship.WorldToGridLocal( c - Vec(64,64) ));
            AssertEqual(Vec(0,0),   ship.WorldToGridLocal( c + Vec(32,32) ));

            AssertEqual(Pos(2,2), ship.WorldToGridLocalPoint( c ));
            AssertEqual(Pos(4,4), ship.WorldToGridLocalPoint( c - Vec(32,32) ));
            AssertEqual(Pos(6,6), ship.WorldToGridLocalPoint( c - Vec(64,64) ));
            AssertEqual(Pos(0,0), ship.WorldToGridLocalPoint( c + Vec(32,32) ));

            AssertEqual(Pos(3,3), ship.WorldToGridLocalPointClipped( c - Vec(64,64)  )); // outside TopLeft
            AssertEqual(Pos(0,3), ship.WorldToGridLocalPointClipped( c + Vec(48,-48) )); // outside TopRight
            AssertEqual(Pos(0,0), ship.WorldToGridLocalPointClipped( c + Vec(32,32)  )); // outside BotRight
            AssertEqual(Pos(3,0), ship.WorldToGridLocalPointClipped( c + Vec(-48,48) )); // outside BotLeft
        }

        [TestMethod]
        public void GridLocalToWorldCoords()
        {
            var c = Vec(1000, 1000);
            Ship ship = SpawnShip("TEST_Vulcan Scout", Player, c);
            AssertEqual(4, ship.GridWidth);

            AssertEqual(c + Vec(0,0),   ship.GridLocalToWorld( Vec(32,32)   ));
            AssertEqual(c - Vec(32,32), ship.GridLocalToWorld( Vec(0,0)     ));
            AssertEqual(c - Vec(64,64), ship.GridLocalToWorld( Vec(-32,-32) ));
            AssertEqual(c + Vec(32,32), ship.GridLocalToWorld( Vec(64,64)   ));

            AssertEqual(c + Vec(0,0),   ship.GridLocalPointToWorld( Pos(2,2)   ));
            AssertEqual(c - Vec(32,32), ship.GridLocalPointToWorld( Pos(0,0)   ));
            AssertEqual(c - Vec(64,64), ship.GridLocalPointToWorld( Pos(-2,-2) ));
            AssertEqual(c + Vec(32,32), ship.GridLocalPointToWorld( Pos(4,4)   ));
        }

        [TestMethod]
        public void DesignModuleGrid_WorldToGridLocalCoords()
        {
            Vector2 c = Vector2.Zero;
            Ship ship = SpawnShip("TEST_Vulcan Scout", Player, c);
            var grid = new DesignModuleGrid(null, ship.ShipData as ShipDesign);

            AssertEqual(4, grid.Width);
            AssertEqual(4, grid.Height);

            AssertEqual(Pos(2,2),   grid.WorldToGridPos( c ));
            AssertEqual(Pos(0,0),   grid.WorldToGridPos( c - Vec(32,32) ));
            AssertEqual(Pos(-2,-2), grid.WorldToGridPos( c - Vec(64,64) ));
            AssertEqual(Pos(4,4),   grid.WorldToGridPos( c + Vec(32,32) ));
        }

        [TestMethod]
        public void ModuleGridClipCoordinateBounds()
        {
            var c = new Vector2(1000);
            Ship ship = SpawnShip("TEST_Vulcan Scout", Player, c);
            
            //  0  16  32  48    center = 32,32  size = 64,64
            // x0  x1  x2  x3
            // ___|IO_|IO_|___ y0 0
            // ___|I__|I__|___ y1 16
            // IO_|I__|IC_|IO_ y2 32
            // IO_|E__|E__|IO_ y3 48

            // straight parallel lines out of bounds
            Assert.IsFalse(ClipLine(Vec(-16, -16), Vec(16, -16)), "Out of bounds line should return false");
            Assert.IsFalse(ClipLine(Vec(80, -16), Vec(80, 32)), "Out of bounds line should return false");
            Assert.IsFalse(ClipLine(Vec(64, 80), Vec(32, 80)), "Out of bounds line should return false");
            Assert.IsFalse(ClipLine(Vec(-16, 80), Vec(-16, 32)), "Out of bounds line should return false");
            Assert.IsFalse(ClipLine(Vec(0, 64), Vec(48, 64)), "Out of bounds line should return false");

            // diagonal lines out of bounds
            Assert.IsFalse(ClipLine(Vec(-32, 16), Vec(16, -32)), "Out of bounds line should return false");
            Assert.IsFalse(ClipLine(Vec(48, -16), Vec(80, 16)), "Out of bounds line should return false");
            Assert.IsFalse(ClipLine(Vec(80, 48), Vec(48, 80)), "Out of bounds line should return false");
            Assert.IsFalse(ClipLine(Vec(-32, 48), Vec(16, 80)), "Out of bounds line should return false");

            // lines which are already in bounds
            AssertEqual((Vec(0,0), Vec(16,16)), GetClipped(Vec(0,0), Vec(16,16)));
            AssertEqual((Vec(32,16), Vec(48,16)), GetClipped(Vec(32,16), Vec(48,16)));
            AssertEqual((Vec(0,48), Vec(48,48)), GetClipped(Vec(0,48), Vec(48,48)));

            // lines which enter from outside horizontally
            // from left --->
            AssertEqual((Vec(0,0), Vec(16,0)), GetClipped(Vec(-16,0), Vec(16,0)));
            AssertEqual((Vec(0,32), Vec(16,32)), GetClipped(Vec(-16,32), Vec(16,32)));
            AssertEqual((Vec(0,63), Vec(16,63)), GetClipped(Vec(-16,63), Vec(16,63)));
            AssertEqual((Vec(0,63), Vec(0.5f,63)), GetClipped(Vec(-0.1f,63), Vec(0.5f,63)));

            // from right <---
            AssertEqual((Vec(63.99f,0), Vec(48,0)), GetClipped(Vec(80,0), Vec(48,0)));
            AssertEqual((Vec(63.99f,32), Vec(48,32)), GetClipped(Vec(80,32), Vec(48,32)));
            AssertEqual((Vec(63.99f,63), Vec(48,63)), GetClipped(Vec(80,63), Vec(48,63)));
            AssertEqual((Vec(63.99f,63), Vec(63.5f,63)), GetClipped(Vec(64.1f,63), Vec(63.5f,63)));

            // line entering from top
            AssertEqual(0.001f, (Vec(32,0), Vec(32,16)), GetClipped(Vec(32,-32), Vec(32,16)));

            // line entering from bottom
            AssertEqual(0.001f, (Vec(32,63.99f), Vec(32,32)), GetClipped(Vec(32,80), Vec(32,32)));


            // @return False if both [a] and [b] are out of bounds
            (Vector2, Vector2) GetClipped(Vector2 a, Vector2 b)
            {
                Vector2 ca = a, cb = b;
                Assert.IsTrue(ship.ClipLineToGrid(a, b, ref ca, ref cb), "Line should be in bounds");
                return (ca, cb);
            }
            bool ClipLine(Vector2 a, Vector2 b)
            {
                Vector2 ca = a, cb = b;
                return ship.ClipLineToGrid(a, b, ref ca, ref cb);
            }
        }
    }
}
