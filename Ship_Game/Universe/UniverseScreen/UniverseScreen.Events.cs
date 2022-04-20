using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        /// EVT: triggered when Player's buildable ships are updated
        public void OnPlayerBuildableShipsUpdated()
        {
            aw?.UpdateDropDowns();
        }
    }
}
