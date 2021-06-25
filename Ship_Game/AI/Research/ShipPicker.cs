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

                        if (tech.IsTechnologyType(TechnologyType.GroundCombat))              cost *= options.GetShipMod(GroundCombat);
                        if (tech.IsTechnologyType(TechnologyType.ShipHull))                  cost *= options.GetShipMod(AllHulls);

                        techScore += cost * options.GetUIDMod(techName) * options.GetAnyTypeMod(tech);
                    }
                }
                
                switch (s.DesignRole)
                {
                    case ShipData.RoleName.platform:
                    case ShipData.RoleName.station:                                     techScore *= options.GetShipMod(Orbitals);   break;
                    case ShipData.RoleName.colony:                                      techScore *= options.GetShipMod(ColonyShip); break;
                    case ShipData.RoleName.freighter:                                   techScore *= options.GetShipMod(Freighter);  break;
                    case ShipData.RoleName.troopShip when !empire.canBuildTroopShips:   techScore *= options.GetShipMod(TroopShip);  break;
                    case ShipData.RoleName.support   when !empire.canBuildSupportShips: techScore *= options.GetShipMod(Support);    break;
                    case ShipData.RoleName.bomber    when !empire.canBuildBombers:      techScore *= options.GetShipMod(Bomber);     break;
                    case ShipData.RoleName.carrier   when !empire.canBuildCarriers:     techScore *= options.GetShipMod(Carrier);    break;
                }
                float costRatio = techScore / avgNonShipTechCost;
                return techScore * costRatio;
            });
            return pickedShip;
        }

        /*
         *              { "SHIPTECH",     Randomizer(threat,                   1f)          },
                { "Research",     Randomizer(strat.ResearchRatio + 1,  ResearchDebt)},
                { "Colonization", Randomizer(strat.ExpansionRatio + 1, FoodNeeds)   },
                { "Economic",     Randomizer(strat.ExpansionRatio + 1, Economics)   },
                { "Industry",     Randomizer(strat.IndustryRatio + 1,  Industry)    },
                { "General",      Randomizer(strat.ResearchRatio + 1,  0)           },
                { "GroundCombat", Randomizer(strat.MilitaryRatio + 1,  threat)},
         */
    }
}