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
               PrepareMission
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

        GoalStep PrepareMission()
        {
            Relationship rel = Pirates.GetRelations(TargetEmpire);
            if (!rel.AtWar)
                return GoalStep.GoalFailed; // Not at war anymore, maybe we got paid

            float startChance = MissionStartChance();
            if (RandomMath.RollDice(startChance))
            {
                GoalType mission  = GetMission();
                EmpireAI pirateAI = Pirates.GetEmpireAI();
                switch (mission)
                {
                    case GoalType.CorsairTransportRaid: pirateAI.Goals.Add(new CorsairTransportRaid(Pirates, TargetEmpire)); break;
                }
            }

            return GoalStep.TryAgain;
        }

        public int NumMissions()
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
                        case GoalType.CorsairTransportRaid: numGoals += 1; break;
                    }
                }
            }

            return numGoals;
        }

        int MissionStartChance()
        {
            int numCurrentMissions = NumMissions();
            if (numCurrentMissions >= TargetEmpire.PirateThreatLevel)
                return 0; // Limit maximum of missions to threat vs this empire

            int startChance = TargetEmpire.PirateThreatLevel.LowerBound((int)CurrentGame.Difficulty * 2);
            startChance    /= numCurrentMissions.LowerBound(1);
            float taxRate   = TargetEmpire.data.TaxRate;

            if (taxRate > 0.25f) // High Tax rate encourages more pirate tippers
                startChance *= (int)(1 + taxRate);

            return startChance;
        }

        GoalType GetMission()
        {
            int mission = RandomMath.RollDie(Pirates.PirateThreatLevel.UpperBound(TargetEmpire.PirateThreatLevel));

            switch (mission)
            {
                default:
                case 1:
                case 2: return GoalType.CorsairTransportRaid;
                // capture and destroy ssp
                // hijack colonyship
                // hijack combat ship in warp
                // capture and destroy shipyard
                // defeat planet orbitals
            }
        }
    }
}