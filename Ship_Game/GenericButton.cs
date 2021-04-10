using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
	public sealed class GenericButton
	{
		public Rectangle R;

		private string Text;

		private SpriteFont Font;

		private Vector2 TextPos;

		public bool ToggleOn;
		public Color HoveredColor   = Color.White;
		public Color UnHoveredColor = Color.DarkGray;
		private Vector2 CapitalPos;

		private SpriteFont Cap;

		private SpriteFont Small;

		private string capT;

		private string smallT;

		private bool Hover;

		public GenericButton(Rectangle r, string text, SpriteFont font)
		{
			this.R = r;
			this.Text = text;
			this.Font = font;
			TextPos = new Vector2(r.X + r.Width / 2 - font.MeasureString(text).X / 2f, r.Y + r.Height / 2 - font.LineSpacing / 2);
		}

		public GenericButton(Vector2 v, string text, SpriteFont capitalFont, SpriteFont smallFont)
		{
			Cap = capitalFont;
			Small = smallFont;
			capT = text[0].ToString();
			smallT = text.Remove(0, 1);

			Vector2 capTsize = capitalFont.MeasureString(capT);
			Vector2 textSize = smallFont.MeasureString(text);
			R = new Rectangle((int)v.X - (int)capTsize.X - (int)textSize.X, (int)v.Y, (int)capTsize.X + (int)textSize.X, capitalFont.LineSpacing);
			CapitalPos = new Vector2(R.X, R.Y);
			TextPos = new Vector2(CapitalPos.X + capTsize.X + 1f, CapitalPos.Y + capitalFont.LineSpacing - smallFont.LineSpacing - 3f);
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
				white = (Hover ? HoveredColor : UnHoveredColor);
			}
			spriteBatch.DrawString(font, text, textPos, white);
		}

		public void DrawWithShadow(SpriteBatch batch)
		{
			Color white;
			string text = Text;
			Vector2 textPos = TextPos;
			SpriteFont font = Font;
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