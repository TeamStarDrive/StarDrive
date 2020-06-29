using System;
using Microsoft.Xna.Framework;

namespace Ship_Game.AI
{
    public enum BuildRatio
    {
        CanBuildFighters,
        CanBuildCorvettes,
        CanBuildFrigates,
        CanBuildCruisers,
        CanBuildCapitals
    }

    public struct FleetRatios
    {
        public float TotalCount { get; set; }
        public float MinFighters { get; set; }
        public float MinCorvettes { get; set; }
        public float MinFrigates { get; set; }
        public float MinCruisers { get; set; }
        public float MinCapitals { get; set; }
        public float MinTroopShip { get; set; }
        public float MinBombers { get; set; }
        public float MinCarriers { get; set; }
        public float MinSupport { get; set; }
        public int MinCombatFleet { get; set; }

        public Empire OwnerEmpire { get; }

        public FleetRatios(Empire empire)
        {
            OwnerEmpire    = empire;

            TotalCount     = 0;
            MinFighters    = 0;
            MinCorvettes   = 0;
            MinFrigates    = 0;
            MinCruisers    = 0;
            MinCapitals    = 0;
            MinTroopShip   = 0;
            MinBombers     = 0;
            MinCarriers    = 0;
            MinSupport     = 0;
            MinCombatFleet = 0;
            SetFleetRatios();


        }

        public void SetFleetRatios()
        {
            // fighters, corvettes, frigate, cruisers, capitals, troopShip,bombers,carriers,support
            if (OwnerEmpire.canBuildCapitals)
                SetCounts(ResourceManager.BuildRatios[BuildRatio.CanBuildCapitals]);

            else if (OwnerEmpire.canBuildCruisers)
                SetCounts(ResourceManager.BuildRatios[BuildRatio.CanBuildCruisers]);

            else if (OwnerEmpire.canBuildFrigates)
                SetCounts(ResourceManager.BuildRatios[BuildRatio.CanBuildFrigates]);

            else if (OwnerEmpire.canBuildCorvettes)
                SetCounts(ResourceManager.BuildRatios[BuildRatio.CanBuildCorvettes]);
            else
                SetCounts(ResourceManager.BuildRatios[BuildRatio.CanBuildFighters]);
        }

        private void SetCounts(int[] counts)
        {
            MinFighters  = counts[0];
            MinCorvettes = counts[1];
            MinFrigates  = counts[2];
            MinCruisers  = counts[3];
            MinCapitals  = counts[4];
            MinTroopShip = counts[5];
            MinBombers   = counts[6];
            MinCarriers  = counts[7];
            MinSupport   = counts[8];

            if (!OwnerEmpire.canBuildTroopShips)
                MinTroopShip = 0;

            if (!OwnerEmpire.canBuildBombers)
                MinBombers = 0;

            if (OwnerEmpire.canBuildCarriers)
                MinFighters = 0;
            else
                MinCarriers = 0;

            if (!OwnerEmpire.canBuildSupportShips)
                MinSupport = 0;

            MinCombatFleet = (int)(MinFighters + MinCorvettes + MinFrigates + MinCruisers
                               + MinCapitals);
            TotalCount = MinCombatFleet + MinBombers + MinTroopShip + MinSupport + MinCarriers;

        }
    }
}