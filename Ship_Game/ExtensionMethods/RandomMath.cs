using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
    public static class RandomMath
    {
        static readonly ThreadSafeRandom Random = new ThreadSafeRandom();

        /// Generate random, inclusive [minimum, maximum]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RandomBetween(float minimum, float maximum)
        {
            return Random.Float(minimum, maximum);
        }

        /// Generate random, inclusive [minimum, maximum]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IntBetween(int minimum, int maximum)
        {
            return Random.Int(minimum, maximum);
        }

        /// Generate random index, upper bound excluded: [startIndex, arrayLength)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int InRange(int startIndex, int arrayLength)
        {
            return Random.InRange(startIndex, arrayLength);
        }

        /// Random index, in range [0, arrayLength)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int InRange(int arrayLength)
        {
            return Random.InRange(0, arrayLength);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T RandItem<T>(IReadOnlyList<T> items)
        {
            return items[InRange(items.Count)];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T RandItem<T>(Array<T> items)
        {
            return items[InRange(items.Count)];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T RandItem<T>(T[] items)
        {
            return items[InRange(items.Length)];
        }

        public static float AvgRandomBetween(float minimum, float maximum)
        {
            const int ITERATIONS = 3;
            float rand = 0;
            for (int x = 0; x < ITERATIONS; ++x)
            {
                rand += RandomBetween(minimum, maximum);
            }
            return (float.IsNaN(rand) || float.IsInfinity(rand)) ? minimum : (rand / ITERATIONS);
        }

        public static int AvgRandomBetween(int minimum, int maximum)
        {
            const int ITERATIONS = 3;
            int rand = 0;
            for (int x = 0; x < ITERATIONS; ++x)
            {
                rand += Random.Int(minimum, maximum);
            }
            return (rand == 0) ? minimum : (rand / ITERATIONS);
        }

        // performs a dice-roll, where chance must be between [0..100]
        // @return TRUE if random chance passed
        // @example if (RandomMath.RollDice(33)) {..} // 33% chance
        public static bool RollDice(float percent)
        {
            return RandomBetween(0f, 100f) < percent;
        }

        // returns a specific die size roll, like 1d20, 1d6, etc. Minimum value can be changed
        public static int RollDie(int dieSize, int minimum = 1)
        {
            return Random.Int(minimum, dieSize);
        }

        public static bool Roll3DiceAvg(float percent)
        {
            return RollDiceAvg(3, 100) < percent;
        }

        public static int RollDiceAvg(int numberOfDice, int size)
        {
            if (numberOfDice == 0)
                return 0;

            float result = 0;
            for (int i = 0; i < numberOfDice; i++)
            {
                result += RandomBetween(0f, size);
            }
            return (int)(result / numberOfDice);
        }

        public static int RollAvgPercentVarianceFrom50()
        {
            int result = RollDiceAvg(3, 100);
            return Math.Abs(result - 50);
        }

        public static Vector2 RandomDirection()
        {
            float radians = RandomBetween(0f, RadMath.TwoPI);
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