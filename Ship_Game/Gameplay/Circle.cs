using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game.Gameplay
{
	public class Circle
	{
		public float Radius;

		public Vector2 Center;

		public string ID;

		public Color c;

		public bool isChecked;

		public Circle(Vector2 Center, float Radius)
		{
			this.Radius = Radius;
			this.Center = Center;
		}
	}
}