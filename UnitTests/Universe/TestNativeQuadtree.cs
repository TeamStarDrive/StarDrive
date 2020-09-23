using System;
using Ship_Game.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Universe
{
    [TestClass]
    public class TestNativeQuadtree : TestQuadtreeCommon
    {
        const SpatialType Type = SpatialType.Grid;

        [TestMethod]
        public void BasicInsert()
        {
            TestBasicInsert(new NativeSpatial(Type, 100_000));
        }

        [TestMethod]
        public void FindNearbySingle()
        {
            TestFindNearbySingle(new NativeSpatial(Type, 10_000));
        }

        [TestMethod]
        public void FindNearbyMulti()
        {
            TestFindNearbyMulti(new NativeSpatial(Type, 10_000));
        }
        
        [TestMethod]
        public void FindNearbyTypeFilter()
        {
            TestFindNearbyTypeFilter(new NativeSpatial(Type, 10_000));
        }

        [TestMethod]
        public void TreeUpdatePerformance()
        {
            TestTreeUpdatePerformance(new NativeSpatial(Type, 1_000_000));
        }

        [TestMethod]
        public void TreeSearchPerformance()
        {
            TestTreeSearchPerformance(new NativeSpatial(Type, 500_000));
        }

        [TestMethod]
        public void ConcurrentUpdateAndSearch()
        {
            TestConcurrentUpdateAndSearch(new NativeSpatial(Type, 500_000));
        }

        [TestMethod]
        public void TreeCollisionPerformance()
        {
            TestTreeCollisionPerformance(new NativeSpatial(Type, 50_000));
        }
    }
}
