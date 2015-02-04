using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class TexturedButton
	{
		public Rectangle r;

		public int LocalizerTip;

		public object ReferenceObject;

		public string Action = "";

		public bool IsToggle = true;

		public bool Toggled;

		public bool Hover;

		private string tPath;

		private string hPath;

		private string pPath;

		public int WhichToolTip;

		public bool HasToolTip;

		public string Hotkey = "";

		public Color BaseColor = Color.White;

		private Color ToggleColor = new Color(33, 26, 18);

		public TexturedButton(Rectangle r, string TexturePath, string HoverPath, string PressPath)
		{
			this.tPath = TexturePath;
			this.hPath = HoverPath;
			this.pPath = PressPath;
			this.r = r;
		}

		public void Draw(ScreenManager screenManager)
		{
			if (this.Hover)
			{
				screenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.hPath], this.r, Color.White);
				return;
			}
			screenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.tPath], this.r, Color.White);
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
				if (this.LocalizerTip != 0)
				{
					if (string.IsNullOrEmpty(this.Hotkey))
					{
						ToolTip.CreateTooltip(Localizer.Token(this.LocalizerTip), Ship.universeScreen.ScreenManager, this.Hotkey);
					}
					else
					{
						ToolTip.CreateTooltip(Localizer.Token(this.LocalizerTip), Ship.universeScreen.ScreenManager);
					}
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