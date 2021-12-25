using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;

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
            Assert.AreEqual(10, ship.InternalSlotCount);
            Assert.AreEqual(1, ship.HealthPercent);
            Assert.AreEqual(ship.Health, ship.HealthMax);
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
            Assert.AreEqual(18, ship.Modules.Count(m => m.Active));
            Assert.AreEqual(10, ship.Modules.Count(m => m.HasInternalRestrictions));

            // Internal slots are counted using Module SurfaceArea
            // This way big 3x3 modules give 9 points, instead of mere 1
            Assert.AreEqual(26, ship.SurfaceArea);
            Assert.AreEqual(18, ship.InternalSlotCount);
            Assert.AreEqual(18, ship.ActiveInternalSlotCount);
            Assert.AreEqual(1, ship.HealthPercent);
            Assert.AreEqual(1, ship.InternalSlotsHealthPercent);
            
            // kill half of internal modules
            var killed = KillModules(7, ship, m => m.HasInternalRestrictions);
            int surfaceKilled = killed.Sum(m => m.Area);
            Assert.AreEqual(12, surfaceKilled);

            Assert.AreEqual(18-7, ship.Modules.Count(m => m.Active));
            Assert.AreEqual(18, ship.InternalSlotCount);
            Assert.AreEqual(6, ship.ActiveInternalSlotCount);
            Assert.AreEqual((1f - surfaceKilled/18f), ship.InternalSlotsHealthPercent);
        }

        [TestMethod]
        public void ShipDiesIfInternalModulesDestroyed()
        {
            Ship ship = SpawnShip("Colony Ship", Player, Vector2.Zero);
            Assert.AreEqual(10, ship.Modules.Count(m => m.HasInternalRestrictions));
            Assert.AreEqual(1, ship.InternalSlotsHealthPercent);

            int slotsToDestroy = (int)Math.Ceiling(ShipResupply.ShipDestroyThreshold * ship.InternalSlotCount) + 1;
            int destroyed = 0;
            foreach (ShipModule m in ship.Modules)
            {
                m.Die(null, cleanupOnly: true);
                destroyed += m.Area;
                if (destroyed >= slotsToDestroy)
                    break;
            }

            Assert.That.LessThan(ship.InternalSlotsHealthPercent, ShipResupply.ShipDestroyThreshold);
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

            Assert.AreEqual(0f, ship.InternalSlotsHealthPercent, "internal slots health must be 0");
            Assert.AreEqual(0f, ship.Health);
            Assert.AreEqual(0f, ship.HealthPercent);
            Assert.IsFalse(ship.Active, "ship should be dead after all modules killed");
        }
    }
}
