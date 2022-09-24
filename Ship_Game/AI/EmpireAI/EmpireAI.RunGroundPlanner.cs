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
            int buildPlanets = OwnerEmpire.GetBestPortsForShipBuilding(OwnerEmpire.MilitaryOutposts, portQuality: 0.2f).Count;
            if (NumTroopGoals() > buildPlanets)
                return;

            Troop[] troops = ResourceManager.GetTroopTemplatesFor(OwnerEmpire);
            if (troops.Length == 0)
            {
                Log.Warning($"EmpireAI GroundPlanner no Troops for {OwnerEmpire.data.Traits.Name}");
                return;
            }

            Troop loCost = troops.First();
            Troop hiCost = troops.Last();
            Troop chosenTroop = DefensiveCoordinator.TroopsToTroopsWantedRatio > 0.7f ? hiCost : loCost;
            AddGoal(new BuildTroop(chosenTroop, OwnerEmpire));
        }
    }
}