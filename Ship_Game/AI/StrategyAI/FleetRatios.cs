using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;

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
            SetFleetRatios();


        }

        public void SetFleetRatios()
        {
            // fighters, corvettes, frigate, cruisers, capitals, troopShip,bombers,carriers,support

            int[] counts;

            if      (OwnerEmpire.canBuildCapitals)    counts = ResourceManager.GetFleetRatios(BuildRatio.CanBuildCapitals);
            else if (OwnerEmpire.CanBuildBattleships) counts = ResourceManager.GetFleetRatios(BuildRatio.CanBuildBattleships);
            else if (OwnerEmpire.canBuildCruisers)    counts = ResourceManager.GetFleetRatios(BuildRatio.CanBuildCruisers);
            else if (OwnerEmpire.canBuildFrigates)    counts = ResourceManager.GetFleetRatios(BuildRatio.CanBuildFrigates);
            else if (OwnerEmpire.canBuildCorvettes)   counts = ResourceManager.GetFleetRatios(BuildRatio.CanBuildCorvettes);
            else                                      counts = ResourceManager.GetFleetRatios(BuildRatio.CanBuildFighters);

            SetCounts(counts);
            CountIndexed = counts;
        }

        private void SetCounts(int[] counts)
        {
            MinFighters    = counts[0];
            MinCorvettes   = counts[1];
            MinFrigates    = counts[2];
            MinCruisers    = counts[3];
            MinBattleships = counts[4];
            MinCapitals    = counts[5];
            MinTroopShip   = counts[6];
            MinBombers     = counts[7];
            MinCarriers    = counts[8];
            MinSupport     = counts[9];

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
                               + MinBattleships + MinCapitals);
            TotalCount = MinCombatFleet + MinBombers + MinTroopShip + MinSupport + MinCarriers;

        }

        public float GetWanted(ShipData.RoleName role) => GetWanted(EmpireAI.RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(role));
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

        public static int CombatRoleToRatio(ShipData.RoleName role)
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