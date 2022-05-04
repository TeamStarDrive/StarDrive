using System;
using System.Collections.Generic;
using SDGraphics;

namespace Ship_Game.Utils
{
    /// <summary>
    /// Provides a base class for Random number utilities
    /// </summary>
    public abstract class RandomBase
    {
        protected int Seed;

        protected RandomBase(int seed)
        {
            Seed = seed == 0 ? Environment.TickCount : seed;
        }

        protected abstract Random Rand { get; }

        /// <summary>
        /// Generate a random float within inclusive [min, max]
        /// </summary>
        public float Float(float min, float max)
        {
            return min + (float)Rand.NextDouble() * (max - min);
        }

        /// <summary>
        /// Generate a random float within inclusive [0..1]
        /// </summary>
        public float Float()
        {
            return (float)Rand.NextDouble();
        }

        /// <summary>
        /// Generate a random double within inclusive [min, max]
        /// </summary>
        public double Double(double min, double max)
        {
            return min + Rand.NextDouble() * (max - min);
        }

        /// <summary>
        /// Generate a random double within inclusive [0..1]
        /// </summary>
        public double Double()
        {
            return Rand.NextDouble();
        }

        /// <summary>
        /// Generate random integer within inclusive [min, max]
        /// </summary>
        public int Int(int min, int max)
        {
            return Rand.Next(min, max + 1);
        }

        /// <summary>
        /// Generates a random byte
        /// </summary>
        public byte Byte()
        {
            return (byte)Rand.Next(255);
        }

        /// <summary>
        /// Generate random index, upper bound excluded: [startIndex, arrayLength)
        /// Example: int itemIndex = random.InRange(0, items.Length);
        /// </summary>
        public int InRange(int startIndex, int arrayLength)
        {
            return Rand.Next(startIndex, arrayLength);
        }

        /// <summary>
        /// Random index, in range [0, arrayLength)
        /// Example: int itemIndex = random.InRange(items.Length);
        /// </summary>
        public int InRange(int arrayLength)
        {
            return Rand.Next(0, arrayLength);
        }

        /// <summary>
        /// Chooses a random element from the list
        /// </summary>
        public T RandItem<T>(IReadOnlyList<T> items)
        {
            return items[InRange(items.Count)];
        }

        /// <summary>
        /// Performs a dice-roll, where chance must be between [0..100]
        /// @return TRUE if random chance passed
        /// @example if (RandomMath.RollDice(33)) {..} // 33% chance
        /// </summary>
        public bool RollDice(float percent)
        {
            float randomFloat = Float(0f, 100f);
            return randomFloat < percent;
        }

        /// <summary>
        /// @return a specific die size roll, like 1d20, 1d6, etc. Minimum value can be changed
        /// Example: RollDie(10) will give a random int within [1, 10]
        /// Example: RollDie(12, minimum:4) will give a random int within [4, 12]
        /// </summary>
        public int RollDie(int dieSize, int minimum = 1)
        {
            return Math.Max(minimum, Int(1, dieSize));
        }

        /// <summary>
        /// Rolls a 100 sided dice 3 times and checks
        /// if the average result is less than percent
        /// </summary>
        public bool Roll3DiceAvg(float percent)
        {
            return RollDiceAvg(3) < percent;
        }

        float RollDiceAvg(int iterations)
        {
            if (iterations == 0)
                return 0f;

            float sum = 0;
            for (int i = 0; i < iterations; i++)
                sum += Float(0f, 100f);
            return (sum / iterations);
        }

        /// <summary>
        /// Generates a random 2D direction vector
        /// </summary>
        public Vector2 Direction2D()
        {
            float radians = Float(0f, RadMath.TwoPI);
            return radians.RadiansToDirection();
        }

        /// <summary>
        /// Generates a Vector2 with X and Y in range [-radius, +radius]
        /// </summary>
        public Vector2 Vector2D(float radius)
        {
            return new Vector2(Float(-radius, +radius),
                               Float(-radius, +radius));
        }

        /// <summary>
        /// Generates a Vector2 with X and Y in range [-min, +max]
        /// </summary>
        public Vector2 Vector2D(float min, float max)
        {
            return new Vector2(Float(min, max),
                               Float(min, max));
        }

        /// <summary>
        /// Generates a Vector3 with X Y Z in range [-radius, +radius]
        /// </summary>
        public Vector3 Vector3D(float radius)
        {
            return new Vector3(Float(-radius, +radius),
                               Float(-radius, +radius),
                               Float(-radius, +radius));
        }

        /// <summary>
        /// Generates a Vector3 with Z set to 0.0f
        /// </summary>
        public Vector3 Vector32D(float radius)
        {
            return new Vector3(Float(-radius, +radius),
                               Float(-radius, +radius),
                               0f);
        }

        /// <summary>
        /// Generates a Vector3 with X Y Z in range [min, max]
        /// </summary>
        public Vector3 Vector3D(float min, float max)
        {
            return new Vector3(Float(min, max),
                               Float(min, max),
                               Float(min, max));
        }

        /// <summary>
        /// Generates a Vector3 with X Y Z in range [minValue, maxValue]
        /// </summary>
        public Vector3 Vector3D(in Vector3 minValue, in Vector3 maxValue)
        {
            return new Vector3(Float(minValue.X, maxValue.X),
                               Float(minValue.Y, maxValue.Y),
                               Float(minValue.Z, maxValue.Z));
        }

        /// <summary>
        /// Generates a Vector3 with X Y Z in range [-minMax, +minMax]
        /// </summary>
        public Vector3 Vector3D(in Vector3 minMax)
        {
            return new Vector3(Float(-minMax.X, minMax.X),
                               Float(-minMax.Y, minMax.Y),
                               Float(-minMax.Z, minMax.Z));
        }

        /// <summary>
        /// Average of multiple Float randoms
        /// </summary>
        public float AvgFloat(float min, float max, int iterations = 3)
        {
            if (iterations <= 1)
                return Float(min, max);

            float rand = 0;
            for (int x = 0; x < iterations; ++x)
                rand += Float(min, max);
            return (rand / iterations);
        }

        /// <summary>
        /// Average of multiple Int randoms
        /// </summary>
        public int AvgInt(int min, int max, int iterations = 3)
        {
            if (iterations <= 1)
                return Int(min, max);

            int rand = 0;
            for (int x = 0; x < iterations; ++x)
                rand += Int(min, max);
            return (rand / iterations);
        }
    }
}
