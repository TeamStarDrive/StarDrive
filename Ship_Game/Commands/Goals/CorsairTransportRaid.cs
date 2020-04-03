using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class CorsairTransportRaid : Goal
    {
        public const string ID = "CorsairTransportRaid";
        public override string UID => ID;
        public CorsairTransportRaid() : base(GoalType.CorsairTransportRaid)
        {
            Steps = new Func<GoalStep>[]
            {
               DetectAndSpawnRaidForce,
               CheckIfHijacked
            };
        }
        public CorsairTransportRaid(Empire owner, Empire targetEmpire) : this()
        {
            empire       = owner;
            TargetEmpire = targetEmpire;
        }

        GoalStep DetectAndSpawnRaidForce()
        {
            if (!empire.GetRelations(TargetEmpire).AtWar)
                return GoalStep.GoalFailed; // They paid

            int nearPlanetRaidChange = TargetEmpire.PirateThreatLevel * 10;
            if (RandomMath.RollDice(nearPlanetRaidChange))
            {
                if (ScanFreightersNearPlanets(out Array<Ship> freighters))
                {
                    SpawnBoardingForce(freighters);
                    return GoalStep.GoToNextStep;
                }
            }
            else
            {
                if (ScanFreightersAtWarp(out Ship freighter))
                {
                    SpawnBoardingShip(freighter, freighter.Center + freighter.Velocity * 3);
                    return GoalStep.GoToNextStep;
                }
            }

            return GoalStep.GoalFailed;
        }

        GoalStep CheckIfHijacked()
        {

            if (!TargetShip.Active || TargetShip.loyalty != empire && !TargetShip.Inhibited)
                return GoalStep.GoalFailed; // Target destroyed or escaped

            return TargetShip.loyalty == empire ? GoalStep.GoalComplete :  GoalStep.TryAgain;
        }

        bool ScanFreightersAtWarp(out Ship freighter)
        {
            freighter       = null;
            var targetShips = TargetEmpire.GetShips();
            for (int i = 0; i < targetShips.Count; i++)
            {
                Ship ship = targetShips[i];
                if (ship.IsFreighter && ship.AI.FindGoal(ShipAI.Plan.DropOffGoods, out ShipAI.ShipGoal goal) 
                                     &&  ship.IsInWarp)
                {
                    freighter = ship;
                    break;
                }
            }

            return freighter != null;
        }

        bool ScanFreightersNearPlanets(out Array<Ship> freighters)
        {
            freighters  = new Array<Ship>();
            var planets = TargetEmpire.GetPlanets();
            for (int i = 0; i < planets.Count; i++)
            {
                Planet planet      = planets[i];
                SolarSystem system = planet.ParentSystem;
                for (int j = 0; j < system.ShipList.Count; j++)
                {
                    Ship ship = system.ShipList[j];
                    if (ship.IsFreighter
                        && ship.AI.FindGoal(ShipAI.Plan.DropOffGoods, out _)
                        && ship.InRadius(planet.Center, planet.ObjectRadius + 2000))
                    {
                        freighters.AddUnique(ship);
                    }
                }

                if (freighters.Count > 0)
                    return true;
            }

            return false;
        }

        void SpawnBoardingShip(Ship freighter, Vector2 where)
        {
            if (where == Vector2.Zero)
                where = freighter.Center + RandomMath.Vector2D(1000);

            TargetShip        = freighter; // This is the main target, we want this to arrive to our base
            Ship boardingShip = Ship.CreateShipAtPoint("Corsair-Slaver", empire, where);
            boardingShip?.AI.OrderAttackSpecificTarget(freighter);
        }

        void SpawnBoardingForce(Array<Ship> freighters)
        {
            // Launch a board ship per freighter, but the maximum  is the threat level
            for (int i = 0; i < freighters.Count.UpperBound(TargetEmpire.PirateThreatLevel); i++)
            {
                Ship freighter = freighters[i];
                Ship.CreateShipAtPoint("Corsair-Slaver", empire, freighter.Center + RandomMath.Vector2D(1000));
            }

            // also spawn escort ships by planet defense str / number of freighters
        }
    }
}