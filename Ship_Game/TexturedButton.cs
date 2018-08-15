using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class TexturedButton
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
			tPath = TexturePath;
			hPath = HoverPath;
			pPath = PressPath;
			this.r = r;
		}

		public void Draw(ScreenManager screenManager)
		{
			if (Hover)
			{
				screenManager.SpriteBatch.Draw(ResourceManager.Texture(hPath), r, Color.White);
				return;
			}
			screenManager.SpriteBatch.Draw(ResourceManager.Texture(tPath), r, Color.White);
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