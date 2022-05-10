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
    public class GameObjectListTests : StarDriveTest
    {
        class DummyShip : GameObject
        {
            string Name;
            public DummyShip(int id) : base(id, GameObjectType.Ship)
            {
                Name = "ship"+id;
            }
            public override string ToString() => $"DummyShip {Name}";
        }

        [TestMethod]
        public void AddDoesNotAffectFrontBuffer()
        {
            var arr = new GameObjectList<GameObject>();

            arr.Add(new DummyShip(1));
            var front = arr.GetItems();
            Assert.AreEqual(0, front.Length);
            arr.Add(new DummyShip(2));
            Assert.AreEqual(0, front.Length);

            arr.ApplyChanges();
            Assert.AreEqual(0, front.Length, "Front should still be unmodified after ApplyChanges");

            var newFront = arr.GetItems();
            Assert.AreEqual(2, newFront.Length);
            arr.Add(new DummyShip(3));
            Assert.AreEqual(2, newFront.Length);

            arr.ApplyChanges();
            Assert.AreEqual(2, newFront.Length);
            var newFront2 = arr.GetItems();
            Assert.AreEqual(3, newFront2.Length);
        }

        [TestMethod]
        public void FindDoesNotRequireApplyChanges()
        {
            var arr = new GameObjectList<GameObject>();
            var first = new DummyShip(1);
            var second = new DummyShip(2);

            arr.Add(first);
            Assert.IsTrue(arr.Contains(1), "List should contain element");
            Assert.AreEqual(first, arr.Find(1), "List Find must return correct element");

            arr.ApplyChanges();
            Assert.IsTrue(arr.Contains(1), "List should contain element");
            Assert.AreEqual(first, arr.Find(1), "List Find must return correct element");
            
            arr.Add(second);
            Assert.IsTrue(arr.Contains(1), "List should contain element");
            Assert.IsTrue(arr.Contains(2), "List should contain element");
            Assert.AreEqual(first, arr.Find(1), "List Find must return correct element");
            Assert.AreEqual(second, arr.Find(2), "List Find must return correct element");

            arr.ApplyChanges();
            Assert.IsTrue(arr.Contains(1), "List should contain element");
            Assert.IsTrue(arr.Contains(2), "List should contain element");
            Assert.AreEqual(first, arr.Find(1), "List Find must return correct element");
            Assert.AreEqual(second, arr.Find(2), "List Find must return correct element");

            Assert.IsFalse(arr.Contains(3), "List should not contain this element");
            Assert.AreEqual(null, arr.Find(3), "List Find should not return an invalid element");

            arr.ClearAndApplyChanges();
            Assert.AreEqual(0, arr.NumBackingItems);
            Assert.IsFalse(arr.Contains(1), "Empty list should not contain stale elements");
            Assert.IsFalse(arr.Contains(2), "Empty list should not contain stale elements");
        }

        [TestMethod]
        public void InActiveElementsAreRemovedOnDemand()
        {
            var arr = new GameObjectList<GameObject>();
            var first = new DummyShip(1);
            var second = new DummyShip(2);

            arr.Add(first);
            arr.Add(second);
            arr.ApplyChanges();
            Assert.IsTrue(arr.Contains(1), "List should contain element");
            Assert.IsTrue(arr.Contains(2), "List should contain element");

            first.Active = false;
            arr.RemoveInActiveAndApplyChanges();
            Assert.AreEqual(1, arr.NumBackingItems);
            Assert.IsFalse(arr.Contains(1), "List should not contain removed elements");
            Assert.IsTrue(arr.Contains(2), "List should contain element");
        }
    }
}
