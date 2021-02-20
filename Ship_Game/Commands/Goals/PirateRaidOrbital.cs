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
               CheckSuccess
            };
        }

        public PirateRaidOrbital(Empire owner, Empire targetEmpire) : this()
        {
            empire       = owner;
            TargetEmpire = targetEmpire;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Pirates: New {empire.Name} Orbital Raid vs. {targetEmpire.Name} ----");
            Evaluate();
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

            StarDateAdded = Empire.Universe.StarDate;
            var orbitalType = GetOrbital();
            // orbitalType = Pirates.TargetType.Projector; // TODO for testing
            if (!Pirates.GetTarget(TargetEmpire, orbitalType, out Ship orbital))
                return Empire.Universe.StarDate.Greater(StarDateAdded + 1) ? GoalStep.GoalFailed : GoalStep.TryAgain;

            TargetShip           = orbital; // This is the main target, we want this dead or possibly boarded
            float spawnDistance  = TargetShip.System?.Radius ?? 80000;
            Vector2 where        = orbital.Center.GenerateRandomPointOnCircle(spawnDistance);
            int numBoardingShips = (TargetShip.TroopCount / 2).LowerBound(1);

            if (Pirates.SpawnForce(TargetShip, where, 5000, out Array<Ship> force))
                Pirates.OrderAttackShip(TargetShip, force);

            for (int i = 0; i < numBoardingShips; i++)
            {
                Vector2 pos = where.GenerateRandomPointInsideCircle(2000);
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
                Pirates.TryLevelUp();
                return GoalStep.GoalComplete; // Target was destroyed
            }

            if (TargetShip.loyalty == Pirates.Owner)
            {
                Pirates.TryLevelUp();
                TargetShip.AI.OrderPirateFleeHome(signalRetreat: true);
                return GoalStep.GoalComplete; // Target was boarded
            }

            // 25 turns to try finish the job
            return Empire.Universe.StarDate.Greater(StarDateAdded + 2.5f) ? GoalStep.GoalFailed : GoalStep.TryAgain;
        }

        Pirates.TargetType GetOrbital()
        {
            int divider = (Pirates.MaxLevel / 5).LowerBound(1); // so the bonus will be 1 to 5
            int roll    = RandomMath.RollDie(10 + Pirates.Level/divider);
            switch (roll)
            {
                default: return Pirates.TargetType.Platform;
                case 7:  return Pirates.TargetType.Shipyard;
                case 8:  return Pirates.TargetType.Station;
                case 9:  return Pirates.TargetType.Shipyard;
                case 10: return Pirates.TargetType.Platform;
                case 11: return Pirates.TargetType.Station;
                case 12: return Pirates.TargetType.Shipyard;
                case 13: return Pirates.TargetType.Platform;
                case 14: return Pirates.TargetType.Station;
                case 15: return Pirates.TargetType.Shipyard;
            }
        }
    }
}