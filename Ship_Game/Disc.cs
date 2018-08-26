using Microsoft.Xna.Framework;

namespace Ship_Game
{
	public sealed class Disc : RoundLine
	{
		public Vector2 Pos
		{
			get
			{
				return P0;
			}
			set
			{
				P0 = value;
				P1 = value;
			}
		}

		public Disc(Vector2 p) : base(p, p)
		{
		}

		public Disc(float x, float y) : base(x, y, x, y)
		{
		}
	}
}