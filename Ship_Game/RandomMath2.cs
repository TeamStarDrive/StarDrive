using Microsoft.Xna.Framework;
using System;

namespace Ship_Game
{
	public static class RandomMath2
	{
		public static readonly Random Random = new Random();

        /// Generate random, inclusive [minimum, maximum]
		public static float RandomBetween(float minimum, float maximum)
		{
			return minimum + (float)Random.NextDouble() * (maximum - minimum);
		}

        /// Generate random index, upper bound excluded: [startIndex, arrayLength)
        public static int InRange(int startIndex, int arrayLength)
        {
            return Random.Next(startIndex, arrayLength);
        }

        /// Random index, in range [0, arrayLength)
        public static int InRange(int arrayLength)
        {
            return Random.Next(0, arrayLength);
        }

		public static Vector2 RandomDirection()
		{
			float angle = RandomBetween(0f, 6.28318548f);
			return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
		}

		public static Vector2 RandomDirection(float minimumAngle, float maximumAngle)
		{
			float angle = RandomBetween(minimumAngle.ToRadians(), maximumAngle.ToRadians()) - 1.57079637f;
			return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
		}
	}
}