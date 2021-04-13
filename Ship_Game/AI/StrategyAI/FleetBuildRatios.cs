using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;

#pragma warning disable 649
// ReSharper disable UnassignedReadonlyField

namespace Ship_Game
{
    [StarDataType]
    public class FleetBuildRatios
    {
        [StarDataKeyValue] readonly BuildRatio CanBuild;
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

        public static int[] GetRatiosFor(Array<FleetBuildRatios> ratios, BuildRatio canBuild)
        {
            FleetBuildRatios matchingRatios = ratios.Find(c => c.CanBuild == canBuild);
            if (matchingRatios == null)
                throw new Exception($"Failed to find matching FleetBuildRatios for {canBuild}");

            int[] countsForWhatWeCanBuild =
            {
                matchingRatios.Fighters,
                matchingRatios.Corvettes,
                matchingRatios.Frigates,
                matchingRatios.Cruisers,
                matchingRatios.Battleships,
                matchingRatios.Capitals,
                matchingRatios.TroopShips,
                matchingRatios.Bombers,
                matchingRatios.Carriers,
                matchingRatios.Support
            };

            return countsForWhatWeCanBuild;
        }
    }
}