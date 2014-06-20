using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Ship_Game
{
	public class MySplashScreen : GameScreen, IDisposable
	{
		private Texture2D Splash;

		private Vector2 mousePos = new Vector2(0f, 0f);

		private MouseState mouseStateCurrent;

		private MouseState mouseStatePrevious;

		private bool handleInput;

		public MySplashScreen()
		{
			base.TransitionOnTime = TimeSpan.FromSeconds(1);
			base.TransitionOffTime = TimeSpan.FromSeconds(1);
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
			if (base.ScreenState == Ship_Game.ScreenState.TransitionOn && base.TransitionPosition > 0f)
			{
				base.ScreenManager.FadeBackBufferToBlack(255 - base.TransitionAlpha);
			}
			base.ScreenManager.SpriteBatch.Begin();
			base.ScreenManager.SpriteBatch.Draw(this.Splash, new Rectangle(0, 0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight), new Color(255, 255, 255, base.TransitionAlpha));
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			base.ExitScreen();
		}

		private void ExportToXML()
		{
		}

		/*protected override void Finalize()
		{
			try
			{
				this.Dispose(false);
			}
			finally
			{
				base.Finalize();
			}
		}*/
        ~MySplashScreen() {
            //should implicitly do the same thing as the original bad finalize
        }

		public override void HandleInput(InputState input)
		{
			if (this.handleInput)
			{
				if (input.AButtonDown)
				{
					this.ExitScreen();
				}
				this.HandleMouseInput();
			}
		}

		public void HandleMouseInput()
		{
			this.mouseStateCurrent = Mouse.GetState();
			this.mousePos = new Vector2((float)this.mouseStateCurrent.X, (float)this.mouseStateCurrent.Y);
			this.mouseStatePrevious = this.mouseStateCurrent;
		}

		public override void LoadContent()
		{
			this.Splash = base.ScreenManager.Content.Load<Texture2D>("Textures/UI/splash");
		}

		public override void UnloadContent()
		{
			base.UnloadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
			if (gameTime.TotalRealTime.TotalSeconds > 6)
			{
				this.handleInput = true;
			}
			if (gameTime.TotalRealTime.TotalSeconds > 2)
			{
				this.ExitScreen();
			}
		}
	}
}