using Microsoft.Xna.Framework;
using System;

namespace Ship_Game.Gameplay
{
	public class Arc
	{
		public Vector2 Start;

		public Vector2 End;

		public Vector2 Center;

		public float Radius;

		public float AngularDistance;

		public Arc(Vector2 Start, Vector2 End, Vector2 Center, float Radius, float AngularDistance)
		{
			this.Start = Start;
			this.End = End;
			this.Center = Center;
			this.Radius = Radius;
			this.AngularDistance = AngularDistance;
		}
	}
}