using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class ToolTip
    {
        // minimum hover time until tip is shown
        const float TipShowTimePoint = 0.35f;

        // this provides a sort of grace period before the tip can be shown again
        const float TipReappearTimeDelay = 1.0f;

        // how much time after disappearing should we reset the tooltip completely?
        // (we forget about the reappear delay)
        const float TipResetTimeDelay = 3.0f;

        // minimum time a tip is shown, this includes fadeIn/stay/fadeOut
        const float TipTime = 1f;

        // how fast a tip fades in/out
        const float TipFadeInOutTime = 0.35f;

        // default width of a tool tip
        public const float DefaultWidth = 200;

        static readonly Array<TipItem> ActiveTips = new();

        public static void ShipYardArcTip()
            => CreateTooltip("Shift for fine tune\nAlt for previous arcs");

        public static void PlanetLandingSpotsTip(string locationText, int spots)
            => CreateTooltip($"{locationText}\n{spots} Landing Spots");

        // Allows a tool tip which floats regardless of hovering on the position
        public static void CreateFloatingText(in LocalizedText tip, string hotKey, Vector2? position, float lifeTime) 
            => CreateTooltip(tip, hotKey, position, lifeTime);

        /// <summary>
        /// Sets the currently active Tooltip
        /// </summary>
        /// <param name="tip">Tooltip text</param>
        /// <param name="hotKey">Hotkey text</param>
        /// <param name="position">Position hint, otherwise cursor will be used</param>
        /// <param name="minShowTime">Minimum time to show this Tooltip, regardless of being hovered</param>
        /// <param name="maxWidth">Maximum width for the tooltip</param>
        public static void CreateTooltip(in LocalizedText tip, string hotKey, Vector2? position,
                                         float minShowTime = 0, float maxWidth = DefaultWidth)
        {
            string rawText = tip.Text;
            if (rawText.IsEmpty())
            {
                Log.Error($"Invalid Tooltip: tip.Id={tip.Id} tip.Text={tip.Text}");
                return;
            }

            TipItem tipItem = ActiveTips.Find(t => t.RawText == rawText);
            if (tipItem != null)
            {
                tipItem.HoveredThisFrame = true;
                return;
            }

            tipItem = new(minShowTime);
            ActiveTips.Add(tipItem);

            tipItem.RawText = rawText;
            tipItem.Text = Fonts.Arial12Bold.ParseText(rawText, maxWidth);
            tipItem.HotKey = hotKey;

            Vector2 size = Fonts.Arial12Bold.MeasureString(tipItem.Text);
            if (hotKey.NotEmpty()) // Reserve space for HotKey as well:
                size.Y += Fonts.Arial12Bold.LineSpacing * 2;
            
            Vector2 pos = position ?? GameBase.ScreenManager.input.CursorPosition;
            var tipRect = new Rectangle((int)pos.X  + 10, (int)pos.Y  + 10,
                                        (int)size.X + 20, (int)size.Y + 10);

            if (tipRect.X + tipRect.Width > GameBase.ScreenWidth)
                tipRect.X -= (tipRect.Width + 10);

            while (tipRect.Y + tipRect.Height > GameBase.ScreenHeight)
                tipRect.Y -= 1;

            tipItem.Rect = tipRect;
        }

        public static void CreateTooltip(in LocalizedText tip, string hotKey, float maxWidth = DefaultWidth) => CreateTooltip(tip, hotKey, null, maxWidth: maxWidth);
        public static void CreateTooltip(in LocalizedText tip, float maxWidth = DefaultWidth) => CreateTooltip(tip, "", null, maxWidth: maxWidth);
        
        // Clears the current tooltip (if any)
        public static void Clear()
        {
            ActiveTips.Clear();
        }

        class TipItem
        {
            public string RawText;
            public string Text;
            public string HotKey;
            public Rectangle Rect;
            public bool HoveredThisFrame = true;
            float MinShowTime; // Let the tip show regardless of being hovered on

            float LifeTime;
            bool Visible;

            public TipItem(float minShowTime)
            {
                MinShowTime = minShowTime;
            }

            // @return FALSE: tip died, TRUE: tip is OK
            public bool Update(float deltaTime)
            {
                bool hovered = HoveredThisFrame;
                if (MinShowTime <= 0f) HoveredThisFrame = false;
                MinShowTime -= deltaTime;

                // if tip is hovered, we increase its lifetime
                // when not hovered, we decrease the lifetime
                LifeTime += (hovered ? deltaTime : -deltaTime);
                LifeTime = Math.Min(LifeTime, TipTime);

                const float TipReappearTimePoint = TipShowTimePoint - TipReappearTimeDelay;
                const float TipResetTimePoint = TipReappearTimePoint - TipResetTimeDelay;
                if (LifeTime <= TipResetTimePoint)
                    return false; // tip died

                // if tooltip goes invisible,
                // set the lifetime so that the tip reappears at least with TipReappearTimeDelay
                if (Visible && LifeTime <= 0)
                {
                    Visible = false;
                    LifeTime = TipReappearTimePoint;
                    return true;
                }
                
                // when tooltip starts reappearing, make sure tip reappears with TipReappearTimeDelay
                if (hovered)
                {
                    LifeTime = Math.Max(LifeTime, TipReappearTimePoint);
                }

                if (!Visible && LifeTime > TipShowTimePoint) // tip can be shown
                {
                    LifeTime = 0.01f; // fix the lifetime so we get correct fade-in
                    Visible = true;
                }
                return true;
            }

            public void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                if (!Visible)
                    return;

                float alpha = (255 * LifeTime / TipFadeInOutTime).Clamped(0, 255);
                var textPos = new Vector2(Rect.X + 10, Rect.Y + 5);
                var sel = new Selector(Rect, new Color(Color.Black, (byte)alpha),  alpha);
                sel.Draw(batch, elapsed);

                var textColor = new Color(255, 239, 208, (byte) alpha);
                if (HotKey.NotEmpty())
                {
                    string title = Localizer.Token(GameText.Hotkey) + ": ";

                    batch.DrawString(Fonts.Arial12Bold, title, textPos, textColor);

                    Vector2 hotKey = textPos;
                    hotKey.X += Fonts.Arial12Bold.MeasureString(title).X;

                    batch.DrawString(Fonts.Arial12Bold, HotKey, hotKey, new Color(Color.Gold, (byte)alpha));
                    textPos.Y += Fonts.Arial12Bold.LineSpacing * 2;
                }

                batch.DrawString(Fonts.Arial12Bold, Text, textPos, textColor);
            }
        }

        public static void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            TipItem[] tips = ActiveTips.ToArray();
            if (tips.Length == 0)
                return;
            
            batch.SafeBegin();
            foreach (TipItem tipItem in tips)
            {
                if (tipItem.Update(elapsed.RealTime.Seconds))
                {
                    tipItem.Draw(batch, elapsed);
                }
                else // tip died
                {
                    ActiveTips.Remove(tipItem);
                }
            }
            batch.SafeEnd();
        }
    }
}
