using Microsoft.Xna.Framework;
using Ship_Game.AI;

namespace Ship_Game.Ships
{
    /// <summary>
    /// This should be doing a lot more than it is.
    /// 
    /// </summary>
    public class ShipEngines
    {
        Ship Owner;
        ShipAI AI => Owner.AI;

        public ShipModule[] Engines { get; private set; }
        public ShipModule[] ActiveEngines => Engines.Filter(e=> e.Active);

        public Status EngineStatus { get; private set; }
        public Status ReadyForWarp { get; private set; }
        public Status ReadyForFormationWarp { get; private set; }

        public ShipEngines(Ship owner, ShipModule[] slots)
        {
            Owner   = owner;
            Engines = slots.Filter(module => module.Is(ShipModuleType.Engine));
        }

        public void Dispose()
        {
            Owner = null;
            Engines = null;
        }

        public void Update()
        {
            // These need to be done in order
            EngineStatus          = GetEngineStatus();
            ReadyForWarp          = GetWarpReadyStatus();
            ReadyForFormationWarp = GetFormationWarpReadyStatus();
        }

        Status GetEngineStatus()
        {
            if (Owner.EnginesKnockedOut)
                return Status.Critical;
            if (Owner.Inhibited || Owner.EMPDisabled)
                return Status.Poor;

            //add more status based on engine damage.
            return Status.Excellent;
        }

        Status GetFormationWarpReadyStatus()
        {
            if (Owner.Fleet == null || Owner.AI.State != AIState.FormationWarp) 
                return ReadyForWarp;

            if (!Owner.CanTakeFleetMoveOrders())
                return ReadyForWarp;

            if (Owner.engineState == Ship.MoveState.Warp && ReadyForWarp <= Status.Good)
                return ReadyForWarp;

            float speedLimit = Owner.Fleet.GetSpeedLimitFor(Owner);
            if (speedLimit < 1 || speedLimit == float.MaxValue)
                return Status.NotApplicable;

            if (!Owner.Position.InRadius(Owner.Fleet.FinalPosition + Owner.FleetOffset, 1000)
                && Owner.AI.State != AIState.AwaitingOrders)
            {
                Vector2 movePosition;
                if (AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal) && goal.MovePosition != Vector2.Zero)
                    movePosition = goal.MovePosition;
                else
                    movePosition = Owner.Fleet.FinalPosition;

                float facingFleetDirection = Owner.AngleDifferenceToPosition(movePosition);
                if (facingFleetDirection > 0.02)
                    return Status.Poor;
            }
            return ReadyForWarp;
        }

        Status GetWarpReadyStatus()
        {
            if (Owner.MaxFTLSpeed < 1 || !Owner.Active)
                return Status.NotApplicable;

            Status engineStatus = GetEngineStatus();

            // less than average means the ship engines are not warp ready ATM;

            if (Owner.engineState == Ship.MoveState.Warp)
                return Status.Good;

            if (engineStatus < Status.Average)
                return Status.Critical;

            if (Owner.Carrier.RecallingFighters())
                return Status.Poor;

            return Owner.WarpRangeStatus(7500f);
        }
    }
}
