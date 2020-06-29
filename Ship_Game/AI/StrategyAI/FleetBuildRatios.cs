using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;

#pragma warning disable 649
// ReSharper disable UnassignedReadonlyField

namespace Ship_Game
{
    [StarDataType]
    public class FleetBuildRatios
    {
        [StarDataKey] readonly BuildRatio CanBuild;
        [StarData] readonly int Fighters;
        [StarData] readonly int Corvettes;
        [StarData] readonly int Frigates;
        [StarData] readonly int Cruisers;
        [StarData] readonly int Capitals;
        [StarData] readonly int TroopShips;
        [StarData] readonly int Bombers;
        [StarData] readonly int Carriers;
        [StarData] readonly int Support;

        public static int[] GetRatiosFor(Array<FleetBuildRatios> counts, BuildRatio canBuild)
        {
            var hullTypeCount = counts.Find(c => c.CanBuild == BuildRatio.CanBuildFighters);
            int[] CountsForWhatWeCanBuild = new int[]
            {
                hullTypeCount.Fighters,
                hullTypeCount.Corvettes,
                hullTypeCount.Frigates,
                hullTypeCount.Cruisers,
                hullTypeCount.Capitals,
                hullTypeCount.TroopShips,
                hullTypeCount.Bombers,
                hullTypeCount.Carriers,
                hullTypeCount.Support
            };

            return CountsForWhatWeCanBuild;
        }
    }
}