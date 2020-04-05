using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class CorsairAI : Goal
    {
        public const string ID = "CorsairAI";
        public override string UID => ID;
        public Empire Player;
        public CorsairAI() : base(GoalType.CorsairAI)
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
            if (!empire.GetCorsairBases(out Array<Ship> bases))
            {
                Log.Warning("Could not find a Corsair base. Pirate AI is disabled!");
                return GoalStep.GoalFailed;
            }

            Ship firstBase    = bases.First;
            EmpireAI empireAi = empire.GetEmpireAI();

            empireAi.Goals.Add(new CorsairAsteroidBase(empire, firstBase, firstBase.SystemName));
            PopulatePirateFightersForCarriers();

            // FB - Pirate main goal can be set per AI empire as well, in the future
            empireAi.Goals.Add(new CorsairMain(empire, EmpireManager.Player));
            empire.SetPirateThreatLevel(1); // Initial Level of the pirates
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

        void PopulatePirateFightersForCarriers()
        {
            empire.ShipsWeCanBuild.Add(empire.data.PirateFighterBasic);
            empire.ShipsWeCanBuild.Add(empire.data.PirateFighterImproved);
            empire.ShipsWeCanBuild.Add(empire.data.PirateFighterAdvanced);
            /*
            empire.ShipsWeCanBuild.Add(empire.data.PirateFrigateBasic);
            empire.ShipsWeCanBuild.Add(empire.data.PirateFrigateImproved);
            empire.ShipsWeCanBuild.Add(empire.data.PirateFrigateAdvanced);
            empire.ShipsWeCanBuild.Add(empire.data.PirateSlaverBasic);
            empire.ShipsWeCanBuild.Add(empire.data.PirateSlaverImproved);
            empire.ShipsWeCanBuild.Add(empire.data.PirateSlaverAdvanced);
            empire.ShipsWeCanBuild.Add(empire.data.PirateBaseBasic);
            empire.ShipsWeCanBuild.Add(empire.data.PirateBaseImproved);
            empire.ShipsWeCanBuild.Add(empire.data.PirateBaseAdvanced);
            empire.ShipsWeCanBuild.Add(empire.data.PirateStationBasic);
            empire.ShipsWeCanBuild.Add(empire.data.PirateStationImproved);
            empire.ShipsWeCanBuild.Add(empire.data.PirateStationAdvanced);
            */
        }
    }
}