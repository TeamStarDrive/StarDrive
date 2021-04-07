using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

namespace Ship_Game
{
    [StarDataType]
    public sealed class ToolTip
    {
        public string NameId;            // Serialized from: ToolTips.yaml
        [StarData] public int Id;        // Serialized from: ToolTips.yaml
        [StarData] public string TextId; // Serialized from: ToolTips.yaml

        // minimum hover time until tip is shown
        const float TipShowTimePoint = 0.5f;

        // this provides a sort of grace period before the tip can be shown again
        const float TipReappearTimeDelay = 1.5f;

        // how much time after disappearing should we reset the tooltip completely?
        // (we forget about the reappear delay)
        const float TipResetTimeDelay = 5.0f;

        // minimum time a tip is shown, this includes fadeIn/stay/fadeOut
        const float TipTime = 1f;

        // how fast a tip fades in/out
        const float TipFadeInOutTime = 0.35f;

        static readonly Array<TipItem> ActiveTips = new Array<TipItem>();

        public static void ShipYardArcTip()
            => CreateTooltip("Shift for fine tune\nAlt for previous arcs");

        public static void PlanetLandingSpotsTip(string locationText, int spots)
            => CreateTooltip($"{locationText}\n{spots} Landing Spots");

        public static int AutoTaxToolTip => 7040;

        // Allows a tool tip which floats regardless of hovering on the position
        public static void CreateFloatingText(in ToolTipText tip, string hotKey, Vector2? position, float lifeTime) 
            => CreateTooltip(tip, hotKey, position, lifeTime);

        /**
         * Sets the currently active ToolTip
         */
        public static void CreateTooltip(in ToolTipText tip, string hotKey, Vector2? position, float forceNoneHoverTime = 0)
        {
            string rawText = tip.LocalizedText;
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

            tipItem = new TipItem(forceNoneHoverTime);
            ActiveTips.Add(tipItem);

            tipItem.RawText = rawText;
            tipItem.Text = Fonts.Arial12Bold.ParseText(rawText, 200f);
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

        public static void CreateTooltip(in ToolTipText tip, string hotKey) => CreateTooltip(tip, hotKey, null);
        public static void CreateTooltip(in ToolTipText tip) => CreateTooltip(tip, "", null);
        
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
            float ForceNonHoverTime; // Let the tip show regardless of being hovered on

            float LifeTime;
            bool Visible;

            public TipItem(float forceNonHoverTime)
            {
                ForceNonHoverTime = forceNonHoverTime;
            }

            // @return FALSE: tip died, TRUE: tip is OK
            public bool Update(float deltaTime)
            {
                bool hovered = HoveredThisFrame;
                if (ForceNonHoverTime < 0)
                    HoveredThisFrame = false;

                // if tip is hovered, we increase its lifetime
                // when not hovered, we decrease the lifetime
                LifeTime += (hovered ? deltaTime : -deltaTime);
                LifeTime = Math.Min(LifeTime, TipTime);

                ForceNonHoverTime -= deltaTime;

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
            
            batch.Begin();
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
            batch.End();
        }
        
        static readonly Array<ToolTip> ToolTips = new Array<ToolTip>();
        static readonly HashSet<int> MissingTooltips = new HashSet<int>();

        public static void ClearToolTips()
        {
            ToolTips.Clear();
            MissingTooltips.Clear();
        }

        public static void LoadToolTips()
        {
            ClearToolTips();

            var gameTips = new FileInfo("Content/ToolTips.yaml");
            AddToolTips(gameTips);
            if (GlobalStats.HasMod)
            {
                var modTips = new FileInfo($"{GlobalStats.ModPath}/ToolTips.yaml");
                if (modTips.Exists)
                    AddToolTips(modTips);
            }
        }

        public static ToolTip GetToolTip(int tipId)
        {
            if (tipId > ToolTips.Count)
            {
                if (!MissingTooltips.Contains(tipId))
                {
                    MissingTooltips.Add(tipId);
                    Log.Warning($"Missing ToolTip: {tipId}");
                }
                return null;
            }
            return ToolTips[tipId - 1];
        }

        static void AddToolTips(FileInfo file)
        {
            var tips = new Array<ToolTip>();
            using (var parser = new YamlParser(file))
            {
                foreach (KeyValuePair<object, ToolTip> kv in parser.DeserializeMap<ToolTip>())
                {
                    kv.Value.NameId = (string)kv.Key;
                    tips.Add(kv.Value);
                }
            }
            
            ToolTips.Clear();
            if (ToolTips.Capacity < tips.Count)
                ToolTips.Capacity = tips.Count;

            foreach (ToolTip tip in tips)
            {
                int idx = tip.Id - 1;
                while (ToolTips.Count <= idx) ToolTips.Add(null); // sparse List
                ToolTips[idx] = tip;
            }
        }
    }
}
