using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game
{
    public class Localizer
    {
        public static Dictionary<int, string> LocalizerDict;
        public static Dictionary<int, bool> used;

        static Localizer()
        {
            Localizer.LocalizerDict = new Dictionary<int, string>();
            Localizer.used = new Dictionary<int, bool>();
        }

        public Localizer()
        {
        }

        public static void FillLocalizer()
        {
            foreach (Token t in ResourceManager.LanguageFile.TokenList)
            {
                t.Index += ResourceManager.OffSet;
                if (Localizer.LocalizerDict.ContainsKey(t.Index))
                {
                    Localizer.LocalizerDict[t.Index] = t.Text;
                    if (ResourceManager.OffSet > 0)
                        Localizer.used[t.Index] = false;
                }
                else
                {
                    Localizer.LocalizerDict.Add(t.Index, t.Text);
                    if (ResourceManager.OffSet > 0)
                        Localizer.used.Add(t.Index, false);
                }
            }

        }

        public static string GetRole(string role, Empire Owner)
        {
            if (ResourceManager.ShipRoles.ContainsKey(role))
            {
                for (int i = 0; i < ResourceManager.ShipRoles[role].RaceList.Count(); i++)
                {
                    if (ResourceManager.ShipRoles[role].RaceList[i].ShipType == Owner.data.Traits.ShipType)
                    {
                        return Localizer.Token(ResourceManager.ShipRoles[role].RaceList[i].Localization);
                    }
                }
                return Localizer.Token(ResourceManager.ShipRoles[role].Localization);
            }
            else
            {
                return role;
            }
        }

        public static string Token(int index)
        {
            string str;
            try
            {
                string val = Localizer.LocalizerDict[index];
                string[] strArrays = new string[] { "\\n" };
                string[] array = val.Split(strArrays, StringSplitOptions.None);
                val = array[0];
                for (int i = 1; i < array.Count<string>(); i++)
                {
                    val = string.Concat(val, "\n");
                    val = string.Concat(val, array[i]);
                }
                str = val;
            }
            catch
            {
                str = "";
            }
            return str;
        }
        public static void cleanLocalizer()
        {
            //foreach (KeyValuePair<int, string> local in LocalizerDict)
            for (int i = 0; i< LocalizerDict.Count; i++ )
            {
                if (i < ResourceManager.OffSet)
                    continue;
                foreach (KeyValuePair<int, bool> inuse in used)
                {
                    string replace = null;

                    int clear = inuse.Key - ResourceManager.OffSet;
                    if (LocalizerDict.TryGetValue(clear, out replace))
                    {

                        LocalizerDict[clear] = LocalizerDict[i];
                    }

                }
            }
        }
    }

}