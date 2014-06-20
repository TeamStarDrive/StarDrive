using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;

namespace Ship_Game
{
	public class ZeroSplash : GameScreen, IDisposable
	{
		//private Empire them;

		//private Empire playerEmpire;

		//private string whichScreen;

		private Rectangle Portrait;

		//private Vector2 TextCursor;

		private Video video;

		private VideoPlayer player;

		private Texture2D videoTexture;

		public ZeroSplash()
		{
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
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

		/*protected override void Finalize()
		{
			try
			{
				this.Dispose(false);
			}
			finally
			{
				base.Finalize();
			}
		}*/
        ~ZeroSplash() {
            //should implicitly do the same thing as the original bad finalize
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
			this.video = base.ScreenManager.Content.Load<Video>("Video/zerosplash");
			this.player = new VideoPlayer()
			{
				IsLooped = false
			};
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