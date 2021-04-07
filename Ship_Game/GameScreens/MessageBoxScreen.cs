using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public enum MessageBoxButtons
    {
        Default, // Ok / Cancel
        Ok, // only OK
    }

    public class MessageBoxScreen : GameScreen
    {
        readonly UIButton Ok;
        readonly UIButton Cancel;

        float Timer;
        readonly bool Timed;
        
        string Message;
        readonly string Original;
        string ToAppend;
        readonly int BoxWidth;

        public Action Accepted;
        public Action Cancelled;

        public MessageBoxScreen(GameScreen parent, string message,
                                MessageBoxButtons buttons = MessageBoxButtons.Default, int width = 270)
            : this(parent, message, Localizer.Token(GameText.Ok), Localizer.Token(GameText.Cancel), buttons, width)
        {
        }

        public MessageBoxScreen(GameScreen parent, int localID, string okText, string cancelText)
            : this(parent, Localizer.Token(localID), okText, cancelText)
        {
        }

        public MessageBoxScreen(GameScreen parent, string message, float timer,
                                MessageBoxButtons buttons = MessageBoxButtons.Default)
            : this(parent, message, Localizer.Token(GameText.Ok), Localizer.Token(GameText.Cancel), buttons)
        {
            Timed = true;
            Timer = timer;
        }

        public MessageBoxScreen(GameScreen parent, string message, string okText, string cancelText,
                                MessageBoxButtons buttons = MessageBoxButtons.Default, int width = 270) : base(parent)
        {
            Original = message;
            Message = message;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
            BoxWidth = width;

            Ok = ButtonSmall(0f, 0f, okText, click: OnOkClicked);
            if (buttons == MessageBoxButtons.Default)
                Cancel = ButtonSmall(0f, 0f, cancelText, click: OnCancelClicked);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

            Message = Fonts.Arial12Bold.ParseText(Original + ToAppend, 250f);
            Vector2 msgSize = Fonts.Arial12Bold.MeasureString(Message);
            var r = new Rectangle(ScreenWidth / 2 - BoxWidth/2, ScreenHeight / 2 - (int)(msgSize.Y + 40f) / 2,
                                  BoxWidth, (int)(msgSize.Y + 40f) + 15);

            var textPosition = new Vector2(r.X + r.Width / 2 - Fonts.Arial12Bold.MeasureString(Message).X / 2f, r.Y + 10);
            
            Ok.SetAbsPos(     r.X + r.Width / 2 + 5,  r.Y + r.Height - 28);
            Cancel?.SetAbsPos(r.X + r.Width / 2 - 73, r.Y + r.Height - 28);

            batch.Begin();
            batch.FillRectangle(r, Color.Black);
            batch.DrawRectangle(r, Color.Orange);
            batch.DrawString(Fonts.Arial12Bold, Message, textPosition, Color.White);
            base.Draw(batch, elapsed);
            batch.End();
        }

        void OnOkClicked(UIButton b)
        {
            Accepted?.Invoke();
            GameAudio.AffirmativeClick();
            ExitScreen();
        }

        void OnCancelClicked(UIButton b)
        {
            Cancelled?.Invoke();
            ExitScreen();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.MenuSelect)
            {
                Accepted?.Invoke();
                ExitScreen();
                return true;
            }
            if (input.MenuCancel)
            {
                Cancelled?.Invoke();
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        public override void Update(UpdateTimes elapsed, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            Timer -= elapsed.RealTime.Seconds;
            if (Timed && !IsExiting)
            {
                ToAppend = string.Concat(" ", Timer.String(0), " ", Localizer.Token(GameText.Seconds));
                if (Timer <= 0f)
                {
                    Cancelled?.Invoke();
                    ExitScreen();
                }
            }
            base.Update(elapsed, otherScreenHasFocus, coveredByOtherScreen);
        }
    }
}
