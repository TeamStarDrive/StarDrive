using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game
{
    public sealed class GameplayMMScreen : GameScreen
    {
        private UniverseScreen screen;
        private GameScreen caller;
        private Menu2 window;
        private UIButton Save;

        private GameplayMMScreen(GameScreen parent) : base(parent, pause: true)
        {
            IsPopup = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
        }
        public GameplayMMScreen(UniverseScreen screen) : this((GameScreen)screen)
        {
            this.screen = screen;
        }
        public GameplayMMScreen(UniverseScreen screen, GameScreen caller) : this(screen)
        {
            this.caller = caller;
            this.screen = screen;
        }

        public override void ExitScreen()
        {
            if (caller == null)
                screen.Paused = false;
            base.ExitScreen();
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            if (SavedGame.IsSaving)
            {
                GameTime gameTime = StarDriveGame.Instance.GameTime;
                TimeSpan totalGameTime = gameTime.TotalGameTime;
                float f = (float)Math.Sin(totalGameTime.TotalSeconds);
                f = Math.Abs(f) * 255f;
                Color flashColor = new Color(255, 255, 255, (byte)f);
                Vector2 pausePos = new Vector2(ScreenManager.Center().X - Fonts.Pirulen16.MeasureString("Paused").X / 2f, 45 + Fonts.Pirulen16.LineSpacing * 2 + 4);
                batch.DrawString(Fonts.Pirulen16, "Saving...", pausePos, flashColor);
            }
            window.Draw(batch);

            Save.Enabled = SavedGame.NotSaving;
            base.Draw(batch);
            batch.End();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.KeyPressed(Keys.O) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }


        private void Save_OnClick(UIButton button)
        {
            if (SavedGame.NotSaving) // no save in progress
                ScreenManager.AddScreen(new SaveGameScreen(Empire.Universe));
            else GameAudio.NegativeClick();
        }
        private void Load_OnClick(UIButton button)
        {
            if (SavedGame.NotSaving)
            {
                ScreenManager.AddScreen(new LoadSaveScreen(Empire.Universe));
                ExitScreen();
            }
            else GameAudio.NegativeClick();
        }
        private void Options_OnClick(UIButton button)
        {
            ScreenManager.AddScreen(new OptionsScreen(screen, this)
            {
                TitleText  = Localizer.Token(4),
                MiddleText = Localizer.Token(4004)
            });
        }
        private void Return_OnClick(UIButton button)
        {
            ExitScreen(); 
        }
        private void ExitToMain_OnClick(UIButton button)
        {
            ExitScreen();
            if (caller != null)
                ScreenManager.RemoveScreen(caller);
            screen.ExitScreen();
            ScreenManager.AddScreen(new MainMenuScreen());
        }
        private void Exit_OnClick(UIButton button)
        {
            if (SavedGame.NotSaving) StarDriveGame.Instance.Exit();
            else GameAudio.NegativeClick();
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RemoveAll();

            Vector2 c = ScreenCenter;
            window = new Menu2(new Rectangle((int)c.X - 100, (int)c.Y - 150, 200, 330));

            UIList list = List(new Vector2(c.X - 84, c.Y - 100));
            list.Padding = new Vector2(2f, 12f);
            list.LayoutStyle = ListLayoutStyle.Resize;

            Save = list.AddButton(300, Save_OnClick);
            list.AddButton(titleId: 2,   Load_OnClick);
            list.AddButton(titleId: 4,   Options_OnClick);
            list.AddButton(titleId: 301, Return_OnClick);
            list.AddButton(titleId: 302, ExitToMain_OnClick);
            list.AddButton(titleId: 303, Exit_OnClick);
        }  
    }
}