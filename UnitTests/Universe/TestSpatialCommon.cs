using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using Ship_Game.Utils;
using Parallel = Ship_Game.Parallel;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Universe
{
    public abstract class TestSpatialCommon : StarDriveTest
    {
        protected static bool EnableVisualization = false;
        protected static bool EnableMovingShips = true;
        protected SpatialObjectBase[] AllObjects = Empty<SpatialObjectBase>.Array;

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
            if (AllObjects.Length != 0)
            {
                UState.Objects.Clear();
                AllObjects = Empty<SpatialObjectBase>.Array;
            }
            AllObjects = QtreePerfTests.CreateTestSpace(tree, numShips, spawnProjectilesWithOffset, 
                                                        Player, Enemy, SpawnShip);
            return tree;
        }

        protected void DebugVisualize(ISpatial tree, bool enableMovingShips = true, bool updateObjects = false)
        {
            bool moving = enableMovingShips && EnableMovingShips;
            var vis = new SpatialVisualization(this, AllObjects, tree, moving);
            vis.MoveShips |= updateObjects;
            Game.ShowAndRun(screen: vis);
        }

        protected SpatialObjectBase[] FindNearby(ISpatial tree, GameObjectType type, Vector2 pos, float r)
        {
            SearchOptions opt = new(pos, r, type)
            {
                MaxResults = 128
            };
            return tree.FindNearby(ref opt);
        }
        
        [TestMethod]
        public void BasicInsert()
        {
            ISpatial tree = CreateQuadTree(100_000, 100);
            AssertEqual(AllObjects.Length, tree.Count);

            foreach (SpatialObjectBase go in AllObjects)
            {
                SpatialObjectBase[] ships = FindNearby(tree, GameObjectType.Ship, go.Position, go.Radius);
                AssertEqual(1, ships.Length);
                AssertEqual(go, ships[0]);
            }

            if (EnableVisualization)
                DebugVisualize(tree);
        }
        
        [TestMethod]
        public void FindNearbySingle()
        {
            ISpatial tree = CreateQuadTree(100_000, 1);

            Ship s = (Ship)AllObjects[0];
            var offset = new Vector2(0, 256);
            SpatialObjectBase[] found1 = FindNearby(tree, GameObjectType.Any, s.Position+offset, 256);
            AssertEqual(1, found1.Length, "FindNearby exact 256 must return match");

            SpatialObjectBase[] found2 = FindNearby(tree, GameObjectType.Any, s.Position+offset, (256-s.Radius)+0.001f);
            AssertEqual(1, found2.Length, "FindNearby touching radius must return match");
            
            SpatialObjectBase[] found3 = FindNearby(tree, GameObjectType.Any, s.Position+offset, 255-s.Radius);
            AssertEqual(0, found3.Length, "FindNearby outside radius must not match");
        }
        
        [TestMethod]
        public void FindNearbyMulti()
        {
            ISpatial tree = CreateQuadTree(100_000, 100);

            SpatialObjectBase[] f1 = FindNearby(tree, GameObjectType.Any, Vector2.Zero, 7200);
            AssertEqual(4, f1.Length, "FindNearby center 7200 must match 4");

            SpatialObjectBase[] f2 = FindNearby(tree, GameObjectType.Any, Vector2.Zero, 16000);
            AssertEqual(12, f2.Length, "FindNearby center 16000 must match 12");
            
            SpatialObjectBase[] f3 = FindNearby(tree, GameObjectType.Any, Vector2.Zero, 26000);
            AssertEqual(24, f3.Length, "FindNearby center 26000 must match 24");
        }

        void CheckFindNearby(SpatialObjectBase[] found, GameObjectType expected,
                             Vector2 pos, float radius)
        {
            Assert.AreNotEqual(0, found.Length);
            foreach (SpatialObjectBase go in found)
            {
                AssertEqual(expected, go.Type);
                float distance = go.Position.Distance(pos);
                float maxError = 0.5f;
                Assert.IsTrue(distance-maxError <= radius, $"distance:{distance} <= radius:{radius} is false");
            }
        }

        void CheckShipsLoyalty(SpatialObjectBase[] found, Empire expected = null, 
                               Empire notExpected = null, Ship notShip = null)
        {
            foreach (SpatialObjectBase foundObj in found)
                if (foundObj is Ship foundShip)
                {
                    if (notShip != null)
                        Assert.AreNotEqual(notShip, foundShip);
                    if (expected != null)
                        AssertEqual(expected, foundShip.Loyalty);
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

            foreach (SpatialObjectBase obj in AllObjects)
            {
                if (!(obj is Ship s))
                    continue;
                SpatialObjectBase[] projectiles = FindNearby(tree, GameObjectType.Proj, s.Position, 10000);
                CheckFindNearby(projectiles, GameObjectType.Proj, s.Position, 10000);

                SpatialObjectBase[] ships = FindNearby(tree, GameObjectType.Ship, s.Position, 10000);
                CheckFindNearby(ships, GameObjectType.Ship, s.Position, 10000);
            }
        }

        [TestMethod]
        public void FindNearbyOnlyLoyaltyFilter()
        {
            ISpatial tree = CreateQuadTree(10_000, 100, spawnProjectilesWithOffset:100f);

            foreach (SpatialObjectBase obj in AllObjects)
            {
                if (!(obj is Ship s))
                    continue;
                SearchOptions opt = new(s.Position, 10000, GameObjectType.Ship)
                {
                    MaxResults = 32,
                    Exclude = s,
                    OnlyLoyalty = s.Loyalty,
                };
                SpatialObjectBase[] found = tree.FindNearby(ref opt);
                CheckFindNearby(found, GameObjectType.Ship, s.Position, 10000);
                CheckShipsLoyalty(found, expected:s.Loyalty, notShip:s);
            }
        }
        
        [TestMethod]
        public void FindNearbyExcludeLoyaltyFilter()
        {
            ISpatial tree = CreateQuadTree(10_000, 100, spawnProjectilesWithOffset:100f);

            foreach (SpatialObjectBase obj in AllObjects)
            {
                if (!(obj is Ship s))
                    continue;
                SearchOptions opt = new(s.Position, 10000, GameObjectType.Ship)
                {
                    MaxResults = 32,
                    Exclude = s,
                    ExcludeLoyalty = s.Loyalty,
                };
                SpatialObjectBase[] found = tree.FindNearby(ref opt);
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
            var s = (Ship)AllObjects[2];
            SearchOptions opt = new(s.Position, 30000, GameObjectType.Ship)
            {
                MaxResults = 32,
                Exclude = s,
                OnlyLoyalty = s.Loyalty, // loyalty must be '1', not '0'
            };
            SpatialObjectBase[] found = tree.FindNearby(ref opt);
            AssertEqual(3, found.Length, "FindNearby must include all friends and not self");
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
            for (int i = 0; i < AllObjects.Length; ++i)
            {
                var s = (Ship)AllObjects[i];
                SearchOptions opt = new(s.Position, defaultSensorRange);
                tree.FindLinear(ref opt);
            }
            float e1 = t1.Elapsed;
            Console.WriteLine($"-- LinearSearch 10k ships, 30k sensor elapsed: {(e1*1000).String(2)}ms");

            var t2 = new PerfTimer();
            for (int i = 0; i < AllObjects.Length; ++i)
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
            
            const float TIME_TO_RUN = 1.1f;

            var timer = new PerfTimer();
            var allObjects = new Array<GameObject>();

            // update
            TaskResult updateResult = Parallel.Run(() =>
            {
                var rand = new SeededRandom(1337);
                var spawned = new HashSet<Ship>();

                while (timer.Elapsed < TIME_TO_RUN)
                {
                    for (int i = 0; i < allObjects.Count; ++i)
                    {
                        if (allObjects[i] is Ship ship)
                        {
                            ship.Position.X += 10f;
                            ship.UpdateModulePositions(TestSimStep, true, forceUpdate: true);

                            if (rand.RollDice(percent:10) && !spawned.Contains(ship))
                            {
                                spawned.Add(ship);
                                Weapon weapon = ship.Weapons.First;
                                var p = Projectile.Create(weapon, ship, ship.Position, Vectors.Up, null, false);
                                allObjects.Add(p);
                            }
                        }
                        else if (allObjects[i] is Projectile proj)
                        {
                            proj.Update(TestSimStep);
                        }
                    }

                    tree.UpdateAll(AllObjects);

                    allObjects.RemoveInActiveObjects();
                    AllObjects = allObjects.ToArray().FastCast<GameObject, SpatialObjectBase>();

                    tree.CollideAll(TestSimStep, showCollisions: false);
                }
            });

            // search
            TaskResult searchResult = Parallel.Run(() =>
            {
                const float defaultSensorRange = 30000f;
                while (timer.Elapsed < (TIME_TO_RUN-0.1f))
                {
                    for (int i = 0; i < AllObjects.Length; ++i)
                    {
                        if (AllObjects[i] is Ship s)
                        {
                            SearchOptions shipOpt = new(s.Position, defaultSensorRange, GameObjectType.Ship);
                            SearchOptions projOpt = new(s.Position, defaultSensorRange, GameObjectType.Proj);
                            SpatialObjectBase[] ships = tree.FindNearby(ref shipOpt);
                            SpatialObjectBase[] projectiles = tree.FindNearby(ref projOpt);

                            foreach (SpatialObjectBase go in ships)
                            {
                                Assert.IsTrue(go is Ship, $"FindNearby(Type=Ship) contains a non-ship: {go?.ToString() ?? "null"}");
                            }
                            foreach (SpatialObjectBase go in projectiles)
                            {
                                Assert.IsTrue(go is Projectile, $"FindNearby(Type=Proj) contains a non-projectile: {go?.ToString() ?? "null"}");
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
            var allObjects = new Array<GameObject>();

            int x = 0;
            foreach (GameObject go in allObjects.ToArray())
            {
                go.Radius *= 2;
                ++x;
                go.Velocity.X = (5 - x % 10) * 200.0f;
                go.Velocity.Y = (10 - x % 20) * 200.0f;

                var ship = (Ship)go;
                Weapon weapon = ship.Weapons.First;
                for (int j = 0; j < 5; ++j)
                {
                    var p = Projectile.Create(weapon, ship, ship.Position + new Vector2(200), Vectors.Up, null, false);
                    p.Radius = go.Radius / 2;
                    p.Velocity = go.Velocity.LeftVector();
                    p.VelocityMax = p.Velocity.Length();
                    p.Duration = 10;
                    allObjects.Add(p);
                }
            }

            AllObjects = allObjects.ToArray().FastCast<GameObject, SpatialObjectBase>();
            tree.UpdateAll(AllObjects);
            //DebugVisualize(tree, enableMovingShips:false, updateObjects:true);

            const int iterations = 60*3;
            int total = 0;
            
            var t1 = new PerfTimer();
            for (int i = 0; i < iterations; ++i)
            {
                foreach (SpatialObjectBase go in AllObjects)
                {
                    if (go is Ship s)
                    {
                        s.UpdateVelocityAndPosition(TestSimStep.FixedTime, Vector2.Zero, isZeroAcc:true);
                        s.UpdateModulePositions(TestSimStep, true);
                    }
                    else if (go is Projectile p)
                    {
                        p.TestUpdatePhysics(TestSimStep);
                    }
                }
                tree.UpdateAll(AllObjects);
                total += tree.CollideAll(TestSimStep, showCollisions: false);
            }
            float e1 = t1.Elapsed;
            Console.WriteLine($"-- CollideAll 10k ships, 30k sensor elapsed: {(e1*1000).String(2)}ms");
            Console.WriteLine($"-- CollideAll total collisions: {total}");
        }
    }
}
