using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class ModEntry : UIElementContainer
    {
        public GamePlayGlobals Settings;
        public ModInformation Mod => Settings.Mod;
        public bool IsSupported { get; }
        public SubTexture PortraitTex;

        // Make sure this mode can be loaded regarding data format. If this is spinned up,
        // the mod will not be loaded and the modde will have to check the changes.
        public const int FormatVersion = 1; 

        public ModEntry(GamePlayGlobals settings)
        {
            Settings = settings;
            IsSupported = CheckSupport(Mod.SupportedBlackBoxVersions);
        }

        public SubTexture LoadPortrait(GameScreen screen)
        {
            if (PortraitTex == null || PortraitTex.Texture.IsDisposed)
                PortraitTex = screen.ContentManager.LoadModTexture(Mod.Path, Mod.IconPath);
            return PortraitTex;
        }

        public void LoadContent(GameScreen screen)
        {
            RemoveAll();
            Size = screen.Size;
            LoadPortrait(screen);
        }

        public void DrawListElement(SpriteBatch batch, Rectangle clickRect)
        {
            var portrait = new Rectangle(clickRect.X + 6, clickRect.Y, 128, 128);
            var titlePos = new Vector2(portrait.X + 140, portrait.Y);
            
            float titleWidth = Fonts.Arial20Bold.TextWidth(Mod.Name);
            batch.DrawString(Fonts.Arial20Bold, Mod.Name, titlePos, Color.Gold);
            batch.DrawString(Fonts.Arial12Bold, Mod.Path, new(titlePos.X + titleWidth + 8, titlePos.Y+4), Color.Gray);
            titlePos.Y += Fonts.Arial20Bold.LineSpacing + 2;
            if (!IsSupported)
            {
                batch.DrawString(Fonts.Arial12Bold, "Not supported on This BlackBox Version, try updating this mod.", titlePos, Color.Red);
                titlePos.Y += Fonts.Arial12Bold.LineSpacing + 1;
            }

            string author = "Author: " + Mod.Author;
            batch.DrawString(Fonts.Arial12Bold, author, titlePos, Color.Gold);
            titlePos.Y   += Fonts.Arial12Bold.LineSpacing + 1;
            string url = "URL: " + Settings.URL;
            batch.DrawString(Fonts.Arial12Bold, url, titlePos, Color.SteelBlue);
            titlePos.Y += Fonts.Arial12Bold.LineSpacing + 1;

            string description = Mod.Description;
            if (Mod.Version.NotEmpty())
                description = description + "\n----\nVersion - " + Mod.Version;

            batch.DrawString(Fonts.Arial12Bold, Fonts.Arial12Bold.ParseText(description, 450f), titlePos, Color.White);
            batch.Draw(PortraitTex, portrait, Color.White);
            batch.DrawRectangle(portrait, Color.White);
        }

        static public bool CheckSupport(string supportedBbVers)
        {
            if (supportedBbVers.IsEmpty())
                return false;

            foreach (string version in supportedBbVers.Split(',')) 
            {
                if (GlobalStats.Version.Contains(version))
                    return true;
            }

            return false;
        }

        public override bool HandleInput(InputState input)
        {
            return false;
        }
    }
}