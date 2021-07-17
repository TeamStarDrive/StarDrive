using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace Ship_Game
{
    public class UITextEntry : UIElementV2
    {
        string TextValue;
        public bool HandlingInput { get; private set; }
        
        /// <summary>
        /// If TRUE, this UITextEntry will automatically start capturing input
        /// if any keys are pressed
        /// </summary>
        public bool AutoCaptureOnKeys = false;

        /// <summary>
        /// If TRUE, this UITextEntry will automatically start capturing input
        /// when hovering
        /// </summary>
        public bool AutoCaptureOnHover = false;

        public bool Hover;
        public bool AllowPeriod = false;
        public bool AutoClearTextOnInputCapture;
        public int MaxCharacters = 40;
        int CursorPos;

        /// <summary>
        /// Time it takes for AutoCapture to lose focus on this text entry
        /// </summary>
        public float AutoCaptureLoseFocusTime = 1.0f;
        float AutoDecaptureTimer;
        
        const float FirstRepeatTime = 0.2f;
        const float RepeatInterval = 0.1f;
        float RepeatCooldown;
        float LastRepeatStart;

        public Graphics.Font Font = Fonts.Arial14Bold;
        public Color Color = Color.Orange;
        public Color HoverColor = Color.White;
        public Color InputColor = Color.BurlyWood;

        /// <summary>
        /// EVT: Text input has been captured, user is starting to type text
        /// </summary>
        public Action OnTextInputCapture;

        /// <summary>
        /// EVT: Text was changed during input
        /// </summary>
        public Action<string> OnTextChanged;
        bool IsInvokingOnTextChanged;

        /// <summary>
        /// EVT: Text was submitted using ENTER or ESCAPE
        /// </summary>
        public Action<string> OnTextSubmit;

        /// <summary>
        /// Takes ownership of a background element to draw and update 
        /// </summary>
        public UIElementV2 Background;

        public UITextEntry()
        {
        }

        public UITextEntry(in LocalizedText text) : this(Vector2.Zero, Fonts.Arial20Bold, text)
        {
        }

        public UITextEntry(Vector2 pos, in LocalizedText text) : this(pos, Fonts.Arial20Bold, text)
        {
        }

        public UITextEntry(Vector2 pos, Graphics.Font font, in LocalizedText text)
            : this(pos.X, pos.Y, font.TextWidth(text) + 20, font, text)
        {
        }
        
        public UITextEntry(float x, float y, float width, Graphics.Font font, in LocalizedText text)
            : this(x, y, width, font.LineSpacing + 2, font, text)
        {
        }

        public UITextEntry(in Rectangle rect, Graphics.Font font, in LocalizedText text)
            : this(rect.X, rect.Y, rect.Width, rect.Height, font, text)
        {
        }

        public UITextEntry(float x, float y, float width, float height, Graphics.Font font, in LocalizedText text)
        {
            Font = font;
            Text = text.Text;
            CursorPos = Text.Length;
            Pos = new Vector2(x, y);
            Size = new Vector2(width, height);
        }
        
        public void Clear()
        {
            HandlingInput = false;
            Text = "";
        }

        public void Reset(string text)
        {
            HandlingInput = false;
            CursorPos = text.Length;
            Text = text;
        }

        void StartInput()
        {
            if (HandlingInput)
                return;

            HandlingInput = true;
            GlobalStats.TakingInput = true;

            if (AutoClearTextOnInputCapture)
                Text = "";
            else
                CursorPos = TextValue.Length;

            OnTextInputCapture?.Invoke();
        }

        // If Input is being captured, stops capture an submits the current text
        public void StopInput()
        {
            if (!HandlingInput)
                return;

            HandlingInput = false;
            GlobalStats.TakingInput = false;
            OnTextSubmit?.Invoke(TextValue);
        }

        public void SetPos(float x, float y) => SetPos(new Vector2(x, y));
        public void SetPos(Vector2 pos)
        {
            Pos = pos;
            int newWidth = Font.TextWidth(Text) + 20;
            Width = (newWidth > Width) ? newWidth : Width;
        }

        public void SetColors(Color color, Color hoverColor)
        {
            Color = color;
            HoverColor = hoverColor;
        }

        public string Text
        {
            get => TextValue;
            set
            {
                if (TextValue == value)
                    return;

                TextValue = value;
                CursorPos = CursorPos.Clamped(0, value.Length);
                if (IsInvokingOnTextChanged)
                    return;
                try
                {
                    IsInvokingOnTextChanged = true;
                    OnTextChanged?.Invoke(value);
                }
                finally
                {
                    IsInvokingOnTextChanged = false;
                }
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Background?.Draw(batch, elapsed);

            Vector2 pos = Pos;
            Color color = Color;
            if (HandlingInput) color = InputColor;
            else if (Hover)    color = HoverColor;

            pos.X += 2f;
            batch.DrawString(Font, Text, pos, color);
            if (HandlingInput)
            {
                float f = Math.Abs(RadMath.Sin(GameBase.Base.TotalElapsed * 5f));
                var flashColor = Color.White.Alpha((f + 0.1f).Clamped(0f, 1f));
                
                int length = Math.Min(Text.Length, CursorPos);
                string substring = Text.Substring(0, length);
                pos.X += Font.TextWidth(substring);
                batch.DrawString(Font, "|", pos, flashColor);
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (!Enabled)
                return false;

            // some gamescreen has decided to globally disable input
            if (HandlingInput && !GlobalStats.TakingInput)
                HandlingInput = false;

            //Background?.HandleInput(input); // not really needed for background elements

            bool wasHovering = Hover; // was hovering last frame
            Hover = HitTest(input.CursorPosition);
            bool autoKeysDown = AutoCaptureOnKeys && AnyValidInputKeysDown(input);
            bool autoHover    = AutoCaptureOnHover && Hover;
            bool hoverAndClick = Hover && input.LeftMouseClick;

            if ((autoKeysDown || autoHover || hoverAndClick) && !HandlingInput)
            {
                StartInput();
            }
            // click in the middle of the text?
            else if (Hover && HandlingInput && input.LeftMouseClick)
            {
                float textWidth = Font.TextWidth(TextValue);
                float localX = input.CursorPosition.X - Pos.X;
                float relX = (localX / textWidth).Clamped(0f, 1f);
                CursorPos = (int)(relX * TextValue.Length);
            }
            else if (HandlingInput)
            {
                if (!Hover)
                {
                    // in case we have both CaptureOnKeys and CaptureOnHover, disallow autoKeysExit
                    bool autoKeysExit = AutoCaptureOnKeys && !AutoCaptureOnHover 
                                         && !autoKeysDown && AutoDecaptureTimer <= 0f;
                    bool autoHoverExit = AutoCaptureOnHover && !Hover && wasHovering;
                    if (autoKeysExit || autoHoverExit || input.RightMouseClick || input.LeftMouseClick)
                    {
                        StopInput();
                    }
                }
                else if (input.IsEnterOrEscape)
                {
                    StopInput();
                }
            }

            if (HandlingInput)
                return HandleTextInput(input);
            return false;
        }

        bool HandleTextInput(InputState input)
        {
            AutoDecaptureTimer -= GameBase.Base.Elapsed.RealTime.Seconds;
            if (HandleCursor(input))
                return true;

            Keys[] keysDown = input.GetKeysDown();
            for (int i = 0; i < keysDown.Length; i++)
            {
                AutoDecaptureTimer = AutoCaptureLoseFocusTime;
                Keys key = keysDown[i];
                if (key != Keys.Back && input.KeyPressed(key) && TextValue.Length < MaxCharacters)
                {
                    if (AddKeyToText(input, key))
                        GameAudio.BlipClick();
                    else
                        GameAudio.NegativeClick();
                    return true; // TODO: align return with new UI system
                }
            }

            // NOTE: always force input capture
            return true; // TODO: align return with new UI system
        }

        bool HandleCursor(InputState input)
        {
            RepeatCooldown -= GameBase.Base.Elapsed.RealTime.Seconds;

            bool back   = input.IsKeyDown(Keys.Back);
            bool delete = input.IsKeyDown(Keys.Delete);
            bool left   = input.IsKeyDown(Keys.Left);
            bool right  = input.IsKeyDown(Keys.Right);
            if (!back && !delete && !left && !right)
                return false;
            
            AutoDecaptureTimer = AutoCaptureLoseFocusTime;

            // back, left or right were pressed, wait until cooldown reaches 0
            if (RepeatCooldown <= 0f)
            {
                float timeSinceLastStart = (GameBase.Base.TotalElapsed - LastRepeatStart);
                LastRepeatStart = GameBase.Base.TotalElapsed;

                if (timeSinceLastStart > FirstRepeatTime*1.5f) // slow it down a little during first press
                    RepeatCooldown = FirstRepeatTime;
                else // user is holding down the keys
                    RepeatCooldown = RepeatInterval;

                if (HandleCursorMove(back, delete, left, right))
                    GameAudio.BlipClick();
                else
                    GameAudio.NegativeClick();
            }
            return true;
        }

        bool HandleCursorMove(bool back, bool delete, bool left, bool right)
        {
            CursorPos = CursorPos.Clamped(0, TextValue.Length);

            if (back && TextValue.Length != 0 && CursorPos > 0)
            {
                --CursorPos;
                Text = TextValue.Remove(CursorPos, 1);
                return true;
            }
            else if (delete && TextValue.Length != 0 && CursorPos < TextValue.Length)
            {
                Text = TextValue.Remove(CursorPos, 1);
                return true;
            }
            else if (left && CursorPos > 0)
            {
                --CursorPos;
                return true;
            }
            else if (right && CursorPos < TextValue.Length)
            {
                ++CursorPos;
                return true;
            }
            return false;
        }

        bool AddKeyToText(InputState input, Keys key)
        {
            char ch = GetCharFromKey(key);
            if (ch != '\0')
            {
                if (input.IsShiftKeyDown || input.IsCapsLockDown)
                    ch = char.ToUpper(ch);
                
                CursorPos = CursorPos.Clamped(0, TextValue.Length);
                Text = TextValue.Insert(CursorPos, ch.ToString());
                CursorPos += 1;
                return true;
            }
            return false;
        }

        char GetCharFromKey(Keys key)
        {
            switch (key)
            {
                default: return '\0';
                case Keys.Space: return ' ';
                case Keys.D0: return '0';
                case Keys.D1: return '1';
                case Keys.D2: return '2';
                case Keys.D3: return '3';
                case Keys.D4: return '4';
                case Keys.D5: return '5';
                case Keys.D6: return '6';
                case Keys.D7: return '7';
                case Keys.D8: return '8';
                case Keys.D9: return '9';
                case Keys.A: return 'a';
                case Keys.B: return 'b';
                case Keys.C: return 'c';
                case Keys.D: return 'd';
                case Keys.E: return 'e';
                case Keys.F: return 'f';
                case Keys.G: return 'g';
                case Keys.H: return 'h';
                case Keys.I: return 'i';
                case Keys.J: return 'j';
                case Keys.K: return 'k';
                case Keys.L: return 'l';
                case Keys.M: return 'm';
                case Keys.N: return 'n';
                case Keys.O: return 'o';
                case Keys.P: return 'p';
                case Keys.Q: return 'q';
                case Keys.R: return 'r';
                case Keys.S: return 's';
                case Keys.T: return 't';
                case Keys.U: return 'u';
                case Keys.V: return 'v';
                case Keys.W: return 'w';
                case Keys.X: return 'x';
                case Keys.Y: return 'y';
                case Keys.Z: return 'z';
                case Keys.NumPad0: return '0';
                case Keys.NumPad1: return '1';
                case Keys.NumPad2: return '2';
                case Keys.NumPad3: return '3';
                case Keys.NumPad4: return '4';
                case Keys.NumPad5: return '5';
                case Keys.NumPad6: return '6';
                case Keys.NumPad7: return '7';
                case Keys.NumPad8: return '8';
                case Keys.NumPad9: return '9';
                case Keys.OemMinus: return '-';
                case Keys.OemQuotes: return '\'';
                case Keys.OemPeriod when AllowPeriod: return '.';
            }
        }

        bool IsValidKey(Keys key) => GetCharFromKey(key) != '\0' || IsCursorKey(key);
        bool IsCursorKey(Keys key) => key == Keys.Back || key == Keys.Delete || key == Keys.Left || key == Keys.Right;
        bool AnyValidInputKeysDown(InputState input) => input.GetKeysDown().Any(IsValidKey);
    }
}