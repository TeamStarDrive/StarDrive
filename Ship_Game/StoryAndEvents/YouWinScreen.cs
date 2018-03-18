using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class YouWinScreen : GameScreen
	{
		private Vector2 Cursor = Vector2.Zero;

		private string txt;

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

		private AudioHandle Music;

		//private Cue Ambience;

		private Vector2 Origin = new Vector2(960f, 540f);

		private int width = 192;

		private int height = 108;

		private float scale = 20f;

		private float Saturation = 255f;

		private bool ShowingReplay;

		public YouWinScreen(GameScreen parent) : base(parent)
		{
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(30);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public YouWinScreen(GameScreen parent, string text) : base(parent)
        {
            txt = text;
            txt = HelperFunctions.ParseText(Fonts.Arial20Bold, txt, 500f);
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(30);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}


		public override void Draw(SpriteBatch spriteBatch)
		{
			base.ScreenManager.GraphicsDevice.Clear(Color.Black);
			base.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None);
            desaturateEffect.Begin();
            desaturateEffect.CurrentTechnique.Passes[0].Begin();
			Rectangle? nullable = null;
			base.ScreenManager.SpriteBatch.Draw(LoseTexture, new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2)), nullable, new Color(255, 255, 255, (byte)Saturation), 0f, Origin, scale, SpriteEffects.None, 1f);
			Vector2 pos = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 250), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 50));
			base.ScreenManager.SpriteBatch.End();
            desaturateEffect.CurrentTechnique.Passes[0].End();
            desaturateEffect.End();
			base.ScreenManager.SpriteBatch.Begin();
			if (txt != null)
			{
				HelperFunctions.DrawDropShadowText(base.ScreenManager, txt, pos, Fonts.Arial20Bold);
			}
			base.ScreenManager.SpriteBatch.Draw(Reason, ReasonRect, Color.White);
			if (!base.IsExiting && ShowingReplay)
			{
                replay.Draw(base.ScreenManager);
			}
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
            ScreenManager.ExitAllExcept(this);
            Music.Stop();
			ScreenManager.AddScreen(new MainMenuScreen());
			base.ExitScreen();
		}



		public override bool HandleInput(InputState input)
		{
			if (input.InGameSelect && !ShowingReplay)
			{
				if (replay == null)
				{
					if (!LowRes)
					{
                        replay = new ReplayElement(new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 376, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 376, 752, 752));
					}
					else
					{
                        replay = new ReplayElement(new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 290, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 354, 580, 580));
					}
				}
                ShowingReplay = true;
			}
			else if (ShowingReplay)
			{
                replay.HandleInput(input);
			}
			if (input.RightMouseClick && ShowingReplay)
			{
                ShowingReplay = false;
			}
			if (input.Escaped)
			{
                ExitScreen();
			}
			return base.HandleInput(input);
		}

		public override void LoadContent()
		{
		    GameAudio.SwitchToRacialMusic();
		    Music = GameAudio.PlayMusic("TitleTheme");

            if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight < 880)
			{
                LowRes = true;
			}
            LoseTexture = TransientContent.Load<Texture2D>("WinLose/launch");
            Reason = TransientContent.Load<Texture2D>("WinLose/YouWin");
            ReasonRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - Reason.Width / 2, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - Reason.Height / 2 - 200, Reason.Width, Reason.Height);
            desaturateEffect = TransientContent.Load<Effect>("Effects/desaturate");
            Portrait = new Rectangle(0, 0, 1920, 1080);
            SourceRect = new Rectangle(864, 486, 192, 108);
			while (Portrait.Width < base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth && Portrait.Height < base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
			{
                Portrait.Width = Portrait.Width + 19;
                Portrait.X = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - Portrait.Width / 2;
                Portrait.Height = Portrait.Height + 10;
                Portrait.Y = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - Portrait.Height / 2;
			}
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
		    GameAudio.MuteGenericMusic();

            scale = 1f + 2f * base.TransitionPosition;
            Saturation = 100f * (1f - base.TransitionPosition);
            width = (int)MathHelper.Lerp((float)width, (float)((int)(960f + 960f * (1f - base.TransitionPosition))), 0.3f);
            height = (int)MathHelper.Lerp((float)height, 540f + 540f * (1f - base.TransitionPosition), 0.3f);
            SourceRect = new Rectangle((int)MathHelper.Lerp((float)SourceRect.X, (float)(960 - width / 2), 0.3f), (int)MathHelper.Lerp((float)SourceRect.Y, (float)(540 - height / 2), 0.3f), width, height);
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}