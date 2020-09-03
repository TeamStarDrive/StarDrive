using System;
using System.Collections.Generic;
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

        public readonly Array<Ship> AllShips = new Array<Ship>();
        public float UniverseSize;

        public TestQuadTree()
        {
            CreateGameInstance(800, 800, mockInput:false);
            LoadStarterShips();
            CreateUniverseAndPlayerEmpire(out Empire _);
        }

        Quadtree CreateQuadTree(int numShips, float universeSize)
        {
            UniverseSize = universeSize;
            float spacing = universeSize / (float)Math.Sqrt(numShips);

            // universe is centered at [0,0], so Root node goes from [-half, +half)
            float half = universeSize / 2;
            float start = -half + spacing/2;
            float x = start;
            float y = start;

            for (int i = 0; i < numShips; ++i)
            {
                bool player = (i % 2) == 0;

                Ship ship = SpawnShip("Vulcan Scout", player ? Player : Enemy, new Vector2(x, y));
                AllShips.Add(ship);

                x += spacing;
                if (x >= half)
                {
                    x = start;
                    y += spacing;
                }
            }

            var tree = new Quadtree(universeSize);
            foreach (Ship ship in AllShips)
                tree.Insert(ship);
            return tree;
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

            GameplayObject[] found2 = tree.FindNearby(s.Position+offset, 256-s.Radius);
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
        public void ConcurrentUpdateAndSearch()
        {
            var timer = PerfTimer.StartNew();

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
