using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data;

namespace Ship_Game
{
    public class PlanetTypeInfo
    {
        [StarData(true)] public int Id;
        [StarData] public PlanetCategory Category;
        [StarData] public int CompositionId;

        public string Composition => Localizer.Token(CompositionId);
    }
}
