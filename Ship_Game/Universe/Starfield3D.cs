using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class Starfield3D : IDisposable
	{
		private const int numberOfStars = 1000;

		private const int numberOfLayers = 8;

		private const float maximumMovementPerUpdate = 128f;

		public Model star;

		private static readonly Color[] layerColors;
		private static readonly float[] movementFactors;
		public Star[] stars;
		private GraphicsDevice graphicsDevice;
		private GameContentManager contentManager;
		private SpriteBatch spriteBatch;
		private Texture2D starTexture;
		public Model starModel;
		private Rectangle starfieldRectangle;


		static Starfield3D()
		{
			Color[] color = { new Color(255, 255, 255, 192), new Color(255, 255, 255, 192), new Color(255, 255, 255, 192), new Color(255, 255, 255, 160), new Color(255, 255, 255, 128), new Color(255, 255, 255, 96), new Color(255, 255, 255, 64), new Color(255, 255, 255, 32) };
			layerColors = color;
			movementFactors = new [] { 0.1f, 0.09f, 0.08f, 0.06f, 0.04f, 0.02f, 0.01f, 0.005f };
		}

		public Starfield3D(Vector2 position, GraphicsDevice graphicsDevice, GameContentManager contentManager)
		{
			this.graphicsDevice = graphicsDevice;
			this.contentManager = contentManager;
			stars = new Star[1000];
		}

		public void InitializeStars()
		{
			for (int i = 0; i < (int)stars.Length; i++)
			{
				stars[i].Position.X = RandomMath.RandomBetween(100000f, 120000f);
				stars[i].Position.Y = RandomMath.RandomBetween(100000f, 120000f);
				stars[i].Depth = RandomMath.RandomBetween(2500f, 10000f);
				stars[i].scale = RandomMath.RandomBetween(1f, 3f);
				stars[i].WorldMatrix = Matrix.Identity * Matrix.CreateTranslation(stars[i].Position.X, stars[i].Position.Y, stars[i].Depth);
			}
		}

		public void LoadContent()
		{
			starModel = contentManager.Load<Model>("Model/SpaceObjects/singlestar");
			starTexture = new Texture2D(graphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
			starTexture.SetData<Color>(new Color[] { Color.White });
			spriteBatch = new SpriteBatch(graphicsDevice);
			InitializeStars();
		    Viewport viewport = Game1.Instance.Viewport;
            int width = viewport.Width;
			starfieldRectangle = new Rectangle(0, 0, width, viewport.Height);
		}

		public void Update()
		{
		}

		public struct Star
		{
			public Matrix WorldMatrix;

			public Vector2 Position;

			public float Depth;

			public float scale;
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Starfield3D() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            starTexture?.Dispose(ref starTexture);
            spriteBatch?.Dispose(ref spriteBatch);
        }
	}
}