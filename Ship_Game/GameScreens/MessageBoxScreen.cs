using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public  class MessageBoxScreen : GameScreen
    {
        string Message;

        readonly UIButton Ok;
        readonly UIButton Cancel;

        float Timer;
        readonly bool Timed;
        readonly string Original = "";
        string Toappend;

        public event EventHandler<EventArgs> Accepted;
        public event EventHandler<EventArgs> Cancelled;

        public MessageBoxScreen(GameScreen parent, string message) : base(parent)
        {
            Original = message;
            Message = Fonts.Arial12Bold.ParseText(message, 250f);
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;

            Ok     = ButtonSmall(0f, 0f, text:15, click: OnOkClicked);
            Cancel = ButtonSmall(0f, 0f, text:16, click: OnCancelClicked);
        }

        public MessageBoxScreen(GameScreen parent, int localID, string oktext, string canceltext)
            : this(parent, Localizer.Token(localID), oktext, canceltext)
        {
        }
        
        public MessageBoxScreen(GameScreen parent, string message, string oktext, string canceltext) : base(parent)
        {
            Original = message;
            Message = message;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;

            Ok     = ButtonSmall(0f, 0f, oktext,     click: OnOkClicked);
            Cancel = ButtonSmall(0f, 0f, canceltext, click: OnCancelClicked);
        }

        public MessageBoxScreen(GameScreen parent, string message, float timer) : base(parent)
        {
            Timed = true;
            Timer = timer;
            Original = message;
            Message = message;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.0f;

            Ok     = ButtonSmall(0f, 0f, text:15, click: OnOkClicked);
            Cancel = ButtonSmall(0f, 0f, text:16, click: OnCancelClicked);
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

            Message = Fonts.Arial12Bold.ParseText(Original + Toappend, 250f);
            Vector2 msgSize = Fonts.Arial12Bold.MeasureString(Message);
            var r = new Rectangle(ScreenWidth / 2 - 135, ScreenHeight / 2 - (int)(msgSize.Y + 40f) / 2,
                                  270, (int)(msgSize.Y + 40f) + 15);

            var textPosition = new Vector2(r.X + r.Width / 2 - Fonts.Arial12Bold.MeasureString(Message).X / 2f, r.Y + 10);
            
            Ok.SetAbsPos(    r.X + r.Width / 2 + 5,  r.Y + r.Height - 28);
            Cancel.SetAbsPos(r.X + r.Width / 2 - 73, r.Y + r.Height - 28);

            batch.Begin();
            batch.FillRectangle(r, Color.Black);
            batch.DrawRectangle(r, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, Message, textPosition, Color.White);
            base.Draw(batch);
            batch.End();
        }

        void OnOkClicked(UIButton b)
        {
            Accepted?.Invoke(this, EventArgs.Empty);
            GameAudio.AffirmativeClick();
            ExitScreen();
        }

        void OnCancelClicked(UIButton b)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
            ExitScreen();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.MenuSelect)
            {
                Accepted?.Invoke(this, EventArgs.Empty);
                ExitScreen();
                return true;
            }
            if (input.MenuCancel)
            {
                Cancelled?.Invoke(this, EventArgs.Empty);
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Timer -= elapsedTime;
            if (Timed && !IsExiting)
            {
                Toappend = string.Concat(" ", Timer.String(0), " ", Localizer.Token(17));
                if (Timer <= 0f)
                {
                    Cancelled?.Invoke(this, EventArgs.Empty);
                    ExitScreen();
                }
            }
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }
    }
}