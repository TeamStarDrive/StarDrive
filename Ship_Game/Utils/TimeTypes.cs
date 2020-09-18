using Microsoft.Xna.Framework;
using System;

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

        public FixedSimTime(float time)
        {
            FixedTime = time;
        }

        public FixedSimTime(int simulationFramesPerSecond)
        {
            FixedTime = 1f / simulationFramesPerSecond;
        }
    }

    /// <summary>
    /// Time type which contains variable frame delta time used for drawing.
    /// This elapsed time can have huge differences and is NOT suitable for simulations
    /// </summary>
    public readonly struct VariableFrameTime
    {
        /// <summary>
        /// Delta time in seconds, eg 0.0015763
        /// This is the REAL time that has elapsed since last frame
        /// </summary>
        public readonly float Seconds;

        public VariableFrameTime(float time)
        {
            Seconds = time;
        }
    }

    /// <summary>
    /// Aggregate for passing game UPDATE times around the engine.
    /// WARNING: UPDATE time should note be used for RENDERING, use RenderTime for that!
    /// </summary>
    public class UpdateTimes
    {
        /// <summary>
        /// This is the time elapsed between Update calls
        /// </summary>
        public readonly VariableFrameTime RealTime;

        /// <summary>
        /// Total elapsed game time, from the start of the game engine, until this time point
        /// Except for when the window was inactive
        /// </summary>
        public readonly float CurrentGameTime;

        public UpdateTimes(float deltaTime, float currentGameTime)
        {
            RealTime = new VariableFrameTime(deltaTime);
            CurrentGameTime = currentGameTime;
        }

        public override string ToString()
        {
            return $"UpdateTimes  real:{RealTime.Seconds*1000,2:0.0}ms  game:{CurrentGameTime,2:0.0}s";
        }
    }

    /// <summary>
    /// This time type is used purely for rendering
    /// </summary>
    public class DrawTimes
    {
        /// <summary>
        /// TimeSinceLastDrawEvent
        /// 
        /// Variable real time that has passed since last draw event
        /// </summary>
        public VariableFrameTime RealTime { get; private set; }
        
        /// <summary>
        /// Total elapsed game time, from the start of the game engine, until this time point
        /// Except for when the window was inactive
        /// </summary>
        public float CurrentGameTime { get; private set; }

        public DrawTimes()
        {
        }

        /// <summary>
        /// Update the internal timer before rendering
        /// </summary>
        public void UpdateBeforeRendering(float currentGameTime)
        {
            if (CurrentGameTime == 0f)
            {
                CurrentGameTime = currentGameTime;
            }

            float elapsed = (currentGameTime - CurrentGameTime);
            CurrentGameTime = currentGameTime;
            RealTime = new VariableFrameTime(elapsed);
        }

        public override string ToString()
        {
            return $"DrawTimes  real:{RealTime.Seconds*1000,2:0.0}ms";
        }
    }
}
