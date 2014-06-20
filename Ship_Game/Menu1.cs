using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Menu1
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

		private Ship_Game.ScreenManager ScreenManager;

		public Submenu subMenu;

		private Rectangle fillRect;

		public Menu1(Ship_Game.ScreenManager sm, Rectangle theMenu)
		{
			this.ScreenManager = sm;
			this.Menu = theMenu;
			this.corner_TL = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.TextureDict["NewUI/menu_1_corner_TL"].Width, ResourceManager.TextureDict["NewUI/menu_1_corner_TL"].Height);
			this.corner_TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/menu_1_corner_TR"].Width, theMenu.Y, ResourceManager.TextureDict["NewUI/menu_1_corner_TR"].Width, ResourceManager.TextureDict["NewUI/menu_1_corner_TR"].Height);
			this.corner_BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/menu_1_corner_BL"].Height, ResourceManager.TextureDict["NewUI/menu_1_corner_BL"].Width, ResourceManager.TextureDict["NewUI/menu_1_corner_BL"].Height);
			this.corner_BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/menu_1_corner_BR"].Width, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/menu_1_corner_BR"].Height, ResourceManager.TextureDict["NewUI/menu_1_corner_BR"].Width, ResourceManager.TextureDict["NewUI/menu_1_corner_BR"].Height);
			this.horizTop = new Rectangle(this.corner_TL.X + this.corner_TL.Width, this.corner_TL.Y, theMenu.Width - this.corner_TL.Width - this.corner_TR.Width, ResourceManager.TextureDict["NewUI/menu_1_horiz_upper"].Height);
			this.horizBot = new Rectangle(this.corner_BL.X + this.corner_BL.Width, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/menu_1_horiz_lower"].Height, theMenu.Width - this.corner_BL.Width - this.corner_BR.Width, ResourceManager.TextureDict["NewUI/menu_1_horiz_lower"].Height);
			this.vertLeft = new Rectangle(this.corner_TL.X + 1, this.corner_TL.Y + this.corner_TL.Height, ResourceManager.TextureDict["NewUI/menu_1_vert_left"].Width, theMenu.Height - this.corner_TL.Height - this.corner_BL.Height);
			this.vertRight = new Rectangle(theMenu.X + theMenu.Width - 1 - ResourceManager.TextureDict["NewUI/menu_1_vert_right"].Width, this.corner_TR.Y + this.corner_TR.Height, ResourceManager.TextureDict["NewUI/menu_1_vert_right"].Width, theMenu.Height - this.corner_TR.Height - this.corner_BR.Height);
			this.fillRect = new Rectangle(this.Menu.X + 8, this.Menu.Y + 8, this.Menu.Width - 16, this.Menu.Height - 16);
		}

		public Menu1(Ship_Game.ScreenManager sm, Rectangle theMenu, bool withSub)
		{
			this.ScreenManager = sm;
			this.Menu = theMenu;
			this.corner_TL = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.TextureDict["NewUI/menu_1_corner_TL"].Width, ResourceManager.TextureDict["NewUI/menu_1_corner_TL"].Height);
			this.corner_TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/menu_1_corner_TR"].Width, theMenu.Y, ResourceManager.TextureDict["NewUI/menu_1_corner_TR"].Width, ResourceManager.TextureDict["NewUI/menu_1_corner_TR"].Height);
			this.corner_BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/menu_1_corner_BL"].Height, ResourceManager.TextureDict["NewUI/menu_1_corner_BL"].Width, ResourceManager.TextureDict["NewUI/menu_1_corner_BL"].Height);
			this.corner_BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/menu_1_corner_BR"].Width, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/menu_1_corner_BR"].Height, ResourceManager.TextureDict["NewUI/menu_1_corner_BR"].Width, ResourceManager.TextureDict["NewUI/menu_1_corner_BR"].Height);
			this.horizTop = new Rectangle(this.corner_TL.X + this.corner_TL.Width, this.corner_TL.Y, theMenu.Width - this.corner_TL.Width - this.corner_TR.Width, ResourceManager.TextureDict["NewUI/menu_1_horiz_upper"].Height);
			this.horizBot = new Rectangle(this.corner_BL.X + this.corner_BL.Width, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/menu_1_horiz_lower"].Height, theMenu.Width - this.corner_BL.Width - this.corner_BR.Width, ResourceManager.TextureDict["NewUI/menu_1_horiz_lower"].Height);
			this.vertLeft = new Rectangle(this.corner_TL.X + 1, this.corner_TL.Y + this.corner_TL.Height, ResourceManager.TextureDict["NewUI/menu_1_vert_left"].Width, theMenu.Height - this.corner_TL.Height - this.corner_BL.Height);
			this.vertRight = new Rectangle(theMenu.X + theMenu.Width - 1 - ResourceManager.TextureDict["NewUI/menu_1_vert_right"].Width, this.corner_TR.Y + this.corner_TR.Height, ResourceManager.TextureDict["NewUI/menu_1_vert_right"].Width, theMenu.Height - this.corner_TR.Height - this.corner_BR.Height);
			Rectangle psubRect = new Rectangle(this.Menu.X + 20, this.Menu.Y - 5, this.Menu.Width - 40, this.Menu.Height - 15);
			this.subMenu = new Submenu(this.ScreenManager, psubRect);
			this.fillRect = new Rectangle(this.Menu.X + 8, this.Menu.Y + 8, this.Menu.Width - 16, this.Menu.Height - 16);
		}

		public void Draw()
		{
			Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, this.fillRect, new Color(0, 0, 0, 220));
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/menu_1_corner_TL"], this.corner_TL, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/menu_1_corner_TR"], this.corner_TR, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/menu_1_corner_BL"], this.corner_BL, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/menu_1_corner_BR"], this.corner_BR, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/menu_1_horiz_lower"], this.horizBot, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/menu_1_horiz_upper"], this.horizTop, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/menu_1_vert_left"], this.vertLeft, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/menu_1_vert_right"], this.vertRight, Color.White);
			if (this.subMenu != null)
			{
				this.subMenu.Draw();
			}
		}

		public void Update(Rectangle theMenu)
		{
			this.corner_TL = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.TextureDict["NewUI/menu_1_corner_TL"].Width, ResourceManager.TextureDict["NewUI/menu_1_corner_TL"].Height);
			this.corner_TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/menu_1_corner_TR"].Width, theMenu.Y, ResourceManager.TextureDict["NewUI/menu_1_corner_TR"].Width, ResourceManager.TextureDict["NewUI/menu_1_corner_TR"].Height);
			this.corner_BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/menu_1_corner_BL"].Height, ResourceManager.TextureDict["NewUI/menu_1_corner_BL"].Width, ResourceManager.TextureDict["NewUI/menu_1_corner_BL"].Height);
			this.corner_BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/menu_1_corner_BR"].Width, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/menu_1_corner_BR"].Height, ResourceManager.TextureDict["NewUI/menu_1_corner_BR"].Width, ResourceManager.TextureDict["NewUI/menu_1_corner_BR"].Height);
			this.horizTop = new Rectangle(this.corner_TL.X + this.corner_TL.Width, this.corner_TL.Y, theMenu.Width - this.corner_TL.Width - this.corner_TR.Width, ResourceManager.TextureDict["NewUI/menu_1_horiz_upper"].Height);
			this.horizBot = new Rectangle(this.corner_BL.X + this.corner_BL.Width, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/menu_1_horiz_lower"].Height, theMenu.Width - this.corner_BL.Width - this.corner_BR.Width, ResourceManager.TextureDict["NewUI/menu_1_horiz_lower"].Height);
			this.vertLeft = new Rectangle(this.corner_TL.X + 1, this.corner_TL.Y + this.corner_TL.Height, ResourceManager.TextureDict["NewUI/menu_1_vert_left"].Width, theMenu.Height - this.corner_TL.Height - this.corner_BL.Height);
			this.vertRight = new Rectangle(theMenu.X + theMenu.Width - 1 - ResourceManager.TextureDict["NewUI/menu_1_vert_right"].Width, this.corner_TR.Y + this.corner_TR.Height, ResourceManager.TextureDict["NewUI/menu_1_vert_right"].Width, theMenu.Height - this.corner_TR.Height - this.corner_BR.Height);
			this.fillRect = new Rectangle(theMenu.X + 8, theMenu.Y + 8, this.Menu.Width - 16, this.Menu.Height - 16);
			Rectangle psubRect = new Rectangle(theMenu.X + 20, theMenu.Y - 5, this.Menu.Width - 40, this.Menu.Height - 15);
			this.subMenu = new Submenu(this.ScreenManager, psubRect);
		}
	}
}