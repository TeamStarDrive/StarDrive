using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Ship_Game
{
	public sealed class GenericButton
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
			TextPos = new Vector2((float)(R.X + R.Width / 2) - Font.MeasureString(Text).X / 2f, (float)(R.Y + R.Height / 2 - Font.LineSpacing / 2));
		}

		public GenericButton(Vector2 v, string Text, SpriteFont CapitalFont, SpriteFont SmallFont)
		{
			Cap = CapitalFont;
			Small = SmallFont;
			capT = Text[0].ToString();
			smallT = Text.Remove(0, 1);
			R = new Rectangle((int)v.X - (int)CapitalFont.MeasureString(capT).X - (int)SmallFont.MeasureString(Text).X, (int)v.Y, (int)CapitalFont.MeasureString(capT).X + (int)SmallFont.MeasureString(Text).X, CapitalFont.LineSpacing);
			CapitalPos = new Vector2((float)R.X, (float)R.Y);
			TextPos = new Vector2(CapitalPos.X + CapitalFont.MeasureString(capT).X + 1f, CapitalPos.Y + (float)CapitalFont.LineSpacing - (float)SmallFont.LineSpacing - 3f);
		}

		public void Draw(ScreenManager ScreenManager)
		{
			Color white;
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			SpriteFont font = Font;
			string text = Text;
			Vector2 textPos = TextPos;
			if (ToggleOn)
			{
				white = Color.White;
			}
			else
			{
				white = (Hover ? Color.White : Color.DarkGray);
			}
			spriteBatch.DrawString(font, text, textPos, white);
		}

		public void DrawWithShadow(ScreenManager ScreenManager)
		{
			Color white;
			ScreenManager screenManager = ScreenManager;
			string text = Text;
			Vector2 textPos = TextPos;
			SpriteFont font = Font;
			if (ToggleOn)
			{
				white = Color.White;
			}
			else
			{
				white = (Hover ? Color.White : Color.DarkGray);
			}
			HelperFunctions.DrawDropShadowText(screenManager, text, textPos, font, white);
		}

		public void DrawWithShadowCaps(ScreenManager ScreenManager)
		{
			Color white;
			Color color;
			ScreenManager screenManager = ScreenManager;
			string str = capT;
			Vector2 capitalPos = CapitalPos;
			SpriteFont cap = Cap;
			if (ToggleOn)
			{
				white = Color.White;
			}
			else
			{
				white = (Hover ? Color.White : Color.DarkGray);
			}
			HelperFunctions.DrawDropShadowText(screenManager, str, capitalPos, cap, white);
			ScreenManager screenManager1 = ScreenManager;
			string str1 = smallT;
			Vector2 textPos = TextPos;
			SpriteFont small = Small;
			if (ToggleOn)
			{
				color = Color.White;
			}
			else
			{
				color = (Hover ? Color.White : Color.DarkGray);
			}
			HelperFunctions.DrawDropShadowText(screenManager1, str1, textPos, small, color);
		}

		public bool HandleInput(InputState input)
		{
			if (!R.HitTest(input.CursorPosition))
			{
				Hover = false;
			}
			else
			{
				Hover = true;
				if (input.LeftMouseClick)
				{
					GameAudio.PlaySfxAsync("echo_affirm");
					return true;
				}
			}
			return false;
		}

		public void Transition(Rectangle R)
		{
			TextPos = new Vector2((float)(R.X + R.Width / 2) - Font.MeasureString(Text).X / 2f, (float)(R.Y + R.Height / 2 - Font.LineSpacing / 2));
		}

		public void TransitionCaps(Rectangle R)
		{
			CapitalPos = new Vector2((float)R.X, (float)R.Y);
			TextPos = new Vector2(CapitalPos.X + Cap.MeasureString(capT).X + 1f, CapitalPos.Y + (float)Cap.LineSpacing - (float)Small.LineSpacing - 3f);
		}
	}
}