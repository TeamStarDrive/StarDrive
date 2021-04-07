using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    //[Obsolete("Use UIButton for showing buttons")]
	public sealed class TexturedButton
	{
		public Rectangle r;
		public LocalizedText Tooltip;
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
			SubTexture tex = ResourceManager.Texture(Hover ? hPath : tPath);
			batch.Draw(tex, r, Color.White);
        }

		public bool HandleInput(InputState input)
		{
			Hover = r.HitTest(input.CursorPosition);
			if (Hover)
            {
				if (Tooltip.IsValid)
					ToolTip.CreateTooltip(Tooltip, Hotkey);

				if (input.InGameSelect)
					return true;
            }
			return false;
		}
	}
}