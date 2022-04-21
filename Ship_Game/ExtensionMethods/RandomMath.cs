using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Ship_Game.Utils;

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
            return Random.InRange(arrayLength);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T RandItem<T>(IReadOnlyList<T> items)
        {
            return Random.RandItem(items);
        }

        // performs a dice-roll, where chance must be between [0..100]
        // @return TRUE if random chance passed
        // @example if (RandomMath.RollDice(33)) {..} // 33% chance
        public static bool RollDice(float percent)
        {
            return Random.RollDice(percent);
        }

        // returns a specific die size roll, like 1d20, 1d6, etc. Minimum value can be changed
        public static int RollDie(int dieSize, int minimum = 1)
        {
            return Random.RollDie(dieSize, minimum);
        }

        public static Vector2 RandomDirection()
        {
            return Random.Direction2D();
        }

        // Generates a Vector2 with X Y in range [-radius, +radius]
        public static Vector2 Vector2D(float radius)
        {
            return Random.Vector2D(radius);
        }

        // Generates a Vector3 with X Y Z in range [-radius, +radius]
        public static Vector3 Vector3D(float radius)
        {
            return Random.Vector3D(radius);
        }

        // Generates a Vector3 with X Y Z in range [minradius, maxradius]
        public static Vector3 Vector3D(float minradius, float maxradius)
        {
            return Random.Vector3D(minradius, maxradius);
        }

        // Generates a Vector3 with X Y Z in range [minValue, maxValue]
        public static Vector3 Vector3D(in Vector3 minValue, in Vector3 maxValue)
        {
            return Random.Vector3D(minValue, maxValue);
        }

        // Generates a Vector3 with X Y Z in range [-minMax, +minMax]
        public static Vector3 Vector3D(in Vector3 minMax)
        {
            return Random.Vector3D(minMax);
        }

        public static float AvgFloat(float minimum, float maximum, int iterations = 3)
        {
            return Random.AvgFloat(minimum, maximum, iterations);
        }

        public static int AvgInt(int minimum, int maximum, int iterations = 3)
        {
            return Random.AvgInt(minimum, maximum, iterations);
        }

        public static bool Roll3DiceAvg(float percent)
        {
            return Random.Roll3DiceAvg(percent);
        }
    }
}