using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Spatial;
using SDUtils;

namespace UnitTests.Universe
{
    // NOTE: This tests GenericQtree only. For collision quadtree check TestNativeSpatial
    [TestClass]
    public class GenericQtreeTests : StarDriveTest
    {
        protected static bool EnableVisualization = false;
        protected SpatialObjectBase[] AllObjects = Empty<SpatialObjectBase>.Array;

        public GenericQtreeTests()
        {
            CreateUniverseAndPlayerEmpire();
        }

        protected void DebugVisualize(GenericQtree tree)
        {
            var vis = new GenericQtreeVisualization(AllObjects, tree, moving);
            vis.MoveShips |= updateObjects;
            Game.ShowAndRun(screen: vis);
        }

        [TestMethod]
        public void Test
    }
}
