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
    public class TestSafeQueueT
    {
        [TestMethod]
        public void SafeQueueEnqueueDequeue()
        {
            var queue = new SafeQueue<string>();

            queue.Enqueue("Jim");
            Assert.AreEqual("Jim", queue.PeekFirst);
            Assert.AreEqual("Jim", queue.PeekLast);
            queue.Enqueue("Tim");
            Assert.AreEqual("Jim", queue.PeekFirst);
            Assert.AreEqual("Tim", queue.PeekLast);
            queue.Enqueue("Bob");
            Assert.AreEqual("Jim", queue.PeekFirst);
            Assert.AreEqual("Bob", queue.PeekLast);
            queue.Enqueue("Mike");
            Assert.AreEqual("Jim", queue.PeekFirst);
            Assert.AreEqual("Mike", queue.PeekLast);
            queue.Enqueue("Jake");
            Assert.AreEqual("Jim", queue.PeekFirst);
            Assert.AreEqual("Jake", queue.PeekLast);

            Assert.IsTrue(queue.Count == 5, "Queue should have 5 elements");
            Assert.IsFalse(queue.IsEmpty, "Queue should have 5 elements, so IsEmpty should be false");

            Assert.AreEqual("Jim", queue.Dequeue());
            Assert.AreEqual("Tim", queue.Dequeue());
            Assert.AreEqual("Bob", queue.Dequeue());
            Assert.AreEqual("Mike", queue.Dequeue());
            Assert.AreEqual("Jake", queue.Dequeue());

            Assert.IsTrue(queue.Count == 0, "Queue must be empty after dequeuing all");
            Assert.IsTrue(queue.IsEmpty, "Queue must be empty after dequeueing all");
            Assert.AreEqual(null, queue.Dequeue(), "Empty queue Dequeue must result in default(T)");
        }

        [TestMethod]
        public void SafeQueuePushToFront()
        {
            var queue = new SafeQueue<string>();

            queue.Enqueue("Jim");
            queue.Enqueue("Tim");
            queue.Enqueue("Bob");
            Assert.AreEqual("Jim", queue.PeekFirst);
            Assert.AreEqual("Bob", queue.PeekLast);
            queue.PushToFront("Mike");
            Assert.AreEqual("Mike", queue.PeekFirst);
            Assert.AreEqual("Bob", queue.PeekLast);
            queue.PushToFront("Jake");
            Assert.AreEqual("Jake", queue.PeekFirst);
            Assert.AreEqual("Bob", queue.PeekLast);

            Assert.IsTrue(queue.Count == 5, "Queue should have 5 elements");
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
            Assert.AreEqual("Tim", queue.PeekFirst);
            Assert.AreEqual(4, queue.Count);

            queue.RemoveLast();
            Assert.AreEqual("Mike", queue.PeekLast);
            Assert.AreEqual(3, queue.Count);
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
            CollectionAssert.AreEqual(array, new[] { "Jim", "Tim", "Bob", "Mike", "Jake" });

            var list = new Array<string>();
            foreach (string str in queue)
                list.Add(str);

            CollectionAssert.AreEqual(list, new[] { "Jim", "Tim", "Bob", "Mike", "Jake" });
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

            Assert.IsTrue(queue.Contains("Jim"));
            Assert.IsTrue(queue.Contains("Tim"));
            Assert.IsTrue(queue.Contains("Bob"));
            Assert.IsTrue(queue.Contains("Mike"));
            Assert.IsTrue(queue.Contains("Jake"));

            Assert.IsFalse(queue.Contains("jack"));
            Assert.IsFalse(queue.Contains(null));

            queue.Enqueue(null);
            Assert.IsTrue(queue.Contains(null));
        }

        [TestMethod]
        public void DotNetQueueOrder()
        {
            var queue = new Queue<string>();
            queue.Enqueue("A");
            queue.Enqueue("B");
            queue.Enqueue("C");
            queue.Enqueue("D");

            Assert.AreEqual("A", queue.First());
            Assert.AreEqual("B", queue.ElementAt(1));
            Assert.AreEqual("C", queue.ElementAt(2));
            Assert.AreEqual("D", queue.Last());

            Assert.AreEqual("A", queue.Dequeue());
            Assert.AreEqual("B", queue.Dequeue());
            Assert.AreEqual("C", queue.Dequeue());
            Assert.AreEqual("D", queue.Dequeue());
        }

        [TestMethod]
        public void SafeQueueLINQ()
        {
            var queue = new SafeQueue<string>();
            queue.Enqueue("A");
            queue.Enqueue("B");
            queue.Enqueue("C");
            queue.Enqueue("D");

            Assert.AreEqual("A", queue.First());
            Assert.AreEqual("B", queue.ElementAt(1));
            Assert.AreEqual("C", queue.ElementAt(2));
            Assert.AreEqual("D", queue.Last());

            Assert.IsTrue(queue.Any(s => s == "D"));
            Assert.IsFalse(queue.Any(s => s == "X"));

            Assert.AreEqual("A", queue.Dequeue());
            Assert.AreEqual("B", queue.Dequeue());
            Assert.AreEqual("C", queue.Dequeue());
            Assert.AreEqual("D", queue.Dequeue());
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
            Assert.AreEqual("A", queue.ElementAt(0));
            Assert.AreEqual("B", queue.ElementAt(1));
            Assert.AreEqual("C", queue.ElementAt(2));
            Assert.AreEqual("D", queue.ElementAt(3));
            Assert.AreEqual("E", queue.ElementAt(4));
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
            var r = new SeededRandom();

            var writer = Parallel.Run(() =>
            {
                var items = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };

                while (t.Elapsed < 1.0)
                {
                    int toAdd = r.Int(1, 3);
                    int toRemove = r.Int(1, 3);
                    for (int i = 0; i < toAdd; ++i)
                        queue.Enqueue(items.RandItem());
                    for (int i = 0; i < toRemove; ++i)
                    {
                        Assert.IsTrue(queue.Count >= 0, "SafeQueue<T> count must always be positive!");
                        queue.Dequeue();
                        Assert.IsTrue(queue.Count >= 0, "SafeQueue<T> count must always be positive!");
                    }
                }
            });

            var reader = Parallel.Run(() =>
            {
                while (t.Elapsed < 1.0)
                {
                    Assert.IsTrue(queue.Count >= 0, "SafeQueue<T> count must always be positive!");
                    queue.TryPeekFirst(out string first);
                    queue.TryPeekLast(out string last);

                    if (queue.Any(s => s == "A") || queue.Contains("B"))
                    {
                    }

                    queue.TryDequeue(out string first1);
                    Assert.IsTrue(queue.Count >= 0, "SafeQueue<T> count must always be positive!");
                }
            });

            writer.Wait();
            reader.Wait();

            // If it didn't crash here, the test succeeded
        }
    }
}
