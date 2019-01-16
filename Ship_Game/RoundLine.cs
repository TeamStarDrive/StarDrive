using System;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
	public class RoundLine
	{
		private Vector2 p0;
		private Vector2 p1;

        public Vector2 P0
		{
			get => p0;
            set
			{
				p0 = value;
				RecalcRhoTheta();
			}
		}

		public Vector2 P1
		{
			get => p1;
            set
			{
				p1 = value;
				RecalcRhoTheta();
			}
		}

		public float Rho { get; private set; }

        public float Theta { get; private set; }

        public RoundLine(Vector2 p0, Vector2 p1)
		{
			this.p0 = p0;
			this.p1 = p1;
			RecalcRhoTheta();
		}

		public RoundLine(float x0, float y0, float x1, float y1)
		{
			p0 = new Vector2(x0, y0);
			p1 = new Vector2(x1, y1);
			RecalcRhoTheta();
		}

		protected void RecalcRhoTheta()
		{
			Vector2 delta = P1 - P0;
			Rho = delta.Length();
			Theta = (float)Math.Atan2(delta.Y, delta.X);
		}
	}
}