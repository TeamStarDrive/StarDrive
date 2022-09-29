using System;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class PirateAI : Goal
    {
        [StarData] Pirates Pirates;

        [StarDataConstructor]
        public PirateAI(Empire owner) : base(GoalType.PirateAI, owner)
        {
            Steps = new Func<GoalStep>[]
            {
               PiratePlan
            };
        }

        GoalStep PiratePlan()
        {
            if (!Owner.WeArePirates)
                return GoalStep.GoalFailed; // This is mainly for save compatibility

            Pirates = Owner.Pirates;
            bool firstRun = Pirates.PaymentTimers.Count == 0;
            Pirates.Init();
            Pirates.TryLevelUp(Owner.Universe, alwaysLevelUp: true); // build initial base

            if (!Pirates.GetBases(out Array<Ship> bases) && !firstRun)
            {
                Log.Warning($"Could not find a Pirate base for {Owner.Name}. Pirate AI is disabled for them!");
                return GoalStep.GoalFailed;
            }

            //Pirates.AddGoalDirectorPayment(EmpireManager.Player); // TODO for testing
            foreach (Empire victim in Pirates.Universe.MajorEmpires)
                Pirates.AddGoalDirectorPayment(victim);

            return GoalStep.GoalComplete;
        }
    }
}