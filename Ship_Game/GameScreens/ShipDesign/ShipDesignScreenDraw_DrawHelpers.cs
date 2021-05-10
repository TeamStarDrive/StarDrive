using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        enum ValueTint
        {
            None,
            Bad,
            GoodBad,
            BadLowerThan2,
            BadPercentLowerThan1,
            CompareValue
        }

        struct StatValue
        {
            public LocalizedText Title;
            public Color TitleColor;
            public float Value;
            public float CompareValue;
            public LocalizedText Tooltip;
            public ValueTint Tint;
            public bool IsPercent;
            public float Spacing;
            public int LineSpacing;

            public StatValue(in LocalizedText title, float value, in LocalizedText tooltip, 
                             Color titleColor, ValueTint tint = ValueTint.None, float spacing = 165, int lineSpacing = 1)
            {
                Title = title;
                TitleColor = titleColor;
                Value = value;
                CompareValue = 0f;
                Tooltip = tooltip;
                Tint = tint;
                IsPercent = false;
                Spacing = spacing;
                LineSpacing = lineSpacing;
            }

            public Color ValueColor
            {
                get
                {
                    switch (Tint)
                    {
                        case ValueTint.GoodBad:              return Value > 0f ? Color.LightGreen : Color.LightPink;
                        case ValueTint.Bad:                  return Color.LightPink;
                        case ValueTint.BadLowerThan2:        return Value > 2f ? Color.LightGreen : Color.LightPink;
                        case ValueTint.BadPercentLowerThan1: return Value > 1f ? Color.LightGreen : Color.LightPink;
                        case ValueTint.CompareValue:         return CompareValue < Value ? Color.LightGreen : Color.LightPink;
                        case ValueTint.None:
                        default: return Color.White;
                    }
                }
            }

            public string ValueText => IsPercent ? Value.ToString("P0") : Value.GetNumberString();
        }

        static void WriteLine(ref Vector2 cursor, int lines = 1)
        {
            cursor.Y += Fonts.Arial12Bold.LineSpacing * lines;
        }

        static StatValue MakeStat(in LocalizedText title, float value, LocalizedText tooltip, Color titleColor, ValueTint tint = ValueTint.None, float spacing = 165, int lineSpacing = 1)
            => new StatValue(title.Text+":", value, tooltip, titleColor, tint, spacing, lineSpacing);

        static StatValue TintedValue(in LocalizedText title, float value, LocalizedText tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue(title.Text+":", value, tooltip, titleColor, ValueTint.GoodBad, spacing, lineSpacing);

        void DrawStatColor(ref Vector2 cursor, StatValue stat)
        {
            Graphics.Font font = Fonts.Arial12Bold;

            WriteLine(ref cursor);
            cursor.Y += stat.LineSpacing;

            Vector2 statCursor = new Vector2(cursor.X + stat.Spacing, cursor.Y);
            string title = stat.Title.Text;
            DrawString(FontSpace(statCursor, -20, title, font), stat.TitleColor, title, font); // @todo Replace with DrawTitle?

            string valueText = stat.ValueText;
            DrawString(statCursor, stat.ValueColor, valueText, font);
            CheckToolTip(stat.Tooltip, cursor, title, valueText, font, MousePos);
        }

        public void DrawStat(ref Vector2 cursor, LocalizedText words, float stat, Color color, LocalizedText tooltipId, bool doGoodBadTint = true, bool isPercent = false, float spacing = 165)
        {
            StatValue sv = TintedValue(words, stat, tooltipId, color, spacing, 0);
            sv.IsPercent = isPercent;
            DrawStatColor(ref cursor, sv);
        }

        public void DrawStatBadPercentLower1(ref Vector2 cursor, LocalizedText words, float stat, Color color, LocalizedText tooltipId, float spacing = 165)
        {
            StatValue sv = MakeStat(words, stat, tooltipId, color, ValueTint.BadPercentLowerThan1, spacing, 0);
            sv.IsPercent = true;
            DrawStatColor(ref cursor, sv);
        }
    }
}
