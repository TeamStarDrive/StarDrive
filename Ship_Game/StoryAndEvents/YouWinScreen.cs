using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class YouWinScreen : GameScreen
    {
        UniverseScreen Universe;
        string txt;
        Rectangle Portrait;
        Rectangle SourceRect;

        #pragma warning disable CA2213 // managed by content manager
        Effect desaturateEffect;
        Texture2D LoseTexture;
        Texture2D Reason;
        #pragma warning restore CA2213

        Rectangle ReasonRect;
        ReplayElement replay;
        float MusicCheckTimer;

        Vector2 Origin = new Vector2(960f, 540f);
        int width = 192;
        int height = 108;
        float scale = 20f;
        float Saturation = 255f;
        bool ShowingReplay;

        public YouWinScreen(UniverseScreen parent, LocalizedText text) : base(parent, toPause: parent)
        {
            Universe = parent;
            IsPopup = false;
            TransitionOnTime = 30f;
            TransitionOffTime = 0; // exit immediately

            if (text.IsValid)
                txt = Fonts.Arial20Bold.ParseText(text, 500f);

            Log.LogEventStats(Log.GameEvent.YouWin, parent.UState.P);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.GraphicsDevice.Clear(Color.Black);

            batch.SafeBegin(SpriteBlendMode.None, sortImmediate:true);
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
            batch.SafeEnd();

            batch.SafeBegin();
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
                    replay.Draw(batch, elapsed);
                }
            }
            batch.SafeEnd();
        }

        public override void ExitScreen()
        {
            if (IsExiting)
                return;

            base.ExitScreen(); // set IsExiting=true to avoid calling this again
            ScreenManager.GoToScreen(new MainMenuScreen(MainMenuType.Victory), clear3DObjects:true);
        }

        public override bool HandleInput(InputState input)
        {
            if (input.InGameSelect && !ShowingReplay)
            {
                if (replay == null)
                {
                    RectF rect = LowRes ? new(CenterX - 290, CenterY - 354, 580, 580)
                                        : new(CenterX - 376, CenterY - 376, 752, 752);
                    replay = new(Universe, rect);
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
            if (ScreenManager.Music.IsStopped && !GameAudio.IsMusicDisabled)
            {
                Log.Write("Play Win Theme");
                ScreenManager.Music = GameAudio.PlayMusic("TitleTheme");
            }
            MusicCheckTimer = 5f;
        }

        public override void LoadContent()
        {
            ScreenManager.Music.Stop();
            GameAudio.SwitchToRacialMusic();
            GameAudio.MuteGenericMusic();
            PlayWinTheme();

            LoseTexture = TransientContent.Load<Texture2D>("Textures/WinLose/launch.dds");
            Reason = TransientContent.Load<Texture2D>("Textures/WinLose/YouWin.png");
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

        public override void Update(float fixedDeltaTime)
        {
            // there's a nasty issue with the music, so using a timer check here
            MusicCheckTimer -= fixedDeltaTime;
            if (MusicCheckTimer <= 0f)
            {
                PlayWinTheme();
            }

            scale = 1f + 2f * TransitionPosition;
            Saturation = 100f * (1f - TransitionPosition);
            width = width.LerpTo((int)(960f + 960f * (1f - TransitionPosition)), 0.3f);
            height = height.LerpTo((int)(540f + 540f * (1f - TransitionPosition)), 0.3f);
            SourceRect = new Rectangle(SourceRect.X.LerpTo(960 - width / 2, 0.3f), SourceRect.Y.LerpTo(540 - height / 2, 0.3f), width, height);

            base.Update(fixedDeltaTime);
        }
    }
}