using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public sealed class RaceSave
    {
        public EmpireData data;
        public string name;
        public ModEntry mod;

        public RaceSave(EmpireData data, string name)
        {
            this.data = data;
            this.name = name;
            this.mod = GlobalStats.ActiveMod;
        }
    }
}
