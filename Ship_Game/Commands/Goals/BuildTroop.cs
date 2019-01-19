using System;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class BuildTroop : Goal
    {
        public const string ID = "Build Troop";
        public override string UID => ID;

        public BuildTroop() : base(GoalType.BuildTroop)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitMainGoalCompletion
            };
        }
        public BuildTroop(Troop toCopy, Empire owner, Planet p) : this()
        {
            PlanetBuildingAt = p;
            ToBuildUID = toCopy.Name;
            empire = owner;
            if (ToBuildUID.IsEmpty())
                Log.Warning($"Missing Troop {ToBuildUID}");
        }

        GoalStep FindPlanetToBuildAt()
        {
            Troop troopTemplate = ResourceManager.GetTroopTemplate(ToBuildUID);
            PlanetBuildingAt.Construction.AddTroop(troopTemplate, this);
            return GoalStep.GoToNextStep;
        }
    }
}
