using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class CorsairAsteroidBase : Goal
    {
        public const string ID = "CorsairAsteroidBase";
        public override string UID => ID;
        private Pirates Corsairs;
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
            Log.Info(ConsoleColor.Green, $"---- New Corsair Asteroid Base in {systemName} ----");
        }

        public sealed override void PostInit()
        {
            Corsairs   = empire.Pirates;
            PirateBase = TargetShip;
        }

        GoalStep SalvageShips()
        {
            if (PirateBase == null || !PirateBase.Active)
            {
                Corsairs.LevelDown();
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
                        if (ship.shipData.ShipStyle == Corsairs.ShipStyle) 
                            ship.QueueTotalRemoval();
                        else
                            Corsairs.SalvageShip(ship);
                    }
                }
            }

            return GoalStep.TryAgain;
        }
    }
}