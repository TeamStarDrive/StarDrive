using System;
using System.IO;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Ships;

namespace SDUnitTests
{
    [TestClass]
    public class TestSerialization
    {
        public class DataToSerialize
        {
            public ShipData.RoleName RoleName;
        }

        [TestMethod]
        public void SerializeEnums()
        {
            var ser = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
            };

            var data = new DataToSerialize { RoleName = ShipData.RoleName.carrier }; 

            var writer = new StringWriter();
            ser.Serialize(writer, data);
            string serialized = writer.ToString();

            var reader = new JsonTextReader(new StringReader(serialized));
            var data2 = ser.Deserialize<DataToSerialize>(reader);

            Assert.AreEqual(data2.RoleName, data.RoleName);

            var reader3 = new JsonTextReader(new StringReader(serialized));
            string custom = "{\"RoleName\":\"carrier\"}";
            var data3 = ser.Deserialize<DataToSerialize>(reader3);

            Assert.AreEqual(data3.RoleName, data.RoleName);
        }

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
