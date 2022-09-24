using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class DeepSpaceBuildGoal : Goal
    {
        [StarData] public DeepSpaceBuildable Build;
        public override IShipDesign ToBuild => Build.Template;

        [StarDataConstructor]
        public DeepSpaceBuildGoal(GoalType type, Empire owner) : base(type, owner)
        {
        }
    }
}
