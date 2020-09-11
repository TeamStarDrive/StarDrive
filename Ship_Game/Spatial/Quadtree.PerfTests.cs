using System;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class QuadtreePerfTests
    {
        public class TestContext
        {
            public Array<Ship> Ships = new Array<Ship>();
            public IQuadtree Tree;
        }

        public delegate Ship SpawnShipFunc(string name, Empire loyalty, Vector2 pos, Vector2 dir);

        public static TestContext CreateTestSpace(int numShips, IQuadtree tree,
                                                  Empire player, Empire enemy,
                                                  SpawnShipFunc spawnShip)
        {
            var test = new TestContext();
            float spacing = tree.UniverseSize / (float)Math.Sqrt(numShips);

            // universe is centered at [0,0], so Root node goes from [-half, +half)
            float half = tree.UniverseSize / 2;
            float start = -half + spacing/2;
            float x = start;
            float y = start;

            for (int i = 0; i < numShips; ++i)
            {
                bool isPlayer = (i % 2) == 0;

                Ship ship = spawnShip("Vulcan Scout", isPlayer ? player : enemy, new Vector2(x, y), default);
                test.Ships.Add(ship);

                x += spacing;
                if (x >= half)
                {
                    x = start;
                    y += spacing;
                }
            }

            test.Tree = tree;
            foreach (Ship ship in test.Ships)
                test.Tree.Insert(ship);

            test.Tree.UpdateAll();
            return test;
        }

        static Ship SpawnShip(string name, Empire loyalty, Vector2 pos, Vector2 dir)
        {
            var target = Ship.CreateShipAtPoint(name, loyalty, pos);
            target.Rotation = dir.Normalized().ToRadians();
            target.InFrustum = true; // force module pos update
            //target.UpdateShipStatus(new FixedSimTime(0.01f)); // update module pos
            target.UpdateModulePositions(new FixedSimTime(0.01f), true, forceUpdate: true);
            return target;
        }

        public static void RunSearchPerfTest()
        {
            TestContext test = CreateTestSpace(10000, new Quadtree(500_000f),
                                    EmpireManager.Void, EmpireManager.Void, SpawnShip);

            const float defaultSensorRange = 30000f;
            const int iterations = 10;

            var t1 = new PerfTimer();
            for (int x = 0; x < iterations; ++x)
            {
                for (int i = 0; i < test.Ships.Count; ++i)
                {
                    Ship ship = test.Ships[i];
                    test.Tree.FindLinear(GameObjectType.Any, ship.Center, defaultSensorRange,
                                         maxResults:256, null, null, null);
                }
            }
            float e1 = t1.Elapsed;
            Log.Write($"-- LinearSearch 10k ships, 30k sensor elapsed: {e1.String(2)}s");

            var t2 = new PerfTimer();
            for (int x = 0; x < iterations; ++x)
            {
                for (int i = 0; i < test.Ships.Count; ++i)
                {
                    test.Tree.FindNearby(GameObjectType.Any, test.Ships[i].Center, defaultSensorRange,
                                         maxResults:256, null, null, null);
                }
            }
            float e2 = t2.Elapsed;
            Log.Write($"-- TreeSearch 10k ships, 30k sensor elapsed: {e2.String(2)}s");

            float speedup = e1 / e2;
            Log.Write($"-- TreeSearch is {speedup.String(2)}x faster than LinearSearch");
        }

        public static void RunCollisionPerfTest()
        {
            TestContext test = CreateTestSpace(10000, new Quadtree(500_000f), 
                                    EmpireManager.Void, EmpireManager.Void, SpawnShip);

            const int iterations = 1000;
            var timeStep = new FixedSimTime(1f / 60f);

            var t1 = new PerfTimer();
            for (int i = 0; i < iterations; ++i)
            {
                test.Tree.CollideAll(timeStep);
            }
            float e1 = t1.Elapsed;
            Console.WriteLine($"-- CollideAllIterative 10k ships, 30k sensor elapsed: {(e1*1000).String(2)}ms");

            var t2 = new PerfTimer();
            for (int i = 0; i < iterations; ++i)
            {
                test.Tree.CollideAllRecursive(timeStep);
            }
            float e2 = t2.Elapsed;
            Console.WriteLine($"-- CollideAllRecursive 10k ships, 30k sensor elapsed: {(e2*1000).String(2)}ms");

        }
    }
}