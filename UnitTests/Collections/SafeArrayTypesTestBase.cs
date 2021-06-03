using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;

namespace UnitTests.Collections
{
    public abstract class SafeArrayTypesTestBase : ArrayTypesTestBase
    {
        // This should not crash while the list is modified
        [TestMethod]
        public void ConcurrentAccess()
        {
            var arr = New<string>();
            for (int i = 0; i < 1000; ++i)
                arr.Add(i.ToString());

            var sw = Stopwatch.StartNew();
            const double seconds = 1.0;

            // iterate over the items for X seconds
            Parallel.Run(() =>
            {
                while (sw.Elapsed.TotalSeconds < seconds)
                {
                    foreach (var item in arr)
                    {
                    }
                    var filtered = arr.Filter(x => x != null);
                    System.Threading.Thread.Sleep(1);
                }
            });

            while (sw.Elapsed.TotalSeconds < seconds)
            {
                Parallel.For(1000, (start, end) =>
                {
                    for (int i = start; i < end; ++i)
                    {
                        string s = i.ToString();
                        arr.Add(s);
                        arr.Remove(s);
                    }
                });
            }
        }

        [TestMethod]
        public void ConcurrentPerfAddRemove()
        {
            const int iterations = 100_000;

            var arr1 = new Array<string>();
            var t1 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; ++i)
            {
                string s = i.ToString();
                arr1.Add(s);
                arr1.Remove(s);
            }
            double e1 = t1.Elapsed.TotalMilliseconds;
            Console.WriteLine($"{arr1} Elapsed: {e1:0.0}ms");

            var arr2 = New<string>();
            var t2 = Stopwatch.StartNew();

            Parallel.For(0, iterations, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                {
                    string s = i.ToString();
                    arr2.Add(s);
                    arr2.Remove(s);
                }
            });

            double e2 = t2.Elapsed.TotalMilliseconds;
            Console.WriteLine($"{arr2} Elapsed: {e2:0.0}ms");
        }
    }
}
