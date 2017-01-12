using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Ship_Game;

namespace SDUnitTests
{
    [TestFixture]
    public class TestCollectionExt
    {
        [Test]
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

        [Test]
        public void TestFindMinMax()
        {
            var list = new Array<string> { "a", "bb", "ccc", "dddd", "55555", "666666"};

            Assert.AreEqual("666666", list.FindMax(s => s.Length));
            Assert.AreEqual("a", list.FindMin(s => s.Length));

            Assert.AreEqual("dddd", list.FindMaxFiltered(s => s.Length < 5, s => s.Length));
            Assert.AreEqual("ccc", list.FindMinFiltered(s => s.Length > 2, s => s.Length));
        }

        [Test]
        public void TestAny()
        {
            var list = new Array<string> { "a", "bb", "ccc", "dddd", "55555", "666666" };

            Assert.IsTrue(list.Any(s => s == "55555"));
            Assert.IsTrue(list.Any(s => s.Length > 5));
            Assert.IsFalse(list.Any(s => s == "not in list"));
            Assert.IsFalse(list.Any(s => s.Length > 10));
        }

        [Test]
        public void TestCount()
        {
            var list = new Array<string> { "a", "bb", "ccc", "dddd", "55555", "666666" };

            Assert.AreEqual(3, list.Count(s => s.Length % 2 == 0));
            Assert.AreEqual(6, list.Count(s => true));
            Assert.AreEqual(1, list.Count(s => s == "ccc"));
        }

        [Test]
        public void TestParallelForEach()
        {
            var numbers = new int[133333337];
            for (int i = 0; i < numbers.Length; ++i)
                numbers[i] = i;

            Parallel.For(0, 1000, (start, end) => { });

            PerfTimer timer = PerfTimer.StartNew();
            long sum = 0;
            numbers.ParallelForEach(value =>
            {
                Interlocked.Add(ref sum, value);
            });
            Console.WriteLine("ParallelFor  elapsed: {0:0.0,4}s  result: {1}", timer.Elapsed, sum);

            timer.Start();
            long sum2 = 0;
            foreach (int value in numbers)
                sum2 += value;
            Console.WriteLine("Singlethread elapsed: {0:0.0,4}s  result: {1}", timer.Elapsed, sum2);

            Assert.AreEqual(sum2, sum, "ParallelFor<T> result incorrect. Incorrect loop logic?");
        }

        [Test]
        public void TestParallelFor()
        {
            var numbers = new int[133333337];
            for (int i = 0; i < numbers.Length; ++i)
                numbers[i] = i;

            Parallel.For(0, 1000, (start, end) => { });

            PerfTimer timer = PerfTimer.StartNew();
            long sum = 0;
            Parallel.For(0, numbers.Length, (start, end) =>
            {
                long isum = 0;
                for (int i = start; i < end; ++i)
                    isum += numbers[i];
                Interlocked.Add(ref sum, isum);
            });
            Console.WriteLine("ParallelFor  elapsed: {0:0.0,4}s  result: {1}", timer.Elapsed, sum);

            timer.Start();
            long sum2 = 0;
            foreach (int value in numbers)
                sum2 += value;
            Console.WriteLine("Singlethread elapsed: {0:0.0,4}s  result: {1}", timer.Elapsed, sum2);

            Assert.AreEqual(sum2, sum, "ParallelFor<T> result incorrect. Incorrect loop logic?");
        }
    }
}
