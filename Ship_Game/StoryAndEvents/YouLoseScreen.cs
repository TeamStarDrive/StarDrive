using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;
using SDGraphics;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class YouLoseScreen : GameScreen
    {
        UniverseScreen Universe;
        Effect desaturateEffect;
        Rectangle Portrait;
        Rectangle SourceRect;
        Texture2D LoseTexture;
        Texture2D Reason;
        Rectangle ReasonRect;
        ReplayElement replay;
        AudioHandle Ambient = new();
        Vector2 Origin = new Vector2(960f, 540f);
        int width = 192;
        int height = 108;
        float scale = 20f;
        float Saturation = 255f;
        bool ShowingReplay;
        //string RememberedAs = "A footnote in a treatise on failed governance.";

        public YouLoseScreen(UniverseScreen parent) : base(parent, toPause: parent)
        {
            Universe = parent;
            IsPopup = false;
            TransitionOnTime = 30f;
            TransitionOffTime = 0; // exit immediately

            Log.LogEventStats(Log.GameEvent.YouLose, parent.UState.P);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.GraphicsDevice.Clear(Color.Black);
            batch.SafeBegin(SpriteBlendMode.None, sortImmediate:true);
            desaturateEffect.Begin();
            desaturateEffect.CurrentTechnique.Passes[0].Begin();
            batch.Draw(LoseTexture, ScreenCenter, null, new Color(255, 255, 255, (byte)Saturation), 0f, Origin, scale, SpriteEffects.None, 1f);
            batch.SafeEnd();
            desaturateEffect.CurrentTechnique.Passes[0].End();
            desaturateEffect.End();
            batch.SafeBegin();
            batch.Draw(Reason, ReasonRect, Color.White);
            if (!IsExiting && ShowingReplay)
            {
                replay.Draw(batch, elapsed);
            }
            batch.SafeEnd();
        }

        public override void ExitScreen()
        {
            if (IsExiting)
                return;

            base.ExitScreen(); // set IsExiting=true to avoid calling this again
            Ambient.Stop();
            ScreenManager.GoToScreen(new MainMenuScreen(MainMenuType.Defeat), clear3DObjects:true);
        }

        public override bool HandleInput(InputState input)
        {
            if (input.InGameSelect && !ShowingReplay)
            {
                if (replay == null)
                {
                    if (!LowRes)
                    {
                        replay = new ReplayElement(Universe, new Rectangle(ScreenWidth / 2 - 376, ScreenHeight / 2 - 376, 752, 752));
                    }
                    else
                    {
                        replay = new ReplayElement(Universe, new Rectangle(ScreenWidth / 2 - 290, ScreenHeight / 2 - 354, 580, 580));
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
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            LoseTexture = TransientContent.Load<Texture2D>("Textures/WinLose/groundbattle_final.dds");
            Reason = TransientContent.Load<Texture2D>("Textures/WinLose/YouLose.png");
            ReasonRect = new Rectangle(ScreenWidth / 2 - Reason.Width / 2, ScreenHeight / 2 - Reason.Height / 2 - 200, Reason.Width, Reason.Height);
            desaturateEffect = TransientContent.Load<Effect>("Effects/desaturate");
            Portrait = new Rectangle(0, 0, 1920, 1080);
            SourceRect = new Rectangle(864, 486, 192, 108);
            while (Portrait.Width < ScreenWidth && Portrait.Height < ScreenHeight)
            {
                Portrait.Width += 19;
                Portrait.X = ScreenWidth / 2 - Portrait.Width / 2;
                Portrait.Height += 10;
                Portrait.Y = ScreenHeight / 2 - Portrait.Height / 2;
            }
            GameAudio.SwitchToRacialMusic();

            Log.Write("Play Lose Theme");
            ScreenManager.Music = GameAudio.PlayMusic("Female_02_loop");
            Ambient = GameAudio.PlayMusic("sd_battle_ambient");
            base.LoadContent();
        }

        public override void Update(float fixedDeltaTime)
        {
            scale = 1f + 2f * TransitionPosition;
            Saturation = 100f * (1f - TransitionPosition);
            width = width.LerpTo((int)(960f + 960f * (1f - TransitionPosition)), 0.3f);
            height = height.LerpTo((int)(540f + 540f * (1f - TransitionPosition)), 0.3f);
            GameAudio.MuteGenericMusic();
            SourceRect = new Rectangle(SourceRect.X.LerpTo(960 - width / 2, 0.3f), SourceRect.Y.LerpTo(540 - height / 2, 0.3f), width, height);

            base.Update(fixedDeltaTime);
        }
    }
}