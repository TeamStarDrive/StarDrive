using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Ship_Game
{
	public class PieCursor : DrawableGameComponent
	{
		private float CursorSpeed = 600f;

		private Texture2D cursorTexture;

		private Vector2 textureCenter;

		private Vector2 position;

		private PieMenu pieMenu;

		private SpriteBatch spriteBatch;

		private SpriteFont spriteFont;

		private ContentManager content;

		private CursorMode cursorMode;

		private Vector2 deltaMovement;

		public CursorMode CurrentCursorMode
		{
			get
			{
				return this.cursorMode;
			}
			set
			{
				this.cursorMode = value;
			}
		}

		public Vector2 Position
		{
			get
			{
				return this.position;
			}
		}

		public PieCursor(Microsoft.Xna.Framework.Game game, ContentManager content) : base(game)
		{
			this.pieMenu = new PieMenu();
			this.content = content;
		}

		public Ray CalculateCursorRay(Matrix projectionMatrix, Matrix viewMatrix)
		{
			Vector3 nearSource = new Vector3(this.Position, 0f);
			Vector3 farSource = new Vector3(this.Position, 1f);
			Viewport viewport = base.GraphicsDevice.Viewport;
			Vector3 nearPoint = viewport.Unproject(nearSource, projectionMatrix, viewMatrix, Matrix.Identity);
			Viewport viewport1 = base.GraphicsDevice.Viewport;
			Vector3 farPoint = viewport1.Unproject(farSource, projectionMatrix, viewMatrix, Matrix.Identity);
			Vector3 direction = farPoint - nearPoint;
			direction.Normalize();
			return new Ray(nearPoint, direction);
		}

		public override void Draw(GameTime gameTime)
		{
			this.pieMenu.Draw(this.spriteBatch, this.spriteFont);
			this.spriteBatch.Begin();
			Rectangle? nullable = null;
			this.spriteBatch.Draw(this.cursorTexture, this.Position, nullable, Color.White, 0f, this.textureCenter, 1f, SpriteEffects.None, 0f);
			this.spriteBatch.End();
		}

		public bool HandleInput(InputState input)
		{
			input.CursorPosition = this.Position;
			float x = this.Position.X / (float)base.GraphicsDevice.Viewport.Width;
			float y = this.Position.Y;
			Viewport viewport = base.GraphicsDevice.Viewport;
			input.NormalizedCursorPosition = new Vector2(x, y / (float)viewport.Height);
			bool retVal = this.pieMenu.Visible;
			if (this.cursorMode != CursorMode.GamePad)
			{
				Vector2 selDir = this.position - this.pieMenu.Position;
				selDir.Y = selDir.Y * -1f;
				selDir = selDir / this.pieMenu.Radius;
				this.pieMenu.HandleInput(input, selDir);
			}
			else
			{
				this.pieMenu.HandleInput(input);
			}
			this.deltaMovement = input.CurrentGamePadState.ThumbSticks.Left;
			this.deltaMovement.Y = this.deltaMovement.Y * -1f;
			return retVal;
		}

		protected override void LoadContent()
		{
			this.cursorTexture = this.content.Load<Texture2D>("cursor");
			this.textureCenter = new Vector2((float)(this.cursorTexture.Width / 2), (float)(this.cursorTexture.Height / 2));
			this.spriteBatch = new SpriteBatch(base.GraphicsDevice);
			this.spriteFont = this.content.Load<SpriteFont>("menufont");
			base.LoadContent();
		}

		public void ShowPieMenu(PieMenuNode node)
		{
			this.pieMenu.RootNode = node;
			this.ShowPieMenu();
		}

		public void ShowPieMenu()
		{
			this.pieMenu.Show(this.position);
		}

		public override void Update(GameTime gameTime)
		{
			this.pieMenu.Update(gameTime);
			if (this.pieMenu.Visible && this.cursorMode == CursorMode.GamePad)
			{
				return;
			}
			MouseState mouseState = Mouse.GetState();
			this.position.X = (float)mouseState.X;
			this.position.Y = (float)mouseState.Y;
			PieCursor totalSeconds = this;
			Vector2 vector2 = totalSeconds.position;
			Vector2 cursorSpeed = this.deltaMovement * this.CursorSpeed;
			TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
			totalSeconds.position = vector2 + (cursorSpeed * (float)elapsedGameTime.TotalSeconds);
			Mouse.SetPosition((int)this.position.X, (int)this.position.Y);
		}
	}
}