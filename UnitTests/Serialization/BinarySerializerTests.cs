using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace UnitTests.Serialization
{
    [TestClass]
    public class BinarySerializerTests : StarDriveTest
    {
        static byte[] Serialize<T>(BinarySerializer ser, T instance)
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            ser.Serialize(writer, instance);
            return ms.ToArray();
        }

        static T Deserialize<T>(BinarySerializer ser, byte[] bytes)
        {
            var reader = new BinaryReader(new MemoryStream(bytes));
            return (T)ser.Deserialize(reader);
        }

        static byte[] Serialize<T>(T instance)
        {
            var ser = new BinarySerializer(typeof(T));
            return Serialize(ser, instance);
        }

        static T Deserialize<T>(byte[] bytes)
        {
            var ser = new BinarySerializer(typeof(T));
            return Deserialize<T>(ser, bytes);
        }

        static T SerDes<T>(T instance, out byte[] bytes)
        {
            var ser = new BinarySerializer(typeof(T));
            bytes = Serialize<T>(ser, instance);
            return Deserialize<T>(ser, bytes);
        }

        [StarDataType]
        class IntegersType
        {
            [StarData] public int IntZero, IntMin, IntMax;
            [StarData] public uint UIntZero, UIntMin, UIntMax;
            [StarData] public long LongZero, LongMin, LongMax;
            [StarData] public ulong ULongZero, ULongMin, ULongMax;
            [StarData] public short ShortZero, ShortMin, ShortMax;
            [StarData] public ushort UShortZero, UShortMin, UShortMax;
            [StarData] public sbyte SByteZero, SByteMin, SByteMax;
            [StarData] public byte ByteZero, ByteMin, ByteMax;
        }

        [TestMethod]
        public void IntegerTypes()
        {
            var instance = new IntegersType
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

            var result = SerDes(instance, out byte[] bytes);
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
        class FloatsType
        {
            [StarData] public float FloatZero, FloatMin, FloatMax;
            [StarData] public double DoubleZero, DoubleMin, DoubleMax;
        }

        [TestMethod]
        public void FloatTypes()
        {
            var instance = new FloatsType
            {
                FloatZero = 0,
                FloatMin = float.MinValue,
                FloatMax = float.MaxValue,
                DoubleZero = 0,
                DoubleMin = double.MinValue,
                DoubleMax = double.MaxValue,
            };

            var result = SerDes(instance, out byte[] bytes);
            Assert.AreEqual(result.FloatZero, (float)0.0f);
            Assert.AreEqual(result.FloatMin, float.MinValue);
            Assert.AreEqual(result.FloatMax, float.MaxValue);
            Assert.AreEqual(result.DoubleZero, (double)0.0);
            Assert.AreEqual(result.DoubleMin, double.MinValue);
            Assert.AreEqual(result.DoubleMax, double.MaxValue);
        }

        [StarDataType]
        class VectorsType
        {
            [StarData] public Vector2 Vector2Zero, Vector2Min, Vector2Max;
            [StarData] public Vector3 Vector3Zero, Vector3Min, Vector3Max;
            [StarData] public Vector4 Vector4Zero, Vector4Min, Vector4Max;
            [StarData] public Vector2d Vector2dZero, Vector2dMin, Vector2dMax;
            [StarData] public Vector3d Vector3dZero, Vector3dMin, Vector3dMax;
            [StarData] public Point PointZero, PointMin, PointMax;
        }

        [TestMethod]
        public void VectorTypes()
        {
            var instance = new VectorsType
            {
                Vector2Zero = Vector2.Zero, Vector2Min = new Vector2(-1,-2), Vector2Max = new Vector2(1,2),
                Vector3Zero = Vector3.Zero, Vector3Min = new Vector3(-1,-2,-3), Vector3Max = new Vector3(1,2,3),
                Vector4Zero = Vector4.Zero, Vector4Min = new Vector4(-1,-2,-3,-4), Vector4Max = new Vector4(1,2,3,4),
                Vector2dZero = Vector2d.Zero, Vector2dMin = new Vector2d(-1,-2), Vector2dMax = new Vector2d(1,2),
                Vector3dZero = Vector3d.Zero, Vector3dMin = new Vector3d(-1,-2,-3), Vector3dMax = new Vector3d(1,2,3),
                PointZero = Point.Zero, PointMin = new Point(-1,-2), PointMax = new Point(1,2),
            };

            var result = SerDes(instance, out byte[] bytes);
            Assert.AreEqual(result.Vector2Zero, Vector2.Zero);
            Assert.AreEqual(result.Vector2Min, new Vector2(-1,-2));
            Assert.AreEqual(result.Vector2Max, new Vector2(1,2));
            Assert.AreEqual(result.Vector3Zero, Vector3.Zero);
            Assert.AreEqual(result.Vector3Min, new Vector3(-1,-2,-3));
            Assert.AreEqual(result.Vector3Max, new Vector3(1,2,3));
            Assert.AreEqual(result.Vector4Zero, Vector4.Zero);
            Assert.AreEqual(result.Vector4Min, new Vector4(-1,-2,-3,-4));
            Assert.AreEqual(result.Vector4Max, new Vector4(1,2,3,4));
            Assert.AreEqual(result.Vector2dZero, Vector2d.Zero);
            Assert.AreEqual(result.Vector2dMin, new Vector2d(-1,-2));
            Assert.AreEqual(result.Vector2dMax, new Vector2d(1,2));
            Assert.AreEqual(result.Vector3dZero, Vector3d.Zero);
            Assert.AreEqual(result.Vector3dMin, new Vector3d(-1,-2,-3));
            Assert.AreEqual(result.Vector3dMax, new Vector3d(1,2,3));
            Assert.AreEqual(result.PointZero, Point.Zero);
            Assert.AreEqual(result.PointMin, new Point(-1, -2));
            Assert.AreEqual(result.PointMax, new Point(1, 2));
        }

        [StarDataType]
        class CustomRecursiveType
        {
            [StarData] public CustomRecursiveType RecursiveSelf;
            [StarData] public string Text;
            [StarData] public int Count;
        }

        [TestMethod]
        public void BasicRecursiveType()
        {
            var instance = new CustomRecursiveType
            {
                Text = "Hello",
                Count = 42,
            };
            instance.RecursiveSelf = instance;

            var result = SerDes(instance, out byte[] bytes);
            Assert.AreEqual(result.RecursiveSelf, result, "Recursive self reference must match");
            Assert.AreEqual(instance.Text, result.Text, "string must match");
            Assert.AreEqual(instance.Count, result.Count, "int field must match");
        }

        [StarDataType]
        struct SmallStruct
        {
            [StarData] public int X;
            [StarData] public int Y;
        }

        [StarDataType]
        class CustomStructContainer
        {
            [StarData] public SmallStruct Pos;
        }

        [TestMethod]
        public void NestedUserTypeStruct()
        {
            var instance = new CustomStructContainer
            {
                Pos = new SmallStruct { X = 15, Y = 33 },
            };
            var result = SerDes(instance, out byte[] bytes);
            Assert.AreEqual(instance.Pos.X, result.Pos.X, "SmallStruct fields must match");
            Assert.AreEqual(instance.Pos.Y, result.Pos.Y, "SmallStruct fields must match");
        }

        //[TestMethod]
        //public void SerializeAShip()
        //{
        //    CreateUniverseAndPlayerEmpire();
        //    Ship ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
        //    byte[] bytes = Serialize(ship);
        //}
    }
}
