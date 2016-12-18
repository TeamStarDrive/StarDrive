using System;
using System.Collections;
using System.Linq;
using System.Diagnostics;

namespace Ship_Game
{
    public struct Token
    {
        public int Index;
        public string Text;
    }

    public sealed class LocalizationFile
    {
        public Token[] TokenList;
    }

    public static class Localizer
    {
        private static string[] Strings;

        public static bool Contains(int locIndex)
        {
            return locIndex <= Strings.Length && Strings[locIndex - 1] != null;
        }
        public static string Token(int locIndex)
        {
            return Contains(locIndex) ? Strings[locIndex - 1] : "<localization missing>";
        }

        // add extra localization tokens to the localizer
        public static void AddTokens(Token[] tokens)
        {
            // Index entries aren't guaranteed to be ordered properly (due to messy mods)
            int limit = tokens.Max(t => t.Index);

            // Fill sparse map with empty entries
            if (Strings == null || Strings.Length < limit)
                Array.Resize(ref Strings, limit);

            foreach (Token t in tokens)
            {
                int locIndex = t.Index;
                string text = t.Text.Replace("\\n", "\n"); // only creates new string if \\n is found

                Strings[locIndex - 1] = text;
            }
        }

        public static string GetRole(ShipData.RoleName role, Empire owner)
        {
            if (!ResourceManager.ShipRoles.TryGetValue(role, out ShipRole shipRole))
                return "unknown";

            foreach (ShipRole.Race race in shipRole.RaceList)
                if (race.ShipType == owner.data.Traits.ShipType)
                    return Token(race.Localization);

            return Token(shipRole.Localization);
        }

        // statistic for amount of memory used for storing strings
        public static int CountBytesUsed()
        {
            if (Strings == null)
                return 0;

            int bytes = Strings.Length  * 4 + 8;
            foreach (string text in Strings)
                if (text != null) bytes += 4 + text.Length * 2;

            return bytes;
        }
    }

}