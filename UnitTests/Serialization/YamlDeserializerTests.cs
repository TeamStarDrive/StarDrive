using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;
using Ship_Game.Universe.SolarBodies;
using Vector4 = SDGraphics.Vector4;

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
                AssertEqual("SunZone", zone.Name);
                AssertEqual("Weights", zone["Weights"].Name);
                AssertEqual(4, zone["Weights"].Count);

                var sunZone = parser.DeserializeOne<SunZoneData>();
                AssertEqual(SunZone.Any, sunZone.Zone);
                AssertEqual(4, sunZone.Weights.Length);
                
                AssertEqual(PlanetCategory.Barren, sunZone.Weights[0].Type);
                AssertEqual(20,                    sunZone.Weights[0].Weight);
                AssertEqual(PlanetCategory.Desert, sunZone.Weights[1].Type);
                AssertEqual(10,                    sunZone.Weights[1].Weight);
                AssertEqual(PlanetCategory.Steppe, sunZone.Weights[2].Type);
                AssertEqual(1,                     sunZone.Weights[2].Weight);
                AssertEqual(PlanetCategory.Tundra, sunZone.Weights[3].Type);
                AssertEqual(0,                     sunZone.Weights[3].Weight);
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

                AssertEqual("List", list.Type);
                AssertEqual("buttons", list.Name);
                AssertEqual(0.01f, new Vector4(-20f,0.22f,200f,600f), list.Rect);
                AssertEqual(Align.CenterRight, list.AxisAlign);
                AssertEqual(2, list.Children1.Length);
                AssertEqual(2, list.Children2.Count);
                
                AssertEqual("Button", list.Children1[0].Type);
                AssertEqual("new_game", list.Children1[0].Name);
                AssertEqual(0.01f, new Vector4(0, 0, 1, 0), list.Children1[0].Rect);
                AssertEqual(Align.CenterRight, list.Children1[0].AxisAlign);
                
                AssertEqual("Button", list.Children1[1].Type);
                AssertEqual("load_game", list.Children1[1].Name);
                AssertEqual(0.01f, new Vector4(0, 50, 1, 0), list.Children1[1].Rect);
                AssertEqual(Align.CenterRight, list.Children1[1].AxisAlign);
                
                AssertEqual("Button", list.Children2[0].Type);
                AssertEqual("new_game", list.Children2[0].Name);
                AssertEqual(0.01f, new Vector4(0, 0, 1, 0), list.Children2[0].Rect);
                AssertEqual(Align.CenterRight, list.Children2[0].AxisAlign);
                
                AssertEqual("Button", list.Children2[1].Type);
                AssertEqual("load_game", list.Children2[1].Name);
                AssertEqual(0.01f, new Vector4(0, 50, 1, 0), list.Children2[1].Rect);
                AssertEqual(Align.CenterRight, list.Children2[1].AxisAlign);
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
                AssertEqual("star_red2", sun.Id);
                AssertEqual("Suns/star_red2_icon.png", sun.IconPath);
                AssertEqual(2, sun.IconLayer);
                AssertEqual(2.5f, sun.IconScale);
                AssertEqual(1.4f, sun.LightIntensity);
                AssertEqual(100000f, sun.Radius);
                AssertEqual(new Color(255,160,122,255), sun.LightColor);
                AssertEqual(true, sun.Habitable);

                AssertEqual(2, sun.Layers.Count);
                AssertEqual("Suns/star_yellow.png", sun.Layers[0].TexturePath);
                AssertEqual(new Color(1.0f,0.8f,0.8f,0.5f), sun.Layers[0].TextureColor);
                AssertEqual(SpriteBlendMode.Additive, sun.Layers[0].BlendMode);
                AssertEqual(2.5f, sun.Layers[0].LayerScale);
                AssertEqual(-0.015f, sun.Layers[0].RotationSpeed);
                AssertEqual(5.0f, sun.Layers[0].PulsePeriod);
                AssertEqual(new Range(0.96f,1.08f), sun.Layers[0].PulseScale);
                AssertEqual(new Range(1.1f,1.1f), sun.Layers[0].PulseColor);
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

                AssertEqual(3, map.Settings.Count);
                AssertEqual(1.1f, map.Settings[MapKeys.House]);
                AssertEqual(2.2f, map.Settings[MapKeys.Plane]);
                AssertEqual(3.3f, map.Settings[MapKeys.Ship]);
            }
        }
    }
}
