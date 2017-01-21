using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using Ship_Game;
using Ship_Game.Gameplay;

namespace SDUnitTests
{
    [TestFixture]
    public class TestArrayT
    {
        [Test]
        public void TestArrayAdd()
        {
            var arr = new Array<int>();
            arr.Add(1);
            Assert.AreEqual(arr.Count, 1, "Count should be 1");
            Assert.AreEqual(arr.Capacity, 4, "Capacity should be 4");
            arr.Add(2);
            arr.Add(3);
            arr.Add(4);
            arr.Add(5);
            Assert.AreEqual(5, arr.Count, "Count should be 5");
            Assert.AreEqual(8, arr.Capacity, "Capacity should grow aligned to 4, expected 8");
        }

        [Test]
        public void TestArrayContains()
        {
            var arr = new Array<string> { "a", "b", "c", "d" };

            Assert.IsTrue(arr.Contains("c"), "Contains should work for existing items");
            Assert.IsFalse(arr.Contains("x"), "Contains should not give false positives");

            arr.Add(null);
            Assert.IsTrue(arr.Contains(null), "Contains must detect null properly");
        }

        [Test]
        public void TestArrayRemoveAll()
        {
            var arr = new Array<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            arr.RemoveAll(x => true);
            Assert.AreEqual(0, arr.Count, "RemoveAll true should erase all elements");

            arr = new Array<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            arr.RemoveAll(x => x % 2 == 1);
            Assert.AreEqual(4, arr.Count, "RemoveAll odd should remove half the elements");
        }

        [Test]
        public void TestToArrayList()
        {
            var arr = new[] { "a", "b", "c" };
            Array<string> arr1 = new Array<string>();
            arr1.AddRange(arr);
            Assert.AreEqual(arr, arr1);
            Assert.Throws<InvalidOperationException>(() => arr1.ToArrayList());

            var arr2 = ((ICollection<string>)arr1).ToArrayList();
            Assert.AreEqual(arr, arr2);

            arr2 = ((IReadOnlyList<string>)arr1).ToArrayList();
            Assert.AreEqual(arr, arr2);

            arr2 = ((IReadOnlyCollection<string>)arr1).ToArrayList();
            Assert.AreEqual(arr, arr2);

            arr2 = ((IEnumerable<string>)arr1).ToArrayList();
            Assert.AreEqual(arr, arr2);
        }

        [Test]
        public void TestToArray()
        {
            var arr = new[] { "a", "b", "c" };
            var arr1 = new Array<string>();
            arr1.AddRange(arr);
            Assert.AreEqual(arr, arr1);

            string[] arr2 = ((ICollection<string>)arr1).ToArray();
            Assert.AreEqual(arr, arr2);

            arr2 = ((IReadOnlyList<string>)arr1).ToArray();
            Assert.AreEqual(arr, arr2);

            arr2 = ((IReadOnlyCollection<string>)arr1).ToArray();
            Assert.AreEqual(arr, arr2);

            arr2 = ((IEnumerable<string>)arr1).ToArray();
            Assert.AreEqual(arr, arr2);
        }

        private static void ReportPerf<T>(float e1, float e2, float e3, float e4)
        {
            Console.WriteLine("{0} for:{1,3}ms  .NET:{2,3}ms  APEX:{3,3}ms  Hybrid:{4,3}ms", 
                typeof(T).Name.PadLeft(7), 
                (int)e1, (int)e2, (int)e3, (int)e4);
        }

        // We are testing multiple types because Array.Copy performance varies
        // depending on the type
        public void ArrayCopyPerf<T>(T[] values, int count, int timesToRun)
        {
            var arr = new T[count];
            for (int i = 0; i < arr.Length; ++i)
                arr[i] = values[i % values.Length];

            // warmup loop
            var copy = new T[arr.Length];
            for (int j = 0; j < 5; ++j)
            {
                Memory.ForCopy(copy, 0, arr, arr.Length);
                Array.Copy(arr, 0, copy, 0, arr.Length);
                Memory.ApexCopy(copy, 0, arr, arr.Length);
                Memory.HybridCopy(copy, 0, arr, arr.Length);
            }

            PerfTimer t = PerfTimer.StartNew();
            for (int i = 0; i < timesToRun; ++i)
                Memory.ForCopy(copy, 0, arr, arr.Length);
            float e1 = t.ElapsedMillis;
            Assert.AreEqual(arr, copy);

            t.Start();
            for (int i = 0; i < timesToRun; ++i)
                Array.Copy(arr, 0, copy, 0, arr.Length);
            float e2 = t.ElapsedMillis;
            Assert.AreEqual(arr, copy);

            t.Start();
            for (int i = 0; i < timesToRun; ++i)
                Memory.ApexCopy(copy, 0, arr, arr.Length);
            float e3 = t.ElapsedMillis;
            Assert.AreEqual(arr, copy);

            t.Start();
            for (int i = 0; i < timesToRun; ++i)
                Memory.HybridCopy(copy, 0, arr, arr.Length);
            float e4 = t.ElapsedMillis;
            Assert.AreEqual(arr, copy);

            ReportPerf<T>(e1, e2, e3, e4);
        }


        [Test]
        public void TestArrayRefTypeCopy()
        {
            string[] strings  = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };
            Weapon[] weapons = { new Weapon(), new Weapon(), new Weapon(), new Weapon() };

            int[] sizes = { 2, 4, 6, 8, 10, 12, 16, 20, 32, 64, 128, 512, 8192, 32768, 262144, 1333337 };
            const int elementsToProcess = 10000000;

            foreach (int size in sizes)
            {
                int iterations = elementsToProcess / size;
                Console.WriteLine("array[{0}] iterations {1}:", size, iterations);
                ArrayCopyPerf(strings, size, timesToRun: iterations);
                ArrayCopyPerf(weapons, size, timesToRun: iterations);
                Console.WriteLine("===============================");
            }
        }

        [Test]
        public void TestArrayValueTypeCopy()
        {
            // struct types
            int[] integers = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            float[] singles = { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f };
            double[] doubles = { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0 };
            Vector2[] vectors = { new Vector2(0f), new Vector2(0.5f), new Vector2(1f), new Vector2(1.33f), new Vector2(2.66f), new Vector2(-3.33f) };

            int[] sizes = { 2, 4, 6, 8, 10, 12, 16, 20, 32, 64, 128, 512, 8192, 32768, 262144, 1333337 };
            const int elementsToProcess = 10000000;

            foreach (int size in sizes)
            {
                int iterations = elementsToProcess / size;
                Console.WriteLine("array[{0}] iterations {1}:", size, iterations);
                ArrayCopyPerf(integers, size, timesToRun: iterations);
                ArrayCopyPerf(singles,  size, timesToRun: iterations);
                ArrayCopyPerf(doubles,  size, timesToRun: iterations);
                ArrayCopyPerf(vectors,  size, timesToRun: iterations);
                Console.WriteLine("===============================");
            }
        }

    }
}
