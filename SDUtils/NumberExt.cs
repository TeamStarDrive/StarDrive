using System.Globalization;

namespace SDUtils
{
    public static class NumberExt
    {
        // Shorthand for String(1)
        public static string String(this float number)
        {
            return number.ToString("0.#", CultureInfo.InvariantCulture);
        }

        // Shorthand for String(1)
        public static string String(this double number)
        {
            return number.ToString("0.#", CultureInfo.InvariantCulture);
        }

        // Shorthand for String(0), added this here to make maintaining float <-> int changes easier
        public static string String(this int number)
        {
            return number.ToString();
        }

        /// <summary>
        /// Culture Invariant number formatting to string
        /// This ensures we always format numbers the same way, even on systems with overriding localizations
        /// Examples:
        /// String(1.0, 0) => "1"
        /// String(1.0, 1) => "1"
        /// String(1.1, 1) => "1.1"
        /// String(1.257, 2) => "1.26"
        /// </summary>
        /// <param name="numDecimals">Controls the number of decimals after the . </param>
        public static string String(this float number, int numDecimals)
        {
            switch (numDecimals)
            {
                case 0:  return ((int)number).ToString();
                case 1:  return number.ToString("0.#",   CultureInfo.InvariantCulture);
                case 2:  return number.ToString("0.##",  CultureInfo.InvariantCulture);
                default: return number.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }

        public static string String(this double number, int numDecimals)
        {
            switch (numDecimals)
            {
                case 0: return ((int)number).ToString();
                case 1: return number.ToString("0.#", CultureInfo.InvariantCulture);
                case 2: return number.ToString("0.##", CultureInfo.InvariantCulture);
                default: return number.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Shorthand to SignString(number, 1)
        /// </summary>
        public static string SignString(this float number)
        {
            return number.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Always adds a sign to the value, displaying 1.012 as "+1.0"
        /// Also, the trailing 0 is never trimmed, so 1.0 is "+1.0", unlike String(1)
        /// </summary>
        public static string SignString(this float number, int numDecimals)
        {
            switch (numDecimals)
            {
                case 0:  return ((int)number).ToString();
                case 1:  return number.ToString("+0.0;-0.0;0.0",   CultureInfo.InvariantCulture);
                case 2:  return number.ToString("+0.00;-0.00;0.00",  CultureInfo.InvariantCulture);
                default: return number.ToString("+0.000;-0.000;0.000", CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Displays a relative [0.0; 1.0] number as a percentage 0% .. 100%
        /// If value is outside of [0; 1] range, it will display 1.27 as 127%
        /// </summary>
        public static string PercentString(this float number, int numDecimals)
        {
            float percentage = number * 100f;
            switch (numDecimals)
            {
                case 0:  return ((int)percentage) + "%";
                case 1:  return percentage.ToString("+0.0;-0.0;0.0",   CultureInfo.InvariantCulture) + "%";
                case 2:  return percentage.ToString("+0.00;-0.00;0.00",  CultureInfo.InvariantCulture) + "%";
                default: return percentage.ToString("+0.000;-0.000;0.000", CultureInfo.InvariantCulture) + "%";
            }
        }

        /// Shorthand for PercentString(number, 0)
        public static string PercentString(this float number)
        {
            float percentage = number * 100f;
            return ((int)percentage) + "%";
        }

        const double RadianToDegree = 180.0 / 3.14159265358979;

        /// Converts Radians to Degrees and appends degrees symbol
        /// Example: DegreeString(3.14...) => "180°"
        public static string DegreeString(this float radians, int numDecimals = 1)
        {
            double degrees = radians * RadianToDegree;
            return String(degrees, numDecimals) + "°";
        }

        /// <summary>
        /// Always 2 decimal places
        /// Example: 100.5 => "100.50"
        /// </summary>
        public static string MoneyString(this float number)
        {
            return number.ToString("0.00", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Always 1 decimal place, eg 1000.0 => "1000.0"
        /// </summary>
        public static string StarDateString(this float starDate)
        {
            return starDate.ToString("####.0", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Displays this `seconds` as: 60s or 1m 32s or 1h 45m 32s or 2d 8h 32m 5s
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static string TimeString(this float seconds)
        {
            if (float.IsInfinity(seconds))
                return "?";
            const float MINUTE = 60;
            const float HOUR = 60*MINUTE;
            const float DAY = 24*HOUR;

            if (seconds < MINUTE)
                return $"{(int)seconds}s";

            if (seconds < HOUR)
                return $"{(int)(seconds / MINUTE)}m {(int)(seconds % MINUTE)}s";

            if (seconds < DAY)
                return $"{(int)(seconds / HOUR)}h {(int)((seconds % HOUR) / MINUTE)}m {(int)(seconds % MINUTE)}s";

            return $"{(int)(seconds / DAY)}d {(int)((seconds % DAY) / HOUR)}h {(int)((seconds % HOUR) / MINUTE)}m {(int)(seconds % MINUTE)}s";
        }
    }
}
