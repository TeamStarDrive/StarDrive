using Microsoft.Xna.Framework;
using System;

namespace Ship_Game
{
	public class RandomThreadMath
	{
		private static System.Random random;

		public static float Last;

		public static System.Random Random
		{
			get
			{
				return RandomThreadMath.random;
			}
		}

		static RandomThreadMath()
		{
			RandomThreadMath.random = new System.Random();
			RandomThreadMath.Last = 0f;
		}

		public RandomThreadMath()
		{
		}

		public float RandomBetween(float minimum, float maximum)
		{
			return minimum + (float)RandomThreadMath.random.NextDouble() * (maximum - minimum);
		}

		public Vector2 RandomDirection()
		{
			float angle = this.RandomBetween(0f, 6.28318548f);
			return new Vector2((float)Math.Cos((double)angle), (float)Math.Sin((double)angle));
		}

		public Vector2 RandomDirection(float minimumAngle, float maximumAngle)
		{
			float angle = this.RandomBetween(MathHelper.ToRadians(minimumAngle), MathHelper.ToRadians(maximumAngle)) - 1.57079637f;
			return new Vector2((float)Math.Cos((double)angle), (float)Math.Sin((double)angle));
		}
	}
}