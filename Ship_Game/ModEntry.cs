using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.UI;

namespace Ship_Game
{
    public sealed class ModEntry : UIElementContainer
    {
        public string ModName;
        public ModInformation mi;
        public string MainMenuMusic;
        public string Version;
        public bool IsSupported { get; }
        SubTexture PortraitTex;

        public ModEntry(ModInformation modInfo)
        {
            ModName       = modInfo.ModName;
            mi            = modInfo;
            MainMenuMusic = mi.CustomMenuMusic;
            Version       = mi.Version;
            IsSupported   = CheckSupport(modInfo.SupportedBlackBoxVersions);
        }

        public void LoadPortrait(GameScreen screen)
        {
            PortraitTex = screen.ContentManager.LoadModTexture(ModName, mi.PortraitPath);
        }

        public void LoadContent(GameScreen screen)
        {
            RemoveAll();
            Size = screen.Size;
            LoadPortrait(screen);
            
            LayoutParser.LoadLayout(screen, Size, "UI/MainMenu.Mod.yaml", clearElements: false, required: false);
        }

        public void DrawListElement(SpriteBatch batch, Rectangle clickRect)
        {
            var portrait = new Rectangle(clickRect.X + 6, clickRect.Y, 128, 128);
            var titlePos = new Vector2(portrait.X + 140, portrait.Y);
            
            //added by gremlin Draw Mod Version
            string title = mi.ModName;

            batch.DrawString(Fonts.Arial20Bold, title, titlePos, Color.Gold);
            titlePos.Y += Fonts.Arial20Bold.LineSpacing + 2;

            if (!IsSupported)
            {
                batch.DrawString(Fonts.Arial12Bold, "Not supported on This BlackBox Version", titlePos, Color.Red);
                titlePos.Y += Fonts.Arial12Bold.LineSpacing + 1;
            }

            string author = "Author: " + mi.Author;
            batch.DrawString(Fonts.Arial12Bold, author, titlePos, Color.Gold);
            titlePos.Y   += Fonts.Arial12Bold.LineSpacing + 1;
            string url = "URL: " + mi.URL;
            batch.DrawString(Fonts.Arial12Bold, url, titlePos, Color.SteelBlue);
            titlePos.Y += Fonts.Arial12Bold.LineSpacing + 1;

            string description = mi.ModDescription;
            if (!string.IsNullOrEmpty(mi.Version))
                description = description + "\n----\nVersion - " + Version;

            batch.DrawString(Fonts.Arial12Bold, Fonts.Arial12Bold.ParseText(description, 450f), titlePos, Color.White);
            batch.Draw(PortraitTex, portrait, Color.White);
            batch.DrawRectangle(portrait, Color.White);
        }

        bool CheckSupport(string supportedBbVers)
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