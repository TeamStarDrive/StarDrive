using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
	public sealed class GenericButton
	{
		public Rectangle R;

		private string Text;

		private Graphics.Font Font;

		private Vector2 TextPos;

		public bool ToggleOn;
		public Color HoveredColor   = Color.White;
		public Color UnHoveredColor = Color.DarkGray;
		private Vector2 CapitalPos;

		private Graphics.Font Cap;

		private Graphics.Font Small;

		private string capT;

		private string smallT;

		private bool Hover;

		public GenericButton(Rectangle R, string Text, Graphics.Font Font)
		{
			this.R = R;
			this.Text = Text;
			this.Font = Font;
			TextPos = new Vector2(R.X + R.Width / 2 - Font.MeasureString(Text).X / 2f, R.Y + R.Height / 2 - Font.LineSpacing / 2);
		}

		public GenericButton(Vector2 v, string Text, Graphics.Font CapitalFont, Graphics.Font SmallFont)
		{
			Cap = CapitalFont;
			Small = SmallFont;
			capT = Text[0].ToString();
			smallT = Text.Remove(0, 1);
			R = new Rectangle((int)v.X - (int)CapitalFont.MeasureString(capT).X - (int)SmallFont.MeasureString(Text).X, (int)v.Y, (int)CapitalFont.MeasureString(capT).X + (int)SmallFont.MeasureString(Text).X, CapitalFont.LineSpacing);
			CapitalPos = new Vector2(R.X, R.Y);
			TextPos = new Vector2(CapitalPos.X + CapitalFont.MeasureString(capT).X + 1f, CapitalPos.Y + CapitalFont.LineSpacing - SmallFont.LineSpacing - 3f);
		}

		public void Draw(ScreenManager ScreenManager)
		{
			Color white;
			SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
			Graphics.Font font = Font;
			string text = Text;
			Vector2 textPos = TextPos;
			if (ToggleOn)
			{
				white = Color.White;
			}
			else
			{
				white = (Hover ? HoveredColor : UnHoveredColor);
			}
			spriteBatch.DrawString(font, text, textPos, white);
		}

		public void DrawWithShadow(SpriteBatch batch)
		{
			Color white;
			string text = Text;
			Vector2 textPos = TextPos;
			Graphics.Font font = Font;
			if (ToggleOn)
			{
				white = Color.White;
			}
			else
			{
				white = (Hover ? HoveredColor : UnHoveredColor);
			}
			HelperFunctions.DrawDropShadowText(batch, text, textPos, font, white);
		}

		public void DrawWithShadowCaps(SpriteBatch batch)
		{
			Color color;
            if (ToggleOn)
            {
                color = Color.DarkOrange;
            }
            else
            {
                color = (Hover ? HoveredColor : UnHoveredColor);
            }
			HelperFunctions.DrawDropShadowText(batch, capT, CapitalPos, Cap, color);
			HelperFunctions.DrawDropShadowText(batch, smallT, TextPos, Small, color);
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
					GameAudio.EchoAffirmative();
					return true;
				}
			}
			return false;
		}

		public void Transition(Rectangle R)
		{
			TextPos = new Vector2(R.X + R.Width / 2 - Font.MeasureString(Text).X / 2f, R.Y + R.Height / 2 - Font.LineSpacing / 2);
		}

		public void TransitionCaps(Rectangle R)
		{
			CapitalPos = new Vector2(R.X, R.Y);
			TextPos = new Vector2(CapitalPos.X + Cap.MeasureString(capT).X + 1f, CapitalPos.Y + Cap.LineSpacing - Small.LineSpacing - 3f);
		}
	}
}