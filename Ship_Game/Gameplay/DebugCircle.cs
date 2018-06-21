using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Gameplay
{
	public struct DebugCircle
	{
		public float Radius;
		public Vector2 Center;
		public Color Color;
        public float Time;

		public DebugCircle(Vector2 center, float radius, Color color, float time)
		{
			Radius = radius;
			Center = center;
            Color  = color;
		    Time   = time;
		}
	}
}