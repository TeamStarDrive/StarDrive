using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.GameScreens;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game
{
    public sealed class GameLoadingScreen : GameScreen
    {
        readonly ScreenMediaPlayer LoadingPlayer;
        readonly ScreenMediaPlayer SplashPlayer;
        Rectangle BridgeRect;
        Texture2D BridgeTexture;
        TaskResult LoadResult;
        readonly bool ShowSplash;
        readonly bool ResetResources;
        SpriteFont StatusFont;

        public GameLoadingScreen(bool showSplash, bool resetResources) : base(null/*no parent*/)
        {
            CanEscapeFromScreen = false;
            ShowSplash = showSplash;
            ResetResources = resetResources;
            LoadingPlayer = new ScreenMediaPlayer(TransientContent);
            SplashPlayer  = new ScreenMediaPlayer(TransientContent);
        }
        
        static string StatusCategory;
        static string StatusText;
        public static void SetStatus(string category, string item)
        {
            SetCategory(category);
            SetItem(item);
        }
        public static void SetCategory(string category)
        {
            StatusCategory = category;
        }
        public static void SetItem(string item)
        {
            StatusText = item.NotEmpty() ? StatusCategory+":"+item : StatusCategory;
        }

        bool ShowSplashVideo => ShowSplash && !Debugger.IsAttached;

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            // NOTE: by throttling LoadingScreen rendering, we get ~4x faster loading
            // this is because video player Decode+Draw is very expensive.

            // no splash:               DEBUG load in ~1.6 seconds
            // no throttling + splash:  DEBUG load in ~5.6 seconds
            // throttling(50) + splash: DEBUG load in ~1.7 seconds
            if (ShowSplashVideo)
                Thread.Sleep(10); // smoother intro video
            else
                Thread.Sleep(50); // faster loading

            if (!LoadingFinished())
            {
                Device.Clear(Color.Black);

                try
                {
                    batch.Begin();
                    try
                    {
                        LoadingPlayer.Draw(batch);
                        SplashPlayer.Draw(batch);
                    }
                    catch
                    {
                    }

                    if (BridgeTexture != null)
                        batch.Draw(BridgeTexture, BridgeRect, Color.White);

                    string status = StatusText;
                    if (status.NotEmpty())
                        DrawCentered(batch, status, Height*0.8f, Color.White);
                }
                finally
                {
                    batch.End();
                }
            }
        }

        void DrawCentered(SpriteBatch batch, string text, float y, Color c)
        {
            int width = StatusFont.TextWidth(text);
            batch.DrawString(StatusFont, text, CenterX - width/2, y, c);
        }

        public override void Update(float fixedDeltaTime)
        {
            LoadingPlayer.Update(this);
            SplashPlayer.Update(this);
            base.Update(fixedDeltaTime);
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
            if (ResetResources)
                ResourceManager.UnloadAllData(ScreenManager);

            base.LoadContent();

            int w = ScreenWidth, h = ScreenHeight;
            int screenCx = w / 2, screenCy = h / 2;

            StatusFont = Fonts.LoadFont(TransientContent, "Arial12");

            BridgeTexture = TransientContent.Load<Texture2D>("Textures/GameScreens/Bridge");
            // fit to screen width
            float ratio = ScreenWidth / (float)BridgeTexture.Width;
            int bridgeW = (int)(BridgeTexture.Width * ratio);
            int bridgeH = (int)(BridgeTexture.Height * ratio);
            BridgeRect = new Rectangle(screenCx - bridgeW/2, screenCy - bridgeH/2, bridgeW, bridgeH);

            // little loading icon
            LoadingPlayer.PlayVideo("Loading 2", looping:true);
            LoadingPlayer.Rect = new Rectangle(screenCx - 64, screenCy - 64, 128, 128);

            if (ShowSplashVideo)
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
                ResourceManager.LoadItAll(ScreenManager, GlobalStats.ActiveMod?.IsSupported == true ? GlobalStats.ActiveMod : null);
                Log.Write($"Finished loading 'Root' Assets {GameBase.GameContent.GetLoadedAssetMegabytes():0.0}MB");

                //QuadtreePerfTests.RunCollisionPerfTest();
                //StarDriveGame.Instance.Exit();
            }
            catch (Exception ex)
            {
                Log.ErrorDialog(ex, "Failed to load game data!", Program.UNHANDLED_EXCEPTION);
                throw;
            }
        }

        protected override void Destroy()
        {
            LoadingPlayer.Dispose();
            SplashPlayer.Dispose();
            base.Destroy();
        }
    }
}