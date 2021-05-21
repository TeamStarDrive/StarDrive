using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using Parallel = Ship_Game.Parallel;

namespace UnitTests.Universe
{
    public class TestSpatialCommon : StarDriveTest
    {
        protected static bool EnableVisualization = false;
        protected static bool EnableMovingShips = true;
        //protected Array<Ship> AllShips = new Array<Ship>();
        protected Array<GameplayObject> AllObjects = new Array<GameplayObject>();

        protected TestSpatialCommon()
        {
            CreateGameInstance(800, 800, mockInput:false);
            LoadStarterShips();
            CreateUniverseAndPlayerEmpire(out Empire _);
        }

        protected void CreateQuadTree(int numShips, ISpatial tree)
        {
            if (!AllObjects.IsEmpty)
            {
                Universe.Objects.Clear();
                AllObjects.Clear();
            }
            AllObjects = QtreePerfTests.CreateTestSpace(numShips, tree, Player, Enemy, SpawnShip);
        }

        protected void DebugVisualize(ISpatial tree, bool enableMovingShips = true)
        {
            bool moving = enableMovingShips && EnableMovingShips;
            var vis = new SpatialVisualization(AllObjects, tree, moving);
            Game.ShowAndRun(screen: vis);
        }

        protected GameplayObject[] FindNearby(ISpatial tree, GameObjectType type, Vector2 pos, float r)
        {
            var opt = new SearchOptions(pos, r, type);
            opt.MaxResults = 128;
            return tree.FindNearby(opt);
        }

        public void TestBasicInsert(ISpatial tree)
        {
            CreateQuadTree(100, tree);
            Assert.AreEqual(AllObjects.Count, tree.Count);

            foreach (GameplayObject go in AllObjects)
            {
                GameplayObject[] ships = FindNearby(tree, GameObjectType.Ship, go.Position, go.Radius);
                Assert.AreEqual(1, ships.Length);
                Assert.AreEqual(go, ships[0]);
            }

            if (EnableVisualization)
                DebugVisualize(tree);
        }

        public void TestFindNearbySingle(ISpatial tree)
        {
            CreateQuadTree(1, tree);

            Ship s = (Ship)AllObjects.First;
            var offset = new Vector2(0, 256);
            GameplayObject[] found1 = FindNearby(tree, GameObjectType.Any, s.Position+offset, 256);
            Assert.AreEqual(1, found1.Length, "FindNearby exact 256 must return match");

            GameplayObject[] found2 = FindNearby(tree, GameObjectType.Any, s.Position+offset, (256-s.Radius)+0.001f);
            Assert.AreEqual(1, found2.Length, "FindNearby touching radius must return match");
            
            GameplayObject[] found3 = FindNearby(tree, GameObjectType.Any, s.Position+offset, 255-s.Radius);
            Assert.AreEqual(0, found3.Length, "FindNearby outside radius must not match");
        }

        public void TestFindNearbyMulti(ISpatial tree)
        {
            CreateQuadTree(100, tree);

            GameplayObject[] f1 = FindNearby(tree, GameObjectType.Any, Vector2.Zero, 7200);
            Assert.AreEqual(4, f1.Length, "FindNearby center 7200 must match 4");

            GameplayObject[] f2 = FindNearby(tree, GameObjectType.Any, Vector2.Zero, 16000);
            Assert.AreEqual(12, f2.Length, "FindNearby center 16000 must match 12");
            
            GameplayObject[] f3 = FindNearby(tree, GameObjectType.Any, Vector2.Zero, 26000);
            Assert.AreEqual(24, f3.Length, "FindNearby center 26000 must match 24");
        }

        void CheckFindNearby(GameplayObject[] found, GameObjectType expected,
                             Vector2 pos, float radius)
        {
            Assert.AreNotEqual(0, found.Length);
            foreach (GameplayObject go in found)
            {
                Assert.AreEqual(expected, go.Type);
                Assert.IsTrue(go.Position.Distance(pos) <= radius);
            }
        }

        public void TestFindNearbyTypeFilter(ISpatial tree)
        {
            CreateQuadTree(100, tree);
            QtreePerfTests.SpawnProjectilesFromEachShip(tree, AllObjects, new Vector2(100));

            foreach (GameplayObject obj in AllObjects)
            {
                if (obj is Ship s)
                {
                    GameplayObject[] found = FindNearby(tree, GameObjectType.Proj, s.Position, 10000);
                    CheckFindNearby(found, GameObjectType.Proj, s.Position, 10000);
                }
            }
        }

        public void TestFindNearbyExcludeLoyaltyFilter(ISpatial tree)
        {
            CreateQuadTree(100, tree);

            foreach (Ship s in AllObjects)
            {
                var opt = new SearchOptions(s.Position, 10000, GameObjectType.Ship)
                {
                    ExcludeLoyalty = s.loyalty
                };
                GameplayObject[] found = tree.FindNearby(opt);
                CheckFindNearby(found, GameObjectType.Ship, s.Position, 10000);
            }
        }

        public void TestFindNearbyOnlyLoyaltyFilter(ISpatial tree)
        {
            CreateQuadTree(100, tree);

            foreach (Ship s in AllObjects)
            {
                var opt = new SearchOptions(s.Position, 10000, GameObjectType.Ship)
                {
                    OnlyLoyalty = s.loyalty
                };
                GameplayObject[] found = tree.FindNearby(opt);
                CheckFindNearby(found, GameObjectType.Ship, s.Position, 10000);
            }
        }

        public void TestTreeUpdatePerformance(ISpatial tree)
        {
            CreateQuadTree(5_000, tree);
            float e = 0f;
            for (int i = 0; i < 10; ++i)
            {
                foreach (Ship ship in AllObjects)
                {
                    ship.Center.X += 10f;
                    ship.Position = ship.Center;
                    ship.UpdateModulePositions(TestSimStep, true, forceUpdate: true);
                }
                var t = new PerfTimer();
                tree.UpdateAll(AllObjects);
                e += t.Elapsed;
            }
            Console.WriteLine($"-- Tree UpdateAll elapsed: {(e*1000).String(2)}ms");
            
            if (EnableVisualization)
                DebugVisualize(tree);
        }

        public void TestTreeSearchPerformance(ISpatial tree)
        {
            CreateQuadTree(1_000, tree);
            const float defaultSensorRange = 30000f;

            var t1 = new PerfTimer();
            for (int i = 0; i < AllObjects.Count; ++i)
            {
                var s = (Ship)AllObjects[i];
                var opt = new SearchOptions(s.Position, defaultSensorRange);
                tree.FindLinear(opt);
            }
            float e1 = t1.Elapsed;
            Console.WriteLine($"-- LinearSearch 10k ships, 30k sensor elapsed: {(e1*1000).String(2)}ms");

            var t2 = new PerfTimer();
            for (int i = 0; i < AllObjects.Count; ++i)
            {
                var s = (Ship)AllObjects[i];
                var opt = new SearchOptions(s.Position, defaultSensorRange);
                tree.FindNearby(opt);
            }
            float e2 = t2.Elapsed;
            Console.WriteLine($"-- TreeSearch 10k ships, 30k sensor elapsed: {(e2*1000).String(2)}ms");

            float speedup = e1 / e2;
            Assert.IsTrue(speedup > 1.2f, "TreeSearch must be significantly faster than linear search!");
            Console.WriteLine($"-- TreeSearch is {speedup.String(2)}x faster than LinearSearch");

            if (EnableVisualization)
                DebugVisualize(tree);
        }

        public void TestConcurrentUpdateAndSearch(ISpatial tree)
        {
            CreateQuadTree(5_000, tree);
            var timer = new PerfTimer();

            // update
            Parallel.Run(() =>
            {
                while (timer.Elapsed < 1.0)
                {
                    foreach (Ship ship in AllObjects)
                    {
                        ship.Center.X += 10f;
                        ship.Position = ship.Center;
                        ship.UpdateModulePositions(TestSimStep, true, forceUpdate: true);
                    }
                    tree.UpdateAll(AllObjects);
                    tree.CollideAll(TestSimStep);
                }
            });

            // search
            Parallel.Run(() =>
            {
                const float defaultSensorRange = 30000f;
                while (timer.Elapsed < 1.0)
                {
                    for (int i = 0; i < AllObjects.Count; ++i)
                    {
                        var s = (Ship)AllObjects[i];
                        var opt = new SearchOptions(s.Position, defaultSensorRange);
                        tree.FindNearby(opt);
                    }
                }
            });
        }

        public void TestTreeCollisionPerformance(ISpatial tree)
        {
            CreateQuadTree(10_000, tree);
            const int iterations = 50;

            var t1 = new PerfTimer();
            for (int i = 0; i < iterations; ++i)
            {
                tree.CollideAll(TestSimStep);
            }
            float e1 = t1.Elapsed;
            Console.WriteLine($"-- CollideAll 10k ships, 30k sensor elapsed: {(e1*1000).String(2)}ms");
        }
    }
}
