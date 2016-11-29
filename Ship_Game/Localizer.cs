using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ship_Game
{
    public sealed class Localizer
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
                if (ResourceManager.OffSet != 0 && t.Index > ResourceManager.OffSet)
                    continue;
                t.Index += ResourceManager.OffSet;
                if (Localizer.LocalizerDict.ContainsKey(t.Index))
                {
                    Localizer.LocalizerDict[t.Index] = string.Intern(t.Text);
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

        public static string GetRole(ShipData.RoleName role, Empire Owner)
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
                return "unknown";
            }
        }

        public static string Token(int index)
        {
            string str;
            //try
            {
                string val;
                 
                if(!Localizer.LocalizerDict.TryGetValue(index,out val) || val ==null)
                {
                    return "String not found";

                }
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
            //catch
            //{
            //    str = "String not found";
            //}
            return str;
        }
        public static void cleanLocalizer()
        {
            //foreach (KeyValuePair<int, string> local in LocalizerDict)
            //for (int i = 0; i< LocalizerDict.Count; i++ )
            if (ResourceManager.OffSet == 0)
                return;
            List<int> keys = new List<int>(LocalizerDict.Keys);
            foreach(int i in keys)
            {
                if (i < ResourceManager.OffSet || (i >= ResourceManager.OffSet&& used[i] ==true))
                    continue;

                string replace = null;
                int clear = i - ResourceManager.OffSet;
                try
                {
                    if (LocalizerDict.TryGetValue(clear, out replace) && !string.IsNullOrEmpty(replace))
                    {
                        //Debug.WriteLine(string.Concat("vkey=", clear, " ", LocalizerDict[clear], "\nnewKey=", i, " ", LocalizerDict[i]));
                        LocalizerDict[clear] = LocalizerDict[i];
                    }
                    else if (LocalizerDict.TryGetValue(i, out replace) && !string.IsNullOrEmpty(replace))
                    {
                        if (!LocalizerDict.ContainsKey(clear))
                        {
                            //Debug.WriteLine(string.Concat("vkey=", clear, " ", LocalizerDict[clear], "\nnewKey=", i, " ", LocalizerDict[i]));
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