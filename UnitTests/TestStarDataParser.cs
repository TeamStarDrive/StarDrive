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

        void ParserDump(StarDataParser parser)
        {
            Console.WriteLine(parser.Root.SerializedText());
            foreach (StarDataParser.Error error in parser.Errors)
            {
                Console.WriteLine(error.ToString());
            }
            if (parser.Errors.Count > 0)
                throw new InvalidDataException($"Parser failed with {parser.Errors.Count} error(s)");
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
                ParserDump(parser);

                var items = parser.Root.SubNodes;
                Assert.AreEqual("Value", items[0].Value);
                Assert.AreEqual("String Text # with : characters[{:\r\n\t", items[1].Value);
                Assert.AreEqual("Value Text", items[2].Value);
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
                ParserDump(parser);

                var root = parser.Root;
                Assert.AreEqual(-1234567, root.GetSubNode("Int1").ValueInt);
                Assert.AreEqual(+1234567, root.GetSubNode("Int2").ValueInt);
                Assert.AreEqual(-123.4567, root.GetSubNode("Float1").ValueFloat, 0.0001);
                Assert.AreEqual(+123.4567, root.GetSubNode("Float2").ValueFloat, 0.0001);
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
                ParserDump(parser);

                var root = parser.Root;
                Assert.That.Equal(Array(0,1,2), root[0].ValueArray);
                Assert.That.Equal(Array(Array(0,1),Array(2,3),Array(4,5)), root[1].ValueArray);
                Assert.That.Equal(Array(3,2,1, "{:#takeoff:["), root[2].ValueArray);
            }
        }

        [TestMethod]
        public void ValidateMaps()
        {
            const string yaml = @"
                Map0: { A: 0 , B: Value , C: 'String value:' } # comment
                Map1: Key
                  A: 0
                  B: Value
                  C: 'String value:'
                  D: true
                Map2: { A: [1,2], B: { X: Y } }
                ";
            using (var parser = new StarDataParser(">ValidateMaps<", new StringReader(yaml)))
            {
                ParserDump(parser);

                var root = parser.Root;
                Assert.AreEqual(0,       root[0]["A"].ValueInt);
                Assert.AreEqual("Value", root[0]["B"].ValueText);
                Assert.AreEqual("String value:", root[0]["C"].ValueText);

                Assert.AreEqual(0,       root[1]["A"].ValueInt);
                Assert.AreEqual("Value", root[1]["B"].ValueText);
                Assert.AreEqual("String value:", root[1]["C"].ValueText);
                Assert.AreEqual(true, root[1]["D"].ValueBool);

                Assert.That.Equal(Array(1,2),  root[2]["A"].ValueArray);
                Assert.AreEqual("Y", root[2]["B"]["X"].Value);
            }
        }

        [TestMethod]
        public void SequenceSimple()
        {
            const string yaml1 = @"
                Sequence:
                  - 1234
                  - 'hello'
                  - abc
                  - key: value
                ";
            using (var parser = new StarDataParser(">SequenceSimple<", new StringReader(yaml1)))
            {
                ParserDump(parser);

                var seq1 = parser.Root["Sequence"];
                Assert.IsTrue(seq1.HasSequence);
                Assert.AreEqual(4, seq1.Count);
                Assert.AreEqual(1234,    seq1.SequenceNodes[0].Value);
                Assert.AreEqual("hello", seq1.SequenceNodes[1].Value);
                Assert.AreEqual("abc",   seq1.SequenceNodes[2].Value);
                Assert.AreEqual("key",   seq1.SequenceNodes[3].Key);
                Assert.AreEqual("value", seq1.SequenceNodes[3].Value);
            }
        }

        [TestMethod]
        public void SequenceNested()
        {
            const string yaml = @"
                Sequence:
                  - elem1:
                    - a: 1
                    - b: 2
                  - elem2:
                    - c: 3
                      d: 4
                    - e: 5
                      f: 6
                ";
            using (var parser = new StarDataParser(">SequenceNested<", new StringReader(yaml)))
            {
                ParserDump(parser);

                var seq2 = parser.Root["Sequence"];
                Assert.IsTrue(seq2.HasSequence);
                Assert.AreEqual(2, seq2.Count);

                var elem1 = seq2.GetElement(0);
                Assert.IsTrue(elem1.HasSequence);
                Assert.AreEqual(2, elem1.Count);
                Assert.AreEqual("elem1", elem1.Name);
                Assert.AreEqual("a", elem1.GetElement(0).Name);
                Assert.AreEqual(1,   elem1.GetElement(0).Value);
                Assert.AreEqual("b", elem1.GetElement(1).Name);
                Assert.AreEqual(2,   elem1.GetElement(1).Value);

                var elem2 = seq2.GetElement(1);
                Assert.IsTrue(elem2.HasSequence);
                Assert.AreEqual(2, elem2.Count);
                Assert.AreEqual("elem2", elem2.Name);
                Assert.AreEqual("c", elem2.GetElement(0).Name);
                Assert.AreEqual(3,   elem2.GetElement(0).Value);
                Assert.AreEqual(4,   elem2.GetElement(0)["d"].Value);

                Assert.AreEqual("e", elem2.GetElement(1).Name);
                Assert.AreEqual(5,   elem2.GetElement(1).Value);
                Assert.AreEqual(6,   elem2.GetElement(1)["f"].Value);
            }
        }

        [TestMethod]
        public void SequenceOfArrays()
        {
            const string yaml = @"
                Sequence:
                  - seq1: [0, 1, 2]
                    - [3, 4, 5]
                  - seq2: [5, 6, 7]
                    - [8, 9, 10]
                ";
            using (var parser = new StarDataParser(">SequenceOfArrays<", new StringReader(yaml)))
            {
                ParserDump(parser);

                var seq = parser.Root["Sequence"];
                Assert.IsTrue(seq.HasSequence);
                Assert.AreEqual(2, seq.Count);

                var seq1 = seq.GetElement(0);
                var seq2 = seq.GetElement(1);
                Assert.IsTrue(seq1.HasSequence);
                Assert.IsTrue(seq2.HasSequence);
                Assert.AreEqual(1, seq1.Count);
                Assert.AreEqual(1, seq2.Count);

                Assert.That.Equal(Array(0,1,2), seq1.ValueArray);
                Assert.That.Equal(Array(3,4,5), seq1.SequenceNodes[0].ValueArray);

                Assert.That.Equal(Array(5,6,7), seq2.ValueArray);
                Assert.That.Equal(Array(8,9,10), seq2.SequenceNodes[0].ValueArray);
            }
        }

        [TestMethod]
        public void SequenceOfMaps()
        {
            const string yaml = @"
                Sequence:
                  - { Id: 0, Size: 5 }
                  - { Id: 1, Size: 10 }
                ";
            using (var parser = new StarDataParser(">SequenceOfMaps<", new StringReader(yaml)))
            {
                ParserDump(parser);

                var seq = parser.Root["Sequence"];
                Assert.IsTrue(seq.HasSequence);
                Assert.AreEqual(2, seq.Count);

                Assert.AreEqual(0, seq.SequenceNodes[0]["Id"].Value);
                Assert.AreEqual(5, seq.SequenceNodes[0]["Size"].Value);

                Assert.AreEqual(1, seq.SequenceNodes[1]["Id"].Value);
                Assert.AreEqual(10, seq.SequenceNodes[1]["Size"].Value);
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
            using (var parser = new StarDataParser(">CombinedSyntax<", new StringReader(yaml)))
            {
                ParserDump(parser);

                var o = parser.Root["Object"];
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void HandleInvalidArraySyntax()
        {
            const string yaml = @"
                InvalidArray1: [ OkSyntax, { InvalidMap: Syntax } ]
                ";
            using (var parser = new StarDataParser(">HandleInvalidArraySyntax<", new StringReader(yaml)))
            {
                ParserDump(parser);
            }
        }

        [TestMethod]
        public void ParsePlanetTypes()
        {
            using (var parser = new StarDataParser("PlanetTypes.yaml"))
            {
                ParserDump(parser);

                Array<PlanetType> planets = parser.DeserializeArray<PlanetType>();
                planets.Sort(p => p.Id);
                foreach (PlanetType type in planets)
                {
                    Console.WriteLine(type.ToString());
                }
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
        public void ParseSunZones()
        {
            const string yaml = @"
                SunZone: Near
                  Weights:
                    - {Type: Barren, Weight: 20}
                    - {Type: Desert, Weight: 10}
                    - {Type: Steppe, Weight: 1}
                    - {Type: Tundra, Weight: 0}
                ";

            using (var parser = new StarDataParser(">SunZones<", new StringReader(yaml)))
            {
                ParserDump(parser);

                StarDataNode zone = parser.Root["SunZone"];
                Assert.AreEqual("SunZone", zone.Name);
                Assert.AreEqual("Weights", zone["Weights"].Name);

                //Assert.AreEqual(4, zone["Weights"].)

                Array<SunZoneData> zones = parser.DeserializeArray<SunZoneData>();
            }
        }
    }
}
