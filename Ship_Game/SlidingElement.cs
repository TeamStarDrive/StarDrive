using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class SlidingElement
	{
		public Rectangle Housing;

		public Rectangle ClickArea;

		private bool Hover;

		public bool Open;

		public SlidingElement(Rectangle r)
		{
			this.Housing = r;
			this.ClickArea = new Rectangle(r.X + r.Width - 32, r.Y + 38, 21, 56);
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, int HowFar)
		{
			Rectangle r = this.Housing;
			r.X = r.X + HowFar;
			this.ClickArea.X = r.X + r.Width - 32;
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/unitselmenu_tab"], r, Color.White);
			if (!this.Open)
			{
				if (!this.Hover)
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/button_arrow_right"], this.ClickArea, Color.White);
					return;
				}
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/button_arrow_right_hover"], this.ClickArea, Color.White);
				return;
			}
			if (!this.Hover)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/button_arrow_left"], this.ClickArea, Color.White);
				return;
			}
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/button_arrow_left_hover"], this.ClickArea, Color.White);
		}

		public bool HandleInput(InputState input)
		{
			this.Hover = false;
			if (HelperFunctions.CheckIntersection(this.ClickArea, input.CursorPosition))
			{
				this.Hover = true;
				if (input.InGameSelect)
				{
					this.Open = !this.Open;
					return true;
				}
			}
			return false;
		}
	}
}