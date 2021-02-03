using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;

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

        public static SpriteFont Arial10       { get; private set; }
        public static SpriteFont Arial11Bold   { get; private set; }
        public static SpriteFont Arial12       { get; private set; }
        public static SpriteFont Arial12Bold   { get; private set; }
        public static SpriteFont Arial14Bold   { get; private set; }
        public static SpriteFont Arial20Bold   { get; private set; }
        public static SpriteFont Arial8Bold    { get; private set; }
        public static SpriteFont Consolas18    { get; private set; }
        //public static SpriteFont Corbel14      { get; private set; }
        public static SpriteFont Laserian14    { get; private set; }
        public static SpriteFont Pirulen12     { get; private set; }
        public static SpriteFont Pirulen16     { get; private set; }
        public static SpriteFont Pirulen20     { get; private set; }
        //public static SpriteFont Stratum72     { get; private set; }
        public static SpriteFont Tahoma10      { get; private set; }
        public static SpriteFont Tahoma11      { get; private set; }
        public static SpriteFont TahomaBold9   { get; private set; }
        public static SpriteFont Verdana12     { get; private set; }
        public static SpriteFont Verdana12Bold { get; private set; }
        public static SpriteFont Verdana10     { get; private set; }
        public static SpriteFont Verdana14Bold { get; private set; }
        public static SpriteFont Visitor10     { get; private set; }
        //public static SpriteFont Visitor12     { get; private set; }

        public static string GetGoldString(int gold) => $"{gold:n0}";

        public static void DrawCenteredText(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color color)
        {
            if (string.IsNullOrEmpty(text))
                return;
            Vector2 textSize = font.MeasureString(text);
            Vector2 centerPos = new Vector2(position.X - (int)(textSize.X / 2), 
                                            position.Y - (int)(textSize.Y / 2));
            spriteBatch.DrawString(font, text, centerPos, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f - position.Y / 720f);
        }


        static SpriteFont LoadFont(GameContentManager content, string name, int extraLineSpacing = 0)
        {
            var font = content.Load<SpriteFont>("Fonts/" + name);
            if (extraLineSpacing != 0)
                font.LineSpacing += extraLineSpacing;
            return font;
        }

        public static void LoadContent(GameContentManager c)
        {
            Arial20Bold   = LoadFont(c, "Arial20Bold", -3);
            Arial14Bold   = LoadFont(c, "Arial14Bold");
            Arial12Bold   = LoadFont(c, "Arial12Bold", -2);

            // hack fix for french, Polish and Russian font error. 
            Arial10       = LoadFont(c, "Arial10", -2);
            Arial11Bold   = GlobalStats.IsFrench || GlobalStats.IsPolish || GlobalStats.IsRussian ? Arial10 : LoadFont(c, "Arial11Bold");
            Arial8Bold    = LoadFont(c, "Arial8Bold");
            Arial12       = LoadFont(c, "Arial12", -2);
            //Stratum72     = LoadFont(c, "stratum72");
            //Corbel14      = LoadFont(c, "Corbel14");
            Laserian14    = LoadFont(c, "Laserian14");
            Pirulen16     = LoadFont(c, "Pirulen16");
            Pirulen20     = LoadFont(c, "Pirulen20");
            Consolas18    = LoadFont(c, "consolas18", -4);
            Tahoma10      = LoadFont(c, "Tahoma10");
            Tahoma11      = LoadFont(c, "Tahoma11");
            TahomaBold9   = LoadFont(c, "TahomaBold9");
            Visitor10     = LoadFont(c, "Visitor10");
            //Visitor12     = LoadFont(c, "Visitor12");
            Verdana14Bold = LoadFont(c, "Verdana14Bold");
            Verdana12     = LoadFont(c, "Verdana12");
            Verdana12Bold = LoadFont(c, "Verdana12Bold");
            Verdana10     = LoadFont(c, "Verdana10");

            Consolas18.Spacing -= 2f;
            //Stratum72.Spacing = 1f;
            Visitor10.Spacing = 1f;
            //Visitor12.Spacing = 1f;

            if (GlobalStats.IsRussian || GlobalStats.IsPolish)
            {
                Pirulen12 = LoadFont(c, "Pirulen12");
                if (GlobalStats.IsRussian)
                    Pirulen12.Spacing -= 3f;
            }
            else
            {
                Pirulen12 = LoadFont(c, "Pirulen12a");
            }
        }
    }
}