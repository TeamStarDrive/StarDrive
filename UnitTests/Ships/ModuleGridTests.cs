using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class ModuleGridTests : StarDriveTest
    {
        public ModuleGridTests()
        {
            CreateGameInstance();
            LoadStarterShips("Vulcan Scout");
            CreateUniverseAndPlayerEmpire(out _);
        }

        [TestMethod]
        public void Simple3x2ModuleSlotGrid()
        {
            var localOrigin = new Vector2(64f);
            Vector2 origin = localOrigin + new Vector2(ShipModule.ModuleSlotOffset);
            // ___|O__|___
            // IO_|E__|IO_
            var design = new []
            {
                new ModuleSlotData(origin + new Vector2(16,0), Restrictions.O),
                new ModuleSlotData(origin + new Vector2(0,16), Restrictions.IO),
                new ModuleSlotData(origin + new Vector2(16,16), Restrictions.E),
                new ModuleSlotData(origin + new Vector2(32,16), Restrictions.IO),
            };

            var gridInfo = new ShipGridInfo(design, isHull:true);
            Assert.AreEqual(4,                     gridInfo.SurfaceArea);
            Assert.AreEqual(localOrigin,           gridInfo.Origin);
            Assert.AreEqual(new Point(3, 2),       gridInfo.Size);
            Assert.AreEqual(new Vector2(48f, 32f), gridInfo.Span);

            var grid = new ModuleGrid<ModuleSlotData>(gridInfo, design);
            Assert.AreEqual(3, grid.Width);
            Assert.AreEqual(2, grid.Height);
            Assert.AreEqual(localOrigin, grid.Origin);

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
            var localOrigin = new Vector2(64f);
            Vector2 origin = localOrigin + new Vector2(ShipModule.ModuleSlotOffset);
            // ___|O__|___
            // O__|O__|O__
            var design = new []
            {
                new ModuleSlotData(origin + new Vector2(16, 0), Restrictions.O) { ModuleUID = "SteelArmorSmall" },
                new ModuleSlotData(origin + new Vector2(0, 16), Restrictions.O) { ModuleUID = "SteelArmorSmall" },
                new ModuleSlotData(origin + new Vector2(16,16), Restrictions.O) { ModuleUID = "SteelArmorSmall" },
                new ModuleSlotData(origin + new Vector2(32,16), Restrictions.O) { ModuleUID = "SteelArmorSmall" },
            };

            Ship ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
            ShipModule[] modules = design.Select(slot => ShipModule.Create(slot, ship, false, false));

            var gridInfo = new ShipGridInfo(modules);
            Assert.AreEqual(localOrigin, gridInfo.Origin);
            Assert.AreEqual(4, gridInfo.SurfaceArea);
            Assert.AreEqual(new Vector2(48f, 32f), gridInfo.Span);
            Assert.AreEqual(new Point(3, 2), gridInfo.Size);

            var grid = new ModuleGrid<ShipModule>(gridInfo, modules);
            Assert.AreEqual(4,                     gridInfo.SurfaceArea);
            Assert.AreEqual(localOrigin,           gridInfo.Origin);
            Assert.AreEqual(new Point(3, 2),       gridInfo.Size);
            Assert.AreEqual(new Vector2(48f, 32f), gridInfo.Span);

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
