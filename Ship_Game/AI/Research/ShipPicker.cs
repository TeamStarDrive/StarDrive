using System.Collections.Generic;
using Ship_Game.Ships;

namespace Ship_Game.AI.Research
{
    public class ShipPicker
    {
        public ShipPicker() {}

        public Ship FindCheapestShipInList(Empire empire, Array<Ship> ships, HashSet<string> nonShipTechs)
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

                        if (tech.IsTechnologyType(TechnologyType.Economic) && !s.isColonyShip) cost *= 2f;
                        if (tech.IsTechnologyType(TechnologyType.ShipHull))                    cost *= 2f;
                        if (tech.IsTechnologyType(TechnologyType.GroundCombat))                cost *= 0.95f;

                        techScore += cost;
                    }
                }
                
                switch (s.DesignRole)
                {
                    case ShipData.RoleName.platform:
                    case ShipData.RoleName.station:                                     techScore *= 1.25f;  break;
                    case ShipData.RoleName.colony:                                      techScore *= 0.95f; break;
                    case ShipData.RoleName.freighter:                                   techScore *= 1.25f; break;
                    case ShipData.RoleName.troopShip when !empire.canBuildTroopShips:   techScore *= 0.95f; break;
                    case ShipData.RoleName.support   when !empire.canBuildSupportShips: techScore *= 0.95f; break;
                    case ShipData.RoleName.bomber    when !empire.canBuildBombers:      techScore *= 0.95f; break;
                    case ShipData.RoleName.carrier   when !empire.canBuildCarriers:     techScore *= 0.95f; break;
                }
                float costRatio = techScore / avgNonShipTechCost;
                return techScore * costRatio;
            });
            return pickedShip;
        }
    }
}