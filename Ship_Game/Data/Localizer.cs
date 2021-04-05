using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ship_Game.Data.Yaml;
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
        public Array<Token> TokenList;
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
        public static string Trade => Token(321);
        public static string GovernorBudget => Token(1916);
        public static string TreasuryGoal => Token(1917);

        public static string AutoTaxes => Token(6138);
        public static string BudgetScreenTaxSlider => Token(311);

        static string[] Strings = Empty<string>.Array;
        static Map<string, string> NameIdToString = new Map<string, string>();

        public static bool Contains(int locIndex)
        {
            int idx = locIndex - 1;
            return (uint)idx < (uint)Strings.Length && Strings[idx] != null;
        }

        public static string Token(GameText gameText)
        {
            return Token((int)gameText);
        }

        public static string Token(int locIndex)
        {
            int idx = locIndex - 1;
            if ((uint)idx < (uint)Strings.Length)
            {
                string token = Strings[idx];
                if (token != null)
                    return token;
            }
            return "<localization missing>";
        }

        /// <summary>
        /// Gets a Token by using its assigned UID string
        /// string text = Localizer.Token("AttackRunsOrder"); // "Attack Runs Order"
        /// </summary>
        public static string Token(string nameId)
        {
            return NameIdToString.TryGetValue(nameId, out string text)
                ? text : "<"+nameId+">";
        }

        public static void Reset()
        {
            Strings = Empty<string>.Array;
            NameIdToString = new Map<string, string>();
        }

        // add extra localization tokens to the localizer
        public static void AddTokens(Array<Token> tokens)
        {
            // Index entries aren't guaranteed to be ordered properly (due to messy mods)
            int limit = tokens.Max(t => t.Index);

            // Fill sparse map with empty entries
            if (Strings.Length < limit)
                Array.Resize(ref Strings, limit);

            for (int i = 0; i < tokens.Count; ++i)
            {
                Token t = tokens[i];
                string text = t.Text.Replace("\\n", "\n"); // only creates new string if \\n is found
                Strings[t.Index - 1] = text;
            }
        }

        class LangToken
        {
            public string NameId;
            public int Id;
            public string ENG;
            public string RUS;
            public string SPA;
        }

        public static void AddFromYaml(string yamlFile, Language language)
        {
            var tokens = new Array<LangToken>();
            using (var parser = new YamlParser(yamlFile))
            {
                foreach (KeyValuePair<object, LangToken> kv in parser.DeserializeMap<LangToken>())
                {
                    kv.Value.NameId = (string)kv.Key;
                    tokens.Add(kv.Value);
                }
            }
            
            // Index entries aren't guaranteed to be ordered properly (due to messy mods)
            int limit = tokens.Max(t => t.Id);

            // Fill sparse map with empty entries
            if (Strings.Length < limit)
                Array.Resize(ref Strings, limit);

            for (int i = 0; i < tokens.Count; ++i)
            {
                LangToken t = tokens[i];
                string text;
                switch (language)
                {
                    default:
                    case Language.English: text = t.ENG; break;
                    case Language.Russian: text = t.RUS; break;
                    case Language.Spanish: text = t.SPA; break;
                }
                Strings[t.Id - 1] = text;
                NameIdToString.Add(t.NameId, text);
            }
        }

        public static string GetRole(ShipData.RoleName role, Empire owner) => GetRole(role, owner.data.Traits.ShipType);
        public static string GetRole(ShipData.RoleName role, string shipType)
        {
            int localIndex = ShipRole.GetRoleName(role, shipType);
            return localIndex > 0 ? Token(localIndex) : "unknown";
        }
    }
}
