using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Ship_Game.Data;
using Ship_Game.GameScreens.MainMenu;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
	public sealed class GameLoadingScreen : GameScreen
	{
        VideoPlayer2 LoadingPlayer;
        VideoPlayer2 SplashPlayer;
        Rectangle BridgeRect;
        Texture2D BridgeTexture;
        TaskResult LoadResult;

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
            if (SkipSplashVideo)
                Thread.Sleep(50); // faster loading
            else
                Thread.Sleep(10); // smoother intro video

            if (LoadingFinished())
				return;
			Device.Clear(Color.Black);
            batch.Begin();
            LoadingPlayer?.Draw(batch);
            SplashPlayer?.Draw(batch);
            if (BridgeTexture != null) 
                batch.Draw(BridgeTexture, BridgeRect, Color.White);
            batch.End();
		}

        bool SkipSplashVideo => Debugger.IsAttached;

		public override bool HandleInput(InputState input)
		{
		    if (IsExiting || !IsActive)
                return false;
            return LoadingFinished() || base.HandleInput(input);
		}

        bool LoadingFinished()
        {
            bool ready = LoadResult?.Wait(1) == true;
            if (ready && (Input.InGameSelect ||  SplashPlayer?.IsPlaying != true))
            {
                ScreenManager.GoToScreen(new MainMenuScreen(), clear3DObjects:true);
                return true;
            }
            return false;
        }

		public override void LoadContent()
		{
            base.LoadContent();

            int w = ScreenWidth, h = ScreenHeight;
            int screenCx = w / 2, screenCy = h / 2;

            BridgeTexture = TransientContent.Load<Texture2D>("Textures/GameScreens/Bridge");
            // fit to screen width
            float ratio = ScreenWidth / (float)BridgeTexture.Width;
            int bridgeW = (int)(BridgeTexture.Width * ratio);
            int bridgeH = (int)(BridgeTexture.Height * ratio);
            BridgeRect = new Rectangle(screenCx - bridgeW/2, screenCy - bridgeH/2, bridgeW, bridgeH);

            // little loading icon
            LoadingPlayer = new VideoPlayer2(TransientContent, "Video/Loading 2", true,
                                new Rectangle(screenCx - 64, screenCy - 64, 128, 128));
            LoadingPlayer.Play();

            if (!SkipSplashVideo)
            {
                // "Play it cool"
                int videoW = (int)(1280 * ratio);
                int videoH = (int)(720 * ratio);
                SplashPlayer = new VideoPlayer2(TransientContent, "Video/zerosplash", false,
                                    new Rectangle(screenCx - videoW/2, screenCy - videoH/2, videoW, videoH));
                SplashPlayer.Play();
            }

            // Initialize all game resources in background
            // The splash videos will play while we're loading the assets
            LoadResult = Parallel.Run(LoadGameThread);
        }

        void LoadGameThread()
        {
            try
            {
                ResourceManager.LoadItAll(ScreenManager, GlobalStats.ActiveMod, reset:false);
                Log.Write($"Finished loading 'Root' Assets {StarDriveGame.GameContent.GetLoadedAssetMegabytes():0.0}MB");
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to load game data!");
                throw;
            }
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