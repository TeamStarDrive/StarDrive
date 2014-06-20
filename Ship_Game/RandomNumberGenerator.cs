using System;

namespace Ship_Game
{
	public sealed class RandomNumberGenerator : Random
	{
		private readonly static RandomNumberGenerator instance;

		public static RandomNumberGenerator Instance
		{
			get
			{
				return RandomNumberGenerator.instance;
			}
		}

		static RandomNumberGenerator()
		{
			RandomNumberGenerator.instance = new RandomNumberGenerator();
		}

		private RandomNumberGenerator()
		{
		}
	}
}