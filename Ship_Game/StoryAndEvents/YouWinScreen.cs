using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game
{
	public sealed class YouWinScreen : GameScreen
	{
        string txt;
        Effect desaturateEffect;
        Rectangle Portrait;
        Rectangle SourceRect;
        Texture2D LoseTexture;
        Texture2D Reason;
        Rectangle ReasonRect;
        ReplayElement replay;
		AudioHandle Music = new AudioHandle();
        Vector2 Origin = new Vector2(960f, 540f);
        int width = 192;
        int height = 108;
        float scale = 20f;
        float Saturation = 255f;
        bool ShowingReplay;

		public YouWinScreen(GameScreen parent) : base(parent)
		{
			IsPopup = false;
			TransitionOnTime = 30f;
			TransitionOffTime = 0.25f;
		}

		public YouWinScreen(GameScreen parent, string text) : base(parent)
        {
            txt = text;
            txt = Fonts.Arial20Bold.ParseText(txt, 500f);
			IsPopup = false;
			TransitionOnTime = 30f;
			TransitionOffTime = 0.25f;
		}


		public override void Draw(SpriteBatch batch, DrawTimes elapsed)
		{
			ScreenManager.GraphicsDevice.Clear(Color.Black);

            batch.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None);
            if (desaturateEffect != null)
            {
                desaturateEffect.Begin();
                desaturateEffect.CurrentTechnique.Passes[0].Begin();

                batch.Draw(LoseTexture, ScreenCenter, null,
                    new Color(255, 255, 255, (byte)Saturation),
                    0f, Origin, scale, SpriteEffects.None, 1f);

                desaturateEffect.CurrentTechnique.Passes[0].End();
                desaturateEffect.End();
            }
            batch.End();

            batch.Begin();
            {
                if (txt != null)
                {
                    Vector2 pos = ScreenCenter;
                    pos.X -= 250;
                    pos.Y -= 50;
                    batch.DrawDropShadowText(txt, pos, Fonts.Arial20Bold);
                }
                batch.Draw(Reason, ReasonRect, Color.White);
                if (!IsExiting && ShowingReplay)
                {
                    replay.Draw(ScreenManager);
                }
            }
            batch.End();
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
                        replay = new ReplayElement(new Rectangle(ScreenWidth / 2 - 376, ScreenHeight / 2 - 376, 752, 752));
					}
					else
					{
                        replay = new ReplayElement(new Rectangle(ScreenWidth / 2 - 290, ScreenHeight / 2 - 354, 580, 580));
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

        void PlayWinTheme()
        {
            if (!Music.IsPlaying)
                Music = GameAudio.PlayMusic("TitleTheme");
        }

		public override void LoadContent()
		{
		    GameAudio.SwitchToRacialMusic();
            GameAudio.MuteGenericMusic();
            PlayWinTheme();

            LoseTexture = TransientContent.Load<Texture2D>("WinLose/launch");
            Reason = TransientContent.Load<Texture2D>("WinLose/YouWin");
            ReasonRect = new Rectangle(ScreenWidth / 2 - Reason.Width / 2, ScreenHeight / 2 - Reason.Height / 2 - 200, Reason.Width, Reason.Height);
            desaturateEffect = TransientContent.Load<Effect>("Effects/desaturate");
            Portrait = new Rectangle(0, 0, 1920, 1080);
            SourceRect = new Rectangle(864, 486, 192, 108);
			while (Portrait.Width < ScreenWidth && Portrait.Height < ScreenHeight)
			{
                Portrait.Width = Portrait.Width + 19;
                Portrait.X = ScreenWidth / 2 - Portrait.Width / 2;
                Portrait.Height = Portrait.Height + 10;
                Portrait.Y = ScreenHeight / 2 - Portrait.Height / 2;
			}
			base.LoadContent();
		}

		public override void Update(UpdateTimes elapsed, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
            PlayWinTheme();

            scale = 1f + 2f * TransitionPosition;
            Saturation = 100f * (1f - TransitionPosition);
            width = (int)MathHelper.Lerp(width, (int)(960f + 960f * (1f - TransitionPosition)), 0.3f);
            height = (int)MathHelper.Lerp(height, 540f + 540f * (1f - TransitionPosition), 0.3f);
            SourceRect = new Rectangle((int)MathHelper.Lerp(SourceRect.X, 960 - width / 2, 0.3f), (int)MathHelper.Lerp(SourceRect.Y, 540 - height / 2, 0.3f), width, height);
			base.Update(elapsed, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}