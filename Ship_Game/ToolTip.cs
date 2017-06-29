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
			TipTimer = 0; //Changed to 0 from 50. tooltips were taking a long time to come up.
            LastWhich = -1;
		}

		public ToolTip()
		{
		}

        private static void SpawnTooltip(string intext, int toolTipId, string hotkey, int timer = 30)
        {
            Hotkey = hotkey;
            MouseState state = Mouse.GetState();

            if (toolTipId >= 0)
            {
                var tooltip = ResourceManager.GetToolTip(toolTipId);
                intext = Localizer.Token(tooltip.Data);
                Ti     = tooltip.Title;
            }
            else
            {
                Ti = "";
            }

            Text = HelperFunctions.ParseText(Fonts.Arial12Bold, intext, 200f);

            Vector2 pos  = new Vector2(state.X, state.Y);
            Vector2 size = Fonts.Arial12Bold.MeasureString(Text);
            var tipRect = new Rectangle((int)pos.X  + 10, (int)pos.Y  + 10, 
                                        (int)size.X + 20, (int)size.Y + 10);

            if (tipRect.X + tipRect.Width > Game1.Instance.RenderWidth)
                tipRect.X = tipRect.X - (tipRect.Width + 10);
            while (tipRect.Y + tipRect.Height > Game1.Instance.RenderHeight)
                tipRect.Y = tipRect.Y - 1;

            if (TextLast != Text)
            {
                TipTimer = timer;
                TextLast = Text;
            }
            Rect = tipRect;
        }

		public static void CreateTooltip(string intext)
		{
            SpawnTooltip(intext, 0, "");
		}

		public static void CreateTooltip(string intext, string hotKey)
		{
		    SpawnTooltip(intext, 0, hotKey, 5);
		}

		public static void CreateTooltip(int which)
		{
		    SpawnTooltip("", which, "");
		}

		public static void CreateTooltip(int which, string hotKey)
		{
		    SpawnTooltip("", which, hotKey);
		}

		public static void Draw(SpriteBatch spriteBatch)
		{
			TipTimer = TipTimer - 1;
			if (TipTimer <= 0)
			{
				TipTimer = 0;
			}
			float alpha = 210f - 210f * TipTimer / 20f;
			if (TipTimer < 20 && Text != null)
			{
				var sel = new Selector(Rect, new Color(0, 0, 0, (byte)alpha));
				sel.Draw(spriteBatch);
				var textpos = new Vector2(Rect.X + 10, Rect.Y + 5);
				alpha = 255f - 255f * TipTimer / 30f;

                var color = new Color(255, 239, 208, (byte)alpha);
				if (!string.IsNullOrEmpty(Hotkey))
				{
					Vector2 hotkeypos = textpos;

                    string title = Localizer.Token(2300) + ": ";

				    spriteBatch.DrawString(Fonts.Arial12Bold, title, textpos, color);
					hotkeypos.X = hotkeypos.X + Fonts.Arial12Bold.MeasureString(title).X;
                    var gold = new Color(Color.Gold, (byte)alpha);
				    spriteBatch.DrawString(Fonts.Arial12Bold, Hotkey, hotkeypos, gold);
					textpos.Y = textpos.Y + Fonts.Arial12Bold.LineSpacing * 2;
				}
			    spriteBatch.DrawString(Fonts.Arial12Bold, Text, textpos, color);
			}
			if (Text == null)
			{
				TipTimer = 30;
			}
			Text = null;
		}
	}
}