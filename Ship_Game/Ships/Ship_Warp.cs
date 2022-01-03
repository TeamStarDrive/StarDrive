using Microsoft.Xna.Framework;
using Ship_Game.Audio;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        // This is the light speed threshold
        // Warp ships cannot go slower than this
        public const float LightSpeedConstant = 3000f;

        /// <summary> This is point at which relativistic effects begin to reduce targeting ability </summary>
        public const float TargetErrorFocalPoint = 2300;

        // This is the maximum STL speed that ships can achieve
        // This is both for balancing and for realism, since sub-light
        // ships should not get even close to light speed
        public const float MaxSubLightSpeed = 1500f;

        // Limit in Turn Speed in Radians
        public const float MaxTurnRadians = 3.14159f;

        // Ship is trying to enter Warp
        public bool IsSpooling { get; private set; }

        // Timer for doing Warp Inhibit checks
        // Setting a default value here to reduce impact of ship respawn loops.
        public float InhibitedCheckTimer = 0.1f;

        public InhibitionType InhibitionSource { get; protected set; }
        public bool Inhibited { get; protected set; }
        public MoveState engineState;

         // [0.0 to 1.0], current Warp thrust percentage
        public float WarpPercent { get; private set; } = 1f;

        public bool BaseCanWarp => ShipData.BaseCanWarp;

        public bool IsSpoolingOrInWarp => IsSpooling || engineState == MoveState.Warp;
        public bool IsInWarp => engineState == MoveState.Warp;
        // safe Warp out distance so the ship still has time to slow down
        public float WarpOutDistance => 3200f + MaxSTLSpeed * 3f;
        public string WarpState => engineState == MoveState.Warp ? "FTL" : "Sublight";

        public void ResetJumpTimer()
        {
            JumpTimer = Stats.FTLSpoolTime;
        }

        public static string GetStartWarpCue(IEmpireData data, int surfaceArea)
        {
            if (data.WarpStart != null)
                return data.WarpStart;
            if (surfaceArea < 60)
                return "sd_warp_start_small";
            return surfaceArea > 350 ? "sd_warp_start_large" : "sd_warp_start_02";
        }

        public static string GetEndWarpCue(IEmpireData data, int surfaceArea)
        {
            if (data.WarpStart != null)
                return data.WarpEnd;
            if (surfaceArea < 60)
                return "sd_warp_stop_small";
            return surfaceArea > 350 ? "sd_warp_stop_large" : "sd_warp_stop";
        }

        string GetStartWarpCue() => GetStartWarpCue(Loyalty.data, SurfaceArea);
        string GetEndWarpCue()   => GetEndWarpCue(Loyalty.data, SurfaceArea);

        public void EngageStarDrive()
        {
            // Check warp status for Fighter recall
            Status warpStatus = ShipEngines.ReadyForFormationWarp;
            if (warpStatus <= Status.Poor)
            {
                if (warpStatus == Status.Critical)
                    HyperspaceReturn();
            }
            else if (!IsSpoolingOrInWarp && !Inhibited)
            {
                IsSpooling = true;
                ResetJumpTimer();
            }
        }

        public void HyperspaceReturn()
        {
            if (IsSpoolingOrInWarp)
            {
                // stop the SFX and always reset the replay timeout
                JumpSfx.Stop();

                if (engineState == MoveState.Warp && IsVisibleToPlayer)
                {
                    GameAudio.PlaySfxAsync(GetEndWarpCue(), SoundEmitter);
                    FTLManager.ExitFTL(GetWarpEffectPosition, Direction3D, Radius);
                }
                engineState     = MoveState.Sublight;
                IsSpooling      = false;
                VelocityMaximum = MaxSTLSpeed;
                SpeedLimit      = VelocityMaximum;
            }
        }

        // Used for Remnant portal exit
        public void EmergeFromPortal()
        {
            if (Universe != null
                && Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                FTLManager.ExitFTL(GetWarpEffectPosition, Direction3D, Radius);
            }
        }

        Vector3 GetWarpEffectPosition() => Position.ToVec3();

        /// Checks whether the ship is in warp Inhibition zone
        /// @return TRUE if ship is inhibited
        protected bool UpdateHyperspaceInhibited(FixedSimTime timeStep, bool warpingOrSpooling)
        {
            InhibitedCheckTimer -= timeStep.FixedTime;

            if (InhibitedCheckTimer <= 0f)
            {
                if (RandomEventManager.ActiveEvent?.InhibitWarp == true)
                {
                    SetWarpInhibited(source: InhibitionType.GlobalEvent, secondsToInhibit: 5f);
                    return true;
                }
                if (System != null && IsInhibitedByUnfriendlyGravityWell)
                {
                    SetWarpInhibited(source: InhibitionType.GravityWell, secondsToInhibit: 1f);
                    return true;
                }
                if (IsInhibitedFromEnemyShips())
                {
                    SetWarpInhibited(source: InhibitionType.EnemyShip, secondsToInhibit: 5f);
                    return true;
                }

                // nothing is inhibiting so reset timer and states
                Inhibited = false;
                InhibitionSource = InhibitionType.None;

                // set the next inhibition check time
                // when we are warp, timer=0 because we need to check every frame
                InhibitedCheckTimer = warpingOrSpooling ? 0f : Stats.FTLSpoolTime;
                return false;
            }
            return Inhibited;
        }

        public enum InhibitionType
        {
            None,
            GlobalEvent,
            GravityWell,
            EnemyShip
        }

        /// Sets the ship as inhibited
        public void SetWarpInhibited(InhibitionType source, float secondsToInhibit)
        {
            Inhibited = true;
            InhibitionSource = source;
            InhibitedCheckTimer = secondsToInhibit;
        }

        // TODO: move this to ship engines.
        public void UpdateWarpSpooling(FixedSimTime timeStep)
        {
            JumpTimer -= timeStep.FixedTime;

            if (ShipEngines.ReadyForFormationWarp < Status.Poor)
            {
                Log.Info("ship not ready for warp but spool timer was activated.\n " +
                        $"               warp Status: {ShipEngines.ReadyForFormationWarp} \n " +
                        $"               Fleet:       {Fleet}\n " +
                        $"               Ship:        {this} ");
            }
            else
            {
                if (JumpTimer <= 4.0f)
                {
                    if (IsVisibleToPlayer
                        && !Universe.Paused && JumpSfx.IsStopped && JumpSfx.IsReadyToReplay)
                    {
                        JumpSfx.PlaySfxAsync(GetStartWarpCue(), SoundEmitter, replayTimeout: 4.0f);
                    }
                }

                if (JumpTimer <= 0.1f)
                {
                    if (engineState == MoveState.Sublight)
                    {
                        if (IsVisibleToPlayer)
                            FTLManager.EnterFTL(Position.ToVec3(), Direction3D, Radius);
                        engineState = MoveState.Warp;
                    }
                    IsSpooling = false;
                    ResetJumpTimer();
                }
            }
        }

        // todo: Move to action queue
        bool IsInhibitedFromEnemyShips()
        {
            for (int x = 0; x < EmpireManager.Empires.Count; x++)
            {
                Empire e = EmpireManager.Empires[x];
                if (e.WillInhibit(Loyalty))
                {
                    for (int i = 0; i < e.Inhibitors.Count; ++i)
                    {
                        Ship ship = e.Inhibitors[i];
                        if (ship != null && Position.InRadius(ship.Position, ship.InhibitionRadius))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a ship status based on how well it can reach the wanted range in warp before running out of power.
        /// by default it will try and warp 300k units which is the current diameter of a solar system.
        /// </summary>
        public Status WarpRangeStatus(float neededRange = 300000)
        {
            float powerDuration = NetPower.PowerDuration(MoveState.Warp, PowerCurrent);
            return ToShipStatus(powerDuration * MaxFTLSpeed, neededRange);
        }

        public void SetWarpPercent(FixedSimTime timeStep, float warpPercent)
        {
            if      (WarpPercent < warpPercent) WarpPercent += timeStep.FixedTime;
            else if (WarpPercent > warpPercent) WarpPercent -= timeStep.FixedTime;
            WarpPercent = WarpPercent.Clamped(0.05f, 1f);
        }
    }
}
