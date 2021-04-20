using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;
using Ship_Game.Ships;

namespace Ship_Game
{
    // TODO: DEPRECATED
    public struct Token
    {
        public int Index;
        public string Text;
    }
    
    // TODO: DEPRECATED
    public sealed class LocalizationFile
    {
        public Array<Token> TokenList;
    }
    
    [StarDataType]
    public class LangToken
    {
        [StarDataKeyName] public string NameId;
        [StarData] public int Id;
        [StarData] public string ENG;
        [StarData] public string RUS;
        [StarData] public string SPA;
    }

    public static class Localizer
    {
        //Hull Bonus Text
        public static string HullArmorBonus => Token(GameText.ArmorProtection); 
        public static string HullShieldBonus => "Shield Strength";
        public static string HullSensorBonus => Token(GameText.ArmorProtection);
        public static string HullSpeedBonus => Token(GameText.MaxSpeed);
        public static string HullCargoBonus => Token(GameText.CargoSpace2);
        public static string HullDamageBonus => "Weapon Damage";
        public static string HullFireRateBonus => Token(GameText.FireRate);
        public static string HullRepairBonus => Token(GameText.RepairRate);
        public static string HullCostBonus => Token(GameText.CostReduction);
        public static string Trade => Token(GameText.Trade);
        public static string GovernorBudget => Token(GameText.GovernorBudget);
        public static string TreasuryGoal => Token(GameText.TreasuryGoal);

        public static Language Language { get; private set; }

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
        /// 
        /// For backwards compatibility we also support Integer ID-s:
        /// string text = Localizer.Token("1"); // "New Game"
        /// </summary>
        public static string Token(string nameId)
        {
            if (nameId.IsEmpty())
                return "<nameId missing>";

            if (char.IsDigit(nameId[0]) && int.TryParse(nameId, out int id))
                return Token(id);

            return NameIdToString.TryGetValue(nameId, out string text) ? text : "<"+nameId+">";
        }

        public static bool Token(string nameId, out string text)
        {
            if (nameId.IsEmpty())
            {
                text = null;
                return false;
            }

            if (char.IsDigit(nameId[0]) && int.TryParse(nameId, out int id))
            {
                text = Token(id);
                return true;
            }

            return NameIdToString.TryGetValue(nameId, out text);
        }

        public static void Reset()
        {
            Strings = Empty<string>.Array;
            NameIdToString = new Map<string, string>();
        }

        // add extra localization tokens to the localizer
        public static void AddTokens(Array<Token> tokens, Language language)
        {
            Language = language;

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

        public static void AddFromYaml(FileInfo file, Language language)
        {
            Language = language;
            Array<LangToken> tokens = YamlParser.DeserializeArray<LangToken>(file);
            
            // Index entries aren't guaranteed to be ordered properly (due to messy mods)
            int limit = tokens.Max(t => t.Id);

            // Fill sparse map with empty entries
            if (Strings.Length < limit)
                Array.Resize(ref Strings, limit);

            for (int i = 0; i < tokens.Count; ++i)
            {
                LangToken t = tokens[i];
                string text = t.ENG;
                switch (language)
                {
                    case Language.Russian: text = t.RUS.NotEmpty() ? t.RUS : t.ENG; break;
                    case Language.Spanish: text = t.SPA.NotEmpty() ? t.SPA : t.ENG; break;
                }
                
                // if this ID already exist, overwrite by using new text
                Strings[t.Id - 1] = text;
                if (NameIdToString.ContainsKey(t.NameId))
                    NameIdToString[t.NameId] = text;
                else
                    NameIdToString.Add(t.NameId, text);
            }
        }

        public static string GetRole(ShipData.RoleName role, Empire owner) => GetRole(role, owner.data.Traits.ShipType);
        public static string GetRole(ShipData.RoleName role, string shipType)
        {
            int localIndex = ShipRole.GetRoleName(role, shipType);
            return localIndex > 0 ? Token(localIndex) : "unknown";
        }

        public static IEnumerable<string> EnumerateTokens()
        {
            for (int i = 0; i < Strings.Length; ++i)
            {
                string token = Strings[i];
                if (token != null)
                    yield return token;
            }
        }
    }
}
