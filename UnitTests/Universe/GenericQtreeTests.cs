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

        public GenericQtreeTests()
        {
            CreateUniverseAndPlayerEmpire();
        }

        protected void DebugVisualize(GenericQtree tree)
        {
            var vis = new GenericQtreeVisualization(tree.Objects.ToArr(), tree);
            EnableMockInput(false); // switch from mocked input to real input
            Game.ShowAndRun(screen: vis); // run the sim
            EnableMockInput(true); // restore the mock input
        }

        [TestMethod]
        public void SearchForSolarSystems()
        {
            Planet playerHome = AddHomeWorldToEmpire(new(500_000, 750_000f), Player);
            Planet enemyHome = AddHomeWorldToEmpire(new(-500_000, -750_000f), Enemy);

            var tree = new GenericQtree(UState.Size * 2f);

            tree.Insert(playerHome.ParentSystem);
            tree.Insert(enemyHome.ParentSystem);

            tree.Insert(playerHome);
            tree.Insert(enemyHome);

            if (EnableVisualization)
                DebugVisualize(tree);
        }
    }
}
