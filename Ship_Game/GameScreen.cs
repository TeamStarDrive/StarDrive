using Microsoft.Xna.Framework;
using System;

namespace Ship_Game
{
	public abstract class GameScreen
	{
		public bool IsLoaded;

		private bool isPopup;

		private TimeSpan transitionOnTime = TimeSpan.Zero;

		private TimeSpan transitionOffTime = TimeSpan.Zero;

		private float transitionPosition = 1f;

		private Ship_Game.ScreenState screenState;

		public bool AlwaysUpdate;

		private bool isExiting;

		private bool otherScreenHasFocus;

		private Ship_Game.ScreenManager screenManager;

		public bool IsActive
		{
			get
			{
				if (this.otherScreenHasFocus)
				{
					return false;
				}
				if (this.screenState == Ship_Game.ScreenState.TransitionOn)
				{
					return true;
				}
				return this.screenState == Ship_Game.ScreenState.Active;
			}
		}

		public bool IsExiting
		{
			get
			{
				return this.isExiting;
			}
			protected set
			{
				this.isExiting = value;
			}
		}

		public bool IsPopup
		{
			get
			{
				return this.isPopup;
			}
			protected set
			{
				this.isPopup = value;
			}
		}

		public Ship_Game.ScreenManager ScreenManager
		{
			get
			{
				return this.screenManager;
			}
			internal set
			{
				this.screenManager = value;
			}
		}

		public Ship_Game.ScreenState ScreenState
		{
			get
			{
				return this.screenState;
			}
			protected set
			{
				this.screenState = value;
			}
		}

		public byte TransitionAlpha
		{
			get
			{
				return (byte)(255f - this.TransitionPosition * 255f);
			}
		}

		public TimeSpan TransitionOffTime
		{
			get
			{
				return this.transitionOffTime;
			}
			protected set
			{
				this.transitionOffTime = value;
			}
		}

		public TimeSpan TransitionOnTime
		{
			get
			{
				return this.transitionOnTime;
			}
			protected set
			{
				this.transitionOnTime = value;
			}
		}

		public float TransitionPosition
		{
			get
			{
				return this.transitionPosition;
			}
			protected set
			{
				this.transitionPosition = value;
			}
		}

		protected GameScreen()
		{
		}

		public abstract void Draw(GameTime gameTime);

		public virtual void ExitScreen()
		{
			if (this.TransitionOffTime != TimeSpan.Zero)
			{
				this.isExiting = true;
				return;
			}
			this.ScreenManager.RemoveScreen(this);
		}

		public virtual void HandleInput(InputState input)
		{
		}

		public virtual void LoadContent()
		{
		}

		public virtual void UnloadContent()
		{
		}

		public virtual void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			this.otherScreenHasFocus = otherScreenHasFocus;
			if (!this.isExiting)
			{
				if (coveredByOtherScreen)
				{
					if (this.UpdateTransition(gameTime, this.transitionOffTime, 1))
					{
						this.screenState = Ship_Game.ScreenState.TransitionOff;
						return;
					}
					this.screenState = Ship_Game.ScreenState.Hidden;
					return;
				}
				if (this.UpdateTransition(gameTime, this.transitionOnTime, -1))
				{
					this.screenState = Ship_Game.ScreenState.TransitionOn;
					return;
				}
				this.screenState = Ship_Game.ScreenState.Active;
			}
			else
			{
				this.screenState = Ship_Game.ScreenState.TransitionOff;
				if (!this.UpdateTransition(gameTime, this.transitionOffTime, 1))
				{
					this.ScreenManager.RemoveScreen(this);
					this.isExiting = false;
					return;
				}
			}
		}

		private bool UpdateTransition(GameTime gameTime, TimeSpan time, int direction)
		{
			float transitionDelta;
			transitionDelta = (time != TimeSpan.Zero ? (float)(gameTime.ElapsedGameTime.TotalMilliseconds / time.TotalMilliseconds) : 1f);
			GameScreen gameScreen = this;
			gameScreen.transitionPosition = gameScreen.transitionPosition + transitionDelta * (float)direction;
			if (this.transitionPosition > 0f && this.transitionPosition < 1f)
			{
				return true;
			}
			this.transitionPosition = MathHelper.Clamp(this.transitionPosition, 0f, 1f);
			return false;
		}
	}
}