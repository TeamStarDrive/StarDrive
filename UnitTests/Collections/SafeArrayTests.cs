using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Utils;

namespace UnitTests.Collections
{
    [TestClass]
    public class SafeArrayTests : ArrayTypesTestBase
    {
        public override IArray<T> New<T>() => new SafeArray<T>();
        public override IArray<T> New<T>(params T[] args) => new SafeArray<T>(args);

        [TestMethod]
        public void Performance()
        {
            const int iterations = 100_000;

            var arr1 = new Array<int>();
            var t1 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                arr1.Add(i);
                arr1.PopLast();
            }
            double e1 = t1.Elapsed.TotalMilliseconds;
            Console.WriteLine($"Array Elapsed: {e1:0.0}ms");

            var arr2 = new SafeArray<int>();
            var t2 = Stopwatch.StartNew();

            Parallel.For(0, iterations, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                {
                    arr2.Add(i);
                    arr2.PopLast();
                }
            });

            double e2 = t2.Elapsed.TotalMilliseconds;
            Console.WriteLine($"SafeArray Elapsed: {e2:0.0}ms");
        }
    }
}
