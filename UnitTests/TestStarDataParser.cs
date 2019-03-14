using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Data;

namespace UnitTests
{
    [TestClass]
    [SuppressMessage("ReSharper", "UnassignedReadonlyField")]
    public class TestStarDataParser
    {
        static TestStarDataParser()
        {
            Directory.SetCurrentDirectory("/Projects/BlackBox");
        }

        [TestMethod]
        public void ValidateKeyValue()
        {
            const string yaml = @"
                Key1: Value #comment
                Key2: 'String Text # with : characters[{:\r\n\t' #comment
                Key3: Value Text # comment
                ";
            using (var parser = new StarDataParser(">ValidateKeyValue<", new StringReader(yaml)))
            {
                var items = parser.Root.SubNodes;
                Assert.AreEqual("Value", items[0].Value);
                Assert.AreEqual("String Text # with : characters[{:\r\n\t", items[1].Value);
                Assert.AreEqual("Value Text", items[2].Value);
                Console.WriteLine(parser.Root.SerializedText());
            }
        }

        [TestMethod]
        public void ValidateNumberParse()
        {
            const string yaml = @"
                Int1: -1234567 # comment
                Int2: +1234567
                Float1: -123.4567 # comment
                Float2: +123.4567
                ";
            using (var parser = new StarDataParser(">ValidateMaps<", new StringReader(yaml)))
            {
                var root = parser.Root;
                Assert.AreEqual(-1234567, root.GetSubNode("Int1").Value);
                Assert.AreEqual(+1234567, root.GetSubNode("Int2").Value);
                Assert.AreEqual(-123.4567, root.GetSubNode("Float1").Value);
                Assert.AreEqual(+123.4567, root.GetSubNode("Float2").Value);

                Console.WriteLine(parser.Root.SerializedText());
            }
        }

        static object[] Array(params object[] args) => args;

        [TestMethod]
        public void ValidateArrays()
        {
            const string yaml = @"
                Array1: [ 0 , 1 , 2 ]
                Array2: [ [0,1], [2,3], [4,5] ] # Comment
                Array3: [3, 2, 1, '{:#takeoff:[' ]
                ";
            using (var parser = new StarDataParser(">ValidateArrays<", new StringReader(yaml)))
            {
                var root = parser.Root;
                Assert.That.Equal(Array(0,1,2), root[0].ValueArray);
                Assert.That.Equal(Array(Array(0,1),Array(2,3),Array(4,5)), root[1].ValueArray);
                Assert.That.Equal(Array(3,2,1, "{:#takeoff:["), root[2].ValueArray);
                Console.WriteLine(parser.Root.SerializedText());
            }
        }

        [TestMethod]
        public void ValidateMaps()
        {
            const string yaml = @"
                Map1: { A: 0 , B: Value , C: 'String value:' } # comment
                Map2: Key
                  A: 0
                  B: Value
                  C: 'String value:'
                Map3: { A: [1,2], { X: Y } }
                ";
            using (var parser = new StarDataParser(">ValidateMaps<", new StringReader(yaml)))
            {
                var root = parser.Root;
                Assert.AreEqual(0,       root[0].GetSubNode("A").Value);
                Assert.AreEqual("Value", root[0].GetSubNode("B").Value);
                Assert.AreEqual("String value:", root[0].GetSubNode("C").Value);

                Assert.AreEqual(0,       root[1].GetSubNode("A").Value);
                Assert.AreEqual("Value", root[1].GetSubNode("B").Value);
                Assert.AreEqual("String value:", root[1].GetSubNode("C").Value);

                Console.WriteLine(parser.Root.SerializedText());
            }
        }

        [TestMethod]
        public void ValidateSequences()
        {
            const string yaml = @"
                Seq1:
                  - 1234
                  - 'hello'
                  - abc
                Seq2:
                  - elem1:
                    - a: 0
                    - b: 1
                  - elem2:
                    - a: 2
                    - b: 3
                ";
            using (var parser = new StarDataParser(">ValidateMaps<", new StringReader(yaml)))
            {
                var root = parser.Root;
                var seq1 = root.GetSubNode("Seq1");
                var seq2 = root.GetSubNode("Seq2");
                Assert.IsTrue(seq1.HasSequence);
                Assert.IsTrue(seq2.HasSequence);

                Assert.AreEqual(1234,    seq1.GetElement(0).Value);
                Assert.AreEqual("hello", seq1.GetElement(1).Value);
                Assert.AreEqual("abc",   seq1.GetElement(2).Value);

                var elem1 = seq2.GetElement(0);
                var elem2 = seq2.GetElement(1);
                Assert.IsTrue(elem1.HasSequence);
                Assert.IsTrue(elem2.HasSequence);
                Assert.AreEqual(2, elem1.Count);
                Assert.AreEqual(2, elem2.Count);
                Assert.AreEqual("elem1", elem1.Name);
                Assert.AreEqual("elem2", elem2.Name);
                Assert.AreEqual("a", elem1.GetElement(0).Name);
                Assert.AreEqual("b", elem1.GetElement(1).Name);

                Assert.That.Equal(0,       root[1].GetSubNode("A").Value);
                Assert.That.Equal("Value", root[1].GetSubNode("B").Value);
                Assert.That.Equal("String value:", root[1].GetSubNode("C").Value);

                Console.WriteLine(parser.Root.SerializedText());
            }
        }

        [TestMethod]
        public void ParsePlanetTypes()
        {
            using (var parser = new StarDataParser("PlanetTypes.yaml"))
            {
                Array<PlanetType> planets = parser.DeserializeArray<PlanetType>();
                planets.Sort(p => p.Id);
                foreach (PlanetType type in planets)
                {
                    Console.WriteLine(type.ToString());
                }
                Console.WriteLine(parser.Root.SerializedText());
            }
        }

        
        [TestMethod]
        public void ValidateCombinedSyntax()
        {
            const string yaml = @"  # Comment 
                Object : RootKey # comment
                  Key1 : 'Value' # Comment
                  Key2 : [ 0 , 1 , 2 ] # Comment
                  Key3 : { Name : Mike, Age : 10 } # Comment
                  Sequence1:
                    - Element0: Value0
                    - Element1: [ 0, 1 ]
                    - Element2: { Name: Mike , Sub: [3, 2, 1, '{:#takeoff:[' ] }
                    - Element3: [ [0,1], [2,3], [4,5] ]
                  Sequence2:
                    - Name: ':Mike:'
                      Age: 20
                    - Name: ':Lisa:'
                      Age: 19
                ";
            using (var parser = new StarDataParser(">BasicSyntax<", new StringReader(yaml)))
            {
                Console.WriteLine(parser.Root.SerializedText());
            }
        }

        [StarDataType]
        public class TypeWeight
        {
            [StarDataKey] public readonly PlanetCategory Type; // Barren
            [StarData] public readonly int Value; // 10
        }
        public class SunZoneData
        {
            [StarDataKey] public readonly SunZone Zone; // Near
            [StarData] public readonly TypeWeight[] Weights;
        }

        [TestMethod]
        public void ParseArrayOfCustomType()
        {
            const string yaml = @"# ParseArrayOfCustomType 
                SunZone: Near # test
                  Weights:
                    - {Type: Barren, Weight: 20, X: [0, 1, 2] }
                    - {Type: Desert, Weight: 10}
                    - {Type: Steppe, Weight: 1}
                    - {Type: Tundra, Weight: 0}
                    - {Type: Terran, Weight: 3}
                    - {Type: Volcanic, Weight: 20}
                    - {Type: Ice, Weight: 0}
                    - {Type: Swamp, Weight: 5}
                    - {Type: Oceanic, Weight: 1}
                    - {Type: GasGiant, Weight: 2}
                SunZone: Habital
                  Weights:
                    - Type: Barren
                      Weight: 20
                    - Type: Desert
                      Weight: 10
                ";

            using (var parser = new StarDataParser(">SunZones<", new StringReader(yaml)))
            {
                StarDataNode root = parser.Root;
                Assert.AreEqual(2, root.SubNodes.Count);
                StarDataNode zone1 = root.SubNodes[0];
                StarDataNode zone2 = root.SubNodes[1];
                Assert.AreEqual("SunZone", zone1.Name);
                Assert.AreEqual("SunZone", zone2.Name);
                Assert.AreEqual(1, zone1.SubNodes.Count);
                Assert.AreEqual(1, zone2.SubNodes.Count);
                StarDataNode weights1 = zone1.SubNodes[0];
                StarDataNode weights2 = zone2.SubNodes[0];
                Assert.AreEqual("Weights", weights1.Name);
                Assert.AreEqual("Weights", weights2.Name);

                Array<SunZoneData> zones = parser.DeserializeArray<SunZoneData>();
                Console.WriteLine(parser.Root.SerializedText());
            }
        }
    }
}
