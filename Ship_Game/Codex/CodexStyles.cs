using Microsoft.Xna.Framework;
using Ship_Game.Graphics;

namespace Ship_Game.Codex
{
    // Central registry of named colors and font roles for the Codex body
    // <color=Name>, <b>, and <url> tags. Add entries here and reference them
    // from yaml-sourced strings via <color=Name>...</color>.
    public static class CodexStyles
    {
        public static Color Default   = Color.Wheat;
        public static Color Caption   = Color.Gold;
        public static Color Highlight = Color.Orange;
        public static Color Warning   = Color.Orange;
        public static Color Lore      = new(180, 180, 200);
        public static Color Url       = new(120, 180, 255);

        // Category list, not markup: a header nested inside another header, and the
        // title of a topic that sits under one. Top-level headers and their direct
        // topics keep the plain white the list has always used.
        public static Color NestedHeader = new(225, 205, 140);
        public static Color NestedTitle  = new(170, 215, 235);

        // Caption/title bump from 14 → 20 once the screen is wide enough to
        // absorb the extra size; body stays at 12 (no plain Arial14 exists).
        static bool LargeScreen => GameBase.ScreenWidth >= 1920;
        public static Font DefaultFont => Fonts.Arial12;
        public static Font BoldFont    => Fonts.Arial12Bold;
        public static Font CaptionFont => LargeScreen ? Fonts.Arial20Bold : Fonts.Arial14Bold;

        // Returns true and sets `color` if the name matches one of the registered
        // colors above. Lookup is case-sensitive (yaml authors copy the field name).
        public static bool TryGetColor(string name, out Color color)
        {
            switch (name)
            {
                case "Default":   color = Default;   return true;
                case "Caption":   color = Caption;   return true;
                case "Highlight": color = Highlight; return true;
                case "Warning":   color = Warning;   return true;
                case "Lore":      color = Lore;      return true;
                case "Url":       color = Url;       return true;
                default:          color = Default;   return false;
            }
        }
    }
}
