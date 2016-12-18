using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Xml.Serialization;
using MsgPack.Serialization;

namespace Ship_Game
{
	public abstract class GameplayObject
	{
        public static GraphicsDevice device;
        public static AudioListener audioListener { get; set; }

        [XmlIgnore][MessagePackIgnore] public bool Active = true;
        [XmlIgnore][MessagePackIgnore] protected Cue dieCue;
        [XmlIgnore][MessagePackIgnore] public SolarSystem System;

        [MessagePackMember(0)] public Vector2 Position;
        [MessagePackMember(1)] public Vector2 Center;
        [MessagePackMember(2)] public Vector2 Velocity;
        [MessagePackMember(3)] public float Rotation;

        [MessagePackMember(4)] public Vector2 Dimensions;
        [MessagePackMember(5)] public float Radius = 1f;
        [MessagePackMember(6)] public float Mass = 1f;
        [MessagePackMember(7)] public float Health;
        [MessagePackMember(8)] public bool isInDeepSpace = true;

        [XmlIgnore][MessagePackIgnore] public GameplayObject LastDamagedBy;
        [XmlIgnore][MessagePackIgnore] public bool CollidedThisFrame;

        protected GameplayObject()
		{
		}

		public virtual bool Damage(GameplayObject source, float damageAmount)
		{
			return false;
		}

		public virtual void Die(GameplayObject source, bool cleanupOnly)
		{
			Active = false;
		}

		public virtual void Draw(float elapsedTime, SpriteBatch spriteBatch, Texture2D sprite, Rectangle? sourceRectangle, Color color)
		{
			if (sprite != null)
				spriteBatch?.Draw(sprite, Position, sourceRectangle, color, Rotation, 
                    new Vector2(sprite.Width * 0.5f, sprite.Height * 0.5f), 2f * Radius / Math.Min(sprite.Width, sprite.Height), SpriteEffects.None, 0f);
		}

		public virtual void Draw(float elapsedTime, SpriteBatch spriteBatch, Texture2D sprite, Rectangle? sourceRectangle, Color color, float scaleFactor)
		{
			if (sprite != null) spriteBatch?.Draw(sprite, Position, sourceRectangle, color, Rotation, 
                    new Vector2(sprite.Width * 0.5f, sprite.Height * 0.5f), scaleFactor, SpriteEffects.None, 0f);
		}

		public virtual void DrawShield(float elapsedTime, SpriteBatch spriteBatch, Texture2D sprite, Rectangle? sourceRectangle, Color color, float scaleFactor)
		{
			if (sprite != null)
				spriteBatch?.Draw(sprite, Position, sourceRectangle, color, Rotation, 
                    new Vector2(sprite.Width * 0.5f, sprite.Height * 0.5f), scaleFactor + Radius / 10000f, SpriteEffects.None, 0f);
		}

        public string SystemName => System?.Name ?? "Deep Space";

        public virtual void Initialize()
		{
		}

		public void SetSystem(SolarSystem s)
		{
			this.System = s;
		}

		public virtual bool Touch(GameplayObject target)
		{
			return true;
		}

		public virtual void Update(float elapsedTime)
		{
			this.CollidedThisFrame = false;
		}

		public void UpdateSystem(float elapsedTime)
		{
		}
	}
}