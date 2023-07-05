using Ship_Game.Data.Serialization;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.AI.Tasks;

namespace Ship_Game.Commands.Goals
{
    // Goals which use `Fleet`
    [StarDataType]
    public abstract class FleetGoal : Goal
    {
        [StarData] public Fleet Fleet;
        [StarData] public MilitaryTask Task;

        protected FleetGoal(GoalType type, Empire owner) : base(type, owner)
        {
        }

        public override void RemoveTask(MilitaryTask task)
        {
            if (Task == task)
                Task = null;    
        }

        public override void RemoveFleet(Fleet fleet)
        {
            if (Fleet == fleet)
                Fleet = null;
        }
    }
}
