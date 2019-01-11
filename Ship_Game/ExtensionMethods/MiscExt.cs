using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public static class MiscExt
    {
        // gets time elapsed as millis, and Restarts the timer
        public static int NextMillis(this Stopwatch t)
        {
            int elapsed = (int)t.Elapsed.TotalMilliseconds;
            t.Restart();
            return elapsed;
        }
    }
}
