using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Ship_Game
{
	public sealed class PieCursor : DrawableGameComponent
	{
		private float CursorSpeed = 600f;

		private Texture2D cursorTexture;

		private Vector2 textureCenter;

		private Vector2 position;

		private PieMenu pieMenu;

		private SpriteBatch spriteBatch;

		private SpriteFont spriteFont;

		private GameContentManager content;

	    private Vector2 deltaMovement;

		public CursorMode CurrentCursorMode { get; set; }

	    public Vector2 Position => position;

	    public PieCursor(Game1 game, GameContentManager content) : base(game)
		{
			pieMenu = new PieMenu();
			this.content = content;
		}

		public Ray CalculateCursorRay(Matrix projectionMatrix, Matrix viewMatrix)
		{
			Vector3 nearSource = new Vector3(Position, 0f);
			Vector3 farSource = new Vector3(Position, 1f);
			Vector3 nearPoint = Game1.Instance.Viewport.Unproject(nearSource, projectionMatrix, viewMatrix, Matrix.Identity);
			Vector3 farPoint = Game1.Instance.Viewport.Unproject(farSource, projectionMatrix, viewMatrix, Matrix.Identity);
			Vector3 direction = farPoint - nearPoint;
			direction.Normalize();
			return new Ray(nearPoint, direction);
		}

		public override void Draw(GameTime gameTime)
		{
			pieMenu.Draw(spriteBatch, spriteFont);
			spriteBatch.Begin();
			Rectangle? nullable = null;
			spriteBatch.Draw(cursorTexture, Position, nullable, Color.White, 0f, textureCenter, 1f, SpriteEffects.None, 0f);
			spriteBatch.End();
		}

		public bool HandleInput(InputState input)
		{
			bool retVal = pieMenu.Visible;
			if (CurrentCursorMode != CursorMode.GamePad)
			{
				Vector2 selDir = position - pieMenu.Position;
				selDir.Y = selDir.Y * -1f;
				selDir = selDir / pieMenu.Radius;
				pieMenu.HandleInput(input, selDir);
			}
			else
			{
				pieMenu.HandleInput(input);
			}
			deltaMovement = input.GamepadCurr.ThumbSticks.Left;
			deltaMovement.Y = deltaMovement.Y * -1f;
			return retVal;
		}

		protected override void LoadContent()
		{
			cursorTexture = content.Load<Texture2D>("cursor");
			textureCenter = new Vector2((float)(cursorTexture.Width / 2), (float)(cursorTexture.Height / 2));
			spriteBatch = new SpriteBatch(GraphicsDevice);
			spriteFont = content.Load<SpriteFont>("menufont");
			base.LoadContent();
		}

		public void ShowPieMenu(PieMenuNode node)
		{
			pieMenu.RootNode = node;
			ShowPieMenu();
		}

		public void ShowPieMenu()
		{
			pieMenu.Show(position);
		}

		public override void Update(GameTime gameTime)
		{
			pieMenu.Update(gameTime);
			if (pieMenu.Visible && CurrentCursorMode == CursorMode.GamePad)
			{
				return;
			}
			MouseState mouseState = Mouse.GetState();
			position.X = (float)mouseState.X;
			position.Y = (float)mouseState.Y;
			PieCursor totalSeconds = this;
			Vector2 vector2 = totalSeconds.position;
			Vector2 cursorSpeed = deltaMovement * CursorSpeed;
			TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
			totalSeconds.position = vector2 + (cursorSpeed * (float)elapsedGameTime.TotalSeconds);
			Mouse.SetPosition((int)position.X, (int)position.Y);
		}
	}
}