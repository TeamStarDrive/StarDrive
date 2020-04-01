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
        public Empire Player;
        public CorsairTransportRaid() : base(GoalType.CorsairTransportRaid)
        {
            Steps = new Func<GoalStep>[]
            {
               DetectAndSpawnRaidForce
            };
        }
        public CorsairTransportRaid(Empire owner) : this()
        {
            empire = owner;
        }

        GoalStep DetectAndSpawnRaidForce()
        {
            Player = EmpireManager.Player;
            int nearPlanetRaidChange = Player.PirateThreatLevel * 10;
            if (RandomMath.RollDice(nearPlanetRaidChange))
            {
                if (ScanFreightersNearPlanets(out Array<Ship> freighters))
                {
                    SpawnBoardingForce(freighters);
                    return GoalStep.GoalComplete;
                }
            }
            else
            {
                if (ScanFreightersAtWarp(out Ship freighter))
                {
                    Ship.CreateShipAtPoint("Corsair-Slaver", empire, freighter.Center);
                    return GoalStep.GoalComplete;
                }
            }

            return GoalStep.GoalFailed;
        }

        bool ScanFreightersAtWarp(out Ship freighter)
        {
            freighter       = null;
            Player          = EmpireManager.Player;
            var playerShips = Player.GetShips();
            for (int i = 0; i < playerShips.Count; i++)
            {
                Ship ship = playerShips[i];
                if (ship.IsFreighter && ship.AI.FindGoal(ShipAI.Plan.DropOffGoods, out _) 
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
            Player      = EmpireManager.Player;
            freighters  = new Array<Ship>();
            var planets = Player.GetPlanets();
            for (int i = 0; i < planets.Count; i++)
            {
                Planet planet = planets[i];
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

        void SpawnBoardingShip(Ship freighter)
        {
            TargetShip = freighter; // This is the main target, we want this to arrive to our base
            Ship.CreateShipAtPoint("Corsair-Slaver", empire, freighter.Center + RandomMath.Vector2D(1000));
        }

        void SpawnBoardingForce(Array<Ship> freighters)
        {
            for (int i = 0; i < freighters.Count; i++)
            {
                Ship freighter = freighters[i];
                Ship.CreateShipAtPoint("Corsair-Slaver", empire, freighter.Center + RandomMath.Vector2D(1000));
                // also spawn escort ships by planet defense str / number of freighters
            }
        }
    }
}