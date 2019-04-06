using System.Linq;
using Ship_Game.Commands.Goals;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        void RunGroundPlanner()
        {
            if (DefensiveCoordinator.TroopsToTroopsWantedRatio > 0.9f)
                return;
            var troopGoal = SearchForGoals(GoalType.BuildTroop);
            if (troopGoal.Count > 2)
                return;

            Troop[] troops = ResourceManager.GetTroopTemplates()
                            .Filter(t => OwnerEmpire.WeCanBuildTroop(t.Name))
                            .Sorted(t => t.ActualCost);
            if (troops.Length == 0)
            {
                Log.Warning($"EmpireAI GroundPlanner no Troops for {EmpireName}");
                return;
            }

            Troop loCost = troops.First();
            Troop hiCost = troops.Last();
            Troop chosenTroop = DefensiveCoordinator.TroopsToTroopsWantedRatio > 0.5f ? hiCost : loCost;
            Goals.Add(new BuildTroop(chosenTroop, OwnerEmpire));
        }
    }
}