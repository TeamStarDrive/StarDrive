using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class DropOptions
	{
		private DropOptions.RecTexPair TL;

		private DropOptions.RecTexPair TR;

		private DropOptions.RecTexPair BL;

		private DropOptions.RecTexPair BR;

		private DropOptions.RecTexPair LV;

		private DropOptions.RecTexPair RV;

		private DropOptions.RecTexPair Top;

		private DropOptions.RecTexPair Bot;

		private List<DropOptions.RecTexPair> container = new List<DropOptions.RecTexPair>();

		public Rectangle r;

		private Rectangle OpenRect = new Rectangle();

		private Rectangle ClickAbleOpenRect = new Rectangle();

		public int ActiveIndex;

		public List<Entry> Options = new List<Entry>();

		public bool Open;

		public Entry Active
		{
			get
			{
				return this.Options[this.ActiveIndex];
			}
		}

		public DropOptions(Rectangle r)
		{
			this.r = r;
			this.TL = new DropOptions.RecTexPair(r.X, r.Y, "NewUI/dropdown_menu_corner_TL");
			this.TR = new DropOptions.RecTexPair(r.X + r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_TR"].Width, r.Y, "NewUI/dropdown_menu_corner_TR");
			this.BL = new DropOptions.RecTexPair(r.X, r.Y + r.Height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Height, "NewUI/dropdown_menu_corner_BL");
			this.BR = new DropOptions.RecTexPair(r.X + r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Width, r.Y + r.Height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BR"].Height, "NewUI/dropdown_menu_corner_BR");
			this.LV = new DropOptions.RecTexPair(r.X, r.Y + 6, r.Height - 12, "NewUI/dropdown_menu_sides_left");
			this.RV = new DropOptions.RecTexPair(r.X + r.Width - 6, r.Y + 6, r.Height - 12, "NewUI/dropdown_menu_sides_right");
			this.Top = new DropOptions.RecTexPair(r.X + this.TL.r.Width, r.Y, "NewUI/dropdown_menu_sides_top", r.Width - this.TL.r.Width - this.TR.r.Width);
			this.Bot = new DropOptions.RecTexPair(r.X + this.TL.r.Width, r.Y + r.Height - 6, "NewUI/dropdown_menu_sides_bottom", r.Width - this.BL.r.Width - this.BR.r.Width);
			this.container.Add(this.TL);
			this.container.Add(this.TR);
			this.container.Add(this.BL);
			this.container.Add(this.BR);
			this.container.Add(this.LV);
			this.container.Add(this.RV);
			this.container.Add(this.Top);
			this.container.Add(this.Bot);
		}

		public void AddOption(string option, int value)
		{
			Entry e = new Entry()
			{
				Name = option,
				Hover = false,
				clickRect = new Rectangle(this.r.X, this.r.Y + this.r.Height * this.Options.Count + 3, this.r.Width, 18),
				@value = value
			};
			this.Options.Add(e);
		}

		public void AddOption(string option, object value)
		{
			Entry e = new Entry()
			{
				Name = option,
				Hover = false,
				clickRect = new Rectangle(this.r.X, this.r.Y + this.r.Height * this.Options.Count + 3, this.r.Width, 18),
				ReferencedObject = value
			};
			this.Options.Add(e);
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
			foreach (DropOptions.RecTexPair r in this.container)
			{
				spriteBatch.Draw(ResourceManager.TextureDict[r.tex], r.r, Color.White);
			}
			if (!hover)
			{
				string txt = this.Options[this.ActiveIndex].Name;
				bool addDots = false;
				while (Fonts.Arial12Bold.MeasureString(txt).X > (float)(this.r.Width - 22))
				{
					txt = txt.Remove(txt.Length - 1);
					addDots = true;
				}
				if (addDots)
				{
					txt = string.Concat(txt, "...");
				}
				spriteBatch.DrawString(Fonts.Arial12Bold, txt, new Vector2((float)(this.r.X + 10), (float)(this.r.Y + this.r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), new Color(255, 239, 208));
			}
			else
			{
				string txt = this.Options[this.ActiveIndex].Name;
				bool addDots = false;
				while (Fonts.Arial12Bold.MeasureString(txt).X > (float)(this.r.Width - 22))
				{
					txt = txt.Remove(txt.Length - 1);
					addDots = true;
				}
				if (addDots)
				{
					txt = string.Concat(txt, "...");
				}
				spriteBatch.DrawString(Fonts.Arial12Bold, txt, new Vector2((float)(this.r.X + 10), (float)(this.r.Y + this.r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), Color.White);
			}
			if (this.Open)
			{
				Primitives2D.FillRectangle(spriteBatch, this.OpenRect, new Color(22, 22, 23));
				int i = 1;
				foreach (Entry e in this.Options)
				{
					if (e.Name == this.Options[this.ActiveIndex].Name)
					{
						continue;
					}
					Rectangle rectangle = new Rectangle(this.r.X, this.r.Y + this.r.Height * i + 3, this.r.Width, 18);
					Rectangle rectangle1 = rectangle;
					e.clickRect = rectangle;
					e.clickRect = rectangle1;
					if (HelperFunctions.CheckIntersection(e.clickRect, MousePos))
					{
						Rectangle HoverLeft = new Rectangle(e.clickRect.X + 5, e.clickRect.Y + 1, 6, 15);
						Rectangle HoverMiddle = new Rectangle(e.clickRect.X + 11, e.clickRect.Y + 1, e.clickRect.Width - 22, 15);
						Rectangle HoverRight = new Rectangle(HoverMiddle.X + HoverMiddle.Width, HoverMiddle.Y, 6, 15);
						spriteBatch.Draw(ResourceManager.TextureDict["NewUI/dropdown_menuitem_hover_left"], HoverLeft, Color.White);
						spriteBatch.Draw(ResourceManager.TextureDict["NewUI/dropdown_menuitem_hover_middle"], HoverMiddle, Color.White);
						spriteBatch.Draw(ResourceManager.TextureDict["NewUI/dropdown_menuitem_hover_right"], HoverRight, Color.White);
					}
					string txt = e.Name;
					bool addDots = false;
					while (Fonts.Arial12Bold.MeasureString(txt).X > (float)(this.r.Width - 22))
					{
						txt = txt.Remove(txt.Length - 1);
						addDots = true;
					}
					if (addDots)
					{
						txt = string.Concat(txt, "...");
					}
					spriteBatch.DrawString(Fonts.Arial12Bold, txt, new Vector2((float)(this.r.X + 10), (float)(e.clickRect.Y + e.clickRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), Color.White);
					i++;
				}
			}
		}

		public void DrawGrayed(SpriteBatch spriteBatch)
		{
			foreach (DropOptions.RecTexPair r in this.container)
			{
				spriteBatch.Draw(ResourceManager.TextureDict[r.tex], r.r, Color.DarkGray);
			}
			spriteBatch.DrawString(Fonts.Arial12Bold, "-", new Vector2((float)(this.r.X + 10), (float)(this.r.Y + this.r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), Color.DarkGray);
		}

		public void HandleInput(InputState input)
		{
			if (HelperFunctions.CheckIntersection(this.r, input.CursorPosition))
			{
				if (input.InGameSelect)
				{
					this.Open = !this.Open;
					if (this.Open && this.Options.Count == 1)
					{
						this.Open = false;
					}
					if (this.Open)
					{
						AudioManager.PlayCue("sd_ui_accept_alt3");
					}
					this.Reset();
					return;
				}
			}
			else if (HelperFunctions.CheckIntersection(this.ClickAbleOpenRect, input.CursorPosition))
			{
				if (this.Open)
				{
					foreach (Entry e in this.Options)
					{
						if (!HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition) || !input.InGameSelect)
						{
							continue;
						}
						this.Options[this.ActiveIndex].clickRect = e.clickRect;
						e.clickRect = new Rectangle();
						this.ActiveIndex = this.Options.IndexOf(e);
						AudioManager.PlayCue("sd_ui_accept_alt3");
						this.Open = false;
						this.Reset();
						return;
					}
				}
			}
			else if (input.InGameSelect)
			{
				this.Open = false;
				this.Reset();
			}
		}

		public void Reset()
		{
			this.container.Clear();
			this.TL = new DropOptions.RecTexPair(this.r.X, this.r.Y, "NewUI/dropdown_menu_corner_TL");
			this.TR = new DropOptions.RecTexPair(this.r.X + this.r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_TR"].Width, this.r.Y, "NewUI/dropdown_menu_corner_TR");
			this.BL = new DropOptions.RecTexPair(this.r.X, this.r.Y + this.r.Height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Height, "NewUI/dropdown_menu_corner_BL");
			this.BR = new DropOptions.RecTexPair(this.r.X + this.r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Width, this.r.Y + this.r.Height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BR"].Height, "NewUI/dropdown_menu_corner_BR");
			this.LV = new DropOptions.RecTexPair(this.r.X, this.r.Y + 6, this.r.Height - 12, "NewUI/dropdown_menu_sides_left");
			this.RV = new DropOptions.RecTexPair(this.r.X + this.r.Width - 6, this.r.Y + 6, this.r.Height - 12, "NewUI/dropdown_menu_sides_right");
			this.Top = new DropOptions.RecTexPair(this.r.X + this.TL.r.Width, this.r.Y, "NewUI/dropdown_menu_sides_top", this.r.Width - this.TL.r.Width - this.TR.r.Width);
			this.Bot = new DropOptions.RecTexPair(this.r.X + this.TL.r.Width, this.r.Y + this.r.Height - 6, "NewUI/dropdown_menu_sides_bottom", this.r.Width - this.BL.r.Width - this.BR.r.Width);
			this.container.Add(this.TL);
			this.container.Add(this.TR);
			this.container.Add(this.BL);
			this.container.Add(this.BR);
			this.container.Add(this.LV);
			this.container.Add(this.RV);
			this.container.Add(this.Top);
			this.container.Add(this.Bot);
			if (this.Open)
			{
				int Height = (this.Options.Count - 1) * 18;
				this.OpenRect = new Rectangle(this.r.X + 6, this.r.Y + this.r.Height + 3 + 6, this.r.Width - 12, Height - 12);
				this.ClickAbleOpenRect = new Rectangle(this.r.X + 6, this.r.Y + this.r.Height + 3, this.r.Width - 12, Height - 6);
				this.TL = new DropOptions.RecTexPair(this.r.X, this.r.Y + this.r.Height + 3, "NewUI/dropdown_menu_corner_TL");
				this.TR = new DropOptions.RecTexPair(this.r.X + this.r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_TR"].Width, this.TL.r.Y, "NewUI/dropdown_menu_corner_TR");
				this.BL = new DropOptions.RecTexPair(this.r.X, this.TL.r.Y + Height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Height, "NewUI/dropdown_menu_corner_BL");
				this.BR = new DropOptions.RecTexPair(this.r.X + this.r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Width, this.TL.r.Y + Height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BR"].Height, "NewUI/dropdown_menu_corner_BR");
				this.LV = new DropOptions.RecTexPair(this.r.X, this.TL.r.Y + 6, Height - 12, "NewUI/dropdown_menu_sides_left");
				this.RV = new DropOptions.RecTexPair(this.r.X + this.r.Width - 6, this.TL.r.Y + 6, Height - 12, "NewUI/dropdown_menu_sides_right");
				this.Top = new DropOptions.RecTexPair(this.r.X + this.TL.r.Width, this.TL.r.Y, "NewUI/dropdown_menu_sides_top", this.r.Width - this.TL.r.Width - this.TR.r.Width);
				this.Bot = new DropOptions.RecTexPair(this.r.X + this.TL.r.Width, this.TL.r.Y + Height - 6, "NewUI/dropdown_menu_sides_bottom", this.r.Width - this.BL.r.Width - this.BR.r.Width);
				this.container.Add(this.TL);
				this.container.Add(this.TR);
				this.container.Add(this.BL);
				this.container.Add(this.BR);
				this.container.Add(this.LV);
				this.container.Add(this.RV);
				this.container.Add(this.Top);
				this.container.Add(this.Bot);
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