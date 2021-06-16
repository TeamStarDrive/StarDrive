using System;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Spatial;

namespace UnitTests.Universe
{
    [TestClass]
    public class TestManagedQuadTree : TestSpatialCommon
    {
        protected override ISpatial Create(int worldSize)
        {
            return new Qtree(worldSize, 1024);
        }
    }
}
