using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class CorsairAsteroidBase : Goal
    {
        public const string ID = "CorsairAsteroidBase";
        public override string UID => ID;
        private readonly Empire Pirates;
        private readonly Ship PirateBase;

        public CorsairAsteroidBase() : base(GoalType.CorsairAsteroidBase)
        {
            Pirates    = empire;
            PirateBase = TargetShip;
            Steps = new Func<GoalStep>[]
            {
               SalvageShips
            };
        }
        public CorsairAsteroidBase(Empire owner, Ship ship) : this()
        {
            empire     = owner;
            TargetShip = ship; // This is the Pirate Base
            Pirates    = empire;
            PirateBase = TargetShip;

            Log.Info(ConsoleColor.Green, $"---- New Pirate Asteroid Base in {PirateBase.SystemName} ----");
        }

        GoalStep SalvageShips()
        {
            if (PirateBase == null || !PirateBase.Active)
                return GoalStep.GoalFailed; // Base is destroyed

            SolarSystem system = PirateBase.System;
            for (int i = 0; i < system.ShipList.Count; i++)
            {
                Ship ship = system.ShipList[i];
                if (ship.InRadius(PirateBase.Center, 1200))
                {
                    switch (ship.Name)
                    {
                        case "Corsair-Slaver": ship.QueueTotalRemoval(); break;
                        default:                SalvageShip(ship);       break;
                    }
                }
            }

            return GoalStep.TryAgain;
        }

        void SalvageShip(Ship ship)
        {
            if (ship.IsFreighter)
            {
                ship.QueueTotalRemoval();
                Pirates.CorsairsTryLevelUp();
            }
            else if (ship.isColonyShip) // colonize world?
            {
                ship.QueueTotalRemoval();
            }
            else
            {
                if (ShouldSalvageCombatShip())  // Do we need to level up?
                {
                    ship.QueueTotalRemoval();
                    Pirates.CorsairsTryLevelUp();
                }
                else  // Find a base which orbits a planet and go there
                {
                    if (Pirates.GetClosestCorsairBasePlanet(ship.Center, out Planet planet))
                        ship.AI.OrderToOrbit(planet);
                }
            }
        }

        bool ShouldSalvageCombatShip()
        {
            var empires         = EmpireManager.Empires.Filter(e => !e.isFaction);
            bool needMoreLevels = false;
            for (int i = 0; i < empires.Length; i++)
            {
                Empire victim = empires[i];
                if (Pirates.PirateThreatLevel < victim.PirateThreatLevel)
                {
                    needMoreLevels = true;
                    break;
                }
            }

            return needMoreLevels; 
        }
    }
}