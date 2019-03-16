using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data;

namespace Ship_Game
{
    [StarDataType]
    public class TypeWeight  // Created by Fat Bastard - to select odds of planet category to be created per sun zone
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
            /* CreateWeights will take each planet category from the yaml and
             * create an array consisting a number of value corresponding to
             * the weights per category in the yaml. For instance, if SunZone.Near
             * contains Volcanic weight of 20 and Barren weight of 10, an array of
             * size 30 will be created. It will contain 20 volcanic and 10 barren enums.
             * Then 1 category can be chosen randomly from this array for star system creation
             */

            var categoryList = new  Array<PlanetCategory>();
            // search for the sunZone from the yaml list
            for (int i = 0; i < sunZoneList.Count; i++)
            {
                SunZone sunZoneFromList = sunZoneList[i].Zone;
                if (sunZone != sunZoneFromList)
                    continue;

                // cycling each planet category to add the weights to the array
                var sunZoneWeights = sunZoneList[i].Weights;
                for (int j = 0; j < sunZoneWeights.Count; j++)
                {
                    // adding the defined weight to the category list array
                    int categoryWeight = sunZoneWeights[j].Value;
                    for (int h = 0; h < categoryWeight; h++)
                    {
                        categoryList.Add(sunZoneWeights[j].Type);
                    }
                }
            }
            return categoryList;
        }
    }
}