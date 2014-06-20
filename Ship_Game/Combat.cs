using System;

namespace Ship_Game
{
	public class Combat
	{
		public float Timer = 4f;

		public PlanetGridSquare Attacker;

		public PlanetGridSquare Defender;

		public int phase = 1;

		public Combat()
		{
		}
	}
}