using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class BlueButton
	{
		public Rectangle Button;

		public bool Hover;

		public string Text = "";

		public bool ToggleOn;

		public object Value;

		public int Tip_ID;

		private Vector2 TextPos;

		public BlueButton(Vector2 Position, string text)
		{
			this.Button = new Rectangle((int)Position.X, (int)Position.Y, 180, 33);
			this.Text = text;
			this.TextPos = new Vector2((float)(this.Button.X + this.Button.Width / 2) - Fonts.Pirulen12.MeasureString(this.Text).X / 2f, (float)(this.Button.Y + this.Button.Height / 2 - Fonts.Pirulen12.LineSpacing / 2));
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			if (!this.ToggleOn)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/button_blue_hover0"], this.Button, Color.White);
			}
			else
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/button_blue_hover1"], this.Button, Color.White);
			}
			if (this.Hover)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/button_blue_hover2"], this.Button, Color.White);
			}
			if (this.Hover && this.Tip_ID != 0)
			{
				ToolTip.CreateTooltip(this.Tip_ID, ScreenManager);
			}
			ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, this.Text, this.TextPos, Color.White);
		}

		public void DrawTransition(Ship_Game.ScreenManager ScreenManager, Rectangle b)
		{
			this.TextPos = new Vector2((float)(b.X + this.Button.Width / 2) - Fonts.Pirulen12.MeasureString(this.Text).X / 2f, (float)(b.Y + this.Button.Height / 2 - Fonts.Pirulen12.LineSpacing / 2));
			if (!this.ToggleOn)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/button_blue_hover0"], b, Color.White);
			}
			else
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/button_blue_hover1"], b, Color.White);
			}
			if (this.Hover)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/button_blue_hover2"], b, Color.White);
			}
			if (this.Hover && this.Tip_ID != 0)
			{
				ToolTip.CreateTooltip(this.Tip_ID, ScreenManager);
			}
			ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, this.Text, this.TextPos, Color.White);
		}

		public bool HandleInput(InputState input)
		{
			if (!HelperFunctions.CheckIntersection(this.Button, input.CursorPosition))
			{
				this.Hover = false;
			}
			else
			{
				this.Hover = true;
				if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
				{
					AudioManager.PlayCue("echo_affirm");
					return true;
				}
			}
			return false;
		}
	}
}