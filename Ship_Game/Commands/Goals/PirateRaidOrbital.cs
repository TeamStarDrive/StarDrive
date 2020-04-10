using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class PirateRaidOrbital : Goal
    {
        public const string ID = "PirateRaidOrbital";
        public override string UID => ID;
        private Pirates Pirates;

        public PirateRaidOrbital() : base(GoalType.PirateRaidOrbital)
        {
            Steps = new Func<GoalStep>[]
            {
               DetectAndSpawnRaidForce,
               CheckIfHijacked,
               ScuttleOrbital,
               WaitForDestruction
            };
        }

        public PirateRaidOrbital(Empire owner, Empire targetEmpire) : this()
        {
            empire        = owner;
            TargetEmpire = targetEmpire;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Pirates: New {empire.Name} Orbital Raid vs. {targetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates = empire.Pirates;
        }

        GoalStep DetectAndSpawnRaidForce()
        {
            if (Pirates.PaidBy(TargetEmpire) || Pirates.VictimIsDefeated(TargetEmpire))
                return GoalStep.GoalFailed; // They paid or dead

            var orbitalType = GetOrbital();
            orbitalType = Pirates.TargetType.Projector; // TODO for testing
            if (Pirates.GetTarget(TargetEmpire, orbitalType, out Ship orbital))
            {
                Vector2 where = orbital.Center.GenerateRandomPointOnCircle(1000);
                if (Pirates.SpawnBoardingShip(orbital, where, out Ship boardingShip))
                {
                    TargetShip = orbital; // This is the main target, we want this to be boarded
                    if (orbitalType != Pirates.TargetType.Projector)
                        SpawnBoardingForce(orbital, boardingShip);

                    return GoalStep.GoToNextStep;
                }
            }
            
            // Try locating viable orbital for maximum of 1 year (10 turns), else just give up
            return Empire.Universe.StarDate % 1 > 0 ? GoalStep.TryAgain : GoalStep.GoalFailed;
        }

        GoalStep CheckIfHijacked()
        {
            if (TargetShip == null
                || !TargetShip.Active
                || TargetShip.loyalty != Pirates.Owner && !TargetShip.InCombat)
            {
                return GoalStep.GoalFailed; // Target or our forces were destroyed 
            }

            return TargetShip.loyalty == Pirates.Owner ? GoalStep.GoToNextStep : GoalStep.TryAgain;
        }

        GoalStep ScuttleOrbital()
        {
            if (TargetShip == null || !TargetShip.Active || TargetShip.loyalty != Pirates.Owner)
                return GoalStep.GoalFailed; // Target destroyed or they took it from us

            TargetShip.DisengageExcessTroops(TargetShip.TroopCount); // She's gonna blow!
            TargetShip.ScuttleTimer = 10f;
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

        Pirates.TargetType GetOrbital()
        {
            int roll = RandomMath.RollDie(10 + Pirates.Level/4);
            switch (roll)
            {
                default: return Pirates.TargetType.Projector;
                case 7:  return Pirates.TargetType.Shipyard;
                case 8:  return Pirates.TargetType.Projector;
                case 9:  return Pirates.TargetType.Shipyard;
                case 10: return Pirates.TargetType.Projector;
                case 11: return Pirates.TargetType.Station;
                case 12: return Pirates.TargetType.Shipyard;
                case 13: return Pirates.TargetType.Projector;
                case 14: return Pirates.TargetType.Station;
                case 15: return Pirates.TargetType.Shipyard;
            }
        }

        void SpawnBoardingForce(Ship orbital, Ship boardingShip)
        {
            // Todo check for the target ally forces nearby and spawn escort ships 
        }
    }
}