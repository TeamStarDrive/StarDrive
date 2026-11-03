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
            AssertEqual(4, ship.NumInternalSlots, "the hull's four I slots");
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
            AssertEqual(18, ship.Modules.Count(m => m.Active));

            // internal slots are the hull's I slots that hold a module, so a 2x2 reactor on four of them gives 4
            AssertEqual(26, ship.SurfaceArea);
            AssertEqual(8, ship.NumInternalSlots);
            AssertEqual(8, ship.ActiveInternalModuleSlots);
            AssertEqual(1, ship.HealthPercent);
            AssertEqual(1, ship.InternalSlotsHealthPercent);

            // the first two modules on I slots, a structure and the cockpit
            var killed = KillModules(2, ship, m => ship.Grid.InternalSlotsOf(m) > 0);
            AssertEqual(2, killed.Sum(m => ship.Grid.InternalSlotsOf(m)));

            AssertEqual(18-2, ship.Modules.Count(m => m.Active));
            AssertEqual(8, ship.NumInternalSlots);
            AssertEqual(6, ship.ActiveInternalModuleSlots);
            AssertEqual(0.75f, ship.InternalSlotsHealthPercent);
        }

        [TestMethod]
        public void OnlyModulesOnISlotsCountTowardIntegrity()
        {
            Ship ship = SpawnShip("Colony Ship", Player, Vector2.Zero);
            ShipModule colonyModule = Array.Find(ship.Modules, m => m.UID == "ColonyModule");
            ShipModule conduit = Array.Find(ship.Modules, m => m.UID == "PowerConduit");
            AssertEqual(0, ship.Grid.InternalSlotsOf(colonyModule), "test setup: an IO module on IO slots");
            AssertEqual(1, ship.Grid.InternalSlotsOf(conduit), "test setup: a conduit on an I slot");

            colonyModule.Die(null, cleanupOnly: true);
            AssertEqual(8, ship.ActiveInternalModuleSlots, "a module on IO slots takes nothing off");
            conduit.Die(null, cleanupOnly: true);
            AssertEqual(7, ship.ActiveInternalModuleSlots, "any module on an I slot counts");

            conduit.SetHealth(conduit.ActualMaxHealth, null);
            AssertEqual(8, ship.ActiveInternalModuleSlots, "and counts again once it is back");
        }

        [TestMethod]
        public void ShipDiesIfInternalModulesDestroyed()
        {
            Ship ship = SpawnShip("Colony Ship", Player, Vector2.Zero);
            AssertEqual(8, ship.NumInternalSlots);
            AssertEqual(1, ship.InternalSlotsHealthPercent);

            int slotsToDestroy = (int)Math.Ceiling(GlobalStats.Defaults.ShipDestroyThreshold * ship.NumInternalSlots) + 1;
            int destroyed = 0;
            foreach (ShipModule m in ship.Modules)
            {
                m.Die(null, cleanupOnly: true);
                destroyed += ship.Grid.InternalSlotsOf(m);
                if (destroyed >= slotsToDestroy)
                    break;
            }

            AssertLessThan(ship.InternalSlotsHealthPercent, GlobalStats.Defaults.ShipDestroyThreshold);
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
