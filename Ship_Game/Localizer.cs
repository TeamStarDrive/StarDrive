using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game
{
	public class Localizer
	{
		public static Dictionary<int, string> LocalizerDict;

		static Localizer()
		{
			Localizer.LocalizerDict = new Dictionary<int, string>();
		}

		public Localizer()
		{
		}

		public static void FillLocalizer()
		{
			foreach (Token t in ResourceManager.LanguageFile.TokenList)
			{
				if (Localizer.LocalizerDict.ContainsKey(t.Index))
				{
					Localizer.LocalizerDict[t.Index] = t.Text;
				}
				else
				{
					Localizer.LocalizerDict.Add(t.Index, t.Text);
				}
			}
		}

		public static string GetRole(string role)
		{
			string str = role;
			string str1 = str;
			if (str != null)
			{
				switch (str1)
				{
					case "carrier":
					{
						return Localizer.Token(138);
					}
					case "fighter":
					{
						return Localizer.Token(137);
					}
					case "scout":
					{
						return Localizer.Token(139);
					}
					case "freighter":
					{
						return Localizer.Token(140);
					}
					case "frigate":
					{
						return Localizer.Token(141);
					}
					case "troop":
					{
						return Localizer.Token(142);
					}
					case "construction":
					{
						return Localizer.Token(143);
					}
					case "cruiser":
					{
						return Localizer.Token(144);
					}
					case "capital":
					{
						return Localizer.Token(145);
					}
					case "supply":
					{
						return Localizer.Token(146);
					}
					case "platform":
					{
						return Localizer.Token(147);
					}
					case "station":
					{
						return "Station";
					}
				}
			}
			return role;
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
	}
}