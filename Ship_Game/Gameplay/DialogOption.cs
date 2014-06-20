using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game;
using System;

namespace Ship_Game.Gameplay
{
	public class DialogOption
	{
		public object Target;

		public int number;

		public string words;

		private Rectangle ClickRect;

		public string SpecialInquiry = "";

		public string Response;

		public bool Hover;

		public DialogOption()
		{
		}

		public DialogOption(int n, string w, Vector2 Cursor, SpriteFont Font)
		{
			this.number = n;
			this.words = w;
			int width = (int)Font.MeasureString(w).X;
			this.ClickRect = new Rectangle((int)Cursor.X, (int)Cursor.Y, width, Font.LineSpacing);
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, SpriteFont Font)
		{
			HelperFunctions.DrawDropShadowText(ScreenManager, string.Concat(this.number.ToString(), ". ", this.words), new Vector2((float)this.ClickRect.X, (float)this.ClickRect.Y), Font, (this.Hover ? Color.White : new Color(255, 255, 255, 220)));
		}

		public string HandleInput(InputState input)
		{
			if (!HelperFunctions.CheckIntersection(this.ClickRect, input.CursorPosition))
			{
				this.Hover = false;
			}
			else
			{
				this.Hover = true;
				if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
				{
					return this.Response;
				}
			}
			return null;
		}

		public void Update(Vector2 Cursor)
		{
			this.ClickRect.Y = (int)Cursor.Y;
		}
	}
}