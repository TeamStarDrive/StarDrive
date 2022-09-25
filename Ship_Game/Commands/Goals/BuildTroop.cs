using System;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class BuildTroop : Goal
    {
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        [StarData] string TroopName;

        [StarDataConstructor]
        public BuildTroop(Empire owner) : base(GoalType.BuildTroop, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitForTroopCompletion
            };
        }

        public BuildTroop(Troop toCopy, Empire owner) : this(owner)
        {
            TroopName = toCopy.Name;
            if (TroopName.IsEmpty())
                Log.Error($"Missing Troop for empire {owner}");
        }

        GoalStep FindPlanetToBuildAt()
        {
            float troopRatio = Owner.AI.DefensiveCoordinator.TroopsToTroopsWantedRatio;
            if (troopRatio.GreaterOrEqual(1))
                return GoalStep.GoalFailed;

            // find a planet
            Troop troopTemplate = ResourceManager.GetTroopTemplate(TroopName);
            if (Owner.FindPlanetToBuildTroopAt(Owner.MilitaryOutposts, troopTemplate, 0.1f, out Planet planet))
            {
                // submit troop into queue
                // let the colony governor prioritize troops
                PlanetBuildingAt = planet;
                planet.Construction.Enqueue(troopTemplate, this);
                return GoalStep.GoToNextStep;
            }

            return GoalStep.GoalFailed;
        }

        GoalStep WaitForTroopCompletion()
        {
            if (IsMainGoalCompleted)
                return GoalStep.GoalComplete;

            if (PlanetBuildingAt.Owner != Owner || !PlanetBuildingAt.Construction.ContainsTroopWithGoal(this))
                return GoalStep.GoalFailed;

            return GoalStep.TryAgain;
        }
    }
}
