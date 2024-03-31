using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;

#pragma warning disable 649
// ReSharper disable UnassignedReadonlyField

namespace Ship_Game
{
    [StarDataType]
    public class FleetBuildRatios
    {
        [StarData] readonly BuildRatio CanBuild;
        [StarData] readonly Range Fighters;
        [StarData] readonly Range Corvettes;
        [StarData] readonly Range Frigates;
        [StarData] readonly Range Cruisers;
        [StarData] readonly Range Battleships;
        [StarData] readonly Range Capitals;
        [StarData] readonly Range TroopShips;
        [StarData] readonly Range Bombers;
        [StarData] readonly Range Carriers;
        [StarData] readonly Range Support;

        public static Range[] GetRatiosFor(Array<FleetBuildRatios> ratios, BuildRatio canBuild)
        {
            FleetBuildRatios matchingRatios = ratios.Find(c => c.CanBuild == canBuild);
            if (matchingRatios == null)
                throw new Exception($"Failed to find matching FleetBuildRatios for {canBuild}");

            Range[] countsForWhatWeCanBuild =
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