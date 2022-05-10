using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Graphics;

namespace Ship_Game
{
    public static class Fonts
    {
        public static readonly Color CountColor        = new Color(79, 24, 44);
        public static readonly Color TitleColor        = new Color(59, 18, 6);
        public static readonly Color CaptionColor      = new Color(228, 168, 57);
        public static readonly Color HighlightColor    = new Color(223, 206, 148);
        public static readonly Color DisplayColor      = new Color(68, 32, 19);
        public static readonly Color DescriptionColor  = new Color(0, 0, 0);
        public static readonly Color RestrictionColor  = new Color(0, 0, 0);
        public static readonly Color ModifierColor     = new Color(0, 0, 0);
        public static readonly Color MenuSelectedColor = new Color(248, 218, 127);

        public static Font Arial10       { get; private set; }
        public static Font Arial11Bold   { get; private set; }
        public static Font Arial12       { get; private set; }
        public static Font Arial12Bold   { get; private set; }
        public static Font Arial14Bold   { get; private set; }
        public static Font Arial20Bold   { get; private set; }
        public static Font Arial8Bold    { get; private set; }
        public static Font Consolas18    { get; private set; }
        public static Font Laserian14    { get; private set; }
        public static Font Pirulen12     { get; private set; }
        public static Font Pirulen16     { get; private set; }
        public static Font Pirulen20     { get; private set; }
        public static Font Tahoma10      { get; private set; }
        public static Font Tahoma11      { get; private set; }
        public static Font TahomaBold9   { get; private set; }
        public static Font Verdana12     { get; private set; }
        public static Font Verdana12Bold { get; private set; }
        public static Font Verdana10     { get; private set; }
        public static Font Verdana14Bold { get; private set; }
        public static Font Visitor10     { get; private set; }

        static readonly Map<string, Font> FontsLookup = new Map<string, Font>();

        public static Font GetFont(string name)
        {
            return FontsLookup[name];
        }
        
        static Font LoadFont(GameContentManager content, string name)
        {
            var font = new Font(content, name, name);
            FontsLookup.Add(name, font);
            return font;
        }

        static Font LoadFont(GameContentManager content, string name, string fontName)
        {
            var font = new Font(content, name, fontName);
            FontsLookup.Add(name, font);
            return font;
        }

        static Font LoadFont(GameContentManager content, string name, string fontName, float monoSpaceSpacing)
        {
            var font = new Font(content, name, fontName, monoSpaceSpacing);
            FontsLookup.Add(name, font);
            return font;
        }

        public static void LoadFonts(GameContentManager c, Language language)
        {
            bool russian = language == Language.Russian;
            bool notEnglish = language != Language.English;
            GameLoadingScreen.SetStatus("LoadFonts");
            FontsLookup.Clear();

            Arial20Bold = LoadFont(c, "Arial20Bold");
            Arial14Bold = LoadFont(c, "Arial14Bold");
            Arial12Bold = LoadFont(c, "Arial12Bold");

            // hack fix for french, Polish and Russian font error.
            Arial10     = LoadFont(c, "Arial10");
            Arial11Bold = LoadFont(c, "Arial11Bold", fontName: (notEnglish ? "Arial10" : "Arial11Bold"));
            Arial8Bold  = LoadFont(c, "Arial8Bold");
            Arial12     = LoadFont(c, "Arial12");
            Laserian14  = LoadFont(c, "Laserian14");
            Pirulen12   = LoadFont(c, "Pirulen12",   fontName: (russian ? "Arial12Bold" : "Pirulen12a"));
            Pirulen16   = LoadFont(c, "Pirulen16");
            Pirulen20   = LoadFont(c, "Pirulen20");
            Consolas18  = LoadFont(c, "consolas18");
            Tahoma10    = LoadFont(c, "Tahoma10");
            Tahoma11    = LoadFont(c, "Tahoma11",    fontName: (russian ? "Tahoma10" : "Tahoma11"));
            TahomaBold9 = LoadFont(c, "TahomaBold9", fontName: (russian ? "Tahoma10" : "TahomaBold9"));
            Visitor10   = LoadFont(c, "Visitor10",   fontName: (russian ? "Arial10" : "Visitor10"), 1f);

            Verdana14Bold = LoadFont(c, "Verdana14Bold");
            Verdana12     = LoadFont(c, "Verdana12");
            Verdana12Bold = LoadFont(c, "Verdana12Bold");
            Verdana10     = LoadFont(c, "Verdana10");
        }
    }
}