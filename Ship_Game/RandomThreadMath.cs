using Microsoft.Xna.Framework;
using System;

namespace Ship_Game
{
    // @todo This is at least third copy of RandomMath, RandomMath2, ... what gives?
	public sealed class RandomThreadMath
	{
		public static readonly Random Random = new Random();

		public float RandomBetween(float minimum, float maximum)
		{
			return minimum + (float)Random.NextDouble() * (maximum - minimum);
		}

        public int IntBetween(int min, int max)
        {
            return Random.Next(min, max);
        }

		public Vector2 RandomDirection()
		{
			float angle = RandomBetween(0f, 6.28318548f);
			return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
		}

		public Vector2 RandomDirection(float minimumAngle, float maximumAngle)
		{
			float angle = RandomBetween(minimumAngle.ToRadians(), maximumAngle.ToRadians()) - 1.57079637f;
			return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
		}
	}
}