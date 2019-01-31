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
            public bool Holding { get; private set; }
            public float Time   { get; private set; }
            public Vector2 StartPos { get; private set; }
            public Vector2 EndPos   { get; private set; }

            public void Update(float elapsedTime, ButtonState current, Vector2 cursorPos)
            {
                bool wasHeld = Holding;
                Holding = current == ButtonState.Pressed && Previous == ButtonState.Pressed;
                Previous = current;

                if (wasHeld && Holding) // continuous holding
                {
                    Time += elapsedTime;
                    EndPos = cursorPos;
                }
                else if (!wasHeld && Holding) // Hold started
                {
                    StartPos = cursorPos;
                    EndPos   = cursorPos;
                    Time = 0f;
                }
                else if (wasHeld && !Holding) // Hold finished
                {
                    EndPos = cursorPos;
                }
            }
        }

        public MouseHoldStatus LeftHold = new MouseHoldStatus();
        public MouseHoldStatus RightHold = new MouseHoldStatus();

        public bool LeftMouseHeld(float heldForSeconds = 0.15f)
            => LeftHold.Holding && LeftHold.Time > heldForSeconds;

        public bool RightMouseHeld(float heldForSeconds = 0.15f)
            => RightHold.Holding && RightHold.Time > heldForSeconds;

        public bool LeftMouseHeldDown  => LeftHold.Holding;
        public bool RightMouseHeldDown => RightHold.Holding;
        public bool LeftMouseHeldUp    => !LeftHold.Holding;
        public bool RightMouseHeldUp   => !RightHold.Holding;

        public bool MouseDrag => LeftMouseHeldDown || RightMouseHeldDown;

        public Vector2 StartLeftHold  => LeftHold.StartPos;
        public Vector2 EndLeftHold    => LeftHold.EndPos;
        public Vector2 StartRightHold => RightHold.StartPos;
        public Vector2 EndRightHold   => RightHold.EndPos;


        void UpdateHolding(float elapsedTime)
        {
            LeftHold.Update(elapsedTime, MouseCurr.LeftButton, CursorPosition);
            RightHold.Update(elapsedTime, MouseCurr.RightButton, CursorPosition);
        }
    }
}
