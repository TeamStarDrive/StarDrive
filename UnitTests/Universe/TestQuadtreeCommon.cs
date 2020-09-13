using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;
using Parallel = Ship_Game.Parallel;

namespace UnitTests.Universe
{
    public class TestQuadtreeCommon : StarDriveTest
    {
        protected static bool EnableVisualization = false;
        protected Array<Ship> AllShips = new Array<Ship>();

        protected TestQuadtreeCommon()
        {
            CreateGameInstance(800, 800, mockInput:false);
            LoadStarterShips();
            CreateUniverseAndPlayerEmpire(out Empire _);
        }

        protected IQuadtree CreateQuadTree(int numShips, IQuadtree tree)
        {
            var test = QuadtreePerfTests.CreateTestSpace(numShips, tree,
                                                         Player, Enemy, SpawnShip);
            AllShips = test.Ships;
            return test.Tree;
        }

        protected void DebugVisualize(IQuadtree tree)
        {
            var vis = new QuadTreeVisualization(AllShips, tree);
            Game.ShowAndRun(screen: vis);
        }

        protected GameplayObject[] FindNearby(IQuadtree tree, GameObjectType type, Vector2 pos, float r)
        {
            return tree.FindNearby(type, pos, r, 128, null, null, null);
        }

        protected void CheckSingleFindNearBy(IQuadtree tree, Ship s)
        {
            var offset = new Vector2(0, 256);
            GameplayObject[] found1 = FindNearby(tree, GameObjectType.Any, s.Position+offset, 256);
            Assert.AreEqual(1, found1.Length, "FindNearby exact 256 must return match");

            GameplayObject[] found2 = FindNearby(tree, GameObjectType.Any, s.Position+offset, (256-s.Radius)+0.001f);
            Assert.AreEqual(1, found2.Length, "FindNearby touching radius must return match");
            
            GameplayObject[] found3 = FindNearby(tree, GameObjectType.Any, s.Position+offset, 255-s.Radius);
            Assert.AreEqual(0, found3.Length, "FindNearby outside radius must not match");
        }

        public void TestBasicInsert(IQuadtree tree)
        {
            CreateQuadTree(100, tree);
            Assert.AreEqual(AllShips.Count, tree.Count);

            foreach (Ship ship in AllShips)
            {
                GameplayObject[] ships = FindNearby(tree, GameObjectType.Ship, ship.Position, ship.Radius);
                Assert.AreEqual(1, ships.Length);
                Assert.AreEqual(ship, ships[0]);
            }

            if (EnableVisualization)
                DebugVisualize(tree);
        }

        public void TestFindNearbySingle(IQuadtree tree)
        {
            CreateQuadTree(1, tree);
            CheckSingleFindNearBy(tree, AllShips.First);
        }

        public void TestFindNearbyMulti(IQuadtree tree)
        {
            CreateQuadTree(100, tree);
            
            GameplayObject[] f1 = FindNearby(tree, GameObjectType.Any, Vector2.Zero, 1000);
            Assert.AreEqual(4, f1.Length, "FindNearby center 1000 must match 4");

            GameplayObject[] f2 = FindNearby(tree, GameObjectType.Any, Vector2.Zero, 2000);
            Assert.AreEqual(12, f2.Length, "FindNearby center 2000 must match 12");

            GameplayObject[] f3 = FindNearby(tree, GameObjectType.Any, Vector2.Zero, 3000);
            Assert.AreEqual(32, f3.Length, "FindNearby center 3000 must match 32");
        }

        public void TestTreeUpdatePerformance(IQuadtree tree)
        {
            CreateQuadTree(10_000, tree);
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

        public void TestTreeSearchPerformance(IQuadtree tree)
        {
            CreateQuadTree(10_000, tree);
            const float defaultSensorRange = 30000f;

            var t1 = new PerfTimer();
            for (int i = 0; i < AllShips.Count; ++i)
            {
                Ship ship = AllShips[i];
                tree.FindLinear(GameObjectType.Any, ship.Center, defaultSensorRange, 128, null, null, null);
            }
            float e1 = t1.Elapsed;
            Console.WriteLine($"-- LinearSearch 10k ships, 30k sensor elapsed: {(e1*1000).String(2)}ms");

            var t2 = new PerfTimer();
            for (int i = 0; i < AllShips.Count; ++i)
            {
                tree.FindNearby(GameObjectType.Any, AllShips[i].Center, defaultSensorRange, 128, null, null, null);
            }
            float e2 = t2.Elapsed;
            Console.WriteLine($"-- TreeSearch 10k ships, 30k sensor elapsed: {(e2*1000).String(2)}ms");

            float speedup = e1 / e2;
            Assert.IsTrue(speedup > 1.2f, "TreeSearch must be significantly faster than linear search!");
            Console.WriteLine($"-- TreeSearch is {speedup.String(2)}x faster than LinearSearch");

            if (EnableVisualization)
                DebugVisualize(tree);
        }

        public void TestConcurrentUpdateAndSearch(IQuadtree tree)
        {
            CreateQuadTree(10_000, tree);
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
                    tree.CollideAll(TestSimStep);
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
                        tree.FindNearby(GameObjectType.Any, AllShips[i].Center, defaultSensorRange, 
                                        maxResults:128, null, null, null);
                    }
                }
            });
        }

        public void TestTreeCollisionPerformance(IQuadtree tree)
        {
            CreateQuadTree(10_000, tree);
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
