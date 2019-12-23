using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.GameScreens;
using Ship_Game.GameScreens.MainMenu;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
	public sealed class GameLoadingScreen : GameScreen
	{
        readonly ScreenMediaPlayer LoadingPlayer;
        readonly ScreenMediaPlayer SplashPlayer;
        Rectangle BridgeRect;
        Texture2D BridgeTexture;
        TaskResult LoadResult;

        public GameLoadingScreen() : base(null/*no parent*/)
        {
            LoadingPlayer = new ScreenMediaPlayer(TransientContent);
            SplashPlayer = new ScreenMediaPlayer(TransientContent);
        }
        
        bool SkipSplashVideo => Debugger.IsAttached;

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

            if (!LoadingFinished())
            {
                Device.Clear(Color.Black);

                batch.Begin();
                LoadingPlayer.Draw(batch);
                SplashPlayer.Draw(batch);
                if (BridgeTexture != null)
                    batch.Draw(BridgeTexture, BridgeRect, Color.White);
                batch.End();
            }
        }

        public override void Update(float deltaTime)
        {
            LoadingPlayer.Update(this);
            SplashPlayer.Update(this);
            base.Update(deltaTime);
        }

        public override bool HandleInput(InputState input)
		{
		    if (IsExiting || !IsActive)
                return false;
            return LoadingFinished() || base.HandleInput(input);
		}

        bool LoadingFinished()
        {
            bool ready = LoadResult?.WaitNoThrow(1) == true;
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
            LoadingPlayer.PlayVideo("Loading 2", looping:true);
            LoadingPlayer.Rect = new Rectangle(screenCx - 64, screenCy - 64, 128, 128);

            if (!SkipSplashVideo)
            {
                // "Play it cool"
                int videoW = (int)(1280 * ratio);
                int videoH = (int)(720 * ratio);
                SplashPlayer.PlayVideo("zerosplash", looping:false);
                SplashPlayer.Rect = new Rectangle(screenCx - videoW/2, screenCy - videoH/2, videoW, videoH);
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
                Log.Write($"Finished loading 'Root' Assets {GameBase.GameContent.GetLoadedAssetMegabytes():0.0}MB");
            }
            catch (Exception ex)
            {
                Log.ErrorDialog(ex, "Failed to load game data!", isFatal:true);
                throw;
            }
        }

        public override void ExitScreen()
        {
            LoadingPlayer.Dispose();
            SplashPlayer.Dispose();
            base.ExitScreen();
        }

        protected override void Destroy()
        {
            LoadingPlayer.Dispose();
            SplashPlayer.Dispose();
            base.Destroy();
        }
	}
}