using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public enum MouseButton
    {
        Left,
        Right,
        Middle,
        XButton1,
        XButton2
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
            OnPress // down and then released. the event is triggered on release (!)
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
                    case TriggerState.OnPress: return before.IsKeyDown(key) && now.IsKeyUp(key);
                }
            }

            private bool Triggered(ButtonState before, ButtonState now)
            {
                switch (When)
                {
                    default:
                    case TriggerState.OnDown:  return before == ButtonState.Released && now == ButtonState.Pressed;
                    case TriggerState.OnHeld:  return before == ButtonState.Pressed  && now == ButtonState.Pressed;
                    case TriggerState.OnPress: return before == ButtonState.Pressed  && now == ButtonState.Released;
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
                    case TriggerState.OnPress: return before.IsButtonDown(button) && now.IsButtonUp(button);
                }
            }
        }

        public interface IBinding
        {
            Enum InputEvent { get; }
            bool Triggered(InputState input);
        }

        private class KeyBinding : IBinding
        {
            public Keys First;
            public Keys Second;
            public TriggerCondition Condition;
            public Enum InputEvent { get; set; }
            public bool Triggered(InputState input)
            {
                return Condition.CheckModifiers(ref input.KeysCurr)
                    && (First  == 0 || Condition.Triggered(ref input.KeysPrev, ref input.KeysCurr, First))
                    && (Second == 0 || Condition.Triggered(ref input.KeysPrev, ref input.KeysCurr, Second));
            }
        }

        private class MouseBinding : IBinding
        {
            public MouseButton Button;
            public TriggerCondition Condition;
            public Enum InputEvent { get; set; }
            public bool Triggered(InputState input)
            {
                return Condition.CheckModifiers(ref input.KeysCurr)
                       && (Condition.Triggered(ref input.MousePrev, ref input.MouseCurr, Button));
            }
        }

        private class GamepadBinding : IBinding
        {
            public Buttons First;
            public Buttons Second;
            public TriggerCondition Condition;
            public Enum InputEvent { get; set; }
            public bool Triggered(InputState input)
            {
                return Condition.CheckModifiers(ref input.KeysCurr)
                       && (Condition.Triggered(ref input.GamepadPrev, ref input.GamepadCurr, First))
                       && (Second == 0 || Condition.Triggered(ref input.GamepadPrev, ref input.GamepadCurr, Second));
            }
        }

        private static readonly char[] BindingPairSep = {' ', '\t' };
        private static readonly char[] BindingChainSep = {' ', '\t', '+' };

        private static IBinding InvalidSyntax(string line, string what)
        {
            Log.Error($"Syntax error: '{what}' in: {line}\n"
                      +"Correct syntax expects: <EventEnum> [down|held|press] (<ctrl>+<alt>+<shift>+)<Key|Mouse|GamepadBinding>\n"
                      +"Example:   UniverseKeys.CheatMenu  press  Ctrl + Shift + Keys.Tab\n"
                      +"Example:   UniverseKeys.Fleet1     Keys.F1");
            return null;
        }

        public IBinding ParsePair(string line)
        {
            string[] bindingPair = line.Split(BindingChainSep, 2);
            if (bindingPair.Length < 2)
                return InvalidSyntax(line, "Empty binding");

            string eventString = bindingPair[0];
            string inputChain  = bindingPair[1]; // chain of keys and modifiers
            
            if (eventString.StartsWith("UniverseKeys.", StringComparison.OrdinalIgnoreCase))
            {
                if (Enum.TryParse(eventString, true, out UniverseKeys value))
                    return Parse(value, inputChain);
            }

            return InvalidSyntax(line, "Unrecognized event "+eventString);
        }

        private static bool TryParseInput(string inputChain, string input, out Enum value)
        {
            if (input.StartsWith("Keys.", StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse(input, true, out Keys k))
            {
                value = k;
                return true;
            }
            if (input.StartsWith("MouseButton.", StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse(input, true, out MouseButton mb))
            {
                value = mb;
                return true;
            }
            if (input.StartsWith("Buttons.", StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse(input, true, out Buttons b))
            {
                value = b;
                return true;
            }
            InvalidSyntax(inputChain, "Unrecognized input "+input);
            value = null;
            return false;
        }

        private static bool TryParseSecond(string inputChain, string input, Type type, out Enum value)
        {
            if (!TryParseInput(inputChain, input, out value))
                return false;
            if (value.GetType() == type)
                return true;
            InvalidSyntax(inputChain, "Expected "+type+" instead of "+value.GetType());
            return false;
        }

        private static IBinding ParseInputs(Enum inputEvent, TriggerCondition c, string inputChain, Array<string> chainKeys)
        {
            if (!TryParseInput(inputChain, chainKeys[0], out Enum first))
                return null;

            if (first is Keys k)
            {
                var kb = new KeyBinding
                {
                    First      = k,
                    Condition  = c,
                    InputEvent = inputEvent
                };
                if (chainKeys.Count > 1)
                {
                    if (!TryParseSecond(inputChain, chainKeys[1], typeof(Keys), out Enum second))
                        return null;
                    kb.Second = (Keys)second;
                }
                return kb;
            }

            if (first is MouseButton m)
            {
                var mb = new MouseBinding
                {
                    Button     = m,
                    Condition  = c,
                    InputEvent = inputEvent
                };
                return mb;
            }

            if (first is Buttons b)
            {
                var gb = new GamepadBinding
                {
                    First      = b,
                    Condition  = c,
                    InputEvent = inputEvent
                };
                if (chainKeys.Count > 1)
                {
                    if (!TryParseSecond(inputChain, chainKeys[1], typeof(Buttons), out Enum second))
                        return null;
                    gb.Second = (Buttons)second;
                }
                return gb;
            }

            return null;
        }

        public IBinding Parse(Enum inputEvent, string inputChain)
        {
            var chainKeys = new Array<string>(inputChain.Split(BindingChainSep));
            var c = new TriggerCondition();

            // consume all modifiers to build the trigger condition
            for (int i = 0; i < chainKeys.Count; ++i)
            {
                string key = chainKeys[i];
                if      (key.Equals("down",  StringComparison.OrdinalIgnoreCase)) c.When = TriggerState.OnDown;
                else if (key.Equals("held",  StringComparison.OrdinalIgnoreCase)) c.When = TriggerState.OnHeld;
                else if (key.Equals("press", StringComparison.OrdinalIgnoreCase)) c.When = TriggerState.OnPress;
                else if (key.Equals("Ctrl",  StringComparison.OrdinalIgnoreCase)) c.Ctrl  = 1;
                else if (key.Equals("Alt",   StringComparison.OrdinalIgnoreCase)) c.Alt   = 1;
                else if (key.Equals("Shift", StringComparison.OrdinalIgnoreCase)) c.Shift = 1;
                else continue;
                chainKeys.RemoveAtSwapLast(i);
            }

            if (chainKeys.IsEmpty)
                return InvalidSyntax(inputChain, "Input chain only contains modifiers. Use Keys.A or MouseButton.Left");

            if (chainKeys.Count > 2)
                return InvalidSyntax(inputChain, "Input chain contains too many inputs. Max allowed is 2.");

            return ParseInputs(inputEvent, c, inputChain, chainKeys);
        }



        private readonly Map<Enum, Action>   Actions  = new Map<Enum, Action>();
        private readonly Map<Enum, IBinding> Bindings = new Map<Enum, IBinding>();

        private void Bind(Enum inputEvent, IBinding binding, Action action)
        {
            Actions.Add(inputEvent, action);
            Bindings.Add(inputEvent, binding);
        }

        public void TriggerInputActions(InputState input)
        {
            foreach (KeyValuePair<Enum, IBinding> eventBinding in Bindings)
            {
                if (eventBinding.Value.Triggered(input))
                {
                    Action action = Actions[eventBinding.Key];
                    action();
                }
            }
        }
    }
}
