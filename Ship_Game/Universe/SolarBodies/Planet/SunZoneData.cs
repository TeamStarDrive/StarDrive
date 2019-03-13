using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data;

namespace Ship_Game
{
    [StarDataType]
    public class TypeWeight
    {
        [StarDataKey] public readonly PlanetCategory Type; // Barren
        [StarData] public readonly int Value; // 10
    }

    public class SunZoneData
    {
        [StarDataKey] public readonly SunZone Zone; // Near
        [StarData] public readonly Array<TypeWeight> Weights;
    }
}