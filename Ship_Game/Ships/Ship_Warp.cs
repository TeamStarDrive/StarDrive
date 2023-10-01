using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

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
        [StarData] public bool IsSpooling { get; private set; }

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
            UpdateVelocityMax();
            // Check warp status for Fighter recall
            WarpStatus warpStatus = ShipEngines.ReadyForFormationWarp;
            if (warpStatus != WarpStatus.ReadyToWarp)
            {
                if (warpStatus == WarpStatus.WaitingOrRecalling)
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
                engineState = MoveState.Sublight;
                IsSpooling = false;
                UpdateVelocityMax();
            }
        }

        // Used for Remnant portal exit
        public void EmergeFromPortal()
        {
            if (Universe != null
                && Universe.Screen.viewState <= UniverseScreen.UnivScreenState.SystemView)
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
                if (Universe.Events.ActiveEvent?.InhibitWarp == true)
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

        public void UpdateWarpSpooling(FixedSimTime timeStep)
        {
            JumpTimer -= timeStep.FixedTime;

            // sometimes a ship stars warping to escape for resupply or any other reason
            // but hostiles manage to take out all the engines
            if (ShipEngines.ReadyForFormationWarp < WarpStatus.ReadyToWarp)
            {
                IsSpooling = false;
                ResetJumpTimer();
                return;
            }

            if (JumpTimer <= 4.0f)
            {
                if (IsVisibleToPlayer && !Universe.Paused && JumpSfx.IsStopped && JumpSfx.IsReadyToReplay)
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
                    WarpPercent = 1f;
                }
                IsSpooling = false;
                ResetJumpTimer();
            }
        }

        // TODO: This needs optimization, make use of InfluenceTree
        bool IsInhibitedFromEnemyShips()
        {
            for (int x = 0; x < Universe.Empires.Count; x++)
            {
                Empire e = Universe.Empires[x];
                if (e.Inhibitors.Count > 0 && e.WillInhibit(Loyalty))
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

        /// @return TRUE if ship can effectively warp the given distance in 1 jump
        public bool IsWarpRangeGood(float neededRange)
        {
            if (IsLaunching) 
                return false;

            if (neededRange <= 0f) return true;
            float powerDuration = NetPower.PowerDuration(MoveState.Warp, PowerCurrent);
            return ShipStats.IsWarpRangeGood(neededRange, powerDuration, MaxFTLSpeed);
        }

        public void SetWarpPercent(FixedSimTime timeStep, float warpPercent)
        {
            if      (WarpPercent < warpPercent) WarpPercent += timeStep.FixedTime;
            else if (WarpPercent > warpPercent) WarpPercent -= timeStep.FixedTime;
            WarpPercent = WarpPercent.Clamped(0.05f, 1f);
        }
    }
}
