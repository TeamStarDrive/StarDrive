using System;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class PirateDefendBase : Goal
    {
        [StarData] public sealed override Ship TargetShip { get; set; }

        Pirates Pirates => Owner.Pirates;
        Ship BaseToDefend => TargetShip;

        [StarDataConstructor]
        public PirateDefendBase(Empire owner) : base(GoalType.PirateDefendBase, owner)
        {
            Steps = new Func<GoalStep>[]
            {
               SendDefenseForce
            };
        }

        public PirateDefendBase(Empire owner, Ship baseToDefend) : this(owner)
        {
            TargetShip = baseToDefend;
            if (Pirates.Verbose)
                Log.Info(ConsoleColor.Green, $"---- Pirates: New {Owner.Name} Defend Base ----");
        }

        GoalStep SendDefenseForce()
        {
            if (BaseToDefend == null || !BaseToDefend.Active)
                return GoalStep.GoalFailed; // Base is destroyed

            if (!BaseToDefend.InCombat)
                return GoalStep.GoalComplete; // Battle is over

            var ourStrength   = BaseToDefend.AI.FriendliesNearby.Sum(s => s.BaseStrength);
            var enemyStrength = BaseToDefend.AI.PotentialTargets.Sum(s => s.BaseStrength);

            if (ourStrength < enemyStrength)
                SendMoreForces();

            return GoalStep.TryAgain;
        }

        void SendMoreForces()
        {
            Ship ship = Pirates.Owner.Random.ItemFilter(Pirates.Owner.OwnedShips,
                s => !s.IsFreighter
                  && !Pirates.SpawnedShips.Contains(s.Id)
                  && s.BaseStrength > 0
                  && !s.InCombat
                  && !s.IsPlatformOrStation
                  && s.AI.State != AIState.Resupply
                  && s.AI.EscortTarget != BaseToDefend);

            if (ship != null)
            {
                ship.AI.ClearOrders();
                ship.AI.AddEscortGoal(BaseToDefend);
            }
        }
    }
}