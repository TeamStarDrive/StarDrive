using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    //[Obsolete("Use UIButton for showing buttons")]
	public sealed class TexturedButton
	{
		public Rectangle r;

		public int LocalizerTip;

		public string Action = "";

		public bool Hover;

		private string tPath;

		private string hPath;

		public string Hotkey = "";

		public Color BaseColor = Color.White;

		public TexturedButton(Rectangle r, string TexturePath, string HoverPath, string PressPath)
		{
			tPath = TexturePath;
			hPath = HoverPath;
			this.r = r;
		}

		public void Draw(SpriteBatch batch)
		{
			if (Hover)
			{
				batch.Draw(ResourceManager.Texture(hPath), r, Color.White);
			}
            else
            {
                batch.Draw(ResourceManager.Texture(tPath), r, Color.White);
            }
        }

		public bool HandleInput(InputState input)
		{
			if (!r.HitTest(input.CursorPosition))
			{
				Hover = false;
			}
			else
			{
				Hover = true;
				if (LocalizerTip != 0)
				{
					if (string.IsNullOrEmpty(Hotkey))
					{
						ToolTip.CreateTooltip(Localizer.Token(LocalizerTip), Hotkey);
					}
					else
					{
						ToolTip.CreateTooltip(Localizer.Token(LocalizerTip));
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