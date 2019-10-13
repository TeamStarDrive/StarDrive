using System;
using System.Diagnostics;

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

        // Provides a simple NaN-checking facility to avoid broken float values
        // @note FIX/WORKAROUND for save-game NaN bug
        public static float NaNChecked(this float value, float defaultValue, string where)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                Log.Warning(ConsoleColor.DarkRed, $"{where} NaN, defaulting to {defaultValue}");
                return defaultValue;
            }
            return value;
        }
    }
}
