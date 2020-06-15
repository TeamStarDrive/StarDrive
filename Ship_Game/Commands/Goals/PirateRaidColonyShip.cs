using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class PirateRaidColonyShip : Goal
    {
        public const string ID = "PirateRaidColonyShip";
        public override string UID => ID;
        private Pirates Pirates;

        public PirateRaidColonyShip() : base(GoalType.PirateRaidColonyShip)
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
            empire = owner;
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
            if (Pirates.PaidBy(TargetEmpire) || Pirates.VictimIsDefeated(TargetEmpire))
                return GoalStep.GoalFailed; // They paid or dead

            if (Pirates.GetTarget(TargetEmpire, Pirates.TargetType.ColonyShip, out Ship colonyShip))
            {
                Vector2 where = colonyShip.Center.GenerateRandomPointOnCircle(1000);
                colonyShip.HyperspaceReturn();
                if (Pirates.SpawnBoardingShip(colonyShip, where, out Ship boardingShip))
                {
                    TargetShip = colonyShip;
                    if (Pirates.SpawnForce(TargetShip, boardingShip.Center, 5000, out Array<Ship> force))
                        Pirates.OrderEscortShip(boardingShip, force);

                    Pirates.ExecuteProtectionContracts(TargetEmpire, TargetShip);
                    Pirates.ExecuteVictimRetaliation(TargetEmpire);
                    return GoalStep.GoToNextStep;
                }
            }

            // Try locating viable colony ships  for maximum of 1 year (10 turns), else just give up
            return Empire.Universe.StarDate % 1 > 0 ? GoalStep.TryAgain : GoalStep.GoalFailed;
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