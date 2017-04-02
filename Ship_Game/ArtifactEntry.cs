using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class ArtifactEntry
	{
		public Array<SkinnableButton> ArtifactButtons = new Array<SkinnableButton>();

		public ArtifactEntry()
		{
		}

		public void Update(Vector2 Position)
		{
			Vector2 Cursor = Position;
			foreach (SkinnableButton button in this.ArtifactButtons)
			{
				button.r.X = (int)Cursor.X;
				button.r.Y = (int)Cursor.Y;
				Cursor.X = Cursor.X + 36f;
			}
		}
	}
}