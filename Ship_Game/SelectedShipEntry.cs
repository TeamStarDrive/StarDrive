using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class SelectedShipEntry
	{
		public List<SkinnableButton> ShipButtons = new List<SkinnableButton>();

		public SelectedShipEntry()
		{
		}

		public void Update(Vector2 Position)
		{
			Vector2 Cursor = Position;
			foreach (SkinnableButton button in this.ShipButtons)
			{
				button.r.X = (int)Cursor.X;
				button.r.Y = (int)Cursor.Y;
				Cursor.X = Cursor.X + 24f;
			}
		}
	}
}