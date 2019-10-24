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
        public void TestAdd()
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
        public void TestInsert()
        {
            var arr = new Array<string>();
            arr.Insert(0, "a");
            arr.Insert(1, "c");
            Assert.That.Equal(new[] { "a", "c" }, arr);
            arr.Insert(1, "b");
            Assert.That.Equal(new[] { "a", "b", "c" }, arr);
        }

        [TestMethod]
        public void TestContains()
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
        public void TestRemoveAt()
        {
            var arr = new Array<string> { "a", "b", "c", "d", "e" };
            arr.RemoveAt(2);
            Assert.That.Equal(new[] { "a", "b", "d", "e" }, arr);
            arr.RemoveAt(0);
            Assert.That.Equal(new[] { "b", "d", "e" }, arr);
            arr.RemoveAt(arr.Count - 1);
            Assert.That.Equal(new[] { "b", "d" }, arr);
            arr.RemoveAt(arr.Count - 1);
            Assert.That.Equal(new[] { "b" }, arr);
            arr.RemoveAt(0);
            Assert.IsTrue(arr.IsEmpty);
        }

        [TestMethod]
        public void TestRemove()
        {
            var arr = new Array<string> { "a", "b", "c", "b", "d" };
            arr.Remove("b");
            Assert.That.Equal(new[] { "a", "c", "b", "d" }, arr);
            arr.Remove("nope");
            Assert.That.Equal(new[] { "a", "c", "b", "d" }, arr);
            arr.Remove("d");
            Assert.That.Equal(new[] { "a", "c", "b" }, arr);
            arr.Remove("a");
            Assert.That.Equal(new[] { "c", "b" }, arr);
            arr.Remove("b");
            Assert.That.Equal(new[] { "c" }, arr);
            arr.Remove("c");
            Assert.IsTrue(arr.IsEmpty);
        }

        [TestMethod]
        public void TestRemoveFirst()
        {
            var arr = new Array<string> { "a", "b", "c", "b", "d" };
            arr.RemoveFirst(s => s == "b");
            Assert.That.Equal(new[] { "a", "c", "b", "d" }, arr);
        }

        [TestMethod]
        public void TestRemoveLast()
        {
            var arr = new Array<string> { "a", "b", "c", "b", "d" };
            arr.RemoveLast(s => s == "b");
            Assert.That.Equal(new[] { "a", "b", "c", "d" }, arr);
        }

        [TestMethod]
        public void TestRemoveSwapLast()
        {
            var arr = new Array<string> { "a", "b", "c", "d" };
            arr.RemoveSwapLast("b");
            Assert.AreEqual(3, arr.Count);
            Assert.That.Equal(new[] { "a", "d", "c" }, arr);
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
        public void TestRemoveDuplicates()
        {
            var arr = new Array<string> { "a", "a", "b", "c", "d", "c", "c" };
            // @note RemoveDuplicates is an UNSTABLE algorithm, which means
            //       item ordering can change
            arr.RemoveDuplicates();
            Assert.AreEqual(4, arr.Count);
            arr.Sort();
            Assert.That.Equal(new[] { "a", "b", "c", "d" }, arr);
        }

        [TestMethod]
        public void TestAddRange()
        {
            var src = new[] { "a", "b", "c" };
            var dst = new Array<string>();
            dst.AddRange(src);
            Assert.AreEqual(dst.Count, 3);
            Assert.AreEqual(dst.Capacity, 4);
            Assert.That.Equal(dst, new[] { "a", "b", "c" });

            dst.AddRange(src);
            Assert.AreEqual(dst.Count, 6);
            Assert.AreEqual(dst.Capacity, 8);
            Assert.That.Equal(dst, new[] { "a", "b", "c", "a", "b", "c" });
        }

        [TestMethod]
        public void TestToArrayList()
        {
            var arr = new[] { "a", "b", "c" };
            var arr1 = new Array<string>();
            arr1.AddRange(arr);
            Assert.That.Equal(arr1, arr);
            Assert.ThrowsException<InvalidOperationException>(() => arr1.ToArrayList());

            var arr2 = ((ICollection<string>)arr1).ToArrayList();
            Assert.That.Equal(arr, arr2);

            arr2 = ((IReadOnlyList<string>)arr1).ToArrayList();
            Assert.That.Equal(arr, arr2);

            arr2 = ((IReadOnlyCollection<string>)arr1).ToArrayList();
            Assert.That.Equal(arr, arr2);

            arr2 = ((IEnumerable<string>)arr1).ToArrayList();
            Assert.That.Equal(arr, arr2);
        }
        
        [TestMethod]
        public void TestToArray()
        {
            var arr = new[] { "a", "b", "c" };
            var arr1 = new Array<string>();
            arr1.AddRange(arr);
            Assert.That.Equal(arr, arr1);

            string[] arr2 = ((ICollection<string>)arr1).ToArray();
            Assert.That.Equal(arr, arr2);

            arr2 = ((IReadOnlyList<string>)arr1).ToArray();
            Assert.That.Equal(arr, arr2);

            arr2 = ((IReadOnlyCollection<string>)arr1).ToArray();
            Assert.That.Equal(arr, arr2);

            arr2 = ((IEnumerable<string>)arr1).ToArray();
            Assert.That.Equal(arr, arr2);
        }

        [TestMethod]
        public void TestPopFirst()
        {
            var arr = new Array<string> { "a", "b", "c" };

            Assert.AreEqual("a", arr.PopFirst());
            Assert.That.Equal(new[] { "b", "c" }, arr);

            Assert.AreEqual("b", arr.PopFirst());
            Assert.That.Equal(new[] { "c" }, arr);

            Assert.AreEqual("c", arr.PopFirst());
            Assert.IsTrue(arr.IsEmpty);
        }

        [TestMethod]
        public void TestPopLast()
        {
            var arr = new Array<string> { "a", "b", "c" };

            Assert.AreEqual("c", arr.PopLast());
            Assert.That.Equal(new[] { "a", "b" }, arr);

            Assert.AreEqual("b", arr.PopLast());
            Assert.That.Equal(new[] { "a" }, arr);

            Assert.AreEqual("a", arr.PopFirst());
            Assert.IsTrue(arr.IsEmpty);
        }
    }
}
