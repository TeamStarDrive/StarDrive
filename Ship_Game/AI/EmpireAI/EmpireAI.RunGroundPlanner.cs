using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {
        private void RunGroundPlanner()
        {
            if (DefensiveCoordinator.UniverseWants > .8)
                return;
            float totalideal = 0;
            float totalwanted = 0;

            IEnumerable<Troop> troopTemplates = ResourceManager.GetTroopTemplates()
                .Where(t => OwnerEmpire.WeCanBuildTroop(t.Name))
                .OrderBy(t => t.Cost);
            Troop lowCostTroop = troopTemplates.FirstOrDefault();
            Troop highCostTroop = troopTemplates.LastOrDefault();
            Troop troop = highCostTroop;

            foreach (SolarSystem system in this.OwnerEmpire.GetOwnedSystems())
            {
                SystemCommander defenseSystem = this.DefensiveCoordinator.DefenseDict[system];
                //int planetcount = system.PlanetList.Where(planet => planet.Owner == empire).Count();
                //planetcount = planetcount == 0 ? 1 : planetcount;

                if (defenseSystem.TroopStrengthNeeded <= 0)
                {
                    continue;
                }
                totalwanted += defenseSystem.TroopStrengthNeeded; // >0 ?defenseSystem.TroopStrengthNeeded : 1;
                totalideal += defenseSystem.IdealTroopCount; // >0 ? defenseSystem.IdealTroopStr : 1;
            }
            if (totalwanted / totalideal > .5f)
            {
                troop = lowCostTroop;
            }
            if (totalwanted / totalideal <= .1f)
                return;
            Planet targetBuild = this.OwnerEmpire.GetPlanets()
                .Where(planet => planet.AllowInfantry && planet.colonyType != Planet.ColonyType.Research
                                 && planet.GetMaxProductionPotential() > 5
                                 && (planet.ProductionHere) - 2 * (planet.ConstructionQueue.Where(
                                         goal => goal.Goal != null
                                                 && goal.Goal.type == GoalType.BuildTroop)
                                     .Sum(cost => cost.Cost)) > 0 //10 turns to build curremt troops in queue
                )
                .OrderBy(noshipyard => !noshipyard.HasShipyard)
                .ThenByDescending(build => build.GrossProductionPerTurn)
                .FirstOrDefault();
            if (targetBuild == null)
                return;


            Goal g = new Goal(troop, this.OwnerEmpire, targetBuild);
            this.Goals.Add(g);
        }
    }
}