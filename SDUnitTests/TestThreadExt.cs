using System;
using System.Threading;
using NUnit.Framework;
using Ship_Game;

namespace SDUnitTests
{
    [TestFixture]
    public class TestThreadExt
    {
        [Test]
        public void TestParallelRange()
        {
            var numbers = new int[133333337];

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

        [Test]
        public void TestParallelFor()
        {
            var numbers = new int[133333337];
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

        [Test]
        public void TestForbidNestedPFor()
        {
            TestDelegate action = () =>
            {
                var items = new int[13337];
                Parallel.For(0, items.Length, (start, end) =>
                {
                    Parallel.For(start, end, (start1, end1) =>
                    {
                        // inner parallel for loop, will cause spawning NCores * NCores of ParallelTask threads
                        // for me, this would spawn 144 threads :)

                        // So, this must be forbidden to ensure optimal usage
                    });
                });
            };
            Assert.Throws<ThreadStateException>(action);
        }

        [Test]
        public void TestPForExceptions()
        {
            TestDelegate action = () =>
            {
                var items = new int[13337];
                Parallel.For(0, items.Length, (start, end) =>
                {
                    throw new ArgumentException("Test");
                });
            };
            Assert.Throws<ArgumentException>(action);
        }
    }
}
