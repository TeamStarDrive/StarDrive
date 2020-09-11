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

        public TestQuadTree()
        {
            CreateGameInstance(800, 800, mockInput:false);
            LoadStarterShips();
            CreateUniverseAndPlayerEmpire(out Empire _);
        }

        IQuadtree CreateQuadTree(int numShips, IQuadtree tree)
        {
            var test = QuadtreePerfTests.CreateTestSpace(numShips, tree,
                                                         Player, Enemy, SpawnShip);
            AllShips = test.Ships;
            return test.Tree;
        }

        void DebugVisualize(IQuadtree tree)
        {
            var vis = new QuadTreeVisualization(AllShips, tree);
            Game.ShowAndRun(screen: vis);
        }

        [TestMethod]
        public void BasicInsert()
        {
            IQuadtree tree = CreateQuadTree(100, new Quadtree(100_000f));
            Assert.AreEqual(AllShips.Count, tree.Count);

            foreach (Ship ship in AllShips)
            {
                GameplayObject[] ships = tree.FindNearby(ship.Position, ship.Radius, 128,
                                                         GameObjectType.Ship, null, null);
                Assert.AreEqual(1, ships.Length);
                Assert.AreEqual(ship, ships[0]);
            }

            if (EnableVisualization)
                DebugVisualize(tree);
        }

        void CheckSingleFindNearBy(IQuadtree tree, Ship s)
        {
            var offset = new Vector2(0, 256);
            GameplayObject[] found1 = tree.FindNearby(s.Position+offset, 256, 128, GameObjectType.Any, null, null);
            Assert.AreEqual(1, found1.Length, "FindNearby exact 256 must return match");

            GameplayObject[] found2 = tree.FindNearby(s.Position+offset, (256-s.Radius)+0.001f, 128, GameObjectType.Any, null, null);
            Assert.AreEqual(1, found2.Length, "FindNearby touching radius must return match");
            
            GameplayObject[] found3 = tree.FindNearby(s.Position+offset, 255-s.Radius, 128, GameObjectType.Any, null, null);
            Assert.AreEqual(0, found3.Length, "FindNearby outside radius must not match");
        }

        [TestMethod]
        public void FindNearbySingle()
        {
            IQuadtree tree = CreateQuadTree(1, new Quadtree(10_000f));
            CheckSingleFindNearBy(tree, AllShips.First);
        }

        [TestMethod]
        public void FindNearbyMulti()
        {
            IQuadtree tree = CreateQuadTree(100, new Quadtree(10_000f));
            
            GameplayObject[] f1 = tree.FindNearby(Vector2.Zero, 1000, 128, GameObjectType.Any, null, null);
            Assert.AreEqual(4, f1.Length, "FindNearby center 1000 must match 4");

            GameplayObject[] f2 = tree.FindNearby(Vector2.Zero, 2000, 128, GameObjectType.Any, null, null);
            Assert.AreEqual(12, f2.Length, "FindNearby center 2000 must match 12");

            GameplayObject[] f3 = tree.FindNearby(Vector2.Zero, 3000, 128, GameObjectType.Any, null, null);
            Assert.AreEqual(32, f3.Length, "FindNearby center 3000 must match 32");
        }

        [TestMethod]
        public void TreeUpdatePerformance()
        {
            IQuadtree tree = CreateQuadTree(10_000, new Quadtree(1_000_000f));
            float e = 0f;
            for (int i = 0; i < 60; ++i)
            {
                foreach (Ship ship in AllShips)
                {
                    ship.Center.X += 10f;
                    ship.Position = ship.Center;
                    ship.UpdateModulePositions(TestSimStep, true, forceUpdate: true);
                }
                var t = new PerfTimer();
                tree.UpdateAll();
                e += t.Elapsed;
            }
            Console.WriteLine($"-- Tree UpdateAll elapsed: {(e*1000).String(2)}ms");
            
            if (EnableVisualization)
                DebugVisualize(tree);
        }

        [TestMethod]
        public void TreeSearchPerformance()
        {
            IQuadtree tree = CreateQuadTree(10_000, new Quadtree(500_000f));
            const float defaultSensorRange = 30000f;

            var t1 = new PerfTimer();
            for (int i = 0; i < AllShips.Count; ++i)
            {
                Ship ship = AllShips[i];
                tree.FindLinear(ship.Center, defaultSensorRange, 128, GameObjectType.Any, null, null);
            }
            float e1 = t1.Elapsed;
            Console.WriteLine($"-- LinearSearch 10k ships, 30k sensor elapsed: {(e1*1000).String(2)}ms");

            var t2 = new PerfTimer();
            for (int i = 0; i < AllShips.Count; ++i)
            {
                tree.FindNearby(AllShips[i].Center, defaultSensorRange, 128, GameObjectType.Any, null, null);
            }
            float e2 = t2.Elapsed;
            Console.WriteLine($"-- TreeSearch 10k ships, 30k sensor elapsed: {(e2*1000).String(2)}ms");

            float speedup = e1 / e2;
            Assert.IsTrue(speedup > 1.2f, "TreeSearch must be significantly faster than linear search!");
            Console.WriteLine($"-- TreeSearch is {speedup.String(2)}x faster than LinearSearch");
        }

        [TestMethod]
        public void ConcurrentUpdateAndSearch()
        {
            IQuadtree tree = CreateQuadTree(10_000, new Quadtree(500_000f));
            var timer = new PerfTimer();

            // update
            Parallel.Run(() =>
            {
                while (timer.Elapsed < 1.0)
                {
                    foreach (Ship ship in AllShips)
                    {
                        ship.Center.X += 10f;
                        ship.Position = ship.Center;
                        ship.UpdateModulePositions(TestSimStep, true, forceUpdate: true);
                    }
                    tree.UpdateAll();
                }
            });

            // search
            Parallel.Run(() =>
            {
                const float defaultSensorRange = 30000f;
                while (timer.Elapsed < 1.0)
                {
                    for (int i = 0; i < AllShips.Count; ++i)
                    {
                        tree.FindNearby(AllShips[i].Center, defaultSensorRange,
                                        128, GameObjectType.Any, null, null);
                    }
                }
            });
        }

        [TestMethod]
        public void TreeCollisionPerformance()
        {
            IQuadtree tree = CreateQuadTree(10_000, new Quadtree(50_000f));
            const int iterations = 1000;

            var t1 = new PerfTimer();
            for (int i = 0; i < iterations; ++i)
            {
                tree.CollideAll(TestSimStep);
            }
            float e1 = t1.Elapsed;
            Console.WriteLine($"-- CollideAllIterative 10k ships, 30k sensor elapsed: {(e1*1000).String(2)}ms");

            var t2 = new PerfTimer();
            for (int i = 0; i < iterations; ++i)
            {
                tree.CollideAllRecursive(TestSimStep);
            }
            float e2 = t2.Elapsed;
            Console.WriteLine($"-- CollideAllRecursive 10k ships, 30k sensor elapsed: {(e2*1000).String(2)}ms");
        }
    }
}
