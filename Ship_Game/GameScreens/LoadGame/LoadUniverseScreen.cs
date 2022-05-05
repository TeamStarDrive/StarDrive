using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Threading;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.GameScreens.MainMenu;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public class LoadUniverseScreen : GameScreen
    {
        LoadGame Loader;
        string AdviceText;
        Texture2D LoadingImage;
        TaskResult<UniverseScreen> AsyncUniverse;

        public LoadUniverseScreen(FileInfo activeFile) : base(null/*no parent*/, toPause: null)
        {
            CanEscapeFromScreen = false;

            Loader = new LoadGame(activeFile);
            AsyncUniverse = Loader.LoadAsync();
        }

        public override void LoadContent()
        {
            LoadingImage = ResourceManager.LoadRandomLoadingScreen(TransientContent);
            AdviceText = Fonts.Arial12Bold.ParseText(ResourceManager.LoadRandomAdvice(), 500f);
            base.LoadContent();
        }

        public override bool HandleInput(InputState input)
        {
            if (AsyncUniverse.IsComplete && AsyncUniverse.Result != null && input.InGameSelect)
            {
                OnLoadSuccess(AsyncUniverse.Result);
                return true;
            }
            return false;
        }

        void OnLoadSuccess(UniverseScreen universe)
        {
            ExitScreen();
            ScreenManager.AddScreenNoLoad(universe);
        }

        void OnLoadFailed()
        {
            // go back to main menu
            ScreenManager.GoToScreen(new MainMenuScreen(), clear3DObjects: true);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!AsyncUniverse.IsComplete)
            {
                // heavily throttle main thread, so the worker thread can turbo
                Thread.Sleep(33);
                if (IsDisposed) // just in case we died
                    return;
            }

            if (Loader.LoadingFailed) // fatal error when loading save game
            {
                OnLoadFailed();
                return;
            }

            ScreenManager.GraphicsDevice.Clear(Color.Black);
            batch.Begin();
            var artRect = new Rectangle(ScreenWidth / 2 - 960, ScreenHeight / 2 - 540, 1920, 1080);
            batch.Draw(LoadingImage, artRect, Color.White);
            var meterBar = new Rectangle(ScreenWidth / 2 - 150, ScreenHeight - 25, 300, 25);

            float percentLoaded = Loader.ProgressPercent;
            var pb = new ProgressBar(meterBar)
            {
                Max = 100f,
                Progress = percentLoaded * 100f
            };
            pb.Draw(ScreenManager.SpriteBatch);

            var cursor = new Vector2(ScreenCenter.X - 250f, meterBar.Y - Fonts.Arial12Bold.MeasureString(AdviceText).Y - 5f);
            batch.DrawString(Fonts.Arial12Bold, AdviceText, cursor, Color.White);

            if (AsyncUniverse.IsComplete)
            {
                cursor.Y -= Fonts.Pirulen16.LineSpacing - 10f;
                const string begin = "Click to Continue!";
                cursor.X = ScreenCenter.X - Fonts.Pirulen16.MeasureString(begin).X / 2f;
                batch.DrawString(Fonts.Pirulen16, begin, cursor, CurrentFlashColor);
            }
            batch.End();
        }
    }
}