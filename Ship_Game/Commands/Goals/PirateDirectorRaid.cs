using System;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class PirateDirectorRaid : Goal
    {
        public const string ID = "PirateDirectorRaid";
        public override string UID => ID;
        private Pirates Pirates;

        public PirateDirectorRaid() : base(GoalType.PirateDirectorRaid)
        {
            Steps = new Func<GoalStep>[]
            {
               PrepareRaid
            };
        }
        public PirateDirectorRaid(Empire owner, Empire targetEmpire) : this()
        {
            empire       = owner;
            TargetEmpire = targetEmpire;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Pirates: New {empire.Name} Raid Director vs. {TargetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates = empire.Pirates;
        }

        GoalStep PrepareRaid()
        {
            if (Pirates.PaidBy(TargetEmpire) || Pirates.VictimIsDefeated(TargetEmpire))
            {
                Log.Info(ConsoleColor.Green, $"---- Pirates: {empire.Name} Raid Director vs. {TargetEmpire.Name}, They paid, terminating ----");
                return GoalStep.GoalFailed; // We got paid or they are gone, Raid Director can go on vacation
            }

            float startChance = RaidStartChance();
            if (RandomMath.RollDice(startChance))
            {
                GoalType raid  = GetRaid();
                switch (raid)
                {
                    case GoalType.PirateRaidTransport:  Pirates.AddGoalRaidTransport(TargetEmpire);      break;
                    case GoalType.PirateRaidOrbital:    Pirates.AddGoalRaidOrbital(TargetEmpire);        break;
                    case GoalType.PirateRaidProjector:  Pirates.AddGoalRaidProjector(TargetEmpire);      break;
                    case GoalType.PirateRaidCombatShip: Pirates.AddGoalRaidCombatShip(TargetEmpire);     break;
                }
            }

            return GoalStep.TryAgain;
        }

        int RaidStartChance()
        {
            if (!Pirates.CanDoAnotherRaid(out int numRaids))
                return 0; // Limit maximum of concurrent raids

            int startChance = Pirates.Level.LowerBound((int)CurrentGame.Difficulty + 1);
            float taxRate   = TargetEmpire.data.TaxRate;
            startChance    /= numRaids + 1;

            if (taxRate > 0.25f) // High Tax rate encourages more pirate tippers
                startChance *= (int)(1 + taxRate);

            //startChance = 100; // For testing
            return startChance.UpperBound(Pirates.ThreatLevelFor(TargetEmpire));
        }

        GoalType GetRaid()
        {
            int raid = RandomMath.RollDie(Pirates.Level.UpperBound(Pirates.ThreatLevelFor(TargetEmpire)));

            switch (raid)
            {
                default:
                case 1:
                case 2:  return GoalType.PirateRaidTransport;
                case 3:  return GoalType.PirateRaidProjector;
                case 4:  return GoalType.PirateRaidTransport;
                case 5:  return GoalType.PirateRaidTransport;
                case 6:  return GoalType.PirateRaidProjector;
                case 7:  return GoalType.PirateRaidOrbital;
                case 8:  return GoalType.PirateRaidCombatShip;
                case 9:  return GoalType.PirateRaidOrbital;
                case 10: return GoalType.PirateRaidProjector;
                case 11: return GoalType.PirateRaidCombatShip;
                case 12: return GoalType.PirateRaidOrbital;
                case 13: return GoalType.PirateRaidCombatShip;
                case 14: return GoalType.PirateRaidTransport;
                case 15: return GoalType.PirateRaidProjector;
                case 16: return GoalType.PirateRaidCombatShip;
                case 17: return GoalType.PirateRaidTransport;
                case 18: return GoalType.PirateRaidOrbital;
                case 19: return GoalType.PirateRaidCombatShip;
            }
        }
    }
}