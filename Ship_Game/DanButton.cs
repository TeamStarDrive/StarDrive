using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class DanButton
	{
		public Rectangle r;

		public string Text;

		public string ToggledText;

		public bool IsToggle;

		public bool Toggled;

		public Vector2 Pos;

		public bool Hover;

		private Vector2 TextPos;

		public DanButton(Vector2 rPos, string Text)
		{
			this.Pos = rPos;
			this.r = new Rectangle((int)rPos.X, (int)rPos.Y, 182, 25);
			this.Text = Text;
			this.TextPos = new Vector2((float)(this.r.X + 20), (float)(this.r.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2 - 2));
			this.ToggledText = Text;
		}

		public void Draw(ScreenManager screenManager)
		{
			string str;
			Color color;
			Vector2 pos = this.TextPos;
			if (GlobalStats.Config.Language == "German")
			{
				pos.X = pos.X - 9f;
			}
			screenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/dan_button"], this.r, Color.White);
			SpriteBatch spriteBatch = screenManager.SpriteBatch;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			str = (this.Toggled ? this.ToggledText : this.Text);
			Vector2 vector2 = pos;
			if (this.Hover)
			{
				color = new Color(255, 255, 255, 150);
			}
			else
			{
				color = (this.Toggled ? new Color(121, 98, 75) : Color.White);
			}
			spriteBatch.DrawString(arial12Bold, str, vector2, color);
		}

		public void Draw(ScreenManager screenManager, Rectangle rect)
		{
			string str;
			Color color;
			screenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/dan_button"], rect, Color.White);
			Vector2 tPos = new Vector2((float)(rect.X + 25), (float)(rect.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2));
			Vector2 pos = tPos;
			if (GlobalStats.Config.Language == "German")
			{
				pos.X = pos.X - 9f;
			}
			SpriteBatch spriteBatch = screenManager.SpriteBatch;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			str = (this.Toggled ? this.ToggledText : this.Text);
			Vector2 vector2 = pos;
			if (this.Hover)
			{
				color = new Color(255, 255, 255, 150);
			}
			else
			{
				color = (this.Toggled ? new Color(121, 98, 75) : Color.White);
			}
			spriteBatch.DrawString(arial12Bold, str, vector2, color);
		}

		public void DrawBlue(ScreenManager screenManager)
		{
			string str;
			Color color;
			Vector2 pos = this.TextPos;
			if (GlobalStats.Config.Language == "German")
			{
				pos.X = pos.X - 9f;
			}
			screenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/dan_button_blue"], this.r, Color.White);
			SpriteBatch spriteBatch = screenManager.SpriteBatch;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			str = (this.Toggled ? this.ToggledText : this.Text);
			Vector2 vector2 = pos;
			if (this.Hover)
			{
				color = new Color(174, 202, 255);
			}
			else
			{
				color = (this.Toggled ? Color.White : new Color(88, 108, 146));
			}
			spriteBatch.DrawString(arial12Bold, str, vector2, color);
		}

		public void DrawBlue(ScreenManager screenManager, Rectangle rect)
		{
			string str;
			Color color;
			screenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/dan_button_blue"], rect, Color.White);
			Vector2 tPos = new Vector2((float)(rect.X + 25), (float)(rect.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2));
			Vector2 pos = tPos;
			if (GlobalStats.Config.Language == "German")
			{
				pos.X = pos.X - 9f;
			}
			SpriteBatch spriteBatch = screenManager.SpriteBatch;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			str = (this.Toggled ? this.ToggledText : this.Text);
			Vector2 vector2 = pos;
			if (this.Hover)
			{
				color = new Color(174, 202, 255);
			}
			else
			{
				color = (this.Toggled ? Color.White : new Color(88, 108, 146));
			}
			spriteBatch.DrawString(arial12Bold, str, vector2, color);
		}

		public bool HandleInput(InputState input)
		{
			if (!HelperFunctions.CheckIntersection(this.r, input.CursorPosition))
			{
				this.Hover = false;
			}
			else
			{
				this.Hover = true;
				if (input.InGameSelect)
				{
					AudioManager.PlayCue("echo_affirm");
					if (this.IsToggle)
					{
						this.Toggled = !this.Toggled;
					}
					return true;
				}
			}
			return false;
		}
	}
}