using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public abstract class UIElement
	{
		public Rectangle ElementRect;

		public Ship_Game.ScreenManager ScreenManager;

		public Color tColor = new Color(255, 239, 208);

		private TimeSpan transitionOnTime = TimeSpan.Zero;

		private TimeSpan transitionOffTime = TimeSpan.Zero;

		private float transitionPosition = 1f;

		private bool isExiting;

		private UIElement.ElementState elementState = UIElement.ElementState.Closed;

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

		public UIElement.ElementState State
		{
			get
			{
				return this.elementState;
			}
			protected set
			{
				this.elementState = value;
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

		protected UIElement()
		{
		}

		public abstract void Draw(GameTime gameTime);

		public virtual bool HandleInput(InputState input)
		{
			return false;
		}

		public virtual void Update(GameTime gameTime)
		{
			if (this.State == UIElement.ElementState.TransitionOn)
			{
				if (this.UpdateTransition(gameTime, this.transitionOnTime, -1))
				{
					this.State = UIElement.ElementState.TransitionOn;
					return;
				}
				this.State = UIElement.ElementState.Open;
				return;
			}
			if (this.State == UIElement.ElementState.TransitionOff)
			{
				if (this.UpdateTransition(gameTime, this.transitionOffTime, 1))
				{
					this.isExiting = false;
					return;
				}
				this.State = UIElement.ElementState.Closed;
			}
		}

		private bool UpdateTransition(GameTime gameTime, TimeSpan time, int direction)
		{
			float transitionDelta;
			transitionDelta = (time != TimeSpan.Zero ? (float)(gameTime.ElapsedGameTime.TotalMilliseconds / time.TotalMilliseconds) : 1f);
			UIElement uIElement = this;
			uIElement.transitionPosition = uIElement.transitionPosition + transitionDelta * (float)direction;
			if (this.transitionPosition > 0f && this.transitionPosition < 1f)
			{
				return true;
			}
			this.transitionPosition = MathHelper.Clamp(this.transitionPosition, 0f, 1f);
			return false;
		}

		public enum ElementState
		{
			TransitionOn,
			Open,
			TransitionOff,
			Closed
		}
	}
}