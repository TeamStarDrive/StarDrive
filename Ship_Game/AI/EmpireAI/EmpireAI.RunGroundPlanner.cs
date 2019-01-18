using System.Linq;
using Ship_Game.Commands.Goals;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        private void RunGroundPlanner()
        {
            if (DefensiveCoordinator.UniverseWants > .8)
                return;
            float totalideal  = 0;
            float totalwanted = 0;

            IOrderedEnumerable<Troop> troopTemplates = ResourceManager.GetTroopTemplates()
                .Where(t => OwnerEmpire.WeCanBuildTroop(t.Name))
                .OrderBy(t => t.ActualCost);
            Troop lowCostTroop = troopTemplates.FirstOrDefault();
            Troop highCostTroop = troopTemplates.LastOrDefault();
            Troop troop = highCostTroop;

            foreach (SolarSystem system in OwnerEmpire.GetOwnedSystems())
            {
                SystemCommander defenseSystem = DefensiveCoordinator.DefenseDict[system];

                if (defenseSystem.TroopStrengthNeeded <= 0)                
                    continue;
                
                totalwanted += defenseSystem.TroopStrengthNeeded;
                totalideal += defenseSystem.IdealTroopCount; 
            }
            if (totalwanted / totalideal > .5f)            
                troop = lowCostTroop;
            
            if (totalwanted / totalideal <= .1f)
                return;
            Planet targetBuild = OwnerEmpire.GetPlanets()
                .Where(planet => planet.AllowInfantry && planet.colonyType != Planet.ColonyType.Research
                                 && planet.Prod.NetMaxPotential > 5
                                 && (planet.ProdHere) - 2 * (planet.ConstructionQueue.Where(
                                         qi => qi.Goal != null && qi.Goal.type == GoalType.BuildTroop)
                                     .Sum(qi => qi.Cost)) > 0 
                )
                .OrderBy(p => !p.HasSpacePort)
                .ThenByDescending(p => p.Prod.GrossIncome)
                .FirstOrDefault();
            if (targetBuild == null)
                return;


            var g = new BuildTroop(troop, OwnerEmpire, targetBuild);
            Goals.Add(g);
        }
    }
}