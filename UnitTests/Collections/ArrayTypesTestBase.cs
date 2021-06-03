using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        public void Insert()
        {
            var arr = New<string>();
            arr.Insert(0, "a");
            arr.Insert(1, "c");
            Assert.That.Equal(new[] { "a", "c" }, arr);
            arr.Insert(1, "b");
            Assert.That.Equal(new[] { "a", "b", "c" }, arr);
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
            Assert.AreEqual(2 , arr.IndexOf("c"), "IndexOf should work for existing items");
            Assert.AreEqual(-1, arr.IndexOf("x"), "IndexOf should not give false positives");
            arr.Add(null);
            Assert.AreEqual(4, arr.IndexOf((string)null), "IndexOf must detect null properly");
        }

        [TestMethod]
        public void RemoveAt()
        {
            var arr = MakeABCDE();
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
        public void RemoveAtSwapLast()
        {
            // SwapLast is unstable, so the elements will be swapped around instead
            // of doing an expensive array unshift
            var arr = MakeABCDE();
            arr.RemoveAtSwapLast(2);
            Assert.That.Equal(new[] { "a", "b", "e", "d" }, arr);
            arr.RemoveAtSwapLast(0);
            Assert.That.Equal(new[] { "d", "b", "e" }, arr);
            arr.RemoveAtSwapLast(arr.Count - 1);
            Assert.That.Equal(new[] { "d", "b" }, arr);
            arr.RemoveAtSwapLast(arr.Count - 1);
            Assert.That.Equal(new[] { "d" }, arr);
            arr.RemoveAtSwapLast(0);
            Assert.IsTrue(arr.IsEmpty);
        }

        [TestMethod]
        public void Remove()
        {
            var arr = New("a", "b", "c", "b", "d");
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
        public void RemoveSwapLast()
        {
            var arr = New("a", "b", "c", "d");
            arr.RemoveSwapLast("b");
            Assert.AreEqual(3, arr.Count);
            Assert.That.Equal(new[] { "a", "d", "c" }, arr);
        }

        [TestMethod]
        public void PopFirst()
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
        public void PopLast()
        {
            var arr = new Array<string> { "a", "b", "c" };

            Assert.AreEqual("c", arr.PopLast());
            Assert.That.Equal(new[] { "a", "b" }, arr);

            Assert.AreEqual("b", arr.PopLast());
            Assert.That.Equal(new[] { "a" }, arr);

            Assert.AreEqual("a", arr.PopFirst());
            Assert.IsTrue(arr.IsEmpty);
        }

        [TestMethod]
        public void TryPopLast()
        {
            var arr = new Array<string> { "a", "b", "c" };

            Assert.IsTrue(arr.TryPopLast(out string c));
            Assert.AreEqual("c", c);
            Assert.That.Equal(new[] { "a", "b" }, arr);
            
            Assert.IsTrue(arr.TryPopLast(out string b));
            Assert.AreEqual("b", b);
            Assert.That.Equal(new[] { "a" }, arr);
            
            Assert.IsTrue(arr.TryPopLast(out string a));
            Assert.AreEqual("a", a);
            Assert.IsTrue(arr.IsEmpty);

            Assert.IsFalse(arr.TryPopLast(out string _));
        }
    }
}
