using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    // @todo This is at least third copy of RandomMath, RandomMath2, ... what gives?
    public sealed class UniverseRandom
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
            return Random.Int(minimum, maximum+1);
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

        // performs a dice-roll, where chance must be between [0..100]
        // @return TRUE if random chance passed
        // @example if (RandomMath.RollDice(33)) {..} // 33% chance
        public static bool RollDice(float percent)
        {
            return RandomBetween(0f, 100f) < percent;
        }

        public static Vector2 RandomDirection()
        {
            float angle = RandomBetween(0f, RadMath.TwoPI);
            return angle.RadiansToDirection();
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