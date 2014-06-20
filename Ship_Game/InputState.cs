using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Ship_Game
{
	public class InputState
	{
		public KeyboardState CurrentKeyboardState;

		public GamePadState CurrentGamePadState;

		public KeyboardState LastKeyboardState;

		public GamePadState LastGamePadState;

		public MouseState CurrentMouseState;

		public MouseState LastMouseState;

		public int previousScrollWheelValue;

		private Vector2 cursorPosition;

		private Vector2 normalizedCursorPosition;

		public float RightMouseTimer = 0.35f;

		public bool AButtonDown
		{
			get
			{
				if (this.CurrentGamePadState.Buttons.A != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.Buttons.A == ButtonState.Released;
			}
		}

		public bool BButtonDown
		{
			get
			{
				if (this.CurrentGamePadState.Buttons.B != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.Buttons.B == ButtonState.Released;
			}
		}

		public bool BButtonHeld
		{
			get
			{
				return this.CurrentGamePadState.Buttons.B == ButtonState.Pressed;
			}
		}

		public bool C
		{
			get
			{
				return this.IsNewKeyPress(Keys.C);
			}
		}

		public bool Command_OpenInventory
		{
			get
			{
				if (this.IsNewKeyPress(Keys.I))
				{
					return true;
				}
				if (this.CurrentGamePadState.DPad.Down != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.DPad.Down == ButtonState.Released;
			}
		}

		public Vector2 CursorPosition
		{
			get
			{
				return this.cursorPosition;
			}
			set
			{
				this.cursorPosition = value;
			}
		}

		public bool Down
		{
			get
			{
				if (this.IsNewKeyPress(Keys.Down))
				{
					return true;
				}
				if (this.CurrentGamePadState.DPad.Down != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.DPad.Down == ButtonState.Released;
			}
		}

		public bool Escaped
		{
			get
			{
				return this.IsNewKeyPress(Keys.Escape);
			}
		}

		public bool ExitScreen
		{
			get
			{
				if (this.CurrentGamePadState.Buttons.Back != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.Buttons.Back == ButtonState.Released;
			}
		}

		public bool InGameSelect
		{
			get
			{
				if ((this.CurrentMouseState.LeftButton != ButtonState.Pressed ? false : this.LastMouseState.LeftButton == ButtonState.Released))
				{
					return true;
				}
				if (this.CurrentGamePadState.Buttons.A != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.Buttons.A == ButtonState.Released;
			}
		}

		public bool Land
		{
			get
			{
				return this.IsNewKeyPress(Keys.L);
			}
		}

		public bool Left
		{
			get
			{
				if (this.IsNewKeyPress(Keys.Left))
				{
					return true;
				}
				if (this.CurrentGamePadState.DPad.Left != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.DPad.Left == ButtonState.Released;
			}
		}

		public bool LeftShoulderDown
		{
			get
			{
				if (this.CurrentGamePadState.Buttons.LeftShoulder != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.Buttons.LeftShoulder == ButtonState.Released;
			}
		}

		public bool MenuCancel
		{
			get
			{
				if (this.IsNewKeyPress(Keys.Escape) || this.CurrentGamePadState.Buttons.B == ButtonState.Pressed && this.LastGamePadState.Buttons.B == ButtonState.Released)
				{
					return true;
				}
				if (this.CurrentGamePadState.Buttons.Back != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.Buttons.Back == ButtonState.Released;
			}
		}

		public bool MenuDown
		{
			get
			{
				if (this.IsNewKeyPress(Keys.Down) || this.CurrentGamePadState.DPad.Down == ButtonState.Pressed && this.LastGamePadState.DPad.Down == ButtonState.Released)
				{
					return true;
				}
				if (this.CurrentGamePadState.ThumbSticks.Left.Y >= 0f)
				{
					return false;
				}
				return this.LastGamePadState.ThumbSticks.Left.Y >= 0f;
			}
		}

		public bool MenuSelect
		{
			get
			{
				if (this.IsNewKeyPress(Keys.Space) || this.IsNewKeyPress(Keys.Enter) || this.CurrentGamePadState.Buttons.A == ButtonState.Pressed && this.LastGamePadState.Buttons.A == ButtonState.Released)
				{
					return true;
				}
				if (this.CurrentGamePadState.Buttons.Start != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.Buttons.Start == ButtonState.Released;
			}
		}

		public bool MenuUp
		{
			get
			{
				if (this.IsNewKeyPress(Keys.Up) || this.CurrentGamePadState.DPad.Up == ButtonState.Pressed && this.LastGamePadState.DPad.Up == ButtonState.Released)
				{
					return true;
				}
				if (this.CurrentGamePadState.ThumbSticks.Left.Y <= 0f)
				{
					return false;
				}
				return this.LastGamePadState.ThumbSticks.Left.Y <= 0f;
			}
		}

		public Vector2 NormalizedCursorPosition
		{
			get
			{
				return this.normalizedCursorPosition;
			}
			set
			{
				this.normalizedCursorPosition = value;
			}
		}

		public bool OpenMap
		{
			get
			{
				return this.IsNewKeyPress(Keys.M);
			}
		}

		public bool PauseGame
		{
			get
			{
				if (this.IsNewKeyPress(Keys.Escape))
				{
					return true;
				}
				if (this.CurrentGamePadState.Buttons.Start != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.Buttons.Start == ButtonState.Released;
			}
		}

		public bool Right
		{
			get
			{
				if (this.IsNewKeyPress(Keys.Right))
				{
					return true;
				}
				if (this.CurrentGamePadState.DPad.Right != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.DPad.Right == ButtonState.Released;
			}
		}

		public bool RightMouseClick
		{
			get
			{
				return (this.CurrentMouseState.RightButton != ButtonState.Pressed ? false : this.LastMouseState.RightButton == ButtonState.Released);
			}
		}

		public bool RightShoulderDown
		{
			get
			{
				if (this.CurrentGamePadState.Buttons.RightShoulder != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.Buttons.RightShoulder == ButtonState.Released;
			}
		}

		public bool ScrollIn
		{
			get
			{
				return this.CurrentMouseState.ScrollWheelValue > this.previousScrollWheelValue;
			}
		}

		public bool ScrollOut
		{
			get
			{
				return this.CurrentMouseState.ScrollWheelValue < this.previousScrollWheelValue;
			}
		}

		public bool StartButtonDown
		{
			get
			{
				if (this.CurrentGamePadState.Buttons.Start != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.Buttons.Start == ButtonState.Released;
			}
		}

		public bool Tab
		{
			get
			{
				return this.IsNewKeyPress(Keys.Tab);
			}
		}

		public bool Up
		{
			get
			{
				if (this.IsNewKeyPress(Keys.Up))
				{
					return true;
				}
				if (this.CurrentGamePadState.DPad.Up != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.DPad.Up == ButtonState.Released;
			}
		}

		public bool XButtonDown
		{
			get
			{
				if (this.CurrentGamePadState.Buttons.X != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.Buttons.X == ButtonState.Released;
			}
		}

		public bool XButtonHeld
		{
			get
			{
				return this.CurrentGamePadState.Buttons.X == ButtonState.Pressed;
			}
		}

		public bool YButtonDown
		{
			get
			{
				if (this.CurrentGamePadState.Buttons.Y != ButtonState.Pressed)
				{
					return false;
				}
				return this.LastGamePadState.Buttons.Y == ButtonState.Released;
			}
		}

		public bool YButtonHeld
		{
			get
			{
				return this.CurrentGamePadState.Buttons.Y == ButtonState.Pressed;
			}
		}

		public InputState()
		{
		}

		private bool IsNewKeyPress(Keys key)
		{
			if (!this.CurrentKeyboardState.IsKeyDown(key))
			{
				return false;
			}
			return this.LastKeyboardState.IsKeyUp(key);
		}

		public void Update(GameTime gameTime)
		{
			float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (this.CurrentMouseState.RightButton != ButtonState.Pressed)
			{
				this.RightMouseTimer = 0.35f;
			}
			else
			{
				InputState rightMouseTimer = this;
				rightMouseTimer.RightMouseTimer = rightMouseTimer.RightMouseTimer - elapsedTime;
			}
			this.LastKeyboardState = this.CurrentKeyboardState;
			this.LastGamePadState = this.CurrentGamePadState;
			this.LastMouseState = this.CurrentMouseState;
			this.previousScrollWheelValue = this.CurrentMouseState.ScrollWheelValue;
			this.CurrentMouseState = Mouse.GetState();
			this.cursorPosition = new Vector2((float)this.CurrentMouseState.X, (float)this.CurrentMouseState.Y);
			this.CurrentKeyboardState = Keyboard.GetState();
		}
	}
}