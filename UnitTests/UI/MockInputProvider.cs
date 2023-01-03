using Ship_Game;
using SDGraphics;
using SDUtils;
using SDGraphics.Input;
using XnaInput = Microsoft.Xna.Framework.Input;

namespace UnitTests.UI
{
    public class MockInputProvider : IInputProvider
    {
        public Vector2 MousePos = new(512, 512);
        public ButtonState LeftMouse = ButtonState.Released;
        public ButtonState RightMouse = ButtonState.Released;

        public Array<Keys> KeysDown = new();

        public XnaInput.MouseState GetMouse()
        {
            return new XnaInput.MouseState((int)MousePos.X, (int)MousePos.Y, 0,
                (XnaInput.ButtonState)LeftMouse, XnaInput.ButtonState.Released, 
                (XnaInput.ButtonState)RightMouse, XnaInput.ButtonState.Released, XnaInput.ButtonState.Released);
        }

        public XnaInput.KeyboardState GetKeyboard()
        {
            return new XnaInput.KeyboardState(KeysDown.Select(key => (XnaInput.Keys)key));
        }

        public XnaInput.GamePadState GetGamePad()
        {
            return new XnaInput.GamePadState();
        }

        public void SetMouse(int x, int y)
        {
            MousePos = new(x, y);
        }
    }
}
