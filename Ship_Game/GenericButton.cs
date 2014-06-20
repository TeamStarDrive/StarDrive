using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Ship_Game
{
	public class GenericButton
	{
		public Rectangle R;

		private string Text;

		private SpriteFont Font;

		private Vector2 TextPos;

		public bool ToggleOn;

		private Vector2 CapitalPos;

		private SpriteFont Cap;

		private SpriteFont Small;

		private string capT;

		private string smallT;

		private bool Hover;

		public GenericButton(Rectangle R, string Text, SpriteFont Font)
		{
			this.R = R;
			this.Text = Text;
			this.Font = Font;
			this.TextPos = new Vector2((float)(R.X + R.Width / 2) - Font.MeasureString(Text).X / 2f, (float)(R.Y + R.Height / 2 - Font.LineSpacing / 2));
		}

		public GenericButton(Vector2 v, string Text, SpriteFont CapitalFont, SpriteFont SmallFont)
		{
			this.Cap = CapitalFont;
			this.Small = SmallFont;
			this.capT = Text[0].ToString();
			this.smallT = Text.Remove(0, 1);
			this.R = new Rectangle((int)v.X - (int)CapitalFont.MeasureString(this.capT).X - (int)SmallFont.MeasureString(Text).X, (int)v.Y, (int)CapitalFont.MeasureString(this.capT).X + (int)SmallFont.MeasureString(Text).X, CapitalFont.LineSpacing);
			this.CapitalPos = new Vector2((float)this.R.X, (float)this.R.Y);
			this.TextPos = new Vector2(this.CapitalPos.X + CapitalFont.MeasureString(this.capT).X + 1f, this.CapitalPos.Y + (float)CapitalFont.LineSpacing - (float)SmallFont.LineSpacing - 3f);
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			Color white;
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			SpriteFont font = this.Font;
			string text = this.Text;
			Vector2 textPos = this.TextPos;
			if (this.ToggleOn)
			{
				white = Color.White;
			}
			else
			{
				white = (this.Hover ? Color.White : Color.DarkGray);
			}
			spriteBatch.DrawString(font, text, textPos, white);
		}

		public void DrawWithShadow(Ship_Game.ScreenManager ScreenManager)
		{
			Color white;
			Ship_Game.ScreenManager screenManager = ScreenManager;
			string text = this.Text;
			Vector2 textPos = this.TextPos;
			SpriteFont font = this.Font;
			if (this.ToggleOn)
			{
				white = Color.White;
			}
			else
			{
				white = (this.Hover ? Color.White : Color.DarkGray);
			}
			HelperFunctions.DrawDropShadowText(screenManager, text, textPos, font, white);
		}

		public void DrawWithShadowCaps(Ship_Game.ScreenManager ScreenManager)
		{
			Color white;
			Color color;
			Ship_Game.ScreenManager screenManager = ScreenManager;
			string str = this.capT;
			Vector2 capitalPos = this.CapitalPos;
			SpriteFont cap = this.Cap;
			if (this.ToggleOn)
			{
				white = Color.White;
			}
			else
			{
				white = (this.Hover ? Color.White : Color.DarkGray);
			}
			HelperFunctions.DrawDropShadowText(screenManager, str, capitalPos, cap, white);
			Ship_Game.ScreenManager screenManager1 = ScreenManager;
			string str1 = this.smallT;
			Vector2 textPos = this.TextPos;
			SpriteFont small = this.Small;
			if (this.ToggleOn)
			{
				color = Color.White;
			}
			else
			{
				color = (this.Hover ? Color.White : Color.DarkGray);
			}
			HelperFunctions.DrawDropShadowText(screenManager1, str1, textPos, small, color);
		}

		public bool HandleInput(InputState input)
		{
			if (!HelperFunctions.CheckIntersection(this.R, input.CursorPosition))
			{
				this.Hover = false;
			}
			else
			{
				this.Hover = true;
				if (input.CurrentMouseState.LeftButton == ButtonState.Released && input.LastMouseState.LeftButton == ButtonState.Pressed)
				{
					AudioManager.PlayCue("echo_affirm");
					return true;
				}
			}
			return false;
		}

		public void Transition(Rectangle R)
		{
			this.TextPos = new Vector2((float)(R.X + R.Width / 2) - this.Font.MeasureString(this.Text).X / 2f, (float)(R.Y + R.Height / 2 - this.Font.LineSpacing / 2));
		}

		public void TransitionCaps(Rectangle R)
		{
			this.CapitalPos = new Vector2((float)R.X, (float)R.Y);
			this.TextPos = new Vector2(this.CapitalPos.X + this.Cap.MeasureString(this.capT).X + 1f, this.CapitalPos.Y + (float)this.Cap.LineSpacing - (float)this.Small.LineSpacing - 3f);
		}
	}
}