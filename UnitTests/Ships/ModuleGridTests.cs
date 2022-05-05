using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Point = Microsoft.Xna.Framework.Point;

namespace UnitTests.Ships
{
    [TestClass]
    public class ModuleGridTests : StarDriveTest
    {
        public ModuleGridTests()
        {
            CreateUniverseAndPlayerEmpire();
        }

        [TestMethod]
        public void Simple3x2ModuleSlotGrid()
        {
            // ___|O__|___
            // IO_|E__|IO_
            var design = new []
            {
                new HullSlot(1, 0, Restrictions.O ),
                new HullSlot(0, 1, Restrictions.IO),
                new HullSlot(1, 1, Restrictions.E ),
                new HullSlot(2, 1, Restrictions.IO),
            };

            var gridInfo = new ShipGridInfo(design);
            Assert.AreEqual(new Point(3, 2), gridInfo.Size);
            Assert.AreEqual(4, gridInfo.SurfaceArea);

            var grid = new ModuleGrid<HullSlot>(gridInfo, design);
            Assert.AreEqual(3, grid.Width);
            Assert.AreEqual(2, grid.Height);

            Assert.AreEqual(null,      grid[0, 0]);
            Assert.AreEqual(design[0], grid[1, 0]);
            Assert.AreEqual(null,      grid[2, 0]);
            Assert.AreEqual(design[1], grid[0, 1]);
            Assert.AreEqual(design[2], grid[1, 1]);
            Assert.AreEqual(design[3], grid[2, 1]);
            Assert.IsFalse(grid.Get(new Point(0, 0), out _));
            Assert.IsTrue( grid.Get(new Point(1, 0), out _));
            Assert.IsFalse(grid.Get(new Point(2, 0), out _));
            Assert.IsTrue( grid.Get(new Point(0, 1), out _));
            Assert.IsTrue( grid.Get(new Point(1, 1), out _));
            Assert.IsTrue( grid.Get(new Point(2, 1), out _));

            // all coordinates outside of the grid
            Assert.IsFalse(grid.Get(new Point(-1,  0), out _)); // out left, in top
            Assert.IsFalse(grid.Get(new Point( 0, -1), out _)); // in left, out top
            Assert.IsFalse(grid.Get(new Point(-1, -1), out _)); // out left, out top
            Assert.IsFalse(grid.Get(new Point(3, 1), out _)); // out right, in bottom
            Assert.IsFalse(grid.Get(new Point(2, 2), out _)); // in right, out bottom
            Assert.IsFalse(grid.Get(new Point(3, 2), out _)); // out right, out bottom
        }

        [TestMethod]
        public void Simple3x2ShipModuleGrid()
        {
            DesignSlot MakeDesignSlot(int x, int y, string uid, Restrictions r)
            {
                return new DesignSlot(new Point(x,y), uid, new Point(1,1), 0, ModuleOrientation.Normal, null);
            }

            // ___|O__|___
            // O__|O__|O__
            var design = new []
            {
                MakeDesignSlot(1, 0, "SteelArmorSmall", Restrictions.O),
                MakeDesignSlot(0, 1, "SteelArmorSmall", Restrictions.O),
                MakeDesignSlot(1, 1, "SteelArmorSmall", Restrictions.O),
                MakeDesignSlot(2, 1, "SteelArmorSmall", Restrictions.O),
            };

            Ship ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            ShipModule[] modules = design.Select(slot => ShipModule.Create(null, slot, ship, false));

            var gridInfo = new ShipGridInfo(modules);
            Assert.AreEqual(4, gridInfo.SurfaceArea);
            Assert.AreEqual(new Point(3, 2), gridInfo.Size);

            var grid = new ModuleGrid<ShipModule>(gridInfo, modules);
            Assert.AreEqual(4, gridInfo.SurfaceArea);
            Assert.AreEqual(new Point(3, 2), gridInfo.Size);

            Assert.AreEqual(null,       grid[0, 0]);
            Assert.AreEqual(modules[0], grid[1, 0]);
            Assert.AreEqual(null,       grid[2, 0]);
            Assert.AreEqual(modules[1], grid[0, 1]);
            Assert.AreEqual(modules[2], grid[1, 1]);
            Assert.AreEqual(modules[3], grid[2, 1]);
            Assert.IsFalse(grid.Get(new Point(0, 0), out _));
            Assert.IsTrue( grid.Get(new Point(1, 0), out _));
            Assert.IsFalse(grid.Get(new Point(2, 0), out _));
            Assert.IsTrue( grid.Get(new Point(0, 1), out _));
            Assert.IsTrue( grid.Get(new Point(1, 1), out _));
            Assert.IsTrue( grid.Get(new Point(2, 1), out _));

            // all coordinates outside of the grid
            Assert.IsFalse(grid.Get(new Point(-1,  0), out _)); // out left, in top
            Assert.IsFalse(grid.Get(new Point( 0, -1), out _)); // in left, out top
            Assert.IsFalse(grid.Get(new Point(-1, -1), out _)); // out left, out top
            Assert.IsFalse(grid.Get(new Point(3, 1), out _)); // out right, in bottom
            Assert.IsFalse(grid.Get(new Point(2, 2), out _)); // in right, out bottom
            Assert.IsFalse(grid.Get(new Point(3, 2), out _)); // out right, out bottom
        }
    }
}
