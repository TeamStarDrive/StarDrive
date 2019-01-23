using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class ModEntry : UIElementV2
	{
		public string ModName;
		public ModInformation mi;
		public string MainMenuMusic;
        public string Version;        
        SubTexture PortraitTex;
        SubTexture BackgroundTex;
        Vector2 BackgroundSize;

        public ModEntry(ModInformation modInfo) : base(null, Vector2.Zero)
		{
			ModName       = modInfo.ModName;
			mi            = modInfo;
			MainMenuMusic = mi.CustomMenuMusic;
            Version       = mi.Version;
		}

        public void LoadContent(GameScreen screen)
        {
            Size = screen.Size;
            var content = screen.ContentManager;
            PortraitTex = content.LoadModTexture(ModName, mi.PortraitPath);

            if (mi.Layout.ShowModImage)
            {
                BackgroundTex = content.LoadModTexture(ModName, mi.ModImagePath);
                BackgroundSize = BackgroundTex.SizeF;
                if (mi.Layout.ModImageFillScreen)
                {
                    // fill the width of the screen
                    float aspectRatio = (float)BackgroundTex.Height / BackgroundTex.Width;
                    BackgroundSize = new Vector2(screen.ScreenWidth, screen.ScreenWidth * aspectRatio);
                }
            }
        }

        public void DrawListElement(SpriteBatch batch, Rectangle clickRect)
		{
			var portrait = new Rectangle(clickRect.X + 6, clickRect.Y, 128, 128);
			var titlePos = new Vector2(portrait.X + 140, portrait.Y);
            
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
            batch.Draw(PortraitTex, portrait, Color.White);
            batch.DrawRectangle(portrait, Color.White);
		}

        public override void Draw(SpriteBatch batch)
        {
            if (BackgroundTex != null)
                batch.Draw(BackgroundTex, mi.Layout.ModImagePos, BackgroundSize);
        }

        public override bool HandleInput(InputState input)
        {
            return false;
        }
    }
}