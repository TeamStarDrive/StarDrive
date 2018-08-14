using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class SlidingElement
	{
		public Rectangle Housing;

		public Rectangle ClickArea;

		private bool Hover;

		public bool Open;

        public Rectangle ButtonHousing;

		public SlidingElement(Rectangle r)
		{
			Housing = r;
			ClickArea = new Rectangle(r.X + r.Width - 32, r.Y + 38, 21, 56);
		}

		public void Draw(ScreenManager ScreenManager, int HowFar)
		{
			Rectangle r = Housing;
			r.X = r.X + HowFar;
            ButtonHousing = r;
			ClickArea.X = r.X + r.Width - 32;
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/unitselmenu_tab"), r, Color.White);
			if (!Open)
			{
				if (!Hover)
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_arrow_right"), ClickArea, Color.White);
					return;
				}
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_arrow_right_hover"), ClickArea, Color.White);
				return;
			}
			if (!Hover)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_arrow_left"), ClickArea, Color.White);
				return;
			}
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_arrow_left_hover"), ClickArea, Color.White);
		}

		public bool HandleInput(InputState input)
		{
			Hover = false;
			if (ClickArea.HitTest(input.CursorPosition))
			{
				Hover = true;
				if (input.InGameSelect)
				{
					Open = !Open;
					return true;
				}
			}
			return false;
		}
	}
}