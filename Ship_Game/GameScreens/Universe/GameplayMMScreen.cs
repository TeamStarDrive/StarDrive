using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDGraphics.Input;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    /// <summary>
    /// In-Game Main Menu
    /// </summary>
    public sealed class GamePlayMenuScreen : GameScreen
    {
        UniverseScreen Universe;
        UILabel SavingText;
        UIButton SaveButton;

        public GamePlayMenuScreen(UniverseScreen screen) : base(screen, toPause: screen)
        {
            Universe = screen;
            IsPopup = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
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
            SaveButton.Enabled = !Universe.IsSaving;
            SavingText.Enabled = Universe.IsSaving;
            if (Universe.IsSaving)
            {
                SavingText.Color = CurrentFlashColor;
            }
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.SafeBegin();
            base.Draw(batch, elapsed);
            batch.SafeEnd();
        }

        public override void ExitScreen()
        {
            base.ExitScreen();
        }

        void Save_OnClick(UIButton button)
        {
            if (!Universe.IsSaving) // no save in progress
                ScreenManager.AddScreen(new SaveGameScreen(Universe));
            else GameAudio.NegativeClick();
        }

        void Load_OnClick(UIButton button)
        {
            if (!Universe.IsSaving)
            {
                ExitScreen(); // exit before opening new screen
                ScreenManager.AddScreen(new LoadSaveScreen(Universe));
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
            if (IsExiting)
                return;

            // safely go to MainMenu, clear all 3D stuff if there's anything left
            ScreenManager.GoToScreen(new MainMenuScreen(), clear3DObjects:true);
        }

        void Exit_OnClick(UIButton button)
        {
            if (!Universe.IsSaving) StarDriveGame.Instance.Exit();
            else GameAudio.NegativeClick();
        }
    }
}
