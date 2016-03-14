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
        public string Name;
        public string ModName = "";
        public string ModPath = "";
        public string Version;
        public RacialTrait Traits;

        public RaceSave()
        { }

        public RaceSave(RacialTrait Traits)
        {
            this.Name = Traits.Name;
            if (GlobalStats.ActiveMod != null)
            {
                this.ModName = GlobalStats.ActiveMod.mi.ModName;
                this.ModPath = GlobalStats.ActiveMod.ModPath;
            }
            this.Version = ConfigurationManager.AppSettings["ExtendedVersion"];
            this.Traits = Traits;
        }
    }
}
