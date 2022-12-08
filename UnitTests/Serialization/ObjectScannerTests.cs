using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Serialization;
#pragma warning disable CS0649

namespace UnitTests.Serialization;

[TestClass]
public class ObjectScannerTests : StarDriveTest
{
    [StarDataType]
    public class RootObject
    {
        [StarData] public string Name;
        [StarData] public Array<ShipObject> Ships;
        [StarData] public ShipObject NullShip;
    }

    [StarDataType]
    public class ShipObject
    {
        [StarData] public int Id;
        [StarData] public string Name;
        [StarData] public Vector2 Position;
        [StarData] public ShipInfo Info;
        [StarData] public Array<Array<ShipObject>> GroupsOfShips;

        [StarDataConstructor] ShipObject() {}

        public ShipObject(int id, string name, Vector2 pos)
        {
            Id = id;
            Name = name;
            Position = pos;
            Info = new ShipInfo(new Point(15, 20), 72); // intentionally duplicate for all ships
            GroupsOfShips = new();
            GroupsOfShips.Add(new Array<ShipObject>{ this });
        }
    }

    [StarDataType]
    public struct ShipInfo
    {
        [StarData] public Point GridSize;
        [StarData] public Point GridCenter;
        [StarData] public int NumSlots;
        public ShipInfo(Point gridSize, int numSlots)
        {
            GridSize = gridSize;
            GridCenter = new Point(gridSize.X/2, gridSize.Y/2);
            NumSlots = numSlots;
        }
    }

    ObjectScanner Scanner;

    (RootObject, ObjectScanner) CreateDefaultRootObject()
    {
        var root = new RootObject()
        {
            Name = "Universe",
            Ships = new Array<ShipObject>()
            {
                new(1, "Ship1", new Vector2(100, 200)),
                new(2, "Ship2", new Vector2(-100, 200)),
            },
            NullShip = null,
        };
        Scanner = new(new BinarySerializer(root.GetType()), root);
        return (root, Scanner);
    }

    [TestMethod]
    public void FinalizeTypes()
    {
        (RootObject _, var rs) = CreateDefaultRootObject();
        rs.FinalizeTypes();

        AssertEqual(1, rs.Types.Values.Length);
        AssertEqual("ShipInfo", rs.Types.Values[0].NiceTypeName);
        AssertEqual(2, rs.Types.Classes.Length);
        AssertEqual("ShipObject", rs.Types.Classes[0].NiceTypeName, "RootObject depends on ShipObject");
        AssertEqual("RootObject", rs.Types.Classes[1].NiceTypeName, "RootObject should be last");
        AssertEqual(2, rs.Types.Collections.Length);
        AssertEqual("Array<ShipObject>", rs.Types.Collections[0].NiceTypeName);
        AssertEqual("Array<Array<ShipObject>>", rs.Types.Collections[1].NiceTypeName);
    }

    [TestMethod]
    public void CreateWriteCommands()
    {
        (RootObject root, var rs) = CreateDefaultRootObject();
        rs.CreateWriteCommands();

        AssertEqual(19, rs.NumObjects);
        AssertEqual(14u, rs.RootObjectId);
        AssertEqual(9, rs.TypeGroups.Length);

        var rootGroup = rs.TypeGroups.First(g => g.Type.Type == typeof(RootObject));
        AssertEqual("RootObject", rootGroup.Type.NiceTypeName);
        var rootOS = (UserTypeState)rootGroup.GroupedObjects[0];
        AssertEqual(rs.RootObjectId, rootOS.Id);
        AssertEqual(root, rootOS.Obj);
        AssertEqual(3, rootOS.Fields.Length);

        var name = rs.GetObject(rootOS.Fields[0]);
        var ships = rs.GetObject(rootOS.Fields[1]);
        var nullShip = rs.GetObject(rootOS.Fields[2]);
        AssertEqual(root.Name, name);
        AssertEqual(root.Ships, ships);
        AssertEqual(null, nullShip);
    }

    [StarDataType]
    class RecursiveType2
    {
        [StarData] public Main R;
    }

    [StarDataType]
    class Main
    {
        [StarData] public Main Self;
        [StarData] public RecursiveType2 RecursiveProxy;
        [StarData] public Array<Main> Arr;
        [StarData] public Array<Array<Main>> ArrOfArrs;
        [StarData] public Map<string, Main> Map;
        [StarData] public Map<string, Array<Main>> MapOfArrs;
        [StarData] public Map<string, Map<string,Main>> AMapOfMaps;
    }

    TypeSerializer Find<T>()
    {
        return Scanner.Types.All.Find(s => s.Type == typeof(T));
    }
    int IndexOf(TypeSerializer s)
    {
        return Scanner.Types.All.IndexOf(s);
    }
    bool DependsOn(TypeSerializer type, TypeSerializer on)
    {
        return ObjectScanner.TypeDependsOn(type, on);
    }

    [TestMethod]
    public void TypeDependencyOrdering()
    {
        var root = new Main();
        var rs = Scanner = new(new(root.GetType()), root);
        rs.CreateWriteCommands();

        var mainType = Find<Main>();
        var recType = Find<RecursiveType2>();
        var arrType = Find<Array<Main>>();
        var arrOfArrs = Find<Array<Array<Main>>>();
        var mapOfMain = Find<Map<string, Main>>();
        var mapOfArrs = Find<Map<string, Array<Main>>>();
        var mapOfMaps = Find<Map<string, Map<string,Main>>>();

        Assert.IsTrue(DependsOn(recType, mainType), "Recursive subtype should depend on Main type");
        Assert.IsTrue(DependsOn(mainType, recType), "Main type should also depend on Recursive subtype");

        Assert.IsTrue(DependsOn(arrType, mainType), "Array<Main> should depend on Main type");
        Assert.IsTrue(DependsOn(arrOfArrs, mainType), "Array<Array<Main>> should depend on Main type");
        Assert.IsTrue(DependsOn(arrOfArrs, arrType), "Array<Array<Main>> should depend on Array<Main>");

        Assert.IsTrue(DependsOn(mapOfMain, mainType), "Map<K,Main> should depend on Main type");
        Assert.IsTrue(DependsOn(mapOfArrs, mainType), "Map<K,Array<Main>> should depend on Main type");
        Assert.IsTrue(DependsOn(mapOfArrs, arrType), "Map<K,Array<Main>> should depend on Array<Main> type");
        Assert.IsTrue(DependsOn(mapOfMaps, mainType), "Map<K,Map<K,Main>> should depend on Main type");
        Assert.IsTrue(DependsOn(mapOfMaps, mapOfMain), "Map<K,Map<K,Main>> should depend on Map<K,Main> type");

        Assert.IsFalse(DependsOn(mainType, mainType), "Self dependency is invalid");
        Assert.IsFalse(DependsOn(recType, recType), "Self dependency is invalid");
        Assert.IsFalse(DependsOn(arrType, arrType), "Self dependency is invalid");
        Assert.IsFalse(DependsOn(arrOfArrs, arrOfArrs), "Self dependency is invalid");
        Assert.IsFalse(DependsOn(mapOfMain, mapOfMain), "Self dependency is invalid");
        Assert.IsFalse(DependsOn(mapOfArrs, mapOfArrs), "Self dependency is invalid");
        Assert.IsFalse(DependsOn(mapOfMaps, mapOfMaps), "Self dependency is invalid");

        // regular types should NEVER depend on Collections!
        Assert.IsFalse(DependsOn(mainType, arrType), "Main type should NOT depend on Array<Main>");
        Assert.IsFalse(DependsOn(mainType, arrOfArrs), "Main type should NOT depend on Array<Array<Main>>");
        Assert.IsFalse(DependsOn(mainType, mapOfMain), "Main type should NOT depend on Map<K,Main>");
        Assert.IsFalse(DependsOn(mainType, mapOfArrs), "Main type should NOT depend on Map<K,Array<Main>>");
        Assert.IsFalse(DependsOn(mainType, mapOfMaps), "Main type should NOT depend on Map<K,Map<K,Main>>");

        AssertLessThan(IndexOf(mainType), IndexOf(arrType), "Main type must be before Array<Main>");
        AssertLessThan(IndexOf(arrType),  IndexOf(arrOfArrs), "Array<Main> must be before Array<Array<Main>>");
        AssertLessThan(IndexOf(mainType), IndexOf(mapOfMain), "Main type must be before Map<K,Main>");
        AssertLessThan(IndexOf(mainType), IndexOf(mapOfArrs), "Main type must be before Map<K,Array<Main>>");
        AssertLessThan(IndexOf(arrType),  IndexOf(mapOfArrs), "Array<Main> type must be before Map<K,Array<Main>>");
        AssertLessThan(IndexOf(mainType),  IndexOf(mapOfMaps), "Main type must be before Map<K,Map<K,Main>>");
        AssertLessThan(IndexOf(mapOfMain), IndexOf(mapOfMaps), "Map<K,Main> type must be before Map<K,Map<K,Main>>");
    }

    [StarDataType]
    class MapType2
    {
        [StarData] public Map<int, MapType2> SelfMap;
        [StarData] public Map<string, Map<int, Snapshot>> SnapsMap;
        [StarData] public Map<int, string> Map1;
        [StarData] public Map<int, Snapshot>[] Snapshots;
    }

    [TestMethod]
    public void MapDependencyOrdering()
    {
        var root = new MapType2();
        var rs = Scanner = new(new(root.GetType()), root);
        rs.CreateWriteCommands();

        var snaps = Find<Map<int, Snapshot>>();
        var arrOfMaps = Find<Map<int, Snapshot>[]>();
        var mapOfSnaps = Find<Map<string, Map<int, Snapshot>>>();

        // arrays have to be read before userclasses
        AssertLessThan(IndexOf(snaps), IndexOf(arrOfMaps), "Map<int,Snapshot> must be before Map<int,Snapshot>[] array");
        AssertLessThan(IndexOf(snaps), IndexOf(mapOfSnaps), "Map<int,Snapshot> must be before Map<string, Map<int,Snapshot>> type");
    }
}
