using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;

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
            Universe.Objects.Update(TestSimStep);
            AssertAllModulesPowered(ships);
        }
        
        [TestMethod]
        public void LargeShipsPowered()
        {
            TestShip[] ships = CreateShips("Heavy Carrier mk5-b", "Fang Strafer");
            Universe.Objects.Update(TestSimStep);
            AssertAllModulesPowered(ships);
        }

        [TestMethod]
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
