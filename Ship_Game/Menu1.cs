using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class Menu1
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
		public Submenu subMenu;
		private Rectangle fillRect;

		public Menu1(Rectangle theMenu)
		{
            Menu = theMenu;
            corner_TL = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.Texture("NewUI/menu_1_corner_TL").Width, ResourceManager.Texture("NewUI/menu_1_corner_TL").Height);
            corner_TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.Texture("NewUI/menu_1_corner_TR").Width, theMenu.Y, ResourceManager.Texture("NewUI/menu_1_corner_TR").Width, ResourceManager.Texture("NewUI/menu_1_corner_TR").Height);
            corner_BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_1_corner_BL").Height, ResourceManager.Texture("NewUI/menu_1_corner_BL").Width, ResourceManager.Texture("NewUI/menu_1_corner_BL").Height);
            corner_BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.Texture("NewUI/menu_1_corner_BR").Width, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_1_corner_BR").Height, ResourceManager.Texture("NewUI/menu_1_corner_BR").Width, ResourceManager.Texture("NewUI/menu_1_corner_BR").Height);
            horizTop = new Rectangle(corner_TL.X + corner_TL.Width, corner_TL.Y, theMenu.Width - corner_TL.Width - corner_TR.Width, ResourceManager.Texture("NewUI/menu_1_horiz_upper").Height);
            horizBot = new Rectangle(corner_BL.X + corner_BL.Width, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_1_horiz_lower").Height, theMenu.Width - corner_BL.Width - corner_BR.Width, ResourceManager.Texture("NewUI/menu_1_horiz_lower").Height);
            vertLeft = new Rectangle(corner_TL.X + 1, corner_TL.Y + corner_TL.Height, ResourceManager.Texture("NewUI/menu_1_vert_left").Width, theMenu.Height - corner_TL.Height - corner_BL.Height);
            vertRight = new Rectangle(theMenu.X + theMenu.Width - 1 - ResourceManager.Texture("NewUI/menu_1_vert_right").Width, corner_TR.Y + corner_TR.Height, ResourceManager.Texture("NewUI/menu_1_vert_right").Width, theMenu.Height - corner_TR.Height - corner_BR.Height);
            fillRect = new Rectangle(Menu.X + 8, Menu.Y + 8, Menu.Width - 16, Menu.Height - 16);
		}

		public Menu1(ScreenManager sm, Rectangle theMenu, bool withSub)
		{
            Menu = theMenu;
            corner_TL = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.Texture("NewUI/menu_1_corner_TL").Width, ResourceManager.Texture("NewUI/menu_1_corner_TL").Height);
            corner_TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.Texture("NewUI/menu_1_corner_TR").Width, theMenu.Y, ResourceManager.Texture("NewUI/menu_1_corner_TR").Width, ResourceManager.Texture("NewUI/menu_1_corner_TR").Height);
            corner_BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_1_corner_BL").Height, ResourceManager.Texture("NewUI/menu_1_corner_BL").Width, ResourceManager.Texture("NewUI/menu_1_corner_BL").Height);
            corner_BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.Texture("NewUI/menu_1_corner_BR").Width, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_1_corner_BR").Height, ResourceManager.Texture("NewUI/menu_1_corner_BR").Width, ResourceManager.Texture("NewUI/menu_1_corner_BR").Height);
            horizTop = new Rectangle(corner_TL.X + corner_TL.Width, corner_TL.Y, theMenu.Width - corner_TL.Width - corner_TR.Width, ResourceManager.Texture("NewUI/menu_1_horiz_upper").Height);
            horizBot = new Rectangle(corner_BL.X + corner_BL.Width, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_1_horiz_lower").Height, theMenu.Width - corner_BL.Width - corner_BR.Width, ResourceManager.Texture("NewUI/menu_1_horiz_lower").Height);
            vertLeft = new Rectangle(corner_TL.X + 1, corner_TL.Y + corner_TL.Height, ResourceManager.Texture("NewUI/menu_1_vert_left").Width, theMenu.Height - corner_TL.Height - corner_BL.Height);
            vertRight = new Rectangle(theMenu.X + theMenu.Width - 1 - ResourceManager.Texture("NewUI/menu_1_vert_right").Width, corner_TR.Y + corner_TR.Height, ResourceManager.Texture("NewUI/menu_1_vert_right").Width, theMenu.Height - corner_TR.Height - corner_BR.Height);
			Rectangle psubRect = new Rectangle(Menu.X + 20, Menu.Y - 5, Menu.Width - 40, Menu.Height - 15);
            subMenu = new Submenu(psubRect);
            fillRect = new Rectangle(Menu.X + 8, Menu.Y + 8, Menu.Width - 16, Menu.Height - 16);
		}

		public void Draw()
		{
            var spriteBatch = Game1.Instance.ScreenManager.SpriteBatch;
		    spriteBatch.FillRectangle(fillRect, new Color(0, 0, 0, 220));
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_1_corner_TL"), corner_TL, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_1_corner_TR"), corner_TR, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_1_corner_BL"), corner_BL, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_1_corner_BR"), corner_BR, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_1_horiz_lower"), horizBot, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_1_horiz_upper"), horizTop, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_1_vert_left"), vertLeft, Color.White);
		    spriteBatch.Draw(ResourceManager.Texture("NewUI/menu_1_vert_right"), vertRight, Color.White);
		    subMenu?.Draw();
		}

		public void Update(Rectangle theMenu)
		{
            corner_TL = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.Texture("NewUI/menu_1_corner_TL").Width, ResourceManager.Texture("NewUI/menu_1_corner_TL").Height);
            corner_TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.Texture("NewUI/menu_1_corner_TR").Width, theMenu.Y, ResourceManager.Texture("NewUI/menu_1_corner_TR").Width, ResourceManager.Texture("NewUI/menu_1_corner_TR").Height);
            corner_BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_1_corner_BL").Height, ResourceManager.Texture("NewUI/menu_1_corner_BL").Width, ResourceManager.Texture("NewUI/menu_1_corner_BL").Height);
            corner_BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.Texture("NewUI/menu_1_corner_BR").Width, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_1_corner_BR").Height, ResourceManager.Texture("NewUI/menu_1_corner_BR").Width, ResourceManager.Texture("NewUI/menu_1_corner_BR").Height);
            horizTop = new Rectangle(corner_TL.X + corner_TL.Width, corner_TL.Y, theMenu.Width - corner_TL.Width - corner_TR.Width, ResourceManager.Texture("NewUI/menu_1_horiz_upper").Height);
            horizBot = new Rectangle(corner_BL.X + corner_BL.Width, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_1_horiz_lower").Height, theMenu.Width - corner_BL.Width - corner_BR.Width, ResourceManager.Texture("NewUI/menu_1_horiz_lower").Height);
            vertLeft = new Rectangle(corner_TL.X + 1, corner_TL.Y + corner_TL.Height, ResourceManager.Texture("NewUI/menu_1_vert_left").Width, theMenu.Height - corner_TL.Height - corner_BL.Height);
            vertRight = new Rectangle(theMenu.X + theMenu.Width - 1 - ResourceManager.Texture("NewUI/menu_1_vert_right").Width, corner_TR.Y + corner_TR.Height, ResourceManager.Texture("NewUI/menu_1_vert_right").Width, theMenu.Height - corner_TR.Height - corner_BR.Height);
            fillRect = new Rectangle(theMenu.X + 8, theMenu.Y + 8, Menu.Width - 16, Menu.Height - 16);
			Rectangle psubRect = new Rectangle(theMenu.X + 20, theMenu.Y - 5, Menu.Width - 40, Menu.Height - 15);
            subMenu = new Submenu(psubRect);
		}
	}
}