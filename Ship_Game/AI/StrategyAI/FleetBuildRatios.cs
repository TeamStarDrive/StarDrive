using Ship_Game.AI;
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
        [StarData] readonly int Battleships;
        [StarData] readonly int Capitals;
        [StarData] readonly int TroopShips;
        [StarData] readonly int Bombers;
        [StarData] readonly int Carriers;
        [StarData] readonly int Support;

        public static int[] GetRatiosFor(Array<FleetBuildRatios> counts, BuildRatio canBuild)
        {
            var hullTypeCount = counts.Find(c => c.CanBuild == canBuild);
            int[] countsForWhatWeCanBuild =
            {
                hullTypeCount.Fighters,
                hullTypeCount.Corvettes,
                hullTypeCount.Frigates,
                hullTypeCount.Cruisers,
                hullTypeCount.Battleships,
                hullTypeCount.Capitals,
                hullTypeCount.TroopShips,
                hullTypeCount.Bombers,
                hullTypeCount.Carriers,
                hullTypeCount.Support
            };

            return countsForWhatWeCanBuild;
        }
    }
}