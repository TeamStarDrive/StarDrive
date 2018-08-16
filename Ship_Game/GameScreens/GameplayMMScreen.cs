using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public sealed class GameplayMMScreen : GameScreen
    {
        private UniverseScreen screen;
        private GameScreen caller;
        private Menu2 window;
        private UIButton Save;
        private UIButton Load;
        private UIButton Options;
        private UIButton Return;
        private UIButton Exit;
        private UIButton ExitToMain;

        private GameplayMMScreen(GameScreen parent) : base(parent)
        {
            IsPopup = true;
            TransitionOnTime  = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }
        public GameplayMMScreen(UniverseScreen screen) : this((GameScreen)screen)
        {
            this.screen = screen;
            screen.Paused = true;
        }
        public GameplayMMScreen(UniverseScreen screen, GameScreen caller) : this(screen)
        {
            this.caller = caller;
            this.screen = screen;
            screen.Paused = true;
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
                GameTime gameTime = Game1.Instance.GameTime;
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
            if (input.WasKeyPressed(Keys.O) && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
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
            else GameAudio.PlaySfxAsync("UI_Misc20");
        }
        private void Load_OnClick(UIButton button)
        {
            if (SavedGame.NotSaving)
            {
                ScreenManager.AddScreen(new LoadSaveScreen(Empire.Universe));
                ExitScreen();
            }
            else GameAudio.PlaySfxAsync("UI_Misc20");
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
            if (SavedGame.NotSaving) Game1.Instance.Exit();
            else GameAudio.PlaySfxAsync("UI_Misc20");
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RemoveAll();

            Vector2 c = ScreenCenter;
            window = new Menu2(new Rectangle((int)c.X - 100, (int)c.Y - 150, 200, 330));

            BeginVLayout(c.X - 84, c.Y - 100, UIButton.StyleSize().Y + 15);
                Save       = Button(titleId: 300, click: Save_OnClick);
                Load       = Button(titleId: 2,   click: Load_OnClick);
                Options    = Button(titleId: 4,   click: Options_OnClick);
                Return     = Button(titleId: 301, click: Return_OnClick);
                ExitToMain = Button(titleId: 302, click: ExitToMain_OnClick);
                Exit       = Button(titleId: 303, click: Exit_OnClick);
            EndLayout();
        }  
    }
}