using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public sealed partial class InputState
    {

        // Mouse Timers
        public struct MouseHoldStatus
        {
            ButtonState Previous;
            public bool IsHolding  { get; set; }
            public bool WasHolding { get; set; }
            public float Time      { get; private set; }
            public float TimeStart { get; private set; }
            public float TimeEnd   { get; private set; }
            public Vector2 StartPos{ get; private set; }
            public Vector2 EndPos  { get; private set; }

            public void Update(UpdateTimes elapsed, ButtonState current, Vector2 cursorPos)
            {
                WasHolding = IsHolding;
                IsHolding = current == ButtonState.Pressed && Previous == ButtonState.Pressed;
                Previous = current;

                if (WasHolding && IsHolding) // continuous holding
                {
                    Time += elapsed.RealTime.Seconds;
                    EndPos = cursorPos;
                }
                else if (!WasHolding && IsHolding) // Hold started
                {
                    StartPos = cursorPos;
                    EndPos   = cursorPos;
                    Time = 0f;
                    TimeStart = elapsed.CurrentGameTime;
                }
                else if (WasHolding && !IsHolding) // Hold finished
                {
                    EndPos = cursorPos;
                    Time = 0f;
                    TimeEnd = elapsed.CurrentGameTime;
                }
            }
        }

        public MouseHoldStatus LeftHold   = new MouseHoldStatus();
        public MouseHoldStatus RightHold  = new MouseHoldStatus();
        public MouseHoldStatus MiddleHold = new MouseHoldStatus();

        public bool LeftMouseHeld(float heldForSeconds = 0.15f)
            => LeftHold.IsHolding && LeftHold.Time > heldForSeconds;

        public bool RightMouseHeld(float heldForSeconds = 0.15f)
            => RightHold.IsHolding && RightHold.Time > heldForSeconds;

        public bool MiddleMouseHeld(float heldForSeconds = 0.15f)
            => MiddleHold.IsHolding && MiddleHold.Time > heldForSeconds;

        public bool LeftMouseHeldDown  => LeftHold.IsHolding;
        public bool RightMouseHeldDown => RightHold.IsHolding;
        public bool LeftMouseHeldUp    => !LeftHold.IsHolding;
        public bool RightMouseHeldUp   => !RightHold.IsHolding;

        public bool LeftMouseWasHeldDown => LeftHold.IsHolding || LeftHold.WasHolding;
        public bool RightMouseWasHeldDown => RightHold.IsHolding || RightHold.WasHolding;

        public float LeftMouseHoldDuration       => LeftHold.TimeEnd - LeftHold.TimeStart;
        public float LeftMouseHoldTimeSinceStart => StarDriveGame.Instance.TotalElapsed - LeftHold.TimeStart;
        public float LeftMouseHoldTimeSinceEnd   => StarDriveGame.Instance.TotalElapsed - LeftHold.TimeEnd;

        public bool MouseDrag => LeftMouseHeldDown || RightMouseHeldDown;

        public Vector2 StartLeftHold  => LeftHold.StartPos;
        public Vector2 EndLeftHold    => LeftHold.EndPos;
        public Vector2 StartRightHold => RightHold.StartPos;
        public Vector2 EndRightHold   => RightHold.EndPos;


        void UpdateHolding(UpdateTimes elapsed)
        {
            LeftHold.Update(elapsed, MouseCurr.LeftButton, CursorPosition);
            RightHold.Update(elapsed, MouseCurr.RightButton, CursorPosition);
            MiddleHold.Update(elapsed, MouseCurr.MiddleButton, CursorPosition);
        }
    }
}
