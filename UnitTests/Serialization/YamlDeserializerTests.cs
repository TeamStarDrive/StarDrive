using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;
using Ship_Game.Universe.SolarBodies;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace UnitTests.Serialization
{
    [TestClass]
    public class YamlDeserializerTests : StarDriveTest
    {
        void ParserDump(YamlParser parser)
        {
            Console.WriteLine(parser.Root.SerializedText());
            foreach (YamlParser.Error error in parser.Errors)
                Console.WriteLine(error.ToString());
            if (parser.Errors.Count > 0)
                throw new InvalidDataException($"Parser failed with {parser.Errors.Count} error(s)");
        }

        [TestMethod]
        public void DeserializePlanetTypes()
        {
            using (var parser = new YamlParser("PlanetTypes.yaml"))
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
            [StarData] public readonly PlanetCategory Type; // Barren
            [StarData] public readonly int Weight; // 20
        }
        [StarDataType]
        public class SunZoneData
        {
            [StarData] public readonly SunZone Zone; // Near
            [StarData] public readonly TypeWeight[] Weights;
        }

        [TestMethod]
        public void ParseSunZones()
        {
            const string yaml = @"
                SunZone:
                  Zone: Any
                  Weights:
                    - {Type: Barren, Weight: 20}
                    - {Type: Desert, Weight: 10}
                    - {Type: Steppe, Weight: 1}
                    - {Type: Tundra, Weight: 0}
                ";

            using (var parser = new YamlParser(">SunZones<", new StringReader(yaml)))
            {
                ParserDump(parser);

                YamlNode zone = parser.Root["SunZone"];
                Assert.AreEqual("SunZone", zone.Name);
                Assert.AreEqual("Weights", zone["Weights"].Name);
                Assert.AreEqual(4, zone["Weights"].Count);

                var sunZone = parser.DeserializeOne<SunZoneData>();
                Assert.AreEqual(SunZone.Any, sunZone.Zone);
                Assert.AreEqual(4, sunZone.Weights.Length);
                
                Assert.AreEqual(PlanetCategory.Barren, sunZone.Weights[0].Type);
                Assert.AreEqual(20,                    sunZone.Weights[0].Weight);
                Assert.AreEqual(PlanetCategory.Desert, sunZone.Weights[1].Type);
                Assert.AreEqual(10,                    sunZone.Weights[1].Weight);
                Assert.AreEqual(PlanetCategory.Steppe, sunZone.Weights[2].Type);
                Assert.AreEqual(1,                     sunZone.Weights[2].Weight);
                Assert.AreEqual(PlanetCategory.Tundra, sunZone.Weights[3].Type);
                Assert.AreEqual(0,                     sunZone.Weights[3].Weight);
            }
        }


        [StarDataType]
        struct LayoutElement
        {
            #pragma warning disable 649
            [StarDataKeyName] public string Type;
            [StarData] public string Name;
            [StarData] public Vector4 Rect;
            [StarData] public Align AxisAlign;
            [StarData] public LayoutElement[] Children1;
            [StarData] public Array<LayoutElement> Children2;
            #pragma warning restore 649
        }

        [TestMethod]
        public void ParseLayout()
        {
            const string yaml = @"
                List:
                  Name: buttons
                  Rect: [-20, 0.22, 200, 600]
                  AxisAlign: CenterRight
                  Children1:
                    - Button:
                        Name: new_game
                        Rect: [0, 0, 1.0, 0]
                        AxisAlign: CenterRight
                    - Button:
                        Name: load_game
                        Rect: [0, 50, 1.0, 0]
                        AxisAlign: CenterRight
                  Children2:
                    - Button:
                        Name: new_game
                        Rect: [0, 0, 1.0, 0]
                        AxisAlign: CenterRight
                    - Button:
                        Name: load_game
                        Rect: [0, 50, 1.0, 0]
                        AxisAlign: CenterRight
                ";
            using (var parser = new YamlParser(">LayoutElement<", new StringReader(yaml)))
            {
                ParserDump(parser);

                var list = parser.DeserializeOne<LayoutElement>();

                Assert.AreEqual("List", list.Type);
                Assert.AreEqual("buttons", list.Name);
                Assert.That.Equal(0.01f, new Vector4(-20f,0.22f,200f,600f), list.Rect);
                Assert.AreEqual(Align.CenterRight, list.AxisAlign);
                Assert.AreEqual(2, list.Children1.Length);
                Assert.AreEqual(2, list.Children2.Count);
                
                Assert.AreEqual("Button", list.Children1[0].Type);
                Assert.AreEqual("new_game", list.Children1[0].Name);
                Assert.That.Equal(0.01f, new Vector4(0, 0, 1, 0), list.Children1[0].Rect);
                Assert.AreEqual(Align.CenterRight, list.Children1[0].AxisAlign);
                
                Assert.AreEqual("Button", list.Children1[1].Type);
                Assert.AreEqual("load_game", list.Children1[1].Name);
                Assert.That.Equal(0.01f, new Vector4(0, 50, 1, 0), list.Children1[1].Rect);
                Assert.AreEqual(Align.CenterRight, list.Children1[1].AxisAlign);
                
                Assert.AreEqual("Button", list.Children2[0].Type);
                Assert.AreEqual("new_game", list.Children2[0].Name);
                Assert.That.Equal(0.01f, new Vector4(0, 0, 1, 0), list.Children2[0].Rect);
                Assert.AreEqual(Align.CenterRight, list.Children2[0].AxisAlign);
                
                Assert.AreEqual("Button", list.Children2[1].Type);
                Assert.AreEqual("load_game", list.Children2[1].Name);
                Assert.That.Equal(0.01f, new Vector4(0, 50, 1, 0), list.Children2[1].Rect);
                Assert.AreEqual(Align.CenterRight, list.Children2[1].AxisAlign);
            }
        }

        [TestMethod]
        public void ParseSunLayers()
        {
            const string yaml = @"
                Sun:
                  Id: star_red2
                  IconPath: Suns/star_red2_icon.png
                  IconLayer: 2 
                  IconScale: 2.5
                  LightIntensity: 1.4
                  Radius: 100000
                  LightColor: [255,160,122] # LightSalmon
                  Habitable: true
                  Layers:
                    - Layer:
                      TexturePath: Suns/star_yellow.png
                      TextureColor: [1.0,0.8,0.8,0.5]
                      BlendMode: Additive
                      LayerScale: 2.5
                      RotationSpeed: -0.015
                      PulsePeriod: 5.0
                      PulseScale: [0.96,1.08]
                      PulseColor: [1.1,1.1]
                    - Layer:
                      AnimationPath: sd_explosion_12a_bb
                      AnimationSpeed: 0.1
                      LayerScale: 1.0
                      TextureColor: 0.5
                      BlendMode: Additive
                      RotationSpeed: 0.05
                      PulsePeriod: 5
                      PulseScale: [0.96,1.04]
                      PulseColor: [0.5,0.9]
                ";
            using (var parser = new YamlParser(">SunLayers<", new StringReader(yaml)))
            {
                ParserDump(parser);

                var sun = parser.DeserializeOne<SunType>();
                Assert.AreEqual("star_red2", sun.Id);
                Assert.AreEqual("Suns/star_red2_icon.png", sun.IconPath);
                Assert.AreEqual(2, sun.IconLayer);
                Assert.AreEqual(2.5f, sun.IconScale);
                Assert.AreEqual(1.4f, sun.LightIntensity);
                Assert.AreEqual(100000f, sun.Radius);
                Assert.AreEqual(new Color(255,160,122,255), sun.LightColor);
                Assert.AreEqual(true, sun.Habitable);

                Assert.AreEqual(2, sun.Layers.Count);
                Assert.AreEqual("Suns/star_yellow.png", sun.Layers[0].TexturePath);
                Assert.AreEqual(new Color(1.0f,0.8f,0.8f,0.5f), sun.Layers[0].TextureColor);
                Assert.AreEqual(SpriteBlendMode.Additive, sun.Layers[0].BlendMode);
                Assert.AreEqual(2.5f, sun.Layers[0].LayerScale);
                Assert.AreEqual(-0.015f, sun.Layers[0].RotationSpeed);
                Assert.AreEqual(5.0f, sun.Layers[0].PulsePeriod);
                Assert.AreEqual(new Range(0.96f,1.08f), sun.Layers[0].PulseScale);
                Assert.AreEqual(new Range(1.1f,1.1f), sun.Layers[0].PulseColor);
            }
        }

        enum MapKeys { House, Plane, Ship, }

        [StarDataType]
        class SettingsMap
        {
            #pragma warning disable 649
            [StarData] public Map<MapKeys, float> Settings;
            #pragma warning restore 649
        }

        [TestMethod]
        public void ParseKeyValueMap()
        {
            const string yaml = @"
                SettingsMap:
                  Settings:
                    House: 1.1
                    Plane: 2.2
                    Ship: 3.3
                ";
            using (var parser = new YamlParser(">SettingsMap<", new StringReader(yaml)))
            {
                ParserDump(parser);
                var map = parser.DeserializeOne<SettingsMap>();

                Assert.AreEqual(3, map.Settings.Count);
                Assert.AreEqual(1.1f, map.Settings[MapKeys.House]);
                Assert.AreEqual(2.2f, map.Settings[MapKeys.Plane]);
                Assert.AreEqual(3.3f, map.Settings[MapKeys.Ship]);
            }
        }
    }
}
