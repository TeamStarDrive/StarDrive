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
        public Empire Player;
        public CorsairAsteroidBase() : base(GoalType.CorsairAsteroidBase)
        {
            Steps = new Func<GoalStep>[]
            {
               SalvageShips
            };
        }
        public CorsairAsteroidBase(Empire owner, Ship ship) : this()
        {
            empire     = owner;
            TargetShip = ship; // This is the Pirate Base
        }

        GoalStep SalvageShips()
        {
            if (TargetShip == null || !TargetShip.Active)
                return GoalStep.GoalFailed; // Base is destroyed

            SolarSystem system = TargetShip.System;
            for (int i = 0; i < system.ShipList.Count; i++)
            {
                Ship ship = system.ShipList[i];
                switch (ship.Name)
                {
                    case "Corsair-Slaver": break;
                    default:
                        ship.ScuttleTimer = 1f;
                        if (ship.IsFreighter)
                        {
                            // increase pirate threat level
                        }
                        else if (ship.isColonyShip)
                        {
                            // colonize world?
                        }

                        break;
                }
            }

            return GoalStep.TryAgain;
        }
    }
}