using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;

namespace Ship_Game
{
	public class ZeroSplash : GameScreen
	{
		//private Empire them;

		//private Empire playerEmpire;

		//private string whichScreen;

		private Rectangle Portrait;

		//private Vector2 TextCursor;

		private Video video;

		private VideoPlayer player;

		private Texture2D videoTexture;

		public ZeroSplash() : base(null/*no parent*/)
		{
		}

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.SpriteBatch.Begin();
			if (this.player.State != MediaState.Stopped)
			{
				this.videoTexture = this.player.GetTexture();
			}
			if (this.videoTexture != null)
			{
				base.ScreenManager.SpriteBatch.Draw(this.videoTexture, this.Portrait, Color.White);
			}
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			if (this.video != null)
			{
				this.player.Stop();
			}
			if (this.player != null)
			{
				this.video = null;
				while (!this.player.IsDisposed)
				{
					this.player.Dispose();
				}
			}
			this.player = null;
			base.ScreenManager.RemoveScreen(this);
		}


		public override void HandleInput(InputState input)
		{
			if (input.Escaped)
			{
				this.ExitScreen();
			}
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			this.video = TransientContent.Load<Video>("Video/zerosplash");
			this.player = new VideoPlayer()
			{
				IsLooped = false
			};
            this.player.Volume = GlobalStats.MusicVolume;
			
            this.player.Play(this.video);
			base.ScreenManager.musicCategory.SetVolume(0f);
			base.ScreenManager.racialMusic.SetVolume(0.7f);
			this.Portrait = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 640, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 360, 1280, 720);
			while (this.Portrait.Width < base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth && this.Portrait.Height < base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
			{
				this.Portrait.Width = this.Portrait.Width + 12;
				this.Portrait.X = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - this.Portrait.Width / 2;
				this.Portrait.Height = this.Portrait.Height + 7;
				this.Portrait.Y = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - this.Portrait.Height / 2;
			}
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			if (this.player.State == MediaState.Stopped)
			{
				this.ExitScreen();
				base.ScreenManager.AddScreen(new MainMenuScreen());
			}
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}