using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.YamlSerializer;

namespace UnitTests.Serialization
{
    [TestClass]
    public class YamlSerializerTests : StarDriveTest
    {
        static string SerializeToString<T>(T instance)
        {
            var sw = new StringWriter();
            var ser = new YamlSerializer(typeof(T));
            ser.Serialize(sw, instance);
            return sw.ToString();
        }

        [TestMethod]
        public void ParticleSettings()
        {
            var ps = new ParticleSettings
            {
                Name = "Flame",
                TextureName = "trail 1.png",
                Effect = "ParticleEffect.fx",
                Static = true,
                OnlyNearView = true,
                MaxParticles = 10,
                Duration = TimeSpan.FromSeconds(123.5),
                DurationRandomness = 1,
                EmitterVelocitySensitivity = 2,
                AlignRotationToVelocity = 1,
                RandomVelocityXY = new []{ new Range(3, 4.2f), new Range(5.4f, 5.8f) },
                AlignRandomVelocityXY = true,
                EndVelocity = 6,
                MinColor = new Color(10,20,30,255),
                MaxColor = new Color(40,50,60,255),
                MinRotateSpeed = 7,
                MaxRotateSpeed = 8,
                MinStartSize = 9,
                MaxStartSize = 10,
                MinEndSize = 11,
                MaxEndSize = 12,
                SourceBlend = Blend.BlendFactor,
                DestinationBlend = Blend.InverseBlendFactor,
            };

            string text = SerializeToString(ps);
            string yaml =
              @"ParticleSettings:
                  Name: Flame
                  TextureName: ""trail 1.png""
                  Effect: ParticleEffect.fx
                  Static: true
                  OnlyNearView: true
                  MaxParticles: 10
                  Duration: 123.5
                  DurationRandomness: 1
                  EmitterVelocitySensitivity: 2
                  AlignRotationToVelocity: 1
                  RandomVelocityXY: [[3,4.2],[5.4,5.8]]
                  AlignRandomVelocityXY: true
                  EndVelocity: 6
                  MinColor: [10,20,30,255]
                  MaxColor: [40,50,60,255]
                  MinRotateSpeed: 7
                  MaxRotateSpeed: 8
                  MinStartSize: 9
                  MaxStartSize: 10
                  MinEndSize: 11
                  MaxEndSize: 12
                  SourceBlend: BlendFactor
                  DestinationBlend: InverseBlendFactor
                ".Replace("\r\n", "\n").Replace("                ", "");

            Assert.That.Equal(yaml, text);
        }
        
        enum MapKeys { House, Plane, Ship, Potato, Bullet } 
        
        [StarDataType]
        class PrimitivesList
        {
            #pragma warning disable 649
            [StarData] public Array<int> Primitives;
            #pragma warning restore 649
        }

        [StarDataType]
        class SmallValue
        {
            [StarData] public MapKeys Key;
            [StarData] public float Value;
        }

        [StarDataType]
        class SmallObjectList
        {
            #pragma warning disable 649
            [StarData] public Array<SmallValue> NamedValues;
            #pragma warning restore 649
        }

        [TestMethod]
        public void ArrayOfPrimitives()
        {
            var list = new PrimitivesList()
            {
                Primitives = new Array<int> { 1, 2, 3 }
            };
            string text = SerializeToString(list);
            string yaml =
              @"PrimitivesList:
                  Primitives: [1,2,3]
                ".Replace("\r\n", "\n").Replace("                ", "");
            Assert.That.Equal(yaml, text);
        }

        [TestMethod]
        public void ArrayOfSmallObjects()
        {
            var list = new SmallObjectList()
            {
                NamedValues = new Array<SmallValue>()
                {
                    new SmallValue{ Key = MapKeys.House, Value = 1.1f },
                    new SmallValue{ Key = MapKeys.Plane, Value = 2.2f },
                    new SmallValue{ Key = MapKeys.Ship,  Value = 3.3f },
                }
            };
            string text = SerializeToString(list);
            string yaml =
              @"SmallObjectList:
                  NamedValues:
                    - { Key:House, Value:1.1 }
                    - { Key:Plane, Value:2.2 }
                    - { Key:Ship, Value:3.3 }
                ".Replace("\r\n", "\n").Replace("                ", "");
            Assert.That.Equal(yaml, text);
        }

        [StarDataType]
        class BigValue
        {
            [StarData] public MapKeys Key;
            [StarData] public float Value1;
            [StarData] public float Value2;
            [StarData] public float Value3;
        }
        
        [StarDataType]
        class BigObjectList
        {
            #pragma warning disable 649
            [StarData] public Array<BigValue> NamedValues;
            #pragma warning restore 649
        }

        [TestMethod]
        public void ArrayOfBigObjects()
        {
            var list = new BigObjectList()
            {
                NamedValues = new Array<BigValue>()
                {
                    new BigValue{ Key = MapKeys.House, Value1 = 1.1f, Value2 = 2.4f, Value3 = 3.6f },
                    new BigValue{ Key = MapKeys.Plane, Value1 = 2.2f, Value2 = 3.4f, Value3 = 4.6f },
                }
            };
            string text = SerializeToString(list);
            string yaml =
              @"BigObjectList:
                  NamedValues:
                    - Key: House
                      Value1: 1.1
                      Value2: 2.4
                      Value3: 3.6
                    - Key: Plane
                      Value1: 2.2
                      Value2: 3.4
                      Value3: 4.6
                ".Replace("\r\n", "\n").Replace("                ", "");
            Assert.That.Equal(yaml, text);
        }
        

        [StarDataType]
        class SmallPrimitivesMap
        {
            #pragma warning disable 649
            [StarData] public Map<MapKeys, float> Map;
            #pragma warning restore 649
        }

        [TestMethod]
        public void SmallPrimitiveMap()
        {
            var map = new SmallPrimitivesMap()
            {
                Map = new Map<MapKeys, float>
                {
                    (MapKeys.House, 1.1f),
                    (MapKeys.Plane, 2.2f),
                    (MapKeys.Ship, 3.3f),
                }
            };
            string text = SerializeToString(map);
            string yaml =
              @"SmallPrimitivesMap:
                  Map: { House:1.1, Plane:2.2, Ship:3.3 }
                ".Replace("\r\n", "\n").Replace("                ", "");
            Assert.That.Equal(yaml, text);
        }

        [StarDataType]
        class LargePrimitivesMap
        {
            #pragma warning disable 649
            [StarData] public Map<MapKeys, float> Map;
            #pragma warning restore 649
        }

        [TestMethod]
        public void LargePrimitiveMap()
        {
            var map = new LargePrimitivesMap()
            {
                Map = new Map<MapKeys, float>
                {
                    (MapKeys.House, 1.1f),
                    (MapKeys.Plane, 2.2f),
                    (MapKeys.Ship, 3.3f),
                    (MapKeys.Potato, 4.4f),
                    (MapKeys.Bullet, 5.5f),
                }
            };
            string text = SerializeToString(map);
            string yaml =
              @"LargePrimitivesMap:
                  Map:
                    House: 1.1
                    Plane: 2.2
                    Ship: 3.3
                    Potato: 4.4
                    Bullet: 5.5
                ".Replace("\r\n", "\n").Replace("                ", "");
            Assert.That.Equal(yaml, text);
        }
    }

}
