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
        UniverseScreen screen;
        GameScreen caller;
        Menu2 window;
        UIButton Save;

        GameplayMMScreen(GameScreen parent) : base(parent, pause: true)
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

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            if (SavedGame.IsSaving)
            {
                var pausePos = new Vector2(ScreenCenter.X - Fonts.Pirulen16.MeasureString("Paused").X / 2f, 45 + Fonts.Pirulen16.LineSpacing * 2 + 4);
                batch.DrawString(Fonts.Pirulen16, "Saving...", pausePos, CurrentFlashColor);
            }
            window.Draw(batch, elapsed);

            Save.Enabled = SavedGame.NotSaving;
            base.Draw(batch, elapsed);
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
            return base.HandleInput(input);
        }


        void Save_OnClick(UIButton button)
        {
            if (SavedGame.NotSaving) // no save in progress
                ScreenManager.AddScreen(new SaveGameScreen(Empire.Universe));
            else GameAudio.NegativeClick();
        }

        void Load_OnClick(UIButton button)
        {
            if (SavedGame.NotSaving)
            {
                ScreenManager.AddScreen(new LoadSaveScreen(Empire.Universe));
                ExitScreen();
            }
            else GameAudio.NegativeClick();
        }

        void Options_OnClick(UIButton button)
        {
            ScreenManager.AddScreen(new OptionsScreen(universe: screen)
            {
                TitleText  = Localizer.Token(GameText.Options),
                MiddleText = Localizer.Token(GameText.ChangeAudioVideoAndGameplay)
            });
        }

        void Return_OnClick(UIButton button)
        {
            ExitScreen(); 
        }

        void ExitToMain_OnClick(UIButton button)
        {
            ExitScreen();
            if (caller != null)
                ScreenManager.RemoveScreen(caller);
            screen.ExitScreen();
            ScreenManager.AddScreen(new MainMenuScreen());
        }

        void Exit_OnClick(UIButton button)
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

            UIList list = AddList(new Vector2(c.X - 84, c.Y - 100));
            list.Padding = new Vector2(2f, 12f);
            list.LayoutStyle = ListLayoutStyle.ResizeList;

            Save = list.AddButton(300, Save_OnClick);
            list.AddButton(text: 2,   Load_OnClick);
            list.AddButton(text: 4,   Options_OnClick);
            list.AddButton(text: 301, Return_OnClick);
            list.AddButton(text: 302, ExitToMain_OnClick);
            list.AddButton(text: 303, Exit_OnClick);
        }  
    }
}
