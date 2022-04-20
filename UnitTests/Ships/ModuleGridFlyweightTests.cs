using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class ModuleGridFlyweightTests : StarDriveTest
    {
        public ModuleGridFlyweightTests()
        {
            CreateUniverseAndPlayerEmpire();
            LoadStarterShips("TEST_Vulcan Scout", "TEST_Heavy Carrier mk1");
        }

        static DesignSlot MakeDesignSlot(int x, int y, string uid)
        {
            return new DesignSlot(new Point(x,y), uid, new Point(1,1), 0, ModuleOrientation.Normal, null);
        }

        [TestMethod]
        public void ValidateAllOverloadsForGetModule()
        {
            // ___|O__|___
            // O__|O__|O__
            var design = new []
            {
                MakeDesignSlot(1, 0, "SteelArmorSmall"),
                MakeDesignSlot(0, 1, "SteelArmorSmall"),
                MakeDesignSlot(1, 1, "SteelArmorSmall"),
                MakeDesignSlot(2, 1, "SteelArmorSmall"),
            };

            var gridInfo = new ShipGridInfo(design);
            var grid = new ModuleGridFlyweight("test", gridInfo, design);

            Ship dummy = SpawnShip("TEST_Vulcan Scout", Player, Vector2.Zero);
            ShipModule[] modules = design.Select(slot => ShipModule.Create(null, slot, dummy, false));

            // COVERAGE: test index out of bounds conditions
            Assert.ThrowsException<IndexOutOfRangeException>(() => grid[modules, -1, 0]);
            Assert.ThrowsException<IndexOutOfRangeException>(() => grid[modules,  0, 3]);
            Assert.ThrowsException<IndexOutOfRangeException>(() => grid.Get(modules, -1, 0));
            Assert.ThrowsException<IndexOutOfRangeException>(() => grid.Get(modules,  0, 3));
            Assert.ThrowsException<IndexOutOfRangeException>(() => grid.Get(modules, new Point(-1, 0)));
            Assert.ThrowsException<IndexOutOfRangeException>(() => grid.Get(modules, new Point(0, 3)));
            Assert.ThrowsException<IndexOutOfRangeException>(() => grid.Get(modules, gridIndex:-1));
            Assert.ThrowsException<IndexOutOfRangeException>(() => grid.Get(modules, gridIndex:6));

            // COVERAGE: test all overloads for Get module
            Assert.AreEqual(null,       grid[modules, 0, 0]);
            Assert.AreEqual(modules[0], grid[modules, 1, 0]);
            Assert.AreEqual(modules[3], grid[modules, 5, 0]);
            Assert.AreEqual(null,       grid.Get(modules, 0, 0));
            Assert.AreEqual(modules[0], grid.Get(modules, 1, 0));
            Assert.AreEqual(modules[3], grid.Get(modules, 5, 0));
            Assert.AreEqual(null,       grid.Get(modules, new Point(0, 0)));
            Assert.AreEqual(modules[0], grid.Get(modules, new Point(1, 0)));
            Assert.AreEqual(modules[3], grid.Get(modules, new Point(5, 0)));
            Assert.AreEqual(null,       grid.Get(modules, gridIndex:0));
            Assert.AreEqual(modules[0], grid.Get(modules, gridIndex:1));
            Assert.AreEqual(modules[3], grid.Get(modules, gridIndex:5));
        }

        [TestMethod]
        public void ValidateModuleGridStateUtil()
        {
            // ___|O__|___
            // O__|O__|O__
            var design = new []
            {
                MakeDesignSlot(1, 0, "SteelArmorSmall"),
                MakeDesignSlot(0, 1, "SteelArmorSmall"),
                MakeDesignSlot(1, 1, "SteelArmorSmall"),
                MakeDesignSlot(2, 1, "SteelArmorSmall"),
            };

            var gridInfo = new ShipGridInfo(design);
            var grid = new ModuleGridFlyweight("test", gridInfo, design);

            Ship dummy = SpawnShip("TEST_Vulcan Scout", Player, Vector2.Zero);
            ShipModule[] modules = design.Select(slot => ShipModule.Create(null, slot, dummy, false));

            // COVERAGE: test index out of bounds conditions
            var gs = new ModuleGridState(grid, modules);
            Assert.ThrowsException<IndexOutOfRangeException>(() => gs[-1, 0]);
            Assert.ThrowsException<IndexOutOfRangeException>(() => gs[ 0, 3]);
            Assert.ThrowsException<IndexOutOfRangeException>(() => gs.Get(-1, 0));
            Assert.ThrowsException<IndexOutOfRangeException>(() => gs.Get( 0, 3));
            Assert.ThrowsException<IndexOutOfRangeException>(() => gs.Get(new Point(-1, 0)));
            Assert.ThrowsException<IndexOutOfRangeException>(() => gs.Get(new Point(0, 3)));
            Assert.ThrowsException<IndexOutOfRangeException>(() => gs.Get(gridIndex:-1));
            Assert.ThrowsException<IndexOutOfRangeException>(() => gs.Get(gridIndex:6));

            // COVERAGE: test all overloads for Get module
            Assert.AreEqual(null,       gs[0, 0]);
            Assert.AreEqual(modules[0], gs[1, 0]);
            Assert.AreEqual(modules[3], gs[5, 0]);
            Assert.AreEqual(null,       gs.Get(0, 0));
            Assert.AreEqual(modules[0], gs.Get(1, 0));
            Assert.AreEqual(modules[3], gs.Get(5, 0));
            Assert.AreEqual(null,       gs.Get(new Point(0, 0)));
            Assert.AreEqual(modules[0], gs.Get(new Point(1, 0)));
            Assert.AreEqual(modules[3], gs.Get(new Point(5, 0)));
            Assert.AreEqual(null,       gs.Get(gridIndex:0));
            Assert.AreEqual(modules[0], gs.Get(gridIndex:1));
            Assert.AreEqual(modules[3], gs.Get(gridIndex:5));
        }

        [TestMethod]
        public void Simple3x2ShipModuleGrid()
        {
            // ___|O__|___
            // O__|O__|O__
            var design = new []
            {
                MakeDesignSlot(1, 0, "SteelArmorSmall"),
                MakeDesignSlot(0, 1, "SteelArmorSmall"),
                MakeDesignSlot(1, 1, "Internal Bulkhead"),
                MakeDesignSlot(2, 1, "SteelArmorSmall"),
            };

            var gridInfo = new ShipGridInfo(design);
            var grid = new ModuleGridFlyweight("test", gridInfo, design);

            Assert.AreEqual(4, gridInfo.SurfaceArea);
            Assert.AreEqual(new Point(3, 2), gridInfo.Size);

            Assert.AreEqual(4, grid.SurfaceArea);
            Assert.AreEqual(new Point(3, 2), new Point(grid.Width, grid.Height));
            Assert.AreEqual(new Vector2(24f, 16f), grid.GridLocalCenter);
            Assert.AreEqual(28.84f, grid.Radius, 0.01f);
            Assert.AreEqual(1, grid.NumInternalSlots);
            
            Ship dummy = SpawnShip("TEST_Vulcan Scout", Player, Vector2.Zero);
            ShipModule[] modules = design.Select(slot => ShipModule.Create(null, slot, dummy, false));

            Assert.AreEqual(null,       grid[modules, 0, 0]); // line0
            Assert.AreEqual(modules[0], grid[modules, 1, 0]);
            Assert.AreEqual(null,       grid[modules, 2, 0]);
            Assert.AreEqual(modules[1], grid[modules, 0, 1]); // line1
            Assert.AreEqual(modules[2], grid[modules, 1, 1]);
            Assert.AreEqual(modules[3], grid[modules, 2, 1]);
            Assert.IsFalse(grid.Get(modules, 0, 0, out _)); // line0
            Assert.IsTrue( grid.Get(modules, 1, 0, out _));
            Assert.IsFalse(grid.Get(modules, 2, 0, out _));
            Assert.IsTrue( grid.Get(modules, 0, 1, out _)); // line1
            Assert.IsTrue( grid.Get(modules, 1, 1, out _));
            Assert.IsTrue( grid.Get(modules, 2, 1, out _));
            
            // all coordinates outside of the grid
            Assert.IsFalse(grid.Get(modules, -1,  0, out _)); // out left, in top
            Assert.IsFalse(grid.Get(modules,  0, -1, out _)); // in left, out top
            Assert.IsFalse(grid.Get(modules, -1, -1, out _)); // out left, out top
            Assert.IsFalse(grid.Get(modules,  3,  1, out _)); // out right, in bottom
            Assert.IsFalse(grid.Get(modules,  2,  2, out _)); // in right, out bottom
            Assert.IsFalse(grid.Get(modules,  3,  2, out _)); // out right, out bottom
        }

        [TestMethod]
        public void VulcanScoutSlots()
        {
            Ship ship = SpawnShip("TEST_Vulcan Scout", Player, Vector2.Zero);
            ShipModule[] modules = ship.Modules;
            Assert.AreEqual(10, modules.Length);

            // TEST Vulcan Scout has 1x2 cannons in the front, so modules 0-1 are duplicated
            // ___|IO_|IO_|___   line0 modules 0-1
            // ___|I__|I__|___   line1 modules 0-1
            // IO_|I__|I__|IO_   line2 modules 2-5
            // IO_|E__|E__|IO_   line3 modules 6-9
            ModuleGridFlyweight grid = ship.ShipData.Grid;
            var gridInfo = ship.ShipData.GridInfo;
            Assert.AreEqual(12, gridInfo.SurfaceArea);
            Assert.AreEqual(new Point(4, 4), gridInfo.Size);

            Assert.AreEqual(12, grid.SurfaceArea);
            Assert.AreEqual(new Point(4, 4), new Point(grid.Width, grid.Height));
            Assert.AreEqual(new Vector2(32f, 32f), grid.GridLocalCenter);
            Assert.AreEqual(45.25f, grid.Radius, 0.01f);
            Assert.AreEqual(9, grid.NumInternalSlots);

            // check all 16 slots
            Assert.AreEqual(null,       grid[modules, 0, 0]); // line0
            Assert.AreEqual(modules[0], grid[modules, 1, 0]);
            Assert.AreEqual(modules[1], grid[modules, 2, 0]);
            Assert.AreEqual(null,       grid[modules, 3, 0]);

            Assert.AreEqual(null,       grid[modules, 0, 1]); // line1
            Assert.AreEqual(modules[0], grid[modules, 1, 1]);
            Assert.AreEqual(modules[1], grid[modules, 2, 1]);
            Assert.AreEqual(null,       grid[modules, 3, 1]);

            Assert.AreEqual(modules[2], grid[modules, 0, 2]); // line2
            Assert.AreEqual(modules[3], grid[modules, 1, 2]);
            Assert.AreEqual(modules[4], grid[modules, 2, 2]);
            Assert.AreEqual(modules[5], grid[modules, 3, 2]);

            Assert.AreEqual(modules[6], grid[modules, 0, 3]); // line3
            Assert.AreEqual(modules[7], grid[modules, 1, 3]);
            Assert.AreEqual(modules[8], grid[modules, 2, 3]);
            Assert.AreEqual(modules[9], grid[modules, 3, 3]);

            // check all 16 slots using the Get() wrapper which allows index out of bounds
            Assert.IsFalse(grid.Get(modules, 0, 0, out _)); // line0
            Assert.IsTrue( grid.Get(modules, 1, 0, out _));
            Assert.IsTrue( grid.Get(modules, 2, 0, out _));
            Assert.IsFalse(grid.Get(modules, 3, 0, out _));

            Assert.IsFalse(grid.Get(modules, 0, 1, out _)); // line1
            Assert.IsTrue( grid.Get(modules, 1, 1, out _));
            Assert.IsTrue( grid.Get(modules, 2, 1, out _));
            Assert.IsFalse(grid.Get(modules, 3, 1, out _));

            Assert.IsTrue( grid.Get(modules, 0, 2, out _)); // line2
            Assert.IsTrue( grid.Get(modules, 1, 2, out _));
            Assert.IsTrue( grid.Get(modules, 2, 2, out _));
            Assert.IsTrue( grid.Get(modules, 3, 2, out _));

            Assert.IsTrue( grid.Get(modules, 0, 3, out _)); // line3
            Assert.IsTrue( grid.Get(modules, 1, 3, out _));
            Assert.IsTrue( grid.Get(modules, 2, 3, out _));
            Assert.IsTrue( grid.Get(modules, 3, 3, out _));

            // all coordinates outside of the grid
            Assert.IsFalse(grid.Get(modules, -1,  0, out _)); // out left, in top
            Assert.IsFalse(grid.Get(modules,  1, -1, out _)); // in left, out top
            Assert.IsFalse(grid.Get(modules, -1, -1, out _)); // out left, out top
            Assert.IsFalse(grid.Get(modules,  4,  3, out _)); // out right, in bottom
            Assert.IsFalse(grid.Get(modules,  3,  4, out _)); // in right, out bottom
            Assert.IsFalse(grid.Get(modules,  4,  4, out _)); // out right, out bottom
        }

        [TestMethod]
        public void ShipWithShieldsAndAmplifiers()
        {
            Ship ship = SpawnShip("TEST_Heavy Carrier mk1", Player, Vector2.Zero);

            ShipModule[] shields = ship.GetShields().ToArray();
            ShipModule[] amplifiers = ship.GetAmplifiers().ToArray();
            Assert.AreEqual(2, shields.Length);
            Assert.AreEqual(52, amplifiers.Length);
        }
    }
}
