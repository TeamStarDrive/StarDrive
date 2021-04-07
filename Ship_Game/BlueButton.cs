using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
	public sealed class BlueButton
	{
		public Rectangle Button;

		public bool Hover;

		public string Text = "";

		public bool ToggleOn;

		public object Value;

		public LocalizedText Tooltip;

		private Vector2 TextPos;

		public BlueButton(Vector2 Position, string text)
		{
			Button = new Rectangle((int)Position.X, (int)Position.Y, 180, 33);
			Text = text;
			TextPos = new Vector2(Button.X + Button.Width / 2 - Fonts.Pirulen12.MeasureString(Text).X / 2f, Button.Y + Button.Height / 2 - Fonts.Pirulen12.LineSpacing / 2);
		}

		public void Draw(ScreenManager ScreenManager)
		{
			if (!ToggleOn)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/button_blue_hover0"), Button, Color.White);
			}
			else
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/button_blue_hover1"), Button, Color.White);
			}
			if (Hover)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/button_blue_hover2"), Button, Color.White);
			}
			if (Hover && Tooltip.IsValid)
			{
				ToolTip.CreateTooltip(Tooltip);
			}
			ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, Text, TextPos, Color.White);
		}

		public void DrawTransition(ScreenManager ScreenManager, Rectangle b)
		{
			TextPos = new Vector2(b.X + Button.Width / 2 - Fonts.Pirulen12.MeasureString(Text).X / 2f, b.Y + Button.Height / 2 - Fonts.Pirulen12.LineSpacing / 2);
			if (!ToggleOn)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/button_blue_hover0"), b, Color.White);
			}
			else
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/button_blue_hover1"), b, Color.White);
			}
			if (Hover)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/button_blue_hover2"), b, Color.White);
			}
			if (Hover && Tooltip.IsValid)
			{
				ToolTip.CreateTooltip(Tooltip);
			}
			ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, Text, TextPos, Color.White);
		}

		public bool HandleInput(InputState input)
		{
			if (!Button.HitTest(input.CursorPosition))
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
	}
}