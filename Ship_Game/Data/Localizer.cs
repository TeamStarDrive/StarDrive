using System;
using System.Linq;
using Ship_Game.Ships;

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

    // Localized text integer reference
    public struct LocText
    {
        public int Id;
        public LocText(int id)
        {
            Id = id;
        }
        public string Text => Localizer.Token(Id);

        public static implicit operator LocText(int id)
        {
            return new LocText(id);
        }
    }

    public static class Localizer
    {
        //Hull Bonus Text
        public static string HullArmorBonus => Token(6016); 
        public static string HullShieldBonus => "Shield Strength";
        public static string HullSensorBonus => Token(6016);
        public static string HullSpeedBonus => Token(6018);
        public static string HullCargoBonus => Token(6019);
        public static string HullDamageBonus => "Weapon Damage";
        public static string HullFireRateBonus => Token(6020);
        public static string HullRepairBonus => Token(6013);
        public static string HullCostBonus => Token(6021);


        private static string[] Strings = new string[0];

        public static bool Contains(int locIndex)
        {
            return 0 < locIndex && locIndex <= Strings.Length && Strings[locIndex - 1] != null;
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
            if (Strings.Length < limit)
                Array.Resize(ref Strings, limit);

            foreach (Token t in tokens)
            {
                string text = t.Text.Replace("\\n", "\n"); // only creates new string if \\n is found

                Strings[t.Index - 1] = text;
            }
        }

        public static string GetRole(ShipData.RoleName role, Empire owner)
        {
            int localIndex = ShipRole.GetRoleName(role, owner);
            return localIndex > 0 ? Token(localIndex) : "unknown";
        }

        // statistic for amount of memory used for storing strings
        public static int CountBytesUsed()
        {
            if (Strings.Length == 0)
                return 0;

            int bytes = Strings.Length  * 4 + 8;
            foreach (string text in Strings)
                if (text != null) bytes += 4 + text.Length * 2;

            return bytes;
        }
    }
}
