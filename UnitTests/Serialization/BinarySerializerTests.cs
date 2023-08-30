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
using UnitTests.Ships;
using Vector4 = SDGraphics.Vector4;
using Point = SDGraphics.Point;
using ResourceManager = Ship_Game.ResourceManager;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Vector2d = SDGraphics.Vector2d;
using Vector3d = SDGraphics.Vector3d;
using Ship_Game.Universe;
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
            var ser = new BinarySerializer(instance);
            bytes = Serialize(ser, instance, verbose);
            return Deserialize<T>(ser, bytes, verbose);
        }

        public static T SerDes<T>(T instance, bool verbose = false)
        {
            return SerDes(instance, out _, verbose);
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
            AssertEqual(result.IntZero, (int)0);
            AssertEqual(result.IntMin, int.MinValue);
            AssertEqual(result.IntMax, int.MaxValue);
            AssertEqual(result.UIntZero, (uint)0);
            AssertEqual(result.UIntMin, uint.MinValue);
            AssertEqual(result.UIntMax, uint.MaxValue);
            AssertEqual(result.LongZero, (long)0);
            AssertEqual(result.LongMin, long.MinValue);
            AssertEqual(result.LongMax, long.MaxValue);
            AssertEqual(result.ULongZero, (ulong)0);
            AssertEqual(result.ULongMin, ulong.MinValue);
            AssertEqual(result.ULongMax, ulong.MaxValue);
            AssertEqual(result.ShortZero, (short)0);
            AssertEqual(result.ShortMin, short.MinValue);
            AssertEqual(result.ShortMax, short.MaxValue);
            AssertEqual(result.UShortZero, (ushort)0);
            AssertEqual(result.UShortMin, ushort.MinValue);
            AssertEqual(result.UShortMax, ushort.MaxValue);
            AssertEqual(result.SByteZero, (sbyte)0);
            AssertEqual(result.SByteMin, sbyte.MinValue);
            AssertEqual(result.SByteMax, sbyte.MaxValue);
            AssertEqual(result.ByteZero, (byte)0);
            AssertEqual(result.ByteMin, byte.MinValue);
            AssertEqual(result.ByteMax, byte.MaxValue);
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
            AssertEqual(result.FloatZero, 0.0f);
            AssertEqual(result.FloatMin, float.MinValue);
            AssertEqual(result.FloatMax, float.MaxValue);
            AssertEqual(result.DoubleZero, 0.0);
            AssertEqual(result.DoubleMin, double.MinValue);
            AssertEqual(result.DoubleMax, double.MaxValue);
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
            AssertEqual(result.Vector2Zero, Vector2.Zero);
            AssertEqual(result.Vector2Min, new Vector2(-1,-2));
            AssertEqual(result.Vector2Max, new Vector2(1,2));
            AssertEqual(result.Vector3Zero, Vector3.Zero);
            AssertEqual(result.Vector3Min, new Vector3(-3,-4,-5));
            AssertEqual(result.Vector3Max, new Vector3(3,4,5));
            AssertEqual(result.Vector4Zero, Vector4.Zero);
            AssertEqual(result.Vector4Min, new Vector4(-6,-7,-8,-9));
            AssertEqual(result.Vector4Max, new Vector4(6,7,8,9));
            AssertEqual(result.Vector2dZero, Vector2d.Zero);
            AssertEqual(result.Vector2dMin, new Vector2d(-11,-12));
            AssertEqual(result.Vector2dMax, new Vector2d(11,12));
            AssertEqual(result.Vector3dZero, Vector3d.Zero);
            AssertEqual(result.Vector3dMin, new Vector3d(-13,-14,-15));
            AssertEqual(result.Vector3dMax, new Vector3d(13,14,15));

            AssertEqual(result.PointZero, Point.Zero);
            AssertEqual(result.PointMin, new Point(-16, -17));
            AssertEqual(result.PointMax, new Point(16, 17));
            AssertEqual(result.RectZero, Rectangle.Empty);
            AssertEqual(result.RectMin, new Rectangle(-30,-31,-32,-33));
            AssertEqual(result.RectMax, new Rectangle(30,31,32,33));
            AssertEqual(result.RectFZero, RectF.Empty);
            AssertEqual(result.RectFMin, new RectF(-40,-41,-42,-43));
            AssertEqual(result.RectFMax, new RectF(40,41,42,43));

            AssertEqual(result.ColorZero, new Color(0, 0, 0));
            AssertEqual(result.ColorMin, new Color(21, 22, 23));
            AssertEqual(result.ColorMax, new Color(245, 250, 255));
            AssertEqual(result.RangeZero, new Range());
            AssertEqual(result.RangeMin, new Range(-24, -25));
            AssertEqual(result.RangeMax, new Range(24, 25));
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

            AssertEqual(instance.StrNull, result.StrNull);
            AssertEqual(instance.StrEmpty, result.StrEmpty);
            AssertEqual(instance.StrShort, result.StrShort);
            AssertEqual(instance.StrLong, result.StrLong);

            AssertEqual(instance.LocId, result.LocId);
            AssertEqual(instance.LocNameId, result.LocNameId);
            AssertEqual(instance.LocRawText, result.LocRawText);
            AssertEqual(instance.LocParse, result.LocParse);
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
            AssertEqual(result.TimeZero, TimeSpan.Zero);
            AssertEqual(result.TimeMin, new TimeSpan(10000));
            AssertEqual(result.TimeMax, new TimeSpan(1000000000L));
            AssertEqual(result.DateTimeMin, DateTime.MinValue);
            AssertEqual(result.DateTimeMax, DateTime.MaxValue);
            AssertEqual(result.DateTimeNow, instance.DateTimeNow);
        }

        [StarDataType]
        class RecursiveAtDepth1
        {
            [StarData] public RecursiveType Owner;
        }

        [StarDataType]
        class RecursiveType
        {
            [StarData] public readonly RecursiveType RecursiveSelf;
            [StarData] public readonly string Text;
            [StarData] public readonly int Count;
            [StarData] public readonly string DefaultIsNotNull = "Default is not null";
            [StarData] public readonly RecursiveAtDepth1 AtDepth1;
            public RecursiveType() {}
            public RecursiveType(string text, int count)
            {
                Text = text;
                Count = count;
                RecursiveSelf = this;
                DefaultIsNotNull = null; // override the default
                AtDepth1 = new() { Owner = this };
            }
        }

        [TestMethod]
        public void RecursiveTypes()
        {
            var instance = new RecursiveType("Hello", 42);
            var result = SerDes(instance);
            AssertEqual(result, result.RecursiveSelf, "Recursive self reference must match");
            AssertEqual(instance.Text, result.Text, "string must match");
            AssertEqual(instance.Count, result.Count, "int field must match");
            AssertEqual(null, result.DefaultIsNotNull, "null field must match");
            AssertEqual(result, result.AtDepth1.Owner, "Recursive self reference at depth 1 must match");
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

        static void AreEqual(in SmallStruct expected, in SmallStruct actual, string message = null)
        {
            AssertEqual(expected.Id, actual.Id, $"Nested SmallStruct Id fields must match. {message}");
            AssertEqual(expected.Name, actual.Name, $"Nested SmallStruct Name fields must match. {message}");
        }

        [StarDataType]
        class StructContainer : IEquatable<StructContainer>
        {
            [StarData] public SmallStruct SS;

            public StructContainer() {}
            public StructContainer(int id, string name)
            {
                SS = new SmallStruct(id, name);
            }
            public override int GetHashCode() => SS.GetHashCode();
            public override bool Equals(object obj) => Equals((StructContainer)obj);
            public bool Equals(StructContainer other)
            {
                if (other is null) return false;
                return ReferenceEquals(this, other)
                    || SS.Id == other.SS.Id && SS.Name == other.SS.Name;
            }
        }

        [TestMethod]
        public void NestedUserTypeStruct()
        {
            var instance = new StructContainer(15, "Laika");
            var result = SerDes(instance);
            AreEqual(instance.SS, result.SS);
        }

        static T[] Arr<T>(params T[] elements) => elements;
        static Array<T> List<T>(params T[] elements) => new(elements);
        static Map<K,V> Map<K,V>(params ValueTuple<K, V>[] elements) => new(elements);
        static HashSet<T> Set<T>(params T[] elements) => new(elements);

        [StarDataType]
        class ContainsRawArrays
        {
            [StarData] public int[] Integers;
            [StarData] public Vector2[] Points;
            [StarData] public string[] Names;
            [StarData] public string[] Empty;
            [StarData] public StructContainer[] Structs;

            // Special array handlers
            [StarData] public byte[] Bytes;

            // A special edge-case which is difficult to handle
            [StarData] public StructWithArrays Struct;

            [StarDataType]
            public struct StructWithArrays
            {
                [StarData] public int[] Integers;
                [StarData] public StructContainer[] Structs;
            }
        }

        [TestMethod]
        public void RawArrayTypes()
        {
            var instance = new ContainsRawArrays()
            {
                Integers = Arr(17, 19, 56, 123, 57),
                Points = Arr(Vector2.One, Vector2.UnitX, Vector2.UnitY),
                Names = Arr("Laika", "Strelka", "Bobby", "Rex", "Baron"),
                Empty = new string[0],
                Structs = Arr(new StructContainer(27, "27"), new StructContainer(42, "42")),
                Bytes = new byte[]{ 100, 200, 255, 115, 143, 0, 42 },
                Struct = new()
                {
                    Integers = Arr(23, 49, 95, 495, 945),
                    Structs = Arr(new StructContainer(45, "45")),
                }
            };
            var result = SerDes(instance, verbose:true);
            AssertEqual(instance.Integers, result.Integers);
            AssertEqual(instance.Points, result.Points);
            AssertEqual(instance.Names, result.Names);
            AssertEqual(instance.Empty, result.Empty);
            AssertEqual(instance.Structs, result.Structs);
            AssertEqual(instance.Bytes, result.Bytes);
            AssertEqual(instance.Struct.Integers, result.Struct.Integers);
            AssertEqual(instance.Struct.Structs, result.Struct.Structs);
        }

        [StarDataType]
        class ArrayOfT_Type
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
        public void ArrayOfT_Types()
        {
            var instance = new ArrayOfT_Type()
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
            AssertEqual(instance.Integers, result.Integers);
            AssertEqual(instance.Points, result.Points);
            AssertEqual(instance.Names, result.Names);
            AssertEqual(instance.Empty, result.Empty);
            AssertEqual(instance.Structs, result.Structs);
            AssertEqual(instance.ReadOnlyList, result.ReadOnlyList);
            AssertEqual(instance.List, result.List);
            AssertEqual(instance.Collection, result.Collection);
            AssertEqual(instance.Enumerable, result.Enumerable);
        }

        [StarDataType]
        public class MyTech : IEquatable<MyTech>
        {
            [StarData] public string UID;
            [StarData] public bool Unlocked;

            public bool Equals(MyTech other) => UID == other?.UID;
            public override bool Equals(object obj) => Equals((MyTech)obj);
            public override int GetHashCode() => UID.GetHashCode();
        }

        MyTech Tech(string uid, bool unlocked) => new(){ UID = uid, Unlocked = unlocked };

        [StarDataType]
        class MapType
        {
            [StarData] public Map<string,string> TextToText;
            [StarData] public Map<string, string> Empty;
            [StarData] public Map<string,StructContainer> TextToClass;
            [StarData] public Map<int,string> IntToText;
            [StarData] public Dictionary<string, string> Dictionary;
            [StarData] public IDictionary<string,string> Interface;
            [StarData] public IReadOnlyDictionary<string, string> ReadOnly;
            [StarData] public Map<string, MyTech> TechDict1;
            [StarData] public Map<string, MyTech> TechDict2;
        }

        [TestMethod]
        public void MapTypes()
        {
            var instance = new MapType()
            {
                TextToText = Map(("BuildingType","Aeroponics"), ("TechName","Foodstuffs")),
                Empty = Map<string,string>(),
                TextToClass = Map(("First", new StructContainer(27, "27")), ("Second", new(42, "42"))),
                IntToText = Map((0,"Zero"), (1,"One"), (2,"Two")),
                Dictionary = Map(("Star","Betelgeuse"), ("Type","SuperGiant"), ("Color","Red")),
                Interface = Map(("EarthAlliance","StarFury"), ("Minbari","Nial")),
                ReadOnly = Map(("Name","Orion"), ("Type","Constellation")),
                TechDict1 = Map(("Aeroponics",Tech("Aeroponics",true)), ("RoverBay",Tech("RoverBay",false))),
                TechDict2 = Map(("Aeroponics",Tech("Aeroponics",true)), ("RoverBay",Tech("RoverBay",false))),
            };
            var result = SerDes(instance);
            AssertEqual(instance.TextToText, result.TextToText);
            AssertEqual(instance.Empty, result.Empty);
            AssertEqual(instance.TextToClass.Count, result.TextToClass.Count);
            foreach (var kv in instance.TextToClass)
                AreEqual(instance.TextToClass[kv.Key].SS, result.TextToClass[kv.Key].SS);
            AssertEqual(instance.IntToText, result.IntToText);
            AssertEqual(instance.Dictionary, result.Dictionary);
            AssertEqual(instance.Interface, result.Interface);
            AssertEqual(instance.ReadOnly, result.ReadOnly);
            AssertEqual(instance.TechDict1, result.TechDict1);
            AssertEqual(instance.TechDict2, result.TechDict2);

            // this is an issue with objects that inherit from IEquatable<T>
            // where they can be accidentally squashed
            Assert.IsFalse(ReferenceEquals(result.TechDict1["Aeroponics"], result.TechDict2["Aeroponics"]),
                           "Map deserializer should not accidentally squash objects");
        }

        [StarDataType]
        class HashSetType
        {
            [StarData] public HashSet<string> Empty;
            [StarData] public HashSet<int> Integers;
            [StarData] public HashSet<string> Strings;
            [StarData] public HashSet<SmallStruct> Structs;
            [StarData] public HashSet<HashSet<string>> SetOfSets;
        }

        [TestMethod]
        public void HashSetTypes()
        {
            var instance = new HashSetType()
            {
                Empty = new(),
                Integers = Set(1,2,3,4,5,6,7,8,9,10),
                Strings = Set("BuildingType","Aeroponics","TechName","Foodstuffs"),
                Structs = Set(new SmallStruct(27, "27"), new SmallStruct(42, "42")),
                SetOfSets = Set(Set("A","B","C"), Set("D","E","F")),
            };
            var result = SerDes(instance);
            AssertEqual(instance.Empty, result.Empty);
            AssertEqual(instance.Integers, result.Integers);
            AssertEqual(instance.Strings, result.Strings);
            AssertEqual(instance.Structs, result.Structs);
            AssertEqual(instance.SetOfSets, result.SetOfSets);
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
            AssertEqual(instance.ListOfArrays, result.ListOfArrays);
            AssertEqual(instance.ListOfLists, result.ListOfLists);
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
            AssertEqual(instance.ListOfMaps, result.ListOfMaps);
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
            AssertEqual(instance.MapOfArrays, result.MapOfArrays);
            AssertEqual(instance.MapOfLists, result.MapOfLists);
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
            AssertEqual(instance.MapOfMaps, result.MapOfMaps);
            AssertEqual(instance.MapOfMapsOfMaps, result.MapOfMapsOfMaps);
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
            AssertEqual(instance.TestText, result.TestText);

            AssertEqual(result.RecursiveType, result.RecursiveType.RecursiveSelf);
            AssertEqual(instance.RecursiveType.Text, result.RecursiveType.Text);
            AssertEqual(instance.RecursiveType.Count, result.RecursiveType.Count);

            AssertEqual(instance.Names, result.Names);
            AssertEqual(instance.SCont.SS, result.SCont.SS);

            AssertEqual(instance.Number, result.Number);
            AreEqual(instance.Struct, result.Struct);
            AssertEqual(instance.Structs, result.Structs);

            AssertEqual("Subtype0", result.ComplexTypes[0].TestText);
            AssertEqual("Subtype1", result.ComplexTypes[1].TestText);

            AssertEqual("Subtype0", result.ComplexTypesArr[0].TestText);
            AssertEqual("Subtype1", result.ComplexTypesArr[1].TestText);

            AssertEqual(1, result.Map["Key1"]);
            AssertEqual(2, result.Map["Key2"]);
            AssertEqual(1, result.ComplexTypes[0].Map["Key1"]);
            AssertEqual(2, result.ComplexTypes[0].Map["Key2"]);
            AssertEqual(1, result.ComplexTypes[1].Map["Key1"]);
            AssertEqual(2, result.ComplexTypes[1].Map["Key2"]);
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
            string containsMovedType = "Ly8vLwEAAgAhBQUBCVVuaXRUZXN0cwEtVW5pdFRlc3RzLlNlcmlhbGl6YXRpb24uQmluYXJ5U2VyaWFsaXplclRlc3RzAhFDb250YWluc01vdmVkVHlwZQlNb3ZlZFR5cGUEAk1UBE5hbWUDUG9zBlZhbHVlNCEAAAEEAQ4DIAAAAAQDDQIhABUBDQEBDgECFQEDIQEEIAEFDQEAIPpEAED6RABg+kQOAQAQekUAIHpFADB6RQBAekUVARVDb250YWlucyBhIG1vdmVkIHR5cGUhAQACIAEAAQQD";

            //containsMovedType = CreateByteStreamForDeletedTypeTest(new ContainsMovedType
            //{
            //    Pos = new Vector3(2001, 2002, 2003),
            //    MT = new MovedType { Value4 = new Vector4(4001, 4002, 4003, 4004) },
            //    Name = "Contains a moved type",
            //});
            //Log.Write("\"" + containsMovedType + "\";");

            var ser = new BinarySerializer(typeof(ContainsMovedType));
            var result = Deserialize<ContainsMovedType>(ser, Convert.FromBase64String(containsMovedType));
            AssertEqual(new Vector3(2001, 2002, 2003), result.Pos);
            AssertEqual(new Vector4(4001, 4002, 4003, 4004), result.MT.Value4);
            AssertEqual("Contains a moved type", result.Name);
        }

        // DELETED FROM HERE
        //[StarDataType] class DeletedType { [StarData] public Vector4 Value4; }
        //[StarDataType] struct DeletedStruct { [StarData] public Vector4 Value4; }

        [StarDataType]
        class ContainsDeletedType
        {
            [StarData] public Vector3 Pos;
            //[StarData] public DeletedType DT; // this field deleted because of deleted type
            //[StarData] public DeletedStruct DS; // this field deleted because of deleted type
            [StarData] public string Name;
        }

        // Handles the case where a field and its type are completely deleted
        // In such a case, the fields should simply be skipped without corrupting other data
        [TestMethod]
        public void ContainsDeletedTypes()
        {
            string containsDeletedType = "Ly8vLwEAAwAiBgcBCVVuaXRUZXN0cwEtVW5pdFRlc3RzLlNlcmlhbGl6YXRpb24uQmluYXJ5U2VyaWFsaXplclRlc3RzAxNDb250YWluc0RlbGV0ZWRUeXBlDURlbGV0ZWRTdHJ1Y3QLRGVsZXRlZFR5cGUFAkRTAkRUBE5hbWUDUG9zBlZhbHVlNCIAAAEFAQ4EIQAAAgQBDgQgAAAABAQNAyEBIgAVAg0BAQ4CAhUBBCIBBSEBBiABBw0BACD6RABA+kQAYPpEDgIAEHpFACB6RQAwekUAQHpFAEicRQBQnEUAWJxFAGCcRRUBFkNvbnRhaW5zIGRlbGV0ZWQgdHlwZXMiAQADIQEAAiABAAEGBQQ=";

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
            AssertEqual(new Vector3(2001, 2002, 2003), result.Pos);
            AssertEqual("Contains deleted types", result.Name);
        }

        [StarDataType]
        class ContainsRemovedFieldType
        {
            [StarData] public Vector3 Pos;
            //[StarData] public RecursiveType Removed; // this field was removed in new version of the game
            [StarData] public string Name;
            [StarData] public Vector2 Pos2;
        }

        // Handles the case where a field is simply removed
        // So the data has to be skipped
        [TestMethod]
        public void ContainsRemovedFieldTypes()
        {
            string containsRemovedField = "Ly8vLwEAAwAiBwYBCVVuaXRUZXN0cwEtVW5pdFRlc3RzLlNlcmlhbGl6YXRpb24uQmluYXJ5U2VyaWFsaXplclRlc3RzAxhDb250YWluc1JlbW92ZWRGaWVsZFR5cGURUmVjdXJzaXZlQXREZXB0aDENUmVjdXJzaXZlVHlwZQoIQXREZXB0aDEFQ291bnQQRGVmYXVsdElzTm90TnVsbAROYW1lBU93bmVyA1BvcwRQb3MyDVJlY3Vyc2l2ZVNlbGYHUmVtb3ZlZARUZXh0IAAAAAQEDQUhCBUDDAYhAAACBAUhBxUJBgEVAiIAIgAAAQQBIQQGAQEMAQINAQMVAgQgAQYhAQciAQgGAZITDAEAoHpFAEB7RQ0BACD6RABA+kQAYPpEFQIPV2lsbCBiZSByZW1vdmVkGENvbnRhaW5zIGEgcmVtb3ZlZCBmaWVsZCABAAMHBQIhAQAHBAEACCIBAAc=";

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
            AssertEqual(new Vector3(2001, 2002, 2003), result.Pos);
            AssertEqual("Contains a removed field", result.Name);
            AssertEqual(new Vector2(4010, 4020), result.Pos2);
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
            AssertEqual(instance.Pos, result.Pos);
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
            AssertEqual(instance.Pos, result.Pos);
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
            AssertEqual(instance.Key1, result.Key1);
            AssertEqual(instance.Key2, result.Key2);
            AssertEqual(instance.Key3, result.Key3);
            AssertEqual(instance.Key4, result.Key4);
            AssertEqual(instance.Key5, result.Key5);
            AssertEqualCollections(instance.Values1, result.Values1);
            AssertEqualCollections(instance.Values2, result.Values2);
            AssertEqualCollections(instance.Values3, result.Values3);
            AssertEqualCollections(instance.Values4, result.Values4);
            AssertEqualCollections(instance.Values5, result.Values5);
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
            AssertEqual(instance.Nested.Name, result.Nested.Name);
        }

        [StarDataType]
        class VirtualBaseClass
        {
            // base class must be allowed to not mark this as [StarData]
            public virtual string Name { get; set; }
            [StarData] public string NonVirtual;
        }
        [StarDataType]
        class VirtualClassA : VirtualBaseClass
        {
            [StarData] public override string Name { get; set; }
        }
        [StarDataType]
        class ContainsVirtualTypes
        {
            [StarData] public VirtualBaseClass Virt;
        }

        [TestMethod]
        public void SupportsVirtualClasses()
        {
            var instance = new ContainsVirtualTypes
            {
                Virt = new VirtualClassA
                {
                    Name = "virtualname",
                    NonVirtual = "nonvirtual",
                }
            };
            var result = SerDes(instance);
            AssertEqual(instance.Virt.Name, result.Virt.Name);
            AssertEqual(instance.Virt.NonVirtual, result.Virt.NonVirtual);
        }


        [StarDataType]
        abstract class AbstractBaseClass
        {
            // base class must be allowed to not mark this as [StarData]
            public abstract string Name { get; set; }
            [StarData] public string NonVirtual;
        }
        [StarDataType]
        class AbstractClassA : AbstractBaseClass
        {
            [StarData] public override string Name { get; set; }
        }
        [StarDataType]
        class ContainsAbstractTypes
        {
            [StarData] public AbstractBaseClass Abstr;
        }

        [TestMethod]
        public void SupportsAbstractClasses()
        {
            var instance = new ContainsAbstractTypes
            {
                Abstr = new AbstractClassA
                {
                    Name = "abstractname",
                    NonVirtual = "nonvirtual",
                }
            };

            var result = SerDes(instance);
            AssertEqual(instance.Abstr.Name, result.Abstr.Name);
            AssertEqual(instance.Abstr.NonVirtual, result.Abstr.NonVirtual);
        }

        [StarDataType]
        class DefaultValues
        {
            [StarData] public bool Falsy = false;
            [StarData(DefaultValue=true)] public bool Truthy = true;

            [StarData] public float DefaultFloat = default;
            [StarData(DefaultValue=1f)] public float OneFloat = 1f;

            [StarData] public string DefaultString;
            [StarData(DefaultValue = "AssignedString")] public string AssignedString = "AssignedString";
        }

        // specific to DefaultValue optimization
        [TestMethod]
        public void DefaultValuesAreWrittenCorrectly()
        {
            var instance = new DefaultValues();
            var result = SerDes(instance);
            AssertEqual(instance.Falsy, result.Falsy);
            AssertEqual(instance.Truthy, result.Truthy);
            AssertEqual(instance.DefaultFloat, result.DefaultFloat);
            AssertEqual(instance.OneFloat, result.OneFloat);
            AssertEqual(instance.DefaultString, result.DefaultString);
            AssertEqual(instance.AssignedString, result.AssignedString);
        }

        [TestMethod]
        public void DefaultValuesAllAreNoneDefault()
        {
            var instance = new DefaultValues()
            {
                Falsy = true,
                Truthy = false,
                DefaultFloat = 13f,
                OneFloat = 12f,
                DefaultString = "abcd",
                AssignedString = "xyzw",
            };
            var result = SerDes(instance);
            AssertEqual(instance.Falsy, result.Falsy);
            AssertEqual(instance.Truthy, result.Truthy);
            AssertEqual(instance.DefaultFloat, result.DefaultFloat);
            AssertEqual(instance.OneFloat, result.OneFloat);
            AssertEqual(instance.DefaultString, result.DefaultString);
            AssertEqual(instance.AssignedString, result.AssignedString);
        }
        
        // case when all fields are default values
        [StarDataType]
        class DefaultValuesAllDefaults
        {
            [StarData] public bool Falsy = false;
            [StarData(DefaultValue=true)] public bool Truthy = true;

            [StarData] public float DefaultFloat = default;
        }

        [TestMethod]
        public void DefaultValuesAllDefaultsAreWrittenCorrectly()
        {
            var instance = new DefaultValuesAllDefaults();
            var result = SerDes(instance);
            AssertEqual(instance.Falsy, result.Falsy);
            AssertEqual(instance.Truthy, result.Truthy);
            AssertEqual(instance.DefaultFloat, result.DefaultFloat);
        }
        
        [TestMethod]
        public void CanSerializeUniverseParams()
        {
            var instance = new UniverseParams();
            var result = SerDes(instance);
            AssertEqual(instance.Difficulty, result.Difficulty);
            AssertEqual(instance.StarsCount, result.StarsCount);
            AssertEqual(instance.GalaxySize, result.GalaxySize);
            AssertEqual(instance.ExtraRemnant, result.ExtraRemnant);
            AssertEqual(instance.NumSystems, result.NumSystems);
            AssertEqual(instance.NumOpponents, result.NumOpponents);
            AssertEqual(instance.Mode, result.Mode);
            AssertEqual(instance.Pace, result.Pace);
            AssertEqual(instance.StarsModifier, result.StarsModifier);
            AssertEqual(instance.MinAcceptableShipWarpRange, result.MinAcceptableShipWarpRange);
            AssertEqual(instance.TurnTimer, result.TurnTimer);
            AssertEqual(instance.PreventFederations, result.PreventFederations);
            AssertEqual(instance.EliminationMode, result.EliminationMode);
            AssertEqual(instance.CustomMineralDecay, result.CustomMineralDecay);
            AssertEqual(instance.VolcanicActivity, result.VolcanicActivity);
            AssertEqual(instance.ShipMaintenanceMultiplier, result.ShipMaintenanceMultiplier);
            AssertEqual(instance.AIUsesPlayerDesigns, result.AIUsesPlayerDesigns);
            AssertEqual(instance.StartingPlanetRichnessBonus, result.StartingPlanetRichnessBonus);
            AssertEqual(instance.FTLModifier, result.FTLModifier);
            AssertEqual(instance.EnemyFTLModifier, result.EnemyFTLModifier);
            AssertEqual(instance.GravityWellRange, result.GravityWellRange);
            AssertEqual(instance.ExtraPlanets, result.ExtraPlanets);
            AssertEqual(instance.PlanetsScreenHideInhospitable, result.PlanetsScreenHideInhospitable);
            AssertEqual(instance.DisableInhibitionWarning, result.DisableInhibitionWarning);
            AssertEqual(instance.EnableStarvationWarning, result.EnableStarvationWarning);
            AssertEqual(instance.SuppressOnBuildNotifications, result.SuppressOnBuildNotifications);
            AssertEqual(instance.PlanetScreenHideOwned, result.PlanetScreenHideOwned);
            AssertEqual(instance.ShipListFilterPlayerShipsOnly, result.ShipListFilterPlayerShipsOnly);
            AssertEqual(instance.ShipListFilterInFleetsOnly, result.ShipListFilterInFleetsOnly);
            AssertEqual(instance.ShipListFilterNotInFleets, result.ShipListFilterNotInFleets);
            AssertEqual(instance.CordrazinePlanetCaptured, result.CordrazinePlanetCaptured);
            AssertEqual(instance.DisableVolcanoWarning, result.DisableVolcanoWarning);
            AssertEqual(instance.ShowAllDesigns, result.ShowAllDesigns);
            AssertEqual(instance.FilterOldModules, result.FilterOldModules);
            AssertEqual(instance.DisableRemnantStory, result.DisableRemnantStory);
            AssertEqual(instance.DisableAlternateAITraits, result.DisableAlternateAITraits);
            AssertEqual(instance.DisablePirates, result.DisablePirates);
            AssertEqual(instance.FixedPlayerCreditCharge, result.FixedPlayerCreditCharge);
        }

        [TestMethod]
        public void CanSerializeShipDesign()
        {
            CreateUniverseAndPlayerEmpire();
            Ship spawned = SpawnShip("Terran-Prototype", Player, Vector2.One);
            UniverseState us = SerDes(UState);

            Ship deserialized = us.Player.OwnedShips[0];
            ShipDataTests.AssertAreEqual((ShipDesign)spawned.ShipData, (ShipDesign)deserialized.ShipData,
                                         checkModules:true);
        }

        [StarDataType]
        class SavesContainer
        {
            [StarData] public ShipDesign[] Designs;
        }

        [TestMethod]
        public void CanSerializeAllShipDesigns()
        {
            LoadAllGameData();

            ShipDesign[] designs = ResourceManager.Ships.Designs.Select(s => s as ShipDesign);

            var t1 = new PerfTimer();
            var sw = new ShipDesignWriter();
            int textBytes = 0;
            var designsByteData = new Array<byte[]>();
            foreach (ShipDesign design in designs)
            {
                var designBytes = design.GetDesignBytes(sw);
                designsByteData.Add(designBytes);
                textBytes += designBytes.Length;
            }
            for (int i = 0; i < designs.Length; ++i)
            {
                ShipDesign.FromBytes(designsByteData[i]);
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
            AssertEqual(1, results1.Length);
            AssertEqual(typeof(HeaderType), results1[0].GetType());

            var header1 = (HeaderType)results1[0];
            AssertEqual(header.Version, header1.Version);
            AssertEqual(header.Name, header1.Name);

            reader.BaseStream.Position = 0;
            var results2 = BinarySerializer.DeserializeMultiType(reader, new[]{ typeof(HeaderType), typeof(PayloadType) });
            AssertEqual(2, results2.Length);
            AssertEqual(typeof(HeaderType), results2[0].GetType());
            AssertEqual(typeof(PayloadType), results2[1].GetType());

            var header2 = (HeaderType)results2[0];
            AssertEqual(header.Version, header2.Version);
            AssertEqual(header.Name, header2.Name);

            var payload2 = (PayloadType)results2[1];
            AssertEqual(payload.Data, payload2.Data);
            AssertEqual(payload.Size, payload2.Size);
        }

        double GetMemory(bool gc) => GC.GetTotalMemory(gc) / (1024.0 * 1024.0);

        // this is the actual big test for SavedGame
        [TestMethod]
        public void SavedGameSerialize()
        {
            bool verbose = false;
            CreateCustomUniverseSandbox(numOpponents: 6, galSize: GalSize.Large);
            Universe.SingleSimulationStep(TestSimStep);

            static Empire E(UniverseScreen u) => u.UState.Empires[0];
            string firstEmpName = E(Universe).Name;
            int numShips        = E(Universe).OwnedShips.Count;
            float firstShipHealth = E(Universe).OwnedShips[0].Health;
            var unlocked1         = E(Universe).UnlockedTechs;

            double memory1 = GetMemory(false);

            var save = new SavedGame(Universe);
            save.Verbose = verbose;
            save.Save("BinarySerializer.Test");
            Universe.ExitScreen();

            double memory2 = GetMemory(false);

            // peek the header as per specs
            HeaderData header = LoadGame.PeekHeader(save.SaveFile);
            AssertEqual(SavedGame.SaveGameVersion, header.Version);
            AssertEqual("BinarySerializer.Test", header.SaveName);

            double memory3 = GetMemory(false);

            var load = new LoadGame(save.SaveFile);
            load.Verbose = verbose;
            UniverseScreen us = load.Load(noErrorDialogs:true, startSimThread:false);
            Assert.IsNotNull(us, "Loaded universe cannot be null");
            us.SingleSimulationStep(TestSimStep);

            double memory4 = GetMemory(false);

            Log.Info($"BeforeSave: {memory1:0.0}MB");
            Log.Info($"AfterSave:  {memory2:0.0}MB  delta:{memory2-memory1:0.0}MB");
            Log.Info($"BeforeLoad: {memory3:0.0}MB  delta:{memory3-memory2:0.0}MB");
            Log.Info($"AfterLoad:  {memory4:0.0}MB  delta:{memory4-memory3:0.0}MB");

            AssertEqual(firstEmpName, E(us).Name, "First empire name should match");
            AssertEqual(numShips, E(us).OwnedShips.Count, "Empire should have same # of ships");
            AssertEqual(firstShipHealth, E(us).OwnedShips[0].Health, "Ships should have health");

            var unlocked2 = E(us).UnlockedTechs;
            AssertEqual(unlocked1.Length, unlocked2.Length, "Unlocked techs count must match");
            AssertEqual(unlocked1.Select(t=>t.UID), unlocked2.Select(t=>t.UID), "Unlocked techs UID-s must match");
        }

        [TestMethod]
        public void SavedGameSerializerPerf()
        {
            CreateCustomUniverseSandbox(numOpponents: 6, galSize: GalSize.Large, numExtraShipsPerEmpire: 100);
            Universe.SingleSimulationStep(TestSimStep);

            const int iterations = 20;

            var timer = new PerfTimer();

            for (int i = 0; i < iterations; ++i)
            {
                var save = new SavedGame(Universe);
                save.Save("BinarySerializer.Test");
            }

            double elapsed = timer.Elapsed;
            Log.Info("=========================================");
            Log.Info($"Save {iterations}x elapsed: {elapsed:0.00}s");
            Log.Info("=========================================");
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
