using System;
using System.Collections;
using System.Collections.Generic;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Collections
{
    [TestClass]
    public class ArrayT_Tests : ArrayTypesTestBase
    {
        public override IArray<T> New<T>() => NewArray<T>();
        public override IArray<T> New<T>(params T[] args) => NewArray<T>(args);

        public Array<T> NewArray<T>() => new Array<T>();
        public Array<T> NewArray<T>(params T[] args) => new Array<T>(args);
        public new Array<string> MakeABCDE() => NewArray("a","b","c","d","e") ;
                
        [TestMethod]
        public void ContainsRef()
        {
            string validRef = "c";
            string nonMatchingRef = new string('c', 1);
            var refs = NewArray("a", "b", "c", "d", validRef, null);
            Assert.IsTrue(refs.ContainsRef(validRef), "ContainsRef should work for existing items");
            Assert.IsFalse(refs.ContainsRef("x"), "ContainsRef should not give false positives");
            Assert.IsFalse(refs.ContainsRef(nonMatchingRef), "ContainsRef should not give false positives");
            Assert.IsTrue(refs.ContainsRef(null), "ContainsRef must detect null properly");
        }

        [TestMethod]
        public void IndexOfRef()
        {
            string validRef = "c";
            string nonMatchingRef = new string('c', 1);
            var refs = NewArray( "a", "b", "c", "d", validRef, null);
            Assert.AreEqual(2, refs.IndexOfRef(validRef), "IndexOfRef should work for existing items");
            Assert.AreEqual(-1, refs.IndexOfRef("x"), "IndexOfRef should not give false positives");
            Assert.AreEqual(-1, refs.IndexOfRef(nonMatchingRef), "IndexOfRef should not give false positives");
            Assert.AreEqual(5, refs.IndexOfRef((string)null), "IndexOfRef must detect null properly");
        }

        [TestMethod]
        public void FirstIndexOf()
        {
            var arr = NewArray("a", null, "b", "c", "b", null, "d");
            Assert.AreEqual(2, arr.FirstIndexOf(s => s == "b"), "FirstIndexOf(predicate) should work for existing items");
            Assert.AreEqual(-1, arr.FirstIndexOf(s => s == "x"), "FirstIndexOf(predicate) should not give false positives");
            Assert.AreEqual(1, arr.FirstIndexOf(s => s == null), "FirstIndexOf(predicate) must detect null properly");
        }

        [TestMethod]
        public void LastIndexOf()
        {
            var arr = NewArray("a", null, "b", "c", "b", null, "d");
            Assert.AreEqual(4, arr.LastIndexOf(s => s == "b"), "LastIndexOf(predicate) should work for existing items");
            Assert.AreEqual(-1, arr.LastIndexOf(s => s == "x"), "LastIndexOf(predicate) should not give false positives");
            Assert.AreEqual(5, arr.LastIndexOf(s => s == null), "LastIndexOf(predicate) must detect null properly");
        }
        
        [TestMethod]
        public void RemoveFirst()
        {
            var arr = NewArray("a", "b", "c", "b", "d");
            arr.RemoveFirst(s => s == "b");
            Assert.That.Equal(new[] { "a", "c", "b", "d" }, arr);
        }

        [TestMethod]
        public void RemoveLast()
        {
            var arr = NewArray("a", "b", "c", "b", "d");
            arr.RemoveLast(s => s == "b");
            Assert.That.Equal(new[] { "a", "b", "c", "d" }, arr);
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
            var arr = MakeABCDE();
            arr.ForEach(s => sum += s);
            Assert.AreEqual("abcde", sum);
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
        public void ReorderFrontToBack()
        {
            var arr = MakeABCDE();
            arr.Reorder(oldIndex:0, newIndex:arr.Count-1);
            Assert.That.Equal(new[] { "b","c","d","e",  "a" }, arr, "Reorder [0] to Last");

            arr = MakeABCDE();
            arr.Reorder(oldIndex:arr.Count-1, newIndex:0);
            Assert.That.Equal(new[] { "e",  "a","b","c","d" }, arr, "Reorder Last to [0]");
        }

        [TestMethod]
        public void ReorderSecondToLast()
        {
            var arr = MakeABCDE();
            arr.Reorder(oldIndex:1, newIndex:arr.Count-1);
            Assert.That.Equal(new[] { "a", "c","d","e",  "b" }, arr, "Reorder [1] to [Last]");

            arr = MakeABCDE();
            arr.Reorder(oldIndex:arr.Count-2, newIndex:0);
            Assert.That.Equal(new[] { "d",  "a","b","c", "e" }, arr, "Reorder [Last-1] to [0]");
        }

        [TestMethod]
        public void ReorderByOne()
        {
            var arr = MakeABCDE();
            arr.Reorder(oldIndex:1, newIndex:2);
            Assert.That.Equal(new[] { "a",  "c","b",  "d","e" }, arr, "Reorder [1] to [2]");

            arr = MakeABCDE();
            arr.Reorder(oldIndex:2, newIndex:1);
            Assert.That.Equal(new[] { "a",  "c","b",  "d","e" }, arr, "Reorder [2] to [1]");
        }

        [TestMethod]
        public void ReorderNothing()
        {
            var arr = MakeABCDE();
            arr.Reorder(oldIndex:1, newIndex:1);
            Assert.That.Equal(new[] { "a","b","c","d","e" }, arr, "Reorder [1] to [1]");

            // and catch index errors
            Assert.ThrowsException<IndexOutOfRangeException>(() => arr.Reorder(-1, 0));
            Assert.ThrowsException<IndexOutOfRangeException>(() => arr.Reorder(0, arr.Count));
            Assert.ThrowsException<IndexOutOfRangeException>(() => arr.Reorder(-1, arr.Count));
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
