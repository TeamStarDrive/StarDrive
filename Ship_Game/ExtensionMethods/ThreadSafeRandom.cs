using System;
using System.Threading;

namespace Ship_Game
{
    public class ThreadSafeRandom
    {
        Map<int, Random> ThreadSpecificRandoms = new Map<int, Random>();

        public ThreadSafeRandom()
        {
            ThreadSpecificRandoms[Thread.CurrentThread.ManagedThreadId] = new Random();
        }

        /// <summary>
        /// Gets a random which is unique to this thread
        /// </summary>
        public Random Random
        {
            get
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                if (!ThreadSpecificRandoms.TryGetValue(threadId, out Random random))
                {
                    random = new Random();
                    ThreadSpecificRandoms.Add(threadId, random);
                }
                return random;
            }
        }
        
        /// Generate random, inclusive [min, max]
        public float Float(float min, float max)
        {
            return min + (float)Random.NextDouble() * (max - min);
        }

        /// Generate random, inclusive [min, max]
        public int Int(int min, int max)
        {
            return Random.Next(min, max+1);
        }

        /// Generate random index, upper bound excluded: [startIndex, arrayLength)
        public int InRange(int startIndex, int arrayLength)
        {
            return Random.Next(startIndex, arrayLength);
        }
    }
}
