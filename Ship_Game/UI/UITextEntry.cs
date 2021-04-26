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
        public Rectangle ClickableArea
        {
            get => Rect;
            set => Rect = value;
        }

        string TextValue;
        public bool HandlingInput;
        
        /// <summary>
        /// If TRUE, this UITextEntry will automatically capture the screen's
        /// Keyboard input if anything is typed or if mouse is hovered over the entry
        /// </summary>
        public bool AutoCaptureInput = false;

        public bool Hover;
        public bool AllowPeriod = false;
        public bool ResetTextOnInput;
        public int MaxCharacters = 30;
        int CursorPos;

        const float FirstRepeatTime = 0.2f;
        const float RepeatInterval = 0.1f;
        const float AutoCaptureInterval = 1.0f;
        float RepeatCooldown;
        float KeyPressCooldown;
        float LastRepeatStart;

        public Graphics.Font Font = Fonts.Arial14Bold;
        public Color Color = Color.Orange;
        public Color HoverColor = Color.White;
        public Color InputColor = Color.BurlyWood;

        /// <summary>
        /// EVT: Text was changed during input
        /// </summary>
        public Action<string> OnTextChanged;
        bool IsInvokingOnTextChanged;

        /// <summary>
        /// EVT: Text was submitted using ENTER or ESCAPE
        /// </summary>
        public Action<string> OnTextSubmit;

        public UITextEntry()
        {
        }

        public UITextEntry(string text) : this(Vector2.Zero, Fonts.Arial20Bold, text)
        {
        }

        public UITextEntry(Vector2 pos, string text) : this(pos, Fonts.Arial20Bold, text)
        {
        }

        public UITextEntry(Vector2 pos, Graphics.Font font, string text)
        {
            Font = font;
            Text = text;
            CursorPos = text.Length;
            ClickableArea = new Rectangle((int)pos.X, (int)pos.Y, font.TextWidth(Text) + 20, font.LineSpacing);
        }

        public UITextEntry(float x, float y, float width, Graphics.Font font = null)
        {
            Font = font ?? Fonts.Arial20Bold;
            Text = "";
            CursorPos = 0;
            Pos = new Vector2(x, y);
            Size = new Vector2(width, Font.LineSpacing);
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
            Vector2 pos = Pos;
            Color color = Color;
            if (HandlingInput) color = InputColor;
            else if (Hover)    color = HoverColor;

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

            Hover = ClickableArea.HitTest(input.CursorPosition);

            bool autoKeysDown = AutoCaptureInput && input.GetKeysDown().Length > 0;
            bool capture = Hover || autoKeysDown;
            if (capture && !HandlingInput)
            {
                if (AutoCaptureInput || input.LeftMouseClick)
                {
                    HandlingInput = true;
                    GlobalStats.TakingInput = true;
                    if (ResetTextOnInput)
                        Text = "";
                    return true;
                }
            }

            if (!Hover && HandlingInput)
            {
                bool autoExit = AutoCaptureInput && !autoKeysDown && KeyPressCooldown <= 0f;
                if (autoExit || input.RightMouseClick || input.LeftMouseClick)
                {
                    HandlingInput = false;
                    GlobalStats.TakingInput = false;
                    return true;
                }
            }

            // click in the middle of the text?
            if (Hover && HandlingInput && input.LeftMouseClick)
            {
                float textWidth = Font.TextWidth(TextValue);
                float localX = input.CursorPosition.X - Pos.X;
                float relX = (localX / textWidth).Clamped(0f, 1f);
                CursorPos = (int)(relX * TextValue.Length);
            }

            if (HandlingInput)
                return HandleTextInput(input);
            return false;
        }

        bool HandleTextInput(InputState input)
        {
            KeyPressCooldown -= GameBase.Base.Elapsed.RealTime.Seconds;

            if (input.IsEnterOrEscape)
            {
                HandlingInput = false;
                OnTextSubmit?.Invoke(TextValue);
                return true;
            }

            if (HandleCursor(input))
                return true;

            Keys[] keysDown = input.GetKeysDown();
            for (int i = 0; i < keysDown.Length; i++)
            {
                KeyPressCooldown = AutoCaptureInterval;
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
    }
}