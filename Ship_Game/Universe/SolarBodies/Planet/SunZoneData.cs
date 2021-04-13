using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;

#pragma warning disable 649
// ReSharper disable UnassignedReadonlyField

namespace Ship_Game
{
    [StarDataType]
    public class TypeWeight // Created by Fat Bastard - to select odds of planet category to be created per sun zone
    {
        [StarData] public readonly PlanetCategory Type; // Barren
        [StarData] public readonly int Count; // 10
    }
    
    [StarDataType]
    public class SunZoneData
    {
        [StarData] readonly SunZone Zone;
        [StarData] readonly Array<TypeWeight> Distribution;

        public static Array<PlanetCategory> CreateDistribution(Array<SunZoneData> sunZones, SunZone sunZone)
        {
            /**
             * CreateWeights will take each planet category from the yaml and
             * create an array consisting a number of value corresponding to
             * the weights per category in the yaml. For instance, if SunZone.Near
             * contains Volcanic weight of 20 and Barren weight of 10, an array of
             * size 30 will be created. It will contain 20 volcanic and 10 barren enums.
             * Then 1 category can be chosen randomly from this array for star system creation
             */
            var categories = new  Array<PlanetCategory>();

            Array<TypeWeight> weights = sunZones.Find(s => s.Zone == sunZone).Distribution;
            foreach (TypeWeight w in weights)
            {
                int numCategoriesToCreate = w.Count;
                for (int i = 0; i < numCategoriesToCreate; i++)
                {
                    categories.Add(w.Type);
                }
            }
            return categories;
        }
    }
}