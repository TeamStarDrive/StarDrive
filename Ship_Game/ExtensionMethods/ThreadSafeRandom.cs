using System;
using System.Threading;

namespace Ship_Game
{
    public class ThreadSafeRandom
    {
        // NOTE: This is really fast
        readonly ThreadLocal<Random> Randoms = new ThreadLocal<Random>(() => new Random());

        public ThreadSafeRandom()
        {
        }

        /// Generate random, inclusive [min, max]
        public float Float(float min, float max)
        {
            return min + (float)Randoms.Value.NextDouble() * (max - min);
        }

        // Generate a random float within [0..1]
        public float Float()
        {
            return (float)Randoms.Value.NextDouble();
        }

        /// Generate random, inclusive [min, max]
        public int Int(int min, int max)
        {
            return Randoms.Value.Next(min, max+1);
        }

        // Generate a random byte
        public byte Byte()
        {
            return (byte)Randoms.Value.Next(255);
        }

        /// Generate random index, upper bound excluded: [startIndex, arrayLength)
        public int InRange(int startIndex, int arrayLength)
        {
            return Randoms.Value.Next(startIndex, arrayLength);
        }
    }
}
