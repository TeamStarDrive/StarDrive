using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
    public  class MessageBoxScreen : GameScreen
    {
        private const string UsageText = "A button = Okay";
        private readonly bool PauseMenu;
        private string Message;

        public UIButton Ok;
        public UIButton Cancel;

        private float Timer;
        private readonly bool Timed;
        private readonly string Original = "";
        private string Toappend;

        public MessageBoxScreen(GameScreen parent, string message) : base(parent)
        {
            this.Message = message;
            this.Message = HelperFunctions.ParseText(Fonts.Arial12Bold, message, 250f);
            base.IsPopup = true;
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);

            Ok     = ButtonSmall(0f, 0f, "OK", titleId: 15);
            Cancel = ButtonSmall(0f, 0f, "Cancel", titleId:16);
        }

        public MessageBoxScreen(GameScreen parent, int localID, string oktext, string canceltext)
            : this(parent, Localizer.Token(localID), oktext, canceltext)
        {
        }
        
        public MessageBoxScreen(GameScreen parent, string message, string oktext, string canceltext) : base(parent)
        {
            this.Message = message;
            this.Message = HelperFunctions.ParseText(Fonts.Arial12Bold, message, 250f);
            base.IsPopup = true;
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);

            Ok     = ButtonSmall(0f, 0f, "OK", oktext);
            Cancel = ButtonSmall(0f, 0f, "Cancel", canceltext);
        }

        public MessageBoxScreen(GameScreen parent, string message, float Timer) : base(parent)
        {
            this.Timed = true;
            this.Timer = Timer;
            this.Original = message;
            this.Message = message;
            base.IsPopup = true;
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);

            Ok     = ButtonSmall(0f, 0f, "OK", titleId: 15);
            Cancel = ButtonSmall(0f, 0f, "Cancel", titleId:16);
        }

        public MessageBoxScreen(GameScreen parent, string message, bool pauseMenu) : this(parent, message)
        {
            this.PauseMenu = pauseMenu;
        }

        public override void Draw(SpriteBatch batch)
        {
            base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
            if (!this.Timed)
            {
                Rectangle r = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 135,
                    base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - (int)(Fonts.Arial12Bold.MeasureString(this.Message).Y + 40f) / 2, 270, (int)(Fonts.Arial12Bold.MeasureString(this.Message).Y + 40f) + 15);
                Vector2 textPosition = new Vector2((float)(r.X + r.Width / 2) - Fonts.Arial12Bold.MeasureString(this.Message).X / 2f, (float)(r.Y + 10));
                base.ScreenManager.SpriteBatch.Begin();
                base.ScreenManager.SpriteBatch.FillRectangle(r, Color.Black);
                base.ScreenManager.SpriteBatch.DrawRectangle(r, Color.Orange);
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(this.Message, this.Toappend), textPosition, Color.White);


                Ok.SetAbsPos(    r.X + r.Width / 2 + 5,  r.Y + r.Height - 28);
                Cancel.SetAbsPos(r.X + r.Width / 2 - 73, r.Y + r.Height - 28);
                foreach (UIButton b in this.Buttons)
                {
                    b.Draw(base.ScreenManager.SpriteBatch);
                }
                base.ScreenManager.SpriteBatch.End();
                return;
            }
            this.Message = HelperFunctions.ParseText(Fonts.Arial12Bold, string.Concat(this.Original, this.Toappend), 250f);
            //renamed r, textposition
            Rectangle r2 = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 135, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - (int)(Fonts.Arial12Bold.MeasureString(this.Message).Y + 40f) / 2, 270, (int)(Fonts.Arial12Bold.MeasureString(this.Message).Y + 40f) + 15);
            Vector2 textPosition2 = new Vector2((float)(r2.X + r2.Width / 2) - Fonts.Arial12Bold.MeasureString(this.Message).X / 2f, (float)(r2.Y + 10));
            base.ScreenManager.SpriteBatch.Begin();
            base.ScreenManager.SpriteBatch.FillRectangle(r2, Color.Black);
            base.ScreenManager.SpriteBatch.DrawRectangle(r2, Color.Orange);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.Message, textPosition2, Color.White);
            Ok.SetAbsPos(    r2.X + r2.Width / 2 + 5,  r2.Y + r2.Height - 28);
            Cancel.SetAbsPos(r2.X + r2.Width / 2 - 73, r2.Y + r2.Height - 28);

            foreach (UIButton b in this.Buttons)
            {
                b.Draw(base.ScreenManager.SpriteBatch);
            }
            base.ScreenManager.SpriteBatch.End();
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
            foreach (UIButton b in this.Buttons)
            {
                if (!b.Rect.HitTest(input.CursorPosition))
                {
                    b.State = UIButton.PressState.Default;
                }
                else
                {
                    if (b.State != UIButton.PressState.Hover && b.State != UIButton.PressState.Pressed)
                    {
                        GameAudio.PlaySfxAsync("mouse_over4");
                    }
                    b.State = UIButton.PressState.Hover;
                    if (input.LeftMouseHeldDown)
                    {
                        b.State = UIButton.PressState.Pressed;
                    }
                    if (!input.LeftMouseClick)
                    {
                        continue;
                    }
                    string launches = b.Launches;
                    string str = launches;
                    if (launches == null)
                    {
                        continue;
                    }
                    if (str == "OK")
                    {
                        if (this.Accepted != null)
                        {
                            this.Accepted(this, EventArgs.Empty);
                        }
                        GameAudio.PlaySfxAsync("echo_affirm1");
                        this.ExitScreen();
                    }
                    else if (str == "Cancel")
                    {
                        if (this.Cancelled != null)
                        {
                            this.Cancelled(this, EventArgs.Empty);
                        }
                        this.ExitScreen();
                    }
                }
            }
            return base.HandleInput(input);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            MessageBoxScreen messageBoxScreen = this;
            messageBoxScreen.Timer = messageBoxScreen.Timer - elapsedTime;
            if (this.Timed)
            {
                string fmt = "0";
                this.Toappend = string.Concat(this.Timer.ToString(fmt), " ", Localizer.Token(17));
                if (this.Timer <= 0f)
                {
                    if (this.Cancelled != null)
                    {
                        this.Cancelled(this, EventArgs.Empty);
                    }
                    this.ExitScreen();
                }
            }
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public event EventHandler<EventArgs> Accepted;

        public event EventHandler<EventArgs> Cancelled;
    }
}