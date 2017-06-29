using Microsoft.Xna.Framework;

namespace Ship_Game
{
	public class Anomaly
	{
		public Vector2 Position;
		public string type;


		public virtual void Draw()
		{
		}

		public virtual void Update(float elapsedTime)
		{
		}
	}
}