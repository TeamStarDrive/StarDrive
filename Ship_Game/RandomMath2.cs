using Microsoft.Xna.Framework;
using System;

namespace Ship_Game
{
	public static class RandomMath2
	{
		private static System.Random random;

		public static float Last;

		public static System.Random Random
		{
			get
			{
				return RandomMath2.random;
			}
		}

		static RandomMath2()
		{
			RandomMath2.random = new System.Random();
			RandomMath2.Last = 0f;
		}

		public static float RandomBetween(float minimum, float maximum)
		{
			return minimum + (float)RandomMath2.random.NextDouble() * (maximum - minimum);
		}

		public static Vector2 RandomDirection()
		{
			float angle = RandomMath2.RandomBetween(0f, 6.28318548f);
			return new Vector2((float)Math.Cos((double)angle), (float)Math.Sin((double)angle));
		}

		public static Vector2 RandomDirection(float minimumAngle, float maximumAngle)
		{
			float angle = RandomMath2.RandomBetween(MathHelper.ToRadians(minimumAngle), MathHelper.ToRadians(maximumAngle)) - 1.57079637f;
			return new Vector2((float)Math.Cos((double)angle), (float)Math.Sin((double)angle));
		}
	}
}