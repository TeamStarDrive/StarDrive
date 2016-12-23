using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class ModEntry
	{
		public string ModPath;
		public Rectangle Container;
		public Rectangle Portrait;
		public ModInformation mi;
		public string MainMenuMusic;
        public string Version;
        private Texture2D PortraitTex;
        private Texture2D MainMenuTex;

        public ModEntry(ModInformation modInfo)
		{
			ModPath       = modInfo.ModName;
			mi            = modInfo;
			MainMenuMusic = mi.CustomMenuMusic;
            Version       = mi.Version;
		}

		public void Draw(ScreenManager screenManager, Rectangle clickRect)
		{
			Container = clickRect;
			Portrait = new Rectangle(Container.X + 6, Container.Y, 128, 128);
			Vector2 titlePos = new Vector2(Portrait.X + 140, Portrait.Y);
            
            //added by gremlin Draw Mod Version
            string title = mi.ModName;

            screenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, title, titlePos, Color.Orange);
            titlePos.Y = titlePos.Y + Fonts.Arial20Bold.LineSpacing + 2;

            Vector2 contactPos = titlePos;
           
            string author = "Author: " + mi.Author;
            screenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, author, titlePos, Color.Red);
            contactPos.X += Fonts.Arial12Bold.MeasureString(author).X;
            
            string url = " URL: " + mi.URL;
            screenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, url, contactPos, Color.CornflowerBlue);
            titlePos.Y = titlePos.Y + Fonts.Arial12Bold.LineSpacing + 1;

            string description = mi.ModDescription;
            if (!string.IsNullOrEmpty(mi.Version))
                description = description + "\n----\nVersion - " + Version;

            screenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, HelperFunctions.ParseText(Fonts.Arial12Bold, description, 450f), titlePos, Color.White);

            if (PortraitTex == null)
                PortraitTex = ResourceManager.LoadModTexture(ModPath, mi.PortraitPath);
            if (PortraitTex != null)
                screenManager.SpriteBatch.Draw(PortraitTex, Portrait, Color.White);

			Primitives2D.DrawRectangle(screenManager.SpriteBatch, Portrait, Color.White);
		}

        public void DrawMainMenuOverlay(ScreenManager screenManager, Rectangle portrait)
        {
            if (MainMenuTex == null)
                MainMenuTex = ResourceManager.LoadModTexture(ModPath, mi.ModImagePath_1920x1280);
            if (MainMenuTex != null)
                screenManager.SpriteBatch.Draw(MainMenuTex, portrait, Color.White);
        }
    }
}