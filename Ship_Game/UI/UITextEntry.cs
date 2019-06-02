using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace Ship_Game
{
    using InputKeys = Keys;

    public class UITextEntry
    {
        public Rectangle ClickableArea;
        public string Text;
        public bool HandlingInput;
        public bool Hover;
        private readonly InputKeys[] KeysToCheck =
        {
            InputKeys.A, InputKeys.B, InputKeys.C, InputKeys.D,
            InputKeys.E, InputKeys.F,
            InputKeys.G, InputKeys.H,
            InputKeys.I, InputKeys.J,
            InputKeys.K, InputKeys.L,
            InputKeys.M, InputKeys.N,
            InputKeys.O, InputKeys.P,
            InputKeys.Q, InputKeys.R,
            InputKeys.S, InputKeys.T,
            InputKeys.U, InputKeys.V,
            InputKeys.W, InputKeys.X,
            InputKeys.Y, InputKeys.Z,
            InputKeys.Back, InputKeys.Space,
            InputKeys.NumPad0, InputKeys.NumPad1,
            InputKeys.NumPad2, InputKeys.NumPad3,
            InputKeys.NumPad4, InputKeys.NumPad5,
            InputKeys.NumPad6, InputKeys.NumPad7,
            InputKeys.NumPad8, InputKeys.NumPad9,
            InputKeys.OemMinus, InputKeys.OemQuotes,
            InputKeys.D0, InputKeys.D1,
            InputKeys.D2, InputKeys.D3,
            InputKeys.D4, InputKeys.D5,
            InputKeys.D6, InputKeys.D7,
            InputKeys.D8, InputKeys.D9
        };
        public int MaxCharacters = 30;
        private int boop;

        private void AddKeyToText(ref string text, InputKeys key, InputState input)
        {
            if (text.Length >= 60 && key != InputKeys.Back)
                return;

            void AppendKeyChar(ref string textRef, char ch)
            {
                if (input.IsKeyDown(InputKeys.RightShift) || input.IsKeyDown(InputKeys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
                    ch = char.ToUpper(ch);
                textRef += ch;
            }

            switch (key)
            {
                case InputKeys.Space: AppendKeyChar(ref text, ' '); return;
                case InputKeys.PageUp:
                case InputKeys.PageDown:
                case InputKeys.End:
                case InputKeys.Home:
                case InputKeys.Left:
                case InputKeys.Up:
                case InputKeys.Right:
                case InputKeys.Down:
                case InputKeys.Select:
                case InputKeys.Print:
                case InputKeys.Execute:
                case InputKeys.PrintScreen:
                case InputKeys.Insert:
                case InputKeys.Delete:
                case InputKeys.Help:
                case InputKeys.Back | InputKeys.D0 | InputKeys.D2 | InputKeys.D8 | InputKeys.Down | InputKeys.PageDown | InputKeys.Print | InputKeys.Space:
                case InputKeys.Back | InputKeys.D0 | InputKeys.D1 | InputKeys.D2 | InputKeys.D3 | InputKeys.D8 | InputKeys.D9 | InputKeys.Down | InputKeys.End | InputKeys.Escape | InputKeys.Execute | InputKeys.Kanji | InputKeys.PageDown | InputKeys.PageUp | InputKeys.Pause | InputKeys.Print | InputKeys.Select | InputKeys.Space | InputKeys.Tab:
                case InputKeys.Back | InputKeys.CapsLock | InputKeys.D0 | InputKeys.D4 | InputKeys.D8 | InputKeys.Down | InputKeys.Home | InputKeys.ImeConvert | InputKeys.PrintScreen | InputKeys.Space:
                case InputKeys.Back | InputKeys.CapsLock | InputKeys.D0 | InputKeys.D1 | InputKeys.D4 | InputKeys.D5 | InputKeys.D8 | InputKeys.D9 | InputKeys.Down | InputKeys.Enter | InputKeys.Home | InputKeys.ImeConvert | InputKeys.ImeNoConvert | InputKeys.Insert | InputKeys.Kana | InputKeys.Kanji | InputKeys.Left | InputKeys.PageUp | InputKeys.PrintScreen | InputKeys.Select | InputKeys.Space | InputKeys.Tab:
                case InputKeys.Back | InputKeys.CapsLock | InputKeys.D0 | InputKeys.D2 | InputKeys.D4 | InputKeys.D6 | InputKeys.D8 | InputKeys.Delete | InputKeys.Down | InputKeys.Home | InputKeys.ImeConvert | InputKeys.PageDown | InputKeys.Print | InputKeys.PrintScreen | InputKeys.Space | InputKeys.Up:
                case InputKeys.Back | InputKeys.CapsLock | InputKeys.D0 | InputKeys.D1 | InputKeys.D2 | InputKeys.D3 | InputKeys.D4 | InputKeys.D5 | InputKeys.D6 | InputKeys.D7 | InputKeys.D8 | InputKeys.D9 | InputKeys.Delete | InputKeys.Down | InputKeys.End | InputKeys.Enter | InputKeys.Escape | InputKeys.Execute | InputKeys.Help | InputKeys.Home | InputKeys.ImeConvert | InputKeys.ImeNoConvert | InputKeys.Insert | InputKeys.Kana | InputKeys.Kanji | InputKeys.Left | InputKeys.PageDown | InputKeys.PageUp | InputKeys.Pause | InputKeys.Print | InputKeys.PrintScreen | InputKeys.Right | InputKeys.Select | InputKeys.Space | InputKeys.Tab | InputKeys.Up:
                //case 64:  wtf?
                case InputKeys.LeftWindows:
                case InputKeys.RightWindows:
                case InputKeys.Apps:
                case InputKeys.B | InputKeys.Back | InputKeys.CapsLock | InputKeys.D | InputKeys.F | InputKeys.H | InputKeys.ImeConvert | InputKeys.J | InputKeys.L | InputKeys.N | InputKeys.P | InputKeys.R | InputKeys.RightWindows | InputKeys.T | InputKeys.V | InputKeys.X | InputKeys.Z:
                case InputKeys.Sleep: return;
                case InputKeys.D0: AppendKeyChar(ref text, '0'); return;
                case InputKeys.D1: AppendKeyChar(ref text, '1'); return;
                case InputKeys.D2: AppendKeyChar(ref text, '2'); return;
                case InputKeys.D3: AppendKeyChar(ref text, '3'); return;
                case InputKeys.D4: AppendKeyChar(ref text, '4'); return;
                case InputKeys.D5: AppendKeyChar(ref text, '5'); return;
                case InputKeys.D6: AppendKeyChar(ref text, '6'); return;
                case InputKeys.D7: AppendKeyChar(ref text, '7'); return;
                case InputKeys.D8: AppendKeyChar(ref text, '8'); return;
                case InputKeys.D9: AppendKeyChar(ref text, '9'); return;
                case InputKeys.A: AppendKeyChar(ref text, 'a'); return;
                case InputKeys.B: AppendKeyChar(ref text, 'b'); return;
                case InputKeys.C: AppendKeyChar(ref text, 'c'); return;
                case InputKeys.D: AppendKeyChar(ref text, 'd'); return;
                case InputKeys.E: AppendKeyChar(ref text, 'e'); return;
                case InputKeys.F: AppendKeyChar(ref text, 'f'); return;
                case InputKeys.G: AppendKeyChar(ref text, 'g'); return;
                case InputKeys.H: AppendKeyChar(ref text, 'h'); return;
                case InputKeys.I: AppendKeyChar(ref text, 'i'); return;
                case InputKeys.J: AppendKeyChar(ref text, 'j'); return;
                case InputKeys.K: AppendKeyChar(ref text, 'k'); return;
                case InputKeys.L: AppendKeyChar(ref text, 'l'); return;
                case InputKeys.M: AppendKeyChar(ref text, 'm'); return;
                case InputKeys.N: AppendKeyChar(ref text, 'n'); return;
                case InputKeys.O: AppendKeyChar(ref text, 'o'); return;
                case InputKeys.P: AppendKeyChar(ref text, 'p'); return;
                case InputKeys.Q: AppendKeyChar(ref text, 'q'); return;
                case InputKeys.R: AppendKeyChar(ref text, 'r'); return;
                case InputKeys.S: AppendKeyChar(ref text, 's'); return;
                case InputKeys.T: AppendKeyChar(ref text, 't'); return;
                case InputKeys.U: AppendKeyChar(ref text, 'u'); return;
                case InputKeys.V: AppendKeyChar(ref text, 'v'); return;
                case InputKeys.W: AppendKeyChar(ref text, 'w'); return;
                case InputKeys.X: AppendKeyChar(ref text, 'x'); return;
                case InputKeys.Y: AppendKeyChar(ref text, 'y'); return;
                case InputKeys.Z: AppendKeyChar(ref text, 'z'); return;
                case InputKeys.NumPad0: AppendKeyChar(ref text, '0'); return;
                case InputKeys.NumPad1: AppendKeyChar(ref text, '1'); return;
                case InputKeys.NumPad2: AppendKeyChar(ref text, '2'); return;
                case InputKeys.NumPad3: AppendKeyChar(ref text, '3'); return;
                case InputKeys.NumPad4: AppendKeyChar(ref text, '4'); return;
                case InputKeys.NumPad5: AppendKeyChar(ref text, '5'); return;
                case InputKeys.NumPad6: AppendKeyChar(ref text, '6'); return;
                case InputKeys.NumPad7: AppendKeyChar(ref text, '7'); return;
                case InputKeys.NumPad8: AppendKeyChar(ref text, '8'); return;
                case InputKeys.NumPad9: AppendKeyChar(ref text, '9'); return;
                case InputKeys.OemMinus: AppendKeyChar(ref text, '-'); return;
                case InputKeys.OemQuotes: AppendKeyChar(ref text, '\''); return;
            }
        }

        bool CheckKey(InputKeys key, InputState input)
        {
            if (key == InputKeys.Back && boop == 0)
                return input.IsKeyDown(key);
            return input.KeyPressed(key);
        }

        public void Draw(SpriteFont font, SpriteBatch spriteBatch, Vector2 pos, GameTime gameTime, Color c)
        {
            spriteBatch.DrawString(font, Text, pos, c);
            pos.X = pos.X + font.MeasureString(Text).X;
            if (HandlingInput)
            {
                float f = Math.Abs(RadMath.Sin(gameTime.TotalGameTime.TotalSeconds)) * 255f;
                var flashColor = new Color(255, 255, 255, (byte)f);
                spriteBatch.DrawString(font, "|", pos, flashColor);
            }
        }

        public void HandleTextInput(ref string text, InputState input)
        {
            InputKeys[] keysArray = KeysToCheck;
            for (int i = 0; i < keysArray.Length; i++)
            {
                InputKeys key = keysArray[i];
                if (CheckKey(key, input))
                {
                    if (text.Length >= MaxCharacters)
                    {
                        GameAudio.NegativeClick();
                    }
                    else
                    {
                        AddKeyToText(ref text, key, input);
                        GameAudio.BlipClick();
                        break;
                    }
                }
            }

            if (input.IsEnterOrEscape)
                HandlingInput = false;

            if (input.IsKeyDown(InputKeys.Back) && boop == 0 && text.Length != 0)
                text = text.Remove(text.Length - 1);

            boop++;
            if (boop == 7)
                boop = 0;
        }

    }
}