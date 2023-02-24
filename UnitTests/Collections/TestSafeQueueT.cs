using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game.Utils;

namespace UnitTests.Collections
{
    [TestClass]
    public class TestSafeQueueT : StarDriveTest
    {
        [TestMethod]
        public void SafeQueueEnqueueDequeue()
        {
            var queue = new SafeQueue<string>();

            queue.Enqueue("Jim");
            AssertEqual("Jim", queue.PeekFirst);
            AssertEqual("Jim", queue.PeekLast);
            queue.Enqueue("Tim");
            AssertEqual("Jim", queue.PeekFirst);
            AssertEqual("Tim", queue.PeekLast);
            queue.Enqueue("Bob");
            AssertEqual("Jim", queue.PeekFirst);
            AssertEqual("Bob", queue.PeekLast);
            queue.Enqueue("Mike");
            AssertEqual("Jim", queue.PeekFirst);
            AssertEqual("Mike", queue.PeekLast);
            queue.Enqueue("Jake");
            AssertEqual("Jim", queue.PeekFirst);
            AssertEqual("Jake", queue.PeekLast);

            AssertTrue(queue.Count == 5, "Queue should have 5 elements");
            AssertFalse(queue.IsEmpty, "Queue should have 5 elements, so IsEmpty should be false");

            AssertEqual("Jim", queue.Dequeue());
            AssertEqual("Tim", queue.Dequeue());
            AssertEqual("Bob", queue.Dequeue());
            AssertEqual("Mike", queue.Dequeue());
            AssertEqual("Jake", queue.Dequeue());

            AssertTrue(queue.Count == 0, "Queue must be empty after dequeuing all");
            AssertTrue(queue.IsEmpty, "Queue must be empty after dequeueing all");
            AssertEqual(null, queue.Dequeue(), "Empty queue Dequeue must result in default(T)");
        }

        [TestMethod]
        public void SafeQueuePushToFront()
        {
            var queue = new SafeQueue<string>();

            queue.Enqueue("Jim");
            queue.Enqueue("Tim");
            queue.Enqueue("Bob");
            AssertEqual("Jim", queue.PeekFirst);
            AssertEqual("Bob", queue.PeekLast);
            queue.PushToFront("Mike");
            AssertEqual("Mike", queue.PeekFirst);
            AssertEqual("Bob", queue.PeekLast);
            queue.PushToFront("Jake");
            AssertEqual("Jake", queue.PeekFirst);
            AssertEqual("Bob", queue.PeekLast);

            AssertTrue(queue.Count == 5, "Queue should have 5 elements");
        }

        [TestMethod]
        public void SafeQueueRemove()
        {
            var queue = new SafeQueue<string>();
            queue.Enqueue("Jim");
            queue.Enqueue("Tim");
            queue.Enqueue("Bob");
            queue.Enqueue("Mike");
            queue.Enqueue("Jake");

            queue.RemoveFirst();
            AssertEqual("Tim", queue.PeekFirst);
            AssertEqual(4, queue.Count);

            queue.RemoveLast();
            AssertEqual("Mike", queue.PeekLast);
            AssertEqual(3, queue.Count);
        }

        [TestMethod]
        public void SafeQueueSequence()
        {
            var queue = new SafeQueue<string>();
            queue.Enqueue("Jim");
            queue.Enqueue("Tim");
            queue.Enqueue("Bob");
            queue.Enqueue("Mike");
            queue.Enqueue("Jake");

            string[] array = queue.ToArray();
            AssertEqualCollections(array, new[] { "Jim", "Tim", "Bob", "Mike", "Jake" });

            var list = new Array<string>();
            foreach (string str in queue)
                list.Add(str);

            AssertEqualCollections(list, new[] { "Jim", "Tim", "Bob", "Mike", "Jake" });
        }

        [TestMethod]
        public void SafeQueueContains()
        {
            var queue = new SafeQueue<string>();
            queue.Enqueue("Jim");
            queue.Enqueue("Tim");
            queue.Enqueue("Bob");
            queue.Enqueue("Mike");
            queue.Enqueue("Jake");

            AssertTrue(queue.Contains("Jim"));
            AssertTrue(queue.Contains("Tim"));
            AssertTrue(queue.Contains("Bob"));
            AssertTrue(queue.Contains("Mike"));
            AssertTrue(queue.Contains("Jake"));

            AssertFalse(queue.Contains("jack"));
            AssertFalse(queue.Contains(null));

            queue.Enqueue(null);
            AssertTrue(queue.Contains(null));
        }

        [TestMethod]
        public void DotNetQueueOrder()
        {
            var queue = new Queue<string>();
            queue.Enqueue("A");
            queue.Enqueue("B");
            queue.Enqueue("C");
            queue.Enqueue("D");

            AssertEqual("A", queue.First());
            AssertEqual("B", queue.ElementAt(1));
            AssertEqual("C", queue.ElementAt(2));
            AssertEqual("D", queue.Last());

            AssertEqual("A", queue.Dequeue());
            AssertEqual("B", queue.Dequeue());
            AssertEqual("C", queue.Dequeue());
            AssertEqual("D", queue.Dequeue());
        }

        [TestMethod]
        public void SafeQueueLINQ()
        {
            var queue = new SafeQueue<string>();
            queue.Enqueue("A");
            queue.Enqueue("B");
            queue.Enqueue("C");
            queue.Enqueue("D");

            AssertEqual("A", queue.First());
            AssertEqual("B", queue.ElementAt(1));
            AssertEqual("C", queue.ElementAt(2));
            AssertEqual("D", queue.Last());

            AssertTrue(queue.Any(s => s == "D"));
            AssertFalse(queue.Any(s => s == "X"));

            AssertEqual("A", queue.Dequeue());
            AssertEqual("B", queue.Dequeue());
            AssertEqual("C", queue.Dequeue());
            AssertEqual("D", queue.Dequeue());
        }

        [TestMethod]
        public void SafeQueueElementAt()
        {
            var queue = new SafeQueue<string>();
            queue.Enqueue("A");
            queue.Enqueue("B");
            queue.Enqueue("C");
            queue.Enqueue("D");
            queue.Enqueue("E");
            AssertEqual("A", queue.ElementAt(0));
            AssertEqual("B", queue.ElementAt(1));
            AssertEqual("C", queue.ElementAt(2));
            AssertEqual("D", queue.ElementAt(3));
            AssertEqual("E", queue.ElementAt(4));
            Assert.ThrowsException<IndexOutOfRangeException>(() => queue.ElementAt(-1));
            Assert.ThrowsException<IndexOutOfRangeException>(() => queue.ElementAt(5));
            Assert.ThrowsException<IndexOutOfRangeException>(() => queue.ElementAt(10));
        }

        // This tests the ability of SafeQueue to concurrently modify
        // and access its elements. One thread will be adding/removing items,
        // while the other one will simply be iterating the queue
        [TestMethod]
        public void SafeQueueConcurrentModification()
        {
            var queue = new SafeQueue<string>();
            var t = new PerfTimer();
            var r = new SeededRandom(12345);
            int numWriteOperations = 0;
            int numReadOperations = 0;

            const double TestTime = 2.0;

            var writer = Parallel.Run(() =>
            {
                var items = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };

                int writeOperations = 0;
                while (t.Elapsed < TestTime)
                {
                    int toAdd = r.Int(1, 3);
                    int toRemove = r.Int(1, 3);
                    for (int i = 0; i < toAdd; ++i)
                        queue.Enqueue(r.Item(items));
                    for (int i = 0; i < toRemove; ++i)
                    {
                        AssertTrue(queue.Count >= 0, "SafeQueue<T> count must always be positive!");
                        queue.Dequeue();
                        AssertTrue(queue.Count >= 0, "SafeQueue<T> count must always be positive!");
                    }
                    writeOperations += toAdd + toRemove;
                }
                numWriteOperations = writeOperations;
            });

            var reader = Parallel.Run(() =>
            {
                int readOperations = 0;
                while (t.Elapsed < TestTime)
                {
                    AssertTrue(queue.Count >= 0, "SafeQueue<T> count must always be positive!");
                    queue.TryPeekFirst(out string first);
                    queue.TryPeekLast(out string last);

                    if (queue.Any(s => s == "A") || queue.Contains("B"))
                    {
                    }

                    queue.TryDequeue(out string first1);
                    AssertTrue(queue.Count >= 0, "SafeQueue<T> count must always be positive!");
                    readOperations += 5;
                }
                numReadOperations = readOperations;
            });

            writer.Wait();
            reader.Wait();

            // If it didn't crash here, the test succeeded
            double numWritesPerSecond = numWriteOperations/TestTime;
            double numReadPerSecond = numReadOperations/TestTime;
            Log.Write($"Concurrent Write Operations: {numWritesPerSecond/1000:0.0}/ms");
            Log.Write($"Concurrent Read Operations: {numReadPerSecond/1000:0.0}/ms");
            Log.Write($"Concurrent AvgWriteOpTime: {(1.0/numWritesPerSecond)*1000_000:0.000}us");
            Log.Write($"Concurrent AvgReadOpTime: {(1.0/numReadPerSecond)*1000_000:0.000}us");
        }
    }
}
