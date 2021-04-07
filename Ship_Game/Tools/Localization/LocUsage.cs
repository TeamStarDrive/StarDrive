using System;
using System.Collections.Generic;
using System.IO;

namespace Ship_Game.Tools.Localization
{
    public class LocUsage
    {
        public string NameId;
        public int Id;
        public Usage Usage;
        public string Name;
        public string Suffix;
        public string File;

        public LocUsage(int id, Usage usage, string name, string suffix, string file)
        {
            Id = id;
            Usage = usage;
            Name = name;
            Suffix = suffix;
            File = file;
            NameId = GetUsagePrefix(Usage) + "_" + name + "_" + suffix;
        }

        static string GetUsagePrefix(Usage usage)
        {
            switch (usage)
            {
                default:
                case Usage.Unknown:    return "Text";
                case Usage.Building:   return "Bldg";
                case Usage.Technology: return "Tech";
                case Usage.Tooltip:    return "Tips";
                case Usage.Weapon:     return "Weps";
                case Usage.Module:     return "Modu";
                case Usage.Artifact:   return "Arti";
                case Usage.Troop:      return "Troop";
                case Usage.Races:      return "Race";
            }
        }
    }
}
