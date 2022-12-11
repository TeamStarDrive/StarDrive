using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.YamlSerializer;

namespace UnitTests.Serialization;

[TestClass]
public class YamlSerializerTests : StarDriveTest
{
    static string SerializeToString<T>(T instance)
    {
        var sw = new StringWriter();
        YamlSerializer.SerializeOne(sw, instance);
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
            InheritOwnerVelocity = 2,
            AlignRotationToVelocity = 1,
            RandomVelocityXY = new []{ new Range(3, 4.2f), new Range(5.4f, 5.8f) },
            AlignRandomVelocityXY = true,
            EndVelocity = 6,
            StartColorRange = new[]{ new Color(10,20,30,255), new Color(40,50,60,255) },
            EndColorRange = new[] { new Color(0, 0, 255, 255) },
            EndColorTime = 0.75f,
            RotateSpeed = new Range(7,8),
            StartEndSize = new[]{ new Range(9,10), new Range(11,12) },
            SrcDstBlend = new[]{ Blend.BlendFactor, Blend.InverseBlendFactor },
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
                  InheritOwnerVelocity: 2
                  AlignRotationToVelocity: 1
                  RandomVelocityXY: [[3,4.2],[5.4,5.8]]
                  AlignRandomVelocityXY: true
                  EndVelocity: 6
                  StartColorRange: [[10,20,30,255],[40,50,60,255]]
                  EndColorRange: [[0,0,255,255]]
                  EndColorTime: 0.75
                  RotateSpeed: [7,8]
                  StartEndSize: [[9,10],[11,12]]
                  SrcDstBlend: [BlendFactor,InverseBlendFactor]
                ".Replace("\r\n", "\n").Replace("                ", "");

        AssertEqual(yaml, text);
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
        AssertEqual(yaml, text);
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
        AssertEqual(yaml, text);
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
                    - BigValue:
                      Key: House
                      Value1: 1.1
                      Value2: 2.4
                      Value3: 3.6
                    - BigValue:
                      Key: Plane
                      Value1: 2.2
                      Value2: 3.4
                      Value3: 4.6
                ".Replace("\r\n", "\n").Replace("                ", "");
        AssertEqual(yaml, text);
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
        AssertEqual(yaml, text);
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
        AssertEqual(yaml, text);
    }

    [TestMethod]
    public void FleetDesigns()
    {
        var design = new FleetDesign
        {
            Name = "Test Fleet",
            FleetIconIndex = 9,
            Nodes =
            {
                new FleetDataDesignNode
                {
                    ShipName = "Ship 1",
                    RelativeFleetOffset = new Vector2(1,2),
                    VultureWeight = 0.1f,
                    AttackShieldedWeight = 0.2f,
                    AssistWeight = 0.3f,
                    DefenderWeight = 0.4f,
                    DPSWeight = 0.5f,
                    SizeWeight = 0.6f,
                    ArmoredWeight = 0.7f,
                    CombatState = CombatState.Evade,
                    OrdersRadius = 100000
                },
                new FleetDataDesignNode
                {
                    ShipName = "Ship 2",
                    RelativeFleetOffset = new Vector2(1,2),
                    VultureWeight = 0.1f,
                    AttackShieldedWeight = 0.2f,
                    AssistWeight = 0.3f,
                    DefenderWeight = 0.4f,
                    DPSWeight = 0.5f,
                    SizeWeight = 0.6f,
                    ArmoredWeight = 0.7f,
                    CombatState = CombatState.Evade,
                    OrdersRadius = 100000
                }
            }
        };

        string text = SerializeToString(design);
        string yaml =
            @"FleetDesign:
                  Name: ""Test Fleet""
                  FleetIconIndex: 9
                  Nodes:
                    - FleetDataDesignNode:
                      ShipName: ""Ship 1""
                      RelativeFleetOffset: [1,2]
                      VultureWeight: 0.1
                      AttackShieldedWeight: 0.2
                      AssistWeight: 0.3
                      DefenderWeight: 0.4
                      DPSWeight: 0.5
                      SizeWeight: 0.6
                      ArmoredWeight: 0.7
                      CombatState: Evade
                      OrdersRadius: 100000
                    - FleetDataDesignNode:
                      ShipName: ""Ship 2""
                      RelativeFleetOffset: [1,2]
                      VultureWeight: 0.1
                      AttackShieldedWeight: 0.2
                      AssistWeight: 0.3
                      DefenderWeight: 0.4
                      DPSWeight: 0.5
                      SizeWeight: 0.6
                      ArmoredWeight: 0.7
                      CombatState: Evade
                      OrdersRadius: 100000
                ".Replace("\r\n", "\n").Replace("                ", "");
        AssertEqual(yaml, text);
    }
}
