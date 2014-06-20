using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ToolTip
	{
		public int TIP_ID;

		public int Data;

		public string Title;

		public static Rectangle r;

		public static string text;

		public static string ti;

		public static string textLast;

		public static string Hotkey;

		public static int TipTimer;

		public static int lastWhich;

		static ToolTip()
		{
			ToolTip.Hotkey = "";
			ToolTip.TipTimer = 50;
			ToolTip.lastWhich = -1;
		}

		public ToolTip()
		{
		}

		public static void CreateTooltip(string intext, Ship_Game.ScreenManager ScreenManager)
		{
			ToolTip.Hotkey = "";
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 Position = new Vector2(x, (float)state.Y);
			ToolTip.text = HelperFunctions.parseText(Fonts.Arial12Bold, intext, 200f);
			float width = Fonts.Arial12Bold.MeasureString(ToolTip.text).X;
			float height = Fonts.Arial12Bold.MeasureString(ToolTip.text).Y;
			Rectangle tipRect = new Rectangle((int)Position.X + 10, (int)Position.Y + 10, (int)width + 20, (int)height + 10);
			if (tipRect.X + tipRect.Width > ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth)
			{
				tipRect.X = tipRect.X - (tipRect.Width + 10);
			}
			while (tipRect.Y + tipRect.Height > ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
			{
				tipRect.Y = tipRect.Y - 1;
			}
			if (ToolTip.textLast != ToolTip.text)
			{
				ToolTip.TipTimer = 50;
				ToolTip.textLast = ToolTip.text;
			}
			ToolTip.r = tipRect;
			ToolTip.ti = "";
		}

		public static void CreateTooltip(string intext, Ship_Game.ScreenManager ScreenManager, string Hotkey)
		{
			ToolTip.Hotkey = Hotkey;
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 Position = new Vector2(x, (float)state.Y);
			ToolTip.text = HelperFunctions.parseText(Fonts.Arial12Bold, intext, 200f);
			float width = Fonts.Arial12Bold.MeasureString(ToolTip.text).X;
			float height = Fonts.Arial12Bold.MeasureString(ToolTip.text).Y + (float)(Fonts.Arial12Bold.LineSpacing * 2);
			Rectangle tipRect = new Rectangle((int)Position.X + 10, (int)Position.Y + 10, (int)width + 20, (int)height + 10);
			if (tipRect.X + tipRect.Width > ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth)
			{
				tipRect.X = tipRect.X - (tipRect.Width + 10);
			}
			while (tipRect.Y + tipRect.Height > ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
			{
				tipRect.Y = tipRect.Y - 1;
			}
			if (ToolTip.textLast != ToolTip.text)
			{
				ToolTip.TipTimer = 50;
				ToolTip.textLast = ToolTip.text;
			}
			ToolTip.r = tipRect;
			ToolTip.ti = "";
		}

		public static void CreateTooltip(int which, Ship_Game.ScreenManager ScreenManager)
		{
			ToolTip.Hotkey = "";
			if (which != ToolTip.lastWhich)
			{
				ToolTip.TipTimer = 50;
				ToolTip.lastWhich = which;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 Position = new Vector2(x, (float)state.Y);
			string text = Localizer.Token(ResourceManager.ToolTips[which].Data);
			text = HelperFunctions.parseText(Fonts.Arial12Bold, text, 200f);
			float width = Fonts.Arial12Bold.MeasureString(text).X;
			float height = Fonts.Arial12Bold.MeasureString(text).Y;
			Rectangle tipRect = new Rectangle((int)Position.X + 10, (int)Position.Y + 10, (int)width + 20, (int)height + 10);
			if (tipRect.X + tipRect.Width > ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth)
			{
				tipRect.X = tipRect.X - (tipRect.Width + 10);
			}
			while (tipRect.Y + tipRect.Height > ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
			{
				tipRect.Y = tipRect.Y - 1;
			}
			ToolTip.r = tipRect;
			ToolTip.text = text;
			ToolTip.ti = ResourceManager.ToolTips[which].Title;
		}

		public static void CreateTooltip(int which, Ship_Game.ScreenManager ScreenManager, string Hotkey)
		{
			ToolTip.Hotkey = Hotkey;
			if (which != ToolTip.lastWhich)
			{
				ToolTip.TipTimer = 50;
				ToolTip.lastWhich = which;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 Position = new Vector2(x, (float)state.Y);
			string text = Localizer.Token(ResourceManager.ToolTips[which].Data);
			text = HelperFunctions.parseText(Fonts.Arial12Bold, text, 200f);
			float width = Fonts.Arial12Bold.MeasureString(text).X;
			float height = Fonts.Arial12Bold.MeasureString(text).Y + (float)(Fonts.Arial12Bold.LineSpacing * 2);
			Rectangle tipRect = new Rectangle((int)Position.X + 10, (int)Position.Y + 10, (int)width + 20, (int)height + 10);
			if (tipRect.X + tipRect.Width > ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth)
			{
				tipRect.X = tipRect.X - (tipRect.Width + 10);
			}
			while (tipRect.Y + tipRect.Height > ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
			{
				tipRect.Y = tipRect.Y - 1;
			}
			ToolTip.r = tipRect;
			ToolTip.text = text;
			ToolTip.ti = ResourceManager.ToolTips[which].Title;
		}

		public static void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			ToolTip.TipTimer = ToolTip.TipTimer - 1;
			if (ToolTip.TipTimer <= 0)
			{
				ToolTip.TipTimer = 0;
			}
			float alpha = 210f - 210f * (float)ToolTip.TipTimer / 20f;
			if (ToolTip.TipTimer < 20 && ToolTip.text != null)
			{
				Selector sel = new Selector(ScreenManager, ToolTip.r, new Color(0, 0, 0, (byte)alpha));
				sel.Draw();
				Vector2 textpos = new Vector2((float)(ToolTip.r.X + 10), (float)(ToolTip.r.Y + 5));
				alpha = 255f - 255f * (float)ToolTip.TipTimer / 30f;
				if (ToolTip.Hotkey != "")
				{
					Vector2 hotkeypos = textpos;
					ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(2300), ": "), textpos, new Color(255, 239, 208, (byte)alpha));
					hotkeypos.X = hotkeypos.X + Fonts.Arial12Bold.MeasureString(string.Concat(Localizer.Token(2300), ": ")).X;
					ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ToolTip.Hotkey, hotkeypos, new Color(Color.Gold, (byte)alpha));
					textpos.Y = textpos.Y + (float)(Fonts.Arial12Bold.LineSpacing * 2);
				}
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ToolTip.text, textpos, new Color(255, 239, 208, (byte)alpha));
			}
			if (ToolTip.text == null)
			{
				ToolTip.TipTimer = 50;
			}
			ToolTip.text = null;
		}
	}
}