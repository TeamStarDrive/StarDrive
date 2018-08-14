using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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
        private static bool AlwaysShow;
        private static float MaxTipTime;

        static ToolTip()
        {
            Hotkey = "";
            TipTimer = 0; //Changed to 0 from 50. tooltips were taking a long time to come up.
            LastWhich = -1;
        }

        public static void ShipYardArcTip() => CreateTooltip("Shift for fine tune\nAlt for previous arcs");
        public static void PlanetLandingSpotsTip(string locationText, int spots) => CreateTooltip($"{locationText}\n{spots} Landing Spots",alwaysShow:true);

    /* @todo tooltip issues
* Main issue here. 
* this class doesnt play well with the uielementv2 process.
* 
* so that several places are creating tooltips here in an unencapsulated way.
* 
* as far as i can tell... also the tooltip rectangle isnt right.
*/
    private static void SpawnTooltip(string intext, int toolTipId, string hotkey, int timer = 6, bool holdTip = false, Vector2? position = null, bool alwaysShow = false)
        {    
            Hotkey = hotkey;
            //HoldTip = holdTip;
            MaxTipTime = timer;
            AlwaysShow = alwaysShow;
            MouseState state = Mouse.GetState();
            if (toolTipId >= 0)
            {
                var tooltip = ResourceManager.GetToolTip(toolTipId);
                if (tooltip != null)
                {
                    intext = Localizer.Token(tooltip.Data);
                    Ti = tooltip.Title;
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

            var text = Fonts.Arial12Bold.ParseText(intext, 200f);
            
            if (TipTimer > 0 && Text == text)
            {
                HoldTip = true;
                return;
            }
            
            Text = text;
            
            Vector2 pos = position ?? new Vector2(state.X, state.Y);
            Vector2 size;
            size = Fonts.Arial12Bold.MeasureString(hotkey.NotEmpty() ? $"{Text}\n\n{hotkey}" : Text);
            var tipRect = new Rectangle((int) pos.X + 10, (int) pos.Y + 10,
                (int) size.X + 20, (int) size.Y + 10);

            if (tipRect.X + tipRect.Width > Game1.Instance.ScreenWidth)
                tipRect.X = tipRect.X - (tipRect.Width + 10);
            while (tipRect.Y + tipRect.Height > Game1.Instance.ScreenHeight)
                tipRect.Y = tipRect.Y - 1;

            if (alwaysShow || TextLast != Text)
            {
                TipTimer = timer;
                TextLast = Text;
            }
            Rect = tipRect;
        }

        public static void CreateTooltip(string intext, Vector2? position = null, bool alwaysShow = false, bool holdTip = false)
        {
            SpawnTooltip(intext, -1, "", position: position,alwaysShow: alwaysShow, holdTip: holdTip);
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
            if (TipTimer <= 0) return;
            TipTimer = Math.Max(TipTimer - elaspsedTime, 0);
            if (!AlwaysShow && MaxTipTime - TipTimer < .5f)
                return;
            var fadeOutTimer = MaxTipTime * .25f;
            var fadeInTimer = MaxTipTime * .75f;
            //var state = Mouse.GetState();
            //HoldTip = Rect.HitTest(state.X, state.Y);

            
            if (HoldTip && TipTimer  < fadeOutTimer)                
                    TipTimer = fadeOutTimer;
            HoldTip = false;
            if (TipTimer <= 0 || Text == null)
            {
                if (Text == null)
                {
                    TipTimer = MaxTipTime;
                }
                if (TipTimer <= 0)
                    Text = null;
                return;
            }

            float alpha =255;
            if (TipTimer < fadeOutTimer)
            {              
                alpha = 255f * TipTimer / fadeOutTimer;
            }
            else if (TipTimer > fadeInTimer)
                alpha = 255f * ((MaxTipTime - TipTimer) / (MaxTipTime - fadeInTimer));
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