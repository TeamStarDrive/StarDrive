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
        /// 
        /// </summary>
        public readonly int SimulationFPS;

        /// <summary>
        /// This is the fixed simulation step: 1.0/SimulationFPS
        ///
        /// By default it is 1 / 60, but players can configure it
        ///
        /// If the game is paused, this will be 0
        /// </summary>
        public readonly FixedSimTime SimulationStep;

        /// <summary>
        /// This is the real time elapsed between frames
        ///
        /// It can vary greatly depending on how many things are being drawn
        /// </summary>
        public readonly VariableFrameTime RealTime;

        /// <summary>
        /// XNA game time for compatibility
        /// </summary>
        public readonly GameTime XnaTime;

        /// <summary>
        /// Total elapsed game time, from the start of the game engine, until this time point
        /// </summary>
        public readonly float TotalGameSeconds;

        public UpdateTimes(int simulationFramesPerSecond, GameTime xnaTime)
        {
            SimulationFPS = simulationFramesPerSecond;
            float simulationFixedTimeStep = 1f / simulationFramesPerSecond;

            SimulationStep = new FixedSimTime(simulationFixedTimeStep);
            XnaTime = xnaTime;

            float frameTime = (float)xnaTime.ElapsedGameTime.TotalSeconds;
            if (frameTime > 0.4f) // @note Probably we were loading something heavy
                frameTime = simulationFixedTimeStep;

            RealTime = new VariableFrameTime(frameTime);

            TotalGameSeconds = (float)xnaTime.TotalGameTime.TotalSeconds;
        }
    }

    /// <summary>
    /// This time type is used purely for rendering
    /// </summary>
    public class DrawTimes
    {
        /// <summary>
        /// XNA game time for compatibility
        /// </summary>
        public GameTime XnaTime { get; private set; }

        /// <summary>
        /// TimeSinceLastDrawEvent
        /// 
        /// Variable real time that has passed since last draw event
        /// </summary>
        public VariableFrameTime RealTime { get; private set; }

        PerfTimer Timer;

        public DrawTimes()
        {
        }

        /// <summary>
        /// Update the internal timer before rendering
        /// </summary>
        public void UpdateBeforeRendering(GameTime xnaTime)
        {
            XnaTime = xnaTime;

            if (Timer == null)
            {
                Timer = new PerfTimer();
            }

            RealTime = new VariableFrameTime(Timer.Elapsed);
        }
    }
}
