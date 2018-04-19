using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                DummyStepTryAgain,
                DummyStepGoalComplete,
            };
        }
        public BuildTroop(Troop toCopy, Empire owner, Planet p) : this()
        {
            PlanetBuildingAt = p;
            ToBuildUID = toCopy.Name;
            empire = owner;
        }

        private GoalStep FindPlanetToBuildAt()
        {
            if (ToBuildUID != null)
            {
                Troop troopTemplate = ResourceManager.GetTroopTemplate(ToBuildUID);
                PlanetBuildingAt.ConstructionQueue.Add(new QueueItem()
                {
                    isTroop = true,
                    QueueNumber = PlanetBuildingAt.ConstructionQueue.Count,
                    troopType = ToBuildUID,
                    Goal = this,
                    Cost = troopTemplate.GetCost()
                });
            }
            else Log.Info("Missing Troop {0}", ToBuildUID);
            return GoalStep.GoToNextStep;
        }
    }
}
