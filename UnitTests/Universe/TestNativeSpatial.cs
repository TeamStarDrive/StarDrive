using System;
using Ship_Game.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;

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
    }
}
