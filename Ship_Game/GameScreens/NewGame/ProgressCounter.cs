using System;
using System.Diagnostics;

namespace Ship_Game.GameScreens.NewGame
{
    /// <summary>
    /// This is a recursive progress counter which can report
    /// elapsed time for sub-steps and yields completion percent.
    ///
    /// 
    /// </summary>
    public class ProgressCounter
    {
        readonly Stopwatch Time = new Stopwatch();
        public int ElapsedMillis => (int)Time.Elapsed.TotalMilliseconds;

        int Index; // general progress index, also used for noting active Steps
        int Max = 1;
        float MaxSeconds;

        ProgressCounter[] Steps;
        float[] Proportions;

        public void Start(int max)
        {
            Max = Math.Max(1, max);
            Time.Start();
        }

        // Special case: progress is reported according to time instead
        public void StartTimeBased(float maxSeconds)
        {
            MaxSeconds = maxSeconds;
            Time.Start();
        }
        
        // Forces step to report 100% [1.0] and stops the timer
        public void Finish()
        {
            Index = Max;
            Time.Stop();
            if (Steps != null) // make sure all sub-steps also stop their Stopwatch
            {
                for (int i = 0; i < Steps.Length; ++i)
                    Steps[i].Finish();
            }
        }

        public void Start(params float[] proportions)
        {
            ProgressCounter[] steps = null;

            // If proportions.Length == 0, then equivalent to Start(max:1) 
            if (proportions.Length != 0)
            {
                steps = new ProgressCounter[proportions.Length];
                float total = 0f;
                for (int i = 0; i < proportions.Length; ++i)
                {
                    total += proportions[i];
                    steps[i] = new ProgressCounter();
                }

                float rem = 1f - total;
                if (!rem.AlmostZero())
                {
                    Log.Warning($"Declared steps don't add up to 1.0: {total}");
                }
                Proportions = proportions;
            }
            Start(proportions.Length);
            Steps = steps; // @note Setting this at the end... to get loose thread safety... 
        }
        
        // Advances progress Value by 1
        public void Advance() => Index += 1;

        // Finish current step (if any) and returns next step
        // You must then start the next step at your own discretion
        public ProgressCounter NextStep()
        {
            if (Index > 0)
                Steps[Index-1].Finish();
            return Steps[Index++];
        }

        // For later inspecting Step timings
        public ProgressCounter this[int index] => Steps[index];

        // Recursively calculates completion percent
        // @note Loose thread safety
        public float Percent
        {
            get
            {
                if (Index >= Max)
                    return 1f;

                if (MaxSeconds > 0f)
                    return ((float)Time.Elapsed.TotalSeconds / MaxSeconds).Clamped(0f, 1f);

                if (Steps == null)
                    return (float)Index.Clamped(0, Max) / Max;

                float total = 0f;
                for (int i = 0; i < Steps.Length; ++i)
                    total += Steps[i].Percent * Proportions[i];

                return total;
            }
        }

    }
}
