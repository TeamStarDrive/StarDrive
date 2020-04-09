using System;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class PirateRaidDirector : Goal
    {
        public const string ID = "PirateRaidDirector";
        public override string UID => ID;
        private Pirates Pirates;

        public PirateRaidDirector() : base(GoalType.PirateRaidDirector)
        {
            Steps = new Func<GoalStep>[]
            {
               PrepareRaid
            };
        }
        public PirateRaidDirector(Empire owner, Empire targetEmpire) : this()
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
            //startChance = 100;
            if (RandomMath.RollDice(startChance))
            {
                GoalType raid  = GetRaid();
                switch (raid)
                {
                    case GoalType.PirateRaidTransport:  Pirates.AddGoalRaidTransport(TargetEmpire);      break;
                    case GoalType.PirateRaidOrbital:    Pirates.AddGoalRaidOrbital(TargetEmpire);        break;
                    case GoalType.PirateRaidColonyShip: Pirates.AddGoalRaidRaidColonyShip(TargetEmpire); break;
                }
            }

            return GoalStep.TryAgain;
        }

        public int NumRaids()
        {
            int numGoals = 0;
            var goals = Pirates.Goals;
            for (int i = 0; i < goals.Count; i++)
            {
                Goal goal = goals[i];
                if (goal.TargetEmpire == TargetEmpire)
                {
                    switch (goal.type)
                    {
                        case GoalType.PirateRaidTransport: numGoals += 1; break;
                    }
                }
            }

            return numGoals;
        }

        int RaidStartChance()
        {
            int numCurrentRaids = NumRaids();
            if (numCurrentRaids >= Pirates.ThreatLevelFor(TargetEmpire))
                return 0; // Limit maximum of raids to threat vs this empire

            int startChance = Pirates.ThreatLevelFor(TargetEmpire).LowerBound((int)CurrentGame.Difficulty * 2);
            startChance    /= numCurrentRaids.LowerBound(1);
            float taxRate   = TargetEmpire.data.TaxRate;

            if (taxRate > 0.25f) // High Tax rate encourages more pirate tippers
                startChance *= (int)(1 + taxRate);

            return startChance;
        }

        GoalType GetRaid()
        {
            int raid = RandomMath.RollDie(Pirates.Level.UpperBound(Pirates.ThreatLevelFor(TargetEmpire)));

            switch (raid)
            {
                default:
                case 1:
                case 2: return GoalType.PirateRaidTransport;
                case 3: return GoalType.PirateRaidOrbital;
                case 4: return GoalType.PirateRaidTransport;
                case 5: return GoalType.PirateRaidColonyShip;
                case 6: return GoalType.PirateRaidTransport;
                case 7: return GoalType.PirateRaidOrbital;
                case 8: return GoalType.PirateRaidColonyShip;
                    // hijack combat ship in warp
                    // capture and destroy shipyard
                    // defeat planet orbitals
                    // .
                    // .
                    // case 20: 
            }
        }
    }
}