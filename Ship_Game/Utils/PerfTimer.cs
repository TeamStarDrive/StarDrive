using System;
using System.Runtime.InteropServices;

namespace Ship_Game
{
    public class PerfTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long perfcount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long freq);

        private static readonly long Frequency;
        private long Time;
        
        public float AvgTime  { get; private set; }
        public float MaxTime  { get; private set;}
        public int NumSamples { get; private set; }

        static PerfTimer()
        {
            QueryPerformanceFrequency(out Frequency);
        }

        public static PerfTimer StartNew()
        {
            var t = new PerfTimer();
            t.Start();
            return t;
        }

        // start perf timer
        public void Start()
        {
            QueryPerformanceCounter(out Time);
        }

        // Get intermediate sampling value that isn't stored
        // (in seconds)
        public float Elapsed
        {
            get
            {
                QueryPerformanceCounter(out long end);
                return (float)((double)(end - Time) / Frequency);
            }
        }
        public float ElapsedMillis => Elapsed * 1000f;

        // stop and gather performance sample
        public void Stop()
        {
            QueryPerformanceCounter(out long end);
            float elapsed = (float)((double)(end - Time) / Frequency);

            const float AVERAGE_RATIO = 0.025f;
            AvgTime = AvgTime*(1f-AVERAGE_RATIO) + elapsed*AVERAGE_RATIO;
            MaxTime = Math.Max(elapsed, MaxTime)*(1f-AVERAGE_RATIO) + MaxTime*AVERAGE_RATIO;

            ++NumSamples;
        }

        public override string ToString()
        {
            return $"{AvgTime*1000f:0.0,5}ms  ( {MaxTime*1000f:0.0,5}ms )";
        }
    }
}
