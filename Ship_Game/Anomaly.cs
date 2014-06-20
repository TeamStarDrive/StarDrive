using Microsoft.Xna.Framework;
using System;

namespace Ship_Game
{
	public class Anomaly
	{
		public static UniverseScreen screen;

		public Vector2 Position;

		public string type;

		public Anomaly()
		{
		}

		public virtual void Draw()
		{
		}

		public virtual void Update(float elapsedTime)
		{
		}
	}
}