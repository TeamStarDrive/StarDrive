using System.Linq;
using Ship_Game.Commands.Goals;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        void RunGroundPlanner()
        {
            if (DefensiveCoordinator.UniverseWants > 0.9f)
                return;
            var troopGoal = SearchForGoals(GoalType.BuildTroop);
            if (troopGoal.Count > 3) 
                return;

            Troop[] troops = ResourceManager.GetTroopTemplates()
                            .Filter(t => OwnerEmpire.WeCanBuildTroop(t.Name))
                            .Sorted(t => t.ActualCost);
            if (troops.Length == 0)
            {
                Log.Warning($"EmpireAI GroundPlanner no Troops for {EmpireName}");
                return;
            }

            float totalIdeal  = 0f;
            float totalWanted = 0f;
            foreach (SolarSystem sys in OwnerEmpire.GetOwnedSystems())
            {
                if (!DefensiveCoordinator.DefenseDict.Get(sys, out SystemCommander commander)
                    || commander.TroopStrengthNeeded <= 0f)
                    continue;
                totalWanted += commander.TroopStrengthNeeded;
                totalIdeal  += commander.IdealTroopCount;
            }

            float requiredRatio = totalWanted / totalIdeal;
            if (requiredRatio <= 0.1f)
                return;

            Troop loCost = troops.First();
            Troop hiCost = troops.Last();
            Troop chosenTroop = requiredRatio > 0.5f ? hiCost : loCost;
            Goals.Add(new BuildTroop(chosenTroop, OwnerEmpire));
        }
    }
}