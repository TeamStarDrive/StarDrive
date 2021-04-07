using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
	public sealed class DropDownMenu
	{
		private RecTexPair TL;

		private RecTexPair TR;

		private RecTexPair BL;

		private RecTexPair BR;

		private RecTexPair LV;

		private RecTexPair RV;

		private RecTexPair Top;

		private RecTexPair Bot;

		private Array<RecTexPair> container = new Array<RecTexPair>();

		public Rectangle r;

		public int ActiveIndex;

		private Array<string> Options = new Array<string>();

		public DropDownMenu(float x, float y, float w, float h) : this(new Rectangle((int)x, (int)y, (int)w, (int)h))
		{
		}

		public DropDownMenu(Rectangle r)
		{
			this.r = r;
			TL = new RecTexPair(r.X, r.Y, "NewUI/dropdown_menu_corner_TL");
			TR = new RecTexPair(r.X + r.Width - ResourceManager.Texture("NewUI/dropdown_menu_corner_TR").Width, r.Y, "NewUI/dropdown_menu_corner_TR");
			BL = new RecTexPair(r.X, r.Y + r.Height - ResourceManager.Texture("NewUI/dropdown_menu_corner_BL").Height, "NewUI/dropdown_menu_corner_BL");
			BR = new RecTexPair(r.X + r.Width - ResourceManager.Texture("NewUI/dropdown_menu_corner_BL").Width, r.Y + r.Height - ResourceManager.Texture("NewUI/dropdown_menu_corner_BR").Height, "NewUI/dropdown_menu_corner_BR");
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

		public void AddOption(in LocalizedText option)
		{
			Options.Add(option.Text);
		}

		public void Draw(SpriteBatch batch)
		{
			bool hover = r.HitTest(GameBase.Base.Manager.input.CursorPosition);
			if (hover)
			{
				batch.FillRectangle(r, new Color(128, 87, 43, 50));
			}
			foreach (RecTexPair r in container)
			{
				batch.Draw(ResourceManager.Texture(r.tex), r.r, Color.White);
			}
			if (hover)
			{
				batch.DrawString(Fonts.Arial12Bold, Options[ActiveIndex], new Vector2(r.X + 8, r.Y + r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2), Color.White);
				return;
			}
			batch.DrawString(Fonts.Arial12Bold, Options[ActiveIndex], new Vector2(this.r.X + 8, this.r.Y + this.r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2), Colors.Cream);
		}

		public void DrawGrayed(SpriteBatch spriteBatch)
		{
			foreach (RecTexPair r in container)
			{
				spriteBatch.Draw(ResourceManager.Texture(r.tex), r.r, Color.DarkGray);
			}
			spriteBatch.DrawString(Fonts.Arial12Bold, "-", new Vector2(this.r.X + 8, this.r.Y + this.r.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2), Color.DarkGray);
		}

		public void Toggle()
		{
			DropDownMenu activeIndex = this;
			activeIndex.ActiveIndex = activeIndex.ActiveIndex + 1;
			if (ActiveIndex > Options.Count - 1)
			{
				ActiveIndex = 0;
			}
		}

		private class RecTexPair
		{
			public Rectangle r;

			public string tex;

			public RecTexPair(int x, int y, string t)
			{
				r = new Rectangle(x, y, ResourceManager.Texture(t).Width, ResourceManager.Texture(t).Height);
				tex = t;
			}

			public RecTexPair(int x, int y, int h, string t)
			{
				r = new Rectangle(x, y, ResourceManager.Texture(t).Width, h);
				tex = t;
			}

			public RecTexPair(int x, int y, string t, int w)
			{
				r = new Rectangle(x, y, w, ResourceManager.Texture(t).Height);
				tex = t;
			}
		}
	}
}