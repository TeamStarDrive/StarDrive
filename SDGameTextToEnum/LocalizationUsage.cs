using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDGameTextToEnum
{
    public enum Usage
    {
        Unknown,    //
        Building,   // prefix=Bldg
        Technology, // prefix=Tech
        Tooltip,    // prefix=Tips
        Ship,       // prefix=Ship
        Weapon,     // prefix=Weps
    }

    public class LocalizationUsage
    {
        public int Id;
        public Usage Usage;
        public string Tag;
        public string File;

        public string UsagePrefix
        {
            get
            {
                switch (Usage)
                {
                    default:
                    case Usage.Unknown: throw new Exception($"Unknown usage Tag={Tag} File={File}");
                    case Usage.Building:   return "Bldg";
                    case Usage.Technology: return "Tech";
                    case Usage.Tooltip:    return "Tips";
                    case Usage.Ship:       return "Ship";
                    case Usage.Weapon:     return "Weps";
                }
            }
        }

        public string CreateNameId(string prefix)
        {
            string nameId = prefix + "_" + ;
            return null;
        }
    }
}
