using Microsoft.Xna.Framework;
using System;

namespace Ship_Game
{
	public static class RandomMath2
	{
		public static float Last;
		public static Random Random { get; } = new Random();

		public static float RandomBetween(float minimum, float maximum)
		{
			return minimum + (float)Random.NextDouble() * (maximum - minimum);
		}

		public static Vector2 RandomDirection()
		{
			float angle = RandomBetween(0f, 6.28318548f);
			return new Vector2((float)Math.Cos((double)angle), (float)Math.Sin((double)angle));
		}

		public static Vector2 RandomDirection(float minimumAngle, float maximumAngle)
		{
			float angle = RandomBetween(MathHelper.ToRadians(minimumAngle), MathHelper.ToRadians(maximumAngle)) - 1.57079637f;
			return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
		}
	}
}