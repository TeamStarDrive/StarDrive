using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class SkinnableButton
	{
		public Rectangle r;

		public object ReferenceObject;

		public string Action = "";

		public bool IsToggle = true;

		public bool Toggled;

		public bool Hover;

		private string tPath;

		public int WhichToolTip;

		public bool HasToolTip;

		public string SecondSkin;

		public Color HoverColor = Color.White;

		public Color BaseColor = Color.White;

		private Color ToggleColor = new Color(33, 26, 18);

		public SkinnableButton(Rectangle r, string TexturePath)
		{
			this.tPath = TexturePath;
			this.r = r;
		}

		public void Draw(ScreenManager screenManager)
		{
			if (this.Toggled)
			{
				Primitives2D.FillRectangle(screenManager.SpriteBatch, this.r, this.ToggleColor);
			}
			screenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.tPath], this.r, (this.Hover ? this.HoverColor : this.BaseColor));
			if (this.SecondSkin != null)
			{
				if (this.Toggled)
				{
					Rectangle secondRect = new Rectangle(this.r.X + this.r.Width / 2 - ResourceManager.TextureDict[this.SecondSkin].Width / 2, this.r.Y + this.r.Height / 2 - ResourceManager.TextureDict[this.SecondSkin].Height / 2, ResourceManager.TextureDict[this.SecondSkin].Width, ResourceManager.TextureDict[this.SecondSkin].Height);
					screenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.SecondSkin], secondRect, Color.White);
					return;
				}
				Rectangle secondRect0 = new Rectangle(this.r.X + this.r.Width / 2 - ResourceManager.TextureDict[this.SecondSkin].Width / 2, this.r.Y + this.r.Height / 2 - ResourceManager.TextureDict[this.SecondSkin].Height / 2, ResourceManager.TextureDict[this.SecondSkin].Width, ResourceManager.TextureDict[this.SecondSkin].Height);
				screenManager.SpriteBatch.Draw(ResourceManager.TextureDict[this.SecondSkin], secondRect0, (this.Hover ? Color.LightGray : Color.Black));
			}
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
					AudioManager.PlayCue("sd_ui_accept_alt3");
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