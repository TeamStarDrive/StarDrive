using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public sealed partial class InputState
    {
        public bool RightMouseDoubleClick { get; private set; }
        public bool LeftMouseDoubleClick  { get; private set; }

        struct DoubleClickTimer
        {
            const float TooSlowThreshold = 0.5f;
            bool FirstClick;
            float Timer;

            // @return TRUE if double click happened this frame
            public bool Update(UpdateTimes elapsed, bool wasClicked, bool mouseMoved)
            {
                if (mouseMoved)
                {
                    FirstClick = false;
                    return false;
                }
                if (!FirstClick) // wait for first click to happen
                {
                    Timer = 0f;
                    if (wasClicked)
                        FirstClick = true;
                    return false; // no double click yet
                }
                // if too much time elapsed, reset everything
                Timer += elapsed.RealTime.Seconds;
                if (Timer > TooSlowThreshold || wasClicked)
                {
                    FirstClick = false;
                    return wasClicked; // if we did a last minute doubleclick then return it
                }
                return false;
            }
        }

        DoubleClickTimer LeftDoubleClicker  = new DoubleClickTimer();
        DoubleClickTimer RightDoubleClicker = new DoubleClickTimer();

        void UpdateDoubleClick(UpdateTimes elapsed)
        {
            LeftMouseDoubleClick  = LeftDoubleClicker.Update(elapsed, LeftMouseClick, MouseMoved);
            RightMouseDoubleClick = RightDoubleClicker.Update(elapsed, RightMouseClick, MouseMoved);
        }
    }
}
