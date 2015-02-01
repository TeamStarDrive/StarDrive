using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class YouLoseScreen : GameScreen, IDisposable
	{
		private Vector2 Cursor = Vector2.Zero;

		//private GameScreen caller;

		private Effect desaturateEffect;

		//private Menu2 window;

		private Rectangle Portrait;

		private Rectangle SourceRect;

		private Texture2D LoseTexture;

		private Texture2D Reason;

		private Rectangle ReasonRect;

		//private Rectangle RememberRect;

		private ReplayElement replay;

		private bool LowRes;

		private Cue Music;

		private Cue Ambience;

		private Vector2 Origin = new Vector2(960f, 540f);

		private int width = 192;

		private int height = 108;

		private float scale = 20f;

		private float Saturation = 255f;

		private bool ShowingReplay;

		private string RememberedAs = "A footnote in a treatise on failed governance.";

		//private float transitionElapsedTime;

		public YouLoseScreen()
		{
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(30);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
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
			base.ScreenManager.GraphicsDevice.Clear(Color.Black);
			base.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None);
			this.desaturateEffect.Begin();
			this.desaturateEffect.CurrentTechnique.Passes[0].Begin();
			Rectangle? nullable = null;
			base.ScreenManager.SpriteBatch.Draw(this.LoseTexture, new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2)), nullable, new Color(255, 255, 255, (byte)this.Saturation), 0f, this.Origin, this.scale, SpriteEffects.None, 1f);
			Vector2 vector2 = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Arial20Bold.MeasureString(this.RememberedAs).X / 2f, (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 + 50));
			base.ScreenManager.SpriteBatch.End();
			this.desaturateEffect.CurrentTechnique.Passes[0].End();
			this.desaturateEffect.End();
			base.ScreenManager.SpriteBatch.Begin();
			base.ScreenManager.SpriteBatch.Draw(this.Reason, this.ReasonRect, Color.White);
			if (!base.IsExiting && this.ShowingReplay)
			{
				this.replay.Draw(base.ScreenManager);
			}
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			for (int i = 0; i < base.ScreenManager.screens.Count; i++)
			{
				if (base.ScreenManager.screens[i] != this)
				{
					base.ScreenManager.screens[i].ExitScreen();
				}
			}
			this.Music.Stop(AudioStopOptions.Immediate);
			this.Ambience.Stop(AudioStopOptions.Immediate);
			base.ScreenManager.AddScreen(new MainMenuScreen());
			base.ExitScreen();
		}


		public override void HandleInput(InputState input)
		{
			if (input.InGameSelect && !this.ShowingReplay)
			{
				if (this.replay == null)
				{
					if (!this.LowRes)
					{
						this.replay = new ReplayElement(new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 376, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 376, 752, 752));
					}
					else
					{
						this.replay = new ReplayElement(new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 290, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 354, 580, 580));
					}
				}
				this.ShowingReplay = true;
			}
			else if (this.ShowingReplay)
			{
				this.replay.HandleInput(input);
			}
			if (input.RightMouseClick && this.ShowingReplay)
			{
				this.ShowingReplay = false;
			}
			if (input.Escaped)
			{
				this.ExitScreen();
			}
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight < 880)
			{
				this.LowRes = true;
			}
			Vector2 vector2 = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
			this.LoseTexture = base.ScreenManager.Content.Load<Texture2D>("WinLose/groundbattle_final");
			this.Reason = base.ScreenManager.Content.Load<Texture2D>("WinLose/YouLose");
			this.ReasonRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - this.Reason.Width / 2, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - this.Reason.Height / 2 - 200, this.Reason.Width, this.Reason.Height);
			this.desaturateEffect = base.ScreenManager.Content.Load<Effect>("Effects/desaturate");
			this.Portrait = new Rectangle(0, 0, 1920, 1080);
			this.SourceRect = new Rectangle(864, 486, 192, 108);
			while (this.Portrait.Width < base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth && this.Portrait.Height < base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
			{
				this.Portrait.Width = this.Portrait.Width + 19;
				this.Portrait.X = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - this.Portrait.Width / 2;
				this.Portrait.Height = this.Portrait.Height + 10;
				this.Portrait.Y = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - this.Portrait.Height / 2;
			}
			base.ScreenManager.musicCategory.Stop(AudioStopOptions.Immediate);
			base.ScreenManager.musicCategory.SetVolume(0f);
			base.ScreenManager.racialMusic.SetVolume(1f);
			this.Music = AudioManager.GetCue("Female_02_loop");
			this.Music.Play();
			this.Ambience = AudioManager.GetCue("sd_battle_ambient");
			this.Ambience.Play();
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			this.scale = 1f + 2f * base.TransitionPosition;
			this.Saturation = 100f * (1f - base.TransitionPosition);
			this.width = (int)MathHelper.Lerp((float)this.width, (float)((int)(960f + 960f * (1f - base.TransitionPosition))), 0.3f);
			this.height = (int)MathHelper.Lerp((float)this.height, 540f + 540f * (1f - base.TransitionPosition), 0.3f);
			base.ScreenManager.musicCategory.SetVolume(0f);
			this.SourceRect = new Rectangle((int)MathHelper.Lerp((float)this.SourceRect.X, (float)(960 - this.width / 2), 0.3f), (int)MathHelper.Lerp((float)this.SourceRect.Y, (float)(540 - this.height / 2), 0.3f), this.width, this.height);
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}