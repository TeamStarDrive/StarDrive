using Microsoft.Xna.Framework;

namespace Ship_Game
{
	public sealed class ArtifactEntry
	{
		public Array<SkinnableButton> ArtifactButtons = new Array<SkinnableButton>();

	    public void Update(Vector2 Position)
		{
			Vector2 Cursor = Position;
			foreach (SkinnableButton button in ArtifactButtons)
			{
				button.r.X = (int)Cursor.X;
				button.r.Y = (int)Cursor.Y;
				Cursor.X = Cursor.X + 36f;
			}
		}
	}
}