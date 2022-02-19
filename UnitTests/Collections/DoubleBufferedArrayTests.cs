using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Utils;
using UnitTests.Ships;

namespace UnitTests.Utils
{
    [TestClass]
    public class DoubleBufferedArrayTests : StarDriveTest
    {
        class DummyShip : GameplayObject
        {
            string Name;
            public DummyShip(string name) : base(0, GameObjectType.Ship)
            {
                Name = name;
            }
            public override string ToString() => $"DummyShip {Name}";
        }
        [TestMethod]
        public void AddDoesNotAffectFrontBuffer()
        {
            var arr = new GameObjectList<GameplayObject>();

            arr.Add(new DummyShip("ship1"));
            var front = arr.GetItems();
            Assert.AreEqual(0, front.Length);
            arr.Add(new DummyShip("ship2"));
            Assert.AreEqual(0, front.Length);

            arr.ApplyChanges();
            Assert.AreEqual(0, front.Length, "Front should still be unmodified after ApplyChanges");

            var newFront = arr.GetItems();
            Assert.AreEqual(2, newFront.Length);
            arr.Add(new DummyShip("ship3"));
            Assert.AreEqual(2, newFront.Length);

            arr.ApplyChanges();
            Assert.AreEqual(2, newFront.Length);
            var newFront2 = arr.GetItems();
            Assert.AreEqual(3, newFront2.Length);
        }
    }
}
