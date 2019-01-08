using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
	public sealed class GameLoadingScreen : GameScreen
	{
        private VideoPlayer2 LoadingPlayer;
        private VideoPlayer2 SplashPlayer;
		private Rectangle BridgeRect;
        private Texture2D BridgeTexture;
        private bool Ready;

        public GameLoadingScreen() : base(null/*no parent*/)
        {
        }

        class VideoPlayer2 : IDisposable
        {
            Video Video;
            readonly VideoPlayer Player;
            readonly Rectangle Rect;
            public VideoPlayer2(GameContentManager content, string video, bool looping, Rectangle rect)
            {
                Video = content.Load<Video>(video);
                Player = new VideoPlayer
                {
                    IsLooped = looping,
                    Volume = GlobalStats.MusicVolume
                };
                Rect = rect;
            }
            public void Play() => Player.Play(Video);
            public bool IsPlaying => Player.State == MediaState.Playing;
            public void Dispose()
            {
                Video = null;
                Player.Stop();
                Player.Dispose();
            }
            public void Draw(SpriteBatch batch)
            {
                if (Player.State == MediaState.Stopped)
                    return;

                Texture2D texture = Player.GetTexture();
                if (texture != null) batch.Draw(texture, Rect, Color.White);;
            }
        }

		public override void Draw(SpriteBatch batch)
		{
            // NOTE: by throttling LoadingScreen rendering, we get ~4x faster loading
            // this is because video player Decode+Draw is very expensive.

            // no splash:               DEBUG load in ~1.6 seconds
            // no throttling + splash:  DEBUG load in ~5.6 seconds
            // throttling(50) + splash: DEBUG load in ~1.7 seconds
            Thread.Sleep(50);

            if (!IsActive)
				return;
			ScreenManager.GraphicsDevice.Clear(Color.Black);
            batch.Begin();
            LoadingPlayer?.Draw(batch);
            SplashPlayer?.Draw(batch);
            if (BridgeTexture != null) 
                batch.Draw(BridgeTexture, BridgeRect, Color.White);
            batch.End();
		}

        bool SkipSplashVideo => false && Debugger.IsAttached;

		public override bool HandleInput(InputState input)
		{
		    if (IsExiting || !IsActive)
                return false;
            if (input.InGameSelect || (Ready && SplashPlayer?.IsPlaying != true))
		    {
                GoToMainMenuScreen();
                return true;
		    }
            return base.HandleInput(input);
		}

        void GoToMainMenuScreen()
        {
            if (!ScreenManager.IsShowing<MainMenuScreen>())
                ScreenManager.AddScreen(new MainMenuScreen());
            ExitScreen();
        }

		public override void LoadContent()
		{
            base.LoadContent();

            var size = new Point(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth,
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);

            BridgeRect = new Rectangle(size.X / 2 - 960, size.Y / 2 - 540, 1920, 1080);
            var screenRect = new Rectangle(0, 0, size.X, size.Y);

            // little loading icon
            LoadingPlayer = new VideoPlayer2(TransientContent, "Video/Loading 2", true,
                new Rectangle(size.X / 2 - 64, size.Y / 2 - 64, 128, 128));
            LoadingPlayer.Play();

            if (!SkipSplashVideo)
            {
                // "Play it cool"
                SplashPlayer = new VideoPlayer2(TransientContent, "Video/zerosplash", false,
                            new Rectangle(screenRect.Width / 2 - 640, screenRect.Height / 2 - 360, 1280, 720));
                SplashPlayer.Play();
            }


            // Initialize all game resources in background
            // The splash videos will play while we're loading the assets
            Parallel.Run(() =>
            {
                BridgeTexture = TransientContent.Load<Texture2D>("Textures/GameScreens/Bridge");

                ResourceManager.LoadItAll(() => Ready = true);
                Log.Info($"Loaded 'Root' Assets {Game1.GameContent.GetLoadedAssetMegabytes():0.0}MB");
            });
        }

        public override void ExitScreen()
        {
            LoadingPlayer?.Dispose(ref LoadingPlayer);
            SplashPlayer?.Dispose(ref SplashPlayer);
            base.ExitScreen();
        }

        protected override void Destroy()
        {
            LoadingPlayer?.Dispose(ref LoadingPlayer);
            SplashPlayer?.Dispose(ref SplashPlayer);
            base.Destroy();
        }
	}
}