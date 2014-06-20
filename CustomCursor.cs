using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game;
using System;
using System.Collections.Generic;

public class CustomCursor : DrawableGameComponent
{
	private ContentManager content;

	private SpriteBatch spriteBatch;

	private int frameRate;

	private int frameCounter;

	private TimeSpan elapsedTime = TimeSpan.Zero;

	public CustomCursor(Microsoft.Xna.Framework.Game game) : base(game)
	{
		this.content = new ContentManager(game.Services);
	}

	public override void Draw(GameTime gameTime)
	{
		CustomCursor customCursor = this;
		customCursor.frameCounter = customCursor.frameCounter + 1;
		string.Format("fps: {0}", this.frameRate);
		float x = (float)Mouse.GetState().X;
		MouseState state = Mouse.GetState();
		Vector2 MousePos = new Vector2(x, (float)state.Y);
		this.spriteBatch.Begin();
		this.spriteBatch.Draw(ResourceManager.TextureDict["Cursors/Cursor"], MousePos, Color.White);
		this.spriteBatch.End();
	}

	protected override void LoadContent()
	{
		this.spriteBatch = new SpriteBatch(base.GraphicsDevice);
	}

	protected override void UnloadContent()
	{
		this.content.Unload();
	}

	public override void Update(GameTime gameTime)
	{
		CustomCursor elapsedGameTime = this;
		elapsedGameTime.elapsedTime = elapsedGameTime.elapsedTime + gameTime.ElapsedGameTime;
		if (this.elapsedTime > TimeSpan.FromSeconds(1))
		{
			CustomCursor customCursor = this;
			customCursor.elapsedTime = customCursor.elapsedTime - TimeSpan.FromSeconds(1);
			this.frameRate = this.frameCounter;
			this.frameCounter = 0;
		}
	}
}