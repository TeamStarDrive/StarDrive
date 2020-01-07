using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class CorsairAI :Goal
    {
        public const string ID = "CorsairAI";
        public override string UID => ID;
        public CorsairAI() : base(GoalType.DeepSpaceConstruction)
        {
            Steps = new Func<GoalStep>[]
            {
               CorsairPlan
            };
        }
        public CorsairAI(Empire owner) : this()
        {
            empire = owner;
        }

        GoalStep CorsairPlan()
        {
            bool alreadyAttacking = false;
            // @todo Wtf?
            foreach (MilitaryTask task in empire.GetEmpireAI().TaskList)
            {
                if (task.type != MilitaryTask.TaskType.CorsairRaid)
                    return GoalStep.TryAgain;
                alreadyAttacking = true;
            }

            if (!alreadyAttacking)
            {
                foreach (KeyValuePair<Empire, Relationship> r in empire.AllRelations)
                {
                    if (!r.Value.AtWar || r.Key.GetPlanets().Count <= 0 || empire.GetShips().Count <= 0)
                    {
                        continue;
                    }

                    var center = new Vector2();
                    foreach (Ship ship in empire.GetShips())
                        center += ship.Center;
                    center /= empire.GetShips().Count;

                    var task = new MilitaryTask(empire);
                    task.SetTargetPlanet(r.Key.GetPlanets().FindMin(p => p.Center.SqDist(center)));
                    task.TaskTimer = 300f;
                    task.type = MilitaryTask.TaskType.CorsairRaid;
                    empire.GetEmpireAI().TaskList.Add(task);
                }
            }

            return GoalStep.TryAgain;
        }
    }
}