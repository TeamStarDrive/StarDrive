using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ProgressBar
	{
		public Rectangle pBar;

		public float Progress;

		public float Max;

		public string color = "brown";

		private Rectangle Left;

		private Rectangle Right;

		private Rectangle Middle;

		private Rectangle gLeft;

		private Rectangle gRight;

		private Rectangle gMiddle;

		private bool Vertical;

		private Rectangle Top;

		private Rectangle Bot;

		//private Rectangle gTop;

		//private Rectangle gBot;

		public ProgressBar(Rectangle r)
		{
			this.pBar = r;
			this.Left = new Rectangle(r.X, r.Y, 7, 18);
			this.gLeft = new Rectangle(this.Left.X + 3, this.Left.Y + 3, 4, 12);
			this.Right = new Rectangle(r.X + r.Width - 7, r.Y, 7, 18);
			this.gRight = new Rectangle(this.Right.X - 3, this.Right.Y + 3, 4, 12);
			this.Middle = new Rectangle(r.X + 7, r.Y, r.Width - 14, 18);
			this.gMiddle = new Rectangle(this.Middle.X, this.Middle.Y + 3, this.Middle.Width, 12);
		}

		public ProgressBar(Rectangle r, bool bleh)
		{
			this.Vertical = true;
			this.pBar = r;
			this.Top = new Rectangle(r.X, r.Y, 18, 7);
			this.gLeft = new Rectangle(this.Left.X + 3, this.Left.Y + 3, 4, 12);
			this.Bot = new Rectangle(r.X, r.Y + r.Height - 7, 18, 7);
			this.gRight = new Rectangle(this.Right.X + 1, this.Right.Y + 3, 4, 12);
			this.Middle = new Rectangle(r.X, r.Y + 7, 18, r.Height - 14);
			this.gMiddle = new Rectangle(this.Middle.X, this.Middle.Y + 3, this.Middle.Width, 12);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			if (this.Vertical)
			{
				spriteBatch.Draw(ResourceManager.TextureDict["NewUI/progressbar_container_top"], this.Top, Color.White);
				spriteBatch.Draw(ResourceManager.TextureDict["NewUI/progressbar_container_mid_vert"], this.Middle, Color.White);
				spriteBatch.Draw(ResourceManager.TextureDict["NewUI/progressbar_container_bot"], this.Bot, Color.White);
				return;
			}
			if (this.Max > 0f)
			{
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("NewUI/progressbar_grd_", this.color, "_left")], this.gLeft, Color.White);
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("NewUI/progressbar_grd_", this.color, "_mid")], this.gMiddle, Color.White);
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("NewUI/progressbar_grd_", this.color, "_right")], this.gRight, Color.White);
				int MaskX = (int)((float)this.Progress / (float)this.Max * (float)this.pBar.Width + (float)this.pBar.X);
				int MaskW = this.pBar.Width - (int)((float)this.Progress / (float)this.Max * (float)this.pBar.Width);
				Rectangle Mask = new Rectangle(MaskX, this.pBar.Y, MaskW, 18);
				Primitives2D.FillRectangle(spriteBatch, Mask, Color.Black);
			}
			if (this.color != "brown")
			{
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("NewUI/progressbar_container_left_", this.color)], this.Left, Color.White);
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("NewUI/progressbar_container_mid_", this.color)], this.Middle, Color.White);
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("NewUI/progressbar_container_right_", this.color)], this.Right, Color.White);
			}
			else
			{
				spriteBatch.Draw(ResourceManager.TextureDict["NewUI/progressbar_container_left"], this.Left, Color.White);
				spriteBatch.Draw(ResourceManager.TextureDict["NewUI/progressbar_container_mid"], this.Middle, Color.White);
				spriteBatch.Draw(ResourceManager.TextureDict["NewUI/progressbar_container_right"], this.Right, Color.White);
			}
			Vector2 textPos = new Vector2((float)(this.Left.X + 7), (float)(this.Left.Y + this.Left.Height / 2 - Fonts.TahomaBold9.LineSpacing / 2));
			spriteBatch.DrawString(Fonts.TahomaBold9, string.Concat((int)this.Progress, "/", (int)this.Max), textPos, new Color(255, 239, 208));
		}

		public void DrawGrayed(SpriteBatch spriteBatch)
		{
			if (this.Vertical)
			{
				spriteBatch.Draw(ResourceManager.TextureDict["NewUI/progressbar_container_top"], this.Top, Color.DarkGray);
				spriteBatch.Draw(ResourceManager.TextureDict["NewUI/progressbar_container_mid_vert"], this.Middle, Color.DarkGray);
				spriteBatch.Draw(ResourceManager.TextureDict["NewUI/progressbar_container_bot"], this.Bot, Color.DarkGray);
				return;
			}
			if (this.Max > 0f)
			{
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("NewUI/progressbar_grd_", this.color, "_left")], this.gLeft, Color.DarkGray);
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("NewUI/progressbar_grd_", this.color, "_mid")], this.gMiddle, Color.DarkGray);
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("NewUI/progressbar_grd_", this.color, "_right")], this.gRight, Color.DarkGray);
				int MaskX = (int)((float)this.Progress / (float)this.Max * (float)this.pBar.Width + (float)this.pBar.X);
				int MaskW = this.pBar.Width - (int)((float)this.Progress / (float)this.Max * (float)this.pBar.Width);
				Rectangle Mask = new Rectangle(MaskX, this.pBar.Y, MaskW, 18);
				Primitives2D.FillRectangle(spriteBatch, Mask, Color.Black);
			}
			if (this.color != "brown")
			{
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("NewUI/progressbar_container_left_", this.color)], this.Left, Color.DarkGray);
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("NewUI/progressbar_container_mid_", this.color)], this.Middle, Color.DarkGray);
				spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("NewUI/progressbar_container_right_", this.color)], this.Right, Color.DarkGray);
			}
			else
			{
				spriteBatch.Draw(ResourceManager.TextureDict["NewUI/progressbar_container_left"], this.Left, Color.DarkGray);
				spriteBatch.Draw(ResourceManager.TextureDict["NewUI/progressbar_container_mid"], this.Middle, Color.DarkGray);
				spriteBatch.Draw(ResourceManager.TextureDict["NewUI/progressbar_container_right"], this.Right, Color.DarkGray);
			}
			Vector2 textPos = new Vector2((float)(this.Left.X + 7), (float)(this.Left.Y + this.Left.Height / 2 - Fonts.TahomaBold9.LineSpacing / 2));
			spriteBatch.DrawString(Fonts.TahomaBold9, string.Concat((int)this.Progress, "/", (int)this.Max), textPos, Color.DarkGray);
		}
	}
}