using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Data;

namespace UnitTests
{
    [TestClass]
    public class TestStarDataParser
    {
        public class TestData
        {
            [StarDataKey] public int Id { get; set; }
            [StarData]    public PlanetCategory Category;
        }

        [TestMethod]
        public void ParsePlanetTypes()
        {
            Directory.SetCurrentDirectory("/Projects/BlackBox");

            using (var parser = new StarDataParser("Content/PlanetTypes.yaml"))
            {
                var items = parser.DeserializeArray<TestData>();
                Log.Info(parser.Root.SerializedText());
            }
        }
    }
}
