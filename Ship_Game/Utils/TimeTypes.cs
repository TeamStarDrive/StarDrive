using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    /// <summary>
    /// Time type which describes FIXED tick time used in simulations.
    /// This is a CONSTANT time step for our physics simulations
    ///
    /// Most common values:  1/60  0.0  1.0
    /// </summary>
    public readonly struct FixedSimTime
    {
        /// <summary>
        /// Time step in seconds, eg 0.016666667   0.0   1.0
        /// </summary>
        public readonly float FixedTime;

        /// <summary>
        /// Used when game is paused or loading, allows us to go through
        /// all game updates without moving the universe
        /// </summary>
        public static readonly FixedSimTime Zero = new FixedSimTime(0f);

        /// <summary>
        /// Used for specific updates which only update once per second
        /// </summary>
        public static readonly FixedSimTime One = new FixedSimTime(1f);

        /// <summary>
        /// This is the default game simulation time, 60 ticks per second
        /// </summary>
        public static readonly FixedSimTime Default = new FixedSimTime(1f / 60f);

        public FixedSimTime(float time)
        {
            FixedTime = time;
        }
    }

    /// <summary>
    /// Time type which contains variable frame delta time used for drawing.
    /// This elapsed time can have huge differences and is NOT suitable for simulations
    /// </summary>
    public struct VariableFrameTime
    {
        /// <summary>
        /// Delta time in seconds, eg 0.0015763
        /// </summary>
        public float VariableTime;

        public VariableFrameTime(float time)
        {
            VariableTime = time;
        }
    }
}
