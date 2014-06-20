using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class MessageBoxScreen : GameScreen
	{
		private const string usageText = "A button = Okay";

		private bool pauseMenu;

		private string message;

		private SpriteFont smallFont;

		public UIButton OK;

		public UIButton Cancel;

		private List<UIButton> Buttons = new List<UIButton>();

		private float timer;

		private bool timed;

		private string original = "";

		private MouseState currentMouse;

		private MouseState previousMouse;

		private string toappend;

		public MessageBoxScreen(string message)
		{
			this.message = message;
			this.message = HelperFunctions.parseText(Fonts.Arial12Bold, message, 250f);
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			this.OK = new UIButton()
			{
				Rect = new Rectangle(0, 0, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
				Text = Localizer.Token(15),
				Launches = "OK"
			};
			this.Buttons.Add(this.OK);
			this.Cancel = new UIButton()
			{
				Rect = new Rectangle(0, 0, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
				Text = Localizer.Token(16),
				Launches = "Cancel"
			};
			this.Buttons.Add(this.Cancel);
		}

		public MessageBoxScreen(string message, string oktext, string canceltext)
		{
			this.message = message;
			this.message = HelperFunctions.parseText(Fonts.Arial12Bold, message, 250f);
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			this.OK = new UIButton()
			{
				Rect = new Rectangle(0, 0, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
				Text = oktext,
				Launches = "OK"
			};
			this.Buttons.Add(this.OK);
			this.Cancel = new UIButton()
			{
				Rect = new Rectangle(0, 0, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
				Text = canceltext,
				Launches = "Cancel"
			};
			this.Buttons.Add(this.Cancel);
		}

		public MessageBoxScreen(string message, float Timer)
		{
			this.timed = true;
			this.timer = Timer;
			this.original = message;
			this.message = message;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			this.OK = new UIButton()
			{
				Rect = new Rectangle(0, 0, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
				Text = Localizer.Token(15),
				Launches = "OK"
			};
			this.Buttons.Add(this.OK);
			this.Cancel = new UIButton()
			{
				Rect = new Rectangle(0, 0, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
				Text = Localizer.Token(16),
				Launches = "Cancel"
			};
			this.Buttons.Add(this.Cancel);
		}

		public MessageBoxScreen(string message, bool pauseMenu) : this(message)
		{
			this.pauseMenu = pauseMenu;
		}

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			if (!this.timed)
			{
				Rectangle r = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 135, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - (int)(Fonts.Arial12Bold.MeasureString(this.message).Y + 40f) / 2, 270, (int)(Fonts.Arial12Bold.MeasureString(this.message).Y + 40f) + 15);
				Vector2 textPosition = new Vector2((float)(r.X + r.Width / 2) - Fonts.Arial12Bold.MeasureString(this.message).X / 2f, (float)(r.Y + 10));
				base.ScreenManager.SpriteBatch.Begin();
				Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, r, Color.Black);
				Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, r, Color.Orange);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(this.message, this.toappend), textPosition, Color.White);
				this.OK.Rect.X = r.X + r.Width / 2 + 5;
				this.OK.Rect.Y = r.Y + r.Height - 28;
				this.Cancel.Rect.X = r.X + r.Width / 2 - 73;
				this.Cancel.Rect.Y = r.Y + r.Height - 28;
				foreach (UIButton b in this.Buttons)
				{
					b.Draw(base.ScreenManager.SpriteBatch);
				}
				base.ScreenManager.SpriteBatch.End();
				return;
			}
			this.message = HelperFunctions.parseText(Fonts.Arial12Bold, string.Concat(this.original, this.toappend), 250f);
            //renamed r, textposition
			Rectangle r2 = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 135, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - (int)(Fonts.Arial12Bold.MeasureString(this.message).Y + 40f) / 2, 270, (int)(Fonts.Arial12Bold.MeasureString(this.message).Y + 40f) + 15);
			Vector2 textPosition2 = new Vector2((float)(r2.X + r2.Width / 2) - Fonts.Arial12Bold.MeasureString(this.message).X / 2f, (float)(r2.Y + 10));
			base.ScreenManager.SpriteBatch.Begin();
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, r2, Color.Black);
			Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, r2, Color.Orange);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.message, textPosition2, Color.White);
			this.OK.Rect.X = r2.X + r2.Width / 2 + 5;
			this.OK.Rect.Y = r2.Y + r2.Height - 28;
			this.Cancel.Rect.X = r2.X + r2.Width / 2 - 73;
			this.Cancel.Rect.Y = r2.Y + r2.Height - 28;
			foreach (UIButton b in this.Buttons)
			{
				b.Draw(base.ScreenManager.SpriteBatch);
			}
			base.ScreenManager.SpriteBatch.End();
		}

		public override void HandleInput(InputState input)
		{
			this.currentMouse = input.CurrentMouseState;
			Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
			if (input.MenuSelect && (!this.pauseMenu || input.CurrentGamePadState.Buttons.A == ButtonState.Pressed))
			{
				if (this.Accepted != null)
				{
					this.Accepted(this, EventArgs.Empty);
				}
				this.ExitScreen();
			}
			else if (input.MenuCancel || input.MenuSelect && this.pauseMenu && input.CurrentGamePadState.Buttons.A == ButtonState.Released)
			{
				if (this.Cancelled != null)
				{
					this.Cancelled(this, EventArgs.Empty);
				}
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
					if (b.State != UIButton.PressState.Hover && b.State != UIButton.PressState.Pressed)
					{
						AudioManager.PlayCue("mouse_over4");
					}
					b.State = UIButton.PressState.Hover;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
					{
						b.State = UIButton.PressState.Pressed;
					}
					if (this.currentMouse.LeftButton != ButtonState.Released || this.previousMouse.LeftButton != ButtonState.Pressed)
					{
						continue;
					}
					string launches = b.Launches;
					string str = launches;
					if (launches == null)
					{
						continue;
					}
					if (str == "OK")
					{
						if (this.Accepted != null)
						{
							this.Accepted(this, EventArgs.Empty);
						}
						AudioManager.PlayCue("echo_affirm1");
						this.ExitScreen();
					}
					else if (str == "Cancel")
					{
						if (this.Cancelled != null)
						{
							this.Cancelled(this, EventArgs.Empty);
						}
						this.ExitScreen();
					}
				}
			}
			this.previousMouse = this.currentMouse;
		}

		public override void LoadContent()
		{
			this.smallFont = Fonts.Arial20Bold;
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			MessageBoxScreen messageBoxScreen = this;
			messageBoxScreen.timer = messageBoxScreen.timer - elapsedTime;
			if (this.timed)
			{
				string fmt = "0";
				this.toappend = string.Concat(this.timer.ToString(fmt), " ", Localizer.Token(17));
				if (this.timer <= 0f)
				{
					if (this.Cancelled != null)
					{
						this.Cancelled(this, EventArgs.Empty);
					}
					this.ExitScreen();
				}
			}
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		public event EventHandler<EventArgs> Accepted;

		public event EventHandler<EventArgs> Cancelled;
	}
}