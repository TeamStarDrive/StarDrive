using Microsoft.Xna.Framework;
using System;

namespace Ship_Game
{
	public class MuzzleFlash
	{
		public Matrix WorldMatrix;

		public float timer = 0.02f;

		public float scale = 0.25f;

		public GameplayObject Owner;

		public MuzzleFlash()
		{
		}
	}
}