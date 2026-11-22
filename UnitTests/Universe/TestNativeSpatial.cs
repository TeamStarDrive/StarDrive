using System;
using Ship_Game.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SDUtils;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Universe
{
    [TestClass]
    public class TestNativeSpatial : TestSpatialCommon
    {
        static SpatialType Type = SpatialType.Qtree;
        
        protected override ISpatial Create(int worldSize)
        {
            // NOTE: each spatial type requires their own parameters,
            // otherwise they will be constructed incorrectly
            if (Type == SpatialType.Grid)
            {
                return new NativeSpatial(SpatialType.Grid, worldSize, 10_000);
            }
            if (Type == SpatialType.GridL2)
            {
                return new NativeSpatial(SpatialType.GridL2, worldSize, 20_000, 1000);
            }
            if (Type == SpatialType.Qtree)
            {
                return new NativeSpatial(SpatialType.Qtree, worldSize, 1024);
            }
            throw new ArgumentException("Invalid SpatialType");
        }

        [TestMethod]
        public void ABufferedSearchKeepsItsResultsWhileASearchInsideItRuns()
        {
            CreateQuadTree(100_000, 100);
            RunObjectsSim(TestSimStep);
            var s = (Ship)AllObjects[0];
            SearchOptions all = new(s.Position, 60000, GameObjectType.Ship)
            {
                MaxResults = 64,
                SortByDistance = true,
            };
            SpatialObjectBase[] expected = UState.Spatial.FindNearby(ref all);
            Assert.IsTrue(expected.Length > 1, $"setup: the search must find ships: {expected.Length}");

            UState.Spatial.FindNearbyBuffered(ref all).Dispose(); // this thread's spare buffer, as any earlier search leaves it
            using (SpatialManager.Found outer = UState.Spatial.FindNearbyBuffered(ref all))
            {
                Assert.AreEqual(expected.Length, outer.Length, "the same ships as an array search");
                for (int i = 0; i < outer.Length; ++i)
                {
                    SearchOptions near = new(outer[i].Position, 60000, GameObjectType.Ship) { MaxResults = 8 };
                    using (SpatialManager.Found inner = UState.Spatial.FindNearbyBuffered(ref near))
                        Assert.IsTrue(inner.Length > 1, $"setup: the inner search must find several ships: {inner.Length}");
                    for (int j = 0; j < expected.Length; ++j)
                        Assert.AreEqual(expected[j], outer[j], $"the outer results stay as they were found, after the search around ship {i}");
                }
            }

            UState.Spatial.FindNearbyBuffered(ref all).Dispose();
            long before = GC.GetAllocatedBytesForCurrentThread();
            using (SpatialManager.Found again = UState.Spatial.FindNearbyBuffered(ref all))
                Assert.AreEqual(expected.Length, again.Length, "and the same ships again");
            Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before, "a search with its buffer given back makes no array");
        }

        [TestMethod]
        public void ACrowdedCellIsCollidedInTheSamePairsAndOrderAsALoopOverEveryPairOfIt()
        {
            using var tree = new NativeSpatial(SpatialType.Qtree, 100_000, 1024);
            var objects = new SpatialObjectBase[40];
            for (int i = 0; i < objects.Length; ++i)
            {
                Ship_Game.Empire owner = i % 3 == 0 ? Enemy : Player;
                Ship ship = SpawnShip("Vulcan Scout", owner, Vector2.Zero);
                objects[i] = i % 2 == 0 ? ship : Projectile.Create(ship.Weapons.First, ship, Vector2.Zero, Vectors.Up, null, false);
            }
            int spacing = new AABoundingBox2Di(objects[1]).Width / 2; // projectiles two places apart just touch
            Assert.IsTrue(spacing >= 2, $"setup: a projectile's box is wide enough to space the objects by: {spacing}");
            for (int i = 0; i < objects.Length; ++i)
            {
                int slot = i * 17 % objects.Length; // left edges in an order unlike the cell's
                objects[i].Position = new(slot * spacing, slot % 2 * 4);
            }
            tree.UpdateAll(objects);
            for (int i = 0; i < objects.Length; ++i)
                Assert.AreEqual(i, objects[i].SpatialIndex, "setup: the cell holds the objects in the order they were added");

            var expected = new Array<CollisionPair>();
            int touching = 0;
            for (int i = 0; i < objects.Length; ++i)
            {
                for (int j = i + 1; j < objects.Length; ++j)
                {
                    SpatialObjectBase a = objects[i], b = objects[j];
                    var ra = new AABoundingBox2Di(a);
                    var rb = new AABoundingBox2Di(b);
                    if ((NativeSpatialObject.GetCollisionMask(a.Type) & NativeSpatialObject.GetCollisionMask(b.Type)) != 0
                        && a.GetLoyaltyId() != b.GetLoyaltyId() && ra.Overlaps(rb))
                    {
                        expected.Add(new CollisionPair(a.SpatialIndex, b.SpatialIndex));
                        if (ra.X2 == rb.X1 || rb.X2 == ra.X1)
                            ++touching;
                    }
                }
            }
            Assert.IsTrue(expected.Count > objects.Length, $"setup: most objects meet several others: {expected.Count} pairs");
            Assert.IsTrue(touching > 0, "setup: some boxes just touch, edge to edge");

            CollisionPair[] found = tree.FindCollisionPairs();
            Assert.AreEqual(expected.Count, found.Length, "the same number of pairs");
            for (int k = 0; k < found.Length; ++k)
            {
                Assert.IsTrue(expected[k].A == found[k].A && expected[k].B == found[k].B,
                              $"pair {k}: ({expected[k].A}, {expected[k].B}) expected, ({found[k].A}, {found[k].B}) found");
            }
        }
    }
}
