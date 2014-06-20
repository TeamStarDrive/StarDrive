using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Windows.Forms;

namespace Ship_Game
{
	public class UITextEntry
	{
		public Rectangle ClickableArea;

		public string Text;

		public bool HandlingInput;

		public bool Hover;

		private Microsoft.Xna.Framework.Input.Keys[] keysToCheck = new Microsoft.Xna.Framework.Input.Keys[] { Microsoft.Xna.Framework.Input.Keys.A, Microsoft.Xna.Framework.Input.Keys.B, Microsoft.Xna.Framework.Input.Keys.C, Microsoft.Xna.Framework.Input.Keys.D, Microsoft.Xna.Framework.Input.Keys.E, Microsoft.Xna.Framework.Input.Keys.F, Microsoft.Xna.Framework.Input.Keys.G, Microsoft.Xna.Framework.Input.Keys.H, Microsoft.Xna.Framework.Input.Keys.I, Microsoft.Xna.Framework.Input.Keys.J, Microsoft.Xna.Framework.Input.Keys.K, Microsoft.Xna.Framework.Input.Keys.L, Microsoft.Xna.Framework.Input.Keys.M, Microsoft.Xna.Framework.Input.Keys.N, Microsoft.Xna.Framework.Input.Keys.O, Microsoft.Xna.Framework.Input.Keys.P, Microsoft.Xna.Framework.Input.Keys.Q, Microsoft.Xna.Framework.Input.Keys.R, Microsoft.Xna.Framework.Input.Keys.S, Microsoft.Xna.Framework.Input.Keys.T, Microsoft.Xna.Framework.Input.Keys.U, Microsoft.Xna.Framework.Input.Keys.V, Microsoft.Xna.Framework.Input.Keys.W, Microsoft.Xna.Framework.Input.Keys.X, Microsoft.Xna.Framework.Input.Keys.Y, Microsoft.Xna.Framework.Input.Keys.Z, Microsoft.Xna.Framework.Input.Keys.Back, Microsoft.Xna.Framework.Input.Keys.Space, Microsoft.Xna.Framework.Input.Keys.NumPad0, Microsoft.Xna.Framework.Input.Keys.NumPad1, Microsoft.Xna.Framework.Input.Keys.NumPad2, Microsoft.Xna.Framework.Input.Keys.NumPad3, Microsoft.Xna.Framework.Input.Keys.NumPad4, Microsoft.Xna.Framework.Input.Keys.NumPad5, Microsoft.Xna.Framework.Input.Keys.NumPad6, Microsoft.Xna.Framework.Input.Keys.NumPad7, Microsoft.Xna.Framework.Input.Keys.NumPad8, Microsoft.Xna.Framework.Input.Keys.NumPad9, Microsoft.Xna.Framework.Input.Keys.OemMinus, Microsoft.Xna.Framework.Input.Keys.OemQuotes, Microsoft.Xna.Framework.Input.Keys.D0, Microsoft.Xna.Framework.Input.Keys.D1, Microsoft.Xna.Framework.Input.Keys.D2, Microsoft.Xna.Framework.Input.Keys.D3, Microsoft.Xna.Framework.Input.Keys.D4, Microsoft.Xna.Framework.Input.Keys.D5, Microsoft.Xna.Framework.Input.Keys.D6, Microsoft.Xna.Framework.Input.Keys.D7, Microsoft.Xna.Framework.Input.Keys.D8, Microsoft.Xna.Framework.Input.Keys.D9 };

		private KeyboardState currentKeyboardState;

		private KeyboardState lastKeyboardState;

		public int MaxCharacters = 30;

		private int boop;

		public UITextEntry()
		{
		}

		private void AddKeyToText(ref string text, Microsoft.Xna.Framework.Input.Keys key)
		{
			string newChar = "";
			if (text.Length >= 60 && key != Microsoft.Xna.Framework.Input.Keys.Back)
			{
				return;
			}
			Microsoft.Xna.Framework.Input.Keys key1 = key;
			switch (key1)
			{
				case Microsoft.Xna.Framework.Input.Keys.Space:
				{
					newChar = string.Concat(newChar, " ");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.PageUp:
				case Microsoft.Xna.Framework.Input.Keys.PageDown:
				case Microsoft.Xna.Framework.Input.Keys.End:
				case Microsoft.Xna.Framework.Input.Keys.Home:
				case Microsoft.Xna.Framework.Input.Keys.Left:
				case Microsoft.Xna.Framework.Input.Keys.Up:
				case Microsoft.Xna.Framework.Input.Keys.Right:
				case Microsoft.Xna.Framework.Input.Keys.Down:
				case Microsoft.Xna.Framework.Input.Keys.Select:
				case Microsoft.Xna.Framework.Input.Keys.Print:
				case Microsoft.Xna.Framework.Input.Keys.Execute:
				case Microsoft.Xna.Framework.Input.Keys.PrintScreen:
				case Microsoft.Xna.Framework.Input.Keys.Insert:
				case Microsoft.Xna.Framework.Input.Keys.Delete:
				case Microsoft.Xna.Framework.Input.Keys.Help:
				case Microsoft.Xna.Framework.Input.Keys.Back | Microsoft.Xna.Framework.Input.Keys.D0 | Microsoft.Xna.Framework.Input.Keys.D2 | Microsoft.Xna.Framework.Input.Keys.D8 | Microsoft.Xna.Framework.Input.Keys.Down | Microsoft.Xna.Framework.Input.Keys.PageDown | Microsoft.Xna.Framework.Input.Keys.Print | Microsoft.Xna.Framework.Input.Keys.Space:
				case Microsoft.Xna.Framework.Input.Keys.Back | Microsoft.Xna.Framework.Input.Keys.D0 | Microsoft.Xna.Framework.Input.Keys.D1 | Microsoft.Xna.Framework.Input.Keys.D2 | Microsoft.Xna.Framework.Input.Keys.D3 | Microsoft.Xna.Framework.Input.Keys.D8 | Microsoft.Xna.Framework.Input.Keys.D9 | Microsoft.Xna.Framework.Input.Keys.Down | Microsoft.Xna.Framework.Input.Keys.End | Microsoft.Xna.Framework.Input.Keys.Escape | Microsoft.Xna.Framework.Input.Keys.Execute | Microsoft.Xna.Framework.Input.Keys.Kanji | Microsoft.Xna.Framework.Input.Keys.PageDown | Microsoft.Xna.Framework.Input.Keys.PageUp | Microsoft.Xna.Framework.Input.Keys.Pause | Microsoft.Xna.Framework.Input.Keys.Print | Microsoft.Xna.Framework.Input.Keys.Select | Microsoft.Xna.Framework.Input.Keys.Space | Microsoft.Xna.Framework.Input.Keys.Tab:
				case Microsoft.Xna.Framework.Input.Keys.Back | Microsoft.Xna.Framework.Input.Keys.CapsLock | Microsoft.Xna.Framework.Input.Keys.D0 | Microsoft.Xna.Framework.Input.Keys.D4 | Microsoft.Xna.Framework.Input.Keys.D8 | Microsoft.Xna.Framework.Input.Keys.Down | Microsoft.Xna.Framework.Input.Keys.Home | Microsoft.Xna.Framework.Input.Keys.ImeConvert | Microsoft.Xna.Framework.Input.Keys.PrintScreen | Microsoft.Xna.Framework.Input.Keys.Space:
				case Microsoft.Xna.Framework.Input.Keys.Back | Microsoft.Xna.Framework.Input.Keys.CapsLock | Microsoft.Xna.Framework.Input.Keys.D0 | Microsoft.Xna.Framework.Input.Keys.D1 | Microsoft.Xna.Framework.Input.Keys.D4 | Microsoft.Xna.Framework.Input.Keys.D5 | Microsoft.Xna.Framework.Input.Keys.D8 | Microsoft.Xna.Framework.Input.Keys.D9 | Microsoft.Xna.Framework.Input.Keys.Down | Microsoft.Xna.Framework.Input.Keys.Enter | Microsoft.Xna.Framework.Input.Keys.Home | Microsoft.Xna.Framework.Input.Keys.ImeConvert | Microsoft.Xna.Framework.Input.Keys.ImeNoConvert | Microsoft.Xna.Framework.Input.Keys.Insert | Microsoft.Xna.Framework.Input.Keys.Kana | Microsoft.Xna.Framework.Input.Keys.Kanji | Microsoft.Xna.Framework.Input.Keys.Left | Microsoft.Xna.Framework.Input.Keys.PageUp | Microsoft.Xna.Framework.Input.Keys.PrintScreen | Microsoft.Xna.Framework.Input.Keys.Select | Microsoft.Xna.Framework.Input.Keys.Space | Microsoft.Xna.Framework.Input.Keys.Tab:
				case Microsoft.Xna.Framework.Input.Keys.Back | Microsoft.Xna.Framework.Input.Keys.CapsLock | Microsoft.Xna.Framework.Input.Keys.D0 | Microsoft.Xna.Framework.Input.Keys.D2 | Microsoft.Xna.Framework.Input.Keys.D4 | Microsoft.Xna.Framework.Input.Keys.D6 | Microsoft.Xna.Framework.Input.Keys.D8 | Microsoft.Xna.Framework.Input.Keys.Delete | Microsoft.Xna.Framework.Input.Keys.Down | Microsoft.Xna.Framework.Input.Keys.Home | Microsoft.Xna.Framework.Input.Keys.ImeConvert | Microsoft.Xna.Framework.Input.Keys.PageDown | Microsoft.Xna.Framework.Input.Keys.Print | Microsoft.Xna.Framework.Input.Keys.PrintScreen | Microsoft.Xna.Framework.Input.Keys.Space | Microsoft.Xna.Framework.Input.Keys.Up:
				case Microsoft.Xna.Framework.Input.Keys.Back | Microsoft.Xna.Framework.Input.Keys.CapsLock | Microsoft.Xna.Framework.Input.Keys.D0 | Microsoft.Xna.Framework.Input.Keys.D1 | Microsoft.Xna.Framework.Input.Keys.D2 | Microsoft.Xna.Framework.Input.Keys.D3 | Microsoft.Xna.Framework.Input.Keys.D4 | Microsoft.Xna.Framework.Input.Keys.D5 | Microsoft.Xna.Framework.Input.Keys.D6 | Microsoft.Xna.Framework.Input.Keys.D7 | Microsoft.Xna.Framework.Input.Keys.D8 | Microsoft.Xna.Framework.Input.Keys.D9 | Microsoft.Xna.Framework.Input.Keys.Delete | Microsoft.Xna.Framework.Input.Keys.Down | Microsoft.Xna.Framework.Input.Keys.End | Microsoft.Xna.Framework.Input.Keys.Enter | Microsoft.Xna.Framework.Input.Keys.Escape | Microsoft.Xna.Framework.Input.Keys.Execute | Microsoft.Xna.Framework.Input.Keys.Help | Microsoft.Xna.Framework.Input.Keys.Home | Microsoft.Xna.Framework.Input.Keys.ImeConvert | Microsoft.Xna.Framework.Input.Keys.ImeNoConvert | Microsoft.Xna.Framework.Input.Keys.Insert | Microsoft.Xna.Framework.Input.Keys.Kana | Microsoft.Xna.Framework.Input.Keys.Kanji | Microsoft.Xna.Framework.Input.Keys.Left | Microsoft.Xna.Framework.Input.Keys.PageDown | Microsoft.Xna.Framework.Input.Keys.PageUp | Microsoft.Xna.Framework.Input.Keys.Pause | Microsoft.Xna.Framework.Input.Keys.Print | Microsoft.Xna.Framework.Input.Keys.PrintScreen | Microsoft.Xna.Framework.Input.Keys.Right | Microsoft.Xna.Framework.Input.Keys.Select | Microsoft.Xna.Framework.Input.Keys.Space | Microsoft.Xna.Framework.Input.Keys.Tab | Microsoft.Xna.Framework.Input.Keys.Up:
				//case 64:  wtf?
				case Microsoft.Xna.Framework.Input.Keys.LeftWindows:
				case Microsoft.Xna.Framework.Input.Keys.RightWindows:
				case Microsoft.Xna.Framework.Input.Keys.Apps:
				case Microsoft.Xna.Framework.Input.Keys.B | Microsoft.Xna.Framework.Input.Keys.Back | Microsoft.Xna.Framework.Input.Keys.CapsLock | Microsoft.Xna.Framework.Input.Keys.D | Microsoft.Xna.Framework.Input.Keys.F | Microsoft.Xna.Framework.Input.Keys.H | Microsoft.Xna.Framework.Input.Keys.ImeConvert | Microsoft.Xna.Framework.Input.Keys.J | Microsoft.Xna.Framework.Input.Keys.L | Microsoft.Xna.Framework.Input.Keys.N | Microsoft.Xna.Framework.Input.Keys.P | Microsoft.Xna.Framework.Input.Keys.R | Microsoft.Xna.Framework.Input.Keys.RightWindows | Microsoft.Xna.Framework.Input.Keys.T | Microsoft.Xna.Framework.Input.Keys.V | Microsoft.Xna.Framework.Input.Keys.X | Microsoft.Xna.Framework.Input.Keys.Z:
				case Microsoft.Xna.Framework.Input.Keys.Sleep:
				{
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.D0:
				{
					newChar = string.Concat(newChar, "0");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.D1:
				{
					newChar = string.Concat(newChar, "1");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.D2:
				{
					newChar = string.Concat(newChar, "2");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.D3:
				{
					newChar = string.Concat(newChar, "3");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.D4:
				{
					newChar = string.Concat(newChar, "4");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.D5:
				{
					newChar = string.Concat(newChar, "5");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.D6:
				{
					newChar = string.Concat(newChar, "6");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.D7:
				{
					newChar = string.Concat(newChar, "7");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.D8:
				{
					newChar = string.Concat(newChar, "8");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.D9:
				{
					newChar = string.Concat(newChar, "9");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.A:
				{
					newChar = string.Concat(newChar, "a");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.B:
				{
					newChar = string.Concat(newChar, "b");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.C:
				{
					newChar = string.Concat(newChar, "c");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.D:
				{
					newChar = string.Concat(newChar, "d");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.E:
				{
					newChar = string.Concat(newChar, "e");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.F:
				{
					newChar = string.Concat(newChar, "f");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.G:
				{
					newChar = string.Concat(newChar, "g");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.H:
				{
					newChar = string.Concat(newChar, "h");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.I:
				{
					newChar = string.Concat(newChar, "i");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.J:
				{
					newChar = string.Concat(newChar, "j");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.K:
				{
					newChar = string.Concat(newChar, "k");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.L:
				{
					newChar = string.Concat(newChar, "l");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.M:
				{
					newChar = string.Concat(newChar, "m");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.N:
				{
					newChar = string.Concat(newChar, "n");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.O:
				{
					newChar = string.Concat(newChar, "o");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.P:
				{
					newChar = string.Concat(newChar, "p");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.Q:
				{
					newChar = string.Concat(newChar, "q");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.R:
				{
					newChar = string.Concat(newChar, "r");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.S:
				{
					newChar = string.Concat(newChar, "s");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.T:
				{
					newChar = string.Concat(newChar, "t");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.U:
				{
					newChar = string.Concat(newChar, "u");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.V:
				{
					newChar = string.Concat(newChar, "v");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.W:
				{
					newChar = string.Concat(newChar, "w");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.X:
				{
					newChar = string.Concat(newChar, "x");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.Y:
				{
					newChar = string.Concat(newChar, "y");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.Z:
				{
					newChar = string.Concat(newChar, "z");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.NumPad0:
				{
					newChar = string.Concat(newChar, "0");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.NumPad1:
				{
					newChar = string.Concat(newChar, "1");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.NumPad2:
				{
					newChar = string.Concat(newChar, "2");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.NumPad3:
				{
					newChar = string.Concat(newChar, "3");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.NumPad4:
				{
					newChar = string.Concat(newChar, "4");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.NumPad5:
				{
					newChar = string.Concat(newChar, "5");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.NumPad6:
				{
					newChar = string.Concat(newChar, "6");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.NumPad7:
				{
					newChar = string.Concat(newChar, "7");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.NumPad8:
				{
					newChar = string.Concat(newChar, "8");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				case Microsoft.Xna.Framework.Input.Keys.NumPad9:
				{
					newChar = string.Concat(newChar, "9");
					if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
					{
						newChar = newChar.ToUpper();
					}
					text = string.Concat(text, newChar);
					return;
				}
				default:
				{
					if (key1 == Microsoft.Xna.Framework.Input.Keys.OemMinus)
					{
						newChar = string.Concat(newChar, "-");
						if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
						{
							newChar = newChar.ToUpper();
						}
						text = string.Concat(text, newChar);
						return;
					}
					else if (key1 == Microsoft.Xna.Framework.Input.Keys.OemQuotes)
					{
						newChar = string.Concat(newChar, "'");
						if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
						{
							newChar = newChar.ToUpper();
						}
						text = string.Concat(text, newChar);
						return;
					}
					else
					{
						if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || this.lastKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || Control.IsKeyLocked(System.Windows.Forms.Keys.Capital))
						{
							newChar = newChar.ToUpper();
						}
						text = string.Concat(text, newChar);
						return;
					}
				}
			}
		}

		private bool CheckKey(Microsoft.Xna.Framework.Input.Keys theKey)
		{
			if (theKey == Microsoft.Xna.Framework.Input.Keys.Back && this.boop == 0)
			{
				return this.currentKeyboardState.IsKeyDown(theKey);
			}
			if (!this.lastKeyboardState.IsKeyUp(theKey))
			{
				return false;
			}
			return this.currentKeyboardState.IsKeyDown(theKey);
		}

		public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
		{
			Primitives2D.DrawRectangle(spriteBatch, this.ClickableArea, Color.Orange);
			Vector2 cursor = new Vector2((float)(this.ClickableArea.X + 5), (float)(this.ClickableArea.Y + 2));
			spriteBatch.DrawString(Fonts.Arial12Bold, this.Text, cursor, Color.Orange);
			cursor.X = cursor.X + Fonts.Arial12Bold.MeasureString(this.Text).X;
			if (this.HandlingInput)
			{
				TimeSpan totalGameTime = gameTime.TotalGameTime;
				float f = (float)Math.Sin((double)totalGameTime.TotalSeconds);
				f = Math.Abs(f) * 255f;
				Color flashColor = new Color(255, 255, 255, (byte)f);
				spriteBatch.DrawString(Fonts.Arial12Bold, "|", cursor, flashColor);
			}
		}

		public void Draw(SpriteFont Font, SpriteBatch spriteBatch, Vector2 pos, GameTime gameTime, Color c)
		{
			spriteBatch.DrawString(Font, this.Text, pos, c);
			pos.X = pos.X + Font.MeasureString(this.Text).X;
			if (this.HandlingInput)
			{
				TimeSpan totalGameTime = gameTime.TotalGameTime;
				float f = (float)Math.Sin((double)totalGameTime.TotalSeconds);
				f = Math.Abs(f) * 255f;
				Color flashColor = new Color(255, 255, 255, (byte)f);
				spriteBatch.DrawString(Font, "|", pos, flashColor);
			}
		}

		public void HandleTextInput(ref string text, InputState input)
		{
			this.currentKeyboardState = Keyboard.GetState();
			Microsoft.Xna.Framework.Input.Keys[] keysArray = this.keysToCheck;
			for (int i = 0; i < (int)keysArray.Length; i++)
			{
				Microsoft.Xna.Framework.Input.Keys key = keysArray[i];
				if (this.CheckKey(key))
				{
					if (text.Length >= this.MaxCharacters)
					{
						AudioManager.PlayCue("UI_Misc20");
					}
					else
					{
						this.AddKeyToText(ref text, key);
						AudioManager.PlayCue("blip_click");
						break;
					}
				}
			}
			if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Enter) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
			{
				this.HandlingInput = false;
			}
			if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Back) && this.boop == 0 && text.Length != 0)
			{
				text = text.Remove(text.Length - 1);
			}
			this.lastKeyboardState = this.currentKeyboardState;
			UITextEntry uITextEntry = this;
			uITextEntry.boop = uITextEntry.boop + 1;
			if (this.boop == 7)
			{
				this.boop = 0;
			}
		}

		public void HandleTextInput(ref string text)
		{
			this.currentKeyboardState = Keyboard.GetState();
			if (!HelperFunctions.CheckIntersection(this.ClickableArea, new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y)) && this.HandlingInput && Mouse.GetState().LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
			{
				this.HandlingInput = false;
			}
			Microsoft.Xna.Framework.Input.Keys[] keysArray = this.keysToCheck;
			int num = 0;
			while (num < (int)keysArray.Length)
			{
				Microsoft.Xna.Framework.Input.Keys key = keysArray[num];
				if (!this.CheckKey(key))
				{
					num++;
				}
				else
				{
					this.AddKeyToText(ref text, key);
					AudioManager.PlayCue("blip_click");
					break;
				}
			}
			if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Enter) || this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
			{
				this.HandlingInput = false;
			}
			if (this.currentKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Back) && this.boop == 0 && text.Length != 0)
			{
				text = text.Remove(text.Length - 1);
			}
			this.lastKeyboardState = this.currentKeyboardState;
			UITextEntry uITextEntry = this;
			uITextEntry.boop = uITextEntry.boop + 1;
			if (this.boop == 7)
			{
				this.boop = 0;
			}
		}
	}
}