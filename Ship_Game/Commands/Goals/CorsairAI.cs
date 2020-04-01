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
               BoardFreighters,
               CorsairPlan
            };
        }
        public CorsairAI(Empire owner) : this()
        {
            empire = owner;
        }

        GoalStep BoardFreighters()
         {
            Player = EmpireManager.Player;
            if (!ScanFreighters(out Array<Ship> freighters))
                return GoalStep.TryAgain;

            Ship freighter = freighters.RandItem();
            SpawnBoardingForce(freighter);
            return GoalStep.GoToNextStep;
        }

        bool ScanFreighters(out Array<Ship> freighters)
        {
            freighters = new Array<Ship>();
            var planets = Player.GetPlanets();
            for (int i = 0; i < planets.Count; i++)
            {
                Planet planet      = planets[i];
                SolarSystem system = planet.ParentSystem;
                for (int j = 0; j < system.ShipList.Count; j++)
                {
                    Ship ship = system.ShipList[j];
                    if (ship.IsFreighter 
                        && ship.AI.FindGoal(ShipAI.Plan.DropOffGoods, out ShipAI.ShipGoal goal)
                        && ship.InRadius(goal.Trade.ImportTo.Center, 2000))
                    {
                        freighters.AddUnique(ship);
                    }
                }

                if (freighters.Count > 0)
                    return true;
            }

            return false;
        }

        GoalStep CorsairPlan()
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

        void SpawnBoardingForce(Ship freighter)
        {
            Ship.CreateShipAtPoint("Corsair-Slaver", empire, freighter.Center + RandomMath.Vector2D(1000));
        }


    }
}