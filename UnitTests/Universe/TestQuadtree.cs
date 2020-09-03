using System;
using Microsoft.Xna.Framework;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Ships;

namespace UnitTests.Universe
{
    [TestClass]
    public class TestQuadTree : StarDriveTest
    {
        static bool EnableVisualization = false;

        public Array<Ship> AllShips = new Array<Ship>();
        public float UniverseSize;

        public TestQuadTree()
        {
            CreateGameInstance(800, 800, mockInput:false);
            LoadStarterShips();
            CreateUniverseAndPlayerEmpire(out Empire _);
        }

        Quadtree CreateQuadTree(int numShips, float universeSize)
        {
            var test = QuadtreePerfTests.CreateTestSpace(numShips, universeSize,
                                                         Player, Enemy, SpawnShip);
            UniverseSize = universeSize;
            AllShips = test.Ships;
            return test.Tree;
        }

        void DebugVisualize(Quadtree tree)
        {
            var vis = new QuadTreeVisualization(this, tree);
            Game.ShowAndRun(screen: vis);
        }

        [TestMethod]
        public void BasicInsert()
        {
            Quadtree tree = CreateQuadTree(100, universeSize:100_000f);
            Assert.AreEqual(AllShips.Count, tree.CountItemsSlow());

            foreach (Ship ship in AllShips)
            {
                var ships = tree.FindNearby(ship.Position, ship.Radius, GameObjectType.Ship);
                Assert.AreEqual(1, ships.Length);
                Assert.AreEqual(ship, ships[0]);
            }

            if (EnableVisualization)
                DebugVisualize(tree);
        }

        void CheckSingleFindNearBy(Quadtree tree, Ship s)
        {
            var offset = new Vector2(0, 256);
            GameplayObject[] found1 = tree.FindNearby(s.Position+offset, 256);
            Assert.AreEqual(1, found1.Length, "FindNearby exact 256 must return match");

            GameplayObject[] found2 = tree.FindNearby(s.Position+offset, (256-s.Radius)+0.001f);
            Assert.AreEqual(1, found2.Length, "FindNearby touching radius must return match");
            
            GameplayObject[] found3 = tree.FindNearby(s.Position+offset, 255-s.Radius);
            Assert.AreEqual(0, found3.Length, "FindNearby outside radius must not match");
        }

        [TestMethod]
        public void FindNearbySingle()
        {
            Quadtree tree = CreateQuadTree(1, universeSize:10_000f);
            CheckSingleFindNearBy(tree, AllShips.First);
        }

        [TestMethod]
        public void FindNearbyMulti()
        {
            Quadtree tree = CreateQuadTree(100, universeSize:10_000f);
            
            GameplayObject[] f1 = tree.FindNearby(Vector2.Zero, 1000);
            Assert.AreEqual(4, f1.Length, "FindNearby center 1000 must match 4");

            GameplayObject[] f2 = tree.FindNearby(Vector2.Zero, 2000);
            Assert.AreEqual(12, f2.Length, "FindNearby center 2000 must match 12");

            GameplayObject[] f3 = tree.FindNearby(Vector2.Zero, 3000);
            Assert.AreEqual(32, f3.Length, "FindNearby center 3000 must match 32");
        }

        [TestMethod]
        public void TreeUpdatePerformance()
        {
            Quadtree tree = CreateQuadTree(10000, universeSize:100_000f);
            
            var timer = new PerfTimer();
            for (int i = 0; i < 1000; ++i)
            {
                foreach (Ship ship in AllShips)
                {
                    ship.Center.X += 10f;
                    ship.Position = ship.Center;
                    ship.UpdateModulePositions(TestSimStep, true, forceUpdate: true);
                }
                tree.UpdateAll(TestSimStep);
            }

            Console.WriteLine($"-- TreeUpdatePerf elapsed: {timer.Elapsed}s");

            //DebugVisualize(tree);
        }

        [TestMethod]
        public void TreeSearchPerformance()
        {
            Quadtree tree = CreateQuadTree(10000, universeSize:500_000f);
            const float defaultSensorRange = 30000f;

            var t1 = new PerfTimer();
            for (int i = 0; i < AllShips.Count; ++i)
            {
                Ship ship = AllShips[i];
                QuadtreePerfTests.FindLinearOpt(AllShips, ship, ship.Center, defaultSensorRange);
            }
            float e1 = t1.Elapsed;
            Console.WriteLine($"-- LinearSearch 10k ships, 30k sensor elapsed: {e1.String(2)}s");

            var t2 = new PerfTimer();
            for (int i = 0; i < AllShips.Count; ++i)
            {
                tree.FindNearby(AllShips[i].Center, defaultSensorRange);
            }
            float e2 = t2.Elapsed;
            Console.WriteLine($"-- TreeSearch 10k ships, 30k sensor elapsed: {e2.String(2)}s");

            float speedup = e1 / e2;
            Assert.IsTrue(speedup > 1.2f, "TreeSearch must be significantly faster than linear search!");
            Console.WriteLine($"-- TreeSearch is {speedup.String(2)}x faster than LinearSearch");
        }

        [TestMethod]
        public void ConcurrentUpdateAndSearch()
        {
            var timer = new PerfTimer();

            // update
            Parallel.Run(() =>
            {
                while (timer.Elapsed < 1.0)
                {

                }
            });

            // search
            Parallel.Run(() =>
            {
                while (timer.Elapsed < 1.0)
                {

                }
            });
        }
    }
}
