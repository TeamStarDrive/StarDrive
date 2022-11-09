using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using SDUtils;

namespace UnitTests.Ships
{
    [TestClass]
    public class ShipHealthTests : StarDriveTest
    {
        public ShipHealthTests()
        {
            LoadStarterShips("TEST_ShipShield");
            CreateUniverseAndPlayerEmpire();
        }

        [TestMethod]
        public void ShipHealthInit()
        {
            Ship ship = SpawnShip("TEST_ShipShield", Player, Vector2.Zero);
            AssertEqual(10, ship.NumInternalSlots);
            AssertEqual(1, ship.HealthPercent);
            AssertEqual(ship.Health, ship.HealthMax);
        }

        Array<ShipModule> KillModules(int modulesToKill, Ship ship, Func<ShipModule, bool> filter)
        {
            var killed = new Array<ShipModule>();
            foreach (ShipModule m in ship.Modules)
            {
                if (filter(m))
                {
                    m.Damage(null, 10000f);
                    killed.Add(m);
                    if (killed.Count >= modulesToKill)
                        break;
                }
            }
            return killed;
        }

        [TestMethod]
        public void ShipInternalModuleDamage()
        {
            Ship ship = SpawnShip("Colony Ship", Player, Vector2.Zero);
            // colony ship has a total of 18 modules, and 14 with I/IO restrictions
            AssertEqual(18, ship.Modules.Count(m => m.Active));
            AssertEqual(10, ship.Modules.Count(m => m.HasInternalRestrictions));

            // Internal slots are counted using Module SurfaceArea
            // This way big 3x3 modules give 9 points, instead of mere 1
            AssertEqual(26, ship.SurfaceArea);
            AssertEqual(18, ship.NumInternalSlots);
            AssertEqual(18, ship.ActiveInternalModuleSlots);
            AssertEqual(1, ship.HealthPercent);
            AssertEqual(1, ship.InternalSlotsHealthPercent);
            
            // kill half of internal modules
            var killed = KillModules(7, ship, m => m.HasInternalRestrictions);
            int surfaceKilled = killed.Sum(m => m.Area);
            AssertEqual(12, surfaceKilled);

            AssertEqual(18-7, ship.Modules.Count(m => m.Active));
            AssertEqual(18, ship.NumInternalSlots);
            AssertEqual(6, ship.ActiveInternalModuleSlots);
            AssertEqual((1f - surfaceKilled/18f), ship.InternalSlotsHealthPercent);
        }

        [TestMethod]
        public void ShipDiesIfInternalModulesDestroyed()
        {
            Ship ship = SpawnShip("Colony Ship", Player, Vector2.Zero);
            AssertEqual(10, ship.Modules.Count(m => m.HasInternalRestrictions));
            AssertEqual(1, ship.InternalSlotsHealthPercent);

            int slotsToDestroy = (int)Math.Ceiling(ShipResupply.ShipDestroyThreshold * ship.NumInternalSlots) + 1;
            int destroyed = 0;
            foreach (ShipModule m in ship.Modules)
            {
                m.Die(null, cleanupOnly: true);
                destroyed += m.Area;
                if (destroyed >= slotsToDestroy)
                    break;
            }

            AssertLessThan(ship.InternalSlotsHealthPercent, ShipResupply.ShipDestroyThreshold);
            Assert.IsFalse(ship.Active, "ship should be dead after enough internal modules killed");
        }

        [TestMethod]
        public void ShipDiesIfAllModulesDestroyed()
        {
            Ship ship = SpawnShip("Colony Ship", Player, Vector2.Zero);
            foreach (ShipModule m in ship.Modules)
            {
                m.Die(null, cleanupOnly: true);
            }

            AssertEqual(0f, ship.InternalSlotsHealthPercent, "internal slots health must be 0");
            AssertEqual(0f, ship.Health);
            AssertEqual(0f, ship.HealthPercent);
            Assert.IsFalse(ship.Active, "ship should be dead after all modules killed");
        }
    }
}
