using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class PirateBase : Goal
    {
        public const string ID = "PirateBase";
        public override string UID => ID;
        private Pirates Pirates;
        private Ship Base;

        public PirateBase() : base(GoalType.PirateBase)
        {
            Steps = new Func<GoalStep>[]
            {
               SalvageShips
            };
        }
        public PirateBase(Empire owner, Ship ship, string systemName) : this()
        {
            empire     = owner;
            TargetShip = ship;
            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Pirates: New {empire.Name} Base in {systemName} ----");
        }

        public sealed override void PostInit()
        {
            Pirates    = empire.Pirates;
            Base       = TargetShip;
        }

        GoalStep SalvageShips()
        {
            if (Base == null || !Base.Active)
            {
                Pirates.LevelDown();
                return GoalStep.GoalFailed; // Base is destroyed, behead the Director
            }

            var friendlies = Base.AI.FriendliesNearby;
            using (friendlies.AcquireReadLock())
            {
                for (int i = 0; i < friendlies.Count; i++)
                {
                    Ship ship = friendlies[i];
                    if (ship.IsPlatformOrStation)
                        continue; // Do not mess with our own structures

                    if (ship.InRadius(Base.Center, 1200))
                        Pirates.ProcessShip(ship);
                }
            }

            return GoalStep.TryAgain;
        }
    }
}