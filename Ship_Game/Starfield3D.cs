using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public class Starfield3D : IDisposable
	{
		private const int numberOfStars = 1000;

		private const int numberOfLayers = 8;

		private const float maximumMovementPerUpdate = 128f;

		public Model star;

		private readonly static Color[] layerColors;

		private readonly static float[] movementFactors;

		//private Vector2 lastPosition;

		//private Vector2 position;

		public Starfield3D.Star[] stars;

		private GraphicsDevice graphicsDevice;

		private ContentManager contentManager;

		private SpriteBatch spriteBatch;

		private Texture2D starTexture;

		public Model starModel;

		private Rectangle starfieldRectangle;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		static Starfield3D()
		{
			Color[] color = new Color[] { new Color(255, 255, 255, 192), new Color(255, 255, 255, 192), new Color(255, 255, 255, 192), new Color(255, 255, 255, 160), new Color(255, 255, 255, 128), new Color(255, 255, 255, 96), new Color(255, 255, 255, 64), new Color(255, 255, 255, 32) };
			Starfield3D.layerColors = color;
			Starfield3D.movementFactors = new float[] { 0.1f, 0.09f, 0.08f, 0.06f, 0.04f, 0.02f, 0.01f, 0.005f };
		}

		public Starfield3D(Vector2 position, GraphicsDevice graphicsDevice, ContentManager contentManager)
		{
			if (graphicsDevice == null)
			{
				throw new ArgumentNullException("graphicsDevice");
			}
			if (contentManager == null)
			{
				throw new ArgumentNullException("contentManager");
			}
			this.graphicsDevice = graphicsDevice;
			this.contentManager = contentManager;
			this.stars = new Starfield3D.Star[1000];
		}

		public void InitializeStars()
		{
			for (int i = 0; i < (int)this.stars.Length; i++)
			{
				this.stars[i].Position.X = RandomMath.RandomBetween(100000f, 120000f);
				this.stars[i].Position.Y = RandomMath.RandomBetween(100000f, 120000f);
				this.stars[i].Depth = RandomMath.RandomBetween(2500f, 10000f);
				this.stars[i].scale = RandomMath.RandomBetween(1f, 3f);
				this.stars[i].WorldMatrix = Matrix.Identity * Matrix.CreateTranslation(this.stars[i].Position.X, this.stars[i].Position.Y, this.stars[i].Depth);
			}
		}

		public void LoadContent()
		{
			this.starModel = this.contentManager.Load<Model>("Model/SpaceObjects/singlestar");
			this.starTexture = new Texture2D(this.graphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
			this.starTexture.SetData<Color>(new Color[] { Color.White });
			this.spriteBatch = new SpriteBatch(this.graphicsDevice);
			this.InitializeStars();
			int width = this.graphicsDevice.Viewport.Width;
			Viewport viewport = this.graphicsDevice.Viewport;
			this.starfieldRectangle = new Rectangle(0, 0, width, viewport.Height);
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.starTexture != null)
                        this.starTexture.Dispose();
                    if (this.spriteBatch != null)
                        this.spriteBatch.Dispose();

                }
                this.starTexture = null;
                this.spriteBatch = null;
                this.disposed = true;
            }
        }
	}
}