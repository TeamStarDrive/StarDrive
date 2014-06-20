using Microsoft.Xna.Framework;
using Ship_Game;
using System;

namespace Ship_Game.Gameplay
{
	public class ClickablePlanetButton
	{
		public Vector2 ScreenPos;

		public float Radius;

		public Planet planet;

		public string Function;

		public bool Hovering;

		public string Path;

		public bool tooFar;

		public ClickablePlanetButton()
		{
		}
	}
}