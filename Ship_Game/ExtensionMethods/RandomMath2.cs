using System;
using Microsoft.Xna.Framework;

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
        
        /// Generate random, inclusive [minimum, maximum]
        public static int IntBetween(int minimum, int maximum)
        {
            return Random.Next(minimum, maximum+1);
        }

        /// Generate random index, upper bound excluded: [startIndex, arrayLength)
        public static int InRange(int startIndex, int arrayLength)
        {
            return Random.Next(startIndex, arrayLength);
        }

        /// Random index, in range [0, arrayLength), arrayLength can be negative
        public static int InRange(int arrayLength)
        {
            if (arrayLength < 0)
                return -Random.Next(0, -arrayLength);
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