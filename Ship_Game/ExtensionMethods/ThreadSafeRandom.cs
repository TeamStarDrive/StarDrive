using System;
using System.Threading;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public class ThreadSafeRandom
    {
        // NOTE: This is really fast
        readonly ThreadLocal<Random> Randoms;
        readonly int Seed;

        public ThreadSafeRandom()
        {
            Randoms = new ThreadLocal<Random>(CreateNewRandomProvider);
        }

        public ThreadSafeRandom(int seed)
        {
            Seed = seed;
            Randoms = new ThreadLocal<Random>(CreateNewRandomProvider);
        }

        Random CreateNewRandomProvider()
        {
            return Seed == 0 ? new Random() : new Random(Seed);
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

        /// Random index, in range [0, arrayLength)
        public int InRange(int arrayLength)
        {
            return Randoms.Value.Next(0, arrayLength);
        }

        /// performs a dice-roll, where chance must be between [0..100]
        /// @return TRUE if random chance passed
        /// @example if (RandomMath.RollDice(33)) {..} // 33% chance
        public bool RollDice(float percent)
        {
            float randomFloat = Float(0f, 100f);
            return randomFloat < percent;
        }

        /// @return a specific die size roll, like 1d20, 1d6, etc. Minimum value can be changed
        public int RollDie(int dieSize, int minimum = 1)
        {
            return Int(1, dieSize).LowerBound(minimum);
        }

        /// Generates a random 2D direction vector
        public Vector2 Direction2D()
        {
            float radians = Float(0f, RadMath.TwoPI);
            return radians.RadiansToDirection();
        }

        /// Generates a Vector2 with X Y in range [-radius, +radius]
        public Vector2 Vector2D(float radius)
        {
            return new Vector2(Float(-radius, +radius), Float(-radius, +radius));
        }
    }
}
