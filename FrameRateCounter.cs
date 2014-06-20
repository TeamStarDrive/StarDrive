using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using System;

public class FrameRateCounter : DrawableGameComponent
{
	private ContentManager content;

	private SpriteBatch spriteBatch;

	private SpriteFont spriteFont;

	private int frameRate;

	private int frameCounter;

	private TimeSpan elapsedTime = TimeSpan.Zero;

	public FrameRateCounter(Microsoft.Xna.Framework.Game game) : base(game)
	{
		this.content = new ContentManager(game.Services);
	}

	public override void Draw(GameTime gameTime)
	{
		FrameRateCounter frameRateCounter = this;
		frameRateCounter.frameCounter = frameRateCounter.frameCounter + 1;
		string fps = string.Format("fps: {0}", this.frameRate);
		this.spriteBatch.Begin();
		this.spriteBatch.DrawString(Fonts.Arial20Bold, fps, new Vector2((float)(base.GraphicsDevice.PresentationParameters.BackBufferWidth - 64), 64f), Color.White, 0f, new Vector2(0f, 0f), 1f, SpriteEffects.None, 1f);
		this.spriteBatch.DrawString(Fonts.Arial20Bold, fps, new Vector2((float)(base.GraphicsDevice.PresentationParameters.BackBufferWidth - 64), 64f), Color.Black, 0f, new Vector2(0f, 0f), 1f, SpriteEffects.None, 1f);
		this.spriteBatch.DrawString(Fonts.Arial20Bold, fps, new Vector2((float)(base.GraphicsDevice.PresentationParameters.BackBufferWidth - 64), 64f), Color.Black);
		this.spriteBatch.DrawString(Fonts.Arial20Bold, fps, new Vector2((float)(base.GraphicsDevice.PresentationParameters.BackBufferWidth - 64), 64f), Color.White);
		this.spriteBatch.End();
	}

	protected override void LoadContent()
	{
		this.spriteBatch = new SpriteBatch(base.GraphicsDevice);
		this.spriteFont = this.content.Load<SpriteFont>("Content/Fonts/Arial20bold");
	}

	protected override void UnloadContent()
	{
		this.content.Unload();
	}

	public override void Update(GameTime gameTime)
	{
		FrameRateCounter elapsedGameTime = this;
		elapsedGameTime.elapsedTime = elapsedGameTime.elapsedTime + gameTime.ElapsedGameTime;
		if (this.elapsedTime > TimeSpan.FromSeconds(1))
		{
			FrameRateCounter frameRateCounter = this;
			frameRateCounter.elapsedTime = frameRateCounter.elapsedTime - TimeSpan.FromSeconds(1);
			this.frameRate = this.frameCounter;
			this.frameCounter = 0;
		}
	}
}