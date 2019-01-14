using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.GameScreens.NewGame
{
    public class ProgressCounter
    {
        readonly Stopwatch Time = new Stopwatch();
        public int ElapsedMillis => (int)Time.Elapsed.TotalMilliseconds;

        // Sets the Maximum value for current step
        int Max = 1;
        int Value;

        // Advances progress Value by 1
        public void Advance() => Value += 1;

        public void Start(int max)
        {
            Max = max;
            Time.Start();
        }

        // Forces step to report 100% [1.0]
        public void Finish()
        {
            Value = Max;
            Steps = null;
            StepProportions = null;
            Time.Stop();
        }

        public float Percent
        {
            get
            {
                if (Steps == null)
                    return (float)Value.Clamped(0, Max) / Max;

                if (StepIndex >= Steps.Length)
                    return 1f;

                float total = 0f;
                for (int i = 0; i < Steps.Length; ++i)
                    total += Steps[i].Percent * StepProportions[i];

                return total;
            }
        }

        // SubStep related
        int StepIndex;
        ProgressCounter[] Steps;
        float[] StepProportions;

        public ProgressCounter this[int step] => Steps[step];
        public ProgressCounter NextStep() => Steps[StepIndex++];
        public int NumSteps => Steps?.Length ?? 0;

        public void DeclareSubSteps(params float[] proportions)
        {
            StepProportions = proportions;
            Steps = new ProgressCounter[proportions.Length];

            float total = 0f;
            for (int i = 0; i < proportions.Length; ++i)
            {
                total += proportions[i];
                Steps[i] = new ProgressCounter();
            }

            float rem = 1f - total;
            if (!rem.AlmostZero())
            {
                Log.Warning($"Declared steps don't add up to 1.0: {total}");
            }
        }
    }
}
