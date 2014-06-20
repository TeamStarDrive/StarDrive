using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
	public abstract class GameplayObject
	{
		public bool Active = true;

		protected SolarSystem system;

		public static GraphicsDevice device;

		public Vector2 Center;

		protected Vector2 velocity = Vector2.Zero;

		protected float rotation;

		protected float radius = 1f;

		protected float mass = 1f;

		protected bool collidedThisFrame;

		protected Cue dieCue;

		private GameplayObject lastDamagedBy;

		public bool isInDeepSpace = true;

		public static AudioListener audioListener
		{
			get;
			set;
		}

		public bool CollidedThisFrame
		{
			get
			{
				return this.collidedThisFrame;
			}
			set
			{
				this.collidedThisFrame = value;
			}
		}

		public Vector2 Dimensions
		{
			get;
			set;
		}

		public float Health
		{
			get;
			set;
		}

		public GameplayObject LastDamagedBy
		{
			get
			{
				return this.lastDamagedBy;
			}
			set
			{
				this.lastDamagedBy = value;
			}
		}

		public float Mass
		{
			get
			{
				return this.mass;
			}
			set
			{
				this.mass = value;
			}
		}

		public Vector2 Position
		{
			get;
			set;
		}

		public float Radius
		{
			get
			{
				return this.radius;
			}
			set
			{
				this.radius = value;
			}
		}

		public float Rotation
		{
			get
			{
				return this.rotation;
			}
			set
			{
				this.rotation = value;
			}
		}

		public Vector2 Velocity
		{
			get
			{
				return this.velocity;
			}
			set
			{
				this.velocity = value;
			}
		}

		protected GameplayObject()
		{
		}

		public virtual bool Damage(GameplayObject source, float damageAmount)
		{
			return false;
		}

		public virtual void Die(GameplayObject source, bool cleanupOnly)
		{
			this.Active = false;
		}

		public virtual void Draw(float elapsedTime, SpriteBatch spriteBatch, Texture2D sprite, Rectangle? sourceRectangle, Color color)
		{
			if (spriteBatch != null && sprite != null)
			{
				spriteBatch.Draw(sprite, this.Position, sourceRectangle, color, this.rotation, new Vector2((float)sprite.Width / 2f, (float)sprite.Height / 2f), 2f * this.radius / MathHelper.Min((float)sprite.Width, (float)sprite.Height), SpriteEffects.None, 0f);
			}
		}

		public virtual void Draw(float elapsedTime, SpriteBatch spriteBatch, Texture2D sprite, Rectangle? sourceRectangle, Color color, float scaleFactor)
		{
			if (spriteBatch != null && sprite != null)
			{
				spriteBatch.Draw(sprite, this.Position, sourceRectangle, color, this.rotation, new Vector2((float)sprite.Width / 2f, (float)sprite.Height / 2f), scaleFactor, SpriteEffects.None, 0f);
			}
		}

		public virtual void DrawShield(float elapsedTime, SpriteBatch spriteBatch, Texture2D sprite, Rectangle? sourceRectangle, Color color, float scaleFactor)
		{
			if (spriteBatch != null && sprite != null)
			{
				spriteBatch.Draw(sprite, this.Position, sourceRectangle, color, this.rotation, new Vector2((float)sprite.Width / 2f, (float)sprite.Height / 2f), scaleFactor + this.radius / 10000f, SpriteEffects.None, 0f);
			}
		}

		public float findAngleToTarget(Vector2 origin, Vector2 target)
		{
			float theta;
			float tX = target.X;
			float tY = target.Y;
			float centerX = origin.X;
			float centerY = origin.Y;
			float angle_to_target = 0f;
			if (tX > centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 90f - Math.Abs(theta);
			}
			else if (tX > centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 90f + theta * 180f / 3.14159274f;
			}
			else if (tX < centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 270f - Math.Abs(theta);
				angle_to_target = -angle_to_target;
			}
			else if (tX < centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 270f + theta * 180f / 3.14159274f;
				angle_to_target = -angle_to_target;
			}
			if (tX == centerX && tY < centerY)
			{
				angle_to_target = 0f;
			}
			else if (tX > centerX && tY == centerY)
			{
				angle_to_target = 90f;
			}
			else if (tX == centerX && tY > centerY)
			{
				angle_to_target = 180f;
			}
			else if (tX < centerX && tY == centerY)
			{
				angle_to_target = 270f;
			}
			return angle_to_target;
		}

		public SolarSystem GetSystem()
		{
			return this.system;
		}

		public string GetSystemName()
		{
			if (this.system == null)
			{
				return "Deep Space";
			}
			return this.system.Name;
		}

		public virtual void Initialize()
		{
		}

		public void SetSystem(SolarSystem s)
		{
			this.system = s;
		}

		public virtual bool Touch(GameplayObject target)
		{
			return true;
		}

		public virtual void Update(float elapsedTime)
		{
			this.collidedThisFrame = false;
		}

		public void UpdateSystem(float elapsedTime)
		{
		}
	}
}