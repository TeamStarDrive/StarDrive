using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Serialization;

namespace UnitTests.Serialization
{
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
            public ShipObject(int id, string name, Vector2 pos)
            {
                Id = id;
                Name = name;
                Position = pos;
                Info = new ShipInfo(new Point(15, 20), 72); // intentionally duplicate for all ships
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

        (RootObject, RecursiveScanner) CreateDefaultRootObject()
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
            var rs = new RecursiveScanner(new BinarySerializer(root.GetType()), root);
            return (root, rs);
        }

        [TestMethod]
        public void PrepareTypes()
        {
            (RootObject _, RecursiveScanner rs) = CreateDefaultRootObject();

            Assert.AreEqual(1, rs.ValueTypes.Length);
            Assert.AreEqual("ShipInfo", rs.ValueTypes[0].NiceTypeName);
            Assert.AreEqual(2, rs.ClassTypes.Length);
            Assert.AreEqual("ShipObject", rs.ClassTypes[0].NiceTypeName, "RootObject depends on ShipObject");
            Assert.AreEqual("RootObject", rs.ClassTypes[1].NiceTypeName, "RootObject should be last");
            Assert.AreEqual(1, rs.CollectionTypes.Length);
            Assert.AreEqual("Array<ShipObject>", rs.CollectionTypes[0].NiceTypeName);
        }

        [TestMethod]
        public void CreateWriteCommands()
        {
            (RootObject root, RecursiveScanner rs) = CreateDefaultRootObject();
            rs.CreateWriteCommands();

            Assert.AreEqual(15, rs.NumObjects);
            Assert.AreEqual(1, rs.RootObjectId);
            Assert.AreEqual(8, rs.TypeGroups.Length);

            Assert.AreEqual("RootObject", rs.TypeGroups[7].Type.NiceTypeName);
            var rootOS = (RecursiveScanner.UserTypeState)rs.TypeGroups[7].GroupedObjects[0];
            Assert.AreEqual(rs.RootObjectId, rootOS.Id);
            Assert.AreEqual(root, rootOS.Obj);
            Assert.AreEqual(3, rootOS.Fields.Length);

            var name = rs.GetObject(rootOS.Fields[0]);
            var ships = rs.GetObject(rootOS.Fields[1]);
            var nullShip = rs.GetObject(rootOS.Fields[2]);
            Assert.AreEqual(root.Name, name);
            Assert.AreEqual(root.Ships, ships);
            Assert.AreEqual(null, nullShip);
        }
    }
}
