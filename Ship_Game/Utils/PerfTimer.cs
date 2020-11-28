using System;
using System.Runtime.InteropServices;

namespace Ship_Game
{
    /// <summary>
    /// Lean and Mean high performance timer
    /// Start() and measure .Elapsed time in fractional seconds
    /// </summary>
    public class PerfTimer
    {
        [DllImport("Kernel32.dll")]
        static extern bool QueryPerformanceCounter(out long perfcount);

        [DllImport("Kernel32.dll")]
        static extern bool QueryPerformanceFrequency(out long freq);

        static long Frequency;
        long Time;

        /// <summary>
        /// Create new instance and Start the timer
        /// </summary>
        public PerfTimer()
        {
            if (Frequency == 0)
            {
                QueryPerformanceFrequency(out Frequency);
            }
            QueryPerformanceCounter(out Time);
        }

        /// <summary>
        /// Reset and Restart the perf timer
        /// </summary>
        public void Start()
        {
            QueryPerformanceCounter(out Time);
        }

        /// <summary>
        /// Get intermediate sampling value (in seconds)
        /// </summary>
        public float Elapsed
        {
            get
            {
                QueryPerformanceCounter(out long end);
                return (float)((double)(end - Time) / Frequency);
            }
        }

        public override string ToString()
        {
            return $"{Elapsed*1000f,5:0.0}ms";
        }
    }

    /// <summary>
    /// Perf timer which aggregates elapsed time 
    /// </summary>
    public class AggregatePerfTimer
    {
        [DllImport("Kernel32.dll")]
        static extern bool QueryPerformanceCounter(out long perfcount);

        [DllImport("Kernel32.dll")]
        static extern bool QueryPerformanceFrequency(out long freq);

        static long Frequency;
        long Time;

        float CurrentTotal;
        float CurrentMax;
        int CurrentSamples;

        public float MeasuredTotal { get; private set; }
        float MeasuredMax;
        public int MeasuredSamples { get; private set; }
        public float AvgTime { get; private set; }

        readonly long StatRefreshInterval;
        long NextRefreshTime;

        public AggregatePerfTimer(float statRefreshInterval = 1f/*refresh once per second*/)
        {
            if (Frequency == 0)
            {
                QueryPerformanceFrequency(out Frequency);
            }
            StatRefreshInterval = (long)(statRefreshInterval * Frequency);
        }

        // start new sampling
        public void Start()
        {
            QueryPerformanceCounter(out Time);
            if (NextRefreshTime == 0)
                NextRefreshTime = Time + StatRefreshInterval;
        }

        // stop and accumulate performance sample
        public void Stop()
        {
            QueryPerformanceCounter(out long now);
            float elapsed = (float)((double)(now - Time) / Frequency);
            CurrentMax = Math.Max(CurrentMax, elapsed);
            CurrentTotal += elapsed;
            ++CurrentSamples;

            if (now >= NextRefreshTime)
            {
                while (now >= NextRefreshTime)
                    NextRefreshTime += StatRefreshInterval;

                MeasuredTotal = CurrentTotal;
                MeasuredMax = CurrentMax;
                MeasuredSamples = CurrentSamples;
                AvgTime = MeasuredTotal / MeasuredSamples;

                CurrentTotal = 0f;
                CurrentMax = 0f;
                CurrentSamples = 0;
            }
        }

        public override string ToString()
        {
            float avg = MeasuredTotal / MeasuredSamples;
            return $"{avg*1000,4:0.0}ms   max {MeasuredMax*1000f,4:0.0}ms   all {MeasuredTotal*1000f,4:0}ms";
        }

        public string String(AggregatePerfTimer total)
        {
            bool isZero = total.MeasuredTotal.AlmostZero();
            int percent = isZero ? 0 : (int)((MeasuredTotal / total.MeasuredTotal) * 100);
            return $"{this}  {percent,3}%";
        }
    }
}
