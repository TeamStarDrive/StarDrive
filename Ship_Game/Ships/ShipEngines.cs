using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships
{
    public enum WarpStatus
    {
        // This ship is not able to warp because of damage, inhibition, EMP damage, ...
        UnableToWarp,

        // This ship is waiting for other ships in the formation, or is recalling fighters
        WaitingOrRecalling,

        // Ship is completely ready to warp
        ReadyToWarp,

        // DO NOT ADD ANY MORE STATES HERE, IT WILL BREAK ALL WARP STATUS LOGIC 100%
    }

    public enum EngineStatus
    {
        // All engines on this ship have been destroyed or disabled by EMP
        Disabled,

        // Engines are up and running
        Active,
    }

    public class ShipEngines
    {
        Ship Owner;
        ShipAI AI => Owner.AI;

        public ShipModule[] Engines { get; private set; }
        public ShipModule[] ActiveEngines => Engines.Filter(e => e.Active && (e.Powered || e.PowerDraw <= 0f));

        public EngineStatus EngineStatus { get; private set; }
        public WarpStatus ReadyForWarp { get; private set; }
        public WarpStatus ReadyForFormationWarp { get; private set; }

        public override string ToString() => $"Status:{EngineStatus} Warp:{ReadyForWarp} FWarp:{ReadyForFormationWarp}";

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
            EngineStatus = GetEngineStatus();
            ReadyForWarp = GetWarpReadyStatus();
            ReadyForFormationWarp = GetFormationWarpReadyStatus();
        }

        EngineStatus GetEngineStatus()
        {
            // this should cover most cases,
            // be careful when adding new conditions, because it might be redundant
            if (Owner.EnginesKnockedOut || Owner.EMPDisabled || Owner.Dying || !Owner.HasCommand)
                return EngineStatus.Disabled;

            return EngineStatus.Active;
        }

        WarpStatus GetWarpReadyStatus()
        {
            if (EngineStatus == EngineStatus.Disabled || !Owner.Active ||
                Owner.Inhibited || Owner.MaxFTLSpeed < 1)
                return WarpStatus.UnableToWarp;

            if (Owner.engineState == Ship.MoveState.Warp)
                return WarpStatus.ReadyToWarp;

            if (Owner.Carrier.RecallingFighters())
                return WarpStatus.WaitingOrRecalling;

            if (!Owner.IsWarpRangeGood(10000f))
                return WarpStatus.UnableToWarp;

            return WarpStatus.ReadyToWarp;
        }

        // consider ship at final position if 
        public const float AtFinalFleetPos = 1000f;

        WarpStatus GetFormationWarpReadyStatus()
        {
            if (Owner.Fleet == null || Owner.AI.State != AIState.FormationMoveTo) 
                return ReadyForWarp;

            if (!Owner.CanTakeFleetMoveOrders())
                return ReadyForWarp;

            if (Owner.engineState == Ship.MoveState.Warp)
                return ReadyForWarp;

            // we are already at the final position, allow everyone else to FormationWarp
            Vector2 finalPos = Owner.Fleet.GetFinalPos(Owner);
            if (Owner.Position.InRadius(finalPos, AtFinalFleetPos))
                return WarpStatus.ReadyToWarp;

            // WARNING: THIS PART GETS COMPLICATED AND VERY EASY TO BREAK FORMATION WARP
            //          Must be in-sync with ShipAI.AddWayPoint()

            //////////////////////////////////////
            //////  FORMATION  WARP  LOGIC  //////
            //////////////////////////////////////

            ShipAI.ShipGoal goal = Owner.AI.OrderQueue.PeekFirst;

            // we are still rotating towards the next move position
            if (goal.Plan == ShipAI.Plan.RotateToFaceMovePosition)
                return WarpStatus.WaitingOrRecalling; // tell everyone to plz wait

            // not quite ready yet, we are moving towards last WayPoint position
            // sometimes ships can get stuck with this
            // TODO: Implement new ShipGoal system where every goal gets a timer
            //       this way we can ensure FinalApproach times out. Currently I won't touch this.
            if (goal.Plan == ShipAI.Plan.MakeFinalApproach)
                return WarpStatus.WaitingOrRecalling; // tell everyone to plz wait

            // The only other possible state should be MoveToWithin1000
            // Here we must make sure we are facing towards the final target
            if (goal.Plan == ShipAI.Plan.MoveToWithin1000)
            {
                // IMPORTANT: ONLY CHECK AGAINST AI.ThrustTarget, OTHERWISE THE SHIP WILL BE FOREVER STUCK, UNABLE TO WARP!
                Vector2 targetPos = AI.ThrustTarget;
                if (targetPos == Vector2.Zero)
                    targetPos = AI.GoalTarget;
                if (targetPos == Vector2.Zero)
                    targetPos = Owner.Fleet.GetFinalPos(Owner);

                float facingFleetDirection = Owner.AngleDifferenceToPosition(targetPos);
                // WARNING: BE EXTREMELY CAREFUL WITH THIS ANGLE HERE,
                //          IF YOU MAKE IT TOO SMALL, FORMATION WARP WILL NOT WORK!
                if (facingFleetDirection > RadMath.Deg10AsRads)
                    return WarpStatus.WaitingOrRecalling;
            }

            // should be ready for warp
            return ReadyForWarp;
        }
    }
}
