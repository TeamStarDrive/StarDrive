using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Ship_Game;

namespace UnitTests.UI
{
    public class MockInputProvider : IInputProvider
    {
        public Vector2 MousePos = new Vector2(512, 512);
        public ButtonState LeftMouse = ButtonState.Released;
        public ButtonState RightMouse = ButtonState.Released;

        public Array<Keys> KeysDown = new Array<Keys>();

        public MouseState GetMouse()
        {
            return new MouseState((int)MousePos.X, (int)MousePos.Y, 0, LeftMouse,
                ButtonState.Released, RightMouse, ButtonState.Released, ButtonState.Released);
        }

        public KeyboardState GetKeyboard()
        {
            return new KeyboardState(KeysDown.ToArray());
        }

        public GamePadState GetGamePad()
        {
            return new GamePadState();
        }
    }
}
