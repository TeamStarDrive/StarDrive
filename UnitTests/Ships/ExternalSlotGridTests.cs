using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Point = SDGraphics.Point;

namespace UnitTests.Ships
{
    [TestClass]
    public class ExternalSlotGridTests : StarDriveTest
    {
        public ExternalSlotGridTests()
        {
            CreateUniverseAndPlayerEmpire();
            LoadStarterShips("TEST_Spearhead mk1-a");
        }

        [TestMethod]
        public void FrigateExternals()
        {
            Ship ship = SpawnShip("TEST_Spearhead mk1-a", Player, Vector2.Zero);
            Assert.AreEqual(new Point(6, 18), new Point(ship.Grid.Width, ship.Grid.Height));
            var gs = ship.GetGridState();

            ship.Externals.DebugDump("TEST_Spearhead mk1-a", gs);

            //      0    1    2    3    4    5  
            //  0 |____|____|E2x2|E2x2|____|____
            //  1 |____|E1x1|E2x2|E2x2|E1x1|____
            //  2 |E1x1|E1x1 1x2  1x2 |E1x1|E1x1
            //  3 |E1x1|E1x2 1x2  1x2 |E1x2|E1x1
            //  4 |E1x1|E1x2 1x1  1x1 |E1x2|E1x1
            //  5 |____|E1x1 1x1  1x1 |E1x1|____
            //  6 |E1x1|E1x1 1x1  1x1 |E1x1|E1x1
            //  7 |E1x1 2x2  2x2  2x2  2x2 |E1x1
            //  8 |E1x1 2x2  2x2  2x2  2x2 |E1x1
            //  9 |E1x1|E1x1 1x2  1x2 |E1x1|E1x1
            // 10 |____|E1x1 1x2  1x2 |E1x1|____
            // 11 |____|E1x1 1x1  1x1 |E1x1|____
            // 12 |E1x1|E1x2 1x1  1x1 |E1x2|E1x1
            // 13 |E1x1|E1x2 1x1  1x1 |E1x2|E1x1
            // 14 |E1x1 1x1  2x2  2x2  1x1 |E1x1
            // 15 |E1x1 1x1  2x2  2x2  1x1 |E1x1
            // 16 |E1x1|E1x1 1x1  1x1 |E1x1|E1x1
            // 17 |____|E1x1|E1x1|E1x1|E1x1|____

            ShipModule At(int x, int y) => ship.GetModuleAt(x,y);

            Assert.AreEqual(null,    ship.Externals.Get(gs, 0,0)); // top row
            Assert.AreEqual(null,    ship.Externals.Get(gs, 1,0));
            Assert.AreEqual(At(2,0), ship.Externals.Get(gs, 2,0));
            Assert.AreEqual(At(3,0), ship.Externals.Get(gs, 3,0));
            Assert.AreEqual(null,    ship.Externals.Get(gs, 4,0));
            Assert.AreEqual(null,    ship.Externals.Get(gs, 5,0));

            Assert.AreEqual(null,    ship.Externals.Get(gs, 0,1)); // second row
            Assert.AreEqual(At(1,1), ship.Externals.Get(gs, 1,1), "must be external - NW,N,W empty");

            //ship.Externals.UpdateSlotsUnderModule(gs, At(2,1));
            Assert.AreEqual(At(2,1), ship.Externals.Get(gs, 2,1), "must be external - NW empty");
            Assert.AreEqual(At(3,1), ship.Externals.Get(gs, 3,1), "must be external - NE empty");
            Assert.AreEqual(At(4,1), ship.Externals.Get(gs, 4,1), "must be external - NE,N,E empty");
            Assert.AreEqual(null,    ship.Externals.Get(gs, 5,1));

            // edges of the front section
            Assert.AreEqual(At(0,2), ship.Externals.Get(gs, 0,2));
            Assert.AreEqual(At(0,3), ship.Externals.Get(gs, 0,3));
            Assert.AreEqual(At(0,4), ship.Externals.Get(gs, 0,4));
            Assert.AreEqual(At(5,2), ship.Externals.Get(gs, 5,2));
            Assert.AreEqual(At(5,3), ship.Externals.Get(gs, 5,3));
            Assert.AreEqual(At(5,4), ship.Externals.Get(gs, 5,4));

            // inner corners of the front section should be external
            // because 1 neighboring tile is empty
            Assert.AreEqual(At(1,2), ship.Externals.Get(gs, 1,2));
            Assert.AreEqual(At(1,3), ship.Externals.Get(gs, 1,3)); // this is a 1x2 module
            Assert.AreEqual(At(1,4), ship.Externals.Get(gs, 1,4)); // this is a 1x2 module

            Assert.AreEqual(At(4,2), ship.Externals.Get(gs, 4,2));
            Assert.AreEqual(At(4,4), ship.Externals.Get(gs, 4,4)); // this is a 1x2 module
            Assert.AreEqual(At(4,3), ship.Externals.Get(gs, 4,3)); // this is a 1x2 module

            Assert.AreEqual(49, ship.Externals.NumModules);

            // 13 |E1x1|E1x2 1x1  1x1 |E1x2|E1x1
            // 14 |E1x1 1x1  2x2  2x2  1x1 |E1x1
            // 15 |E1x1 1x1  2x2  2x2  1x1 |E1x1
            // 16 |E1x1|E1x1 1x1  1x1 |E1x1|E1x1
            // 17 |____|E1x1|E1x1|E1x1|E1x1|____

            // kill a few engine modules, which should trigger an update to external slots
            ship.GetModuleAt(2, 17).SetHealth(0, "Test"); // this is a 1x1 engine slot
            // killing that module should count -1 slot and expose +2 1x1 modules
            Assert.AreEqual(49-1+2, ship.Externals.NumModules);

            // and if we resurrect the module, it should go back to previous value
            ship.GetModuleAt(2, 17).SetHealth(100, "Test");
            Assert.AreEqual(49, ship.Externals.NumModules);

            // kill two 1x1 modules, exposing the 2x2 reactor
            ship.GetModuleAt(3, 17).SetHealth(0, "Test");
            ship.GetModuleAt(3, 16).SetHealth(0, "Test");
            Assert.AreEqual(At(3,15), ship.Externals.Get(gs, 3,15));
            // lose one, but gain 3 externals
            Assert.AreEqual(49-1+3, ship.Externals.NumModules);
        }
    }
}
