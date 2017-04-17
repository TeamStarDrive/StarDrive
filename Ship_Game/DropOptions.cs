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
        private readonly RecTexPair[] Border = new RecTexPair[16];
        private int Count;

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
            Reset();
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
			Vector2 mousePos = Mouse.GetState().Pos();
			if (HelperFunctions.CheckIntersection(r, mousePos))
			{
				hover = true;
			}
			if (hover)
			{
				Primitives2D.FillRectangle(spriteBatch, r, new Color(128, 87, 43, 50));
			}
            for (int i = 0; i < Count; ++i) Border[i].Draw(spriteBatch, Color.White);
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
				spriteBatch.DrawString(Fonts.Arial12Bold, txt, new Vector2(r.X + 10, r.Y + r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2), new Color(255, 239, 208));
			}
			else if(Options.Count >0)
			{
				string txt = Options[ActiveIndex].Name;
				bool addDots = false;
				while (Fonts.Arial12Bold.MeasureString(txt).X > (r.Width - 22))
				{
					txt = txt.Remove(txt.Length - 1);
					addDots = true;
				}
				if (addDots)
				{
					txt = string.Concat(txt, "...");
				}
				spriteBatch.DrawString(Fonts.Arial12Bold, txt, new Vector2((r.X + 10), (r.Y + r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), Color.White);
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
					if (HelperFunctions.CheckIntersection(e.clickRect, mousePos))
					{
						var hoverLeft   = new Rectangle(e.clickRect.X + 5, e.clickRect.Y + 1, 6, 15);
                        var hoverMiddle = new Rectangle(e.clickRect.X + 11, e.clickRect.Y + 1, e.clickRect.Width - 22, 15);
                        var hoverRight  = new Rectangle(hoverMiddle.X + hoverMiddle.Width, hoverMiddle.Y, 6, 15);
						spriteBatch.Draw(ResourceManager.TextureDict["NewUI/dropdown_menuitem_hover_left"], hoverLeft, Color.White);
						spriteBatch.Draw(ResourceManager.TextureDict["NewUI/dropdown_menuitem_hover_middle"], hoverMiddle, Color.White);
						spriteBatch.Draw(ResourceManager.TextureDict["NewUI/dropdown_menuitem_hover_right"], hoverRight, Color.White);
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
            for (int i = 0; i < Count; ++i) Border[i].Draw(spriteBatch, Color.DarkGray);
            spriteBatch.DrawString(Fonts.Arial12Bold, "-", new Vector2(r.X + 10, r.Y + r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2), Color.DarkGray);
		}

		public void HandleInput(InputState input)
		{
			if (r.HitTest(input.CursorPosition) && input.InGameSelect)
			{
                Open = !Open;
				if (Open && Options.Count == 1)
				{
                    Open = false;
				}
				if (Open)
				{
					GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
				}
                Reset();
			}
			else if (ClickAbleOpenRect.HitTest(input.CursorPosition))
			{
				if (Open)
				{
					foreach (Entry e in Options)
					{
						if (!e.clickRect.HitTest(input.CursorPosition) || !input.InGameSelect)
						{
							continue;
						}
                        Options[ActiveIndex].clickRect = e.clickRect;
						e.clickRect = new Rectangle();
                        ActiveIndex = Options.IndexOf(e);
						GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
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
            Array.Clear(Border, 0, Border.Length);

            var ttl = ResourceManager.TextureDict["NewUI/dropdown_menu_corner_TL"];
            var ttr = ResourceManager.TextureDict["NewUI/dropdown_menu_corner_TR"];
            var tbl = ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BL"];
            var tbr = ResourceManager.TextureDict["NewUI/dropdown_menu_corner_BR"];
            var left  = ResourceManager.TextureDict["NewUI/dropdown_menu_sides_left"];
            var right = ResourceManager.TextureDict["NewUI/dropdown_menu_sides_right"];
            var top = ResourceManager.TextureDict["NewUI/dropdown_menu_sides_top"];
            var bot = ResourceManager.TextureDict["NewUI/dropdown_menu_sides_bottom"];

            int x = r.X, y = r.Y, w = r.Width, h = r.Height;
            var tl = Border[0] = new RecTexPair(x, y, ttl);
            var tr = Border[1] = new RecTexPair(x+w-ttr.Width, y, ttr);
            var bl = Border[2] = new RecTexPair(x, y+h-tbl.Height, tbl);
            var br = Border[3] = new RecTexPair(x+w-tbl.Width, y+h-tbr.Height, tbr);
            Border[4] = new RecTexPair(x, y+6, h-12, left);
            Border[5] = new RecTexPair(x+w-6, y+6, h-12, right);
            Border[6] = new RecTexPair(x+tl.W, y, top, w-tl.W-tr.W);
            Border[7] = new RecTexPair(x+tl.W, y+h-6, bot, w-bl.W-br.W);
            Count = 8;
            if (Open)
			{
				int height = (Options.Count - 1) * 18;
                OpenRect = new Rectangle(x + 6, y + h + 3 + 6, w - 12, height - 12);
                ClickAbleOpenRect = new Rectangle(x + 6, y + h + 3, w - 12, height - 6);

                tl = Border[8]  = new RecTexPair(x, y+h+3, ttl);
                tr = Border[9]  = new RecTexPair(x+w-ttr.Width, tl.Y, ttr);
                bl = Border[10] = new RecTexPair(x, tl.Y+height-tbl.Height, tbl);
                br = Border[11] = new RecTexPair(x+w-tbl.Width, tl.Y+height-tbr.Height, tbr);
                Border[12] = new RecTexPair(x, tl.Y+6, height-12, left);
                Border[13] = new RecTexPair(x+w-6, tl.Y+6, height-12, right);
                Border[14] = new RecTexPair(x+tl.W, tl.Y, top, w-tl.W-tr.W);
                Border[15] = new RecTexPair(x+tl.W, tl.Y+height-6, bot, w-bl.W-br.W);
                Count = 16;
			}
		}

		private struct RecTexPair
		{
			private readonly Rectangle Rect;
            private readonly Texture2D Tex;
            public int Y => Rect.Y;
            public int W => Rect.Width;

			public RecTexPair(int x, int y, Texture2D t)
			{
                Rect = new Rectangle(x, y, t.Width, t.Height);
                Tex = t;
			}
			public RecTexPair(int x, int y, int h, Texture2D t)
			{
                Rect = new Rectangle(x, y, t.Width, h);
                Tex = t;
			}
			public RecTexPair(int x, int y, Texture2D t, int w)
			{
                Rect = new Rectangle(x, y, w, t.Height);
                Tex = t;
			}
            public void Draw(SpriteBatch spriteBatch, Color color)
            {
                spriteBatch.Draw(Tex, Rect, color);
            }
        }
	}
}