using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Vector4 = Microsoft.Xna.Framework.Vector4;
using Point = Microsoft.Xna.Framework.Point;
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
            [StarData] public string DefaultIsNotNull = "Default is not null";
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
            var result = SerDes(instance, out byte[] bytes);
            Assert.AreEqual(result.RecursiveSelf, result, "Recursive self reference must match");
            Assert.AreEqual(instance.Text, result.Text, "string must match");
            Assert.AreEqual(instance.Count, result.Count, "int field must match");
            Assert.AreEqual(null, result.DefaultIsNotNull, "null field must match");
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

        T[] Arr<T>(params T[] elements) => elements;
        Array<T> List<T>(params T[] elements) => new Array<T>(elements);
        Map<K,V> Map<K,V>(params ValueTuple<K, V>[] elements) => new Map<K,V>(elements);

        [StarDataType]
        class RawArrayType
        {
            [StarData] public int[] Integers;
            [StarData] public Vector2[] Points;
            [StarData] public string[] Names;
            [StarData] public string[] Empty;
            [StarData] public StructContainer[] Structs;
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
            };
            var result = SerDes(instance, out byte[] bytes);
            Assert.That.Equal(instance.Integers, result.Integers);
            Assert.That.Equal(instance.Points, result.Points);
            Assert.That.Equal(instance.Names, result.Names);
            Assert.That.Equal(instance.Empty, result.Empty);
            Assert.That.Equal(instance.Structs.Length, result.Structs.Length);
            for (int i = 0; i < instance.Structs.Length; ++i)
                Assert.That.Equal(instance.Structs[i].SS, result.Structs[i].SS);
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
            var result = SerDes(instance, out byte[] bytes);
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
            var result = SerDes(instance, out byte[] bytes);
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
            var result = SerDes(instance, out byte[] bytes);
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
            var result = SerDes(instance, out byte[] bytes);
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
            var result = SerDes(instance, out byte[] bytes);
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
            var result = SerDes(instance, out byte[] bytes);
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
            var result = SerDes(instance, out byte[] bytes);
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

        //[StarDataType] class MovedType {[StarData] public Vector4 Value4; }
        [StarDataType]
        class ContainsMovedType
        {
            [StarData] public Vector3 Pos;
            [StarData] public MovedType MT;
            [StarData] public string Name;
            // MOVED TYPE:
            [StarDataType] public class MovedType {[StarData] public Vector4 Value4; }
        }

        // Handles the case where a Type is simply moved from one namespace/class to another
        [TestMethod]
        public void ContainsMovedTypes()
        {
            //string containsMovedType = CreateByteStreamForDeletedTypeTest(new ContainsMovedType
            //{
            //    Pos = new Vector3(2001, 2002, 2003),
            //    MT = new MovedType { Value4 = new Vector4(4001, 4002, 4003, 4004) },
            //    Name = "Contains a moved type",
            //});
            string containsMovedType = "AQACACEDAQEJVW5pdFRlc3RzAS1Vbml0VGVzdHMuU2VyaWFsaXphdGlvbi5CaW5hcnlTZXJpYWxpemVyVGVzdHMCEUNvbnRhaW5zTW92ZWRUeXBlCU1vdmVkVHlwZQQCTVQETmFtZQNQb3MGVmFsdWU0IAAAAAEDIQATAQ0CIQAAAQEBDgMTASABIQEVQ29udGFpbnMgYSBtb3ZlZCB0eXBlIQADEwEBDQIAIPpEAED6RABg+kQOAAAQekUAIHpFADB6RQBAekU=";
            var ser = new BinarySerializer(typeof(ContainsMovedType));
            var result = Deserialize<ContainsMovedType>(ser, Convert.FromBase64String(containsMovedType));
            Assert.AreEqual(new Vector3(2001, 2002, 2003), result.Pos);
            Assert.AreEqual(new Vector4(4001, 4002, 4003, 4004), result.MT.Value4);
            Assert.AreEqual("Contains a moved type", result.Name);
        }

        //[StarDataType] class DeletedType {[StarData] public Vector4 Value4; }
        //[StarDataType] struct DeletedStruct {[StarData] public Vector4 Value4; }

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
            //string containsDeletedType = CreateByteStreamForDeletedTypeTest(new ContainsDeletedType
            //{
            //    Pos = new Vector3(2001, 2002, 2003),
            //    DT = new DeletedType { Value4 = new Vector4(4001, 4002, 4003, 4004) },
            //    DS = new DeletedStruct { Value4 = new Vector4(5001, 5002, 5003, 5004) },
            //    Name = "Contains deleted types",
            //});
            string containsDeletedType = "AQADACIDAQEJVW5pdFRlc3RzAS1Vbml0VGVzdHMuU2VyaWFsaXphdGlvbi5CaW5hcnlTZXJpYWxpemVyVGVzdHMDE0NvbnRhaW5zRGVsZXRlZFR5cGUNRGVsZXRlZFN0cnVjdAtEZWxldGVkVHlwZQUCRFMCRFQETmFtZQNQb3MGVmFsdWU0IgAAAQABDgQgAAAAAQQiACEBEwINAyEAAAIBAQ4EEwEgASEBFkNvbnRhaW5zIGRlbGV0ZWQgdHlwZXMiAA4AAEicRQBQnEUAWJxFAGCcRSEBAxMCAQ0DACD6RABA+kQAYPpEDgAAEHpFACB6RQAwekUAQHpF";
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
            //string containsRemovedField = CreateByteStreamForDeletedTypeTest(new ContainsRemovedFieldType
            //{
            //    Pos = new Vector3(2001, 2002, 2003),
            //    Removed = new RecursiveType("Will be removed", 1234),
            //    Name = "Contains a removed field",
            //    Pos2 = new Vector2(4010, 4020),
            //});
            string containsRemovedField = "AQACACEDAgEJVW5pdFRlc3RzAS1Vbml0VGVzdHMuU2VyaWFsaXphdGlvbi5CaW5hcnlTZXJpYWxpemVyVGVzdHMCGENvbnRhaW5zUmVtb3ZlZEZpZWxkVHlwZQ1SZWN1cnNpdmVUeXBlCAVDb3VudBBEZWZhdWx0SXNOb3ROdWxsBE5hbWUDUG9zBFBvczINUmVjdXJzaXZlU2VsZgdSZW1vdmVkBFRleHQgAAAAAQQTAg0DDAQhBiEAAAEBBAYAEwEhBRMHEwIgASEBGENvbnRhaW5zIGEgcmVtb3ZlZCBmaWVsZA9XaWxsIGJlIHJlbW92ZWQTAAENAQAg+kQAQPpEAGD6RAwCAKB6RQBAe0UhAwQGAJITEwEAIQIEEwMC";
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
            var result = SerDes(instance, out byte[] bytes);
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
            var result = SerDes(instance, out byte[] bytes);
            Assert.AreEqual(instance.Pos, result.Pos);
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
