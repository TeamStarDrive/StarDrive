using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Ship_Game
{
	public sealed class WorldInputState
	{
		public KeyboardState CurrentKeyboardState;

		public GamePadState CurrentGamePadState;

		public KeyboardState LastKeyboardState;

		public GamePadState LastGamePadState;

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

		public bool CameraDown
		{
			get
			{
				if (IsNewKeyPress(Keys.Down))
				{
					return true;
				}
				return CurrentGamePadState.DPad.Down == ButtonState.Pressed;
			}
		}

		public bool CameraLeft
		{
			get
			{
				if (IsNewKeyPress(Keys.Left))
				{
					return true;
				}
				return CurrentGamePadState.DPad.Left == ButtonState.Pressed;
			}
		}

		public bool CameraRight
		{
			get
			{
				if (IsNewKeyPress(Keys.Right))
				{
					return true;
				}
				return CurrentGamePadState.DPad.Right == ButtonState.Pressed;
			}
		}

		public bool CameraUp
		{
			get
			{
				if (IsNewKeyPress(Keys.Up))
				{
					return true;
				}
				return CurrentGamePadState.DPad.Up == ButtonState.Pressed;
			}
		}

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

		public bool ToggleChaseCam
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

		public bool ToggleShowShipInfo
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

		public bool ZoomCameraIn
		{
			get
			{
				return CurrentGamePadState.Buttons.RightShoulder == ButtonState.Pressed;
			}
		}

		public bool ZoomCameraOut
		{
			get
			{
				return CurrentGamePadState.Buttons.LeftShoulder == ButtonState.Pressed;
			}
		}

		public WorldInputState()
		{
		}

		private bool IsNewKeyPress(Keys key)
		{
			return CurrentKeyboardState.IsKeyDown(key);
		}

		public void Update()
		{
			LastKeyboardState = CurrentKeyboardState;
			LastGamePadState = CurrentGamePadState;
			CurrentKeyboardState = Keyboard.GetState();
			CurrentGamePadState = GamePad.GetState(PlayerIndex.One);
		}
	}
}