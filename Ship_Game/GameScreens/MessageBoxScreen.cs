using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
    public  class MessageBoxScreen : GameScreen
    {
        private readonly bool PauseMenu;
        private string Message;

        private UIButton Ok;
        private UIButton Cancel;

        private float Timer;
        private readonly bool Timed;
        private readonly string Original = "";
        private string Toappend;

        public MessageBoxScreen(GameScreen parent, string message) : base(parent)
        {
            Message = message;
            Message = Fonts.Arial12Bold.ParseText(message, 250f);
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);

            Ok     = ButtonSmall(0f, 0f, titleId:15, click: OnOkClicked);
            Cancel = ButtonSmall(0f, 0f, titleId:16, click: OnCancelClicked);
        }

        public MessageBoxScreen(GameScreen parent, int localID, string oktext, string canceltext)
            : this(parent, Localizer.Token(localID), oktext, canceltext)
        {
        }
        
        public MessageBoxScreen(GameScreen parent, string message, string oktext, string canceltext) : base(parent)
        {
            Message = message;
            Message = Fonts.Arial12Bold.ParseText(message, 250f);
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);

            Ok     = ButtonSmall(0f, 0f, oktext,     click: OnOkClicked);
            Cancel = ButtonSmall(0f, 0f, canceltext, click: OnCancelClicked);
        }

        public MessageBoxScreen(GameScreen parent, string message, float Timer) : base(parent)
        {
            Timed = true;
            this.Timer = Timer;
            Original = message;
            Message = message;
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);

            Ok     = ButtonSmall(0f, 0f, titleId:15, click: OnOkClicked);
            Cancel = ButtonSmall(0f, 0f, titleId:16, click: OnCancelClicked);
        }

        public MessageBoxScreen(GameScreen parent, string message, bool pauseMenu) : this(parent, message)
        {
            PauseMenu = pauseMenu;
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            if (!Timed)
            {
                Rectangle r = new Rectangle(ScreenWidth / 2 - 135,
                    ScreenHeight / 2 - (int)(Fonts.Arial12Bold.MeasureString(Message).Y + 40f) / 2, 270, (int)(Fonts.Arial12Bold.MeasureString(Message).Y + 40f) + 15);
                Vector2 textPosition = new Vector2((float)(r.X + r.Width / 2) - Fonts.Arial12Bold.MeasureString(Message).X / 2f, (float)(r.Y + 10));
                batch.Begin();
                batch.FillRectangle(r, Color.Black);
                batch.DrawRectangle(r, Color.Orange);
                batch.DrawString(Fonts.Arial12Bold, string.Concat(Message, Toappend), textPosition, Color.White);

                Ok.SetAbsPos(    r.X + r.Width / 2 + 5,  r.Y + r.Height - 28);
                Cancel.SetAbsPos(r.X + r.Width / 2 - 73, r.Y + r.Height - 28);
                base.Draw(batch);
                batch.End();
                return;
            }
            Message = HelperFunctions.ParseText(Fonts.Arial12Bold, string.Concat(Original, Toappend), 250f);
            //renamed r, textposition
            Rectangle r2 = new Rectangle(ScreenWidth / 2 - 135, ScreenHeight / 2 - (int)(Fonts.Arial12Bold.MeasureString(Message).Y + 40f) / 2, 270, (int)(Fonts.Arial12Bold.MeasureString(Message).Y + 40f) + 15);
            Vector2 textPosition2 = new Vector2((float)(r2.X + r2.Width / 2) - Fonts.Arial12Bold.MeasureString(Message).X / 2f, (float)(r2.Y + 10));
            batch.Begin();
            batch.FillRectangle(r2, Color.Black);
            batch.DrawRectangle(r2, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, Message, textPosition2, Color.White);

            Ok.SetAbsPos(    r2.X + r2.Width / 2 + 5,  r2.Y + r2.Height - 28);
            Cancel.SetAbsPos(r2.X + r2.Width / 2 - 73, r2.Y + r2.Height - 28);
            base.Draw(batch);
            batch.End();
        }

        private void OnOkClicked(UIButton b)
        {
            Accepted?.Invoke(this, EventArgs.Empty);
            GameAudio.PlaySfxAsync("echo_affirm1");
            ExitScreen();
        }

        private void OnCancelClicked(UIButton b)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
            ExitScreen();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.MenuSelect && !PauseMenu)
            {
                Accepted?.Invoke(this, EventArgs.Empty);
                ExitScreen();
                return true;
            }
            if (input.MenuCancel || input.MenuSelect && PauseMenu)
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
            MessageBoxScreen messageBoxScreen = this;
            messageBoxScreen.Timer = messageBoxScreen.Timer - elapsedTime;
            if (Timed)
            {
                Toappend = string.Concat(Timer.String(0), " ", Localizer.Token(17));
                if (Timer <= 0f)
                {
                    Cancelled?.Invoke(this, EventArgs.Empty);
                    ExitScreen();
                }
            }
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public event EventHandler<EventArgs> Accepted;

        public event EventHandler<EventArgs> Cancelled;
    }
}