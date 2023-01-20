using System;
using System.Threading;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class TestThreadExt : StarDriveTest
    {
        [TestMethod]
        public void TestParallelFor()
        {
            var numbers = new int[13333337];
            Parallel.For(0, numbers.Length, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                    numbers[i] = i;
            });

            var timer = new PerfTimer();
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

            AssertEqual(sum2, sum, "Parallel.For result incorrect. Incorrect loop logic?");

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

            AssertEqual(sum2, sum3, "Parallel.For result incorrect. Incorrect loop logic?");
            AssertEqual(poolSize, Parallel.PoolSize, "Parallel.For pool is growing, but it shouldn't. Incorrect ParallelTask states?");
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
                Parallel.For(0, items.Length, (start, end) => throw new ArgumentException("Test"), maxParallelism:4);
            }

            Log.Write($"Parallel.MaxParallelism: {Parallel.MaxParallelism}");
            Log.Write($"Parallel.NumPhysicalCores: {Parallel.NumPhysicalCores}");

            // AppVeyor CI quite often runs 1-core only, which makes Parallel tests most vexing
            if (Parallel.MaxParallelism == 1)
            {
                var ex = Assert.ThrowsException<ArgumentException>((Action)Action);
                AssertEqual("Test", ex.Message);
            }
            else
            {
                var ex = Assert.ThrowsException<ParallelTaskException>((Action)Action);
                AssertEqual(typeof(ArgumentException), ex.InnerException?.GetType());
                AssertEqual("Test", ex.InnerException?.Message);
                AssertEqual("Parallel.For task threw an exception", ex.Message);
            }
        }
    }
}
