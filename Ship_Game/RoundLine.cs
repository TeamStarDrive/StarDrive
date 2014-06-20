using Microsoft.Xna.Framework;
using System;

namespace Ship_Game
{
	public class RoundLine
	{
		private Vector2 p0;

		private Vector2 p1;

		private float rho;

		private float theta;

		public Vector2 P0
		{
			get
			{
				return this.p0;
			}
			set
			{
				this.p0 = value;
				this.RecalcRhoTheta();
			}
		}

		public Vector2 P1
		{
			get
			{
				return this.p1;
			}
			set
			{
				this.p1 = value;
				this.RecalcRhoTheta();
			}
		}

		public float Rho
		{
			get
			{
				return this.rho;
			}
		}

		public float Theta
		{
			get
			{
				return this.theta;
			}
		}

		public RoundLine(Vector2 p0, Vector2 p1)
		{
			this.p0 = p0;
			this.p1 = p1;
			this.RecalcRhoTheta();
		}

		public RoundLine(float x0, float y0, float x1, float y1)
		{
			this.p0 = new Vector2(x0, y0);
			this.p1 = new Vector2(x1, y1);
			this.RecalcRhoTheta();
		}

		protected void RecalcRhoTheta()
		{
			Vector2 delta = this.P1 - this.P0;
			this.rho = delta.Length();
			this.theta = (float)Math.Atan2((double)delta.Y, (double)delta.X);
		}
	}
}