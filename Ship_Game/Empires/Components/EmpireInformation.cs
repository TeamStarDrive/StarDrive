using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;

namespace Ship_Game.Empires.Components
{
    [StarDataType]
    public class EmpireInformation
    {
        // only to supoport old saves. should be removed in the future or when we spin up save version (deleted entire class)
        public enum InformationLevel
        {
            None,
            Minimal,
            Normal,
            High,
            Full
        }
    }
}
