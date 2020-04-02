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
        public Empire Player;
        public CorsairMissionDirector() : base(GoalType.CorsairMissionDirector)
        {
            Steps = new Func<GoalStep>[]
            {
               PrepareMission
            };
        }
        public CorsairMissionDirector(Empire owner) : this()
        {
            empire = owner;
        }

        GoalStep PrepareMission()
        {
            Player           = EmpireManager.Player;
            Relationship rel = empire.GetRelations(Player);
            if (rel.AtWar)
                return GoalStep.GoalFailed; // not at war anymore, maybe we got paid

            int startChance = rel.TurnsAtWar * 3;
            if (NumMissions() < Player.PirateThreatLevel  
                && RandomMath.RollDice(startChance))
            {
                startChance = 0;
                empire.GetEmpireAI().Goals.Add(new CorsairTransportRaid(empire));
            }

            return GoalStep.TryAgain;
        }

        public int NumMissions()
        {
            int numGoals = 0;
            var goals = empire.GetEmpireAI().Goals;
            for (int i = 0; i < goals.Count; i++)
            {
                Goal goal = goals[i];
                if (goal.type == GoalType.CorsairTransportRaid)
                    numGoals += 1;
            }

            return numGoals;
        }
    }
}