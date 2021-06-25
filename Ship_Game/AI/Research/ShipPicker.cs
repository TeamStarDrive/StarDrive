using System.Collections.Generic;
using Ship_Game.Ships;
using static Ship_Game.AI.Research.ResearchOptions.ShipCosts;

namespace Ship_Game.AI.Research
{
    public class ShipPicker
    {
        public ShipPicker() {}

        public Ship FindCheapestShipInList(Empire empire, Array<Ship> ships, HashSet<string> nonShipTechs, ResearchOptions options)
        {
            float avgNonShipTechCost = float.MaxValue;

            foreach (string techName in nonShipTechs)
            {
                var tech = empire.GetTechEntry(techName);
                float techCost = tech.Tech.ActualCost;
                if (!tech.Unlocked && tech.Tech.RootNode == 0 && avgNonShipTechCost > techCost)
                    avgNonShipTechCost = techCost;
            }
            
            // find cheapest ship to research in current set of ships. 
            // adjust cost of some techs to make ships more or less wanted. 
            var pickedShip = ships.FindMin(s =>
            {
                float techScore = 0;
                foreach (string techName in s.shipData.TechsNeeded)
                {
                    var tech = empire.GetTechEntry(techName);
                    if (!tech.Unlocked && tech.Tech.RootNode == 0)
                    {
                        var cost = tech.Tech.ActualCost;

                        if (tech.IsTechnologyType(TechnologyType.GroundCombat))              cost *= options.CostMultiplier(GroundCombat);
                        if (tech.IsTechnologyType(TechnologyType.ShipHull))                  cost *= options.CostMultiplier(AllHulls);

                        techScore += cost * options.CostMultiplier(tech);
                    }
                }
                
                switch (s.DesignRole)
                {
                    case ShipData.RoleName.platform:
                    case ShipData.RoleName.station:                                     techScore *= options.CostMultiplier(Orbitals);   break;
                    case ShipData.RoleName.colony:                                      techScore *= options.CostMultiplier(ColonyShip); break;
                    case ShipData.RoleName.freighter:                                   techScore *= options.CostMultiplier(Freighter);  break;
                    case ShipData.RoleName.troopShip when !empire.canBuildTroopShips:   techScore *= options.CostMultiplier(TroopShip);  break;
                    case ShipData.RoleName.support   when !empire.canBuildSupportShips: techScore *= options.CostMultiplier(Support);    break;
                    case ShipData.RoleName.bomber    when !empire.canBuildBombers:      techScore *= options.CostMultiplier(Bomber);     break;
                    case ShipData.RoleName.carrier   when !empire.canBuildCarriers:     techScore *= options.CostMultiplier(Carrier);    break;
                }
                float costRatio  = techScore / avgNonShipTechCost;
                float randomBase = techScore * options.CostMultiplier(Randomize);
                float random     = randomBase > 0 ? RandomMath.AvgRandomBetween(-randomBase, randomBase) : 0;
                return (int)(techScore * costRatio + random);
            });
            return pickedShip;
        }
    }
}