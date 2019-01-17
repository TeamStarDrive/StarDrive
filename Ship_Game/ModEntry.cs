using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class ModEntry
	{
		public string ModName;
		public Rectangle Container;
		public Rectangle Portrait;
		public ModInformation mi;
		public string MainMenuMusic;
        public string Version;        
        Texture2D PortraitTex;
        Texture2D MainMenuTex;

        public ModEntry(ModInformation modInfo)
		{
			ModName       = modInfo.ModName;
			mi            = modInfo;
			MainMenuMusic = mi.CustomMenuMusic;
            Version       = mi.Version;
		}

        public void LoadContent(GameContentManager content)
        {
            MainMenuTex = ResourceManager.LoadModTexture(content, ModName, mi.ModImagePath_1920x1280);
            PortraitTex = ResourceManager.LoadModTexture(content, ModName, mi.PortraitPath);
        }

        public void Draw(SpriteBatch batch, Rectangle clickRect)
		{
			Container = clickRect;
			Portrait = new Rectangle(Container.X + 6, Container.Y, 128, 128);
			var titlePos = new Vector2(Portrait.X + 140, Portrait.Y);
            
            //added by gremlin Draw Mod Version
            string title = mi.ModName;

            batch.DrawString(Fonts.Arial20Bold, title, titlePos, Color.Orange);
            titlePos.Y = titlePos.Y + Fonts.Arial20Bold.LineSpacing + 2;

            Vector2 contactPos = titlePos;
           
            string author = "Author: " + mi.Author;
            batch.DrawString(Fonts.Arial12Bold, author, titlePos, Color.Red);
            contactPos.X += Fonts.Arial12Bold.MeasureString(author).X;
            
            string url = " URL: " + mi.URL;
            batch.DrawString(Fonts.Arial12Bold, url, contactPos, Color.CornflowerBlue);
            titlePos.Y = titlePos.Y + Fonts.Arial12Bold.LineSpacing + 1;

            string description = mi.ModDescription;
            if (!string.IsNullOrEmpty(mi.Version))
                description = description + "\n----\nVersion - " + Version;

            batch.DrawString(Fonts.Arial12Bold, Fonts.Arial12Bold.ParseText(description, 450f), titlePos, Color.White);

            batch.Draw(PortraitTex, Portrait, Color.White);

            batch.DrawRectangle(Portrait, Color.White);
		}

        public void DrawMainMenuOverlay(SpriteBatch batch, Rectangle portrait)
        {
            batch.Draw(MainMenuTex, portrait, Color.White);
        }

    }
}