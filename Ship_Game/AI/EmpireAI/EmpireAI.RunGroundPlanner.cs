using System.Linq;
using Ship_Game.Commands.Goals;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        void RunGroundPlanner()
        {
            if (DefensiveCoordinator.TroopsToTroopsWantedRatio > 0.95f)
                return;

            VerifyTroopGoals(out int troopGoals);
            if (troopGoals > (OwnerEmpire.GetPlanets().Count / 10).LowerBound(2))
                return;

            Troop[] troops = ResourceManager.GetTroopTemplatesFor(OwnerEmpire);
            if (troops.Length == 0)
            {
                Log.Warning($"EmpireAI GroundPlanner no Troops for {EmpireName}");
                return;
            }

            Troop loCost = troops.First();
            Troop hiCost = troops.Last();
            Troop chosenTroop = DefensiveCoordinator.TroopsToTroopsWantedRatio > 0.7f ? hiCost : loCost;
            Goals.Add(new BuildTroop(chosenTroop, OwnerEmpire));
        }
    }
}