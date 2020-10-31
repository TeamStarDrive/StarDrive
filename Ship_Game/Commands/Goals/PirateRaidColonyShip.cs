using System;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class PirateRaidColonyShip : Goal // FB - this was merged with transport - remove this file in 2021 (because saves..)
    {
        public const string ID = "PirateRaidColonyShip";
        public override string UID => ID;
        private Pirates Pirates;

        public PirateRaidColonyShip() : base(GoalType.PirateRaidTransport)
        {
            Steps = new Func<GoalStep>[]
            {
               DetectAndSpawnRaidForce,
               CheckIfHijacked,
               WaitForReturnHome
            };
        }

        public PirateRaidColonyShip(Empire owner, Empire targetEmpire) : this()
        {
            empire       = owner;
            TargetEmpire = targetEmpire;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Pirates: New {empire.Name} Colony Ship Raid vs. {targetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates = empire.Pirates;
        }

        public override bool IsRaid => true;

        GoalStep DetectAndSpawnRaidForce()
        {
            return GoalStep.GoalComplete;
        }

        GoalStep CheckIfHijacked()
        {

            if (!TargetShip.Active || TargetShip.loyalty != Pirates.Owner && !TargetShip.AI.BadGuysNear)
                return GoalStep.GoalFailed; // Target destroyed or escaped

            if (TargetShip.loyalty == Pirates.Owner)
            {
                TargetShip.AI.OrderPirateFleeHome(signalRetreat: true);
                return GoalStep.GoToNextStep;
            }

            return GoalStep.TryAgain;
        }

        GoalStep WaitForReturnHome()
        {
            if (TargetShip == null || !TargetShip.Active)
                return GoalStep.GoalComplete;

            return GoalStep.TryAgain;
        }
    }
}