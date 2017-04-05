using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Ship_Game
{
	public sealed class InputState
	{
		public KeyboardState CurrentKeyboardState;

		public GamePadState CurrentGamePadState;

		public KeyboardState LastKeyboardState;

		public GamePadState LastGamePadState;

		public MouseState CurrentMouseState;

		public MouseState LastMouseState;

		public int previousScrollWheelValue;

		private Vector2 cursorPosition;

	    public float RightMouseTimer = 0.35f;

		public bool AButtonDown
		{
			get
			{
				if (CurrentGamePadState.Buttons.A != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.Buttons.A == ButtonState.Released;
			}
		}

		public bool BButtonDown
		{
			get
			{
				if (CurrentGamePadState.Buttons.B != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.Buttons.B == ButtonState.Released;
			}
		}

		public bool BButtonHeld
		{
			get
			{
				return CurrentGamePadState.Buttons.B == ButtonState.Pressed;
			}
		}

		public bool C => IsNewKeyPress(Keys.C);

	    public bool CommandOpenInventory
		{
			get
			{
				if (IsNewKeyPress(Keys.I))
				{
					return true;
				}
				if (CurrentGamePadState.DPad.Down != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.DPad.Down == ButtonState.Released;
			}
		}

		public Vector2 CursorPosition
		{
			get => cursorPosition;
		    set => cursorPosition = value;
		}

		public bool Down
		{
			get
			{
				if (IsNewKeyPress(Keys.Down))
				{
					return true;
				}
				if (CurrentGamePadState.DPad.Down != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.DPad.Down == ButtonState.Released;
			}
		}

		public bool Escaped => IsNewKeyPress(Keys.Escape);

	    public bool ExitScreen
		{
			get
			{
				if (CurrentGamePadState.Buttons.Back != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.Buttons.Back == ButtonState.Released;
			}
		}

		public bool InGameSelect
		{
			get
			{
				if (CurrentMouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released)
				{
					return true;
				}
				if (CurrentGamePadState.Buttons.A != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.Buttons.A == ButtonState.Released;
			}
		}

		public bool Land => IsNewKeyPress(Keys.L);

	    public bool Left
		{
			get
			{
				if (IsNewKeyPress(Keys.Left))
				{
					return true;
				}
				if (CurrentGamePadState.DPad.Left != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.DPad.Left == ButtonState.Released;
			}
		}

		public bool LeftShoulderDown
		{
			get
			{
				if (CurrentGamePadState.Buttons.LeftShoulder != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.Buttons.LeftShoulder == ButtonState.Released;
			}
		}

		public bool MenuCancel
		{
			get
			{
				if (IsNewKeyPress(Keys.Escape) || CurrentGamePadState.Buttons.B == ButtonState.Pressed && LastGamePadState.Buttons.B == ButtonState.Released)
				{
					return true;
				}
				if (CurrentGamePadState.Buttons.Back != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.Buttons.Back == ButtonState.Released;
			}
		}

		public bool MenuDown
		{
			get
			{
				if (IsNewKeyPress(Keys.Down) || CurrentGamePadState.DPad.Down == ButtonState.Pressed && LastGamePadState.DPad.Down == ButtonState.Released)
				{
					return true;
				}
				if (CurrentGamePadState.ThumbSticks.Left.Y >= 0f)
				{
					return false;
				}
				return LastGamePadState.ThumbSticks.Left.Y >= 0f;
			}
		}

		public bool MenuSelect
		{
			get
			{
				if (IsNewKeyPress(Keys.Space) || IsNewKeyPress(Keys.Enter) || CurrentGamePadState.Buttons.A == ButtonState.Pressed && LastGamePadState.Buttons.A == ButtonState.Released)
				{
					return true;
				}
				if (CurrentGamePadState.Buttons.Start != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.Buttons.Start == ButtonState.Released;
			}
		}

		public bool MenuUp
		{
			get
			{
				if (IsNewKeyPress(Keys.Up) || CurrentGamePadState.DPad.Up == ButtonState.Pressed && LastGamePadState.DPad.Up == ButtonState.Released)
				{
					return true;
				}
				if (CurrentGamePadState.ThumbSticks.Left.Y <= 0f)
				{
					return false;
				}
				return LastGamePadState.ThumbSticks.Left.Y <= 0f;
			}
		}

		public Vector2 NormalizedCursorPosition { get; set; }

	    public bool OpenMap => IsNewKeyPress(Keys.M);

	    public bool PauseGame
		{
			get
			{
				if (IsNewKeyPress(Keys.Escape))
				{
					return true;
				}
				if (CurrentGamePadState.Buttons.Start != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.Buttons.Start == ButtonState.Released;
			}
		}

		public bool Right
		{
			get
			{
				if (IsNewKeyPress(Keys.Right))
				{
					return true;
				}
				if (CurrentGamePadState.DPad.Right != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.DPad.Right == ButtonState.Released;
			}
		}

		public bool RightMouseClick => (CurrentMouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released);

	    public bool RightShoulderDown
		{
			get
			{
				if (CurrentGamePadState.Buttons.RightShoulder != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.Buttons.RightShoulder == ButtonState.Released;
			}
		}

		public bool ScrollIn => CurrentMouseState.ScrollWheelValue > previousScrollWheelValue;

	    public bool ScrollOut => CurrentMouseState.ScrollWheelValue < previousScrollWheelValue;

	    public bool StartButtonDown
		{
			get
			{
				if (CurrentGamePadState.Buttons.Start != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.Buttons.Start == ButtonState.Released;
			}
		}

		public bool Tab => IsNewKeyPress(Keys.Tab);

	    public bool Up
		{
			get
			{
				if (IsNewKeyPress(Keys.Up))
				{
					return true;
				}
				if (CurrentGamePadState.DPad.Up != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.DPad.Up == ButtonState.Released;
			}
		}

		public bool XButtonDown
		{
			get
			{
				if (CurrentGamePadState.Buttons.X != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.Buttons.X == ButtonState.Released;
			}
		}

		public bool XButtonHeld => CurrentGamePadState.Buttons.X == ButtonState.Pressed;

	    public bool YButtonDown
		{
			get
			{
				if (CurrentGamePadState.Buttons.Y != ButtonState.Pressed)
				{
					return false;
				}
				return LastGamePadState.Buttons.Y == ButtonState.Released;
			}
		}

		public bool YButtonHeld => CurrentGamePadState.Buttons.Y == ButtonState.Pressed;

	    private bool IsNewKeyPress(Keys key)
	    {
	        return CurrentKeyboardState.IsKeyDown(key) && LastKeyboardState.IsKeyUp(key);
	    }

		public void Update(GameTime gameTime)
		{
			float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (CurrentMouseState.RightButton != ButtonState.Pressed)
			{
				RightMouseTimer = 0.35f;
			}
			else
			{
				InputState rightMouseTimer = this;
				rightMouseTimer.RightMouseTimer = rightMouseTimer.RightMouseTimer - elapsedTime;
			}
			LastKeyboardState = CurrentKeyboardState;
			LastGamePadState = CurrentGamePadState;
			LastMouseState = CurrentMouseState;
			previousScrollWheelValue = CurrentMouseState.ScrollWheelValue;
			CurrentMouseState = Mouse.GetState();
			cursorPosition = new Vector2(CurrentMouseState.X, CurrentMouseState.Y);
			CurrentKeyboardState = Keyboard.GetState();
		}
	}
}