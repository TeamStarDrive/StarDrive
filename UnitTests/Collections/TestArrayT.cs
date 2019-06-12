using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Gameplay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Collections
{
    [TestClass]
    public class TestArrayT : StarDriveTest
    {
        [TestMethod]
        public void TestArrayAdd()
        {
            var arr = new Array<int>();
            arr.Add(1);
            Assert.AreEqual(arr.Count, 1, "Count should be 1");
            Assert.AreEqual(arr.Capacity, 4, "Capacity should be 4");
            arr.Add(2);
            arr.Add(3);
            arr.Add(4);
            arr.Add(5);
            Assert.AreEqual(5, arr.Count, "Count should be 5");
            Assert.AreEqual(8, arr.Capacity, "Capacity should grow aligned to 4, expected 8");
        }

        [TestMethod]
        public void TestArrayContains()
        {
            var arr = new Array<string> { "a", "b", "c", "d" };
            Assert.IsTrue(arr.Contains("c"), "Contains should work for existing items");
            Assert.IsFalse(arr.Contains("x"), "Contains should not give false positives");
            arr.Add(null);
            Assert.IsTrue(arr.Contains(null), "Contains must detect null properly");

            var obj = "c";
            var refs = new Array<string> { "a", "b", "c", "d" };
            refs.Add(obj);
            Assert.IsTrue(refs.ContainsRef(obj), "Contains should work for existing items");
            Assert.IsFalse(refs.ContainsRef("x"), "Contains should not give false positives");
            refs.Add(null);
            Assert.IsTrue(refs.ContainsRef(null), "Contains must detect null properly");
        }

        [TestMethod]
        public void TestArrayRemoveAll()
        {
            var arr = new Array<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            arr.RemoveAll(x => true);
            Assert.AreEqual(0, arr.Count, "RemoveAll true should erase all elements");

            arr = new Array<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            arr.RemoveAll(x => x % 2 == 1);
            Assert.AreEqual(4, arr.Count, "RemoveAll odd should remove half the elements");
        }

        
        [TestMethod]
        public void TestAddRange()
        {
            var src = new[] { "a", "b", "c" };
            var dst = new Array<string>();
            dst.AddRange(src);
            Assert.AreEqual(dst.Count, 3);
            Assert.AreEqual(dst.Capacity, 4);
            CollectionAssert.AreEqual(dst, new[] { "a", "b", "c" });

            dst.AddRange(src);
            Assert.AreEqual(dst.Count, 6);
            Assert.AreEqual(dst.Capacity, 8);
            CollectionAssert.AreEqual(dst, new[] { "a", "b", "c", "a", "b", "c" });
        }

        [TestMethod]
        public void TestToArrayList()
        {
            var arr = new[] { "a", "b", "c" };
            var arr1 = new Array<string>();
            arr1.AddRange(arr);
            CollectionAssert.AreEqual(arr, arr1);
            Assert.ThrowsException<InvalidOperationException>(() => arr1.ToArrayList());

            var arr2 = ((ICollection<string>)arr1).ToArrayList();
            CollectionAssert.AreEqual(arr, arr2);

            arr2 = ((IReadOnlyList<string>)arr1).ToArrayList();
            CollectionAssert.AreEqual(arr, arr2);

            arr2 = ((IReadOnlyCollection<string>)arr1).ToArrayList();
            CollectionAssert.AreEqual(arr, arr2);

            arr2 = ((IEnumerable<string>)arr1).ToArrayList();
            CollectionAssert.AreEqual(arr, arr2);
        }
        
        [TestMethod]
        public void TestToArray()
        {
            var arr = new[] { "a", "b", "c" };
            var arr1 = new Array<string>();
            arr1.AddRange(arr);
            CollectionAssert.AreEqual(arr, arr1);

            string[] arr2 = ((ICollection<string>)arr1).ToArray();
            CollectionAssert.AreEqual(arr, arr2);

            arr2 = ((IReadOnlyList<string>)arr1).ToArray();
            CollectionAssert.AreEqual(arr, arr2);

            arr2 = ((IReadOnlyCollection<string>)arr1).ToArray();
            CollectionAssert.AreEqual(arr, arr2);

            arr2 = ((IEnumerable<string>)arr1).ToArray();
            CollectionAssert.AreEqual(arr, arr2);
        }
    }
}
