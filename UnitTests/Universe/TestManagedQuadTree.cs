using System;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Spatial;

namespace UnitTests.Universe
{
    [TestClass]
    public class TestManagedQuadTree : TestSpatialCommon
    {
        [TestMethod]
        public void BasicInsert()
        {
            TestBasicInsert(new Qtree(100_000));
        }

        [TestMethod]
        public void FindNearbySingle()
        {
            TestFindNearbySingle(new Qtree(100_000));
        }

        [TestMethod]
        public void FindNearbyMulti()
        {
            TestFindNearbyMulti(new Qtree(100_000));
        }

        [TestMethod]
        public void FindNearbyTypeFilter()
        {
            TestFindNearbyTypeFilter(new Qtree(100_000));
        }

        [TestMethod]
        public void TestFindNearbyExcludeLoyaltyFilter()
        {
            TestFindNearbyExcludeLoyaltyFilter(new Qtree(100_000));
        }

        [TestMethod]
        public void TestFindNearbyOnlyLoyaltyFilter()
        {
            TestFindNearbyOnlyLoyaltyFilter(new Qtree(100_000));
        }

        [TestMethod]
        public void TreeUpdatePerformance()
        {
            TestTreeUpdatePerformance(new Qtree(1_000_000));
        }

        [TestMethod]
        public void TreeSearchPerformance()
        {
            TestTreeSearchPerformance(new Qtree(500_000));
        }

        [TestMethod]
        public void ConcurrentUpdateAndSearch()
        {
            TestConcurrentUpdateAndSearch(new Qtree(500_000));
        }

        [TestMethod]
        public void TreeCollisionPerformance()
        {
            TestTreeCollisionPerformance(new Qtree(100_000));
        }
    }
}
