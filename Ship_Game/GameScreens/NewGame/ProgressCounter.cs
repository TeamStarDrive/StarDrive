using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        ProgressCounter[] Steps;
        float[] Proportions;

        public void Start(int max)
        {
            Max = max;
            Time.Start();
        }
        
        // Forces step to report 100% [1.0] and stops the timer
        public void Finish()
        {
            Index = Max;
            Time.Stop();
        }

        public void Start(params float[] proportions)
        {
            var steps = new ProgressCounter[proportions.Length];
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

            Start(proportions.Length);
            Proportions = proportions;
            Steps = steps; // @note Setting this at the; sort of thread unsafe... 
        }
        
        // Advances progress Value by 1
        public void Advance() => Index += 1;

        // Finish current step (if any) and returns next step
        // You must then start the next step at your own discretion
        public ProgressCounter AdvanceStep()
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
