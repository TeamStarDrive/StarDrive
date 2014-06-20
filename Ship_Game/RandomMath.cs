using Microsoft.Xna.Framework;
using System;

namespace Ship_Game
{
	public static class RandomMath
	{
		private static System.Random random;

		public static float Last;

		public static System.Random Random
		{
			get
			{
				return RandomMath.random;
			}
		}

		static RandomMath()
		{
			RandomMath.random = new System.Random();
			RandomMath.Last = 0f;
		}

		public static float RandomBetween(float minimum, float maximum)
		{
			return minimum + (float)RandomMath.random.NextDouble() * (maximum - minimum);
		}

		public static Vector2 RandomDirection()
		{
			float angle = RandomMath.RandomBetween(0f, 6.28318548f);
			return new Vector2((float)Math.Cos((double)angle), (float)Math.Sin((double)angle));
		}

		public static Vector2 RandomDirection(float minimumAngle, float maximumAngle)
		{
			float angle = RandomMath.RandomBetween(MathHelper.ToRadians(minimumAngle), MathHelper.ToRadians(maximumAngle)) - 1.57079637f;
			return new Vector2((float)Math.Cos((double)angle), (float)Math.Sin((double)angle));
		}
	}
}