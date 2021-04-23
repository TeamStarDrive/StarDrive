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
        static Map<string, Token> NameIdToToken = new Map<string, Token>();

        public static bool Contains(int locIndex)
        {
            int idx = locIndex - 1;
            return (uint)idx < (uint)Strings.Length && Strings[idx] != null;
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
        /// Gets a token using the pre-processed GameText enum which is run
        /// via the `--run-localizer` command line argument
        /// </summary>
        public static string Token(GameText gameText)
        {
            int id = GetTokenId(gameText);
            return Token(id);
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
            if (Token(nameId, out string text))
                return text;
            return nameId.NotEmpty() ? nameId : "<missing nameid>";
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

            if (NameIdToToken.TryGetValue(nameId, out var token))
            {
                text = token.Text;
                return true;
            }

            text = null;
            return false;
        }

        /// <summary>
        /// Gets the stable ID of a GameText token
        /// Throws on failure
        /// </summary>
        /// <param name="gameText"></param>
        /// <returns></returns>
        public static int GetTokenId(GameText gameText)
        {
            int id = (int)gameText;
            if (id > 0)
                return id;

            string nameId = gameText.ToString();
            if (NameIdToToken.TryGetValue(nameId, out var token))
                return token.Index;
                
            throw new InvalidDataException($"GetTokenId({gameText}) failed!");
        }

        public static void LoadFromYaml(FileInfo gameText, FileInfo modText, Language language)
        {
            Language = language;
            Array<LangToken> tokens = YamlParser.DeserializeArray<LangToken>(gameText);
            if (modText.Exists)
            {
                tokens.AddRange(YamlParser.DeserializeArray<LangToken>(modText));
            }


            // Index entries aren't guaranteed to be ordered properly
            int maxId = tokens.Max(t => t.Id);
            int nextGeneratedId = maxId + 1;
            int numIdsToGenerate = tokens.Count(t => t.Id <= 0);
            maxId += numIdsToGenerate;

            // Fill sparse map with empty entries
            Strings = new string[maxId];
            NameIdToToken = new Map<string, Token>();
            LocalizedText.ClearCache();

            for (int i = 0; i < tokens.Count; ++i)
            {
                LangToken t = tokens[i];
                string text = t.ENG;
                switch (language)
                {
                    case Language.Russian: text = t.RUS.NotEmpty() ? t.RUS : t.ENG; break;
                    case Language.Spanish: text = t.SPA.NotEmpty() ? t.SPA : t.ENG; break;
                }
                
                // when replacing an existing token, reuse its Index
                if (NameIdToToken.TryGetValue(t.NameId, out var token))
                {
                    token.Text = text;
                    Strings[token.Index - 1] = text;
                    NameIdToToken[t.NameId] = token;
                }
                else
                {
                    // In latest localization we no longer use indices
                    // Generate the ID-s here if needed
                    int index = t.Id > 0 ? t.Id : nextGeneratedId++;
                    Strings[index - 1] = text;
                    NameIdToToken.Add(t.NameId, new Token{ Index = index, Text = text });
                }
            }
        }

        public static string GetRole(ShipData.RoleName role, Empire owner) => GetRole(role, owner.data.Traits.ShipType);
        public static string GetRole(ShipData.RoleName role, string shipType)
        {
            LocalizedText name = ShipRole.GetRoleName(role, shipType);
            return name.NotEmpty ? name.Text : "unknown";
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
