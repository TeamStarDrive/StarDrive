using System;
using System.Threading;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SDUnitTests
{
    [TestClass]
    public class TestThreadExt
    {
        [TestMethod]
        public void TestParallelRange()
        {
            var numbers = new int[13333337];

            Parallel.For(0, numbers.Length, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                    numbers[i] = i;
            });

            PerfTimer timer = PerfTimer.StartNew();
            long sum = 0;
            numbers.ParallelRange(range =>
            {
                long isum = 0;
                foreach (int value in range)
                    isum += (long)Math.Sqrt(value);
                Interlocked.Add(ref sum, isum);
            });
            Console.WriteLine("ParallelRange elapsed: {0:0.000}s  result: {1}", timer.Elapsed, sum);

            timer.Start();
            long sum2 = 0;
            foreach (int value in numbers)
                sum2 += (long)Math.Sqrt(value);
            Console.WriteLine("SingleThread  elapsed: {0:0.000}s  result: {1}", timer.Elapsed, sum2);

            Assert.AreEqual(sum2, sum, "ParallelRange result incorrect. Incorrect loop logic?");

            // Test the parallel loop a second time to ensure it doesn't deadlock etc
            int poolSize = Parallel.PoolSize;
            timer.Start();
            long sum3 = 0;
            numbers.ParallelRange(range =>
            {
                long isum = 0;
                foreach (int value in range)
                    isum += (long)Math.Sqrt(value);
                Interlocked.Add(ref sum3, isum);
            });
            Console.WriteLine("ParallelRange elapsed: {0:0.000}s  result: {1}", timer.Elapsed, sum3);

            numbers = null;
            GC.Collect(); // Fixes Test OOM in Debug mode

            Assert.AreEqual(sum2, sum3, "ParallelRange result incorrect. Incorrect loop logic?");
            Assert.AreEqual(poolSize, Parallel.PoolSize, "Parallel.For pool is growing, but it shouldn't. Incorrect ParallelTask states?");
        }

        [TestMethod]
        public void TestParallelFor()
        {
            var numbers = new int[13333337];
            Parallel.For(0, numbers.Length, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                    numbers[i] = i;
            });

            PerfTimer timer = PerfTimer.StartNew();
            long sum = 0;
            Parallel.For(0, numbers.Length, (start, end) =>
            {
                long isum = 0;
                for (int i = start; i < end; ++i)
                    isum += (long)Math.Sqrt(numbers[i]);
                Interlocked.Add(ref sum, isum);
            });
            Console.WriteLine("ParallelFor  elapsed: {0:0.000}s  result: {1}", timer.Elapsed, sum);

            timer.Start();
            long sum2 = 0;
            foreach (int value in numbers)
                sum2 += (long)Math.Sqrt(value);
            Console.WriteLine("SingleThread elapsed: {0:0.000}s  result: {1}", timer.Elapsed, sum2);

            Assert.AreEqual(sum2, sum, "Parallel.For result incorrect. Incorrect loop logic?");

            // Test the parallel loop a second time to ensure it doesn't deadlock etc
            int poolSize = Parallel.PoolSize;
            timer.Start();
            long sum3 = 0;
            Parallel.For(0, numbers.Length, (start, end) =>
            {
                long isum = 0;
                for (int i = start; i < end; ++i)
                    isum += (long)Math.Sqrt(numbers[i]);
                Interlocked.Add(ref sum3, isum);
            });
            Console.WriteLine("ParallelFor  elapsed: {0:0.000}s  result: {1}", timer.Elapsed, sum3);

            numbers = null;
            GC.Collect(); // Fixes Test OOM in Debug mode

            Assert.AreEqual(sum2, sum3, "Parallel.For result incorrect. Incorrect loop logic?");
            Assert.AreEqual(poolSize, Parallel.PoolSize, "Parallel.For pool is growing, but it shouldn't. Incorrect ParallelTask states?");
        }

        [TestMethod]
        public void TestAllowConcurrentPForLoops()
        {
            // These PFor loops are unrelated to one another, thus they should
            // run without throwing ThreadStateException
            var items = new int[1337];
            Parallel.Run(() =>
            {
                Parallel.For(0, items.Length, (start, end) =>
                {
                    Thread.Sleep(1);
                });
            });
            Parallel.Run(() =>
            {
                Parallel.For(0, items.Length, (start, end) =>
                {
                    Thread.Sleep(1);
                });
            });
            Parallel.Run(() =>
            {
                Parallel.For(0, items.Length, (start, end) =>
                {
                    Thread.Sleep(1);
                });
            });
        }

        [TestMethod]
        public void TestPForExceptions()
        {
            void Action()
            {
                var items = new int[1337];
                Parallel.For(0, items.Length, (start, end) => throw new ArgumentException("Test"));
            }
            Assert.ThrowsException<ArgumentException>((Action) Action);
        }
    }
}
