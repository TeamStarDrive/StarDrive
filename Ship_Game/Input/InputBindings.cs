using System;
using System.Collections.Generic;
using System.Text;
using SDGraphics.Input;
using XnaInput = Microsoft.Xna.Framework.Input;
using SDUtils;

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
    ///
    /// TODO: Finish this incomplete system
    /// </summary>
    public class InputBindings
    {
        public enum TriggerState : byte
        {
            OnDown,  // was up, but now is pressed down
            OnHeld,  // continuous event type: while was down before and is down now
            OnPress // down and then released. the event is triggered on release (!)
        }

        public struct TriggerCondition
        {
            public TriggerState When;
            public byte Ctrl; // 1: requires this modifier key, 0: not using this modifier
            public byte Alt;
            public byte Shift;

            public TriggerCondition(TriggerState when, bool ctrl, bool alt, bool shift)
            {
                When = when;
                Ctrl = (byte)(ctrl ? 1 : 0);
                Alt = (byte)(alt ? 1 : 0);
                Shift = (byte)(shift ? 1 : 0);
            }

            public bool HasModifiers => Ctrl != 0 || Alt != 0 || Shift != 0;

            public StringBuilder GetModifiers(StringBuilder sb)
            {
                if (!HasModifiers)
                    return sb;

                if (Ctrl != 0)
                    sb.Append("Ctrl");

                if (Alt != 0)
                {
                    if (Ctrl != 0)
                        sb.Append('+');
                    sb.Append("Alt");
                }

                if (Shift != 0)
                {
                    if (Ctrl != 0 || Alt != 0)
                        sb.Append('+');
                    sb.Append("Shift");
                }
                return sb;
            }

            static bool IsKeyUp(ref XnaInput.KeyboardState kb, Keys key)   => kb.IsKeyUp((XnaInput.Keys)key);
            static bool IsKeyDown(ref XnaInput.KeyboardState kb, Keys key) => kb.IsKeyDown((XnaInput.Keys)key);

            public bool CheckModifiers(ref XnaInput.KeyboardState kb)
            {
                return (Ctrl  == 0 || IsKeyDown(ref kb, Keys.LeftControl) || IsKeyDown(ref kb, Keys.RightControl))
                    && (Alt   == 0 || IsKeyDown(ref kb, Keys.LeftAlt)     || IsKeyDown(ref kb, Keys.RightAlt))
                    && (Shift == 0 || IsKeyDown(ref kb, Keys.LeftShift)   || IsKeyDown(ref kb, Keys.RightShift));
            }

            public bool Triggered(ref XnaInput.KeyboardState before, ref XnaInput.KeyboardState now, Keys key)
            {
                switch (When)
                {
                    default:
                    case TriggerState.OnDown:  return IsKeyUp(ref before, key)   && IsKeyDown(ref now, key);
                    case TriggerState.OnHeld:  return IsKeyDown(ref before, key) && IsKeyDown(ref now, key);
                    case TriggerState.OnPress: return IsKeyDown(ref before, key) && IsKeyUp(ref now, key);
                }
            }

            private bool Triggered(XnaInput.ButtonState before, XnaInput.ButtonState now)
            {
                switch (When)
                {
                    default:
                    case TriggerState.OnDown:  return before == XnaInput.ButtonState.Released && now == XnaInput.ButtonState.Pressed;
                    case TriggerState.OnHeld:  return before == XnaInput.ButtonState.Pressed  && now == XnaInput.ButtonState.Pressed;
                    case TriggerState.OnPress: return before == XnaInput.ButtonState.Pressed  && now == XnaInput.ButtonState.Released;
                }
            }

            public bool Triggered(ref XnaInput.MouseState before, ref XnaInput.MouseState now, MouseButton button)
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

            public bool Triggered(ref XnaInput.GamePadState before, ref XnaInput.GamePadState now, XnaInput.Buttons button)
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
            // Enum Identifier of the input event id
            Enum EventId { get; }
            // string which describes this binding Hotkey
            string Hotkey { get; }
            // true if binding condition has triggered
            bool IsTriggered(InputState input);
        }

        public class KeyBinding : IBinding
        {
            public Keys First;
            public Keys Second;
            public TriggerCondition Condition;
            public Enum EventId { get; set; }
            string TheHotkey;

            public string Hotkey
            {
                get
                {
                    if (TheHotkey == null)
                    {
                        var sb = new StringBuilder();
                        switch (Condition.When)
                        {
                            case TriggerState.OnDown: break;
                            case TriggerState.OnHeld: sb.Append("Hold "); break;
                            case TriggerState.OnPress: break;
                        }
                        if (Condition.HasModifiers)
                        {
                            Condition.GetModifiers(sb);
                            if (First != Keys.None)
                                sb.Append('+');
                        }
                        if (First != Keys.None)
                        {
                            sb.Append(First.ToString());
                        }
                        if (Second != Keys.None)
                        {
                            sb.Append('+');
                            sb.Append(Second.ToString());
                        }
                        TheHotkey = sb.ToString();
                    }
                    return TheHotkey;
                }
            }

            public bool IsTriggered(InputState input)
            {
                return Condition.CheckModifiers(ref input.KeysCurr)
                    && (First  == 0 || Condition.Triggered(ref input.KeysPrev, ref input.KeysCurr, First))
                    && (Second == 0 || Condition.Triggered(ref input.KeysPrev, ref input.KeysCurr, Second));
            }
        }

        public class MouseBinding : IBinding
        {
            public MouseButton Button;
            public TriggerCondition Condition;
            public Enum EventId { get; set; }
            string TheHotkey;

            public string Hotkey
            {
                get
                {
                    if (TheHotkey == null)
                    {
                        var sb = new StringBuilder();
                        switch (Condition.When)
                        {
                            case TriggerState.OnDown: break;
                            case TriggerState.OnHeld: sb.Append("Hold "); break;
                            case TriggerState.OnPress: break;
                        }
                        if (Condition.HasModifiers)
                        {
                            Condition.GetModifiers(sb).Append('+');
                        }
                        sb.Append(Button.ToString());
                        TheHotkey = sb.ToString();
                    }
                    return TheHotkey;
                }
            }

            public bool IsTriggered(InputState input)
            {
                return Condition.CheckModifiers(ref input.KeysCurr)
                       && (Condition.Triggered(ref input.MousePrev, ref input.MouseCurr, Button));
            }
        }

        public class GamepadBinding : IBinding
        {
            public Buttons First;
            public Buttons Second;
            public TriggerCondition Condition;
            public Enum EventId { get; set; }
            string TheHotkey;

            public string Hotkey
            {
                get
                {
                    if (TheHotkey == null)
                    {
                        var sb = new StringBuilder();
                        switch (Condition.When)
                        {
                            case TriggerState.OnDown:  break;
                            case TriggerState.OnHeld:  sb.Append("Hold "); break;
                            case TriggerState.OnPress: break;
                        }
                        sb.Append(First.ToString());
                        if (Second != 0)
                        {
                            sb.Append('+');
                            sb.Append(Second.ToString());
                        }
                        TheHotkey = sb.ToString();
                    }
                    return TheHotkey;
                }
            }

            public bool IsTriggered(InputState input)
            {
                return Condition.CheckModifiers(ref input.KeysCurr)
                       && (Condition.Triggered(ref input.GamepadPrev, ref input.GamepadCurr, (XnaInput.Buttons)First))
                       && (Second == 0 || Condition.Triggered(ref input.GamepadPrev, ref input.GamepadCurr, (XnaInput.Buttons)Second));
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

        public static IBinding ParsePair(string line)
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

        static bool TryParse<TEnum>(string input, out Enum value) where TEnum : struct
        {
            if (Enum.TryParse(input, true, out TEnum result))
            {
                value = result as Enum;
                return true;
            }
            value = default;
            return false;
        }

        private static bool TryParseInput(string inputChain, string input, out Enum value)
        {
            if (input.StartsWith("Keys.", StringComparison.OrdinalIgnoreCase)
                && TryParse<Keys>(input, out value))
                return true;
            if (input.StartsWith("MouseButton.", StringComparison.OrdinalIgnoreCase)
                && TryParse<MouseButton>(input, out value))
                return true;
            if (input.StartsWith("Buttons.", StringComparison.OrdinalIgnoreCase)
                && TryParse<Buttons>(input, out value))
                return true;
            if (TryParse<Keys>(input, out value) ||
                TryParse<MouseButton>(input, out value) ||
                TryParse<Buttons>(input, out value))
                return true;
            InvalidSyntax(inputChain, "Unrecognized input "+input);
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
                    EventId = inputEvent
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
                    EventId = inputEvent
                };
                return mb;
            }

            if (first is Buttons b)
            {
                var gb = new GamepadBinding
                {
                    First      = b,
                    Condition  = c,
                    EventId = inputEvent
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

        public static IBinding Parse(Enum inputEvent, string inputChain)
        {
            var chainKeys = new Array<string>(inputChain.Split(BindingChainSep));
            var c = new TriggerCondition();
            c.When = TriggerState.OnPress; // this is the desired default for most cases

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

        /// <summary>
        /// Parses an input string combination to create an input binding object
        /// Ex: "Ctrl+F1", "A", "Shift+Right", "Ctrl + Alt + MouseButtons.Right"
        /// </summary>
        public static IBinding FromString(string inputCombination)
        {
            return Parse(null, inputCombination);
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
                if (eventBinding.Value.IsTriggered(input))
                {
                    Action action = Actions[eventBinding.Key];
                    action();
                }
            }
        }

        /// Non-Global interface, to be used in UIElementV2-s
        /// Makes a new key-binding with modifier conditions
        /// Default requirement is OnPress which is similar to how Clicks work
        /// but can be changed to OnDown or OnHeld
        public static KeyBinding MakeBinding(
            TriggerState when = TriggerState.OnPress,
            Keys firstKey = Keys.None,
            Keys secondKey = Keys.None,
            bool ctrl = false,
            bool alt = false,
            bool shift = false)
        {
            return new KeyBinding
            {
                First = firstKey,
                Second = secondKey,
                Condition = new TriggerCondition(when, ctrl, alt, shift),
            };
        }
    }
}
