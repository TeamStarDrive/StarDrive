using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class PirateRaidCombatShip : Goal
    {
        public const string ID = "PirateRaidCombatShip";
        public override string UID => ID;
        private Pirates Pirates;

        public PirateRaidCombatShip() : base(GoalType.PirateRaidCombatShip)
        {
            Steps = new Func<GoalStep>[]
            {
               DetectAndSpawnRaidForce,
               CheckIfHijacked
            };
        }

        public PirateRaidCombatShip(Empire owner, Empire targetEmpire) : this()
        {
            empire = owner;
            TargetEmpire = targetEmpire;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Pirates: New {empire.Name} Combat Ship Raid vs. {targetEmpire.Name} ----");
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

            if (Pirates.GetTarget(TargetEmpire, Pirates.TargetType.CombatShipAtWarp, out Ship combatShip))
            {
                Vector2 where = combatShip.Center.GenerateRandomPointOnCircle(1500);
                combatShip.HyperspaceReturn();
                if (Pirates.SpawnBoardingShip(combatShip, where, out Ship boardingShip))
                {
                    TargetShip = combatShip;
                    if (Pirates.SpawnForce(TargetShip, boardingShip.Center, 5000, out Array<Ship> force))
                        Pirates.OrderEscortShip(boardingShip, force);

                    Pirates.ExecuteProtectionContracts(TargetEmpire, TargetShip);
                    return GoalStep.GoToNextStep;
                }
            }

            // Try locating viable combat ships for maximum of 1 year (10 turns), else just give up
            return Empire.Universe.StarDate % 1 > 0 ? GoalStep.TryAgain : GoalStep.GoalFailed;
        }

        GoalStep CheckIfHijacked()
        {
            if (!TargetShip.Active || TargetShip.loyalty != Pirates.Owner && !TargetShip.InCombat)
                return GoalStep.GoalFailed; // Target destroyed or escaped

            if (TargetShip.loyalty == Pirates.Owner)
            {
                TargetShip.AI.OrderPirateFleeHome(signalRetreat: true);
                return GoalStep.GoalComplete;
            }

            return GoalStep.TryAgain;
        }
    }
}