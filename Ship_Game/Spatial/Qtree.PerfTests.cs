using System;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Spatial
{
    public class QtreePerfTests
    {
        public delegate Ship SpawnShipFunc(string name, Empire loyalty, Vector2 pos, Vector2 dir);

        public static SpatialObjectBase[] CreateTestSpace(ISpatial tree, int numShips,
            float spawnProjectilesWithOffset, Empire player, Empire enemy, SpawnShipFunc spawnShip)
        {
            var allObjects = new Array<GameObject>();
            var ships = new Array<Ship>();
            float spacing = tree.WorldSize / (float)Math.Sqrt(numShips);

            // universe is centered at [0,0], so Root node goes from [-half, +half)
            float half = tree.WorldSize / 2;
            float start = -half + spacing/2;
            float x = start;
            float y = start;

            for (int i = 0; i < numShips; ++i)
            {
                bool isPlayer = (i % 2) == 0;

                Ship ship = spawnShip("Vulcan Scout", isPlayer ? player : enemy, new Vector2(x, y), default);
                ships.Add(ship);
                allObjects.Add(ship);

                x += spacing;
                if (x >= half)
                {
                    x = start;
                    y += spacing;
                }
            }

            if (spawnProjectilesWithOffset > 0)
            {
                foreach (Ship ship in ships)
                {
                    Weapon weapon = ship.Weapons.First;
                    var p = Projectile.Create(weapon, ship,
                                              ship.Position + new Vector2(0, spawnProjectilesWithOffset), Vectors.Up, null, false);
                    allObjects.Add(p);
                }
            }

            var objects = allObjects.ToArray();
            tree.UpdateAll(objects);
            return objects;
        }

        static Ship SpawnShip(string name, Empire loyalty, Vector2 pos, Vector2 dir)
        {
            var target = Ship.CreateShipAtPoint(null, name, loyalty, pos);
            target.Rotation = dir.Normalized().ToRadians();
            target.UpdateModulePositions(new FixedSimTime(0.01f), true, forceUpdate: true);
            return target;
        }

        public static GameObject[] SpawnProjectilesFromEachShip(
            ISpatial tree, GameObject[] allObjects, Vector2 offset)
        {
            var projectiles = new Array<Projectile>();
            foreach (GameObject go in allObjects)
            {
                if (!(go is Ship ship))
                    continue;
                Weapon weapon = ship.Weapons.First;
                var p = Projectile.Create(weapon, ship, ship.Position + offset, Vectors.Up, null, false);
                projectiles.Add(p);
            }

            var objects = allObjects.Concat(projectiles).ToArr();
            tree.UpdateAll(objects);
            return objects;
        }

        public static void RunSearchPerfTest()
        {
            var tree = new Qtree(500_000f);
            SpatialObjectBase[] ships = CreateTestSpace(tree, 10000, 0,
                Empire.Void, Empire.Void, SpawnShip);

            const float defaultSensorRange = 30000f;
            const int iterations = 10;

            var t1 = new PerfTimer();
            for (int x = 0; x < iterations; ++x)
            {
                for (int i = 0; i < ships.Length; ++i)
                {
                    var s = (Ship)ships[i];
                    SearchOptions opt = new(s.Position, defaultSensorRange)
                    {
                        MaxResults = 256
                    };
                    tree.FindLinear(ref opt);
                }
            }
            float e1 = t1.Elapsed;
            Log.Write($"-- LinearSearch 10k ships, 30k sensor elapsed: {e1.String(2)}s");

            var t2 = new PerfTimer();
            for (int x = 0; x < iterations; ++x)
            {
                for (int i = 0; i < ships.Length; ++i)
                {
                    var s = (Ship)ships[i];
                    SearchOptions opt = new(s.Position, defaultSensorRange)
                    {
                        MaxResults = 256
                    };
                    tree.FindNearby(ref opt);
                }
            }
            float e2 = t2.Elapsed;
            Log.Write($"-- TreeSearch 10k ships, 30k sensor elapsed: {e2.String(2)}s");

            float speedup = e1 / e2;
            Log.Write($"-- TreeSearch is {speedup.String(2)}x faster than LinearSearch");
        }

        public static void RunCollisionPerfTest()
        {
            var tree = new Qtree(500_000f);
            SpatialObjectBase[] ships = CreateTestSpace(tree, 10000, 0,
                Empire.Void, Empire.Void, SpawnShip);

            const int iterations = 1000;
            var timeStep = new FixedSimTime(1f / 60f);

            var t1 = new PerfTimer();
            for (int i = 0; i < iterations; ++i)
            {
                tree.CollideAll(timeStep, showCollisions: false);
            }
            float e1 = t1.Elapsed;
            Console.WriteLine($"-- CollideAll 10k ships, 30k sensor elapsed: {(e1*1000).String(2)}ms");
        }
    }
}