using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using Ship_Game;

namespace SDUnitTests
{
    [TestFixture]
    public class TestSpatialManager
    {
        private const int CellSize    = 150000;
        private const int SceneWidth  = 20000000; // Epic universe size
        private const int SceneHeight = 20000000;
        private int Width;
        private int Height;
        private int[] Buckets;
        private Vector2[] TestPoints;
        private float UpperLeftX;
        private float UpperLeftY;

        public TestSpatialManager()
        {
            UpperLeftX = SceneWidth * -0.5f;
            UpperLeftY = SceneWidth * -0.5f;
            Width    = SceneWidth  / CellSize;
            Height   = SceneHeight / CellSize;
            Buckets  = new int[Width * Height];

            for (int i = 0; i < Buckets.Length; ++i)
                Buckets[i] = i;

            TestPoints = new Vector2[Buckets.Length];
            var random = new Random(1337);

            for (int i = 0; i < TestPoints.Length; ++i)
            {
                // random point, with a chance of being outside of grid itself
                TestPoints[i] = new Vector2(
                    random.Next((int)UpperLeftX - CellSize, (int)UpperLeftX + SceneWidth), 
                    random.Next((int)UpperLeftY - CellSize, (int)UpperLeftY + SceneWidth));
            }
        }

        [Test]
        public void TestBucketBoundsCustomMinMax()
        {
            PerfTimer t = PerfTimer.StartNew();
            for (int i = 0; i < TestPoints.Length; ++i)
            {
                Vector2 position = TestPoints[i];
                float radius = CellSize;

                float posX = position.X - UpperLeftX;
                float posY = position.Y - UpperLeftY;
                int cellSize = CellSize;
                int width = Width;
                int height = Height;

                int minX = 0, maxX = 0, minY = 0, maxY = 0;
                for (int j = 0; j < 10000; ++j)
                {
                    minX = (int)((posX - radius) / cellSize);
                    maxX = (int)((posX + radius) / cellSize);
                    minY = (int)((posY - radius) / cellSize);
                    maxY = (int)((posY + radius) / cellSize);

                    if (minX < 0) minX = 0; else if (minX >= width) minX = width - 1;
                    if (maxX < 0) maxX = 0; else if (maxX >= width) maxX = width - 1;
                    if (minY < 0) minY = 0; else if (minY >= height) minY = height - 1;
                    if (maxY < 0) maxY = 0; else if (maxY >= height) maxY = height - 1;
                }

                for (int y = minY; y <= maxY; ++y)
                {
                    for (int x = minX; x <= maxX; ++x)
                    {
                        Buckets[y * width + x] += 1;
                    }
                }
            }
            float e = t.ElapsedMillis;
            Console.WriteLine("TestBucketBoundsCustomMinMax elapsed: {0}", e);
        }

        [Test]
        public void TestBucketBoundsBuiltinMath()
        {
            PerfTimer t = PerfTimer.StartNew();
            for (int i = 0; i < TestPoints.Length; ++i)
            {
                Vector2 position = TestPoints[i];
                float radius = CellSize;

                float posX = position.X - UpperLeftX;
                float posY = position.Y - UpperLeftY;
                int cellSize = CellSize;
                int width = Width;
                int height = Height;

                int minX = 0, maxX = 0, minY = 0, maxY = 0;
                for (int j = 0; j < 10000; ++j)
                {
                    minX = (int)((posX - radius) / cellSize);
                    maxX = (int)((posX + radius) / cellSize);
                    minY = (int)((posY - radius) / cellSize);
                    maxY = (int)((posY + radius) / cellSize);

                    minX = Math.Max(0, Math.Min(minX, width - 1));
                    maxX = Math.Max(0, Math.Min(maxX, width - 1));
                    minY = Math.Max(0, Math.Min(minY, height - 1));
                    maxY = Math.Max(0, Math.Min(maxY, height - 1));
                }

                for (int y = minY; y <= maxY; ++y)
                {
                    for (int x = minX; x <= maxX; ++x)
                    {
                        Buckets[y * width + x] += 1;
                    }
                }
            }
            float e = t.ElapsedMillis;
            Console.WriteLine("TestBucketBoundsBuiltinMath elapsed: {0}", e);
        }
    }
}
