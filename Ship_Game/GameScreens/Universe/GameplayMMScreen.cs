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
        UniverseScreen Universe;
        GameScreen Caller;
        UILabel SavingText;
        UIButton SaveButton;

        GameplayMMScreen(GameScreen parent) : base(parent, pause: true)
        {
            IsPopup = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
        }
        public GameplayMMScreen(UniverseScreen screen) : this((GameScreen)screen)
        {
            Universe = screen;
        }
        public GameplayMMScreen(UniverseScreen screen, GameScreen caller) : this(screen)
        {
            Caller = caller;
            Universe = screen;
        }
        
        public override void LoadContent()
        {
            RemoveAll();

            Vector2 c = ScreenCenter;
            Add(new Menu2(new RectF(c.X - 100, c.Y - 150, 200, 330)));

            SavingText = Add(new UILabel(GameText.Saving, Fonts.Pirulen16, Color.White));
            SavingText.Visible = false;
            SavingText.TextAlign = TextAlign.Center;
            SavingText.Pos = new Vector2(c.X - SavingText.Size.X*0.5f, 
                                         50 + Fonts.Pirulen16.LineSpacing * 2);

            UIList list = AddList(new Vector2(c.X - 84, c.Y - 100));
            list.Padding = new Vector2(2f, 12f);
            list.LayoutStyle = ListLayoutStyle.ResizeList;

            SaveButton = list.Add(ButtonStyle.Default, GameText.Save, Save_OnClick);
            list.Add(ButtonStyle.Default, GameText.LoadGame,   Load_OnClick);
            list.Add(ButtonStyle.Default, GameText.Options,   Options_OnClick);
            list.Add(ButtonStyle.Default, GameText.ReturnToGame, Return_OnClick);
            list.Add(ButtonStyle.Default, GameText.ExitToMainMenu, ExitToMain_OnClick);
            list.Add(ButtonStyle.Default, GameText.ExitToWindows, Exit_OnClick);
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

        public override void Update(float fixedDeltaTime)
        {
            SaveButton.Enabled = SavedGame.NotSaving;
            SavingText.Enabled = SavedGame.IsSaving;
            if (SavedGame.IsSaving)
            {
                SavingText.Color = CurrentFlashColor;
            }
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            base.Draw(batch, elapsed);
            batch.End();
        }

        public override void ExitScreen()
        {
            if (Caller == null)
                Universe.Paused = false;
            base.ExitScreen();
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
            ScreenManager.AddScreen(new OptionsScreen(Universe)
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
            if (Caller != null)
                ScreenManager.RemoveScreen(Caller);
            Universe.ExitScreen();
            ScreenManager.AddScreen(new MainMenuScreen());
        }

        void Exit_OnClick(UIButton button)
        {
            if (SavedGame.NotSaving) StarDriveGame.Instance.Exit();
            else GameAudio.NegativeClick();
        }
    }
}
