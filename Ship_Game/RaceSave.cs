using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Ship_Game
{
    public sealed class RaceSave
    {
        public EmpireData Data;
        public string Name;
        public string ModName = "";
        public string ModPath = "";
        public string Version;

        public RaceSave()
        { }

        public RaceSave(EmpireData data)
        {
            this.Data = data;
            this.Name = data.Traits.Name;
            if (GlobalStats.ActiveMod != null)
            {
                this.ModName = GlobalStats.ActiveMod.mi.ModName;
                this.ModPath = GlobalStats.ActiveMod.ModPath;
            }
            this.Version = ConfigurationManager.AppSettings["ExtendedVersion"];
        }
    }
}
