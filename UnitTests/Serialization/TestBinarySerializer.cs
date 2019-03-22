using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Serialization;

namespace UnitTests.Serialization
{
    [TestClass]
    public class TestBinarySerializer
    {

        [StarDataType]
        class CustomType
        {
            [StarData] public CustomType RecursiveSelf;
            [StarData] public string Text = "Hello";
            [StarData] public int Count = 42;
        }

        [TestMethod]
        public void BasicTypeSerialize()
        {
            var instance = new CustomType();
            instance.RecursiveSelf = instance;

            var serializer = new BinarySerializer(typeof(CustomType));
            var ms = new MemoryStream();
            serializer.Serialize(new BinaryWriter(ms), instance);

        }
    }
}
