using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class PirateAI : Goal
    {
        public const string ID = "PirateAI";
        public override string UID => ID;
        private Pirates Pirates;

        public PirateAI() : base(GoalType.PirateAI)
        {
            Steps = new Func<GoalStep>[]
            {  
               PiratePlan
            };
        }
        public PirateAI(Empire owner) : this()
        {
            empire = owner;
        }

        GoalStep PiratePlan()
        {
            if (!empire.WeArePirates)
                return GoalStep.GoalFailed; // This is mainly for save compatibility

            Pirates = empire.Pirates;
            Pirates.Init();
            Pirates.TryLevelUp(alwaysLevelUp: true); // build initial base

            if (!Pirates.GetBases(out Array<Ship> bases))
            {
                Log.Warning($"Could not find a Pirate base for {empire.Name}. Pirate AI is disabled for them!");
                return GoalStep.GoalFailed;
            }

            //Pirates.AddGoalDirectorPayment(EmpireManager.Player); // TODO for testing
            foreach (Empire victim in EmpireManager.MajorEmpires)
                Pirates.AddGoalDirectorPayment(victim);

            return GoalStep.GoalComplete;
        }

        GoalStep CorsairPlanOld() // This is for legacy load save, will be removed later.
        {
            bool alreadyRaiding = empire.GetEmpireAI().HasTaskOfType(MilitaryTask.TaskType.CorsairRaid);
            if (!alreadyRaiding)
            {
                foreach (KeyValuePair<Empire, Relationship> r in empire.AllRelations)
                {
                    if (r.Value.AtWar && r.Key.GetPlanets().Count > 0 && empire.GetShips().Count > 0)
                    {
                        var center = new Vector2();
                        foreach (Ship ship in empire.GetShips())
                            center += ship.Center;
                        center /= empire.GetShips().Count;

                        var task = new MilitaryTask(empire);
                        task.SetTargetPlanet(r.Key.GetPlanets().FindMin(p => p.Center.SqDist(center)));
                        task.TaskTimer = 300f;
                        task.type = MilitaryTask.TaskType.CorsairRaid;
                        empire.GetEmpireAI().AddPendingTask(task);
                    }
                }
            }
            return GoalStep.TryAgain;
        }
    }
}