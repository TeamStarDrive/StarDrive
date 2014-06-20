using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ModuleHeader
	{
		public bool Open;

		public string Text = "";

		private float Width = 305f;

		public bool Hover;

		private Rectangle ClickRect = new Rectangle();

		private Rectangle r;

		public ModuleHeader(string Text)
		{
			this.Text = Text;
		}

		public ModuleHeader(string Text, float Width)
		{
			this.Width = Width;
			this.Text = Text;
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, Vector2 Position)
		{
			this.r = new Rectangle((int)Position.X, (int)Position.Y, (int)this.Width, 30);
			(new Selector(ScreenManager, this.r, (this.Hover ? new Color(95, 82, 47) : new Color(32, 30, 18)))).Draw();
			Vector2 textPos = new Vector2((float)(this.r.X + 10), (float)(this.r.Y + this.r.Height / 2 - Fonts.Pirulen12.LineSpacing / 2));
			ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, this.Text, textPos, Color.White);
			this.ClickRect = new Rectangle(this.r.X + this.r.Width - 15, this.r.Y + 10, 10, 10);
			textPos = new Vector2((float)this.ClickRect.X - Fonts.Arial20Bold.MeasureString((this.Open ? "-" : "+")).X / 2f, (float)(this.ClickRect.Y + 6 - Fonts.Arial20Bold.LineSpacing / 2));
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, (this.Open ? "-" : "+"), textPos, Color.White);
		}

		public void DrawWidth(Ship_Game.ScreenManager ScreenManager, Vector2 Position, int width)
		{
			this.r = new Rectangle((int)Position.X, (int)Position.Y, width, 30);
			(new Selector(ScreenManager, this.r, (this.Hover ? new Color(95, 82, 47) : new Color(32, 30, 18)))).Draw();
			Vector2 textPos = new Vector2((float)(this.r.X + 10), (float)(this.r.Y + this.r.Height / 2 - Fonts.Pirulen12.LineSpacing / 2));
			ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, this.Text, textPos, Color.White);
			this.ClickRect = new Rectangle(this.r.X + this.r.Width - 15, this.r.Y + 10, 10, 10);
			textPos = new Vector2((float)this.ClickRect.X - Fonts.Arial20Bold.MeasureString((this.Open ? "-" : "+")).X / 2f, (float)(this.ClickRect.Y + 6 - Fonts.Arial20Bold.LineSpacing / 2));
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, (this.Open ? "-" : "+"), textPos, Color.White);
		}

		public bool HandleInput(InputState input, ScrollList.Entry e)
		{
			if (!HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))
			{
				this.Hover = false;
			}
			else
			{
				this.Hover = true;
				if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
				{
					AudioManager.PlayCue("sd_ui_accept_alt3");
					this.Open = !this.Open;
					e.ShowingSub = !e.ShowingSub;
					if (!this.Open)
					{
						int num = 0;
						foreach (ScrollList.Entry subEntry in e.SubEntries)
						{
							num++;
						}
						ScrollList parentList = e.ParentList;
						parentList.indexAtTop = parentList.indexAtTop - num;
						if (e.ParentList.indexAtTop < 0)
						{
							e.ParentList.indexAtTop = 0;
						}
					}
					e.ParentList.Update();
					return true;
				}
			}
			return false;
		}
	}
}