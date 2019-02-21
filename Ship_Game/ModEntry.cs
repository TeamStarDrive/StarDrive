using System;
using System.IO;
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
        SubTexture PortraitTex;

        public ModEntry(ModInformation modInfo) : base(null, Vector2.Zero)
		{
			ModName       = modInfo.ModName;
			mi            = modInfo;
			MainMenuMusic = mi.CustomMenuMusic;
            Version       = mi.Version;
		}

        public void LoadContent(GameScreen screen)
        {
            RemoveAll();
            Size = screen.Size;
            PortraitTex = screen.ContentManager.LoadModTexture(ModName, mi.PortraitPath);

            LayoutParser.LoadLayout(this, Size, "UI/MainMenu.Mod.yaml", layoutRequired: false);
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

        public override bool HandleInput(InputState input)
        {
            return false;
        }
    }
}