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
			Menu = theMenu;
			corner_TL = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.Texture("NewUI/menu_2_corner_TL").Width, ResourceManager.Texture("NewUI/menu_2_corner_TL").Height);
			corner_TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.Texture("NewUI/menu_2_corner_TR").Width, theMenu.Y, ResourceManager.Texture("NewUI/menu_2_corner_TR").Width, ResourceManager.Texture("NewUI/menu_2_corner_TR").Height);
			corner_BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_2_corner_BL").Height, ResourceManager.Texture("NewUI/menu_2_corner_BL").Width, ResourceManager.Texture("NewUI/menu_2_corner_BL").Height);
			corner_BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.Texture("NewUI/menu_2_corner_BR").Width, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_2_corner_BR").Height, ResourceManager.Texture("NewUI/menu_2_corner_BR").Width, ResourceManager.Texture("NewUI/menu_2_corner_BR").Height);
			int topDistance = theMenu.Width - corner_TL.Width - corner_TR.Width;
			int numberRepeats = topDistance / ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Width;
			int remainder = numberRepeats * ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Width - topDistance;
			extendTopLeft = new Rectangle(corner_TL.X + corner_TL.Width, corner_TL.Y + 3, remainder / 2, ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender").Height);
			extendTopRight = new Rectangle(corner_TL.X + corner_TL.Width - remainder / 2, corner_TL.Y + 3, remainder / 2, ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender").Height);
			topExtender = new Rectangle(theMenu.X + 8, corner_TL.Y + 3, theMenu.Width - 16, ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender").Height);
			for (int i = 0; i < numberRepeats + 1; i++)
			{
				Rectangle repeat = new Rectangle(corner_TL.X + corner_TL.Width + remainder + ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Width * i, extendTopLeft.Y, ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Width, ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Height);
				RepeatTops.Add(repeat);
			}
			horizTop = new Rectangle(corner_TL.X + corner_TL.Width, corner_TL.Y + 3, theMenu.Width - corner_TL.Width - corner_TR.Width, ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender").Height);
			horizBot = new Rectangle(corner_BL.X + corner_BL.Width, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_2_horiz_lower").Height, theMenu.Width - corner_BL.Width - corner_BR.Width, ResourceManager.Texture("NewUI/menu_2_horiz_lower").Height);
			vertLeft = new Rectangle(corner_TL.X + 1, corner_TL.Y + corner_TL.Height, ResourceManager.Texture("NewUI/menu_2_vert_left").Width, theMenu.Height - corner_TL.Height - corner_BL.Height);
			vertRight = new Rectangle(theMenu.X - 1 + theMenu.Width - ResourceManager.Texture("NewUI/menu_2_vert_right").Width, corner_TR.Y + corner_TR.Height, ResourceManager.Texture("NewUI/menu_2_vert_right").Width, theMenu.Height - corner_TR.Height - corner_BR.Height);
		}

		public void Draw(Color bgcolor)
		{
            var spriteBatch = Game1.Instance.ScreenManager.SpriteBatch;
		    spriteBatch.FillRectangle(new Rectangle(Menu.X + 8, Menu.Y + 8, Menu.Width - 8, Menu.Height - 8), bgcolor);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_lower"), horizBot, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender"), topExtender, Color.White);
			foreach (Rectangle r in RepeatTops)
			{
			    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat"), r, Color.White);
			}
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_left"), vertLeft, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_right"), vertRight, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TL"), corner_TL, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TR"), corner_TR, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BL"), corner_BL, Color.White);
			spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BR"), corner_BR, Color.White);
		}

		public void Draw()
		{
		    var spriteBatch = Game1.Instance.ScreenManager.SpriteBatch;
		    spriteBatch.FillRectangle(new Rectangle(Menu.X + 8, Menu.Y + 8, Menu.Width - 8, Menu.Height - 8), new Color(0, 0, 0, 240));
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_lower"), horizBot, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender"), topExtender, Color.White);
			foreach (Rectangle r in RepeatTops)
			{
			    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat"), r, Color.White);
			}
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_left"), vertLeft, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_right"), vertRight, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TL"), corner_TL, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TR"), corner_TR, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BL"), corner_BL, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BR"), corner_BR, Color.White);
		}

		public void DrawHollow()
		{
		    var spriteBatch = Game1.Instance.ScreenManager.SpriteBatch;
		    spriteBatch.FillRectangle(new Rectangle(0, 0, Menu.Width, 10), Color.Black);
		    spriteBatch.FillRectangle(new Rectangle(0, 0, 10, Menu.Height), Color.Black);
		    spriteBatch.FillRectangle(new Rectangle(0, Menu.Height - 10, Menu.Width, 10), Color.Black);
		    spriteBatch.FillRectangle(new Rectangle(Menu.Width - 10, 0, 10, Menu.Height), Color.Black);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_lower"), horizBot, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender"), topExtender, Color.White);
			foreach (Rectangle r in RepeatTops)
			{
			    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat"), r, Color.White);
			}
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_left"), vertLeft, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_right"), vertRight, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TL"), corner_TL, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TR"), corner_TR, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BL"), corner_BL, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BR"), corner_BR, Color.White);
		}
	}
}