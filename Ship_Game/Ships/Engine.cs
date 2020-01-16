using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.AI;

namespace Ship_Game.Ships
{
    /// <summary>
    /// This should be doing a lot more than it is.
    /// 
    /// </summary>
    public class Engine
    {
        readonly Ship Owner;
        ShipAI AI => Owner.AI;

        public ShipModule[] Engines { get; }
        public ShipModule[] ActiveEngines => Engines.Filter(e=> e.Active);

        public Status EngineStatus { get; private set; }
        public Status ReadyForWarp { get; private set; }
        public Status ReadyForFormationWarp { get; private set; }

        public Engine(Ship owner, ShipModule[] slots)
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

            if (AI.State == AIState.Refit || AI.State == AIState.Resupply)
                return Status.NotApplicable;

            Status warpStatus = ReadyForWarp;

            if (warpStatus == Status.Poor)                return Status.Poor;
            if (warpStatus == Status.Critical)            return Status.Good;
            //if (Owner.engineState == Ship.MoveState.Warp) return Status.Poor;

            Vector2 movePosition;
            if (AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal goal))
            {
                movePosition = goal.MovePosition;
            }
            else
            {
                movePosition = Owner.fleet.FinalPosition;
            }

            Vector2 directionToFleetMove = Owner.fleet.FinalPosition;
            float facingFleetDirection = Owner.AngleDifferenceToPosition(movePosition);

            if (facingFleetDirection > 0.1f) return Status.Poor;

            return warpStatus;
        }

        Status GetWarpReadyStatus()
        {
            if (Owner.MaxFTLSpeed < 1 || !Owner.Active)
                return Status.NotApplicable;

            Status engineStatus = GetEngineStatus();

            // less than average means the ship engines are not warp capable ATM;
            if (engineStatus < Status.Average)
                return Status.Critical;

            if (Owner.Carrier.RecallingFighters())
                return Status.Poor;

            if (!Owner.IsSpooling && Owner.WarpDuration() < Status.Good)
                return Status.Poor;

            if (Owner.engineState == Ship.MoveState.Warp)
                return Status.Excellent;

            return engineStatus;
        }
    }
}
