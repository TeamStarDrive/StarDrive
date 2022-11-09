using System.Collections.Generic;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using static Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert;

namespace UnitTests.Collections
{
    [TestClass]
    public class TestCollectionExt : StarDriveTest
    {
        [TestMethod]
        public void TestIndexOf()
        {
            IReadOnlyList<string> ilist = new Array<string>
            {
                "hello", "world", "ilist", "indexof", "etc"
            };

            AssertEqual(0, ilist.IndexOf("hello"));
            AssertEqual(1, ilist.IndexOf("world"));
            AssertEqual(2, ilist.IndexOf("ilist"));
            AssertEqual(3, ilist.IndexOf("indexof"));
            AssertEqual(4, ilist.IndexOf("etc"));
        }

        
        [TestMethod]
        public void TestFindMinMaxEmpty()
        {
            var arra = new string[0];
            var list = new Array<string>{"","","","",""}; list.Clear(); // force Array to reserve internal array, then clear

            AssertEqual(null, arra.FindMin(s => s.Length));
            AssertEqual(null, list.FindMin(s => s.Length));
            AssertEqual(null, ((IReadOnlyList<string>)list).FindMin(s => s.Length));

            AssertEqual(null, arra.FindMax(s => s.Length));
            AssertEqual(null, list.FindMax(s => s.Length));
            AssertEqual(null, ((IReadOnlyList<string>)list).FindMax(s => s.Length));
            
            AssertEqual(null, arra.FindMinFiltered(s => false, s => s.Length));
            AssertEqual(null, list.FindMinFiltered(s => false, s => s.Length));
            
            AssertEqual(null, arra.FindMaxFiltered(s => false, s => s.Length));
            AssertEqual(null, list.FindMaxFiltered(s => false, s => s.Length));
        }

        [TestMethod]
        public void TestFindMin()
        {
            var arra = new [] { "a", "bb", "ccc", "dddd", "55555", "666666" };
            var list = new Array<string>(arra);

            AssertEqual("a",      list.FindMin(s => s.Length));
            AssertEqual("666666", list.FindMin(s => 6-s.Length));

            AssertEqual("a",      list.GetInternalArrayItems().FindMin(list.Count, s => s.Length));
            AssertEqual("666666", list.GetInternalArrayItems().FindMin(list.Count, s => 6-s.Length));
            
            AssertEqual("a",      ((IReadOnlyList<string>)list).FindMin(s => s.Length));
            AssertEqual("666666", ((IReadOnlyList<string>)list).FindMin(s => 6-s.Length));

            AssertEqual(null,     arra.FindMinFiltered(s => false, s => s.Length));
            AssertEqual("a",      arra.FindMinFiltered(s => s.Length < 4, s => s.Length));
            AssertEqual("ccc",    arra.FindMinFiltered(s => s.Length > 2, s => s.Length));

            AssertEqual(null,     list.FindMinFiltered(s => false, s => s.Length));
            AssertEqual("a",      list.FindMinFiltered(s => s.Length < 4, s => s.Length));
            AssertEqual("ccc",    list.FindMinFiltered(s => s.Length > 2, s => s.Length));

            AssertEqual(null,     list.GetInternalArrayItems().FindMinFiltered(list.Count, s => false, s => s.Length));
            AssertEqual("a",      list.GetInternalArrayItems().FindMinFiltered(list.Count, s => s.Length < 4, s => s.Length));
            AssertEqual("ccc",    list.GetInternalArrayItems().FindMinFiltered(list.Count, s => s.Length > 2, s => s.Length));
        }

        [TestMethod]
        public void TestFindMax()
        {
            var arra = new [] { "a", "bb", "ccc", "dddd", "55555", "666666" };
            var list = new Array<string>(arra);

            AssertEqual("666666", arra.FindMax(s => s.Length));
            AssertEqual("a",      arra.FindMax(s => 6-s.Length));

            AssertEqual("666666", list.FindMax(s => s.Length));
            AssertEqual("a",      list.FindMax(s => 6-s.Length));

            AssertEqual("666666", list.GetInternalArrayItems().FindMax(list.Count, s => s.Length));
            AssertEqual("a",      list.GetInternalArrayItems().FindMax(list.Count, s => 6-s.Length));
            
            AssertEqual("666666", ((IReadOnlyList<string>)list).FindMax(s => s.Length));
            AssertEqual("a",      ((IReadOnlyList<string>)list).FindMax(s => 6-s.Length));
            
            AssertEqual(null,     arra.FindMaxFiltered(s => false, s => s.Length));
            AssertEqual("dddd",   arra.FindMaxFiltered(s => s.Length < 5, s => s.Length));
            AssertEqual("666666", arra.FindMaxFiltered(s => s.Length > 4, s => s.Length));
            
            AssertEqual(null,     list.FindMaxFiltered(s => false, s => s.Length));
            AssertEqual("dddd",   list.FindMaxFiltered(s => s.Length < 5, s => s.Length));
            AssertEqual("666666", list.FindMaxFiltered(s => s.Length > 4, s => s.Length));
            
            AssertEqual(null,     list.GetInternalArrayItems().FindMaxFiltered(s => false, s => s.Length));
            AssertEqual("dddd",   list.GetInternalArrayItems().FindMaxFiltered(list.Count, s => s.Length < 5, s => s.Length));
            AssertEqual("666666", list.GetInternalArrayItems().FindMaxFiltered(list.Count, s => s.Length > 4, s => s.Length));
        }

        [TestMethod]
        public void TestFindMinMaxEdgeCases()
        {
            var nan = new Array<string> { "test", "shouldBeIgnored" };
            AssertEqual("test", nan.FindMin(x => float.NaN));
            AssertEqual("test", nan.FindMin(x => float.PositiveInfinity));
            AssertEqual("test", nan.FindMin(x => float.NegativeInfinity));

            AssertEqual("test", nan.FindMax(x => float.NaN));
            AssertEqual("test", nan.FindMax(x => float.PositiveInfinity));
            AssertEqual("test", nan.FindMax(x => float.NegativeInfinity));
        }

        [TestMethod]
        public void TestFindMinMap()
        {
            var map = new Map<string, string>{ ("a","x"), ("bb", "xx"), ("ccc", "xxx") };

            AssertEqual("x",   map.FindMinValue(v => v.Length));
            AssertEqual("xxx", map.FindMinValue(v => 3-v.Length));
            
            AssertEqual("a",   map.FindMinKey(s => s.Length));
            AssertEqual("ccc", map.FindMinKey(s => 3-s.Length));

            AssertEqual("a",   map.FindMin((k,v) => v.Length).Key);
            AssertEqual("ccc", map.FindMin((k,v) => 3-v.Length).Key);

            var map2 = new Map<string, float>
            {
                ("a", float.PositiveInfinity), 
                ("b", float.NaN),
                ("c", float.NegativeInfinity), 
                ("d", 10f)
            };

            AssertEqual("c", map2.FindMin((key, value) => value).Key);
        }

        [TestMethod]
        public void TestFindMaxKeyValue()
        {
            var map = new Map<int, string>{ (0,"x"), (1, "xx"), (2, "xxx"), (3, "xxxx") };

            AssertEqual("xxxx", map.FindMaxValue(s => s.Length));
            AssertEqual("x",    map.FindMaxValue(s => 3-s.Length));
            
            AssertEqual(3, map.FindMaxKey(i => i));
            AssertEqual(0, map.FindMaxKey(i => 3-i));

            AssertEqual(3, map.FindMax((k,v) => v.Length).Key);
            AssertEqual(0, map.FindMax((k,v) => 3-v.Length).Key);

            AssertEqual(1, map.FindMaxKeyByValuesFiltered(v=>v.Length<3, v=>v.Length));

            var map2 = new Map<string, float>
            {
                ("b", float.NaN),
                ("c", float.NegativeInfinity), 
                ("d", 10f),
                ("a", float.PositiveInfinity), 
            };

            AssertEqual("a", map2.FindMax((key, value) => value).Key);
        }

        [TestMethod]
        public void TestFindMinItemsFiltered()
        {
            var list = new Array<int>{ 9, 8, 4, 3, 8, 9, 10, 2, 1, 4, 5, 7, 8, 9, 10, 11 };

            AreEqual(new int[0], list.FindMinItemsFiltered(5, i => false, i => i));
            AreEqual(new[]{1,2,3,4,4}, list.FindMinItemsFiltered(5, i => i < 6, i => i));
        }

        [TestMethod]
        public void TestFilter()
        {
            var arr = new [] { "a", "bb", "ccc", "dddd", "55555", "666666" };
            var list = new Array<string>(arr);
            var map = new Map<int, string>{ (0,"a"), (1, "bb"), (2, "ccc"), (3, "dddd"), (4, "55555"), (5, "666666") };

            AreEqual(new[]{"55555", "666666"}, arr.Filter(s => s.Length > 4));
            AreEqual(new[]{"55555", "666666"}, list.Filter(s => s.Length > 4));
            AreEqual(new[]{"55555", "666666"}, ((IReadOnlyList<string>)list).Filter(s => s.Length > 4));
            AreEqual(new[]{"55555", "666666"}, map.FilterValues(s => s.Length > 4));
            
            AreEqual(new[]{"a", "bb", "ccc"}, arr.Filter(s => s.Length < 4));
            AreEqual(new[]{"a", "bb", "ccc"}, list.Filter(s => s.Length < 4));
            AreEqual(new[]{"a", "bb", "ccc"}, ((IReadOnlyList<string>)list).Filter(s => s.Length < 4));
            AreEqual(new[]{"a", "bb", "ccc"}, map.FilterValues(s => s.Length < 4));

            
            AreEqual(new[]{5,6}, arr.FilterSelect(s => s.Length > 4, s => s.Length));
            AreEqual(new[]{5,6}, list.FilterSelect(s => s.Length > 4, s => s.Length));

            AreEqual(new[]{1,2,3}, arr.FilterSelect(s => s.Length < 4, s => s.Length));
            AreEqual(new[]{1,2,3}, list.FilterSelect(s => s.Length < 4, s => s.Length));
        }

        [TestMethod]
        public void TestAny()
        {
            var list = new Array<string> { "a", "bb", "ccc", "dddd", "55555", "666666" };

            Assert.IsTrue(list.Any(s => s == "55555"));
            Assert.IsTrue(list.Any(s => s.Length > 5));
            Assert.IsFalse(list.Any(s => s == "not in list"));
            Assert.IsFalse(list.Any(s => s.Length > 10));
        }

        [TestMethod]
        public void TestCount()
        {
            var list = new Array<string> { "a", "bb", "ccc", "dddd", "55555", "666666" };

            AssertEqual(3, list.Count(s => s.Length % 2 == 0));
            AssertEqual(6, list.Count(s => true));
            AssertEqual(1, list.Count(s => s == "ccc"));
        }



        [TestMethod]
        public void TestUniqueExclude()
        {
            string[] setA = { "E", "F", "G", "H", "A", "B", "C", "D" };
            string[] setB = { "G", "D", "A", "H" };
            // unique exclude is unstable, so the resulting order will be scrambled
            string[] excluded = setA.UniqueExclude(setB);

            AssertEqual(4, excluded.Length, "Expected exclusion length doesn't match expected");
            Contains(excluded, "B"); // unstable ordering
            Contains(excluded, "C");
            Contains(excluded, "E");
            Contains(excluded, "F");
            //AssertEqual(new[] { "E", "F", "B", "C"}, excluded, "Invalid exclusion result");

            string[] empty1 = { };
            string[] excludedEmpty = empty1.UniqueExclude(setB);
            AssertEqual(0, excludedEmpty.Length, "Empty exclusion should be an empty array");

            string[] excludeNothing = setA.UniqueExclude(empty1);
            AreEqual(setA, excludeNothing, "Excluding with empty should be equal to original");
        }
    }
}
