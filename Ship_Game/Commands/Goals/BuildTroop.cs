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

        private GoalStep FindPlanetToBuildAt()
        {
            Troop troopTemplate = ResourceManager.GetTroopTemplate(ToBuildUID);
            PlanetBuildingAt.ConstructionQueue.Add(new QueueItem(PlanetBuildingAt)
            {
                isTroop = true,
                QueueNumber = PlanetBuildingAt.ConstructionQueue.Count,
                troopType = ToBuildUID,
                Goal = this,
                Cost = troopTemplate.ActualCost
            });
            return GoalStep.GoToNextStep;
        }
    }
}
