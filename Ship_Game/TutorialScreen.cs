using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ship_Game
{
	public class TutorialScreen : GameScreen
	{
		private Dictionary<string, Texture2D> TexDict = new Dictionary<string, Texture2D>();

		private Rectangle BridgeRect;

		private int Index;

		private CloseButton close;

		public TutorialScreen()
		{
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			base.ScreenManager.SpriteBatch.Draw(this.TexDict[string.Concat("Slide_", this.Index.ToString("00"))], this.BridgeRect, Color.White);
			this.close.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			base.ExitScreen();
		}

		public override void HandleInput(InputState input)
		{
			if (this.close.HandleInput(input))
			{
				this.ExitScreen();
				return;
			}
			if (input.Escaped)
			{
				this.ExitScreen();
			}
			if (input.Right || input.InGameSelect)
			{
				TutorialScreen index = this;
				index.Index = index.Index + 1;
				if (this.Index > this.TexDict.Count - 1)
				{
					this.Index = 0;
				}
			}
			if (input.Left || input.RightMouseClick)
			{
				TutorialScreen tutorialScreen = this;
				tutorialScreen.Index = tutorialScreen.Index - 1;
				if (this.Index < 0)
				{
					this.Index = this.TexDict.Count - 1;
				}
			}
		}

		public override void LoadContent()
		{
			FileInfo[] textList = HelperFunctions.GetFilesFromDirectory(string.Concat("Content/Tutorials/", GlobalStats.Config.Language, "/"));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				string name = Path.GetFileNameWithoutExtension(fileInfoArray[i].Name);
				if (name != "Thumbs")
				{
					Texture2D tex = Game1.Instance.Content.Load<Texture2D>(string.Concat("Tutorials/", GlobalStats.Config.Language, "/", name));
					this.TexDict[name] = tex;
				}
			}
			this.BridgeRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 640, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 360, 1280, 720);
			this.close = new CloseButton(new Rectangle(this.BridgeRect.X + this.BridgeRect.Width - 38, this.BridgeRect.Y + 15, 20, 20));
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}