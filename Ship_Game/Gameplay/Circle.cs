using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Gameplay
{
	public sealed class Circle
	{
		public float Radius;

		public Vector2 Center;

		public string ID;

		public Color C;

		public bool IsChecked;

		public Circle(Vector2 center, float radius)
		{
			this.Radius = radius;
			this.Center = center;
		}
	}
}