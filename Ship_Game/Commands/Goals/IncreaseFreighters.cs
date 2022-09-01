using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Universe;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class IncreaseFreighters : BuildShipsGoalBase
    {
        public const string ID = "IncreaseFreighters";
        public override string UID => ID;

        [StarDataConstructor]
        public IncreaseFreighters(int id, UniverseState us)
            : base(GoalType.IncreaseFreighters, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitForShipBuilt,
                CompleteGoal
            };
        }

        public IncreaseFreighters(Empire empire)
            : this(empire.Universum.CreateId(), empire.Universum)
        {
            this.empire = empire;
        }

        GoalStep FindPlanetToBuildAt()
        {
            if (!GetFreighter(out IShipDesign freighter))
                return GoalStep.GoalFailed;

            if (!empire.FindPlanetToBuildShipAt(empire.SafeSpacePorts, freighter, out Planet planet, priority: 0.1f))
                return GoalStep.GoalFailed;

            planet.Construction.Enqueue(freighter, this, notifyOnEmpty: false);
            if (empire.TotalFreighters < empire.GetPlanets().Count)
                planet.Construction.PrioritizeShip(freighter, 1);

            return GoalStep.GoToNextStep;
        }

        GoalStep CompleteGoal()
        {
            return GoalStep.GoalComplete;
        }
    }
}
