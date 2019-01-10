using System;
using System.IO;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SDUnitTests
{
    [TestClass]
    public class TestSerialization
    {
        [TestMethod]
        public void SerializeXnaRectangle()
        {
            var xnaRect = new Rectangle(42, 8, 20, 40);
            var rectData = new RectangleData(xnaRect);
            var ser = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
            };

            var writer = new StringWriter();
            ser.Serialize(writer, rectData);

            string serialized = writer.ToString();

            var reader = new JsonTextReader(new StringReader(serialized));
            var rectData2 = ser.Deserialize<RectangleData>(reader);
            Rectangle xnaRect2 = rectData2;

            Assert.AreEqual(xnaRect, xnaRect2);
        }
    }
}
