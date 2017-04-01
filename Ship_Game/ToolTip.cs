using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class ToolTip
	{
		public int TIP_ID;
		public int Data;
		public string Title;
		public static Rectangle Rect;
		public static string Text;
		public static string Ti;
		public static string TextLast;
		public static string Hotkey;
		public static int TipTimer;
		public static int LastWhich;

		static ToolTip()
		{
			Hotkey = "";
			TipTimer = 0;
			LastWhich = -1;
		}

		public ToolTip()
		{
		}

		public static void CreateTooltip(string intext, ScreenManager screenManager)
		{
			Hotkey = "";
			MouseState state = Mouse.GetState();
			Text = HelperFunctions.ParseText(Fonts.Arial12Bold, intext, 200f);

			Vector2 pos  = new Vector2(state.X, state.Y);
            Vector2 size = Fonts.Arial12Bold.MeasureString(Text);
			Rectangle tipRect = new Rectangle((int)pos.X + 10, (int)pos.Y + 10, 
                                              (int)size.X + 20, (int)size.Y + 10);

			if (tipRect.X + tipRect.Width > screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth)
				tipRect.X = tipRect.X - (tipRect.Width + 10);
			while (tipRect.Y + tipRect.Height > screenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
				tipRect.Y = tipRect.Y - 1;

			if (TextLast != Text)
			{
				TipTimer = 50;
				TextLast = Text;
			}
			Rect = tipRect;
			Ti = "";
		}

		public static void CreateTooltip(string intext, ScreenManager screenManager, string hotKey)
		{
			Hotkey = hotKey;
			MouseState state = Mouse.GetState();
			Text = HelperFunctions.ParseText(Fonts.Arial12Bold, intext, 200f);

			Vector2 pos = new Vector2(state.X, state.Y);
            Vector2 size = Fonts.Arial12Bold.MeasureString(Text);
			Rectangle tipRect = new Rectangle((int)pos.X + 10, (int)pos.Y + 10, 
                                              (int)size.X + 20, (int)size.Y + 10 + Fonts.Arial12Bold.LineSpacing * 2);

            var presParams = screenManager.GraphicsDevice.PresentationParameters;
			if (tipRect.X + tipRect.Width > presParams.BackBufferWidth)
				tipRect.X = tipRect.X - (tipRect.Width + 10);
			while (tipRect.Y + tipRect.Height > presParams.BackBufferHeight)
				tipRect.Y = tipRect.Y - 1;

			if (TextLast != Text)
			{
				TipTimer = 5;
				TextLast = Text;
			}
			Rect = tipRect;
			Ti = "";
		}

        // @todo This needs some refactoring, lots of inlined code
		public static void CreateTooltip(int which, ScreenManager screenManager)
		{
			Hotkey = "";
			if (which != LastWhich)
			{
				TipTimer = 50;
				LastWhich = which;
			}

			string txt = Localizer.Token(ResourceManager.GetToolTip(which).Data);
			txt = HelperFunctions.ParseText(Fonts.Arial12Bold, txt, 200f);

            MouseState state = Mouse.GetState();
			Vector2 pos  = new Vector2(state.X, state.Y);
            Vector2 size = Fonts.Arial12Bold.MeasureString(txt);
			Rectangle tipRect = new Rectangle((int)pos.X + 10, (int)pos.Y + 10, 
                                              (int)size.X + 20, (int)size.Y + 10);

            var presParams = screenManager.GraphicsDevice.PresentationParameters;
			if (tipRect.X + tipRect.Width > presParams.BackBufferWidth)
				tipRect.X = tipRect.X - (tipRect.Width + 10);
			while (tipRect.Y + tipRect.Height > presParams.BackBufferHeight)
				tipRect.Y = tipRect.Y - 1;

			Rect = tipRect;
			Text = txt;
			Ti = ResourceManager.GetToolTip(which).Title;
		}

		public static void CreateTooltip(int which, ScreenManager screenManager, string hotKey)
		{
			Hotkey = hotKey;
			if (which != LastWhich)
			{
				TipTimer = 50;
				LastWhich = which;
			}
			string txt = Localizer.Token(ResourceManager.GetToolTip(which).Data);
			txt = HelperFunctions.ParseText(Fonts.Arial12Bold, txt, 200f);

            
			MouseState state = Mouse.GetState();
			Vector2 pos = new Vector2(state.X, state.Y);
            Vector2 size = Fonts.Arial12Bold.MeasureString(txt);
			Rectangle tipRect = new Rectangle((int)pos.X + 10, (int)pos.Y + 10, 
                                              (int)size.X + 20, (int)size.Y + 10 + Fonts.Arial12Bold.LineSpacing * 2);

            var presParams = screenManager.GraphicsDevice.PresentationParameters;
			if (tipRect.X + tipRect.Width > presParams.BackBufferWidth)
				tipRect.X = tipRect.X - (tipRect.Width + 10);
			while (tipRect.Y + tipRect.Height > presParams.BackBufferHeight)
				tipRect.Y = tipRect.Y - 1;

			Rect = tipRect;
			Text = txt;
			Ti = ResourceManager.GetToolTip(which).Title;
		}

		public static void Draw(ScreenManager screenManager)
		{
			TipTimer = TipTimer - 1;
			if (TipTimer <= 0)
			{
				TipTimer = 0;
			}
			float alpha = 210f - 210f * TipTimer / 20f;
			if (TipTimer < 20 && Text != null)
			{
				Selector sel = new Selector(screenManager, Rect, new Color(0, 0, 0, (byte)alpha));
				sel.Draw();
				Vector2 textpos = new Vector2(Rect.X + 10, Rect.Y + 5);
				alpha = 255f - 255f * TipTimer / 30f;

                Color color = new Color(255, 239, 208, (byte)alpha);
				if (!string.IsNullOrEmpty(Hotkey))
				{
					Vector2 hotkeypos = textpos;

                    string title = Localizer.Token(2300) + ": ";

					screenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, title, textpos, color);
					hotkeypos.X = hotkeypos.X + Fonts.Arial12Bold.MeasureString(title).X;
                    Color gold = new Color(Color.Gold, (byte)alpha);
					screenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Hotkey, hotkeypos, gold);
					textpos.Y = textpos.Y + Fonts.Arial12Bold.LineSpacing * 2;
				}
				screenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Text, textpos, color);
			}
			if (Text == null)
			{
				TipTimer = 50;
			}
			Text = null;
		}
	}
}