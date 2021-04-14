using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        //public static Graphics.Font Corbel14      { get; private set; }
        public static Font Laserian14    { get; private set; }
        public static Font Pirulen12     { get; private set; }
        public static Font Pirulen16     { get; private set; }
        public static Font Pirulen20     { get; private set; }
        //public static Graphics.Font Stratum72     { get; private set; }
        public static Font Tahoma10      { get; private set; }
        public static Font Tahoma11      { get; private set; }
        public static Font TahomaBold9   { get; private set; }
        public static Font Verdana12     { get; private set; }
        public static Font Verdana12Bold { get; private set; }
        public static Font Verdana10     { get; private set; }
        public static Font Verdana14Bold { get; private set; }
        public static Font Visitor10     { get; private set; }
        //public static Font Visitor12     { get; private set; }

        public static Font LoadFont(GameContentManager content, string name)
        {
            return new Font(content, name);
        }

        public static void LoadFonts(GameContentManager c)
        {
            GameLoadingScreen.SetStatus("LoadFonts");
            Arial20Bold   = LoadFont(c, "Arial20Bold");
            Arial14Bold   = LoadFont(c, "Arial14Bold");
            Arial12Bold   = LoadFont(c, "Arial12Bold");

            // hack fix for french, Polish and Russian font error. 
            Arial10       = LoadFont(c, "Arial10");
            Arial11Bold   = GlobalStats.IsRussian ? Arial10 : LoadFont(c, "Arial11Bold");
            Arial8Bold    = LoadFont(c, "Arial8Bold");
            Arial12       = LoadFont(c, "Arial12");
            //Stratum72     = LoadFont(c, "stratum72");
            //Corbel14      = LoadFont(c, "Corbel14");
            Laserian14    = LoadFont(c, "Laserian14");
            Pirulen16     = LoadFont(c, "Pirulen16");
            Pirulen20     = LoadFont(c, "Pirulen20");
            Consolas18    = LoadFont(c, "consolas18");
            Tahoma10      = LoadFont(c, "Tahoma10");
            Tahoma11      = GlobalStats.IsRussian ? Tahoma10 : LoadFont(c, "Tahoma11");
            TahomaBold9   = GlobalStats.IsRussian ? Tahoma10 : LoadFont(c, "TahomaBold9");
            Visitor10     = GlobalStats.IsRussian ? Arial10  : LoadFont(c, "Visitor10");
            Visitor10.Spacing = 1f;

            //Visitor12     = LoadFont(c, "Visitor12");
            Verdana14Bold = LoadFont(c, "Verdana14Bold");
            Verdana12     = LoadFont(c, "Verdana12");
            Verdana12Bold = LoadFont(c, "Verdana12Bold");
            Verdana10     = LoadFont(c, "Verdana10");

            Pirulen12 = GlobalStats.IsRussian ? LoadFont(c, "Arial12Bold") : LoadFont(c, "Pirulen12a");
        }
    }
}