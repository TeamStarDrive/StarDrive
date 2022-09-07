using System;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Universe;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class PirateAI : Goal
    {
        [StarData] Pirates Pirates;

        [StarDataConstructor]
        public PirateAI(int id, UniverseState us)
            : base(GoalType.PirateAI, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
               PiratePlan
            };
        }
        public PirateAI(Empire owner)
            : this(owner.Universum.CreateId(), owner.Universum)
        {
            empire = owner;
        }

        GoalStep PiratePlan()
        {
            if (!empire.WeArePirates)
                return GoalStep.GoalFailed; // This is mainly for save compatibility

            Pirates = empire.Pirates;
            bool firstRun = Pirates.PaymentTimers.Count == 0;
            Pirates.Init();
            Pirates.TryLevelUp(empire.Universum, alwaysLevelUp: true); // build initial base

            if (!Pirates.GetBases(out Array<Ship> bases) && !firstRun)
            {
                Log.Warning($"Could not find a Pirate base for {empire.Name}. Pirate AI is disabled for them!");
                return GoalStep.GoalFailed;
            }

            //Pirates.AddGoalDirectorPayment(EmpireManager.Player); // TODO for testing
            foreach (Empire victim in EmpireManager.MajorEmpires)
                Pirates.AddGoalDirectorPayment(victim);

            return GoalStep.GoalComplete;
        }
    }
}