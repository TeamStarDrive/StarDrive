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
        private Empire Pirates;
        private Ship PirateBase;

        public CorsairAsteroidBase() : base(GoalType.CorsairAsteroidBase)
        {
            Steps = new Func<GoalStep>[]
            {
               SalvageShips
            };
        }
        public CorsairAsteroidBase(Empire owner, Ship ship, string systemName) : this()
        {
            empire     = owner;
            TargetShip = ship;
            PostInit();
            Log.Info(ConsoleColor.Green, $"---- New Pirate Asteroid Base in {systemName} ----");
        }

        public sealed override void PostInit()
        {
            Pirates    = empire;
            PirateBase = TargetShip;
        }

        GoalStep SalvageShips()
        {
            if (PirateBase == null || !PirateBase.Active)
            {
                Pirates.ReduceOverallPirateThreatLevel();
                return GoalStep.GoalFailed; // Base is destroyed, behead the Director
            }

            var friendlies = PirateBase.AI.FriendliesNearby;
            using (friendlies.AcquireReadLock())
            {
                for (int i = 0; i < friendlies.Count; i++)
                {
                    Ship ship = friendlies[i];
                    if (ship.IsPlatformOrStation)
                        continue; // Do not mess with our own structures

                    if (ship.InRadius(PirateBase.Center, 1200))
                    {
                        // Default Corsair Raiders are removed with no reward
                        if (ship.shipData.ShipStyle == Pirates.data.Singular) // when spawning ships change their style to corsair
                            ship.QueueTotalRemoval();
                        else
                            SalvageShip(ship);
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
            else // This is a combat ship
            {
                if (ShouldSalvageCombatShip())  // Do we need to level up?
                {
                    ship.QueueTotalRemoval();
                    Pirates.CorsairsTryLevelUp();
                }
                else  // Find a base which orbits a planet and go there
                {
                    if (ship.AI.State != AIState.Orbit)
                    {
                        if (Pirates.GetClosestCorsairBasePlanet(ship.Center, out Planet planet))
                            ship.AI.OrderToOrbit(planet);
                    }
                }

                // We can use this ship in future endeavors, ha ha ha!
                if (!Pirates.ShipsWeCanBuild.Contains(ship.Name))
                    Pirates.ShipsWeCanBuild.Add(ship.Name);
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