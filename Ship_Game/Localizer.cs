using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ship_Game
{
    public static class Localizer
    {
        public static readonly Dictionary<int, string> LocalizerDict = new Dictionary<int, string>();
        public static readonly Dictionary<int, bool> used = new Dictionary<int, bool>();

        public static void FillLocalizer()
        {
            foreach (Token t in ResourceManager.LanguageFile.TokenList)
            {
                if (ResourceManager.OffSet != 0 && t.Index > ResourceManager.OffSet)
                    continue;
                t.Index += ResourceManager.OffSet;

                LocalizerDict[t.Index] = string.Intern(t.Text.Replace("\\n", "\n"));
                if (ResourceManager.OffSet > 0)
                    used.Add(t.Index, false);
            }
        }

        public static string GetRole(ShipData.RoleName role, Empire owner)
        {
            if (!ResourceManager.ShipRoles.ContainsKey(role))
                return "unknown";

            var shipRole = ResourceManager.ShipRoles[role];
            foreach (ShipRole.Race race in shipRole.RaceList)
                if (race.ShipType == owner.data.Traits.ShipType)
                    return Token(race.Localization);

            return Token(shipRole.Localization);
        }

        public static string Token(int index)
        {
            if (LocalizerDict.TryGetValue(index, out string str) && str != null)
                return str;
            return "String not found";
        }
        public static void CleanLocalizer()
        {
            if (ResourceManager.OffSet == 0)
                return;
            var keys = new List<int>(LocalizerDict.Keys);
            foreach(int i in keys)
            {
                if (i < ResourceManager.OffSet || (i >= ResourceManager.OffSet&& used[i]))
                    continue;

                string replace = null;
                int clear = i - ResourceManager.OffSet;
                try
                {
                    if (LocalizerDict.TryGetValue(clear, out replace) && !string.IsNullOrEmpty(replace))
                    {
                        LocalizerDict[clear] = LocalizerDict[i];
                    }
                    else if (LocalizerDict.TryGetValue(i, out replace) && !string.IsNullOrEmpty(replace))
                    {
                        if (!LocalizerDict.ContainsKey(clear))
                        {
                            LocalizerDict.Add(clear, LocalizerDict[i]);
                        }
                    }
                }
                catch (Exception e)
                {
                    e.Data.Add("Text", replace);
                    e.Data.Add("SafeIndex", i);
                    e.Data.Add("UnSafeIndex", clear);
                }
            }
        }
    }

}