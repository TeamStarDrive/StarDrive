using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            [StarData] public Color ColorZero, ColorMin, ColorMax;
            [StarData] public Range RangeZero, RangeMin, RangeMax;
        }

        [TestMethod]
        public void VectorTypes()
        {
            var instance = new VectorsType
            {
                Vector2Zero = Vector2.Zero, Vector2Min = new Vector2(-1,-2), Vector2Max = new Vector2(1,2),
                Vector3Zero = Vector3.Zero, Vector3Min = new Vector3(-3,-4,-5), Vector3Max = new Vector3(3,4,5),
                Vector4Zero = Vector4.Zero, Vector4Min = new Vector4(-6,-7,-8,-9), Vector4Max = new Vector4(6,7,8,9),
                Vector2dZero = Vector2d.Zero, Vector2dMin = new Vector2d(-11,-12), Vector2dMax = new Vector2d(11,12),
                Vector3dZero = Vector3d.Zero, Vector3dMin = new Vector3d(-13,-14,-15), Vector3dMax = new Vector3d(13,14,15),
                PointZero = Point.Zero, PointMin = new Point(-16,-17), PointMax = new Point(16,17),
                ColorZero = new Color(0,0,0), ColorMin = new Color(21,22,23), ColorMax = new Color(245,250,255),
                RangeZero = new Range(), RangeMin = new Range(-24,-25), RangeMax = new Range(24,25),
            };

            var result = SerDes(instance, out byte[] bytes);
            Assert.AreEqual(result.Vector2Zero, Vector2.Zero);
            Assert.AreEqual(result.Vector2Min, new Vector2(-1,-2));
            Assert.AreEqual(result.Vector2Max, new Vector2(1,2));
            Assert.AreEqual(result.Vector3Zero, Vector3.Zero);
            Assert.AreEqual(result.Vector3Min, new Vector3(-3,-4,-5));
            Assert.AreEqual(result.Vector3Max, new Vector3(3,4,5));
            Assert.AreEqual(result.Vector4Zero, Vector4.Zero);
            Assert.AreEqual(result.Vector4Min, new Vector4(-6,-7,-8,-9));
            Assert.AreEqual(result.Vector4Max, new Vector4(6,7,8,9));
            Assert.AreEqual(result.Vector2dZero, Vector2d.Zero);
            Assert.AreEqual(result.Vector2dMin, new Vector2d(-11,-12));
            Assert.AreEqual(result.Vector2dMax, new Vector2d(11,12));
            Assert.AreEqual(result.Vector3dZero, Vector3d.Zero);
            Assert.AreEqual(result.Vector3dMin, new Vector3d(-13,-14,-15));
            Assert.AreEqual(result.Vector3dMax, new Vector3d(13,14,15));
            Assert.AreEqual(result.PointZero, Point.Zero);
            Assert.AreEqual(result.PointMin, new Point(-16, -17));
            Assert.AreEqual(result.PointMax, new Point(16, 17));
            Assert.AreEqual(result.ColorZero, new Color(0, 0, 0));
            Assert.AreEqual(result.ColorMin, new Color(21, 22, 23));
            Assert.AreEqual(result.ColorMax, new Color(245, 250, 255));
            Assert.AreEqual(result.RangeZero, new Range());
            Assert.AreEqual(result.RangeMin, new Range(-24, -25));
            Assert.AreEqual(result.RangeMax, new Range(24, 25));
        }

        [StarDataType]
        class TextsType
        {
            [StarData] public string StrNull, StrEmpty, StrShort, StrLong;
            [StarData] public LocalizedText LocId, LocNameId, LocRawText, LocParse;
        }

        [TestMethod]
        public void TextTypes()
        {
            var instance = new TextsType()
            {
                StrNull = null, StrEmpty = string.Empty, StrShort = "AxisAlign",
                StrLong = "Little brown fox jumped over the big dog",
                LocId = new LocalizedText(1234),
                LocNameId = new LocalizedText("NameId1234", LocalizationMethod.NameId),
                LocRawText = new LocalizedText("RawText123", LocalizationMethod.RawText),
                LocParse = new LocalizedText("{1234}", LocalizationMethod.Parse),
            };
            var result = SerDes(instance, out byte[] bytes);

            Assert.AreEqual(instance.StrNull, result.StrNull);
            Assert.AreEqual(instance.StrEmpty, result.StrEmpty);
            Assert.AreEqual(instance.StrShort, result.StrShort);
            Assert.AreEqual(instance.StrLong, result.StrLong);

            Assert.AreEqual(instance.LocId, result.LocId);
            Assert.AreEqual(instance.LocNameId, result.LocNameId);
            Assert.AreEqual(instance.LocRawText, result.LocRawText);
            Assert.AreEqual(instance.LocParse, result.LocParse);
        }

        [StarDataType]
        class DateTimesTypes
        {
            [StarData] public TimeSpan TimeZero, TimeMin, TimeMax;
            [StarData] public DateTime DateTimeMin, DateTimeMax, DateTimeNow;
        }

        [TestMethod]
        public void DateTimeTypes()
        {
            var instance = new DateTimesTypes()
            {
                TimeZero = TimeSpan.Zero, TimeMin = new TimeSpan(10000), TimeMax = new TimeSpan(1000000000L),
                DateTimeMin = DateTime.MinValue, DateTimeMax = DateTime.MaxValue, DateTimeNow = DateTime.UtcNow,
            };
            var result = SerDes(instance, out byte[] bytes);
            Assert.AreEqual(result.TimeZero, TimeSpan.Zero);
            Assert.AreEqual(result.TimeMin, new TimeSpan(10000));
            Assert.AreEqual(result.TimeMax, new TimeSpan(1000000000L));
            Assert.AreEqual(result.DateTimeMin, DateTime.MinValue);
            Assert.AreEqual(result.DateTimeMax, DateTime.MaxValue);
            Assert.AreEqual(result.DateTimeNow, instance.DateTimeNow);
        }

        [StarDataType]
        class RecursiveType
        {
            [StarData] public RecursiveType RecursiveSelf;
            [StarData] public string Text;
            [StarData] public int Count;
            public RecursiveType() {}
            public RecursiveType(string text, int count)
            {
                Text = text;
                Count = count;
                RecursiveSelf = this;
            }
        }

        [TestMethod]
        public void BasicRecursiveType()
        {
            var instance = new RecursiveType("Hello", 42);
            var result = SerDes(instance, out byte[] bytes);
            Assert.AreEqual(result.RecursiveSelf, result, "Recursive self reference must match");
            Assert.AreEqual(instance.Text, result.Text, "string must match");
            Assert.AreEqual(instance.Count, result.Count, "int field must match");
        }

        [StarDataType]
        struct SmallStruct
        {
            [StarData] public int Id;
            [StarData] public string Name;

            public SmallStruct(int id, string name)
            {
                Id = id;
                Name = name;
            }
        }

        [StarDataType]
        class StructContainer
        {
            [StarData] public SmallStruct SS;

            public StructContainer() {}
            public StructContainer(int id, string name)
            {
                SS = new SmallStruct(id, name);
            }
        }

        [TestMethod]
        public void NestedUserTypeStruct()
        {
            var instance = new StructContainer(15, "Laika");
            var result = SerDes(instance, out byte[] bytes);
            Assert.AreEqual(instance.SS.Id, result.SS.Id, "Nested SmallStruct Id fields must match");
            Assert.AreEqual(instance.SS.Name, result.SS.Name, "Nested SmallStruct Name fields must match");
        }


        [StarDataType]
        class ComplexType
        {
            [StarData] public string TestText;
            [StarData] public RecursiveType RecursiveType;
            [StarData] public Array<string> Names;
            [StarData] public StructContainer SCont;
            [StarData] public float Number;
            [StarData] public SmallStruct Struct;
            [StarData] public Array<StructContainer> Structs;
            [StarData] public Array<ComplexType> ComplexTypes;
            [StarData] public ComplexType[] ComplexTypesArr;
            [StarData] public Map<string, int> Map;

            public ComplexType() {}
            public ComplexType(string testText, bool createSubTypes)
            {
                TestText = testText;
                RecursiveType = new RecursiveType("Sayonara", 2021);
                Names = new Array<string>(new[]{ "Little", "Brown", "Fox", "Jumped", "Over" });
                SCont = new StructContainer(2021, "It's Over");
                Number = 4094;
                Struct = new SmallStruct(1337, "Small and awesome");
                Structs = new Array<StructContainer>();
                for (int i = 0; i < 10; ++i)
                    Structs.Add(new StructContainer(i, "sc"+i));
                if (createSubTypes)
                {
                    ComplexTypes = new Array<ComplexType>();
                    for (int i = 0; i < 2; ++i)
                        ComplexTypes.Add(new ComplexType("Subtype"+i, false));
                    ComplexTypesArr = ComplexTypes.ToArray();
                }
                Map = new Map<string, int>();
                Map.Add("Key1", 1);
                Map.Add("Key2", 2);
            }
        }

        [TestMethod]
        public void ComplexTypeTest()
        {
            var instance = new ComplexType("root", createSubTypes:true);
            var result = SerDes(instance, out byte[] bytes);
            Assert.AreEqual(instance.TestText, result.TestText);

            Assert.AreEqual(result.RecursiveType, result.RecursiveType.RecursiveSelf);
            Assert.AreEqual(instance.RecursiveType.Text, result.RecursiveType.Text);
            Assert.AreEqual(instance.RecursiveType.Count, result.RecursiveType.Count);

            Assert.That.Equal(instance.Names, result.Names);
            Assert.AreEqual(instance.SCont.SS, result.SCont.SS);
            Assert.AreEqual(instance.SCont.SS.Id, result.SCont.SS.Id);
            Assert.AreEqual(instance.SCont.SS.Name, result.SCont.SS.Name);

            Assert.AreEqual(instance.Number, result.Number);
            Assert.AreEqual(instance.Struct.Id, result.Struct.Id);
            Assert.AreEqual(instance.Struct.Name, result.Struct.Name);

            Assert.AreEqual(instance.Structs.Count, result.Structs.Count);
            for (int i = 0; i < instance.Structs.Count; ++i)
            {
                //Assert.AreEqual(instance.Structs[i].SS.Id,)
            }
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
