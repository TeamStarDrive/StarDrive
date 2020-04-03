using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class CorsairMissionDirector : Goal
    {
        public const string ID = "CorsairMissionDirector";
        public override string UID => ID;
        private readonly Empire Pirates;

        public CorsairMissionDirector() : base(GoalType.CorsairMissionDirector)
        {
            Pirates = empire;

            Steps = new Func<GoalStep>[]
            {
               PrepareMission
            };
        }
        public CorsairMissionDirector(Empire owner, Empire targetEmpire) : this()
        {
            empire       = owner;
            TargetEmpire = targetEmpire;
            Pirates      = empire;

            Log.Info(ConsoleColor.Green, $"---- New Pirate Mission Director vs. {TargetEmpire.Name} ----");
        }

        GoalStep PrepareMission()
        {
            Relationship rel = Pirates.GetRelations(TargetEmpire);
            if (rel.AtWar)
                return GoalStep.GoalFailed; // not at war anymore, maybe we got paid

            if (RandomMath.RollDice(MissionStartChance(rel.TurnsAtWar)))
                Pirates.GetEmpireAI().Goals.Add(new CorsairTransportRaid(Pirates, TargetEmpire));

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

        int MissionStartChance(int turnsAtWar)
        {
            int numCurrentMissions = NumMissions();
            if (numCurrentMissions >= TargetEmpire.PirateThreatLevel)
                return 0; // Limit maximum of missions to threat vs this empire

            int startChance = turnsAtWar / 3;
            startChance    /= numCurrentMissions.LowerBound(1);
            float taxRate   = TargetEmpire.data.TaxRate;

            if (taxRate > 0.25f) // High Tax rate encourages more pirate tippers
                startChance += (int)(startChance * taxRate);

            return startChance;
        }

        GoalType GetMission()
        {
            int mission = RandomMath.RollDie(TargetEmpire.PirateThreatLevel);

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