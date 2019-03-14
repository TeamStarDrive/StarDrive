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


        public static Array<PlanetCategory> CreateWeights(Array<SunZoneData> sunZoneList, SunZone sunZone)
        {
            var categoryList = new  Array<PlanetCategory>();
            for (int i = 0; i < sunZoneList.Count; i++)
            {
                if (sunZone != sunZoneList[i].Zone)
                    continue;

                for (int j = 0; j < sunZoneList[i].Weights.Count; j++)
                {
                    int value = sunZoneList[i].Weights[j].Value;
                    for (int h = 0; h < value; h++)
                    {
                        categoryList.Add(sunZoneList[i].Weights[j].Type);
                    }
                }
            }
            return categoryList;
        }
    }
}