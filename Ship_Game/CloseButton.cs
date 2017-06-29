using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class CloseButton
	{
		private Rectangle rect;

		private bool Hover;

		public CloseButton(Rectangle r)
		{
			this.rect = r;
		}

		public void Draw(ScreenManager screenManager)
		{
			if (this.Hover)
			{
				screenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/Close_Hover"], this.rect, Color.White);
				return;
			}
			screenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/Close_Normal"], this.rect, Color.White);
		}

		public bool HandleInput(InputState input)
		{
			if (!this.rect.HitTest(input.CursorPosition))
			{
				this.Hover = false;
			}
			else
			{
				this.Hover = true;
				ToolTip.CreateTooltip("Exit Screen");
				if (input.InGameSelect)
				{
					return true;
				}
			}
			return false;
		}
	}
}