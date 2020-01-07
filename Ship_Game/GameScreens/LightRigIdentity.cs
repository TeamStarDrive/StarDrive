using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.GameScreens
{
    /// <summary>
    /// It's important that UniverseScreen resets
    /// the global (yes, global, unfortunately) light rig
    /// if we switch to Shipyard or to Fleet design screen and then back.
    /// </summary>
    public enum LightRigIdentity
    {
        Unknown,
        MainMenu,
        UniverseScreen,
        Shipyard,
        FleetDesign,
        ShipToolScreen,
    }
}
