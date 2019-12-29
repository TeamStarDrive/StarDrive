using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
    public static class RandomMath
    {
        public static readonly Random Random = new Random();

        public static float AvgRandomBetween(float minimum, float maximum)
        {
            float rand = 0;
            int x = 0;
            for(; x<3; x++)
            {
                rand += RandomBetween(minimum, maximum);
            }
            rand /= x;
            if (float.IsNaN(rand) || float.IsInfinity(rand))
                rand = minimum;
            return rand;
        }

        /// Generate random, inclusive [minimum, maximum]
        public static float RandomBetween(float minimum, float maximum)
        {
            return minimum + (float)Random.NextDouble() * (maximum - minimum);
        }

        /// Generate random, inclusive [minimum, maximum]
        public static int IntBetween(int minimum, int maximum)
        {
            return Random.Next(minimum, maximum+1);
        }

        /// Generate random index, upper bound excluded: [startIndex, arrayLength)
        public static int InRange(int startIndex, int arrayLength)
        {
            return Random.Next(startIndex, arrayLength);
        }

        /// Random index, in range [0, arrayLength), arrayLength can be negative
        public static int InRange(int arrayLength)
        {
            if (arrayLength < 0)
                return -Random.Next(0, -arrayLength);
            return Random.Next(0, arrayLength);
        }

        // performs a dice-roll, where chance must be between [0..100]
        // @return TRUE if random chance passed
        // @example if (RandomMath.RollDice(33)) {..} // 33% chance
        public static bool RollDice(float percent)
        {
            return RandomBetween(0f, 100f) < percent;
        }

        // returns a specific die size roll, like 1d20, 1d6, etc.
        public static int RollDie(int dieSize)
        {
            return IntBetween(1, dieSize);
        }

        public static bool RollDiceAvg(float percent)
        {
            float result = RandomBetween(0f, 100f) + RandomBetween(0f, 100f) + RandomBetween(0f, 100f);
            return result / 3 < percent;
        }

        public static T RandItem<T>(IReadOnlyList<T> items)
        {
            return items[InRange(items.Count)];
        }

        public static T RandItem<T>(Array<T> items)
        {
            return items[InRange(items.Count)];
        }

        public static T RandItem<T>(T[] items)
        {
            return items[InRange(items.Length)];
        }

        public static Vector2 RandomDirection()
        {
            float radians = RandomBetween(0f, 6.28318548f);
            return radians.RadiansToDirection();
        }

        // Generates a Vector2 with X Y in range [-radius, +radius]
        public static Vector2 Vector2D(float radius)
        {
            return new Vector2(RandomBetween(-radius, +radius), RandomBetween(-radius, +radius));
        }

        // Generates a Vector3 with X Y Z in range [-radius, +radius]
        public static Vector3 Vector3D(float radius)
        {
            return new Vector3(RandomBetween(-radius, +radius), RandomBetween(-radius, +radius),
                               RandomBetween(-radius, +radius));
        }

        // Generates a Vector3 with X Y Z in range [minradius, maxradius]
        public static Vector3 Vector3D(float minradius, float maxradius)
        {
            return new Vector3(RandomBetween(minradius, maxradius), RandomBetween(minradius, maxradius),
                               RandomBetween(minradius, maxradius));
        }

        // Generates a Vector3 with Z set to 0.0f
        public static Vector3 Vector32D(float radius)
        {
            return new Vector3(RandomBetween(-radius, +radius), RandomBetween(-radius, +radius), 0f);
        }
    }
}