using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	internal class LoadingScreen : GameScreen
	{
		private bool loadingIsSlow;

		private bool otherScreensAreGone;

		private GameScreen[] screensToLoad;

		private LoadingScreen(Ship_Game.ScreenManager screenManager, bool loadingIsSlow, GameScreen[] screensToLoad)
		{
			this.loadingIsSlow = loadingIsSlow;
			this.screensToLoad = screensToLoad;
			base.TransitionOnTime = TimeSpan.FromSeconds(0);
			base.TransitionOffTime = TimeSpan.FromSeconds(0);
		}

		public override void Draw(GameTime gameTime)
		{
			if (base.ScreenState == Ship_Game.ScreenState.Active && (int)base.ScreenManager.GetScreens().Length == 1)
			{
				this.otherScreensAreGone = true;
			}
			if (this.loadingIsSlow)
			{
				SpriteBatch spriteBatch = base.ScreenManager.SpriteBatch;
				Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
				Vector2 viewportSize = new Vector2((float)viewport.Width, (float)viewport.Height);
				Vector2 textSize = Fonts.Arial12Bold.MeasureString("Loading...");
				Vector2 textPosition = (viewportSize - textSize) / 2f;
				Color color = new Color(255, 255, 255, base.TransitionAlpha);
				spriteBatch.Begin();
				spriteBatch.DrawString(Fonts.Arial12Bold, "Loading...", textPosition, color);
				spriteBatch.End();
			}
		}

		public static void Load(Ship_Game.ScreenManager screenManager, bool loadingIsSlow, params GameScreen[] screensToLoad)
		{
			GameScreen[] screens = screenManager.GetScreens();
			for (int i = 0; i < (int)screens.Length; i++)
			{
				screens[i].ExitScreen();
			}
			screenManager.AddScreen(new LoadingScreen(screenManager, loadingIsSlow, screensToLoad));
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
			if (this.otherScreensAreGone)
			{
				base.ScreenManager.RemoveScreen(this);
				GameScreen[] gameScreenArray = this.screensToLoad;
				for (int i = 0; i < (int)gameScreenArray.Length; i++)
				{
					GameScreen screen = gameScreenArray[i];
					if (screen != null)
					{
						base.ScreenManager.AddScreen(screen);
					}
				}
				Game1.Instance.ResetElapsedTime();
			}
		}
	}
}