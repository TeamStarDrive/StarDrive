using System;
using System.Runtime.InteropServices;
using SDGraphics;

namespace Ship_Game
{
    /// <summary>
    /// Lean and Mean high performance timer
    /// Start() and measure .Elapsed time in fractional seconds
    /// </summary>
    public class PerfTimer
    {
        [DllImport("Kernel32.dll", EntryPoint = "QueryPerformanceCounter")]
        public static extern bool GetCurrentTicks(out long count);

        [DllImport("Kernel32.dll")]
        static extern bool QueryPerformanceFrequency(out long freq);

        public static long Frequency;
        public static double InvFrequency;

        long Time;

        public static void GetFrequency(out long freq, out double invFreq)
        {
            QueryPerformanceFrequency(out freq);
            invFreq = 1.0 / freq;
        }
        
        /// <summary>
        /// Create new instance and Start the timer
        /// </summary>
        public PerfTimer()
        {
            if (Frequency == 0)
                GetFrequency(out Frequency, out InvFrequency);
            GetCurrentTicks(out Time);
        }

        public PerfTimer(bool start)
        {
            if (Frequency == 0)
                GetFrequency(out Frequency, out InvFrequency);
            if (start)
                GetCurrentTicks(out Time);
        }

        /// <summary>
        /// Reset and Restart the perf timer
        /// </summary>
        public void Start()
        {
            GetCurrentTicks(out Time);
        }

        /// <summary>
        /// Get intermediate sampling value (in seconds)
        /// </summary>
        public float Elapsed
        {
            get
            {
                GetCurrentTicks(out long end);
                double elapsed = (end - Time) * InvFrequency;
                return (float)elapsed;
            }
        }

        /// <summary>
        /// Get currently elapsed value (in milliseconds)
        /// </summary>
        public float ElapsedMillis => Elapsed * 1000f;

        public override string ToString()
        {
            return $"{Elapsed*1000f,5:0.0}ms";
        }

        /// <summary>
        /// Converts seconds into time stamp ticks
        /// </summary>
        public static long GetTicks(float seconds)
        {
            if (Frequency == 0)
                GetFrequency(out Frequency, out InvFrequency);
            return (long)(seconds * Frequency);
        }

        /// <summary>
        /// Use the timer to spin wait this thread
        /// </summary>
        public static void SpinWait(float seconds)
        {
            GetCurrentTicks(out long start);
            long ticksToWait = GetTicks(seconds);
            while (true)
            {
                GetCurrentTicks(out long end);
                long ticksElapsed = (end - start);
                if (ticksElapsed >= ticksToWait)
                    break;
            }
        }
    }

    /// <summary>
    /// Perf timer which aggregates elapsed time 
    /// </summary>
    public class AggregatePerfTimer
    {
        long StartTime;

        // Total time since last Refresh interval
        float CurrentTotal;
        // maximum elapsed sampling time between Start()/Stop()
        float CurrentMax;
        int CurrentSamples;

        // Total time spent during this interval
        public float MeasuredTotal { get; private set; }
        // maximum elapsed sampling time during this interval
        public float MeasuredMax { get; private set; }
        // How many samples were measued during this interval
        // This is essentiall Samples Per Interval
        // if interval=1sec, then this is essentially the FPS
        public int MeasuredSamples { get; private set; }
        // Average time per sample during refresh interval
        public float AvgTime { get; private set; }

        readonly long StatRefreshInterval;
        long NextRefreshTime;

        public AggregatePerfTimer(float statRefreshInterval = 1f/*refresh once per second*/)
        {
            StatRefreshInterval = PerfTimer.GetTicks(statRefreshInterval);
        }

        public void Clear()
        {
            CurrentTotal = 0f;
            CurrentMax = 0f;
            CurrentSamples = 0;

            MeasuredTotal = 0f;
            MeasuredMax = 0f;
            MeasuredSamples = 0;
            AvgTime = 0f;
        }

        // start new sampling
        public void Start()
        {
            PerfTimer.GetCurrentTicks(out StartTime);

            // if we run multi-frame sampling, this ensures our
            // refresh time doesn't drift
            if (NextRefreshTime == 0)
                NextRefreshTime = StartTime + StatRefreshInterval;
        }

        public float TimeUntilNextRefresh
        {
            get
            {
                PerfTimer.GetCurrentTicks(out long now);
                float remaining = (float)((NextRefreshTime - now) * PerfTimer.InvFrequency);
                return remaining;
            }
        }

        // stop and accumulate performance sample
        // @return True if stats were refreshed
        public bool Stop()
        {
            PerfTimer.GetCurrentTicks(out long now);
            float elapsed = (float)((now - StartTime) * PerfTimer.InvFrequency);
            CurrentMax = Math.Max(CurrentMax, elapsed);
            CurrentTotal += elapsed;
            ++CurrentSamples;

            if (now < NextRefreshTime)
                return false;

            NextRefreshTime += StatRefreshInterval;
            if (now >= NextRefreshTime)
            {
                long n = (now - NextRefreshTime) / StatRefreshInterval;
                NextRefreshTime += (n + 1) * StatRefreshInterval;
            }

            MeasuredTotal = CurrentTotal;
            MeasuredMax = CurrentMax;
            MeasuredSamples = CurrentSamples;
            AvgTime = MeasuredTotal / MeasuredSamples;

            CurrentTotal = 0f;
            CurrentMax = 0f;
            CurrentSamples = 0;
            return true;
        }

        public override string ToString()
        {
            return $"{AvgTime*1000,4:0.0}ms   max {MeasuredMax*1000f,4:0.0}ms   all {MeasuredTotal*1000f,4:0}ms";
        }

        public string String(AggregatePerfTimer total)
        {
            bool isZero = total.MeasuredTotal.AlmostZero();
            int percent = isZero ? 0 : (int)((MeasuredTotal / total.MeasuredTotal) * 100);
            return $"{this}  {percent,3}%";
        }
    }
}
