using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class DropDownMenu
	{
		private DropDownMenu.RecTexPair TL;

		private DropDownMenu.RecTexPair TR;

		private DropDownMenu.RecTexPair BL;

		private DropDownMenu.RecTexPair BR;

		private DropDownMenu.RecTexPair LV;

		private DropDownMenu.RecTexPair RV;

		private DropDownMenu.RecTexPair Top;

		private DropDownMenu.RecTexPair Bot;

		private List<DropDownMenu.RecTexPair> container = new List<DropDownMenu.RecTexPair>();

		public Rectangle r;

		public int ActiveIndex;

		private List<string> Options = new List<string>();

		public DropDownMenu(Rectangle r)
		{
			this.r = r;
			this.TL = new DropDownMenu.RecTexPair(r.X, r.Y, "NewUI/dropdown_menu_corner_TL");
			this.TR = new DropDownMenu.RecTexPair(r.X + r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_TR"].Width, r.Y, "NewUI/dropdown_menu_corner_TR");
			this.BL = new DropDownMenu.RecTexPair(r.X, r.Y + r.Height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Height, "NewUI/dropdown_menu_corner_BL");
			this.BR = new DropDownMenu.RecTexPair(r.X + r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Width, r.Y + r.Height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BR"].Height, "NewUI/dropdown_menu_corner_BR");
			this.LV = new DropDownMenu.RecTexPair(r.X, r.Y + 6, r.Height - 12, "NewUI/dropdown_menu_sides_left");
			this.RV = new DropDownMenu.RecTexPair(r.X + r.Width - 6, r.Y + 6, r.Height - 12, "NewUI/dropdown_menu_sides_right");
			this.Top = new DropDownMenu.RecTexPair(r.X + this.TL.r.Width, r.Y, "NewUI/dropdown_menu_sides_top", r.Width - this.TL.r.Width - this.TR.r.Width);
			this.Bot = new DropDownMenu.RecTexPair(r.X + this.TL.r.Width, r.Y + r.Height - 6, "NewUI/dropdown_menu_sides_bottom", r.Width - this.BL.r.Width - this.BR.r.Width);
			this.container.Add(this.TL);
			this.container.Add(this.TR);
			this.container.Add(this.BL);
			this.container.Add(this.BR);
			this.container.Add(this.LV);
			this.container.Add(this.RV);
			this.container.Add(this.Top);
			this.container.Add(this.Bot);
		}

		public void AddOption(string option)
		{
			this.Options.Add(option);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			bool hover = false;
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			if (HelperFunctions.CheckIntersection(this.r, MousePos))
			{
				hover = true;
			}
			if (hover)
			{
				Primitives2D.FillRectangle(spriteBatch, this.r, new Color(128, 87, 43, 50));
			}
			foreach (DropDownMenu.RecTexPair r in this.container)
			{
				spriteBatch.Draw(ResourceManager.TextureDict[r.tex], r.r, Color.White);
			}
			if (hover)
			{
				spriteBatch.DrawString(Fonts.Arial12Bold, this.Options[this.ActiveIndex], new Vector2((float)(this.r.X + 8), (float)(this.r.Y + this.r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), Color.White);
				return;
			}
			spriteBatch.DrawString(Fonts.Arial12Bold, this.Options[this.ActiveIndex], new Vector2((float)(this.r.X + 8), (float)(this.r.Y + this.r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), new Color(255, 239, 208));
		}

		public void DrawGrayed(SpriteBatch spriteBatch)
		{
			foreach (DropDownMenu.RecTexPair r in this.container)
			{
				spriteBatch.Draw(ResourceManager.TextureDict[r.tex], r.r, Color.DarkGray);
			}
			spriteBatch.DrawString(Fonts.Arial12Bold, "-", new Vector2((float)(this.r.X + 8), (float)(this.r.Y + this.r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), Color.DarkGray);
		}

		public void Toggle()
		{
			DropDownMenu activeIndex = this;
			activeIndex.ActiveIndex = activeIndex.ActiveIndex + 1;
			if (this.ActiveIndex > this.Options.Count - 1)
			{
				this.ActiveIndex = 0;
			}
		}

		private class RecTexPair
		{
			public Rectangle r;

			public string tex;

			public RecTexPair(int x, int y, string t)
			{
				this.r = new Rectangle(x, y, ResourceManager.TextureDict[t].Width, ResourceManager.TextureDict[t].Height);
				this.tex = t;
			}

			public RecTexPair(int x, int y, int h, string t)
			{
				this.r = new Rectangle(x, y, ResourceManager.TextureDict[t].Width, h);
				this.tex = t;
			}

			public RecTexPair(int x, int y, string t, int w)
			{
				this.r = new Rectangle(x, y, w, ResourceManager.TextureDict[t].Height);
				this.tex = t;
			}
		}
	}
}