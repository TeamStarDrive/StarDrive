using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Point = SDGraphics.Point;

namespace UnitTests.Ships
{
    [TestClass]
    public class ModuleGridFlyweightTests : StarDriveTest
    {
        public ModuleGridFlyweightTests()
        {
            CreateUniverseAndPlayerEmpire();
            LoadStarterShips("TEST_Vulcan Scout",
                             "TEST_Heavy Carrier mk1",
                             "TEST_Type VII mk1-c");
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
            AssertEqual(null,       grid[modules, 0, 0]);
            AssertEqual(modules[0], grid[modules, 1, 0]);
            AssertEqual(modules[3], grid[modules, 5, 0]);
            AssertEqual(null,       grid.Get(modules, 0, 0));
            AssertEqual(modules[0], grid.Get(modules, 1, 0));
            AssertEqual(modules[3], grid.Get(modules, 5, 0));
            AssertEqual(null,       grid.Get(modules, new Point(0, 0)));
            AssertEqual(modules[0], grid.Get(modules, new Point(1, 0)));
            AssertEqual(modules[3], grid.Get(modules, new Point(5, 0)));
            AssertEqual(null,       grid.Get(modules, gridIndex:0));
            AssertEqual(modules[0], grid.Get(modules, gridIndex:1));
            AssertEqual(modules[3], grid.Get(modules, gridIndex:5));
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
            AssertEqual(null,       gs[0, 0]);
            AssertEqual(modules[0], gs[1, 0]);
            AssertEqual(modules[3], gs[5, 0]);
            AssertEqual(null,       gs.Get(0, 0));
            AssertEqual(modules[0], gs.Get(1, 0));
            AssertEqual(modules[3], gs.Get(5, 0));
            AssertEqual(null,       gs.Get(new Point(0, 0)));
            AssertEqual(modules[0], gs.Get(new Point(1, 0)));
            AssertEqual(modules[3], gs.Get(new Point(5, 0)));
            AssertEqual(null,       gs.Get(gridIndex:0));
            AssertEqual(modules[0], gs.Get(gridIndex:1));
            AssertEqual(modules[3], gs.Get(gridIndex:5));
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

            AssertEqual(4, gridInfo.SurfaceArea);
            AssertEqual(new Point(3, 2), gridInfo.Size);

            AssertEqual(4, grid.SurfaceArea);
            AssertEqual(new Point(3, 2), new Point(grid.Width, grid.Height));
            AssertEqual(new Vector2(16f, 16f), grid.GridLocalCenter);
            AssertEqual(0.01f, 28.84f, grid.Radius);
            AssertEqual(1, grid.NumInternalSlots);
            
            Ship dummy = SpawnShip("TEST_Vulcan Scout", Player, Vector2.Zero);
            ShipModule[] modules = design.Select(slot => ShipModule.Create(null, slot, dummy, false));

            AssertEqual(null,       grid[modules, 0, 0]); // line0
            AssertEqual(modules[0], grid[modules, 1, 0]);
            AssertEqual(null,       grid[modules, 2, 0]);
            AssertEqual(modules[1], grid[modules, 0, 1]); // line1
            AssertEqual(modules[2], grid[modules, 1, 1]);
            AssertEqual(modules[3], grid[modules, 2, 1]);
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
            AssertEqual(10, modules.Length);

            // TEST Vulcan Scout has 1x2 cannons in the front, so modules 0-1 are duplicated
            // ___|IO_|IO_|___   line0 modules 0-1
            // ___|I__|I__|___   line1 modules 0-1
            // IO_|I__|I__|IO_   line2 modules 2-5
            // IO_|E__|E__|IO_   line3 modules 6-9
            ModuleGridFlyweight grid = ship.ShipData.Grid;
            var gridInfo = ship.ShipData.GridInfo;
            AssertEqual(12, gridInfo.SurfaceArea);
            AssertEqual(new Point(4, 4), gridInfo.Size);

            AssertEqual(12, grid.SurfaceArea);
            AssertEqual(new Point(4, 4), new Point(grid.Width, grid.Height));
            AssertEqual(new Vector2(32f, 32f), grid.GridLocalCenter);
            AssertEqual(0.01f, 45.25f, grid.Radius);
            AssertEqual(9, grid.NumInternalSlots);

            // check all 16 slots
            AssertEqual(null,       grid[modules, 0, 0]); // line0
            AssertEqual(modules[0], grid[modules, 1, 0]);
            AssertEqual(modules[1], grid[modules, 2, 0]);
            AssertEqual(null,       grid[modules, 3, 0]);

            AssertEqual(null,       grid[modules, 0, 1]); // line1
            AssertEqual(modules[0], grid[modules, 1, 1]);
            AssertEqual(modules[1], grid[modules, 2, 1]);
            AssertEqual(null,       grid[modules, 3, 1]);

            AssertEqual(modules[2], grid[modules, 0, 2]); // line2
            AssertEqual(modules[3], grid[modules, 1, 2]);
            AssertEqual(modules[4], grid[modules, 2, 2]);
            AssertEqual(modules[5], grid[modules, 3, 2]);

            AssertEqual(modules[6], grid[modules, 0, 3]); // line3
            AssertEqual(modules[7], grid[modules, 1, 3]);
            AssertEqual(modules[8], grid[modules, 2, 3]);
            AssertEqual(modules[9], grid[modules, 3, 3]);

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

            ShipModule[] shields = ship.GetShields().ToArr();
            ShipModule[] amplifiers = ship.GetAmplifiers().ToArr();
            AssertEqual(2, shields.Length);
            AssertEqual(52, amplifiers.Length);
        }

        [TestMethod]
        public void GetActiveShield()
        {
            // A long Vulfar frigate with 2 shields, 10x26 grid
            // Only front 2x2 armor module is uncovered by shield
            // All other modules are covered
            // A few modules in the center have dual coverage
            Ship ship = SpawnShip("TEST_Type VII mk1-c", Player, Vector2.Zero);
            AssertEqual(2, ship.GetShields().Count());
            ModuleGridFlyweight grid = ship.Grid;

            // get the shields
            ShipModule shield1 = ship.GetModuleAt(4,8);
            ShipModule shield2 = ship.GetModuleAt(4,19);
            Assert.IsTrue(shield1.ShieldsAreActive);
            Assert.IsTrue(shield2.ShieldsAreActive);

            // BottomLeft of front 2x2 armor
            AssertEqual(null, ship.GetActiveShieldAt(4, 1));

            // TopLeft of front 2x2 ordnance
            AssertEqual(shield1, ship.GetActiveShieldAt(4, 2));
            // Random PlastSteel 1x1 module
            AssertEqual(shield1, ship.GetActiveShieldAt(3, 8));

            // 2x2 AdvancedKineticTurret is covered by both shields
            AssertEqual(2, grid.GetNumShieldsAt(3, 13));

            // 2x2 NuclearReactorMed should also be covered by both shields
            AssertEqual(2, grid.GetNumShieldsAt(4, 15));
            AssertEqual(shield1, ship.GetActiveShieldAt(4, 15));

            // if first shield goes down, the second shield should be chosen
            shield1.Active = false;
            AssertEqual(shield2, ship.GetActiveShieldAt(4, 15));
            shield1.Active = true;

            // make sure the shields themselves are covered
            AssertEqual(shield1, ship.GetActiveShieldAt(4, 8));
            AssertEqual(shield2, ship.GetActiveShieldAt(4, 19));

            // a point inside the grid which is not covered by shields
            AssertEqual(null, ship.GetActiveShieldAt(0,0));
            AssertEqual(null, ship.GetActiveShieldAt(7,1));

            // a point inside the grid which is covered by shield
            // BUT does not contain any valid modules
            AssertEqual(shield1, ship.GetActiveShieldAt(0,8));
            AssertEqual(shield1, ship.GetActiveShieldAt(9,8));
            AssertEqual(shield2, ship.GetActiveShieldAt(0,16));
            AssertEqual(shield2, ship.GetActiveShieldAt(9,16));
        }

        [TestMethod]
        public void HitTestShieldsReturnsFirstActiveShield()
        {
            // A long Vulfar frigate with 2 shields, 10x26 grid
            Vector2 c = Vector2.Zero;
            Ship ship = SpawnShip("TEST_Type VII mk1-c", Player, c);
            AssertEqual(2, ship.GetShields().Count());

            // get the shields
            ShipModule shield1 = ship.GetModuleAt(4,8);
            ShipModule shield2 = ship.GetModuleAt(4,19);
            Assert.IsTrue(shield1.ShieldsAreActive);
            Assert.IsTrue(shield2.ShieldsAreActive);

            ShipModule HitTestShields(int x, int y)
                => ship.HitTestShields(ship.GridLocalPointToWorld(new Point(x,y)), 8f);

            AssertEqual(null, HitTestShields(0,0)); // nothing
            AssertEqual(null, HitTestShields(9,0)); // nothing

            AssertEqual(null, HitTestShields(4,0)); // unshielded 2x2 armor
            AssertEqual(null, HitTestShields(4,1)); // unshielded 2x2 armor
            AssertEqual(null, HitTestShields(5,1)); // unshielded 2x2 armor

            AssertEqual(shield1, HitTestShields(4,2)); // shielded 2x2 ordnance
            AssertEqual(shield1, HitTestShields(0, 6)); // shielded empty slot
            AssertEqual(shield1, HitTestShields(9, 4)); // shielded empty slot

            AssertEqual(shield1, HitTestShields(4, 14)); // two shields overlapping 2x2 turret
            shield1.Active = false;
            AssertEqual(shield2, HitTestShields(4, 14)); // two shields overlapping 2x2 turret
            shield1.Active = true;

            AssertEqual(shield1, HitTestShields(4, 15)); // shielded 2x2 reactor
            AssertEqual(shield2, HitTestShields(4, 16)); // shielded 2x2 reactor
            
            AssertEqual(shield2, HitTestShields(0, 19)); // shielded empty slot
            AssertEqual(shield2, HitTestShields(9, 25)); // shielded empty slot

            AssertEqual(shield2, HitTestShields(0, 20)); // shielded 1x1 armor
            AssertEqual(shield2, HitTestShields(8, 20)); // shielded 1x1 ordnance
            AssertEqual(shield2, HitTestShields(4, 25)); // shielded 2x2 engine
            AssertEqual(shield2, HitTestShields(5, 20)); // shielded 2x2 shield (self)
        }
    }
}
