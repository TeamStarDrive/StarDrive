﻿using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class PirateRaidOrbital : Goal
    {
        [StarData] public sealed override Ship TargetShip { get; set; }
        [StarData] public sealed override Empire TargetEmpire { get; set; }

        Pirates Pirates => Owner.Pirates;
        
        [StarDataConstructor]
        public PirateRaidOrbital(Empire owner) : base(GoalType.PirateRaidOrbital, owner)
        {
            Steps = new Func<GoalStep>[]
            {
               DetectAndSpawnRaidForce,
               CheckSuccess
            };
        }

        public PirateRaidOrbital(Empire owner, Empire targetEmpire) : this(owner)
        {
            TargetEmpire = targetEmpire;
            if (Pirates.Verbose)
                Log.Info(ConsoleColor.Green, $"---- Pirates: New {Owner.Name} Orbital Raid vs. {targetEmpire.Name} ----");
        }

        public override bool IsRaid => true;

        GoalStep DetectAndSpawnRaidForce()
        {
            if (Pirates.PaidBy(TargetEmpire) || Pirates.VictimIsDefeated(TargetEmpire))
                return GoalStep.GoalFailed; // They paid or dead

            StarDateAdded = Owner.Universe.StarDate;
            var orbitalType = GetOrbital();
            // orbitalType = Pirates.TargetType.Projector; // TODO for testing
            if (!Pirates.GetTarget(TargetEmpire, orbitalType, out Ship orbital))
                return Owner.Universe.StarDate.Greater(StarDateAdded + 1) ? GoalStep.GoalFailed : GoalStep.TryAgain;

            TargetShip           = orbital; // This is the main target, we want this dead or possibly boarded
            float spawnDistance  = TargetShip.System?.Radius ?? 80000;
            Vector2 where        = orbital.Position.GenerateRandomPointOnCircle(spawnDistance, Owner.Random);
            int numBoardingShips = (TargetShip.TroopCount / 2).LowerBound(1);

            if (Pirates.SpawnForce(TargetShip, where, 5000, out Array<Ship> force))
                Pirates.OrderAttackShip(TargetShip, force);

            for (int i = 0; i < numBoardingShips; i++)
            {
                Vector2 pos = where.GenerateRandomPointInsideCircle(2000, Owner.Random);
                if (Pirates.SpawnBoardingShip(orbital, pos, out Ship boardingShip))
                    boardingShip.AI.OrderAttackSpecificTarget(TargetShip);
            }

            Pirates.ExecuteProtectionContracts(TargetEmpire, TargetShip);
            Pirates.ExecuteVictimRetaliation(TargetEmpire);
            return GoalStep.GoToNextStep;
        }

        GoalStep CheckSuccess()
        {
            if (TargetShip == null || !TargetShip.Active)
            {
                Pirates.TryLevelUp(TargetEmpire.Universe);
                return GoalStep.GoalComplete; // Target was destroyed
            }

            if (TargetShip.Loyalty == Pirates.Owner)
            {
                Pirates.TryLevelUp(TargetEmpire.Universe);
                TargetShip.AI.OrderPirateFleeHome(signalRetreat: true);
                return GoalStep.GoalComplete; // Target was boarded
            }

            // 25 turns to try finish the job
            return Owner.Universe.StarDate.Greater(StarDateAdded + 2.5f) ? GoalStep.GoalFailed : GoalStep.TryAgain;
        }

        Pirates.TargetType GetOrbital()
        {
            int divider = (Pirates.MaxLevel / 2).LowerBound(1); // so the bonus will be 1 to 10
            int roll = Owner.Random.RollDie(5 + Pirates.Level/divider);
            switch (roll)
            {
                default: return Pirates.TargetType.Platform;
                case 5:  return Pirates.TargetType.Research;
                case 6:  return Pirates.TargetType.Shipyard;
                case 7:  return Pirates.TargetType.Station;
                case 8:  return Pirates.TargetType.Research;
                case 9:  return Pirates.TargetType.Shipyard;
                case 10: return Pirates.TargetType.Platform;
                case 11: return Pirates.TargetType.Station;
                case 12: return Pirates.TargetType.Shipyard;
                case 13: return Pirates.TargetType.Platform;
                case 14: return Pirates.TargetType.Station;
                case 15: return Pirates.TargetType.Research;
            }
        }
    }
}