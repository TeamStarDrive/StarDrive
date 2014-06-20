using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ToggleButton
	{
		public Rectangle r;

		public object ReferenceObject;

		public string Action = "";

		public bool Active;

		public bool Hover;

		private string ActivePath;

		private string InactivePath;

		private string HoverPath;

		private string PressPath;

		private string IconPath;

		public int WhichToolTip;

		public bool HasToolTip;

		public Color BaseColor = Color.White;

		private bool Pressed;

		private Color ToggleColor = new Color(33, 26, 18);

		public ToggleButton(Rectangle r, string ActivePath, string InactivePath, string HoverPath, string PressPath, string IconPath)
		{
			this.ActivePath = ActivePath;
			this.InactivePath = InactivePath;
			this.HoverPath = HoverPath;
			this.PressPath = PressPath;
			this.IconPath = IconPath;
			this.r = r;
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			if (this.Pressed)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.PressPath], this.r, Color.White);
			}
			else if (this.Hover)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.HoverPath], this.r, Color.White);
			}
			else if (this.Active)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ActivePath], this.r, Color.White);
			}
			else if (!this.Active)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.InactivePath], this.r, Color.White);
			}
			if (!ResourceManager.TextureDict.ContainsKey(this.IconPath))
			{
				Vector2 wordPos = new Vector2((float)(this.r.X + 12) - Fonts.Arial12Bold.MeasureString(this.IconPath).X / 2f, (float)(this.r.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2));
				if (this.Active)
				{
					ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.IconPath, wordPos, Color.White);
					return;
				}
				if (!this.Active)
				{
					ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.IconPath, wordPos, Color.Gray);
				}
			}
			else
			{
				if (this.Active)
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat(this.IconPath, "_active")], this.r, Color.White);
					return;
				}
				if (!this.Active)
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.IconPath], this.r, Color.White);
					return;
				}
			}
		}

		public void DrawIconResized(Ship_Game.ScreenManager ScreenManager)
		{
			if (this.Pressed)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.PressPath], this.r, Color.White);
			}
			else if (this.Hover)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.HoverPath], this.r, Color.White);
			}
			else if (this.Active)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.ActivePath], this.r, Color.White);
			}
			else if (!this.Active)
			{
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.InactivePath], this.r, Color.White);
			}
			if (!ResourceManager.TextureDict.ContainsKey(this.IconPath))
			{
				Vector2 wordPos = new Vector2((float)(this.r.X + 11) - Fonts.Arial12Bold.MeasureString(this.IconPath).X / 2f, (float)(this.r.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
				if (this.Active)
				{
					ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.IconPath, wordPos, Color.White);
					return;
				}
				if (!this.Active)
				{
					ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.IconPath, wordPos, Color.Gray);
				}
			}
			else
			{
				Rectangle iconRect = new Rectangle(this.r.X + this.r.Width / 2 - ResourceManager.TextureDict[this.IconPath].Width / 2, this.r.Y + this.r.Height / 2 - ResourceManager.TextureDict[this.IconPath].Height / 2, ResourceManager.TextureDict[this.IconPath].Width, ResourceManager.TextureDict[this.IconPath].Height);
				if (this.Active)
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat(this.IconPath, "_active")], iconRect, Color.White);
					return;
				}
				if (!this.Active)
				{
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.IconPath], iconRect, Color.White);
					return;
				}
			}
		}

		public bool HandleInput(InputState input)
		{
			this.Pressed = false;
			if (!HelperFunctions.CheckIntersection(this.r, input.CursorPosition))
			{
				this.Hover = false;
			}
			else
			{
				if (!this.Hover)
				{
					AudioManager.PlayCue("sd_ui_mouseover");
				}
				this.Hover = true;
				if (input.CurrentMouseState.LeftButton == ButtonState.Pressed)
				{
					this.Pressed = true;
				}
				if (input.InGameSelect)
				{
					return true;
				}
			}
			return false;
		}
	}
}