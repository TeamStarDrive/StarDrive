using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class Starfield : IDisposable
	{
		private const int numberOfLayers = 8;

		private const float maximumMovementPerUpdate = 20000f;

		private const int starSize = 1;

		public int numberOfStars = 100;

		private readonly static Color backgroundColor;

		private Vector2 lastPosition;

		private Vector2 position;

		private Starfield.Star[] stars;

		private Texture2D starTexture;

		private Texture2D cloudTexture;

		private Effect cloudEffect;

		private EffectParameter cloudEffectPosition;

		private Texture2D aStar;

		private Texture2D bStar;

		private Texture2D cStar;

		private Texture2D dStar;

		private Texture2D[] starTex = new Texture2D[4];

		private readonly static Color[] layerColors;

		private readonly static float[] movementFactors;

		private int DesiredSmallStars = RandomMath.IntBetween(10,30);

		private int DesiredMedStars = RandomMath.IntBetween(2, 10);

		private int DesiredLargeStars = RandomMath.IntBetween(1, 4);

		private Rectangle starfieldRectangle;

		private Vector2 cloudPos = Vector2.Zero;

		static Starfield()
		{
			Starfield.backgroundColor = new Color(0, 0, 0);
			Color[] color = new Color[] { new Color(255, 255, 255, 160), new Color(255, 255, 255, 160), new Color(255, 255, 255, 255), new Color(255, 255, 255, 255), new Color(255, 255, 255, 255), new Color(255, 255, 255, 110), new Color(255, 255, 255, 220), new Color(255, 255, 255, 90) };
			Starfield.layerColors = color;
			Starfield.movementFactors = new float[] { 0.1f, 0.07f, 7E-05f, 0.0006f, 0.001f, 0.014f, 0.002f, 0.0001f };
		}

		public Starfield(Vector2 position, GraphicsDevice graphicsDevice, GameContentManager contentManager, int numstars)
		{
			numberOfStars = numstars;
			stars = new Star[numberOfStars];
			Reset(position);
		}

		public Starfield(Vector2 position, GraphicsDevice graphicsDevice, GameContentManager contentManager)
		{
			stars = new Star[numberOfStars];
			Reset(position);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
					if (this.starTexture != null)
					{
						this.starTexture.Dispose();
						this.starTexture = null;
					}
				}
			}
		}

		public void Draw(Vector2 position, float zoom, UniverseScreen universe)
		{
			if (zoom > 1f)
			{
				zoom = 1f;
			}
			this.lastPosition = this.position;
			this.position = position;
			Vector2 movement = -1f * (position - this.lastPosition);
			if (movement.Length() > 20000f)
			{
				this.Reset(position);
				return;
			}
			Empire.Universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
			this.cloudEffect.Begin();
			Starfield starfield = this;
			starfield.cloudPos = starfield.cloudPos - ((movement * 0.3f) * zoom);
			this.cloudEffectPosition.SetValue(this.cloudPos);
			this.cloudEffect.CurrentTechnique.Passes[0].Begin();
			Rectangle? nullable = null;
			Empire.Universe.ScreenManager.SpriteBatch.Draw(this.cloudTexture, this.starfieldRectangle, nullable, new Color(255, 0, 0, 255), 0f, Vector2.Zero, SpriteEffects.None, 1f);
			this.cloudEffect.CurrentTechnique.Passes[0].End();
			this.cloudEffect.End();
			Empire.Universe.ScreenManager.SpriteBatch.End();
			Empire.Universe.ScreenManager.SpriteBatch.Begin();
			for (int i = 0; i < (int)this.stars.Length; i++)
			{
				this.stars[i].Position = this.stars[i].Position + ((movement * Starfield.movementFactors[stars[i].whichLayer]) * zoom);
				if (this.stars[i].Position.X < (float)this.starfieldRectangle.X)
				{
					this.stars[i].Position.X = (float)(this.starfieldRectangle.X + this.starfieldRectangle.Width);
					this.stars[i].Position.Y = (float)(this.starfieldRectangle.Y + RandomMath.InRange(starfieldRectangle.Height));
				}
				if (this.stars[i].Position.X > (float)(this.starfieldRectangle.X + this.starfieldRectangle.Width))
				{
					this.stars[i].Position.X = (float)this.starfieldRectangle.X;
					this.stars[i].Position.Y = (float)(this.starfieldRectangle.Y + RandomMath.InRange(starfieldRectangle.Height));
				}
				if (this.stars[i].Position.Y < (float)this.starfieldRectangle.Y)
				{
					this.stars[i].Position.X = (float)(this.starfieldRectangle.X + RandomMath.InRange(starfieldRectangle.Width));
					this.stars[i].Position.Y = (float)(this.starfieldRectangle.Y + this.starfieldRectangle.Height);
				}
				if (this.stars[i].Position.Y > (float)(this.starfieldRectangle.Y + Empire.Universe.Viewport.Height))
				{
					this.stars[i].Position.X = (float)(this.starfieldRectangle.X + RandomMath.InRange(starfieldRectangle.Width));
					this.stars[i].Position.Y = (float)this.starfieldRectangle.Y;
				}
				float alpha = 4.08E+07f / universe.CamHeight;
				if (alpha > 255f)
				{
					alpha = 255f;
				}
				if (alpha < 10f)
				{
					alpha = 0f;
				}
				switch (this.stars[i].whichLayer)
				{
					case 2:
					{
						Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.SmallStars[this.stars[i].whichStar], this.stars[i].Position, Starfield.layerColors[this.stars[i].whichLayer]);
						break;
					}
					case 3:
					{
						Color c = new Color(Starfield.layerColors[this.stars[i].whichLayer].R, Starfield.layerColors[this.stars[i].whichLayer].G, Starfield.layerColors[this.stars[i].whichLayer].B, (byte)alpha);
						Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.MediumStars[this.stars[i].whichStar], this.stars[i].Position, c);
						break;
					}
					case 4:
					{
						Color c1 = new Color(Starfield.layerColors[this.stars[i].whichLayer].R, Starfield.layerColors[this.stars[i].whichLayer].G, Starfield.layerColors[this.stars[i].whichLayer].B, (byte)alpha);
						Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.LargeStars[this.stars[i].whichStar], this.stars[i].Position, c1);
						break;
					}
					default:
					{
						Rectangle r = new Rectangle((int)this.stars[i].Position.X, (int)this.stars[i].Position.Y, this.stars[i].Size, this.stars[i].Size);
						Rectangle? nullable1 = null;
						Empire.Universe.ScreenManager.SpriteBatch.Draw(this.starTex[this.stars[i].whichStar], r, nullable1, Starfield.layerColors[this.stars[i].whichLayer]);
						break;
					}
				}
			}
			Empire.Universe.ScreenManager.SpriteBatch.End();
		}

		public void Draw(Vector2 position)
		{
			this.lastPosition = this.position;
			this.position = position;
			Vector2 movement = -1f * (position - this.lastPosition);
			if (movement.Length() > 20000f)
			{
				this.Reset(position);
				return;
			}
			Empire.Universe.ScreenManager.SpriteBatch.Begin();
			for (int i = 0; i < (int)this.stars.Length; i++)
			{
				this.stars[i].Position = this.stars[i].Position + (movement * Starfield.movementFactors[this.stars[i].whichLayer]);
				if (this.stars[i].Position.X < (float)this.starfieldRectangle.X)
				{
					this.stars[i].Position.X = (float)(this.starfieldRectangle.X + this.starfieldRectangle.Width);
					this.stars[i].Position.Y = (float)(this.starfieldRectangle.Y + RandomMath.Random.Next(this.starfieldRectangle.Height));
				}
				if (this.stars[i].Position.X > (float)(this.starfieldRectangle.X + this.starfieldRectangle.Width))
				{
					this.stars[i].Position.X = (float)this.starfieldRectangle.X;
					this.stars[i].Position.Y = (float)(this.starfieldRectangle.Y + RandomMath.Random.Next(this.starfieldRectangle.Height));
				}
				if (this.stars[i].Position.Y < (float)this.starfieldRectangle.Y)
				{
					this.stars[i].Position.X = (float)(this.starfieldRectangle.X + RandomMath.Random.Next(this.starfieldRectangle.Width));
					this.stars[i].Position.Y = (float)(this.starfieldRectangle.Y + this.starfieldRectangle.Height);
				}
				if (this.stars[i].Position.Y > (float)(this.starfieldRectangle.Y + Empire.Universe.Viewport.Height))
				{
					this.stars[i].Position.X = (float)(this.starfieldRectangle.X + RandomMath.Random.Next(this.starfieldRectangle.Width));
					this.stars[i].Position.Y = (float)this.starfieldRectangle.Y;
				}
				float alpha = 255f;
				switch (this.stars[i].whichLayer)
				{
					case 2:
					{
						Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.SmallStars[this.stars[i].whichStar], this.stars[i].Position, Starfield.layerColors[this.stars[i].whichLayer]);
						break;
					}
					case 3:
					{
						Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.MediumStars[this.stars[i].whichStar], this.stars[i].Position, new Color(Starfield.layerColors[this.stars[i].whichLayer].R, Starfield.layerColors[this.stars[i].whichLayer].G, Starfield.layerColors[this.stars[i].whichLayer].B, (byte)alpha));
						break;
					}
					case 4:
					{
						Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.LargeStars[this.stars[i].whichStar], this.stars[i].Position, new Color(Starfield.layerColors[this.stars[i].whichLayer].R, Starfield.layerColors[this.stars[i].whichLayer].G, Starfield.layerColors[this.stars[i].whichLayer].B, (byte)alpha));
						break;
					}
					default:
					{
						Rectangle? nullable = null;
						Empire.Universe.ScreenManager.SpriteBatch.Draw(this.starTex[this.stars[i].whichStar], new Rectangle((int)this.stars[i].Position.X, (int)this.stars[i].Position.Y, this.stars[i].Size, this.stars[i].Size), nullable, Starfield.layerColors[this.stars[i].whichLayer]);
						break;
					}
				}
			}
			Empire.Universe.ScreenManager.SpriteBatch.End();
		}

		public void Draw(Vector2 position, SpriteBatch spriteBatch)
		{
			this.lastPosition = this.position;
			this.position = position;
			Vector2 movement = -1f * (position - this.lastPosition);
			if (movement.Length() > 20000f)
			{
				this.Reset(position);
				return;
			}
			spriteBatch.End();
			if (this.cloudEffect != null)
			{
				spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
				this.cloudEffect.Begin();
				Starfield starfield = this;
				starfield.cloudPos = starfield.cloudPos - ((movement * 0.3f) * 1f);
				this.cloudEffectPosition.SetValue(this.cloudPos);
				this.cloudEffect.CurrentTechnique.Passes[0].Begin();
				Rectangle? nullable = null;
				spriteBatch.Draw(this.cloudTexture, this.starfieldRectangle, nullable, new Color(255, 0, 0, 255), 0f, Vector2.Zero, SpriteEffects.None, 1f);
				this.cloudEffect.CurrentTechnique.Passes[0].End();
				this.cloudEffect.End();
				spriteBatch.End();
			}
			spriteBatch.Begin();
			for (int i = 0; i < (int)this.stars.Length; i++)
			{
				this.stars[i].Position = this.stars[i].Position + (movement * Starfield.movementFactors[this.stars[i].whichLayer]);
				if (this.stars[i].Position.X < (float)this.starfieldRectangle.X)
				{
					this.stars[i].Position.X = (float)(this.starfieldRectangle.X + this.starfieldRectangle.Width);
					this.stars[i].Position.Y = (float)(this.starfieldRectangle.Y + RandomMath.Random.Next(this.starfieldRectangle.Height));
				}
				if (this.stars[i].Position.X > (float)(this.starfieldRectangle.X + this.starfieldRectangle.Width))
				{
					this.stars[i].Position.X = (float)this.starfieldRectangle.X;
					this.stars[i].Position.Y = (float)(this.starfieldRectangle.Y + RandomMath.Random.Next(this.starfieldRectangle.Height));
				}
				if (this.stars[i].Position.Y < (float)this.starfieldRectangle.Y)
				{
					this.stars[i].Position.X = (float)(this.starfieldRectangle.X + RandomMath.Random.Next(this.starfieldRectangle.Width));
					this.stars[i].Position.Y = (float)(this.starfieldRectangle.Y + this.starfieldRectangle.Height);
				}
				if (this.stars[i].Position.Y > (float)(this.starfieldRectangle.Y + Empire.Universe.Viewport.Height))
				{
					this.stars[i].Position.X = (float)(this.starfieldRectangle.X + RandomMath.Random.Next(this.starfieldRectangle.Width));
					this.stars[i].Position.Y = (float)this.starfieldRectangle.Y;
				}
				float alpha = 255f;
				switch (this.stars[i].whichLayer)
				{
					case 2:
					{
						spriteBatch.Draw(ResourceManager.SmallStars[this.stars[i].whichStar], this.stars[i].Position, Starfield.layerColors[this.stars[i].whichLayer]);
						break;
					}
					case 3:
					{
						spriteBatch.Draw(ResourceManager.MediumStars[this.stars[i].whichStar], this.stars[i].Position, new Color(Starfield.layerColors[this.stars[i].whichLayer].R, Starfield.layerColors[this.stars[i].whichLayer].G, Starfield.layerColors[this.stars[i].whichLayer].B, (byte)alpha));
						break;
					}
					case 4:
					{
						spriteBatch.Draw(ResourceManager.LargeStars[this.stars[i].whichStar], this.stars[i].Position, new Color(Starfield.layerColors[this.stars[i].whichLayer].R, Starfield.layerColors[this.stars[i].whichLayer].G, Starfield.layerColors[this.stars[i].whichLayer].B, (byte)alpha));
						break;
					}
					default:
					{
						Rectangle? nullable1 = null;
						spriteBatch.Draw(this.starTex[this.stars[i].whichStar], new Rectangle((int)this.stars[i].Position.X, (int)this.stars[i].Position.Y, this.stars[i].Size, this.stars[i].Size), nullable1, Starfield.layerColors[this.stars[i].whichLayer]);
						break;
					}
				}
			}
		}

		public void DrawBGOnly(Vector2 position)
		{
			this.lastPosition = this.position;
			this.position = position;
			Vector2 movement = -1f * (position - this.lastPosition);
			int width = Empire.Universe.Viewport.Width;
			Viewport viewport = Empire.Universe.Viewport;
			Rectangle starfieldRectangle = new Rectangle(0, 0, width, viewport.Height);
			Empire.Universe.ScreenManager.SpriteBatch.Begin();
			Empire.Universe.ScreenManager.SpriteBatch.Draw(this.starTexture, starfieldRectangle, Color.Black);
			Empire.Universe.ScreenManager.SpriteBatch.End();
			if (movement.Length() > 20000f)
			{
				this.Reset(position);
				return;
			}
			Empire.Universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
			Empire.Universe.ScreenManager.SpriteBatch.End();
		}

		public void DrawClouds(Vector2 position, float zoom, UniverseScreen universe)
		{
			if (zoom > 1f)
			{
				zoom = 1f;
			}
			this.lastPosition = this.position;
			this.position = position;
			Vector2 vector2 = -1f * (position - this.lastPosition);
			Empire.Universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
			this.cloudEffect.Begin();
			this.cloudPos = new Vector2(3800f, -1200f);
			this.cloudEffectPosition.SetValue(this.cloudPos);
			this.cloudEffect.CurrentTechnique.Passes[0].Begin();
			Rectangle? nullable = null;
			Empire.Universe.ScreenManager.SpriteBatch.Draw(this.cloudTexture, this.starfieldRectangle, nullable, new Color(255, 0, 0, 255), 0f, Vector2.Zero, SpriteEffects.None, 1f);
			this.cloudEffect.CurrentTechnique.Passes[0].End();
			this.cloudEffect.End();
			Empire.Universe.ScreenManager.SpriteBatch.End();
		}

		public void DrawClouds(Vector2 position)
		{
			this.lastPosition = this.position;
			this.position = position;
			Vector2 movement = -1f * (position - this.lastPosition);
			Empire.Universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
			this.cloudEffect.Begin();
			Starfield starfield = this;
			starfield.cloudPos = starfield.cloudPos - movement;
			this.cloudEffectPosition.SetValue(this.cloudPos);
			this.cloudEffect.CurrentTechnique.Passes[0].Begin();
			Rectangle? nullable = null;
			Empire.Universe.ScreenManager.SpriteBatch.Draw(this.cloudTexture, this.starfieldRectangle, nullable, new Color(255, 0, 0, 255), 0f, Vector2.Zero, SpriteEffects.None, 1f);
			this.cloudEffect.CurrentTechnique.Passes[0].End();
			this.cloudEffect.End();
			Empire.Universe.ScreenManager.SpriteBatch.End();
		}

		~Starfield()
		{
			this.Dispose(false);
		}

		public void LoadContent()
		{
			this.cloudTexture = ResourceManager.Texture("clouds");
			this.cloudEffect = Empire.Universe.TransientContent.Load<Effect>("Effects/Clouds");
			this.cloudEffectPosition = this.cloudEffect.Parameters["Position"];
			int width = Empire.Universe.Viewport.Width;
			Viewport viewport = Empire.Universe.Viewport;
			this.starfieldRectangle = new Rectangle(0, 0, width, viewport.Height);
			this.aStar = ResourceManager.Texture("Suns/star_binary");
			this.starTex[0] = this.aStar;
			this.bStar = ResourceManager.Texture("Suns/star_yellow");
			this.starTex[1] = this.bStar;
			this.cStar = ResourceManager.Texture("Suns/star_red");
			this.starTex[2] = this.cStar;
			this.dStar = ResourceManager.Texture("Suns/star_neutron");
			this.starTex[3] = this.dStar;
			this.starTexture = new Texture2D(Empire.Universe.ScreenManager.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
			this.starTexture.SetData<Color>(new Color[] { Color.White });
		}

		public void Reset(Vector2 position)
		{
			int numSmallStars = 0;
			int numMedStars = 0;
			int numLargeStars = 0;
			int viewportWidth = Empire.Universe.Viewport.Width;
			int viewportHeight = Empire.Universe.Viewport.Height;
			for (int i = 0; i < (int)this.stars.Length; i++)
			{
				int depth = i % (int)Starfield.movementFactors.Length;
				if (depth != 2 && depth != 3 && depth != 4)
				{
					this.stars[i].Position = new Vector2((float)RandomMath.Random.Next(0, viewportWidth), (float)RandomMath.Random.Next(0, viewportHeight));
					this.stars[i].Size = 1;
					this.stars[i].whichStar = (int)RandomMath.RandomBetween(0f, 3f);
					this.stars[i].whichLayer = depth;
				}
				else if (depth == 2 && numSmallStars < this.DesiredSmallStars)
				{
					numSmallStars++;
					this.stars[i].Position = new Vector2((float)RandomMath.Random.Next(0, viewportWidth), (float)RandomMath.Random.Next(0, viewportHeight));
					this.stars[i].Size = 1;
					this.stars[i].whichStar = (int)RandomMath.RandomBetween(0f, (float)ResourceManager.SmallStars.Count);
					this.stars[i].whichLayer = 2;
				}
				else if (depth == 3 && numMedStars < this.DesiredMedStars)
				{
					numMedStars++;
					this.stars[i].Position = new Vector2((float)RandomMath.Random.Next(0, viewportWidth), (float)RandomMath.Random.Next(0, viewportHeight));
					this.stars[i].Size = 1;
					this.stars[i].whichStar = (int)RandomMath.RandomBetween(0f, (float)ResourceManager.MediumStars.Count);
					this.stars[i].whichLayer = 3;
				}
				else if (depth != 4 || numLargeStars >= this.DesiredLargeStars)
				{
					this.stars[i].Position = new Vector2((float)RandomMath.Random.Next(0, viewportWidth), (float)RandomMath.Random.Next(0, viewportHeight));
					this.stars[i].Size = 1;
					this.stars[i].whichStar = (int)RandomMath.RandomBetween(0f, 3f);
					this.stars[i].whichLayer = 7;
				}
				else
				{
					numLargeStars++;
					this.stars[i].Position = new Vector2((float)RandomMath.Random.Next(0, viewportWidth), (float)RandomMath.Random.Next(0, viewportHeight));
					this.stars[i].Size = 1;
					this.stars[i].whichStar = (int)RandomMath.RandomBetween(0f, (float)ResourceManager.LargeStars.Count);
					this.stars[i].whichLayer = 4;
				}
			}
		}

		public void UnloadContent()
		{
			this.cloudTexture = null;
			this.cloudEffect = null;
			this.cloudEffectPosition = null;
			if (this.starTexture != null)
			{
				this.starTexture.Dispose();
				this.starTexture = null;
			}
		}

		private struct Star
		{
			public Vector2 Position;

			public int Size;

			public int whichStar;

			public int whichLayer;
		}
	}
}