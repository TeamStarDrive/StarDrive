using System;
using Ship_Game.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Universe
{
    [TestClass]
    public class TestNativeSpatial : TestSpatialCommon
    {
        const SpatialType Type = SpatialType.Grid;
        const int CellSize = 20_000;

        [TestMethod]
        public void BasicInsert()
        {
            TestBasicInsert(new NativeSpatial(Type, 100_000, CellSize));
        }

        [TestMethod]
        public void FindNearbySingle()
        {
            TestFindNearbySingle(new NativeSpatial(Type, 100_000, CellSize));
        }

        [TestMethod]
        public void FindNearbyMulti()
        {
            TestFindNearbyMulti(new NativeSpatial(Type, 100_000, CellSize));
        }
        
        [TestMethod]
        public void FindNearbyTypeFilter()
        {
            TestFindNearbyTypeFilter(new NativeSpatial(Type, 100_000, CellSize));
        }

        [TestMethod]
        public void TestFindNearbyExcludeLoyaltyFilter()
        {
            TestFindNearbyExcludeLoyaltyFilter(new NativeSpatial(Type, 100_000, CellSize));
        }

        [TestMethod]
        public void TestFindNearbyOnlyLoyaltyFilter()
        {
            TestFindNearbyOnlyLoyaltyFilter(new NativeSpatial(Type, 100_000, CellSize));
        }

        [TestMethod]
        public void TreeUpdatePerformance()
        {
            TestTreeUpdatePerformance(new NativeSpatial(Type, 1_000_000, CellSize));
        }

        [TestMethod]
        public void TreeSearchPerformance()
        {
            TestTreeSearchPerformance(new NativeSpatial(Type, 500_000, CellSize));
        }

        [TestMethod]
        public void ConcurrentUpdateAndSearch()
        {
            TestConcurrentUpdateAndSearch(new NativeSpatial(Type, 500_000, CellSize));
        }

        [TestMethod]
        public void TreeCollisionPerformance()
        {
            TestTreeCollisionPerformance(new NativeSpatial(Type, 100_000, CellSize));
        }
    }
}
