using System;
using Ship_Game.Spatial.Native;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Universe
{
    [TestClass]
    public class TestNativeQuadtree : TestQuadtreeCommon
    {
        [TestMethod]
        public void BasicInsert()
        {
            TestBasicInsert(new NativeQuadtree(100_000f));
        }

        [TestMethod]
        public void FindNearbySingle()
        {
            TestFindNearbySingle(new NativeQuadtree(10_000f));
        }

        [TestMethod]
        public void FindNearbyMulti()
        {
            TestFindNearbyMulti(new NativeQuadtree(10_000f));
        }

        [TestMethod]
        public void TreeUpdatePerformance()
        {
            TestTreeUpdatePerformance(new NativeQuadtree(1_000_000f));
        }

        [TestMethod]
        public void TreeSearchPerformance()
        {
            TestTreeSearchPerformance(new NativeQuadtree(500_000f));
        }

        [TestMethod]
        public void ConcurrentUpdateAndSearch()
        {
            TestConcurrentUpdateAndSearch(new NativeQuadtree(500_000f));
        }

        [TestMethod]
        public void TreeCollisionPerformance()
        {
            TestTreeCollisionPerformance(new NativeQuadtree(50_000f));
        }
    }
}
