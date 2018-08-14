using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class Menu2
	{
		public Rectangle Menu;

		private Rectangle corner_TL;

		private Rectangle corner_TR;

		private Rectangle corner_BL;

		private Rectangle corner_BR;

		private Rectangle vertLeft;

		private Rectangle vertRight;

		private Rectangle horizTop;

		private Rectangle horizBot;

		private Array<Rectangle> RepeatTops = new Array<Rectangle>();

		private Rectangle extendTopLeft;

		private Rectangle extendTopRight;

		private Rectangle topExtender;

		public Menu2(Rectangle theMenu)
		{
			this.Menu = theMenu;
			this.corner_TL = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.Texture("NewUI/menu_2_corner_TL").Width, ResourceManager.Texture("NewUI/menu_2_corner_TL").Height);
			this.corner_TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.Texture("NewUI/menu_2_corner_TR").Width, theMenu.Y, ResourceManager.Texture("NewUI/menu_2_corner_TR").Width, ResourceManager.Texture("NewUI/menu_2_corner_TR").Height);
			this.corner_BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_2_corner_BL").Height, ResourceManager.Texture("NewUI/menu_2_corner_BL").Width, ResourceManager.Texture("NewUI/menu_2_corner_BL").Height);
			this.corner_BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.Texture("NewUI/menu_2_corner_BR").Width, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_2_corner_BR").Height, ResourceManager.Texture("NewUI/menu_2_corner_BR").Width, ResourceManager.Texture("NewUI/menu_2_corner_BR").Height);
			int topDistance = theMenu.Width - this.corner_TL.Width - this.corner_TR.Width;
			int numberRepeats = topDistance / ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Width;
			int remainder = numberRepeats * ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Width - topDistance;
			this.extendTopLeft = new Rectangle(this.corner_TL.X + this.corner_TL.Width, this.corner_TL.Y + 3, remainder / 2, ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender").Height);
			this.extendTopRight = new Rectangle(this.corner_TL.X + this.corner_TL.Width - remainder / 2, this.corner_TL.Y + 3, remainder / 2, ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender").Height);
			this.topExtender = new Rectangle(theMenu.X + 8, this.corner_TL.Y + 3, theMenu.Width - 16, ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender").Height);
			for (int i = 0; i < numberRepeats + 1; i++)
			{
				Rectangle repeat = new Rectangle(this.corner_TL.X + this.corner_TL.Width + remainder + ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Width * i, this.extendTopLeft.Y, ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Width, ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Height);
				this.RepeatTops.Add(repeat);
			}
			this.horizTop = new Rectangle(this.corner_TL.X + this.corner_TL.Width, this.corner_TL.Y + 3, theMenu.Width - this.corner_TL.Width - this.corner_TR.Width, ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender").Height);
			this.horizBot = new Rectangle(this.corner_BL.X + this.corner_BL.Width, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_2_horiz_lower").Height, theMenu.Width - this.corner_BL.Width - this.corner_BR.Width, ResourceManager.Texture("NewUI/menu_2_horiz_lower").Height);
			this.vertLeft = new Rectangle(this.corner_TL.X + 1, this.corner_TL.Y + this.corner_TL.Height, ResourceManager.Texture("NewUI/menu_2_vert_left").Width, theMenu.Height - this.corner_TL.Height - this.corner_BL.Height);
			this.vertRight = new Rectangle(theMenu.X - 1 + theMenu.Width - ResourceManager.Texture("NewUI/menu_2_vert_right").Width, this.corner_TR.Y + this.corner_TR.Height, ResourceManager.Texture("NewUI/menu_2_vert_right").Width, theMenu.Height - this.corner_TR.Height - this.corner_BR.Height);
		}

		public void Draw(Color bgcolor)
		{
            var spriteBatch = Game1.Instance.ScreenManager.SpriteBatch;
		    spriteBatch.FillRectangle(new Rectangle(this.Menu.X + 8, this.Menu.Y + 8, this.Menu.Width - 8, this.Menu.Height - 8), bgcolor);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_lower"), this.horizBot, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender"), this.topExtender, Color.White);
			foreach (Rectangle r in this.RepeatTops)
			{
			    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat"), r, Color.White);
			}
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_left"), this.vertLeft, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_right"), this.vertRight, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TL"), this.corner_TL, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TR"), this.corner_TR, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BL"), this.corner_BL, Color.White);
			spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BR"), this.corner_BR, Color.White);
		}

		public void Draw()
		{
		    var spriteBatch = Game1.Instance.ScreenManager.SpriteBatch;
		    spriteBatch.FillRectangle(new Rectangle(this.Menu.X + 8, this.Menu.Y + 8, this.Menu.Width - 8, this.Menu.Height - 8), new Color(0, 0, 0, 240));
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_lower"), this.horizBot, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender"), this.topExtender, Color.White);
			foreach (Rectangle r in this.RepeatTops)
			{
			    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat"), r, Color.White);
			}
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_left"), this.vertLeft, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_right"), this.vertRight, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TL"), this.corner_TL, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TR"), this.corner_TR, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BL"), this.corner_BL, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BR"), this.corner_BR, Color.White);
		}

		public void DrawHollow()
		{
		    var spriteBatch = Game1.Instance.ScreenManager.SpriteBatch;
		    spriteBatch.FillRectangle(new Rectangle(0, 0, this.Menu.Width, 10), Color.Black);
		    spriteBatch.FillRectangle(new Rectangle(0, 0, 10, this.Menu.Height), Color.Black);
		    spriteBatch.FillRectangle(new Rectangle(0, this.Menu.Height - 10, this.Menu.Width, 10), Color.Black);
		    spriteBatch.FillRectangle(new Rectangle(this.Menu.Width - 10, 0, 10, this.Menu.Height), Color.Black);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_lower"), this.horizBot, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender"), this.topExtender, Color.White);
			foreach (Rectangle r in this.RepeatTops)
			{
			    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat"), r, Color.White);
			}
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_left"), this.vertLeft, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_right"), this.vertRight, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TL"), this.corner_TL, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TR"), this.corner_TR, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BL"), this.corner_BL, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BR"), this.corner_BR, Color.White);
		}
	}
}