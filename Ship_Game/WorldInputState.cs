using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Ship_Game
{
	public class WorldInputState
	{
		public KeyboardState CurrentKeyboardState;

		public GamePadState CurrentGamePadState;

		public KeyboardState LastKeyboardState;

		public GamePadState LastGamePadState;

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

		public bool CameraDown
		{
			get
			{
				if (this.IsNewKeyPress(Keys.Down))
				{
					return true;
				}
				return this.CurrentGamePadState.DPad.Down == ButtonState.Pressed;
			}
		}

		public bool CameraLeft
		{
			get
			{
				if (this.IsNewKeyPress(Keys.Left))
				{
					return true;
				}
				return this.CurrentGamePadState.DPad.Left == ButtonState.Pressed;
			}
		}

		public bool CameraRight
		{
			get
			{
				if (this.IsNewKeyPress(Keys.Right))
				{
					return true;
				}
				return this.CurrentGamePadState.DPad.Right == ButtonState.Pressed;
			}
		}

		public bool CameraUp
		{
			get
			{
				if (this.IsNewKeyPress(Keys.Up))
				{
					return true;
				}
				return this.CurrentGamePadState.DPad.Up == ButtonState.Pressed;
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

		public bool ToggleChaseCam
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

		public bool ToggleShowShipInfo
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

		public bool ZoomCameraIn
		{
			get
			{
				return this.CurrentGamePadState.Buttons.RightShoulder == ButtonState.Pressed;
			}
		}

		public bool ZoomCameraOut
		{
			get
			{
				return this.CurrentGamePadState.Buttons.LeftShoulder == ButtonState.Pressed;
			}
		}

		public WorldInputState()
		{
		}

		private bool IsNewKeyPress(Keys key)
		{
			return this.CurrentKeyboardState.IsKeyDown(key);
		}

		public void Update()
		{
			this.LastKeyboardState = this.CurrentKeyboardState;
			this.LastGamePadState = this.CurrentGamePadState;
			this.CurrentKeyboardState = Keyboard.GetState();
			this.CurrentGamePadState = GamePad.GetState(PlayerIndex.One);
		}
	}
}