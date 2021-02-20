using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class PirateRaidProjector : Goal
    {
        public const string ID = "PirateRaidProjector";
        public override string UID => ID;
        private Pirates Pirates;

        public PirateRaidProjector() : base(GoalType.PirateRaidProjector)
        {
            Steps = new Func<GoalStep>[]
            {
               DetectAndSpawnRaidForce,
               CheckIfHijacked,
               FleeFromOrbital,
               WaitForDestruction
            };
        }

        public PirateRaidProjector(Empire owner, Empire targetEmpire) : this()
        {
            empire        = owner;
            TargetEmpire  = targetEmpire;
            StarDateAdded = Empire.Universe.StarDate;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Pirates: New {empire.Name} SSP Raid vs. {targetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates = empire.Pirates;
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

            if (Pirates.GetTarget(TargetEmpire, Pirates.TargetType.Projector, out Ship orbital))
            {
                Vector2 where = orbital.Center.GenerateRandomPointOnCircle(3000);
                if (Pirates.SpawnBoardingShip(orbital, where, out Ship boardingShip))
                {
                    TargetShip   = orbital; // This is the main target, we want this to be boarded
                    BoardingShip = boardingShip;
                    Pirates.ExecuteProtectionContracts(TargetEmpire, TargetShip);
                    Pirates.ExecuteVictimRetaliation(TargetEmpire);
                    BoardingShip.AI.OrderAttackSpecificTarget(TargetShip);
                    return GoalStep.GoToNextStep;
                }
            }

            // Try locating viable SSP for maximum of 1 year (10 turns), else just give up
            return Empire.Universe.StarDate < StarDateAdded + 1 ? GoalStep.TryAgain : GoalStep.GoalFailed;
        }

        GoalStep CheckIfHijacked()
        {
            if (TargetShip == null
                || !TargetShip.Active
                || TargetShip.loyalty != Pirates.Owner && !TargetShip.AI.BadGuysNear)
            {
                BoardingShip?.AI.OrderPirateFleeHome(true);
                return GoalStep.GoalFailed; // Target or our forces were destroyed 
            }

            return TargetShip.loyalty == Pirates.Owner ? GoalStep.GoToNextStep : GoalStep.TryAgain;
        }

        GoalStep FleeFromOrbital()
        {
            BoardingShip?.AI.OrderPirateFleeHome(true);
            if (TargetShip == null || !TargetShip.Active || TargetShip.loyalty != Pirates.Owner)
                return GoalStep.GoalFailed; // Target destroyed or they took it from us

            TargetShip.DisengageExcessTroops(TargetShip.TroopCount); // She's gonna blow! (PiratePostChangeLoyalty)
            TargetShip.AI.OrderPirateFleeHome(signalRetreat: true);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForDestruction()
        {
            if (TargetShip == null || !TargetShip.Active)
            {
                Pirates.TryLevelUp();
                return GoalStep.GoalComplete;
            }

            return TargetShip.loyalty == Pirates.Owner ? GoalStep.TryAgain : GoalStep.GoalFailed;
        }
    }
}