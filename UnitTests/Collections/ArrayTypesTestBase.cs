using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;

namespace UnitTests.Collections
{
    public abstract class ArrayTypesTestBase : StarDriveTest
    {
        public abstract IArray<T> New<T>();
        public abstract IArray<T> New<T>(params T[] args);
        public IArray<string> MakeABCDE() => New("a","b","c","d","e") ;

        [TestMethod]
        public void Add()
        {
            var arr = New<int>();
            arr.Add(1);
            AssertEqual(arr.Count, 1, "Count should be 1");
            AssertEqual(arr.Capacity, 4, "Capacity should be 4");
            arr.Add(2);
            arr.Add(3);
            arr.Add(4);
            arr.Add(5);
            AssertEqual(5, arr.Count, "Count should be 5");
            AssertEqual(8, arr.Capacity, "Capacity should grow aligned to 4, expected 8");
        }

        [TestMethod]
        public void Insert()
        {
            var arr = New<string>();
            arr.Insert(0, "a");
            arr.Insert(1, "c");
            AssertEqual(new[] { "a", "c" }, arr);
            arr.Insert(1, "b");
            AssertEqual(new[] { "a", "b", "c" }, arr);
        }

        [TestMethod]
        public void Contains()
        {
            var arr = New<string>( "a", "b", "c", "d" );
            Assert.IsTrue(arr.Contains("c"), "Contains should work for existing items");
            Assert.IsFalse(arr.Contains("x"), "Contains should not give false positives");
            arr.Add(null);
            Assert.IsTrue(arr.Contains(null), "Contains must detect null properly");
        }

        [TestMethod]
        public void IndexOf()
        {
            var arr = New("a", "b", "c", "d");
            AssertEqual(2 , arr.IndexOf("c"), "IndexOf should work for existing items");
            AssertEqual(-1, arr.IndexOf("x"), "IndexOf should not give false positives");
            arr.Add(null);
            AssertEqual(4, arr.IndexOf((string)null), "IndexOf must detect null properly");
        }

        [TestMethod]
        public void RemoveAt()
        {
            var arr = MakeABCDE();
            arr.RemoveAt(2);
            AssertEqual(new[] { "a", "b", "d", "e" }, arr);
            arr.RemoveAt(0);
            AssertEqual(new[] { "b", "d", "e" }, arr);
            arr.RemoveAt(arr.Count - 1);
            AssertEqual(new[] { "b", "d" }, arr);
            arr.RemoveAt(arr.Count - 1);
            AssertEqual(new[] { "b" }, arr);
            arr.RemoveAt(0);
            Assert.IsTrue(arr.IsEmpty);
        }

        [TestMethod]
        public void RemoveAtSwapLast()
        {
            // SwapLast is unstable, so the elements will be swapped around instead
            // of doing an expensive array unshift
            var arr = MakeABCDE();
            arr.RemoveAtSwapLast(2);
            AssertEqual(new[] { "a", "b", "e", "d" }, arr);
            arr.RemoveAtSwapLast(0);
            AssertEqual(new[] { "d", "b", "e" }, arr);
            arr.RemoveAtSwapLast(arr.Count - 1);
            AssertEqual(new[] { "d", "b" }, arr);
            arr.RemoveAtSwapLast(arr.Count - 1);
            AssertEqual(new[] { "d" }, arr);
            arr.RemoveAtSwapLast(0);
            Assert.IsTrue(arr.IsEmpty);
        }

        [TestMethod]
        public void Remove()
        {
            var arr = New("a", "b", "c", "b", "d");
            arr.Remove("b");
            AssertEqual(new[] { "a", "c", "b", "d" }, arr);
            arr.Remove("nope");
            AssertEqual(new[] { "a", "c", "b", "d" }, arr);
            arr.Remove("d");
            AssertEqual(new[] { "a", "c", "b" }, arr);
            arr.Remove("a");
            AssertEqual(new[] { "c", "b" }, arr);
            arr.Remove("b");
            AssertEqual(new[] { "c" }, arr);
            arr.Remove("c");
            Assert.IsTrue(arr.IsEmpty);
        }

        [TestMethod]
        public void RemoveSwapLast()
        {
            var arr = New("a", "b", "c", "d");
            arr.RemoveSwapLast("b");
            AssertEqual(3, arr.Count);
            AssertEqual(new[] { "a", "d", "c" }, arr);
        }

        [TestMethod]
        public void PopFirst()
        {
            var arr = new Array<string> { "a", "b", "c" };

            AssertEqual("a", arr.PopFirst());
            AssertEqual(new[] { "b", "c" }, arr);

            AssertEqual("b", arr.PopFirst());
            AssertEqual(new[] { "c" }, arr);

            AssertEqual("c", arr.PopFirst());
            Assert.IsTrue(arr.IsEmpty);
        }

        [TestMethod]
        public void PopLast()
        {
            var arr = new Array<string> { "a", "b", "c" };

            AssertEqual("c", arr.PopLast());
            AssertEqual(new[] { "a", "b" }, arr);

            AssertEqual("b", arr.PopLast());
            AssertEqual(new[] { "a" }, arr);

            AssertEqual("a", arr.PopFirst());
            Assert.IsTrue(arr.IsEmpty);
        }

        [TestMethod]
        public void TryPopLast()
        {
            var arr = new Array<string> { "a", "b", "c" };

            Assert.IsTrue(arr.TryPopLast(out string c));
            AssertEqual("c", c);
            AssertEqual(new[] { "a", "b" }, arr);
            
            Assert.IsTrue(arr.TryPopLast(out string b));
            AssertEqual("b", b);
            AssertEqual(new[] { "a" }, arr);
            
            Assert.IsTrue(arr.TryPopLast(out string a));
            AssertEqual("a", a);
            Assert.IsTrue(arr.IsEmpty);

            Assert.IsFalse(arr.TryPopLast(out string _));
        }
    }
}
