using System;
using System.Linq;
using Ship_Game.AI;
using Ship_Game.Universe;

namespace Ship_Game.Commands.Goals
{
    public class BuildTroop : Goal
    {
        public const string ID = "Build Troop";
        public override string UID => ID;

        public BuildTroop(int id, UniverseState us)
            : base(GoalType.BuildTroop, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitForTroopCompletion
            };
        }

        public BuildTroop(Troop toCopy, Empire owner)
            : this(owner.Universum.CreateId(), owner.Universum)
        {
            ToBuildUID = toCopy.Name;
            empire = owner;
            if (ToBuildUID.IsEmpty())
                Log.Error($"Missing Troop {ToBuildUID}");

            Evaluate();
        }

        GoalStep FindPlanetToBuildAt()
        {
            float troopRatio = empire.GetEmpireAI().DefensiveCoordinator.TroopsToTroopsWantedRatio;
            if (troopRatio.GreaterOrEqual(1))
                return GoalStep.GoalFailed;

            // find a planet
            Troop troopTemplate = ResourceManager.GetTroopTemplate(ToBuildUID);
            if (empire.FindPlanetToBuildTroopAt(empire.MilitaryOutposts, troopTemplate, 0.1f, out Planet planet))
            {
                // submit troop into queue
                // let the colony governor prioritize troops
                planet.Construction.Enqueue(troopTemplate, this);

                PlanetBuildingAt = planet;
                return GoalStep.GoToNextStep;
            }

            return GoalStep.GoalFailed;
        }

        GoalStep WaitForTroopCompletion()
        {
            if (IsMainGoalCompleted)
                return GoalStep.GoalComplete;

            if (PlanetBuildingAt.Owner != empire || !PlanetBuildingAt.Construction.ContainsTroopWithGoal(this))
                return GoalStep.GoalFailed;

            return GoalStep.TryAgain;
        }
    }
}
