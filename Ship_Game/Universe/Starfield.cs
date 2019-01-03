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

		private Star[] stars;

		private Texture2D starTexture;

		private SubTexture cloudTexture;

		private Effect cloudEffect;

		private EffectParameter cloudEffectPosition;

		private SubTexture aStar;

		private SubTexture bStar;

		private SubTexture cStar;

		private SubTexture dStar;

		private SubTexture[] starTex = new SubTexture[4];

		private readonly static Color[] layerColors;

		private readonly static float[] movementFactors;

		private int DesiredSmallStars = RandomMath.IntBetween(10,30);

		private int DesiredMedStars = RandomMath.IntBetween(2, 10);

		private int DesiredLargeStars = RandomMath.IntBetween(1, 4);

		private Rectangle starfieldRectangle;

		private Vector2 cloudPos = Vector2.Zero;

		static Starfield()
		{
			backgroundColor = new Color(0, 0, 0);
			Color[] color = { new Color(255, 255, 255, 160), new Color(255, 255, 255, 160), new Color(255, 255, 255, 255), new Color(255, 255, 255, 255), new Color(255, 255, 255, 255), new Color(255, 255, 255, 110), new Color(255, 255, 255, 220), new Color(255, 255, 255, 90) };
			layerColors = color;
			movementFactors = new[] { 0.1f, 0.07f, 7E-05f, 0.0006f, 0.001f, 0.014f, 0.002f, 0.0001f };
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
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
					if (starTexture != null)
					{
						starTexture.Dispose();
						starTexture = null;
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
			lastPosition = this.position;
			this.position = position;
			Vector2 movement = -1f * (position - lastPosition);
			if (movement.Length() > 20000f)
			{
				Reset(position);
				return;
			}
			Empire.Universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
			cloudEffect.Begin();
			cloudPos = cloudPos - ((movement * 0.3f) * zoom);
			cloudEffectPosition.SetValue(cloudPos);
			cloudEffect.CurrentTechnique.Passes[0].Begin();
			Empire.Universe.ScreenManager.SpriteBatch.Draw(cloudTexture, starfieldRectangle, new Color(255, 0, 0, 255), 0f, Vector2.Zero, SpriteEffects.None, 1f);
			cloudEffect.CurrentTechnique.Passes[0].End();
			cloudEffect.End();
			Empire.Universe.ScreenManager.SpriteBatch.End();
			Empire.Universe.ScreenManager.SpriteBatch.Begin();
			for (int i = 0; i < stars.Length; i++)
			{
                Star star = stars[i];
                star.Position = star.Position + ((movement * movementFactors[star.whichLayer]) * zoom);
				if (star.Position.X < starfieldRectangle.X)
				{
					star.Position.X = starfieldRectangle.X + starfieldRectangle.Width;
					star.Position.Y = starfieldRectangle.Y + RandomMath.InRange(starfieldRectangle.Height);
				}
				if (star.Position.X > starfieldRectangle.X + starfieldRectangle.Width)
				{
					star.Position.X = starfieldRectangle.X;
					star.Position.Y = starfieldRectangle.Y + RandomMath.InRange(starfieldRectangle.Height);
				}
				if (star.Position.Y < starfieldRectangle.Y)
				{
					star.Position.X = starfieldRectangle.X + RandomMath.InRange(starfieldRectangle.Width);
					star.Position.Y = starfieldRectangle.Y + starfieldRectangle.Height;
				}
				if (star.Position.Y > starfieldRectangle.Y + Empire.Universe.Viewport.Height)
				{
					star.Position.X = starfieldRectangle.X + RandomMath.InRange(starfieldRectangle.Width);
					star.Position.Y = starfieldRectangle.Y;
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
				switch (star.whichLayer)
				{
					case 2:
					{
						Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.SmallStars[star.whichStar], star.Position, layerColors[star.whichLayer]);
						break;
					}
					case 3:
					{
						Color c = new Color(layerColors[star.whichLayer].R, layerColors[star.whichLayer].G, layerColors[star.whichLayer].B, (byte)alpha);
						Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.MediumStars[star.whichStar], star.Position, c);
						break;
					}
					case 4:
					{
						Color c1 = new Color(layerColors[star.whichLayer].R, layerColors[star.whichLayer].G, layerColors[star.whichLayer].B, (byte)alpha);
						Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.LargeStars[star.whichStar], star.Position, c1);
						break;
					}
					default:
					{
						var r = new Rectangle((int)star.Position.X, (int)star.Position.Y, star.Size, star.Size);
						Empire.Universe.ScreenManager.SpriteBatch.Draw(
                            starTex[star.whichStar], r, layerColors[star.whichLayer]);
						break;
					}
				}
			}
			Empire.Universe.ScreenManager.SpriteBatch.End();
		}

		public void Draw(Vector2 position)
		{
			lastPosition = this.position;
			this.position = position;
			Vector2 movement = -1f * (position - lastPosition);
			if (movement.Length() > 20000f)
			{
				Reset(position);
				return;
			}
			Empire.Universe.ScreenManager.SpriteBatch.Begin();
			for (int i = 0; i < stars.Length; i++)
			{
                Star star = stars[i];
				star.Position = star.Position + (movement * movementFactors[star.whichLayer]);
				if (star.Position.X < starfieldRectangle.X)
				{
					star.Position.X = starfieldRectangle.X + starfieldRectangle.Width;
					star.Position.Y = starfieldRectangle.Y + RandomMath.Random.Next(starfieldRectangle.Height);
				}
				if (star.Position.X > starfieldRectangle.X + starfieldRectangle.Width)
				{
					star.Position.X = starfieldRectangle.X;
					star.Position.Y = starfieldRectangle.Y + RandomMath.Random.Next(starfieldRectangle.Height);
				}
				if (star.Position.Y < starfieldRectangle.Y)
				{
					star.Position.X = starfieldRectangle.X + RandomMath.Random.Next(starfieldRectangle.Width);
					star.Position.Y = starfieldRectangle.Y + starfieldRectangle.Height;
				}
				if (star.Position.Y > starfieldRectangle.Y + Empire.Universe.Viewport.Height)
				{
					star.Position.X = starfieldRectangle.X + RandomMath.Random.Next(starfieldRectangle.Width);
					star.Position.Y = starfieldRectangle.Y;
				}
				float alpha = 255f;
				switch (star.whichLayer)
				{
					case 2:
					{
						Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.SmallStars[star.whichStar], star.Position, layerColors[star.whichLayer]);
						break;
					}
					case 3:
					{
						Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.MediumStars[star.whichStar], star.Position, new Color(layerColors[star.whichLayer].R, layerColors[star.whichLayer].G, layerColors[star.whichLayer].B, (byte)alpha));
						break;
					}
					case 4:
					{
						Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.LargeStars[star.whichStar], star.Position, new Color(layerColors[star.whichLayer].R, layerColors[star.whichLayer].G, layerColors[star.whichLayer].B, (byte)alpha));
						break;
					}
					default:
					{
						Empire.Universe.ScreenManager.SpriteBatch.Draw(starTex[star.whichStar], 
                            new Rectangle((int)star.Position.X, (int)star.Position.Y, star.Size, star.Size),
                            layerColors[star.whichLayer]);
						break;
					}
				}
			}
			Empire.Universe.ScreenManager.SpriteBatch.End();
		}

		public void Draw(Vector2 position, SpriteBatch spriteBatch)
		{
			lastPosition = this.position;
			this.position = position;
			Vector2 movement = -1f * (position - lastPosition);
			if (movement.Length() > 20000f)
			{
				Reset(position);
				return;
			}
			spriteBatch.End();
			if (cloudEffect != null)
			{
				spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
				cloudEffect.Begin();
				Starfield starfield = this;
				starfield.cloudPos = starfield.cloudPos - ((movement * 0.3f) * 1f);
				cloudEffectPosition.SetValue(cloudPos);
				cloudEffect.CurrentTechnique.Passes[0].Begin();
				spriteBatch.Draw(cloudTexture, starfieldRectangle, new Color(255, 0, 0, 255), 0f, Vector2.Zero, SpriteEffects.None, 1f);
				cloudEffect.CurrentTechnique.Passes[0].End();
				cloudEffect.End();
				spriteBatch.End();
			}
			spriteBatch.Begin();
			for (int i = 0; i < stars.Length; i++)
			{
                Star star = stars[i];
				star.Position = star.Position + (movement * movementFactors[star.whichLayer]);
				if (star.Position.X < starfieldRectangle.X)
				{
					star.Position.X = starfieldRectangle.X + starfieldRectangle.Width;
					star.Position.Y = starfieldRectangle.Y + RandomMath.Random.Next(starfieldRectangle.Height);
				}
				if (star.Position.X > starfieldRectangle.X + starfieldRectangle.Width)
				{
					star.Position.X = starfieldRectangle.X;
					star.Position.Y = starfieldRectangle.Y + RandomMath.Random.Next(starfieldRectangle.Height);
				}
				if (star.Position.Y < starfieldRectangle.Y)
				{
					star.Position.X = starfieldRectangle.X + RandomMath.Random.Next(starfieldRectangle.Width);
					star.Position.Y = starfieldRectangle.Y + starfieldRectangle.Height;
				}
				if (star.Position.Y > starfieldRectangle.Y + Empire.Universe.Viewport.Height)
				{
					star.Position.X = starfieldRectangle.X + RandomMath.Random.Next(starfieldRectangle.Width);
					star.Position.Y = starfieldRectangle.Y;
				}
				float alpha = 255f;
				switch (star.whichLayer)
				{
					case 2:
					{
						spriteBatch.Draw(ResourceManager.SmallStars[star.whichStar], star.Position, layerColors[star.whichLayer]);
						break;
					}
					case 3:
					{
						spriteBatch.Draw(ResourceManager.MediumStars[star.whichStar], star.Position, new Color(layerColors[star.whichLayer].R, layerColors[star.whichLayer].G, layerColors[star.whichLayer].B, (byte)alpha));
						break;
					}
					case 4:
					{
						spriteBatch.Draw(ResourceManager.LargeStars[star.whichStar], star.Position, new Color(layerColors[star.whichLayer].R, layerColors[star.whichLayer].G, layerColors[star.whichLayer].B, (byte)alpha));
						break;
					}
					default:
					{
						spriteBatch.Draw(starTex[star.whichStar], 
                            new Rectangle((int)star.Position.X, (int)star.Position.Y, star.Size, star.Size),
                            layerColors[star.whichLayer]);
						break;
					}
				}
			}
		}

		public void DrawBGOnly(Vector2 position)
		{
			lastPosition = this.position;
			this.position = position;
			Vector2 movement = -1f * (position - lastPosition);
			int width = Empire.Universe.Viewport.Width;
			Viewport viewport = Empire.Universe.Viewport;
			Rectangle starfieldRectangle = new Rectangle(0, 0, width, viewport.Height);
			Empire.Universe.ScreenManager.SpriteBatch.Begin();
			Empire.Universe.ScreenManager.SpriteBatch.Draw(starTexture, starfieldRectangle, Color.Black);
			Empire.Universe.ScreenManager.SpriteBatch.End();
			if (movement.Length() > 20000f)
			{
				Reset(position);
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
			lastPosition = this.position;
			this.position = position;
			Vector2 vector2 = -1f * (position - lastPosition);
			Empire.Universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
			cloudEffect.Begin();
			cloudPos = new Vector2(3800f, -1200f);
			cloudEffectPosition.SetValue(cloudPos);
			cloudEffect.CurrentTechnique.Passes[0].Begin();
			Empire.Universe.ScreenManager.SpriteBatch.Draw(cloudTexture, starfieldRectangle, new Color(255, 0, 0, 255), 0f, Vector2.Zero, SpriteEffects.None, 1f);
			cloudEffect.CurrentTechnique.Passes[0].End();
			cloudEffect.End();
			Empire.Universe.ScreenManager.SpriteBatch.End();
		}

		public void DrawClouds(Vector2 position)
		{
			lastPosition = this.position;
			this.position = position;
			Vector2 movement = -1f * (position - lastPosition);
			Empire.Universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
			cloudEffect.Begin();
			Starfield starfield = this;
			starfield.cloudPos = starfield.cloudPos - movement;
			cloudEffectPosition.SetValue(cloudPos);
			cloudEffect.CurrentTechnique.Passes[0].Begin();
			Empire.Universe.ScreenManager.SpriteBatch.Draw(cloudTexture, starfieldRectangle, new Color(255, 0, 0, 255), 0f, Vector2.Zero, SpriteEffects.None, 1f);
			cloudEffect.CurrentTechnique.Passes[0].End();
			cloudEffect.End();
			Empire.Universe.ScreenManager.SpriteBatch.End();
		}

		~Starfield()
		{
			Dispose(false);
		}

		public void LoadContent()
		{
			cloudTexture = ResourceManager.Texture("clouds");
			cloudEffect = Empire.Universe.TransientContent.Load<Effect>("Effects/Clouds");
			cloudEffectPosition = cloudEffect.Parameters["Position"];
			int width = Empire.Universe.Viewport.Width;
			Viewport viewport = Empire.Universe.Viewport;
			starfieldRectangle = new Rectangle(0, 0, width, viewport.Height);
			aStar = ResourceManager.Texture("Suns/star_binary");
			starTex[0] = aStar;
			bStar = ResourceManager.Texture("Suns/star_yellow");
			starTex[1] = bStar;
			cStar = ResourceManager.Texture("Suns/star_red");
			starTex[2] = cStar;
			dStar = ResourceManager.Texture("Suns/star_neutron");
			starTex[3] = dStar;
			starTexture = new Texture2D(Empire.Universe.ScreenManager.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
			starTexture.SetData(new[] { Color.White });
		}

		public void Reset(Vector2 position)
		{
			int numSmallStars = 0;
			int numMedStars = 0;
			int numLargeStars = 0;
			int viewportWidth = Empire.Universe.Viewport.Width;
			int viewportHeight = Empire.Universe.Viewport.Height;
			for (int i = 0; i < stars.Length; i++)
			{
                Star star = stars[i];
				int depth = i % movementFactors.Length;
				if (depth != 2 && depth != 3 && depth != 4)
				{
					star.Position = new Vector2(RandomMath.Random.Next(0, viewportWidth), RandomMath.Random.Next(0, viewportHeight));
					star.Size = 1;
					star.whichStar = (int)RandomMath.RandomBetween(0f, 3f);
					star.whichLayer = depth;
				}
				else if (depth == 2 && numSmallStars < DesiredSmallStars)
				{
					numSmallStars++;
					star.Position = new Vector2(RandomMath.Random.Next(0, viewportWidth), RandomMath.Random.Next(0, viewportHeight));
					star.Size = 1;
					star.whichStar = (int)RandomMath.RandomBetween(0f, ResourceManager.SmallStars.Count);
					star.whichLayer = 2;
				}
				else if (depth == 3 && numMedStars < DesiredMedStars)
				{
					numMedStars++;
					star.Position = new Vector2(RandomMath.Random.Next(0, viewportWidth), RandomMath.Random.Next(0, viewportHeight));
					star.Size = 1;
					star.whichStar = (int)RandomMath.RandomBetween(0f, ResourceManager.MediumStars.Count);
					star.whichLayer = 3;
				}
				else if (depth != 4 || numLargeStars >= DesiredLargeStars)
				{
					star.Position = new Vector2(RandomMath.Random.Next(0, viewportWidth), RandomMath.Random.Next(0, viewportHeight));
					star.Size = 1;
					star.whichStar = (int)RandomMath.RandomBetween(0f, 3f);
					star.whichLayer = 7;
				}
				else
				{
					numLargeStars++;
					star.Position = new Vector2(RandomMath.Random.Next(0, viewportWidth), RandomMath.Random.Next(0, viewportHeight));
					star.Size = 1;
					star.whichStar = (int)RandomMath.RandomBetween(0f, ResourceManager.LargeStars.Count);
					star.whichLayer = 4;
				}
			}
		}

		public void UnloadContent()
		{
			cloudTexture = null;
			cloudEffect = null;
			cloudEffectPosition = null;
			if (starTexture != null)
			{
				starTexture.Dispose();
				starTexture = null;
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