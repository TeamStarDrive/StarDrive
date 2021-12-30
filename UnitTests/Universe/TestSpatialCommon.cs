using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using Parallel = Ship_Game.Parallel;

namespace UnitTests.Universe
{
    public abstract class TestSpatialCommon : StarDriveTest
    {
        protected static bool EnableVisualization = false;
        protected static bool EnableMovingShips = true;
        //protected Array<Ship> AllShips = new Array<Ship>();
        protected Array<GameplayObject> AllObjects = new Array<GameplayObject>();


        protected TestSpatialCommon()
        {
            EnableMockInput(false);
            CreateUniverseAndPlayerEmpire();
        }

        [TestCleanup]
        public void Teardown()
        {
            EnableMockInput(true);
        }

        protected abstract ISpatial Create(int worldSize);

        protected ISpatial CreateQuadTree(int worldSize, int numShips, float spawnProjectilesWithOffset = 0f)
        {
            ISpatial tree = Create(worldSize);
            if (!AllObjects.IsEmpty)
            {
                Universe.Objects.Clear();
                AllObjects.Clear();
            }
            AllObjects = QtreePerfTests.CreateTestSpace(tree, numShips, spawnProjectilesWithOffset, 
                                                        Player, Enemy, SpawnShip);
            return tree;
        }

        protected void DebugVisualize(ISpatial tree, bool enableMovingShips = true, bool updateObjects = false)
        {
            bool moving = enableMovingShips && EnableMovingShips;
            var vis = new SpatialVisualization(AllObjects, tree, moving);
            vis.MoveShips |= updateObjects;
            Game.ShowAndRun(screen: vis);
        }

        protected GameplayObject[] FindNearby(ISpatial tree, GameObjectType type, Vector2 pos, float r)
        {
            var opt = new SearchOptions(pos, r, type);
            opt.MaxResults = 128;
            return tree.FindNearby(ref opt);
        }
        
        [TestMethod]
        public void BasicInsert()
        {
            ISpatial tree = CreateQuadTree(100_000, 100);
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
        
        [TestMethod]
        public void FindNearbySingle()
        {
            ISpatial tree = CreateQuadTree(100_000, 1);

            Ship s = (Ship)AllObjects.First;
            var offset = new Vector2(0, 256);
            GameplayObject[] found1 = FindNearby(tree, GameObjectType.Any, s.Position+offset, 256);
            Assert.AreEqual(1, found1.Length, "FindNearby exact 256 must return match");

            GameplayObject[] found2 = FindNearby(tree, GameObjectType.Any, s.Position+offset, (256-s.Radius)+0.001f);
            Assert.AreEqual(1, found2.Length, "FindNearby touching radius must return match");
            
            GameplayObject[] found3 = FindNearby(tree, GameObjectType.Any, s.Position+offset, 255-s.Radius);
            Assert.AreEqual(0, found3.Length, "FindNearby outside radius must not match");
        }
        
        [TestMethod]
        public void FindNearbyMulti()
        {
            ISpatial tree = CreateQuadTree(100_000, 100);

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
                float distance = go.Position.Distance(pos);
                float maxError = 0.5f;
                Assert.IsTrue(distance-maxError <= radius, $"distance:{distance} <= radius:{radius} is false");
            }
        }

        void CheckShipsLoyalty(GameplayObject[] found, Empire expected = null, 
                              Empire notExpected = null, Ship notShip = null)
        {
            foreach (GameplayObject foundObj in found)
                if (foundObj is Ship foundShip)
                {
                    if (notShip != null)
                        Assert.AreNotEqual(notShip, foundShip);
                    if (expected != null)
                        Assert.AreEqual(expected, foundShip.Loyalty);
                    else if (notExpected != null)
                        Assert.AreNotEqual(notExpected, foundShip.Loyalty);
                }
                else
                    Assert.Fail($"FindNearby result is not a Ship! {foundObj}");
        }
        
        [TestMethod]
        public void FindNearbyTypeFilter()
        {
            ISpatial tree = CreateQuadTree(100_000, 100, spawnProjectilesWithOffset:100f);

            foreach (GameplayObject obj in AllObjects)
            {
                if (!(obj is Ship s))
                    continue;
                GameplayObject[] projectiles = FindNearby(tree, GameObjectType.Proj, s.Position, 10000);
                CheckFindNearby(projectiles, GameObjectType.Proj, s.Position, 10000);

                GameplayObject[] ships = FindNearby(tree, GameObjectType.Ship, s.Position, 10000);
                CheckFindNearby(ships, GameObjectType.Ship, s.Position, 10000);
            }
        }

        [TestMethod]
        public void FindNearbyOnlyLoyaltyFilter()
        {
            ISpatial tree = CreateQuadTree(10_000, 100, spawnProjectilesWithOffset:100f);

            foreach (GameplayObject obj in AllObjects)
            {
                if (!(obj is Ship s))
                    continue;
                var opt = new SearchOptions(s.Position, 10000, GameObjectType.Ship)
                {
                    MaxResults = 32,
                    Exclude = s,
                    OnlyLoyalty = s.Loyalty,
                };
                GameplayObject[] found = tree.FindNearby(ref opt);
                CheckFindNearby(found, GameObjectType.Ship, s.Position, 10000);
                CheckShipsLoyalty(found, expected:s.Loyalty, notShip:s);
            }
        }
        
        [TestMethod]
        public void FindNearbyExcludeLoyaltyFilter()
        {
            ISpatial tree = CreateQuadTree(10_000, 100, spawnProjectilesWithOffset:100f);

            foreach (GameplayObject obj in AllObjects)
            {
                if (!(obj is Ship s))
                    continue;
                var opt = new SearchOptions(s.Position, 10000, GameObjectType.Ship)
                {
                    MaxResults = 32,
                    Exclude = s,
                    ExcludeLoyalty = s.Loyalty,
                };
                GameplayObject[] found = tree.FindNearby(ref opt);
                CheckFindNearby(found, GameObjectType.Ship, s.Position, 10000);
                CheckShipsLoyalty(found, notExpected:s.Loyalty, notShip:s);
            }
        }
        
        /// <summary>
        /// Specific regression which happened with specific ObjectId,
        /// OnlyLoyalty and Exclude id combinations.
        /// </summary>
        [TestMethod]
        public void FindNearbyFriendsExcludeSelf_Regression()
        {
            // we only need a tiny universe with 8 ships
            ISpatial tree = CreateQuadTree(30_000, 8);

            // second ship, this created the specific bitmask 0010+0001 for search fail
            var s = AllObjects[2] as Ship;
            var opt = new SearchOptions(s.Position, 30000, GameObjectType.Ship)
            {
                MaxResults = 32,
                Exclude = s,
                OnlyLoyalty = s.Loyalty, // loyalty must be '1', not '0'
            };
            GameplayObject[] found = tree.FindNearby(ref opt);
            Assert.AreEqual(3, found.Length, "FindNearby must include all friends and not self");
            CheckFindNearby(found, GameObjectType.Ship, s.Position, 30000);
            CheckShipsLoyalty(found, expected:s.Loyalty, notShip:s);
        }
        
        [TestMethod]
        public void TreeUpdatePerformance()
        {
            ISpatial tree = CreateQuadTree(1_000_000, 5_000);
            float e = 0f;
            for (int i = 0; i < 10; ++i)
            {
                foreach (Ship ship in AllObjects)
                {
                    ship.Position.X += 10f;
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
        
        [TestMethod]
        public void TreeSearchPerformance()
        {
            ISpatial tree = CreateQuadTree(500_000, 1_000);
            const float defaultSensorRange = 30000f;

            var t1 = new PerfTimer();
            for (int i = 0; i < AllObjects.Count; ++i)
            {
                var s = (Ship)AllObjects[i];
                var opt = new SearchOptions(s.Position, defaultSensorRange);
                tree.FindLinear(ref opt);
            }
            float e1 = t1.Elapsed;
            Console.WriteLine($"-- LinearSearch 10k ships, 30k sensor elapsed: {(e1*1000).String(2)}ms");

            var t2 = new PerfTimer();
            for (int i = 0; i < AllObjects.Count; ++i)
            {
                var s = (Ship)AllObjects[i];
                var opt = new SearchOptions(s.Position, defaultSensorRange);
                tree.FindNearby(ref opt);
            }
            float e2 = t2.Elapsed;
            Console.WriteLine($"-- TreeSearch 10k ships, 30k sensor elapsed: {(e2*1000).String(2)}ms");

            float speedup = e1 / e2;
            Assert.IsTrue(speedup > 1.2f, "TreeSearch must be significantly faster than linear search!");
            Console.WriteLine($"-- TreeSearch is {speedup.String(2)}x faster than LinearSearch");

            if (EnableVisualization)
                DebugVisualize(tree);
        }
        
        [TestMethod]
        public void ConcurrentUpdateAndSearch()
        {
            ISpatial tree = CreateQuadTree(500_000, 5_000);
            
            GameplayObject[] objects = Empty<GameplayObject>.Array;
            var timer = new PerfTimer();

            // update
            TaskResult updateResult = Parallel.Run(() =>
            {
                var rand = new Random();
                var spawned = new HashSet<Ship>();

                while (timer.Elapsed < 1.0)
                {
                    for (int i = 0; i < AllObjects.Count; ++i)
                    {
                        if (AllObjects[i] is Ship ship)
                        {
                            ship.Position.X += 10f;
                            ship.UpdateModulePositions(TestSimStep, true, forceUpdate: true);

                            if (rand.Next(100) <= 10 && !spawned.Contains(ship)) // 10% chance
                            {
                                spawned.Add(ship);
                                Weapon weapon = ship.Weapons.First;
                                var p = Projectile.Create(weapon, ship.Position, Vectors.Up, null, false);
                                AllObjects.Add(p);
                            }
                        }
                        else if (AllObjects[i] is Projectile proj)
                        {
                            proj.Update(TestSimStep);
                        }
                    }

                    tree.UpdateAll(AllObjects);

                    AllObjects.RemoveInActiveObjects();
                    objects = AllObjects.ToArray();

                    tree.CollideAll(TestSimStep);
                }
            });

            // search
            TaskResult searchResult = Parallel.Run(() =>
            {
                const float defaultSensorRange = 30000f;
                while (timer.Elapsed < 1.0)
                {
                    GameplayObject[] objs = objects;
                    for (int i = 0; i < objs.Length; ++i)
                    {
                        if (objs[i] is Ship s)
                        {
                            var shipOpt = new SearchOptions(s.Position, defaultSensorRange, GameObjectType.Ship);
                            var projOpt = new SearchOptions(s.Position, defaultSensorRange, GameObjectType.Proj);
                            GameplayObject[] ships = tree.FindNearby(ref shipOpt);
                            GameplayObject[] projectiles = tree.FindNearby(ref projOpt);

                            foreach (GameplayObject go in ships)
                            {
                                Assert.IsTrue(go is Ship, $"FindNearby(Type=Ship) contains a non-ship: {go}");
                            }
                            foreach (GameplayObject go in projectiles)
                            {
                                Assert.IsTrue(go is Projectile, $"FindNearby(Type=Proj) contains a non-projectile: {go}");
                            }
                        }
                    }
                }
            });

            updateResult.WaitNoThrow();
            searchResult.WaitNoThrow();
            if (updateResult.Error != null)
                Assert.Fail($"Update thread failed: {updateResult.Error.Message}\n{updateResult.Error.StackTrace}");
            if (searchResult.Error != null)
                Assert.Fail($"Search thread failed: {searchResult.Error.Message}\n{searchResult.Error.StackTrace}");
        }
        
        [TestMethod]
        public void CollisionPerformance()
        {
            ISpatial tree = CreateQuadTree(40_000, 2_000);

            int x = 0;
            foreach (GameplayObject go in AllObjects.ToArray())
            {
                go.Radius *= 2;
                ++x;
                go.Velocity.X = (5 - x % 10) * 200.0f;
                go.Velocity.Y = (10 - x % 20) * 200.0f;

                var ship = (Ship)go;
                Weapon weapon = ship.Weapons.First;
                for (int j = 0; j < 5; ++j)
                {
                    var p = Projectile.Create(weapon, ship.Position + new Vector2(200), Vectors.Up, null, false);
                    p.Radius = go.Radius / 2;
                    p.Velocity = go.Velocity.LeftVector();
                    p.Duration = 10;
                    AllObjects.Add(p);
                }
            }

            tree.UpdateAll(AllObjects);
            //DebugVisualize(tree, enableMovingShips:false, updateObjects:true);

            const int iterations = 60*3;
            int total = 0;
            
            var t1 = new PerfTimer();
            for (int i = 0; i < iterations; ++i)
            {
                foreach (GameplayObject go in AllObjects)
                {
                    if (go is Ship s)
                    {
                        s.IntegratePosVelocityVerlet(TestSimStep.FixedTime, Vector2.Zero);
                        s.UpdateModulePositions(TestSimStep, true);
                    }
                    else if (go is Projectile p)
                    {
                        p.TestUpdatePhysics(TestSimStep);
                    }
                }
                tree.UpdateAll(AllObjects);
                total += tree.CollideAll(TestSimStep);
            }
            float e1 = t1.Elapsed;
            Console.WriteLine($"-- CollideAll 10k ships, 30k sensor elapsed: {(e1*1000).String(2)}ms");
            Console.WriteLine($"-- CollideAll total collisions: {total}");
        }
    }
}
