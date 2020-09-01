using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Utils
{
    /// <summary>
    /// Time type which describes FIXED tick time used in simulations.
    /// This is a CONSTANT time step for our physics simulations
    /// </summary>
    public struct FixedSimTime
    {
        public double Time;
        public FixedSimTime(double time)
        {
            Time = time;
        }
    }

    /// <summary>
    /// Time type which contains variable frame delta time used for drawing.
    /// This elapsed time can have huge differences and is NOT suitable for simulations
    /// </summary>
    public struct VariableFrameTime
    {
        public double Time;
        public VariableFrameTime(double time)
        {
            Time = time;
        }
    }
}
