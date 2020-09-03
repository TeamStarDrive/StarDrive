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
            public Quadtree Tree;
        }

        public delegate Ship SpawnShipFunc(string name, Empire loyalty, Vector2 pos, Vector2 dir);

        public static TestContext CreateTestSpace(int numShips, float universeSize,
                                                  Empire player, Empire enemy,
                                                  SpawnShipFunc spawnShip)
        {
            var test = new TestContext();
            float spacing = universeSize / (float)Math.Sqrt(numShips);

            // universe is centered at [0,0], so Root node goes from [-half, +half)
            float half = universeSize / 2;
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

            test.Tree = new Quadtree(universeSize);
            foreach (Ship ship in test.Ships)
                test.Tree.Insert(ship);
            return test;
        }

        
        // optimized comparison alternative for quadtree search
        public static Array<GameplayObject> FindLinearOpt(Array<Ship> ships, Ship us,
                                                          Vector2 center, float radius,
                                                          Empire loyaltyFilter = null)
        {
            float cx = center.X;
            float cy = center.Y;
            float r  = radius;

            var list = new Array<GameplayObject>();
            Ship[] items = ships.GetInternalArrayItems();
            int count = ships.Count;
            for (int i = 0; i < count; ++i)
            {
                Ship ship = items[i];
                if (ship == us || (loyaltyFilter != null && ship.loyalty != loyaltyFilter))
                    continue;

                // check if inside radius, inlined for perf
                float dx = cx - ship.Center.X;
                float dy = cy - ship.Center.Y;
                float r2 = r + ship.Radius;
                if ((dx*dx + dy*dy) <= (r2*r2))
                {
                    list.Add(ship);
                }
            }
            return list;
        }


        public static void RunSearchPerfTest()
        {
            Ship SpawnShip(string name, Empire loyalty, Vector2 pos, Vector2 dir)
            {
                var target = Ship.CreateShipAtPoint(name, loyalty, pos);
                target.Rotation = dir.Normalized().ToRadians();
                target.InFrustum = true; // force module pos update
                //target.UpdateShipStatus(new FixedSimTime(0.01f)); // update module pos
                target.UpdateModulePositions(new FixedSimTime(0.01f), true, forceUpdate: true);
                return target;
            }

            TestContext test = CreateTestSpace(10000, 500_000f,
                                               EmpireManager.Void, EmpireManager.Void,
                                               SpawnShip);

            const float defaultSensorRange = 30000f;
            const int iterations = 10;

            var t1 = new PerfTimer();
            for (int x = 0; x < iterations; ++x)
            {
                for (int i = 0; i < test.Ships.Count; ++i)
                {
                    Ship ship = test.Ships[i];
                    FindLinearOpt(test.Ships, ship, ship.Center, defaultSensorRange);
                }
            }
            float e1 = t1.Elapsed;
            Log.Write($"-- LinearSearch 10k ships, 30k sensor elapsed: {e1.String(2)}s");

            var t2 = new PerfTimer();
            for (int x = 0; x < iterations; ++x)
            {
                for (int i = 0; i < test.Ships.Count; ++i)
                {
                    test.Tree.FindNearby(test.Ships[i].Center, defaultSensorRange);
                }
            }
            float e2 = t2.Elapsed;
            Log.Write($"-- TreeSearch 10k ships, 30k sensor elapsed: {e2.String(2)}s");

            float speedup = e1 / e2;
            Log.Write($"-- TreeSearch is {speedup.String(2)}x faster than LinearSearch");
        }
    }
}