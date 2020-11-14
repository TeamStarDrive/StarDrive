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
        readonly Ship Owner;
        ShipAI AI => Owner.AI;

        public ShipModule[] Engines { get; }
        public ShipModule[] ActiveEngines => Engines.Filter(e=> e.Active);

        public Status EngineStatus { get; private set; }
        public Status ReadyForWarp { get; private set; }
        public Status ReadyForFormationWarp { get; private set; }

        public ShipEngines(Ship owner, ShipModule[] slots)
        {
            Owner   = owner;
            Engines = slots.Filter(module => module.Is(ShipModuleType.Engine));
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
            if (Owner.Inhibited || Owner.EMPdisabled)
                return Status.Poor;

            //add more status based on engine damage.
            return Status.Excellent;
        }

        Status GetFormationWarpReadyStatus()
        {
            if (Owner.fleet == null) return Status.NotApplicable;

            if (!Owner.CanTakeFleetMoveOrders())
                return Status.NotApplicable;

            Status warpStatus = ReadyForWarp;
            if (Owner.engineState == Ship.MoveState.Warp)
            {
                if (warpStatus == Status.Good) return Status.Good;
                if (warpStatus == Status.Poor) return Status.Poor;
                if (warpStatus == Status.Critical) return Status.Good;
            }

            if (Owner.fleet.GetSpeedLimitFor(Owner) < 1) return Status.NotApplicable;

            Vector2 movePosition;
            if (AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal))
            {
                movePosition = goal.MovePosition;
            }
            else
            {
                movePosition = Owner.fleet.FinalPosition;
            }

            if (!Owner.Center.InRadius(Owner.fleet.FinalPosition + Owner.FleetOffset, 1000))
            {
                float facingFleetDirection = Owner.AngleDifferenceToPosition(movePosition);
                if (facingFleetDirection > 0.02)
                    warpStatus = Status.Poor;
            }
            
            return warpStatus;
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

            if (!Owner.IsSpooling && Owner.WarpDuration() < Status.Good)
                return Status.Poor;

            return engineStatus;
        }
    }
}
