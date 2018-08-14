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
        public float MaxTime  { get; private set; }
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

            AvgTime = (AvgTime*NumSamples + elapsed) / (NumSamples + 1);

            // Well... this is kinda complicated to do without a list indeed
            if (elapsed > MaxTime) MaxTime  = elapsed;
            else                   MaxTime *= 0.98f; // trickle down towards avg time

            ++NumSamples;
        }

        public override string ToString()
        {
            return $"{AvgTime*1000f:0.0,5}ms ({MaxTime*1000f:0.0,5}ms)";
        }
    }
}
