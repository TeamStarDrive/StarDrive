using System;
using System.Collections.Generic;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe;

namespace Ship_Game.Commands.Goals
{
    public class PirateAI : Goal
    {
        public const string ID = "PirateAI";
        public override string UID => ID;
        private Pirates Pirates;

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