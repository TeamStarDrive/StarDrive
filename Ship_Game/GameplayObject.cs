using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace Ship_Game
{
	public abstract class GameplayObject
	{
		public bool Active = true;
		protected SolarSystem system; // @todo This has to be lowercase due to serialization... sigh. Can we change it??
        [XmlIgnore][ScriptIgnore]
        public SolarSystem System { get { return system; } protected set { system = value; } }
        public static GraphicsDevice device;
		public Vector2 Center;
	    protected Cue dieCue;
	    public bool isInDeepSpace = true;

		public static AudioListener audioListener { get; set; }
		public bool CollidedThisFrame { get; set; }
	    public Vector2 Dimensions { get; set; }
		public float Health { get; set; }
		public GameplayObject LastDamagedBy { get; set; }
	    public float Mass { get; set; } = 1f;
	    public Vector2 Position { get; set; }
		public float Radius { get; set; } = 1f;
	    public float Rotation { get; set; }
	    public Vector2 Velocity { get; set; } = Vector2.Zero;

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