using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    [TestClass]
    public class ShipPowerCalcTests : StarDriveTest
    {
        public ShipPowerCalcTests()
        {
            // Excalibur class has all the bells and whistles
            LoadStarterShips("Heavy Carrier mk5-b",
                             "Fang Strafer");
            CreateUniverseAndPlayerEmpire();
        }

        static void AssertAllModulesPowered(IEnumerable<TestShip> ships)
        {
            foreach (TestShip ship in ships)
                foreach (ShipModule m in ship.Modules)
                    if (!m.Powered)
                        Assert.Fail($"Module Not Powered! Ship={ship.Name} Module={m}");
        }

        TestShip[] CreateShips(params string[] names)
        {
            var ships = new Array<TestShip>();
            foreach (string name in names)
                ships.Add(SpawnShip(name, Player, new Vector2(12123, -23222)));
            return ships.ToArray();
        }

        [TestMethod]
        public void StarterShipsPowered()
        {
            TestShip[] ships = CreateShips("Vulcan Scout",
                        "Colony Ship", "Small Transport", "Supply Shuttle",
                        "Subspace Projector", "Terran-Prototype");
            RunObjectsSim(TestSimStep);
            AssertAllModulesPowered(ships);
        }
        
        [TestMethod]
        public void LargeShipsPowered()
        {
            TestShip[] ships = CreateShips("Heavy Carrier mk5-b", "Fang Strafer");
            RunObjectsSim(TestSimStep);
            AssertAllModulesPowered(ships);
        }

        // ShipModule.ActualPowerFlowMax already multiplies by EmpireHullBonuses.PowerFlowMod, and
        // Power.Calculate sums that into Ship.PowerFlowMax, which is what the design screen shows.
        // UpdatePower used to apply data.PowerFlowMod a SECOND time, so a ship recharged at
        // (1+mod)^2 while its own design screen said (1+mod).
        [TestMethod]
        public void ReactorTechBonusIsAppliedOnlyOnce()
        {
            Player.data.PowerFlowMod = 0.5f;
            EmpireHullBonuses.RefreshBonuses(Player);

            TestShip ship = SpawnShip("Heavy Carrier mk5-b", Player, new Vector2(7000, 7000));
            ship.UpdateModulePositions(TestSimStep, forceUpdate: true);

            float flow = ship.PowerFlowMax;
            AssertGreaterThan(flow, 0f, "test needs a ship with reactors");

            ship.PowerCurrent = 0f;
            ship.Update(TestSimStep);

            float expected = (flow - ship.PowerDraw) * TestSimStep.FixedTime;
            AssertEqual(0.01f, expected.LowerBound(0), ship.PowerCurrent,
                "recharge must use the already-boosted PowerFlowMax exactly once");
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void PowerGridPerformanceTest()
        {
            TestShip ship = SpawnShip("Heavy Carrier mk5-b", Player, new Vector2(12213,123123));

            const int iterations = 1000;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < iterations; ++i)
            {
                ship.RecalculatePower();
            }

            double elapsed = sw.Elapsed.TotalMilliseconds;
            Log.Write($"RecalculatePower {iterations}x elapsed:{elapsed:G5}ms  avg:{elapsed/iterations:G5}ms modules:{ship.Modules.Length}");
        }
    }
}
