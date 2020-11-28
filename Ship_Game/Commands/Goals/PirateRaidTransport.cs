using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class PirateRaidTransport : Goal
    {
        public const string ID = "PirateRaidTransport";
        public override string UID => ID;
        private Pirates Pirates;

        public PirateRaidTransport() : base(GoalType.PirateRaidTransport)
        {
            Steps = new Func<GoalStep>[]
            {
               DetectAndSpawnRaidForce,
               CheckIfHijacked,
               WaitForReturnHome
            };
        }

        public PirateRaidTransport(Empire owner, Empire targetEmpire) : this()
        {
            empire        = owner;
            TargetEmpire  = targetEmpire;
            StarDateAdded = Empire.Universe.StarDate;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Pirates: New {empire.Name} Transport Raid vs. {targetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates = empire.Pirates;
        }

        public override bool IsRaid => true;

        GoalStep DetectAndSpawnRaidForce()
        {
            if (Pirates.PaidBy(TargetEmpire) || Pirates.VictimIsDefeated(TargetEmpire))
                return GoalStep.GoalFailed; // They paid or dead

            if (Pirates.GetTarget(TargetEmpire, Pirates.TargetType.FreighterAtWarp, out Ship freighter))
            {
                Vector2 where = freighter.Center.GenerateRandomPointOnCircle(1000);
                if (Pirates.SpawnBoardingShip(freighter, where, out Ship boardingShip))
                {
                    TargetShip = freighter;
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
            return Empire.Universe.StarDate < StarDateAdded + 1 ? GoalStep.TryAgain : GoalStep.GoalFailed;
        }

        GoalStep CheckIfHijacked()
        {
            if (TargetShip == null
                || !TargetShip.Active
                || TargetShip.loyalty != Pirates.Owner && !TargetShip.AI.BadGuysNear)
            {
                return GoalStep.GoalFailed; // Target destroyed or escaped
            }

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