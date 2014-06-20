using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Ship_Game
{
	public class Checkbox
	{
		private Rectangle EnclosingRect;

		private string Text;

		private Ref<bool> connectedTo;

		private SpriteFont Font;

		private Rectangle CheckRect;

		private Vector2 TextPos;

		private Vector2 CheckPos;

		public int Tip_Token;

		public Checkbox(Vector2 Position, string text, Ref<bool> connectedTo, SpriteFont Font)
		{
			this.connectedTo = connectedTo;
			this.Text = text;
			this.Font = Font;
			this.EnclosingRect = new Rectangle((int)Position.X, (int)Position.Y, (int)Font.MeasureString(text).X + 32, Font.LineSpacing + 6);
			this.CheckRect = new Rectangle(this.EnclosingRect.X + 15, this.EnclosingRect.Y + this.EnclosingRect.Height / 2 - 5, 10, 10);
			this.TextPos = new Vector2((float)(this.CheckRect.X + 15), (float)(this.CheckRect.Y + this.CheckRect.Height / 2 - Font.LineSpacing / 2));
			this.CheckPos = new Vector2((float)(this.CheckRect.X + 5) - Fonts.Arial12Bold.MeasureString("x").X / 2f, (float)(this.CheckRect.Y + 4 - Fonts.Arial12Bold.LineSpacing / 2));
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			if (HelperFunctions.CheckIntersection(this.CheckRect, new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y)) && this.Tip_Token != 0)
			{
				ToolTip.CreateTooltip(Localizer.Token(this.Tip_Token), ScreenManager);
			}
			Primitives2D.DrawRectangle(ScreenManager.SpriteBatch, this.CheckRect, new Color(96, 81, 49));
			ScreenManager.SpriteBatch.DrawString(this.Font, this.Text, this.TextPos, Color.White);
			if (this.connectedTo.Value)
			{
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "x", this.CheckPos, Color.White);
			}
		}

		public bool HandleInput(InputState input)
		{
			if (HelperFunctions.CheckIntersection(this.CheckRect, input.CursorPosition) && input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
			{
				this.connectedTo.Value = !this.connectedTo.Value;
			}
			return false;
		}
	}
}