using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Serialization;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using UnitTests.Ships;
using Vector4 = SDGraphics.Vector4;
using Point = SDGraphics.Point;
using ResourceManager = Ship_Game.ResourceManager;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Vector2d = SDGraphics.Vector2d;
using Vector3d = SDGraphics.Vector3d;
#pragma warning disable CS0649

namespace UnitTests.Serialization
{
    [TestClass]
    public class BinarySerializerTests : StarDriveTest
    {
        static byte[] Serialize<T>(BinarySerializer ser, T instance, bool verbose = false)
        {
            var ms = new MemoryStream();
            using var writer = new Writer(ms);
            ser.Serialize(writer, instance, verbose);
            return ms.ToArray();
        }

        static T Deserialize<T>(BinarySerializer ser, byte[] bytes, bool verbose = false)
        {
            using var reader = new Reader(new MemoryStream(bytes));
            return (T)ser.Deserialize(reader, verbose);
        }

        static T SerDes<T>(T instance, out byte[] bytes, bool verbose = false)
        {
            var ser = new BinarySerializer(typeof(T));
            bytes = Serialize(ser, instance, verbose);
            return Deserialize<T>(ser, bytes, verbose);
        }

        static T SerDes<T>(T instance)
        {
            return SerDes(instance, out _);
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

            var result = SerDes(instance);
            Assert.AreEqual(result.FloatZero, 0.0f);
            Assert.AreEqual(result.FloatMin, float.MinValue);
            Assert.AreEqual(result.FloatMax, float.MaxValue);
            Assert.AreEqual(result.DoubleZero, 0.0);
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
            [StarData] public Rectangle RectZero, RectMin, RectMax;
            [StarData] public RectF RectFZero, RectFMin, RectFMax;
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
                RectZero = Rectangle.Empty, RectMin = new Rectangle(-30,-31,-32,-33), RectMax = new Rectangle(30,31,32,33),
                RectFZero = RectF.Empty, RectFMin = new RectF(-40,-41,-42,-43), RectFMax = new RectF(40,41,42,43),
                ColorZero = new Color(0,0,0), ColorMin = new Color(21,22,23), ColorMax = new Color(245,250,255),
                RangeZero = new Range(), RangeMin = new Range(-24,-25), RangeMax = new Range(24,25),
            };

            var result = SerDes(instance);
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
            Assert.AreEqual(result.RectZero, Rectangle.Empty);
            Assert.AreEqual(result.RectMin, new Rectangle(-30,-31,-32,-33));
            Assert.AreEqual(result.RectMax, new Rectangle(30,31,32,33));
            Assert.AreEqual(result.RectFZero, RectF.Empty);
            Assert.AreEqual(result.RectFMin, new RectF(-40,-41,-42,-43));
            Assert.AreEqual(result.RectFMax, new RectF(40,41,42,43));

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
            var result = SerDes(instance);

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
            var result = SerDes(instance);
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
            [StarData] public readonly RecursiveType RecursiveSelf;
            [StarData] public readonly string Text;
            [StarData] public readonly int Count;
            [StarData] public readonly string DefaultIsNotNull = "Default is not null";
            public RecursiveType() {}
            public RecursiveType(string text, int count)
            {
                Text = text;
                Count = count;
                RecursiveSelf = this;
                DefaultIsNotNull = null; // override the default
            }
        }

        [TestMethod]
        public void BasicRecursiveType()
        {
            var instance = new RecursiveType("Hello", 42);
            var result = SerDes(instance);
            Assert.AreEqual(result.RecursiveSelf, result, "Recursive self reference must match");
            Assert.AreEqual(instance.Text, result.Text, "string must match");
            Assert.AreEqual(instance.Count, result.Count, "int field must match");
            Assert.AreEqual(null, result.DefaultIsNotNull, "null field must match");
        }

        [StarDataType]
        struct SmallStruct
        {
            [StarData] public readonly int Id;
            [StarData] public readonly string Name;

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
            var result = SerDes(instance);
            Assert.AreEqual(instance.SS.Id, result.SS.Id, "Nested SmallStruct Id fields must match");
            Assert.AreEqual(instance.SS.Name, result.SS.Name, "Nested SmallStruct Name fields must match");
        }

        static T[] Arr<T>(params T[] elements) => elements;
        static Array<T> List<T>(params T[] elements) => new(elements);
        static Map<K,V> Map<K,V>(params ValueTuple<K, V>[] elements) => new(elements);

        [StarDataType]
        class RawArrayType
        {
            [StarData] public int[] Integers;
            [StarData] public Vector2[] Points;
            [StarData] public string[] Names;
            [StarData] public string[] Empty;
            [StarData] public StructContainer[] Structs;

            // Special array handlers
            [StarData] public byte[] Bytes;
        }

        [TestMethod]
        public void RawArrayTypes()
        {
            var instance = new RawArrayType()
            {
                Integers = Arr(17, 19, 56, 123, 57),
                Points = Arr(Vector2.One, Vector2.UnitX, Vector2.UnitY),
                Names = Arr("Laika", "Strelka", "Bobby", "Rex", "Baron"),
                Empty = new string[0],
                Structs = Arr(new StructContainer(27, "27"), new StructContainer(42, "42")),
                Bytes = new byte[]{ 100, 200, 255, 115, 143, 0, 42 },
            };
            var result = SerDes(instance);
            Assert.That.Equal(instance.Integers, result.Integers);
            Assert.That.Equal(instance.Points, result.Points);
            Assert.That.Equal(instance.Names, result.Names);
            Assert.That.Equal(instance.Empty, result.Empty);
            Assert.That.Equal(instance.Structs.Length, result.Structs.Length);
            for (int i = 0; i < instance.Structs.Length; ++i)
                Assert.That.Equal(instance.Structs[i].SS, result.Structs[i].SS);

            Assert.That.Equal(instance.Bytes, result.Bytes);
        }

        [StarDataType]
        class GenericArrayType
        {
            [StarData] public Array<int> Integers;
            [StarData] public Array<Vector2> Points;
            [StarData] public Array<string> Names;
            [StarData] public Array<string> Empty;
            [StarData] public Array<StructContainer> Structs;
            [StarData] public IReadOnlyList<string> ReadOnlyList;
            [StarData] public IList<string> List;
            [StarData] public ICollection<string> Collection;
            [StarData] public IEnumerable<string> Enumerable;
        }

        [TestMethod]
        public void GenericArrayTypes()
        {
            var instance = new GenericArrayType()
            {
                Integers = List(17, 19, 56, 123, 57),
                Points = List(Vector2.One, Vector2.UnitX, Vector2.UnitY),
                Names = List("Laika", "Strelka", "Bobby", "Rex", "Baron"),
                Empty = List<string>(),
                Structs = List(new StructContainer(27, "27"), new StructContainer(42, "42")),
                ReadOnlyList = List("StarFury", "Thunderbolt", "Omega"),
                List = List("Sirius", "Betelgeuse", "Orion"),
                Collection = List("Morocco", "Italy", "Spain"),
                Enumerable = List("Miami", "New York", "Austin"),
            };
            var result = SerDes(instance);
            Assert.That.Equal(instance.Integers, result.Integers);
            Assert.That.Equal(instance.Points, result.Points);
            Assert.That.Equal(instance.Names, result.Names);
            Assert.That.Equal(instance.Empty, result.Empty);
            Assert.That.Equal(instance.Structs.Count, result.Structs.Count);
            for (int i = 0; i < instance.Structs.Count; ++i)
                Assert.That.Equal(instance.Structs[i].SS, result.Structs[i].SS);
            Assert.That.Equal(instance.ReadOnlyList, result.ReadOnlyList);
            Assert.That.Equal(instance.List, result.List);
            Assert.That.Equal(instance.Collection, result.Collection);
            Assert.That.Equal(instance.Enumerable, result.Enumerable);
        }

        [StarDataType]
        class GenericMapType
        {
            [StarData] public Map<string,string> TextToText;
            [StarData] public Map<string, string> Empty;
            [StarData] public Map<string,StructContainer> TextToClass;
            [StarData] public Map<int,string> IntToText;
            [StarData] public Dictionary<string, string> Dictionary;
            [StarData] public IDictionary<string,string> Interface;
            [StarData] public IReadOnlyDictionary<string, string> ReadOnly;
        }

        [TestMethod]
        public void GenericMapTypes()
        {
            var instance = new GenericMapType()
            {
                TextToText = Map(("BuildingType","Aeroponics"), ("TechName","Foodstuffs")),
                Empty = Map<string,string>(),
                TextToClass = Map(("First", new StructContainer(27, "27")), ("Second", new StructContainer(42, "42"))),
                IntToText = Map((0,"Zero"), (1,"One"), (2,"Two")),
                Dictionary = Map(("Star","Betelgeuse"), ("Type","SuperGiant"), ("Color","Red")),
                Interface = Map(("EarthAlliance","StarFury"), ("Minbari","Nial")),
                ReadOnly = Map(("Name","Orion"), ("Type","Constellation")),
            };
            var result = SerDes(instance);
            Assert.That.Equal(instance.TextToText, result.TextToText);
            Assert.That.Equal(instance.Empty, result.Empty);
            Assert.That.Equal(instance.TextToClass.Count, result.TextToClass.Count);
            foreach (var kv in instance.TextToClass)
                Assert.AreEqual(instance.TextToClass[kv.Key].SS, result.TextToClass[kv.Key].SS);
            Assert.That.Equal(instance.IntToText, result.IntToText);
            Assert.That.Equal(instance.Dictionary, result.Dictionary);
            Assert.That.Equal(instance.Interface, result.Interface);
            Assert.That.Equal(instance.ReadOnly, result.ReadOnly);
        }

        [StarDataType]
        class ListOfArraysType
        {
            [StarData] public Array<string[]> ListOfArrays;
            [StarData] public Array<Array<string>> ListOfLists;
        }

        [TestMethod]
        public void ListOfArraysTypes()
        {
            var instance = new ListOfArraysType()
            {
                ListOfArrays = List(Arr("A", "B", "C"), Arr("D", "E", "F") ),
                ListOfLists = List(List("Regulus", "Orion", "Andromeda"), List("Orion", "Cassiopeia")),
            };
            var result = SerDes(instance);
            Assert.That.Equal(instance.ListOfArrays, result.ListOfArrays);
            Assert.That.Equal(instance.ListOfLists, result.ListOfLists);
        }

        [StarDataType]
        class ListOfMapsType
        {
            [StarData] public Array<Map<string, string>> ListOfMaps;
        }

        [TestMethod]
        public void ListOfMapsTypes()
        {
            var instance = new ListOfMapsType()
            {
                ListOfMaps = List(
                    Map(("Regulus", "Star"), ("Andromeda", "Galaxy")),
                    Map(("Orion", "Constellation"), ("Cassiopeia", "Constellation"))
                ),
            };
            var result = SerDes(instance);
            Assert.That.Equal(instance.ListOfMaps, result.ListOfMaps);
        }

        [StarDataType]
        class MapOfArraysType
        {
            [StarData] public Map<string, string[]> MapOfArrays;
            [StarData] public Map<string, Array<string>> MapOfLists;
        }

        [TestMethod]
        public void MapOfArraysTypes()
        {
            var instance = new MapOfArraysType()
            {
                MapOfArrays = Map(
                    ("Regulus1", Arr("Star1", "Quite ordinary")),
                    ("Orion1", Arr("Constellation1", "Dog Star")),
                    ("Cassiopeia1", Arr("Constellation2", "W"))
                ),
                MapOfLists = Map(
                    ("Regulus2", List("Star11", "Quite ordinary1")),
                    ("Orion2", List("Star21", "Aliens1")),
                    ("Cassiopeia2", List("Constellation21", "W1"))
                ),
            };
            var result = SerDes(instance);
            Assert.That.Equal(instance.MapOfArrays, result.MapOfArrays);
            Assert.That.Equal(instance.MapOfLists, result.MapOfLists);
        }

        [StarDataType]
        class MapOfMapsType
        {
            [StarData] public Map<string, Map<string, string>> MapOfMaps;
            [StarData] public Map<string, Map<string, Map<string, string>>> MapOfMapsOfMaps;
        }

        [TestMethod]
        public void MapOfMapsTypes()
        {
            var instance = new MapOfMapsType()
            {
                MapOfMaps = Map(
                    ("Stars", Map(("Regulus", "Star"), ("Andromeda", "Galaxy"))),
                    ("Constellations", Map(("Orion", "Constellation"), ("Cassiopeia", "Constellation")))
                ),
                MapOfMapsOfMaps = Map(
                (
                    "Stars",
                    Map(
                        ("Regulus", Map(("Star", "BlueR"))),
                        ("Andromeda", Map(("Galaxy", "BlueG")))
                    )
                ),
                (
                    "Constellations",
                    Map(
                        ("Orion", Map(("Constellation", "BlueO"))),
                        ("Cassiopeia", Map(("Constellation", "BlueW")))
                    )
                )),
            };
            var result = SerDes(instance);
            Assert.That.Equal(instance.MapOfMaps, result.MapOfMaps);
            Assert.That.Equal(instance.MapOfMapsOfMaps, result.MapOfMapsOfMaps);
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
            var result = SerDes(instance);
            Assert.AreEqual(instance.TestText, result.TestText);

            Assert.AreEqual(result.RecursiveType, result.RecursiveType.RecursiveSelf);
            Assert.AreEqual(instance.RecursiveType.Text, result.RecursiveType.Text);
            Assert.AreEqual(instance.RecursiveType.Count, result.RecursiveType.Count);

            Assert.That.Equal(instance.Names, result.Names);
            Assert.AreEqual(instance.SCont.SS, result.SCont.SS);

            Assert.AreEqual(instance.Number, result.Number);
            Assert.AreEqual(instance.Struct, result.Struct);

            Assert.AreEqual(instance.Structs.Count, result.Structs.Count);
            for (int i = 0; i < instance.Structs.Count; ++i)
            {
               Assert.AreEqual(instance.Structs[i].SS, result.Structs[i].SS);
            }

            Assert.AreEqual("Subtype0", result.ComplexTypes[0].TestText);
            Assert.AreEqual("Subtype1", result.ComplexTypes[1].TestText);

            Assert.AreEqual("Subtype0", result.ComplexTypesArr[0].TestText);
            Assert.AreEqual("Subtype1", result.ComplexTypesArr[1].TestText);

            Assert.AreEqual(1, result.Map["Key1"]);
            Assert.AreEqual(2, result.Map["Key2"]);
            Assert.AreEqual(1, result.ComplexTypes[0].Map["Key1"]);
            Assert.AreEqual(2, result.ComplexTypes[0].Map["Key2"]);
            Assert.AreEqual(1, result.ComplexTypes[1].Map["Key1"]);
            Assert.AreEqual(2, result.ComplexTypes[1].Map["Key2"]);
        }

        static string CreateByteStreamForDeletedTypeTest(object instance)
        {
            var ser = new BinarySerializer(instance.GetType());
            byte[] bytes = Serialize(ser, instance);
            return Convert.ToBase64String(bytes, Base64FormattingOptions.None);
        }

        // FROM HERE
        //[StarDataType] class MovedType { [StarData] public Vector4 Value4; }
        [StarDataType]
        class ContainsMovedType
        {
            [StarData] public Vector3 Pos;
            [StarData] public MovedType MT;
            [StarData] public string Name;
            // MOVED TO HERE:
            [StarDataType] public class MovedType { [StarData] public Vector4 Value4; }
        }

        // Handles the case where a Type is simply moved from one namespace/class to another
        [TestMethod]
        public void ContainsMovedTypes()
        {
            string containsMovedType = "Tz8vHwEAAgAhAwEBCVVuaXRUZXN0cwEtVW5pdFRlc3RzLlNlcmlhbGl6YXRpb24uQmluYXJ5U2VyaWFsaXplclRlc3RzAhFDb250YWluc01vdmVkVHlwZQlNb3ZlZFR5cGUEAk1UBE5hbWUDUG9zBlZhbHVlNCAAAAAFAw0CIQAVASEAAAEFAQ4DFQEgASEBFQEVQ29udGFpbnMgYSBtb3ZlZCB0eXBlIAENAAAg+kQAQPpEAGD6RCEBAxUCASEBDgAAEHpFACB6RQAwekUAQHpF";

            //containsMovedType = CreateByteStreamForDeletedTypeTest(new ContainsMovedType
            //{
            //    Pos = new Vector3(2001, 2002, 2003),
            //    MT = new MovedType { Value4 = new Vector4(4001, 4002, 4003, 4004) },
            //    Name = "Contains a moved type",
            //});
            //Log.Write("\"" + containsMovedType + "\";");

            var ser = new BinarySerializer(typeof(ContainsMovedType));
            var result = Deserialize<ContainsMovedType>(ser, Convert.FromBase64String(containsMovedType));
            Assert.AreEqual(new Vector3(2001, 2002, 2003), result.Pos);
            Assert.AreEqual(new Vector4(4001, 4002, 4003, 4004), result.MT.Value4);
            Assert.AreEqual("Contains a moved type", result.Name);
        }

        // DELETED FROM HERE
        //[StarDataType] class DeletedType { [StarData] public Vector4 Value4; }
        //[StarDataType] struct DeletedStruct { [StarData] public Vector4 Value4; }

        [StarDataType]
        class ContainsDeletedType
        {
            [StarData] public Vector3 Pos;
            //[StarData] public DeletedType DT;
            //[StarData] public DeletedStruct DS;
            [StarData] public string Name;
        }

        // Handles the case where a field and its type are completely deleted
        // In such a case, the fields should simply be skipped without corrupting other data
        [TestMethod]
        public void ContainsDeletedTypes()
        {
            string containsDeletedType = "Tz8vHwEAAwAiAwEBCVVuaXRUZXN0cwEtVW5pdFRlc3RzLlNlcmlhbGl6YXRpb24uQmluYXJ5U2VyaWFsaXplclRlc3RzAxNDb250YWluc0RlbGV0ZWRUeXBlDURlbGV0ZWRTdHJ1Y3QLRGVsZXRlZFR5cGUFAkRTAkRUBE5hbWUDUG9zBlZhbHVlNCIAAAEEAQ4EIAAAAAUEDQMhASIAFQIhAAACBQEOBBUBIAEhARUBFkNvbnRhaW5zIGRlbGV0ZWQgdHlwZXMgAQ0AACD6RABA+kQAYPpEIQEDIgIOAABInEUAUJxFAFicRQBgnEUVAwEhAQ4AABB6RQAgekUAMHpFAEB6RQ==";

            //containsDeletedType = CreateByteStreamForDeletedTypeTest(new ContainsDeletedType
            //{
            //    Pos = new Vector3(2001, 2002, 2003),
            //    DT = new DeletedType { Value4 = new Vector4(4001, 4002, 4003, 4004) },
            //    DS = new DeletedStruct { Value4 = new Vector4(5001, 5002, 5003, 5004) },
            //    Name = "Contains deleted types",
            //});
            //Log.Write("\"" + containsDeletedType + "\";");

            var ser = new BinarySerializer(typeof(ContainsDeletedType));
            var result = Deserialize<ContainsDeletedType>(ser, Convert.FromBase64String(containsDeletedType));
            Assert.AreEqual(new Vector3(2001, 2002, 2003), result.Pos);
            Assert.AreEqual("Contains deleted types", result.Name);
        }

        [StarDataType]
        class ContainsRemovedFieldType
        {
            [StarData] public Vector3 Pos;
            //[StarData] public RecursiveType Removed;
            [StarData] public string Name;
            [StarData] public Vector2 Pos2;
        }

        // Handles the case where a field is simply removed
        // So the data has to be skipped
        [TestMethod]
        public void ContainsRemovedFieldTypes()
        {
            string containsRemovedField = "Tz8vHwEAAgAhAwIBCVVuaXRUZXN0cwEtVW5pdFRlc3RzLlNlcmlhbGl6YXRpb24uQmluYXJ5U2VyaWFsaXplclRlc3RzAhhDb250YWluc1JlbW92ZWRGaWVsZFR5cGUNUmVjdXJzaXZlVHlwZQgFQ291bnQQRGVmYXVsdElzTm90TnVsbAROYW1lA1BvcwRQb3MyDVJlY3Vyc2l2ZVNlbGYHUmVtb3ZlZARUZXh0IAAAAAUEDQMhBhUCDAQhAAABBQQhBRUHBgAVARUCIAEhARUCD1dpbGwgYmUgcmVtb3ZlZBhDb250YWlucyBhIHJlbW92ZWQgZmllbGQgAQ0AACD6RABA+kQAYPpEIQEEFQICDAMAoHpFAEB7RSEBIQAEFQEBBgKSExUDAA==";

            //containsRemovedField = CreateByteStreamForDeletedTypeTest(new ContainsRemovedFieldType
            //{
            //    Pos = new Vector3(2001, 2002, 2003),
            //    Removed = new RecursiveType("Will be removed", 1234),
            //    Name = "Contains a removed field",
            //    Pos2 = new Vector2(4010, 4020),
            //});
            //Log.Write("\"" + containsRemovedField + "\";");

            var ser = new BinarySerializer(typeof(ContainsRemovedFieldType));
            var result = Deserialize<ContainsRemovedFieldType>(ser, Convert.FromBase64String(containsRemovedField));
            Assert.AreEqual(new Vector3(2001, 2002, 2003), result.Pos);
            Assert.AreEqual("Contains a removed field", result.Name);
            Assert.AreEqual(new Vector2(4010, 4020), result.Pos2);
        }

        [StarDataType(TypeName = "ThisTypeNameDoesNotReflectTheRealProduct")]
        class CustomTypeNameType
        {
            [StarData] public Vector3 Pos;
        }

        [TestMethod]
        public void CustomTypeNameTypes()
        {
            var instance = new CustomTypeNameType{ Pos = new Vector3(1001, 1002, 1003) };
            var result = SerDes(instance);
            Assert.AreEqual(instance.Pos, result.Pos);
        }

        [StarDataType]
        class FieldNameRemapType
        {
            [StarData(NameId = "RenamedPosition")] public Vector3 Pos;
        }

        [TestMethod]
        public void FieldNameRemapTypes()
        {
            var instance = new FieldNameRemapType { Pos = new Vector3(1001, 1002, 1003) };
            var result = SerDes(instance);
            Assert.AreEqual(instance.Pos, result.Pos);
        }

        //[TestMethod]
        //public void SerializeAShip()
        //{
        //    CreateUniverseAndPlayerEmpire();
        //    Ship ship = SpawnShip("Vulcan Scout", Player, Vector2.Zero);
        //    byte[] bytes = Serialize(ship);
        //}

        public enum NestedEnum
        {
            One,
            Two,
            Three,
        }

        [Flags]
        public enum FlagsEnum
        {
            None,
            Flag1 = 8,
            Flag2 = 16,
            Flag3 = 32,
            Flag4 = 64,
        }

        [Flags]
        public enum FlagsEnumByte : byte
        {
            None,
            Flag1 = 8,
            Flag2 = 16,
            Flag3 = 32,
            Flag4 = 64,
        }

        [StarDataType]
        public class EnumType
        {
            public enum NestedEnum2
            {
                Seven, Eight, Nine
            }

            [StarData] public NestedEnum Key1;
            [StarData] public GlobalEnum Key2;
            [StarData] public NestedEnum2 Key3;
            [StarData] public FlagsEnum Key4;
            [StarData] public FlagsEnumByte Key5;
            [StarData] public Array<NestedEnum> Values1;
            [StarData] public Array<GlobalEnum> Values2;
            [StarData] public Array<NestedEnum2> Values3;
            [StarData] public Array<FlagsEnum> Values4;
            [StarData] public Array<FlagsEnumByte> Values5;
        }

        [TestMethod]
        public void EnumTypes()
        {
            var instance = new EnumType
            {
                Key1 = NestedEnum.Three,
                Key2 = GlobalEnum.Five,
                Key3 = EnumType.NestedEnum2.Eight,
                Key4 = FlagsEnum.Flag1|FlagsEnum.Flag2,
                Key5 = FlagsEnumByte.Flag1|FlagsEnumByte.Flag2,
                Values1 = List(NestedEnum.Three,NestedEnum.One,NestedEnum.Two),
                Values2 = List(GlobalEnum.Six,GlobalEnum.Four,GlobalEnum.Five),
                Values3 = List(EnumType.NestedEnum2.Seven,EnumType.NestedEnum2.Nine,EnumType.NestedEnum2.Eight),
                Values4 = List(FlagsEnum.Flag1|FlagsEnum.Flag2, FlagsEnum.Flag4, FlagsEnum.Flag3|FlagsEnum.Flag4),
                Values5 = List(FlagsEnumByte.Flag1|FlagsEnumByte.Flag2, FlagsEnumByte.Flag4, FlagsEnumByte.Flag3|FlagsEnumByte.Flag4),
            };
            var result = SerDes(instance);
            Assert.AreEqual(instance.Key1, result.Key1);
            Assert.AreEqual(instance.Key2, result.Key2);
            Assert.AreEqual(instance.Key3, result.Key3);
            Assert.AreEqual(instance.Key4, result.Key4);
            Assert.AreEqual(instance.Key5, result.Key5);
            Assert.That.EqualCollections(instance.Values1, result.Values1);
            Assert.That.EqualCollections(instance.Values2, result.Values2);
            Assert.That.EqualCollections(instance.Values3, result.Values3);
            Assert.That.EqualCollections(instance.Values4, result.Values4);
            Assert.That.EqualCollections(instance.Values5, result.Values5);
        }

        [StarDataType]
        public class NestedClass1
        {
            [StarData] public NestedClass2 Nested;
            
            [StarDataType]
            public class NestedClass2
            {
                [StarData] public string Name;
            }
        }

        [TestMethod]
        public void SupportsMultipleNestedClasses()
        {
            var instance = new NestedClass1
            {
                Nested = new NestedClass1.NestedClass2{ Name = "Nested2" }
            };
            var result = SerDes(instance);
            Assert.AreEqual(instance.Nested.Name, result.Nested.Name);
        }

        [TestMethod]
        public void CanSerializeShipDesign()
        {
            var instance = (ShipDesign)ResourceManager.Ships.GetDesign("Terran-Prototype");

            var designBytes = instance.GetDesignBytes(new ShipDesignWriter());
            var result = SerDes(instance, out byte[] bytes, verbose:true);
            Log.Info($"ShipDesign Binary={bytes.Length} DesignBytes={designBytes.Length}");

            ShipDataTests.AssertAreEqual(instance, result, checkModules:true);
        }

        [StarDataType]
        class SavesContainer
        {
            [StarData] public ShipDesign[] Designs;
        }

        [TestMethod]
        public void CanSerializeAllShipDesigns()
        {
            Setup();

            ShipDesign[] designs = ResourceManager.Ships.Designs.Select(s => s as ShipDesign);

            var t1 = new PerfTimer();
            var sw = new ShipDesignWriter();
            int textBytes = 0;
            foreach (ShipDesign design in designs)
            {
                var designBytes = design.GetDesignBytes(sw);
                textBytes += designBytes.Length;
            }
            double e1 = t1.Elapsed;

            var t2 = new PerfTimer();
            var instances = new SavesContainer { Designs = designs };
            var ser = new BinarySerializer(typeof(SavesContainer));
            byte[] bytes = Serialize(ser, instances);
            double e2 = t2.Elapsed;
            var saves = Deserialize<SavesContainer>(ser, bytes);
            Log.Write($"ShipDesigns {designs.Length} Binary={bytes.Length} Text={textBytes}");
            Log.Write($"Binary.Elapsed={e2*1000:0.00}ms Text.Elapsed={e1*1000:0.00}ms");

            for (int i = 0; i < designs.Length; i++)
            {
                ShipDataTests.AssertAreEqual(designs[i], saves.Designs[i], checkModules:true);
            }
        }

        [StarDataType]
        class HeaderType
        {
            [StarData] public int Version;
            [StarData] public string Name;
        }

        [StarDataType]
        class PayloadType
        {
            [StarData] public string Data;
            [StarData] public Vector2 Size;
        }

        // this tests whether we can aggregate two completely different types into a single binary file
        // and then read it back partially or completely without issues
        // For savegames we plan to store tiny header object in front and then
        // the huge payload which doesn't need to be parsed at all
        [TestMethod]
        public void MultiTypeSerialize()
        {
            var msOut = new MemoryStream();
            var writer = new Writer(msOut);

            var header = new HeaderType { Version = 11, Name = "Savegame1" };
            var payload = new PayloadType { Data = "123456", Size = new Vector2(1000,1000) };

            BinarySerializer.SerializeMultiType(writer, new object[]{ header, payload });

            var msIn = new MemoryStream(msOut.ToArray());
            var reader = new Reader(msIn);

            var results1 = BinarySerializer.DeserializeMultiType(reader, new[]{ typeof(HeaderType) });
            Assert.AreEqual(1, results1.Length);
            Assert.AreEqual(typeof(HeaderType), results1[0].GetType());

            var header1 = (HeaderType)results1[0];
            Assert.AreEqual(header.Version, header1.Version);
            Assert.AreEqual(header.Name, header1.Name);

            reader.BaseStream.Position = 0;
            var results2 = BinarySerializer.DeserializeMultiType(reader, new[]{ typeof(HeaderType), typeof(PayloadType) });
            Assert.AreEqual(2, results2.Length);
            Assert.AreEqual(typeof(HeaderType), results2[0].GetType());
            Assert.AreEqual(typeof(PayloadType), results2[1].GetType());

            var header2 = (HeaderType)results2[0];
            Assert.AreEqual(header.Version, header2.Version);
            Assert.AreEqual(header.Name, header2.Name);

            var payload2 = (PayloadType)results2[1];
            Assert.AreEqual(payload.Data, payload2.Data);
            Assert.AreEqual(payload.Size, payload2.Size);
        }

        bool LoadedExtraData;

        void Setup()
        {
            LoadedExtraData = true;
            Directory.CreateDirectory(SavedGame.DefaultSaveGameFolder);
            ScreenManager.Instance.UpdateGraphicsDevice(); // create SpriteBatch
            GlobalStats.AsteroidVisibility = ObjectVisibility.None; // dont create Asteroid SO's

            ResourceManager.UnloadAllData(ScreenManager.Instance);
            ResourceManager.LoadItAll(ScreenManager.Instance, null);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (LoadedExtraData)
            {
                LoadedExtraData = false;
                ResourceManager.UnloadAllData(ScreenManager.Instance);
                StarDriveTestContext.LoadStarterContent();
                GlobalStats.AsteroidVisibility = ObjectVisibility.Rendered;
            }
        }

        // this is the actual big test for SavedGame
        [TestMethod]
        public void SavedGameSerialize()
        {
            Setup();

            CreateCustomUniverseSandbox(numOpponents: 6, galSize: GalSize.Large);
            Universe.SingleSimulationStep(TestSimStep);

            int numShips = Universe.UState.Empires[0].OwnedShips.Count;
            float firstShipHealth = Universe.UState.Empires[0].OwnedShips[0].Health;

            var save = new SavedGame(Universe);
            save.Verbose = true;
            save.Save("BinarySerializer.Test", async:false);
            Universe.ExitScreen();

            // peek the header as per specs
            HeaderData header = LoadGame.PeekHeader(save.SaveFile);
            Assert.AreEqual(SavedGame.SaveGameVersion, header.Version);
            Assert.AreEqual("BinarySerializer.Test", header.SaveName);

            var load = new LoadGame(save.SaveFile);
            load.Verbose = true;
            UniverseScreen us = load.Load(noErrorDialogs:true, startSimThread:false);
            Assert.IsNotNull(us, "Loaded universe cannot be null");
            us.SingleSimulationStep(TestSimStep);

            Assert.AreEqual(numShips, us.UState.Empires[0].OwnedShips.Count, "Empire should have same # of ships");
            Assert.AreEqual(firstShipHealth, us.UState.Empires[0].OwnedShips[0].Health, "Ships should have health");
        }
    }

    // Global enums and Nested enums have different resolution rules
    public enum GlobalEnum
    {
        Four, Five, Six
    }

    [StarDataType]
    public class GlobalUserType
    {
        [StarData] public GlobalEnum Enum1;
    }
}
