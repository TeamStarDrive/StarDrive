using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Serialization;

namespace UnitTests.Serialization
{
    [TestClass]
    public class BinarySerializerTests : StarDriveTest
    {
        static byte[] Serialize<T>(T instance)
        {
            var serializer = new BinarySerializer(typeof(T));
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            serializer.Serialize(writer, instance);
            return ms.ToArray();
        }

        static T Deserialize<T>(byte[] bytes)
        {
            var serializer = new BinarySerializer(typeof(T));
            var reader = new BinaryReader(new MemoryStream(bytes));
            return (T)serializer.Deserialize(reader);
        }

        static T SerDes<T>(T instance)
        {
            byte[] bytes = Serialize<T>(instance);
            return Deserialize<T>(bytes);
        }

        [StarDataType]
        class IntegersContainingType
        {
            public int IntZero, IntMin, IntMax;
            public uint UIntZero, UIntMin, UIntMax;
            public long LongZero, LongMin, LongMax;
            public ulong ULongZero, ULongMin, ULongMax;
            public short ShortZero, ShortMin, ShortMax;
            public ushort UShortZero, UShortMin, UShortMax;
            public sbyte SByteZero, SByteMin, SByteMax;
            public byte ByteZero, ByteMin, ByteMax;
        }

        [TestMethod]
        public void IntegerTypes()
        {
            var instance = new IntegersContainingType
            {
                IntZero = 0, IntMin = int.MinValue, IntMax = int.MaxValue,
                UIntZero = 0, UIntMin = uint.MinValue, UIntMax = uint.MaxValue,
                LongZero = 0, LongMin = long.MinValue, LongMax = long.MaxValue,
                ULongZero = 0, ULongMin = ulong.MinValue, ULongMax = ulong.MaxValue,
                ShortZero = 0, ShortMin = short.MinValue, ShortMax = short.MaxValue,
                UShortZero = 0, UShortMin = ushort.MinValue, UShortMax = ushort.MaxValue,
                SByteZero = 0, SByteMin = sbyte.MinValue, SByteMax = sbyte.MaxValue,
                ByteZero = 0, ByteMin = byte.MinValue, ByteMax = byte.MaxValue,
            };

            var result = SerDes(instance);
            Assert.AreEqual(result.IntZero, (int)0);
            Assert.AreEqual(result.IntMin, int.MinValue);
            Assert.AreEqual(result.IntMax, int.MaxValue);
            Assert.AreEqual(result.UIntZero, (uint)0);
            Assert.AreEqual(result.UIntMin, uint.MinValue);
            Assert.AreEqual(result.UIntMax, uint.MaxValue);
            Assert.AreEqual(result.LongZero, (long)0);
            Assert.AreEqual(result.LongMin, long.MinValue);
            Assert.AreEqual(result.LongMax, long.MaxValue);
            Assert.AreEqual(result.ULongZero, (ulong)0);
            Assert.AreEqual(result.ULongMin, ulong.MinValue);
            Assert.AreEqual(result.ULongMax, ulong.MaxValue);
            Assert.AreEqual(result.ShortZero, (short)0);
            Assert.AreEqual(result.ShortMin, short.MinValue);
            Assert.AreEqual(result.ShortMax, short.MaxValue);
            Assert.AreEqual(result.UShortZero, (ushort)0);
            Assert.AreEqual(result.UShortMin, ushort.MinValue);
            Assert.AreEqual(result.UShortMax, ushort.MaxValue);
            Assert.AreEqual(result.SByteZero, (sbyte)0);
            Assert.AreEqual(result.SByteMin, sbyte.MinValue);
            Assert.AreEqual(result.SByteMax, sbyte.MaxValue);
            Assert.AreEqual(result.ByteZero, (byte)0);
            Assert.AreEqual(result.ByteMin, byte.MinValue);
            Assert.AreEqual(result.ByteMax, byte.MaxValue);
        }

        [StarDataType]
        class CustomRecursiveType
        {
            [StarData] public CustomRecursiveType RecursiveSelf;
            [StarData] public string Text = "Hello";
            [StarData] public int Count = 42;
        }

        [TestMethod]
        public void BasicRecursiveType()
        {
            var instance = new CustomRecursiveType();
            instance.RecursiveSelf = instance;
            byte[] bytes = Serialize(instance);
        }
    }
}
