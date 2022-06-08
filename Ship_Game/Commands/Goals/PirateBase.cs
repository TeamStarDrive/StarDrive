using System;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Universe;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class PirateBase : Goal
    {
        public const string ID = "PirateBase";
        public override string UID => ID;
        private Pirates Pirates;
        [StarData] Ship Base;

        public PirateBase(int id, UniverseState us)
            : base(GoalType.PirateBase, id, us)
        {
            Steps = new Func<GoalStep>[]
            {
               SalvageShips
            };
        }

        public PirateBase(Empire owner, Ship ship, string systemName)
            : this(owner.Universum.CreateId(), owner.Universum)
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

            // Increase sensor range so it would also be seen a bit better on the minimap
            Base.SensorRange *= ((int)(CurrentGame.GalaxySize + 1)).UpperBound(3);
        }

        GoalStep SalvageShips()
        {
            if (Base == null || !Base.Active)
            {
                Pirates.LevelDown();
                return GoalStep.GoalFailed; // Base is destroyed, behead the Director
            }

            if (Base.InCombat) // No base operations while in combat
            {
                CallForHelp();
                return GoalStep.TryAgain;
            }

            Base.ChangeOrdnance(Base.OrdinanceMax / 10); // Slowly replenish the base's ordnance stores

            Ship[] friendlies = Base.AI.FriendliesNearby;
            for (int i = 0; i < friendlies.Length; i++)
            {
                Ship ship = friendlies[i];
                if (ship.IsPlatformOrStation || ship.IsHangarShip)
                    continue; // Do not mess with our own structures

                if (ship.InRadius(Base.Position, Base.Radius + 3000))
                {
                    ship.ChangeOrdnance(ship.OrdinanceMax / 10);
                    Pirates.ProcessShip(ship, Base);
                }
            }

            return GoalStep.TryAgain;
        }

        void CallForHelp()
        {
            if (Pirates.Owner.GetEmpireAI().Goals.Any(g => g.type == GoalType.PirateDefendBase && g.TargetShip == Base))
                return; // Help is coming

            Pirates.AddGoalDefendBase(Pirates.Owner, Base);
        }
    }
}