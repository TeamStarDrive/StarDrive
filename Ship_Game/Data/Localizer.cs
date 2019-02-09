using System;
using System.Linq;
using System.Text;
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

    // Localized text reference
    public struct LocText
    {
        public readonly string Text;

        public LocText(int id)
        {
            Text = Localizer.Token(id);
        }

        /**
         * Parse incoming text as localized text.
         * Example:
         * "  {80}: {81}"  -- parsed to '  Astronomers: Races that have extensively studied......'
         * "\{ {80} \}"    -- parsed to '{ Astronomers }'
         * "\"{80}\""      -- parsed to '"Astronomers"'
         * If there are no parentheses, then the text is not parsed!!
         */
        public LocText(string text)
        {
            if (text.IsEmpty())
            {
                Text = "";
                return;
            }

            if (text[0] != '"')
            {
                Log.Error($"Missing string format BEGIN character '\"' -- LocText not parsed!: {text}");
                Text = text;
                return;
            }

            if (text[text.Length-1] != '"')
            {
                Log.Warning($"Missing string format END character '\"' -- LocText may miss some characters!: {text}");
                Text = text;
                return;
            }

            var sb = new StringBuilder(text.Length);
            for (int i = 1; i < text.Length-1; ++i)
            {
                char c = text[i];
                if (c == '{')
                {
                    int j = i+1;
                    for (; j < text.Length-1; ++j)
                        if (text[j] == '}')
                            break;
                    if (j >= text.Length)
                    {
                        Log.Warning($"Missing localization format END character '}}'! -- LocText not parsed correctly!: {text}");
                        break;
                    }

                    string idString = text.Substring(i+1, (j - i)-1);
                    if (!int.TryParse(idString, out int id))
                    {
                        Log.Error($"Failed to parse localization id: {idString}! -- LocText not parsed correctly: {text}");
                        continue;
                    }
                    sb.Append(Localizer.Token(id));
                    i = j;
                }
                else if (c == '\\') // escape character
                {
                    c = text[i++];
                    if      (c == 'n') sb.Append('\n');
                    else if (c == 't') sb.Append('\t');
                    else if (c == '{') sb.Append('{');
                    else if (c == '}') sb.Append('}');
                    else if (c == '"') sb.Append('"');
                    else sb.Append('\\').Append(c); // unrecognized
                }
                else
                {
                    sb.Append(c); // normal char
                }
            }
            Text = sb.ToString();
        }

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
