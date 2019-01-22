using System.Collections.Generic;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SDUnitTests
{
    [TestClass]
    public class TestCollectionExt
    {
        [TestMethod]
        public void TestIndexOf()
        {
            IReadOnlyList<string> ilist = new Array<string>
            {
                "hello", "world", "ilist", "indexof", "etc"
            };

            Assert.AreEqual(0, ilist.IndexOf("hello"));
            Assert.AreEqual(1, ilist.IndexOf("world"));
            Assert.AreEqual(2, ilist.IndexOf("ilist"));
            Assert.AreEqual(3, ilist.IndexOf("indexof"));
            Assert.AreEqual(4, ilist.IndexOf("etc"));
        }

        [TestMethod]
        public void TestFindMinMax()
        {
            var list = new Array<string> { "a", "bb", "ccc", "dddd", "55555", "666666"};

            Assert.AreEqual("666666", list.FindMax(s => s.Length));
            Assert.AreEqual("a", list.FindMin(s => s.Length));

            Assert.AreEqual("dddd", list.FindMaxFiltered(s => s.Length < 5, s => s.Length));
            Assert.AreEqual("ccc", list.FindMinFiltered(s => s.Length > 2, s => s.Length));
        }

        [TestMethod]
        public void TestFindMinKeyValue()
        {
            var map = new Map<string, string>{ ("a","x"), ("bb", "xx"), ("ccc", "xxx") };

            Assert.AreEqual("x", map.FindMinValue(s => s.Length));
            Assert.AreEqual("xxx", map.FindMinValue(s => 3-s.Length));
            
            Assert.AreEqual("a", map.FindMinKey(s => s.Length));
            Assert.AreEqual("ccc", map.FindMinKey(s => 3-s.Length));

            Assert.AreEqual("a", map.FindMin((k,v) => v.Length).Key);
            Assert.AreEqual("ccc", map.FindMin((k,v) => 3-v.Length).Key);

            var map2 = new Map<string, float>
            {
                ("a", float.PositiveInfinity), 
                ("b", float.NaN),
                ("c", float.NegativeInfinity), 
                ("d", 10f)
            };

            Assert.AreEqual("c", map2.FindMin((key, value) => value).Key);
        }

        [TestMethod]
        public void TestFindMaxKeyValue()
        {
            var map = new Map<int, string>{ (0,"x"), (1, "xx"), (2, "xxx") };

            Assert.AreEqual("xxx", map.FindMaxValue(s => s.Length));
            Assert.AreEqual("x", map.FindMaxValue(s => 3-s.Length));
            
            Assert.AreEqual(2, map.FindMaxKey(i => i));
            Assert.AreEqual(0, map.FindMaxKey(i => 3-i));

            Assert.AreEqual(2, map.FindMax((k,v) => v.Length).Key);
            Assert.AreEqual(0, map.FindMax((k,v) => 3-v.Length).Key);

            var map2 = new Map<string, float>
            {
                ("b", float.NaN),
                ("c", float.NegativeInfinity), 
                ("d", 10f),
                ("a", float.PositiveInfinity), 
            };

            Assert.AreEqual("a", map2.FindMax((key, value) => value).Key);
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

            Assert.AreEqual(3, list.Count(s => s.Length % 2 == 0));
            Assert.AreEqual(6, list.Count(s => true));
            Assert.AreEqual(1, list.Count(s => s == "ccc"));
        }



        [TestMethod]
        public void TestUniqueExclude()
        {
            string[] setA = { "E", "F", "G", "H", "A", "B", "C", "D" };
            string[] setB = { "G", "D", "A", "H" };
            // unique exclude is unstable, so the resulting order will be scrambled
            string[] excluded = setA.UniqueExclude(setB);

            Assert.AreEqual(4, excluded.Length, "Expected exclusion length doesn't match expected");
            CollectionAssert.Contains(excluded, "B"); // unstable ordering
            CollectionAssert.Contains(excluded, "C");
            CollectionAssert.Contains(excluded, "E");
            CollectionAssert.Contains(excluded, "F");
            //Assert.AreEqual(new[] { "E", "F", "B", "C"}, excluded, "Invalid exclusion result");

            string[] empty1 = { };
            string[] excludedEmpty = empty1.UniqueExclude(setB);
            Assert.AreEqual(0, excludedEmpty.Length, "Empty exclusion should be an empty array");

            string[] excludeNothing = setA.UniqueExclude(empty1);
            CollectionAssert.AreEqual(setA, excludeNothing, "Excluding with empty should be equal to original");
        }
    }
}
