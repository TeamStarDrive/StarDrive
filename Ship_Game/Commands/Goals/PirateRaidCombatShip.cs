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
                combatShip.HyperspaceReturn();
                TargetShip = combatShip;
                if (Pirates.Level > TargetShip.TroopCount * 5 / ((int)CurrentGame.Difficulty).LowerBound(1) + TargetShip.Level)
                {
                    TargetShip.loyalty.AddMutinyNotification(TargetShip, GameText.MutinySucceeded, Pirates.Owner);
                    TargetShip.ChangeLoyalty(Pirates.Owner, notification: false);
                    Pirates.ExecuteProtectionContracts(TargetEmpire, TargetShip);
                }
                else
                {
                    TargetShip.loyalty.AddMutinyNotification(TargetShip, GameText.MutinyAverted, Pirates.Owner);
                }

                Pirates.ExecuteVictimRetaliation(TargetEmpire);
                KillMutinyDefenseTroops(Pirates.Level / 2 - TargetShip.Level);
                return TargetShip.loyalty == Pirates.Owner ? GoalStep.GoToNextStep : GoalStep.GoalFailed;
            }

            // Try locating viable combat ships for maximum of 1 year (10 turns), else just give up
            return (Empire.Universe.StarDate % 1).Greater(0) ? GoalStep.TryAgain : GoalStep.GoalFailed;
        }

        GoalStep CheckIfHijacked()
        {
            if (TargetShip == null
                || !TargetShip.Active
                || TargetShip.loyalty != Pirates.Owner)
            {
                return GoalStep.GoalFailed; // Target destroyed or escaped
            }

            if (TargetShip.loyalty == Pirates.Owner)
            {
                TargetShip.AI.OrderPirateFleeHome(signalRetreat: true);
                return GoalStep.GoalComplete;
            }

            return GoalStep.TryAgain;
        }

        void KillMutinyDefenseTroops(int numToKill)
        {
            for (int i = 0; i < numToKill; i++)
                TargetShip.KillOneOfOurTroops();
        }
    }
}