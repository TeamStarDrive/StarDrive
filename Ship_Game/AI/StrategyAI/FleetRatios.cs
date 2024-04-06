using System;
using System.Windows.Forms;
using SDGraphics;
using Ship_Game.ExtensionMethods;
using Ship_Game.Ships;
using Ship_Game.Utils;

namespace Ship_Game.AI
{
    public enum BuildRatio
    {
        CanBuildFighters,
        CanBuildCorvettes,
        CanBuildFrigates,
        CanBuildCruisers,
        CanBuildBattleships,
        CanBuildCapitals
    }

    public struct FleetRatios
    {
        public float TotalCount { get; set; }
        public float MinFighters { get; set; }
        public float MinCorvettes { get; set; }
        public float MinFrigates { get; set; }
        public float MinCruisers { get; set; }
        public float MinBattleships { get; set; }
        public float MinCapitals { get; set; }
        public float MinTroopShip { get; set; }
        public float MinBombers { get; set; }
        public float MinCarriers { get; set; }
        public float MinSupport { get; set; }
        public int MinCombatFleet { get; set; }

        int[] CountIndexed;
        public Empire OwnerEmpire { get; }

        public FleetRatios(Empire empire)
        {
            OwnerEmpire    = empire;
            CountIndexed   = new int[10];
            TotalCount     = 0;
            MinFighters    = 0;
            MinCorvettes   = 0;
            MinFrigates    = 0;
            MinCruisers    = 0;
            MinBattleships = 0;
            MinCapitals    = 0;
            MinTroopShip   = 0;
            MinBombers     = 0;
            MinCarriers    = 0;
            MinSupport     = 0;
            MinCombatFleet = 0;
            SetFleetRatios(empire.Universe.P.EnableRandomizedAIFleetSizes);


        }

        public void SetFleetRatios(bool useRandomFleetSizes)
        {
            // fighters, corvettes, frigate, cruisers, capitals, troopShip,bombers,carriers,support

            Range[] counts;

            if      (OwnerEmpire.CanBuildCapitals)    counts = ResourceManager.GetFleetRatios(BuildRatio.CanBuildCapitals);
            else if (OwnerEmpire.CanBuildBattleships) counts = ResourceManager.GetFleetRatios(BuildRatio.CanBuildBattleships);
            else if (OwnerEmpire.CanBuildCruisers)    counts = ResourceManager.GetFleetRatios(BuildRatio.CanBuildCruisers);
            else if (OwnerEmpire.CanBuildFrigates)    counts = ResourceManager.GetFleetRatios(BuildRatio.CanBuildFrigates);
            else if (OwnerEmpire.CanBuildCorvettes)   counts = ResourceManager.GetFleetRatios(BuildRatio.CanBuildCorvettes);
            else                                      counts = ResourceManager.GetFleetRatios(BuildRatio.CanBuildFighters);


            SetCounts(counts, useRandomFleetSizes);            
        }

        private void SetCounts(Range[] counts, bool IsRandomFleetSize)
        {
            if(IsRandomFleetSize) 
            {
                SeededRandom seededRandom = new SeededRandom();

                MinFighters    = CountIndexed[0] = (int) counts[0].Generate(seededRandom);
                MinCorvettes   = CountIndexed[1] = (int) counts[1].Generate(seededRandom);
                MinFrigates    = CountIndexed[2] = (int) counts[2].Generate(seededRandom);
                MinCruisers    = CountIndexed[3] = (int) counts[3].Generate(seededRandom);
                MinBattleships = CountIndexed[4] = (int) counts[4].Generate(seededRandom);
                MinCapitals    = CountIndexed[5] = (int) counts[5].Generate(seededRandom);
                MinTroopShip   = CountIndexed[6] = (int) counts[6].Generate(seededRandom);
                MinBombers     = CountIndexed[7] = (int) counts[7].Generate(seededRandom);
                MinCarriers    = CountIndexed[8] = (int) counts[8].Generate(seededRandom);
                MinSupport     = CountIndexed[9] = (int) counts[9].Generate(seededRandom);
            }
            else
            {
                MinFighters    = CountIndexed[0] = (int) counts[0].Min;
                MinCorvettes   = CountIndexed[1] = (int) counts[1].Min;
                MinFrigates    = CountIndexed[2] = (int) counts[2].Min;
                MinCruisers    = CountIndexed[3] = (int) counts[3].Min;
                MinBattleships = CountIndexed[4] = (int) counts[4].Min;
                MinCapitals    = CountIndexed[5] = (int) counts[5].Min;
                MinTroopShip   = CountIndexed[6] = (int) counts[6].Min;
                MinBombers     = CountIndexed[7] = (int) counts[7].Min;
                MinCarriers    = CountIndexed[8] = (int) counts[8].Min;
                MinSupport     = CountIndexed[9] = (int) counts[9].Min;
            }

            if (!OwnerEmpire.CanBuildTroopShips)
                MinTroopShip = 0;

            if (!OwnerEmpire.CanBuildBombers)
                MinBombers = 0;

            if (!OwnerEmpire.CanBuildCarriers)
                MinCarriers = 0;

            if (!OwnerEmpire.CanBuildSupportShips)
                MinSupport = 0;

            MinCombatFleet = (int)(MinFighters + MinCorvettes + MinFrigates + MinCruisers
                               + MinBattleships + MinCapitals);
            TotalCount = MinCombatFleet + MinBombers + MinTroopShip + MinSupport + MinCarriers;
        }

        public float GetWanted(RoleName role) => GetWanted(EmpireAI.RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(role));
        public float GetWanted(EmpireAI.RoleBuildInfo.RoleCounts.CombatRole role)
        {
            int index = CombatRoleToRatio(role);
            if (index == -1) return -1;
            return CountIndexed[index];
        }

        public int MaxCombatRoleIndex()
        {
            int max =0;
            for (int x =0; x < 5; x++)
            {
                max = CountIndexed[x] > 0 ? x : max;
            }
            return max;
        }

        public static int CombatRoleToRatio(RoleName role)
        {
            return CombatRoleToRatio(EmpireAI.RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(role));
        } 

        public static int CombatRoleToRatio(EmpireAI.RoleBuildInfo.RoleCounts.CombatRole role)
        {
            switch (role)
            {
                case EmpireAI.RoleBuildInfo.RoleCounts.CombatRole.Fighter:    return 0;
                case EmpireAI.RoleBuildInfo.RoleCounts.CombatRole.Corvette:   return 1;
                case EmpireAI.RoleBuildInfo.RoleCounts.CombatRole.Frigate:    return 2;
                case EmpireAI.RoleBuildInfo.RoleCounts.CombatRole.Cruiser:    return 3;
                case EmpireAI.RoleBuildInfo.RoleCounts.CombatRole.Battleship: return 4;
                case EmpireAI.RoleBuildInfo.RoleCounts.CombatRole.Capital:    return 5;
                case EmpireAI.RoleBuildInfo.RoleCounts.CombatRole.TroopShip:  return 6;
                case EmpireAI.RoleBuildInfo.RoleCounts.CombatRole.Bomber:     return 7;
                case EmpireAI.RoleBuildInfo.RoleCounts.CombatRole.Carrier:    return 8;
                case EmpireAI.RoleBuildInfo.RoleCounts.CombatRole.Support:    return 9;
            }
            return -1;
        }
    }
}