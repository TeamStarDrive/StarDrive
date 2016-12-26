using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
    public sealed class Entry
    {
        public string Name;
        public bool Hover;
        public Rectangle clickRect;
        public int @value;
        public object ReferencedObject;
    }

    public sealed class DropOptions
	{
		private RecTexPair TL;
		private RecTexPair TR;
		private RecTexPair BL;
		private RecTexPair BR;
		private RecTexPair LV;
		private RecTexPair RV;
		private RecTexPair Top;
		private RecTexPair Bot;
		private readonly Array<RecTexPair> container = new Array<RecTexPair>();

		public Rectangle r;
		private Rectangle OpenRect;
		private Rectangle ClickAbleOpenRect;
		public int ActiveIndex;
		public Array<Entry> Options = new Array<Entry>();
		public bool Open;
		public Entry Active => Options[ActiveIndex];

	    public DropOptions(Rectangle r)
		{
			this.r = r;
			TL = new RecTexPair(r.X, r.Y, "NewUI/dropdown_menu_corner_TL");
			TR = new RecTexPair(r.X + r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_TR"].Width, r.Y, "NewUI/dropdown_menu_corner_TR");
			BL = new RecTexPair(r.X, r.Y + r.Height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Height, "NewUI/dropdown_menu_corner_BL");
			BR = new RecTexPair(r.X + r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Width, r.Y + r.Height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BR"].Height, "NewUI/dropdown_menu_corner_BR");
			LV = new RecTexPair(r.X, r.Y + 6, r.Height - 12, "NewUI/dropdown_menu_sides_left");
			RV = new RecTexPair(r.X + r.Width - 6, r.Y + 6, r.Height - 12, "NewUI/dropdown_menu_sides_right");
			Top = new RecTexPair(r.X + TL.r.Width, r.Y, "NewUI/dropdown_menu_sides_top", r.Width - TL.r.Width - TR.r.Width);
			Bot = new RecTexPair(r.X + TL.r.Width, r.Y + r.Height - 6, "NewUI/dropdown_menu_sides_bottom", r.Width - BL.r.Width - BR.r.Width);
			container.Add(TL);
			container.Add(TR);
			container.Add(BL);
			container.Add(BR);
			container.Add(LV);
			container.Add(RV);
			container.Add(Top);
			container.Add(Bot);
		}

		public void AddOption(string option, int value)
		{
			Entry e = new Entry()
			{
				Name = option,
				Hover = false,
				clickRect = new Rectangle(r.X, r.Y + r.Height * Options.Count + 3, r.Width, 18),
				@value = value
			};
            Options.Add(e);
		}

		public void AddOption(string option, object value)
		{
			Entry e = new Entry()
			{
				Name = option,
				Hover = false,
				clickRect = new Rectangle(r.X, r.Y + r.Height * Options.Count + 3, r.Width, 18),
				ReferencedObject = value
			};
            Options.Add(e);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			bool hover = false;
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			if (HelperFunctions.CheckIntersection(r, MousePos))
			{
				hover = true;
			}
			if (hover)
			{
				Primitives2D.FillRectangle(spriteBatch, r, new Color(128, 87, 43, 50));
			}
			foreach (DropOptions.RecTexPair r in container)
			{
				spriteBatch.Draw(ResourceManager.TextureDict[r.tex], r.r, Color.White);
			}
            if (!hover && Options.Count > 0)
			{
				
                string txt = Options[ActiveIndex].Name;
				bool addDots = false;
				while (Fonts.Arial12Bold.MeasureString(txt).X > (float)(r.Width - 22))
				{
					txt = txt.Remove(txt.Length - 1);
					addDots = true;
				}
				if (addDots)
				{
					txt = string.Concat(txt, "...");
				}
				spriteBatch.DrawString(Fonts.Arial12Bold, txt, new Vector2((float)(r.X + 10), (float)(r.Y + r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), new Color(255, 239, 208));
			}
			else if(Options.Count >0)
			{
				string txt = Options[ActiveIndex].Name;
				bool addDots = false;
				while (Fonts.Arial12Bold.MeasureString(txt).X > (float)(r.Width - 22))
				{
					txt = txt.Remove(txt.Length - 1);
					addDots = true;
				}
				if (addDots)
				{
					txt = string.Concat(txt, "...");
				}
				spriteBatch.DrawString(Fonts.Arial12Bold, txt, new Vector2((float)(r.X + 10), (float)(r.Y + r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), Color.White);
			}
			if (Open)
			{
				Primitives2D.FillRectangle(spriteBatch, OpenRect, new Color(22, 22, 23));
				int i = 1;
				foreach (Entry e in Options)
				{
					if (e.Name == Options[ActiveIndex].Name)
					{
						continue;
					}
					Rectangle rectangle = new Rectangle(r.X, r.Y + r.Height * i + 3, r.Width, 18);
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
					while (Fonts.Arial12Bold.MeasureString(txt).X > (float)(r.Width - 22))
					{
						txt = txt.Remove(txt.Length - 1);
						addDots = true;
					}
					if (addDots)
					{
						txt = string.Concat(txt, "...");
					}
					spriteBatch.DrawString(Fonts.Arial12Bold, txt, new Vector2((float)(r.X + 10), (float)(e.clickRect.Y + e.clickRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), Color.White);
					i++;
				}
			}
		}

		public void DrawGrayed(SpriteBatch spriteBatch)
		{
			foreach (DropOptions.RecTexPair r in container)
			{
				spriteBatch.Draw(ResourceManager.TextureDict[r.tex], r.r, Color.DarkGray);
			}
			spriteBatch.DrawString(Fonts.Arial12Bold, "-", new Vector2((float)(r.X + 10), (float)(r.Y + r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), Color.DarkGray);
		}

		public void HandleInput(InputState input)
		{
			if (HelperFunctions.CheckIntersection(r, input.CursorPosition))
			{
				if (input.InGameSelect)
				{
                    Open = !Open;
					if (Open && Options.Count == 1)
					{
                        Open = false;
					}
					if (Open)
					{
						AudioManager.PlayCue("sd_ui_accept_alt3");
					}
                    Reset();
					return;
				}
			}
			else if (HelperFunctions.CheckIntersection(ClickAbleOpenRect, input.CursorPosition))
			{
				if (Open)
				{
					foreach (Entry e in Options)
					{
						if (!HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition) || !input.InGameSelect)
						{
							continue;
						}
                        Options[ActiveIndex].clickRect = e.clickRect;
						e.clickRect = new Rectangle();
                        ActiveIndex = Options.IndexOf(e);
						AudioManager.PlayCue("sd_ui_accept_alt3");
                        Open = false;
                        Reset();
						return;
					}
				}
			}
			else if (input.InGameSelect)
			{
                Open = false;
                Reset();
			}
		}

		public void Reset()
		{
            container.Clear();
            TL = new RecTexPair(r.X, r.Y, "NewUI/dropdown_menu_corner_TL");
            TR = new RecTexPair(r.X + r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_TR"].Width, r.Y, "NewUI/dropdown_menu_corner_TR");
            BL = new RecTexPair(r.X, r.Y + r.Height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Height, "NewUI/dropdown_menu_corner_BL");
            BR = new RecTexPair(r.X + r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Width, r.Y + r.Height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BR"].Height, "NewUI/dropdown_menu_corner_BR");
            LV = new RecTexPair(r.X, r.Y + 6, r.Height - 12, "NewUI/dropdown_menu_sides_left");
            RV = new RecTexPair(r.X + r.Width - 6, r.Y + 6, r.Height - 12, "NewUI/dropdown_menu_sides_right");
            Top = new RecTexPair(r.X + TL.r.Width, r.Y, "NewUI/dropdown_menu_sides_top", r.Width - TL.r.Width - TR.r.Width);
            Bot = new RecTexPair(r.X + TL.r.Width, r.Y + r.Height - 6, "NewUI/dropdown_menu_sides_bottom", r.Width - BL.r.Width - BR.r.Width);
            container.Add(TL);
            container.Add(TR);
            container.Add(BL);
            container.Add(BR);
            container.Add(LV);
            container.Add(RV);
            container.Add(Top);
            container.Add(Bot);
			if (Open)
			{
				int height = (Options.Count - 1) * 18;
                OpenRect = new Rectangle(r.X + 6, r.Y + r.Height + 3 + 6, r.Width - 12, height - 12);
                ClickAbleOpenRect = new Rectangle(r.X + 6, r.Y + r.Height + 3, r.Width - 12, height - 6);
                TL = new RecTexPair(r.X, r.Y + r.Height + 3, "NewUI/dropdown_menu_corner_TL");
                TR = new RecTexPair(r.X + r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_TR"].Width, TL.r.Y, "NewUI/dropdown_menu_corner_TR");
                BL = new RecTexPair(r.X, TL.r.Y + height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Height, "NewUI/dropdown_menu_corner_BL");
                BR = new RecTexPair(r.X + r.Width - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"].Width, TL.r.Y + height - ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BR"].Height, "NewUI/dropdown_menu_corner_BR");
                LV = new RecTexPair(r.X, TL.r.Y + 6, height - 12, "NewUI/dropdown_menu_sides_left");
                RV = new RecTexPair(r.X + r.Width - 6, TL.r.Y + 6, height - 12, "NewUI/dropdown_menu_sides_right");
                Top = new RecTexPair(r.X + TL.r.Width, TL.r.Y, "NewUI/dropdown_menu_sides_top", r.Width - TL.r.Width - TR.r.Width);
                Bot = new RecTexPair(r.X + TL.r.Width, TL.r.Y + height - 6, "NewUI/dropdown_menu_sides_bottom", r.Width - BL.r.Width - BR.r.Width);
                container.Add(TL);
                container.Add(TR);
                container.Add(BL);
                container.Add(BR);
                container.Add(LV);
                container.Add(RV);
                container.Add(Top);
                container.Add(Bot);
			}
		}

		private class RecTexPair
		{
			public Rectangle r;
			public readonly string tex;

			public RecTexPair(int x, int y, string t)
			{
                r = new Rectangle(x, y, ResourceManager.TextureDict[t].Width, ResourceManager.TextureDict[t].Height);
                tex = t;
			}

			public RecTexPair(int x, int y, int h, string t)
			{
                r = new Rectangle(x, y, ResourceManager.TextureDict[t].Width, h);
                tex = t;
			}

			public RecTexPair(int x, int y, string t, int w)
			{
                r = new Rectangle(x, y, w, ResourceManager.TextureDict[t].Height);
                tex = t;
			}
		}
	}
}