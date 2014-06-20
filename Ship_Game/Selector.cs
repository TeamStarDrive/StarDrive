using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Selector
	{
		public Rectangle Menu;

		private Ship_Game.ScreenManager ScreenManager;

		private Rectangle UpperLeft;

		private Rectangle TR;

		private Rectangle topHoriz;

		private Rectangle botHoriz;

		private Rectangle BL;

		private Rectangle BR;

		private Rectangle VL;

		private Rectangle VR;

		private Rectangle TL;

		private Color fill;

		public Selector(Ship_Game.ScreenManager sm, Rectangle theMenu)
		{
			this.ScreenManager = sm;
			theMenu.X = theMenu.X - 15;
			theMenu.Y = theMenu.Y - 5;
			theMenu.Width = theMenu.Width + 12;
			this.Menu = theMenu;
			this.UpperLeft = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.TextureDict["NewUI/submenu_header_left"].Width, ResourceManager.TextureDict["NewUI/submenu_header_left"].Height);
			this.TL = new Rectangle(theMenu.X, theMenu.Y - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Height);
			this.TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, theMenu.Y - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Height);
			this.BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height + 2, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height);
			this.BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, theMenu.Y + theMenu.Height + 2 - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height);
			this.topHoriz = new Rectangle(theMenu.X + ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, theMenu.Y - 2, theMenu.Width - this.TR.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, 2);
			this.botHoriz = new Rectangle(theMenu.X + this.BL.Width, theMenu.Y + theMenu.Height, theMenu.Width - this.BL.Width - this.BR.Width, 2);
			this.VL = new Rectangle(theMenu.X, theMenu.Y + this.TR.Height - 2, 2, theMenu.Height - this.BL.Height - 2);
			this.VR = new Rectangle(theMenu.X + theMenu.Width - 2, theMenu.Y + this.TR.Height - 2, 2, theMenu.Height - this.BR.Height - 2);
		}

		public Selector(Ship_Game.ScreenManager sm, Rectangle theMenu, bool UseRealRect)
		{
			this.ScreenManager = sm;
			this.Menu = theMenu;
			this.UpperLeft = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.TextureDict["NewUI/submenu_header_left"].Width, ResourceManager.TextureDict["NewUI/submenu_header_left"].Height);
			this.TL = new Rectangle(theMenu.X, theMenu.Y - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Height);
			this.TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, theMenu.Y - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Height);
			this.BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height + 2, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height);
			this.BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, theMenu.Y + theMenu.Height + 2 - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height);
			this.topHoriz = new Rectangle(theMenu.X + ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, theMenu.Y - 2, theMenu.Width - this.TR.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, 2);
			this.botHoriz = new Rectangle(theMenu.X + this.BL.Width, theMenu.Y + theMenu.Height, theMenu.Width - this.BL.Width - this.BR.Width, 2);
			this.VL = new Rectangle(theMenu.X, theMenu.Y + this.TR.Height - 2, 2, theMenu.Height - this.BL.Height - 2);
			this.VR = new Rectangle(theMenu.X + theMenu.Width - 2, theMenu.Y + this.TR.Height - 2, 2, theMenu.Height - this.BR.Height - 2);
		}

		public Selector(Ship_Game.ScreenManager sm, Rectangle theMenu, Color fillColor)
		{
			this.fill = fillColor;
			this.ScreenManager = sm;
			this.Menu = theMenu;
			this.UpperLeft = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.TextureDict["NewUI/submenu_header_left"].Width, ResourceManager.TextureDict["NewUI/submenu_header_left"].Height);
			this.TL = new Rectangle(theMenu.X, theMenu.Y - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Height);
			this.TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, theMenu.Y - 2, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_TR"].Height);
			this.BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height + 2, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BL"].Height);
			this.BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, theMenu.Y + theMenu.Height + 2 - ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Width, ResourceManager.TextureDict["NewUI/submenu_corner_BR"].Height);
			this.topHoriz = new Rectangle(theMenu.X + ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, theMenu.Y - 2, theMenu.Width - this.TR.Width - ResourceManager.TextureDict["NewUI/submenu_corner_TL"].Width, 2);
			this.botHoriz = new Rectangle(theMenu.X + this.BL.Width, theMenu.Y + theMenu.Height, theMenu.Width - this.BL.Width - this.BR.Width, 2);
			this.VL = new Rectangle(theMenu.X, theMenu.Y + this.TR.Height - 2, 2, theMenu.Height - this.BL.Height - 2);
			this.VR = new Rectangle(theMenu.X + theMenu.Width - 2, theMenu.Y + this.TR.Height - 2, 2, theMenu.Height - this.BR.Height - 2);
		}

		public void Draw()
		{
			Rectangle upperleft = new Rectangle(this.Menu.X, this.Menu.Y, 24, 24);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/rounded_upperLeft"], upperleft, this.fill);
			Rectangle upperRight = new Rectangle(this.Menu.X + this.Menu.Width - 24, this.Menu.Y, 24, 24);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/rounded_upperRight"], upperRight, this.fill);
			Rectangle lowerLeft = new Rectangle(this.Menu.X, this.Menu.Y + this.Menu.Height - 24, 24, 24);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/rounded_lowerLeft"], lowerLeft, this.fill);
			Rectangle lowerRight = new Rectangle(this.Menu.X + this.Menu.Width - 24, this.Menu.Y + this.Menu.Height - 24, 24, 24);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/rounded_lowerRight"], lowerRight, this.fill);
			Rectangle top = new Rectangle(this.Menu.X + 24, this.Menu.Y, this.Menu.Width - 48, 24);
			Rectangle bottom = new Rectangle(this.Menu.X + 24, this.Menu.Y + this.Menu.Height - 24, this.Menu.Width - 48, 24);
			Rectangle right = new Rectangle(this.Menu.X + this.Menu.Width - 24, this.Menu.Y + 24, 24, this.Menu.Height - 48);
			Rectangle left = new Rectangle(this.Menu.X, this.Menu.Y + 24, 24, this.Menu.Height - 48);
			Rectangle middle = new Rectangle(this.Menu.X + 24, this.Menu.Y + 24, this.Menu.Width - 48, this.Menu.Height - 48);
			Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, top, this.fill);
			Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, bottom, this.fill);
			Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, right, this.fill);
			Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, left, this.fill);
			Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, middle, this.fill);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_corner_TL"], this.TL, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_horiz_vert"], this.topHoriz, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_corner_TR"], this.TR, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_horiz_vert"], this.botHoriz, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_corner_BR"], this.BR, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_corner_BL"], this.BL, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_horiz_vert"], this.VR, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/submenu_horiz_vert"], this.VL, Color.White);
		}
	}
}