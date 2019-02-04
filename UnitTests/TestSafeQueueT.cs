using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SDUnitTests
{
    [TestClass]
    public class TestSafeQueueT
    {
        [TestMethod]
        public void TestEnqueueDequeue()
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
        public void TestPushToFront()
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
        public void TestRemove()
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
        public void TestSequence()
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
        public void TestContains()
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
        public void TestDotNetQueueOrder()
        {
            var dotQueue = new Queue<string>();
            dotQueue.Enqueue("A");
            dotQueue.Enqueue("B");
            dotQueue.Enqueue("C");
            dotQueue.Enqueue("D");

            foreach (string item in dotQueue)
                Console.WriteLine($".NET QueueItem: {item}");

            Assert.AreEqual("A", dotQueue.First());
            Assert.AreEqual("B", dotQueue.ElementAt(1));
            Assert.AreEqual("C", dotQueue.ElementAt(2));
            Assert.AreEqual("D", dotQueue.Last());

            Assert.AreEqual("A", dotQueue.Dequeue());
            Assert.AreEqual("B", dotQueue.Dequeue());
            Assert.AreEqual("C", dotQueue.Dequeue());
            Assert.AreEqual("D", dotQueue.Dequeue());
        }

        [TestMethod]
        public void TestElementAt()
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
    }
}
