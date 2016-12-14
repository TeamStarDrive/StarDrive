using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public class PerfTimer
    {
        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long perfcount);

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long freq);

        private static readonly long Frequency;
        private long Time;
        private int NumSamples;

        static PerfTimer()
        {
            QueryPerformanceFrequency(out Frequency);
        }

        // start perf timer
        public void Start()
        {
            QueryPerformanceCounter(out Time);
        }

        // stop and gather performance sample
        public void Stop()
        {
            long end;
            QueryPerformanceCounter(out end);
            float elapsed = (float)((double)(end - Time) / Frequency);

            AvgTime = (AvgTime*NumSamples + elapsed) / (NumSamples + 1);

            // Well... this is kinda complicated to do without a list indeed
            if (elapsed > MaxTime) MaxTime  = elapsed;
            else                   MaxTime *= 0.98f; // trickle down towards avg time

            ++NumSamples;
        }

        public float AvgTime { get; private set; }
        public float MaxTime { get; private set; }

        public override string ToString()
        {
            return $"{AvgTime*1000f:0.0,+5}ms ({MaxTime*1000f:0.0,+5}ms)";
        }
    }
}
