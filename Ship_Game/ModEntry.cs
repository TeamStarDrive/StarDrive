using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public class ModEntry
	{
		public string ModPath;

		public Rectangle Container = new Rectangle();

		public Rectangle Portrait = new Rectangle();

		public ModInformation mi;

		public Texture2D PortraitTex;

		public Texture2D MainMenuTex;

		public string MainMenuMusic;

		public ModEntry(ScreenManager sm, ModInformation mi, string name)
		{
			this.ModPath = name;
			this.mi = mi;
			this.PortraitTex = sm.Content.Load<Texture2D>(string.Concat("../Mods/", name, "/Textures/", mi.PortraitPath));
			this.MainMenuTex = sm.Content.Load<Texture2D>(string.Concat("../Mods/", name, "/Textures/", mi.ModImagePath_1920x1280));
			this.MainMenuMusic = mi.CustomMenuMusic;
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, Rectangle clickRect)
		{
			this.Container = clickRect;
			this.Portrait = new Rectangle(this.Container.X + 6, this.Container.Y, 128, 128);
			Vector2 TitlePos = new Vector2((float)(this.Portrait.X + 140), (float)this.Portrait.Y);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.mi.ModName, TitlePos, Color.Orange);
			TitlePos.Y = TitlePos.Y + (float)(Fonts.Arial20Bold.LineSpacing + 4);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, HelperFunctions.parseText(Fonts.Arial12Bold, this.mi.ModDescription, 450f), TitlePos, Color.White);
			ScreenManager.SpriteBatch.Draw(this.PortraitTex, this.Portrait, Color.White);
			Primitives2D.DrawRectangle(ScreenManager.SpriteBatch, this.Portrait, Color.White);
		}
	}
}