using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class PirateRaidTransport : Goal
    {
        [StarData] public sealed override Ship TargetShip { get; set; }
        [StarData] public sealed override Empire TargetEmpire { get; set; }

        Pirates Pirates => Owner.Pirates;

        [StarDataConstructor]
        public PirateRaidTransport(Empire owner) : base(GoalType.PirateRaidTransport, owner)
        {
            Steps = new Func<GoalStep>[]
            {
               DetectAndSpawnRaidForce,
               CheckIfHijacked,
               WaitForReturnHome
            };
        }

        public PirateRaidTransport(Empire owner, Empire targetEmpire) : this(owner)
        {
            TargetEmpire = targetEmpire;
            Log.Info(ConsoleColor.Green, $"---- Pirates: New {Owner.Name} Transport Raid vs. {targetEmpire.Name} ----");
        }

        Ship BoardingShip
        {
            get => FinishedShip;
            set => FinishedShip = value;
        }

        public override bool IsRaid => true;

        GoalStep DetectAndSpawnRaidForce()
        {
            if (Pirates.PaidBy(TargetEmpire) || Pirates.VictimIsDefeated(TargetEmpire))
                return GoalStep.GoalFailed; // They paid or dead

            if (Pirates.GetTarget(TargetEmpire, Pirates.TargetType.FreighterAtWarp, out Ship freighter))
            {
                Vector2 where = freighter.Position.GenerateRandomPointOnCircle(1000);
                if (Pirates.SpawnBoardingShip(freighter, where, out Ship boardingShip))
                {
                    TargetShip   = freighter;
                    BoardingShip = boardingShip;
                    TargetShip.HyperspaceReturn();
                    TargetShip.CauseEmpDamage(1000);
                    TargetShip.AllStop();
                    boardingShip.AI.OrderAttackSpecificTarget(TargetShip);
                    Pirates.ExecuteProtectionContracts(TargetEmpire, TargetShip);
                    Pirates.ExecuteVictimRetaliation(TargetEmpire);
                    return GoalStep.GoToNextStep;
                }
            }

            // Try locating viable freighters for 1 year (10 turns), else just give up
            return Owner.Universum.StarDate < StarDateAdded + 1 ? GoalStep.TryAgain : GoalStep.GoalFailed;
        }

        GoalStep CheckIfHijacked()
        {
            if (TargetShip == null
                || !TargetShip.Active
                || TargetShip.Loyalty != Pirates.Owner && !TargetShip.AI.BadGuysNear)
            {
                BoardingShip?.AI.OrderPirateFleeHome();
                return GoalStep.GoalFailed; // Target destroyed or escaped
            }

            if (TargetShip.Loyalty == Pirates.Owner)
            {
                BoardingShip?.AI.OrderPirateFleeHome();
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