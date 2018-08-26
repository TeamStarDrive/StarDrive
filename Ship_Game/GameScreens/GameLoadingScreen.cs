using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
	public sealed class GameLoadingScreen : GameScreen
	{
		//private Texture2D BGTexture;
		private Video LoadingVideo;
		private Video SplashVideo;
		private VideoPlayer LoadingPlayer;
		private VideoPlayer SplashPlayer;
		private Rectangle ScreenRect;
		private Rectangle SplashRect;
		private Rectangle LoadingRect;
		private Rectangle BridgeRect;

		private bool Ready;
		private bool AddedScreen;
		private bool PlayedOnce;
		private bool PlayedOnceA;

		private Texture2D LoadingTexture;
		private Texture2D SplashTexture;
		private Texture2D BridgeTexture;

        public GameLoadingScreen() : base(null/*no parent*/)
        {
        }

		public override void Draw(SpriteBatch batch)
		{
			if (!IsActive)
				return;

			ScreenManager.GraphicsDevice.Clear(Color.Black);
			if (LoadingPlayer.State != MediaState.Stopped) LoadingTexture = LoadingPlayer.GetTexture();
			if (SplashPlayer.State  != MediaState.Stopped) SplashTexture  = SplashPlayer.GetTexture();

            ScreenManager.SpriteBatch.Begin();

            if      (SplashTexture  != null && SplashPlayer.State  != MediaState.Stopped) ScreenManager.SpriteBatch.Draw(SplashTexture,  SplashRect,  Color.White);
            else if (LoadingTexture != null && LoadingPlayer.State != MediaState.Stopped) ScreenManager.SpriteBatch.Draw(LoadingTexture, LoadingRect, Color.White);

            ScreenManager.SpriteBatch.Draw(BridgeTexture, BridgeRect, Color.White);
			ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			if (SplashVideo != null)
			{
				SplashPlayer.Stop();
				SplashVideo = null;
				SplashPlayer.Dispose();
			}
			if (LoadingVideo != null)
			{
				LoadingPlayer.Stop();
				LoadingVideo = null;
				LoadingPlayer.Dispose();
			}
			base.ExitScreen();
		}

		public override bool HandleInput(InputState input)
		{
		    if (IsExiting || !IsActive)
                return false;
		    if (PlayedOnce && SplashPlayer.State != MediaState.Playing)
		    {
		        if (!AddedScreen) ScreenManager.AddScreen(new MainMenuScreen());
		        AddedScreen = true;
		        ExitScreen();
                return true;
		    }
		    if (input.InGameSelect)
		    {
		        if (!AddedScreen) ScreenManager.AddScreen(new MainMenuScreen());
		        ExitScreen();
                return true;
		    }
            return base.HandleInput(input);
		}

		public override void LoadContent()
		{
            var size = new Point(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth,
                                 ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);

            BridgeRect = new Rectangle(size.X / 2 - 960, size.Y / 2 - 540, 1920, 1080);
            //BGTexture = ScreenManager.Content.Load<Texture2D>("WinLose/launch");
            BridgeTexture = TransientContent.Load<Texture2D>("Textures/GameScreens/Bridge");
			LoadingVideo  = TransientContent.Load<Video>("Video/Loading 2");
			SplashVideo   = TransientContent.Load<Video>("Video/zerosplash");
			ScreenRect    = new Rectangle(0, 0, size.X, size.Y);
			LoadingRect   = new Rectangle(size.X / 2 - 64, size.Y / 2 - 64, 128, 128);
            SplashRect = new Rectangle(ScreenRect.Width / 2 - 640, ScreenRect.Height / 2 - 360, 1280, 720);
            LoadingPlayer = new VideoPlayer
			{
				IsLooped = true,
                Volume = GlobalStats.MusicVolume
			};
		    SplashPlayer = new VideoPlayer {Volume = GlobalStats.MusicVolume};

            // Initialize all game resources
		    ResourceManager.LoadItAll();

            Log.Info($"Loaded 'Root' Assets {Game1.GameContent.GetLoadedAssetMegabytes():0.0}MB");

            // If you want to export XNB assets:
		    // ResourceManager.ExportAllXnbMeshes();

            base.LoadContent();
            Ready = true;
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			if (!IsActive || Debugger.IsAttached)
			{
				if (LoadingPlayer.State == MediaState.Playing) LoadingPlayer.Pause();
				if (SplashPlayer.State == MediaState.Playing)  SplashPlayer.Pause();
				if (Ready)
				{
					if (!AddedScreen) ScreenManager.AddScreen(new MainMenuScreen());
					AddedScreen = true;
					ExitScreen();
					return;
				}
			}
			else if (SplashScreen.DisplayComplete)
			{
				if (!LoadingPlayer.IsDisposed && !PlayedOnceA)
				{
					PlayedOnceA = true;
					LoadingPlayer.Play(LoadingVideo);
				}
				if (!SplashPlayer.IsDisposed && !PlayedOnce)
				{
                    PlayedOnce = true;
                    SplashPlayer.Play(SplashVideo);
				}
				if (LoadingPlayer.State == MediaState.Paused) LoadingPlayer.Resume();
				if (SplashPlayer.State == MediaState.Paused)  SplashPlayer.Resume();
			}
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

        protected override void Destroy()
        {
            LoadingPlayer?.Dispose(ref LoadingPlayer);
            SplashPlayer?.Dispose(ref SplashPlayer);
            base.Destroy();
        }
	}
}