using System.Globalization;

namespace Ship_Game
{
    public static class NumberExt
    {
        public static string String(this float number)
        {
            return number.ToString("0.#", CultureInfo.InvariantCulture);
        }

        public static string String(this float number, int numDecimals)
        {
            switch (numDecimals)
            {
                case 0:  return ((int)number).ToString();
                case 1:  return number.ToString("0.#", CultureInfo.InvariantCulture);
                case 2:  return number.ToString("0.##", CultureInfo.InvariantCulture);
                default: return number.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }

        public static string StarDateString(this float starDate)
        {
            return starDate.ToString("####.0", CultureInfo.InvariantCulture);
        }
    }
}
