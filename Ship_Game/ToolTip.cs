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
        public static float TipTimer;
        public static int LastWhich;
        private static bool HoldTip;

        static ToolTip()
        {
            Hotkey = "";
            TipTimer = 0; //Changed to 0 from 50. tooltips were taking a long time to come up.
            LastWhich = -1;
        }

        public ToolTip()
        {
        }
        public static void ShipYardArcTip() => CreateTooltip("Shift for fine tune\nAlt for previous arcs");
        /* @todo tooltip issues
  * Main issue here. 
  * this class doesnt play well with the uielementv2 process.
  * 
  * so that several places are creating tooltips here in an unencapsulated way.
  * 
  * as far as i can tell... also the tooltip rectangle isnt right.
  */
        private static void SpawnTooltip(string intext, int toolTipId, string hotkey, int timer = 6, bool holdTip = false, Vector2? position = null)
        {
            Hotkey = hotkey;
            HoldTip = holdTip;
            MouseState state = Mouse.GetState();

            if (toolTipId >= 0)
            {
                var tooltip = ResourceManager.GetToolTip(toolTipId);
                if (tooltip != null)
                {
                    intext = Localizer.Token(tooltip.Data);
                    Ti     = tooltip.Title;
                }
                else if (intext.IsEmpty()) // try to recover.. somehow
                {
                    intext = Localizer.Token(toolTipId);
                    Ti = "";
                }
            }
            else
            {
                Ti = "";
            }

            Text = HelperFunctions.ParseText(Fonts.Arial12Bold, intext, 200f);

            Vector2 pos  = position ??  new Vector2(state.X, state.Y);
            Vector2 size;
            size = Fonts.Arial12Bold.MeasureString(hotkey.NotEmpty() ? $"{Text}\n\n{hotkey}" : Text);
            var tipRect = new Rectangle((int)pos.X  + 10, (int)pos.Y  + 10, 
                                        (int)size.X + 20, (int)size.Y + 10);

            if (tipRect.X + tipRect.Width > Game1.Instance.ScreenWidth)
                tipRect.X = tipRect.X - (tipRect.Width + 10);
            while (tipRect.Y + tipRect.Height > Game1.Instance.ScreenHeight)
                tipRect.Y = tipRect.Y - 1;

            if (TextLast != Text)
            {
                TipTimer = timer;
                TextLast = Text;
            }
            Rect = tipRect;
        }

        public static void CreateTooltip(string intext, Vector2? position = null)
        {
            SpawnTooltip(intext, -1, "", position: position);
        }

        public static void CreateTooltip(string intext, string hotKey)
        {
            SpawnTooltip(intext, -1, hotKey);
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
            float elaspsedTime = (float)Game1.Instance.GameTime.ElapsedGameTime.TotalSeconds;
            TipTimer = Math.Max(TipTimer - elaspsedTime, 0f);
            if (TipTimer > 5.4f || Text == null)
            {
                if (Text == null)
                {
                    TipTimer = 6;
                }
                if (TipTimer <= 0)
                    Text = null;
                return;
            }

            float alpha =255;
            if (TipTimer < 3)
            {
                if (HoldTip)
                    TipTimer = 4.7f;
                alpha = 255f * TipTimer / 3f;
            }
            else if (TipTimer > 4.7f)
                alpha = 255f - 255f * (TipTimer -4.7f);
            var textpos = new Vector2(Rect.X + 10, Rect.Y + 5);
            var sel = new Selector(Rect, new Color(Color.Black, (byte)alpha),  alpha);            
            sel.Draw(spriteBatch);
            var color = new Color(255, 239, 208, (byte) alpha);
            if (Hotkey.NotEmpty())
            {
                Vector2 hotkeypos = textpos;

                string title = Localizer.Token(2300) + ": ";

                spriteBatch.DrawString(Fonts.Arial12Bold, title, textpos, color);
                hotkeypos.X = hotkeypos.X + Fonts.Arial12Bold.MeasureString(title).X;
                var gold = new Color(Color.Gold, (byte) alpha);
                spriteBatch.DrawString(Fonts.Arial12Bold, Hotkey, hotkeypos, gold);
                textpos.Y = textpos.Y + Fonts.Arial12Bold.LineSpacing * 2;
            }
            spriteBatch.DrawString(Fonts.Arial12Bold, Text, textpos, color);

            
        }
    }
}