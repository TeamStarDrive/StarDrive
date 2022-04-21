using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Ship_Game.Utils;

namespace Ship_Game
{
    public static class RandomMath2
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

        /// Random index, in range [0, arrayLength), arrayLength can be negative
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int InRange(int arrayLength)
        {
            return Random.InRange(0, arrayLength);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T RandItem<T>(IReadOnlyList<T> items)
        {
            return Random.RandItem(items);
        }

        // Generates a Vector2 with X Y in range [-radius, +radius]
        public static Vector2 Vector2D(float radius)
        {
            return Random.Vector2D(radius);
        }
    }
}