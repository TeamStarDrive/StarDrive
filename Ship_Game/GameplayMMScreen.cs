using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ship_Game
{
	public class GameplayMMScreen : GameScreen, IDisposable
	{
		private Vector2 Cursor = Vector2.Zero;

		private UniverseScreen screen;

		private GameScreen caller;

		private Menu2 window;

		private List<UIButton> Buttons = new List<UIButton>();

		private UIButton Save;

		private UIButton Load;

		private UIButton Options;

		private UIButton Return;

		private UIButton Exit;

		private UIButton ExitToMain;

		private MouseState currentMouse;

		private MouseState previousMouse;

		//private float transitionElapsedTime;

		public GameplayMMScreen(UniverseScreen screen)
		{
			this.screen = screen;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public GameplayMMScreen(UniverseScreen screen, GameScreen caller)
		{
			this.caller = caller;
			this.screen = screen;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			if (SavedGame.thread != null && SavedGame.thread.IsAlive)
			{
				TimeSpan totalGameTime = gameTime.TotalGameTime;
				float f = (float)Math.Sin((double)totalGameTime.TotalSeconds);
				f = Math.Abs(f) * 255f;
				Color flashColor = new Color(255, 255, 255, (byte)f);
				Vector2 pausePos = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Pirulen16.MeasureString("Paused").X / 2f, (float)(45 + Fonts.Pirulen16.LineSpacing * 2 + 4));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, "Saving...", pausePos, flashColor);
			}
			this.window.Draw();
			foreach (UIButton b in this.Buttons)
			{
				string launches = b.Launches;
				string str = launches;
				if (launches != null)
				{
					if (str == "Save")
					{
						if (SavedGame.thread == null || SavedGame.thread != null && !SavedGame.thread.IsAlive)
						{
							b.Draw(base.ScreenManager.SpriteBatch);
							continue;
						}
						else
						{
							b.DrawInActive(base.ScreenManager.SpriteBatch);
							continue;
						}
					}
					else if (str == "Load Game")
					{
						if (SavedGame.thread == null || SavedGame.thread != null && !SavedGame.thread.IsAlive)
						{
							b.Draw(base.ScreenManager.SpriteBatch);
							continue;
						}
						else
						{
							b.DrawInActive(base.ScreenManager.SpriteBatch);
							continue;
						}
					}
					else if (str != "Exit to Main Menu")
					{
						if (str == "Exit to Windows")
						{
							if (SavedGame.thread == null || SavedGame.thread != null && !SavedGame.thread.IsAlive)
							{
								b.Draw(base.ScreenManager.SpriteBatch);
								continue;
							}
							else
							{
								b.DrawInActive(base.ScreenManager.SpriteBatch);
								continue;
							}
						}
					}
					else if (SavedGame.thread == null || SavedGame.thread != null && !SavedGame.thread.IsAlive)
					{
						b.Draw(base.ScreenManager.SpriteBatch);
						continue;
					}
					else
					{
						b.DrawInActive(base.ScreenManager.SpriteBatch);
						continue;
					}
				}
				b.Draw(base.ScreenManager.SpriteBatch);
			}
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			base.ExitScreen();
		}

		/*protected override void Finalize()
		{
			try
			{
				this.Dispose(false);
			}
			finally
			{
				base.Finalize();
			}
		}*/
        ~GameplayMMScreen() {
            //should implicitly do the same thing as the original bad finalize
        }

		public override void HandleInput(InputState input)
		{
			this.currentMouse = input.CurrentMouseState;
			Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
            if (input.CurrentKeyboardState.IsKeyDown(Keys.O) && !input.LastKeyboardState.IsKeyDown(Keys.O) && !GlobalStats.TakingInput)
            {
                AudioManager.PlayCue("echo_affirm");
                this.ExitScreen();
            }
			if (input.Escaped || input.RightMouseClick)
			{
				this.ExitScreen();
			}
			foreach (UIButton b in this.Buttons)
			{
				if (!HelperFunctions.CheckIntersection(b.Rect, MousePos))
				{
					b.State = UIButton.PressState.Normal;
				}
				else
				{
					string launches = b.Launches;
					string str = launches;
					if (launches != null)
					{
						if (str == "Save")
						{
							if (SavedGame.thread == null || SavedGame.thread != null && !SavedGame.thread.IsAlive)
							{
								b.State = UIButton.PressState.Hover;
							}
						}
						else if (str == "Load Game")
						{
							if (SavedGame.thread == null || SavedGame.thread != null && !SavedGame.thread.IsAlive)
							{
								b.State = UIButton.PressState.Hover;
							}
						}
						else if (str == "Options")
						{
							b.State = UIButton.PressState.Hover;
						}
						else if (str == "Return to Game")
						{
							b.State = UIButton.PressState.Hover;
						}
						else if (str == "Exit to Main Menu")
						{
							b.State = UIButton.PressState.Hover;
						}
						else if (str == "Exit to Windows")
						{
							if (SavedGame.thread == null || SavedGame.thread != null && !SavedGame.thread.IsAlive)
							{
								b.State = UIButton.PressState.Hover;
							}
						}
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
					if (str1 == "Save")
					{
						if (SavedGame.thread == null || SavedGame.thread != null && !SavedGame.thread.IsAlive)
						{
							SaveGameScreen sgs = new SaveGameScreen(this.screen);
							base.ScreenManager.AddScreen(sgs);
						}
						else
						{
							AudioManager.PlayCue("UI_Misc20");
						}
					}
					else if (str1 == "Load Game")
					{
						if (SavedGame.thread == null || SavedGame.thread != null && !SavedGame.thread.IsAlive)
						{
							LoadSaveScreen lss = new LoadSaveScreen(this.screen);
							base.ScreenManager.AddScreen(lss);
							this.ExitScreen();
						}
						else
						{
							AudioManager.PlayCue("UI_Misc20");
						}
					}
					else if (str1 == "Options")
					{
						OptionsScreen options = new OptionsScreen(this.screen, this, new Rectangle(0, 0, 600, 600))
						{
							TitleText = Localizer.Token(4),
							MiddleText = Localizer.Token(4004)
						};
						base.ScreenManager.AddScreen(options);
					}
					else if (str1 == "Return to Game")
					{
						this.ExitScreen();
					}
					else if (str1 == "Exit to Main Menu")
					{
						this.ExitScreen();
						if (this.caller != null)
						{
							base.ScreenManager.RemoveScreen(this.caller);
						}
						this.screen.ExitScreen();
						base.ScreenManager.AddScreen(new MainMenuScreen());
					}
					else if (str1 == "Exit to Windows")
					{
						if (SavedGame.thread == null || SavedGame.thread != null && !SavedGame.thread.IsAlive)
						{
							Game1.Instance.Exit();
						}
						else
						{
							AudioManager.PlayCue("UI_Misc20");
						}
					}
				}
			}
			this.previousMouse = input.LastMouseState;
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			Vector2 Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
			this.window = new Menu2(base.ScreenManager, new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 100, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 150, 200, 330));
			this.Save = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Launches = "Save",
				Text = Localizer.Token(300)
			};
			this.Buttons.Add(this.Save);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Load = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Text = Localizer.Token(2),
				Launches = "Load Game"
			};
			this.Buttons.Add(this.Load);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Options = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Text = Localizer.Token(4),
				Launches = "Options"
			};
			this.Buttons.Add(this.Options);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Return = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Launches = "Return to Game",
				Text = Localizer.Token(301)
			};
			this.Buttons.Add(this.Return);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.ExitToMain = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Launches = "Exit to Main Menu",
				Text = Localizer.Token(302)
			};
			this.Buttons.Add(this.ExitToMain);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Exit = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Launches = "Exit to Windows",
				Text = Localizer.Token(303)
			};
			this.Buttons.Add(this.Exit);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			base.LoadContent();
		}

		public void LoadGraphics()
		{
			Vector2 Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
			this.window = new Menu2(base.ScreenManager, new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 100, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 150, 200, 330));
			this.Save.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Load.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Options.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Return.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.ExitToMain.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Exit.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
			Cursor.Y = Cursor.Y + (float)(ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}