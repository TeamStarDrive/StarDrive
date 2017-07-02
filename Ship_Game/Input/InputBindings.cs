using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public enum MouseButton
    {
        Left,
        Right,
        Middle,
        XButton1,
        XButton2,
    }

    /// <summary>
    /// Allows for generalizing input binding for a more intricate system
    /// with actual input customization.
    /// </summary>
    public class InputBindings
    {
        private enum TriggerState : byte
        {
            OnDown,  // was up, but now is pressed down
            OnHeld,  // continuous event type: while was down before and is down now
            OnClick, // down and then released. the event is triggered on release (!)
        }

        private struct TriggerCondition
        {
            public byte Ctrl; // 1: requires this modifier key, 0: not using this modifier
            public byte Alt;
            public byte Shift;
            public TriggerState When;

            public bool CheckModifiers(ref KeyboardState kb)
            {
                return (Ctrl  == 0 || kb.IsKeyDown(Keys.LeftControl) || kb.IsKeyDown(Keys.RightControl))
                    && (Alt   == 0 || kb.IsKeyDown(Keys.LeftAlt)     || kb.IsKeyDown(Keys.RightAlt))
                    && (Shift == 0 || kb.IsKeyDown(Keys.LeftShift)   || kb.IsKeyDown(Keys.RightShift));
            }

            public bool Triggered(ref KeyboardState before, ref KeyboardState now, Keys key)
            {
                switch (When)
                {
                    default:
                    case TriggerState.OnDown:  return before.IsKeyUp(key)   && now.IsKeyDown(key);
                    case TriggerState.OnHeld:  return before.IsKeyDown(key) && now.IsKeyDown(key);
                    case TriggerState.OnClick: return before.IsKeyDown(key) && now.IsKeyUp(key);
                }
            }

            private bool Triggered(ButtonState before, ButtonState now)
            {
                switch (When)
                {
                    default:
                    case TriggerState.OnDown:  return before == ButtonState.Released && now == ButtonState.Pressed;
                    case TriggerState.OnHeld:  return before == ButtonState.Pressed  && now == ButtonState.Pressed;
                    case TriggerState.OnClick: return before == ButtonState.Pressed  && now == ButtonState.Released;
                }
            }

            public bool Triggered(ref MouseState before, ref MouseState now, MouseButton button)
            {
                switch (button)
                {
                    default:
                    case MouseButton.Left:     return Triggered(before.LeftButton,   now.LeftButton);
                    case MouseButton.Right:    return Triggered(before.RightButton,  now.RightButton);
                    case MouseButton.Middle:   return Triggered(before.MiddleButton, now.MiddleButton);
                    case MouseButton.XButton1: return Triggered(before.XButton1,     now.XButton1);
                    case MouseButton.XButton2: return Triggered(before.XButton2,     now.XButton2);
                }
            }

            public bool Triggered(ref GamePadState before, ref GamePadState now, Buttons button)
            {
                switch (When)
                {
                    default:
                    case TriggerState.OnDown:  return before.IsButtonUp(button)   && now.IsButtonDown(button);
                    case TriggerState.OnHeld:  return before.IsButtonDown(button) && now.IsButtonDown(button);
                    case TriggerState.OnClick: return before.IsButtonDown(button) && now.IsButtonUp(button);
                }
            }
        }

        private interface IBinding
        {
            bool Triggered(InputState input);
        }

        private struct KeyBinding : IBinding
        {
            public Keys First;
            public Keys Second;
            public TriggerCondition Condition;
            public bool Triggered(InputState input)
            {
                return Condition.CheckModifiers(ref input.KeysCurr)
                    && (First  == 0 || Condition.Triggered(ref input.KeysPrev, ref input.KeysCurr, First))
                    && (Second == 0 || Condition.Triggered(ref input.KeysPrev, ref input.KeysCurr, Second));
            }
        }

        private struct MouseBinding : IBinding
        {
            public MouseButton Button;
            public TriggerCondition Condition;

            public bool Triggered(InputState input)
            {
                return Condition.CheckModifiers(ref input.KeysCurr)
                       && (Condition.Triggered(ref input.MousePrev, ref input.MouseCurr, Button));
            }
        }

        private struct GamepadBinding : IBinding
        {
            public Buttons First;
            public Buttons Second;
            public TriggerCondition Condition;
            public bool Triggered(InputState input)
            {
                return Condition.CheckModifiers(ref input.KeysCurr)
                       && (Condition.Triggered(ref input.GamepadPrev, ref input.GamepadCurr, First))
                       && (Second == 0 || Condition.Triggered(ref input.GamepadPrev, ref input.GamepadCurr, Second));
            }
        }
    }
}
