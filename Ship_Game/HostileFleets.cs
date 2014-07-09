using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ship_Game
{
    //Added by McShooterz: defines new enemy fleets to spawn in place of Remnant
    public class HostileFleets
    {
        public List<Fleet> Fleets;

        public struct Fleet
        {
            public string Empire;
            public List<string> Ships;
        }
    }
}
