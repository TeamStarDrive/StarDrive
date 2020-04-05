using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class CorsairRaidDirector : Goal
    {
        public const string ID = "CorsairRaidDirector";
        public override string UID => ID;
        private Empire Pirates;

        public CorsairRaidDirector() : base(GoalType.CorsairRaidDirector)
        {
            Steps = new Func<GoalStep>[]
            {
               PrepareRaid
            };
        }
        public CorsairRaidDirector(Empire owner, Empire targetEmpire) : this()
        {
            empire       = owner;
            TargetEmpire = targetEmpire;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- New Pirate Raid Director vs. {TargetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates = empire;
        }

        GoalStep PrepareRaid()
        {
            if (Paid)
                return GoalStep.GoalFailed; // We got paid, Raid Director can go on vacation

            float startChance = RaidStartChance();
            //startChance = 100;
            if (RandomMath.RollDice(startChance))
            {
                GoalType raid  = GetRaid();
                EmpireAI pirateAI = Pirates.GetEmpireAI();
                switch (raid)
                {
                    case GoalType.CorsairRaidTransport: pirateAI.Goals.Add(new CorsairRaidTransport(Pirates, TargetEmpire)); break;
                }
            }

            return GoalStep.TryAgain;
        }

        public int NumRaids()
        {
            int numGoals = 0;
            var goals = Pirates.GetEmpireAI().Goals;
            for (int i = 0; i < goals.Count; i++)
            {
                Goal goal = goals[i];
                if (goal.TargetEmpire == TargetEmpire)
                {
                    switch (goal.type)
                    {
                        case GoalType.CorsairRaidTransport: numGoals += 1; break;
                    }
                }
            }

            return numGoals;
        }

        int RaidStartChance()
        {
            int numCurrentRaids = NumRaids();
            if (numCurrentRaids >= TargetEmpire.PirateThreatLevel)
                return 0; // Limit maximum of raids to threat vs this empire

            int startChance = TargetEmpire.PirateThreatLevel.LowerBound((int)CurrentGame.Difficulty * 2);
            startChance    /= numCurrentRaids.LowerBound(1);
            float taxRate   = TargetEmpire.data.TaxRate;

            if (taxRate > 0.25f) // High Tax rate encourages more pirate tippers
                startChance *= (int)(1 + taxRate);

            return startChance;
        }

        GoalType GetRaid()
        {
            int raid = RandomMath.RollDie(Pirates.PirateThreatLevel.UpperBound(TargetEmpire.PirateThreatLevel));

            switch (raid)
            {
                default:
                case 1:
                case 2: return GoalType.CorsairRaidTransport;
                // capture and destroy ssp
                // hijack colonyship
                // hijack combat ship in warp
                // capture and destroy shipyard
                // defeat planet orbitals
                // .
                // .
                // case 20: 
            }
        }

        bool Paid => !Pirates.GetRelations(TargetEmpire).AtWar;
    }
}