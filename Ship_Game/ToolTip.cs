using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public struct ToolTipText
    {
        public int Id; // Tooltip ID
        public string Text; // custom text

        public static readonly ToolTipText None = new ToolTipText();

        public bool IsValid => Id > 0 || !string.IsNullOrEmpty(Text);

        public static implicit operator ToolTipText(int id)
        {
            return new ToolTipText{ Id = id };
        }
        
        public static implicit operator ToolTipText(string text)
        {
            return new ToolTipText{ Text = text };
        }
    }

    public sealed class ToolTip
    {
        public int TIP_ID; // Serialized from: Tooltips.xml
        public int Data; // Serialized from: Tooltips.xml
        public string Title; // Serialized from: Tooltips.xml

        static Rectangle Rect;
        static string Text;
        static string TextLast;
        static string HotKey;
        static float TipTimer;
        static bool HoldTip;
        static bool AlwaysShow;
        static float MaxTipTime = 5;

        public static void ShipYardArcTip()
            => CreateTooltip("Shift for fine tune\nAlt for previous arcs");

        public static void PlanetLandingSpotsTip(string locationText, int spots)
            => CreateTooltip($"{locationText}\n{spots} Landing Spots", null, alwaysShow:true);

        /**
         * @todo tooltip issues
         * Main issue here. 
         * this class doesnt play well with the uielementv2 process.
         * 
         * so that several places are creating tooltips here in an unencapsulated way.
         * 
         * as far as i can tell... also the tooltip rectangle isnt right.
         */
        static void SpawnTooltip(ToolTipText tip, string hotKey, Vector2? position = null, bool alwaysShow = false)
        {
            HotKey = hotKey;
            AlwaysShow = alwaysShow;

            string inText = tip.Text;
            if (tip.Id > 0)
            {
                ToolTip tooltip = ResourceManager.GetToolTip(tip.Id);
                if (tooltip != null)
                {
                    inText = Localizer.Token(tooltip.Data);
                }
                else if (inText.IsEmpty()) // try to recover.. somehow
                {
                    inText = Localizer.Token(tip.Id);
                }
            }

            if (inText.IsEmpty())
            {
                Log.Error($"Invalid Tooltip: tip.Id={tip.Id} tip.Text={tip.Text}");
                return;
            }

            string text = Fonts.Arial12Bold.ParseText(inText, 200f);
            
            if (TipTimer > 0 && Text == text)
            {
                HoldTip = true;
                return;
            }
            
            Text = text;
            
            Vector2 pos = position ?? GameBase.ScreenManager.input.CursorPosition;
            Vector2 size = Fonts.Arial12Bold.MeasureString(hotKey.NotEmpty() ? $"{Text}\n\n{hotKey}" : Text);
            var tipRect = new Rectangle((int) pos.X + 10, (int) pos.Y + 10,
                                        (int) size.X + 20, (int) size.Y + 10);

            if (tipRect.X + tipRect.Width > GameBase.ScreenWidth)
                tipRect.X -= (tipRect.Width + 10);

            while (tipRect.Y + tipRect.Height > GameBase.ScreenHeight)
                tipRect.Y -= 1;

            if (alwaysShow || TextLast != Text)
            {
                TipTimer = MaxTipTime;
                TextLast = Text;
            }
            Rect = tipRect;
        }

        public static void CreateTooltip(ToolTipText tip, Vector2? position, bool alwaysShow)
        {
            SpawnTooltip(tip, "", position: position, alwaysShow: alwaysShow);
        }

        public static void CreateTooltip(ToolTipText tip, string hotKey)
        {
            SpawnTooltip(tip, hotKey);
        }

        public static void CreateTooltip(ToolTipText tip)
        {
            SpawnTooltip(tip, "");
        }


        static float FadeInTimer  => MaxTipTime * 0.75f;
        static float FadeOutTimer => MaxTipTime * 0.25f;

        static bool UpdateCurrentTip()
        {
            float elapsedTime = (float)GameBase.Base.GameTime.ElapsedGameTime.TotalSeconds;
            if (TipTimer <= 0)
                return false;

            TipTimer = Math.Max(TipTimer - elapsedTime, 0);
            if (!AlwaysShow && MaxTipTime - TipTimer < 0.5f)
                return false;

            if (HoldTip && TipTimer  < FadeOutTimer)                
                TipTimer = FadeOutTimer;

            HoldTip = false;
            if (TipTimer <= 0 || Text == null)
            {
                if (Text == null)
                    TipTimer = MaxTipTime;
                if (TipTimer <= 0)
                    Text = null;
                return false;
            }
            return true;
        }

        public static void Draw(SpriteBatch batch)
        {            
            if (UpdateCurrentTip() == false)
                return;

            float alpha = 255;
            if (TipTimer < FadeOutTimer)
                alpha = 255f * TipTimer / FadeOutTimer;
            else if (TipTimer > FadeInTimer)
                alpha = 255f * ((MaxTipTime - TipTimer) / (MaxTipTime - FadeInTimer));

            var textPos = new Vector2(Rect.X + 10, Rect.Y + 5);
            var sel = new Selector(Rect, new Color(Color.Black, (byte)alpha),  alpha);            
            sel.Draw(batch);

            var textColor = new Color(255, 239, 208, (byte) alpha);
            if (HotKey.NotEmpty())
            {
                string title = Localizer.Token(2300) + ": ";

                batch.DrawString(Fonts.Arial12Bold, title, textPos, textColor);

                Vector2 hotKey = textPos;
                hotKey.X += Fonts.Arial12Bold.MeasureString(title).X;

                batch.DrawString(Fonts.Arial12Bold, HotKey, hotKey, new Color(Color.Gold, (byte)alpha));
                textPos.Y += Fonts.Arial12Bold.LineSpacing * 2;
            }

            batch.DrawString(Fonts.Arial12Bold, Text, textPos, textColor);
        }
    }
}