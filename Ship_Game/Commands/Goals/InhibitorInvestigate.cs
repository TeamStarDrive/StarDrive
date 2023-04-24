using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Ships;
using System;
using SDUtils;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;
using System.Linq;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class InhibitorInvestigate : Goal
    {
        [StarData] public sealed override Empire TargetEmpire { get; set; }
        [StarData] MilitaryTask Task { get; set; }
        [StarData] Vector2 InvestigatePos { get; set; }
        [StarData] float StrNeeded { get; set; }

        public override bool IsInvsestigationHere(Vector2 pos) => pos.InRadius(InvestigatePos, 50_000);


        [StarDataConstructor]
        public InhibitorInvestigate(Empire owner) : base(GoalType.InhibitorInvestigate, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                CreateTask,
                CheckTaskActive
            };
        }

        public InhibitorInvestigate(Empire e, Empire targetEmpire, float strNeeded, Vector2 pos) : this(e)
        {
            TargetEmpire = targetEmpire;
            Owner = e;
            InvestigatePos = pos;
            StrNeeded = strNeeded;
        }

        GoalStep CreateTask()
        {
            Task =  MilitaryTask.InhibitorInvestigateTask(Owner, InvestigatePos, 30_000, StrNeeded, TargetEmpire);
            Owner.AI.AddPendingTask(Task);
            return GoalStep.GoToNextStep;
        }

        GoalStep CheckTaskActive()
        {
            return Task != null ? GoalStep.TryAgain : GoalStep.GoalComplete;
        }
    }
}
