using Microsoft.Xna.Framework;

namespace Ship_Game
{
	public sealed class MuzzleFlash
	{
		public Matrix WorldMatrix;

		public float timer = 0.02f;

		public float scale = 0.25f;

		public GameplayObject Owner;
	}
}