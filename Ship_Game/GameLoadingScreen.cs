using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Threading;

namespace Ship_Game
{
	public class GameLoadingScreen : GameScreen
	{
		private Texture2D BGTexture;

		private Thread WorkerThread;

		private Video LoadingVideo;

		private Video SplashVideo;

		private VideoPlayer LoadingPlayer;

		private VideoPlayer SplashPlayer;

		private Rectangle ScreenRect;

		private Rectangle SplashRect;

		private Rectangle LoadingRect;

		private Rectangle BridgeRect;

		//private bool Loading;

		private bool Ready;

		private bool AddedScreen;

		private bool playedOnce;

		private bool playedOnceA;

		private Texture2D LoadingTexture;

		private Texture2D SplashTexture;

		private Texture2D bridgetexture;

		public GameLoadingScreen()
		{
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
			if (!base.IsActive)
			{
				return;
			}
			base.ScreenManager.GraphicsDevice.Clear(Color.Black);
			if (this.LoadingPlayer.State != MediaState.Stopped)
			{
				this.LoadingTexture = this.LoadingPlayer.GetTexture();
			}
			base.ScreenManager.SpriteBatch.Begin();
			if (this.SplashPlayer.State != MediaState.Stopped)
			{
				this.SplashTexture = this.SplashPlayer.GetTexture();
			}
			if (this.SplashTexture != null)
			{
				base.ScreenManager.SpriteBatch.Draw(this.SplashTexture, this.SplashRect, Color.White);
			}
			if (this.LoadingTexture != null && this.SplashPlayer.State != MediaState.Playing)
			{
				base.ScreenManager.SpriteBatch.Draw(this.LoadingTexture, this.LoadingRect, Color.White);
			}
			base.ScreenManager.SpriteBatch.Draw(this.bridgetexture, this.BridgeRect, Color.White);
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			try
			{
				if (this.SplashVideo != null)
				{
					this.SplashPlayer.Stop();
					this.SplashVideo = null;
					this.SplashPlayer.Dispose();
				}
				if (this.LoadingVideo != null)
				{
					this.LoadingPlayer.Stop();
					this.LoadingVideo = null;
					this.LoadingPlayer.Dispose();
				}
			}
			catch
			{
			}
			base.ExitScreen();
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
        ~GameLoadingScreen() {
            //should implicitly do the same thing as the original bad finalize
            this.Dispose(false);
        }

		public override void HandleInput(InputState input)
		{
			if (!base.IsExiting && base.IsActive)
			{
				if (this.playedOnce && this.SplashPlayer.State != MediaState.Playing)
				{
					if (!this.AddedScreen)
					{
						base.ScreenManager.AddScreen(new MainMenuScreen());
					}
					this.AddedScreen = true;
					this.ExitScreen();
				}
				if (input.InGameSelect)
				{
					if (!this.AddedScreen)
					{
						base.ScreenManager.AddScreen(new MainMenuScreen());
					}
					this.ExitScreen();
				}
			}
		}

		public override void LoadContent()
		{
			this.BridgeRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 960, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 540, 1920, 1080);
			this.WorkerThread = new Thread(new ThreadStart(this.Worker))
			{
				IsBackground = true
			};
			this.BGTexture = base.ScreenManager.Content.Load<Texture2D>("WinLose/launch");
			this.bridgetexture = base.ScreenManager.Content.Load<Texture2D>("Textures/GameScreens/Bridge");
			this.LoadingVideo = base.ScreenManager.Content.Load<Video>("Video/Loading 2");
			this.SplashVideo = base.ScreenManager.Content.Load<Video>("Video/zerosplash");
			this.ScreenRect = new Rectangle(0, 0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
			this.SplashRect = new Rectangle(this.ScreenRect.Width / 2 - 640, this.ScreenRect.Height / 2 - 360, 1280, 720);
			this.LoadingRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 64, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 64, 128, 128);
			this.LoadingPlayer = new VideoPlayer()
			{
				IsLooped = true
			};
			this.SplashPlayer = new VideoPlayer();
			ResourceManager.Start();
			ResourceManager.Initialize(base.ScreenManager.Content);
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			if (!base.IsActive)
			{
				if (this.LoadingPlayer.State == MediaState.Playing)
				{
					this.LoadingPlayer.Pause();
				}
				if (this.SplashPlayer.State == MediaState.Playing)
				{
					this.SplashPlayer.Pause();
				}
				if (this.Ready)
				{
					if (!this.AddedScreen)
					{
						base.ScreenManager.AddScreen(new MainMenuScreen());
					}
					this.AddedScreen = true;
					this.ExitScreen();
					return;
				}
			}
			else if (SplashScreen.DisplayComplete)
			{
				if (!this.LoadingPlayer.IsDisposed && !this.playedOnceA)
				{
					this.playedOnceA = true;
					this.LoadingPlayer.Play(this.LoadingVideo);
				}
				if (!this.SplashPlayer.IsDisposed && !this.playedOnce)
				{
					this.SplashPlayer.Play(this.SplashVideo);
					this.playedOnce = true;
				}
				if (this.LoadingPlayer.State == MediaState.Paused)
				{
					this.LoadingPlayer.Resume();
				}
				if (this.SplashPlayer.State == MediaState.Paused)
				{
					this.SplashPlayer.Resume();
				}
			}
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		private void Worker()
		{
			//this.Loading = true;
			this.Ready = true;
		}
	}
}