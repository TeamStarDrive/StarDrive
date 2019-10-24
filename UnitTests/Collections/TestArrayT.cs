using System;
using System.Collections;
using System.Collections.Generic;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Collections
{
    [TestClass]
    public class TestArrayT : StarDriveTest
    {
        [TestMethod]
        public void Add()
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
        public void Insert()
        {
            var arr = new Array<string>();
            arr.Insert(0, "a");
            arr.Insert(1, "c");
            Assert.That.Equal(new[] { "a", "c" }, arr);
            arr.Insert(1, "b");
            Assert.That.Equal(new[] { "a", "b", "c" }, arr);
        }

        [TestMethod]
        public void Contains()
        {
            var arr = new Array<string> { "a", "b", "c", "d" };
            Assert.IsTrue(arr.Contains("c"), "Contains should work for existing items");
            Assert.IsFalse(arr.Contains("x"), "Contains should not give false positives");
            arr.Add(null);
            Assert.IsTrue(arr.Contains(null), "Contains must detect null properly");
        }

        [TestMethod]
        public void ContainsRef()
        {
            string validRef = "c";
            string nonMatchingRef = new string('c', 1);
            var refs = new Array<string> { "a", "b", "c", "d", validRef, null };
            Assert.IsTrue(refs.ContainsRef(validRef), "ContainsRef should work for existing items");
            Assert.IsFalse(refs.ContainsRef("x"), "ContainsRef should not give false positives");
            Assert.IsFalse(refs.ContainsRef(nonMatchingRef), "ContainsRef should not give false positives");
            Assert.IsTrue(refs.ContainsRef(null), "ContainsRef must detect null properly");
        }

        [TestMethod]
        public void IndexOf()
        {
            var arr = new Array<string> { "a", "b", "c", "d" };
            Assert.AreEqual(2 , arr.IndexOf("c"), "IndexOf should work for existing items");
            Assert.AreEqual(-1, arr.IndexOf("x"), "IndexOf should not give false positives");
            arr.Add(null);
            Assert.AreEqual(4, arr.IndexOf((string)null), "IndexOf must detect null properly");
        }

        [TestMethod]
        public void IndexOfRef()
        {
            string validRef = "c";
            string nonMatchingRef = new string('c', 1);
            var refs = new Array<string> { "a", "b", "c", "d", validRef, null };
            Assert.AreEqual(2, refs.IndexOfRef(validRef), "IndexOfRef should work for existing items");
            Assert.AreEqual(-1, refs.IndexOfRef("x"), "IndexOfRef should not give false positives");
            Assert.AreEqual(-1, refs.IndexOfRef(nonMatchingRef), "IndexOfRef should not give false positives");
            Assert.AreEqual(5, refs.IndexOfRef((string)null), "IndexOfRef must detect null properly");
        }

        [TestMethod]
        public void FirstIndexOf()
        {
            var arr = new Array<string> { "a", null, "b", "c", "b", null, "d" };
            Assert.AreEqual(2, arr.FirstIndexOf(s => s == "b"), "FirstIndexOf(predicate) should work for existing items");
            Assert.AreEqual(-1, arr.FirstIndexOf(s => s == "x"), "FirstIndexOf(predicate) should not give false positives");
            Assert.AreEqual(1, arr.FirstIndexOf(s => s == null), "FirstIndexOf(predicate) must detect null properly");
        }

        [TestMethod]
        public void LastIndexOf()
        {
            var arr = new Array<string> { "a", null, "b", "c", "b", null, "d" };
            Assert.AreEqual(4, arr.LastIndexOf(s => s == "b"), "LastIndexOf(predicate) should work for existing items");
            Assert.AreEqual(-1, arr.LastIndexOf(s => s == "x"), "LastIndexOf(predicate) should not give false positives");
            Assert.AreEqual(5, arr.LastIndexOf(s => s == null), "LastIndexOf(predicate) must detect null properly");
        }

        [TestMethod]
        public void RemoveAt()
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
        public void RemoveAtSwapLast()
        {
            // SwapLast is unstable, so the elements will be swapped around instead
            // of doing an expensive array unshift
            var arr = new Array<string> { "a", "b", "c", "d", "e" };
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
        public void RemoveFirst()
        {
            var arr = new Array<string> { "a", "b", "c", "b", "d" };
            arr.RemoveFirst(s => s == "b");
            Assert.That.Equal(new[] { "a", "c", "b", "d" }, arr);
        }

        [TestMethod]
        public void RemoveLast()
        {
            var arr = new Array<string> { "a", "b", "c", "b", "d" };
            arr.RemoveLast(s => s == "b");
            Assert.That.Equal(new[] { "a", "b", "c", "d" }, arr);
        }

        [TestMethod]
        public void RemoveSwapLast()
        {
            var arr = new Array<string> { "a", "b", "c", "d" };
            arr.RemoveSwapLast("b");
            Assert.AreEqual(3, arr.Count);
            Assert.That.Equal(new[] { "a", "d", "c" }, arr);
        }

        [TestMethod]
        public void RemoveAll()
        {
            var arr = new Array<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            arr.RemoveAll(x => true);
            Assert.AreEqual(0, arr.Count, "RemoveAll true should erase all elements");

            arr = new Array<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            arr.RemoveAll(x => x % 2 == 1);
            Assert.AreEqual(4, arr.Count, "RemoveAll odd should remove half the elements");
        }

        [TestMethod]
        public void RemoveRange()
        {
            var arr = new Array<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            arr.RemoveRange(0, 4);
            Assert.That.Equal(new []{5, 6, 7, 8}, arr);

            arr = new Array<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            arr.RemoveRange(4, 4);
            Assert.That.Equal(new []{1, 2, 3, 4}, arr);

            arr = new Array<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            arr.RemoveRange(2, 4);
            Assert.That.Equal(new []{1, 2, 7, 8}, arr);
        }

        [TestMethod]
        public void RemoveDuplicates()
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
        public void RemoveDuplicateRefs()
        {
            var arr = new Array<string> { "a", "a", "b", "c", "d", "c", "c" };
            // @note RemoveDuplicates is an UNSTABLE algorithm, which means
            //       item ordering can change
            arr.RemoveDuplicateRefs();
            Assert.AreEqual(4, arr.Count);
            arr.Sort();
            Assert.That.Equal(new[] { "a", "b", "c", "d" }, arr);
        }

        
        [TestMethod]
        public void Reverse()
        {
            var arr = new Array<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            arr.Reverse();
            Assert.That.Equal(new []{8, 7, 6, 5, 4, 3, 2, 1}, arr);

            arr = new Array<int> { 1, 2, 3, 4, 5, 6, 7 };
            arr.Reverse();
            Assert.That.Equal(new []{7, 6, 5, 4, 3, 2, 1}, arr);
        }

        [TestMethod]
        public void ForEach()
        {
            string sum = "";
            var arr = new Array<string> { "a", "b", "c", "d" };
            arr.ForEach(s => sum += s);
            Assert.AreEqual("abcd", sum);
        }

        [TestMethod]
        public void AddRange()
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

        class ReadOnlyList<T> : IReadOnlyList<T>, ICollection
        {
            readonly T[] Items;
            public ReadOnlyList(T[] items)  { Items = items; }
            public int Count => Items.Length;
            public object SyncRoot => this;
            public bool IsSynchronized => false;
            public T this[int index] => Items[index];
            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
            public void CopyTo(Array array, int index) => Items.CopyTo(array, index);
        }

        class ReadOnlyCollection<T> : IReadOnlyCollection<T>, ICollection
        {
            readonly T[] Items;
            public ReadOnlyCollection(T[] items)  { Items = items; }
            int ICollection.Count => Items.Length;
            int IReadOnlyCollection<T>.Count => Items.Length;
            public object SyncRoot => this;
            public bool IsSynchronized => false;
            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => Items.CopyTo(array, index);
        }

        class Enumerable<T> : IEnumerable<T>, ICollection
        {
            readonly T[] Items;
            public Enumerable(T[] items)  { Items = items; }
            public int Count => Items.Length;
            public object SyncRoot => this;
            public bool IsSynchronized => false;
            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => Items.CopyTo(array, index);
        }

        [TestMethod]
        public void Construct()
        {
            string[] items = { "a", "b", "c", "d" };
            var arr = new Array<string> { "a", "b", "c", "d" };
            Assert.That.Equal(arr, items);
            Assert.That.Equal(arr, new Array<string>(arr));
            Assert.That.Equal(arr, new Array<string>(items));
            Assert.That.Equal(arr, arr.Clone());

            ICollection<string> collection = new List<string>(items);
            Assert.That.Equal(collection, new Array<string>(collection));
            Assert.That.Equal(arr, new Array<string>(collection));

            IReadOnlyList<string> constList = new ReadOnlyList<string>(items);
            Assert.That.Equal(constList, new Array<string>(constList));
            Assert.That.Equal(arr, new Array<string>(constList));

            IReadOnlyCollection<string> constCollection = new ReadOnlyCollection<string>(items);
            Assert.That.Equal(constCollection, new Array<string>(constCollection));
            Assert.That.Equal(arr, new Array<string>(constCollection));

            IEnumerable<string> enumerable = new Enumerable<string>(items);
            Assert.That.Equal(enumerable, new Array<string>(enumerable));
            Assert.That.Equal(arr, new Array<string>(enumerable));

            Assert.That.Equal(arr, new Array<string>((IEnumerable<string>)collection));
            Assert.That.Equal(arr, new Array<string>((IEnumerable<string>)constList));
            Assert.That.Equal(arr, new Array<string>((IEnumerable<string>)constCollection));
            Assert.That.Equal(arr, new Array<string>((IEnumerable<string>)enumerable));
        }

        [TestMethod]
        public void ToArrayList()
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
        public void ToArray()
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

        [TestMethod]
        public void Filter()
        {
            var arr = new Array<string> { "a", "b", "c", "b", "d", "e" };

            Assert.That.Equal(new[]{ "a" }, arr.Filter(x => x == "a"));
            Assert.That.Equal(new[]{ "b", "b", "e"}, arr.Filter(x => x == "b" || x == "e"));
            Assert.That.Equal(new string[]{ }, arr.Filter(x => x == "x"));
        }

        [TestMethod]
        public void SubRange()
        {
            var arr = new Array<string> { "a", "b", "c", "b", "d", "e" };

            Assert.That.Equal(new[]{ "a", "b" }, arr.SubRange(0, 2));
            Assert.That.Equal(new[]{ "c", "b", "d" }, arr.SubRange(2, 5));
            Assert.That.Equal(new[]{ "d", "e" }, arr.SubRange(4, 6));
            Assert.That.Equal(new[]{ "a" }, arr.SubRange(0, 1));
            Assert.That.Equal(new[]{ "e" }, arr.SubRange(5, 6));
        }
    }
}
