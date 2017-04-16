using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

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
		private MouseState currentMouse;
		private MouseState previousMouse;

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

	    public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			if (SavedGame.IsSaving)
			{
				TimeSpan totalGameTime = gameTime.TotalGameTime;
				float f = (float)Math.Sin((double)totalGameTime.TotalSeconds);
				f = Math.Abs(f) * 255f;
				Color flashColor = new Color(255, 255, 255, (byte)f);
				Vector2 pausePos = new Vector2(ScreenManager.Center().X - Fonts.Pirulen16.MeasureString("Paused").X / 2f, 45 + Fonts.Pirulen16.LineSpacing * 2 + 4);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Saving...", pausePos, flashColor);
			}
			this.window.Draw();
			foreach (UIButton b in Buttons)
			{
				switch (b.Launches)
				{
                    case "Load Game":
                    case "Exit to Main Menu":
                    case "Exit to Windows":
                    case "Save": b.Draw(ScreenManager.SpriteBatch, enabled: SavedGame.NotSaving); break;
                    default:     b.Draw(ScreenManager.SpriteBatch);                               break;
				}
			}
			ScreenManager.SpriteBatch.End();
		}

		public override void HandleInput(InputState input)
		{
			this.currentMouse = input.CurrentMouseState;
			Vector2 mousePos = new Vector2(currentMouse.X, currentMouse.Y);
            if (input.CurrentKeyboardState.IsKeyDown(Keys.O) && !input.LastKeyboardState.IsKeyDown(Keys.O) && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfx("echo_affirm");
                this.ExitScreen();
            }
			if (input.Escaped || input.RightMouseClick)
			{
				this.ExitScreen();
			}
			foreach (UIButton b in this.Buttons)
			{
				if (!HelperFunctions.CheckIntersection(b.Rect, mousePos))
				{
					b.State = UIButton.PressState.Default;
				}
				else
				{
					switch (b.Launches)
					{
						case "Save":
                        case "Load Game":
                        case "Exit to Windows":
                            if (SavedGame.NotSaving) b.State = UIButton.PressState.Hover; break;
                        default: b.State = UIButton.PressState.Hover; break;
					}
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
					{
						b.State = UIButton.PressState.Pressed;
					}
					if (this.currentMouse.LeftButton != ButtonState.Pressed || this.previousMouse.LeftButton != ButtonState.Released)
					{
						continue;
					}
					string launches1 = b.Launches;
					string str1 = launches1;
					if (launches1 == null)
					{
						continue;
					}
					switch (str1)
					{
					    case "Save":
					        if (SavedGame.NotSaving)
					            ScreenManager.AddScreen(new SaveGameScreen(Empire.Universe));
					        else GameAudio.PlaySfx("UI_Misc20");
					        break;
					    case "Load Game":
					        if (SavedGame.NotSaving)
					        {
					            ScreenManager.AddScreen(new LoadSaveScreen(Empire.Universe));
					            ExitScreen();
					        }
					        else GameAudio.PlaySfx("UI_Misc20");
					        break;
					    case "Options":
					        ScreenManager.AddScreen(new OptionsScreen(screen, this, new Rectangle(0, 0, 600, 600))
                            {
                                TitleText  = Localizer.Token(4),
                                MiddleText = Localizer.Token(4004)
                            });
					        break;
					    case "Return to Game": ExitScreen(); break;
					    case "Exit to Main Menu":
                            ExitScreen();
					        if (caller != null) ScreenManager.RemoveScreen(caller);
					        screen.ExitScreen();
					        ScreenManager.AddScreen(new MainMenuScreen());
					        break;
					    case "Exit to Windows":
					        if (SavedGame.NotSaving) Game1.Instance.Exit();
					        else GameAudio.PlaySfx("UI_Misc20");
					        break;
					}
				}
			}
			previousMouse = input.LastMouseState;
			base.HandleInput(input);
		} 
        public override void LoadContent()
		{
            base.LoadContent();

            var para = ScreenManager.GraphicsDevice.PresentationParameters;
            var size = new Vector2(para.BackBufferWidth / 2f, para.BackBufferHeight / 2f);
			window = new Menu2(ScreenManager, new Rectangle((int)size.X - 100, (int)size.Y - 150, 200, 330));

            Vector2 pos = new Vector2(size.X - 84, size.Y - 100);
            Save       = Button(ref pos, "Save",              localization: 300);
            Load       = Button(ref pos, "Load Game",         localization: 2);
            Options    = Button(ref pos, "Options",           localization: 4);
            Return     = Button(ref pos, "Return to Game",    localization: 301);
            ExitToMain = Button(ref pos, "Exit to Main Menu", localization: 302);
            Exit       = Button(ref pos, "Exit to Windows",   localization: 303);
		}  

        public void LoadGraphics()
		{
            var para = ScreenManager.GraphicsDevice.PresentationParameters;
            var size = new Vector2(para.BackBufferWidth / 2f, para.BackBufferHeight / 2f);
            window = new Menu2(ScreenManager, new Rectangle((int)size.X - 100, (int)size.Y - 150, 200, 330));

            Vector2 pos = new Vector2(size.X - 84, size.Y - 100);
            Layout(ref pos, Save);
            Layout(ref pos, Load);
            Layout(ref pos, Options);
            Layout(ref pos, Return);
            Layout(ref pos, ExitToMain);
            Layout(ref pos, Exit);
		}
	}
}