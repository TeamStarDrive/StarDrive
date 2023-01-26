using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships;

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
    public EngineStatus EngineStatus { get; private set; }
    public WarpStatus ReadyForWarp { get; private set; }
    public WarpStatus ReadyForFormationWarp { get; private set; }

#if DEBUG
    // this is purely for debugging
    public string FormationStatus;
#endif

    public override string ToString() => $"Status:{EngineStatus} Warp:{ReadyForWarp} FWarp:{ReadyForFormationWarp}";

    public ShipEngines()
    {
    }

    // passing `owner` as a parameter to reduce memory footprint and cache misses
    public void Update(Ship owner)
    {
        // These need to be done in order
        EngineStatus = GetEngineStatus(owner);
        ReadyForWarp = GetWarpReadyStatus(owner);

        if (owner.Fleet == null || owner.AI.State != AIState.FormationMoveTo)
            ReadyForFormationWarp = Status(ReadyForWarp, "");
        else
            ReadyForFormationWarp = GetFormationWarpReadyStatus(owner);
    }

    EngineStatus GetEngineStatus(Ship owner)
    {
        // this should cover most cases,
        // be careful when adding new conditions, because it might be redundant
        if (owner.EnginesKnockedOut || owner.EMPDisabled || owner.Dying || !owner.HasCommand)
            return EngineStatus.Disabled;

        return EngineStatus.Active;
    }

    WarpStatus GetWarpReadyStatus(Ship owner)
    {
        if (EngineStatus == EngineStatus.Disabled || !owner.Active ||
            owner.Inhibited || owner.MaxFTLSpeed < 1)
            return WarpStatus.UnableToWarp;

        if (owner.engineState == Ship.MoveState.Warp)
            return WarpStatus.ReadyToWarp;

        if (owner.Carrier.RecallingFighters())
            return WarpStatus.WaitingOrRecalling;

        if (!owner.IsWarpRangeGood(10000f))
            return WarpStatus.UnableToWarp;

        return WarpStatus.ReadyToWarp;
    }

    // consider ship at final position if 
    public const float AtFinalFleetPos = 1000f;

    WarpStatus Status(WarpStatus s, string status)
    {
    #if DEBUG
        this.FormationStatus = status;
    #endif
        return s;
    }

    WarpStatus GetFormationWarpReadyStatus(Ship owner)
    {
        if (owner.engineState == Ship.MoveState.Warp || !owner.CanTakeFleetMoveOrders())
            return Status(ReadyForWarp, "");

        // we are already at the final position, allow everyone else to FormationWarp
        Vector2 finalPos = owner.Fleet.GetFinalPos(owner);
        if (owner.Position.InRadius(finalPos, AtFinalFleetPos))
            return Status(WarpStatus.ReadyToWarp, "");

        // WARNING: THIS PART GETS COMPLICATED AND VERY EASY TO BREAK FORMATION WARP
        //          Must be in-sync with ShipAI.AddWayPoint()

        //////////////////////////////////////
        //////  FORMATION  WARP  LOGIC  //////
        //////////////////////////////////////

        ShipAI.ShipGoal goal = owner.AI.OrderQueue.PeekFirst;
        // normally it shouldn't be null, but a race condition happens here
        if (goal == null)
            return Status(ReadyForWarp, "");

        // we are still rotating towards the next move position
        if (goal.Plan == ShipAI.Plan.RotateToFaceMovePosition)
            return Status(WarpStatus.WaitingOrRecalling, "Turning"); // tell everyone to plz wait

        // not quite ready yet, we are moving towards last WayPoint position
        // sometimes ships can get stuck with this
        // TODO: Implement new ShipGoal system where every goal gets a timer
        //       this way we can ensure FinalApproach times out. Currently I won't touch this.
        if (goal.Plan == ShipAI.Plan.MakeFinalApproach)
            return Status(WarpStatus.WaitingOrRecalling, "Final Approach"); // tell everyone to plz wait

        // The only other possible state should be MoveToWithin1000
        // Here we must make sure we are facing towards the final target
        if (goal.Plan == ShipAI.Plan.MoveToWithin1000)
        {

            // IMPORTANT: ONLY CHECK AGAINST AI.ThrustTarget, OTHERWISE THE SHIP WILL BE FOREVER STUCK, UNABLE TO WARP!
            Vector2 targetPos = owner.AI.ThrustTarget;
            if (targetPos == Vector2.Zero)
                targetPos = owner.AI.GoalTarget;
            if (targetPos == Vector2.Zero)
                targetPos = owner.Fleet.GetFinalPos(owner);

            float facingFleetDirection = owner.AngleDifferenceToPosition(targetPos);
            // WARNING: BE EXTREMELY CAREFUL WITH THIS ANGLE HERE,
            //          IF YOU MAKE IT TOO SMALL, FORMATION WARP WILL NOT WORK!
            if (facingFleetDirection > RadMath.Deg10AsRads)
                return Status(WarpStatus.WaitingOrRecalling, "Precision Turning");
        }

        // should be ready for warp
        return Status(ReadyForWarp, "");
    }
}
