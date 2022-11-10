using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class BuildScout : Goal
    {
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        [StarData] public sealed override BuildableShip Build { get; set; }
        public override IShipDesign ToBuild => Build.Template;

        [StarDataConstructor] BuildScout() : base(GoalType.BuildScout, null)
        {
            InitSteps();
        }

        public BuildScout(Empire owner) : base(GoalType.BuildScout, owner)
        {
            InitSteps();
            IShipDesign scout = Owner.ChooseScoutShipToBuild();
            Build = new(scout);
        }

        void InitSteps()
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitForShipBuilt,
                OrderExplore
            };
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, ToBuild, out Planet buildAt))
                return GoalStep.TryAgain;

            PlanetBuildingAt = buildAt;
            buildAt.Construction.Enqueue(Build.Template, this, notifyOnEmpty: false);
            int priority = Owner.GetPlanets().Count / 3;
            buildAt.Construction.PrioritizeShip(Build.Template, priority, priority * 2);

            return GoalStep.GoToNextStep;
        }
       
        GoalStep OrderExplore()
        {
            if (FinishedShip == null)
            {
                Log.Error($"BuildScout {Build.Template.Name} failed: BuiltShip is null!");
                return GoalStep.GoalFailed;
            }
            FinishedShip.AI.OrderExplore();
            return GoalStep.GoalComplete;
        }
    }
}
