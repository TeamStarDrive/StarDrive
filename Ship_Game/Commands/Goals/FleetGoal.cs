using Ship_Game.Data.Serialization;
using Ship_Game.AI;
using Ship_Game.Fleets;

namespace Ship_Game.Commands.Goals
{
    // Goals which use `Fleet`
    [StarDataType]
    public abstract class FleetGoal : Goal
    {
        [StarData] public Fleet Fleet;

        protected FleetGoal(GoalType type, Empire owner) : base(type, owner)
        {
        }
    }
}
