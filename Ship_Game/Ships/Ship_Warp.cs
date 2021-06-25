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
        public bool IsSpooling { get; private set; }
        public float InhibitedTimer;
        public bool Inhibited { get; private set; }
        public bool InhibitedByEnemy { get; private set; }
        public MoveState engineState;

         // [0.0 to 1.0], current Warp thrust percentage
        public float WarpPercent { get; private set; } = 1f;

        public bool BaseCanWarp;

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

        string GetStartWarpCue() => GetStartWarpCue(loyalty.data, SurfaceArea);
        string GetEndWarpCue()   => GetEndWarpCue(loyalty.data, SurfaceArea);

        public void EngageStarDrive() // added by gremlin: Fighter recall and stuff
        {
            var warpStatus = ShipEngines.ReadyForFormationWarp;

            if (warpStatus <= Status.Poor)
            {
                if (warpStatus == Status.Critical) HyperspaceReturn();
            }
            else if (!IsSpoolingOrInWarp)
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
            if (Empire.Universe != null 
                && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                FTLManager.ExitFTL(GetWarpEffectPosition, Direction3D, Radius);
            }
        }

        Vector3 GetWarpEffectPosition() => Center.ToVec3();

        void UpdateHyperspaceInhibited(FixedSimTime timeStep)
        {
            InhibitedTimer -= timeStep.FixedTime;
            if (InhibitedTimer <= 0f) // timer has run out, lets do the expensive inhibit check
            {
                // if we're inside a system, check every frame
                if (System != null && IsInhibitedByUnfriendlyGravityWell)
                {
                    InhibitedTimer = 0.5f;
                }

                Inhibited = InhibitedTimer > 0f;
            }
            // already inhibited, just wait for the timer to run out before checking again
            else
            {
                // @note InhibitedTimer might be set from where ever
                Inhibited = true;
            }

            if (IsSpoolingOrInWarp && (Inhibited || MaxFTLSpeed < LightSpeedConstant))
                HyperspaceReturn();
        }

        // TODO: move this to ship engines. 
        public void UpdateWarpSpooling(FixedSimTime timeStep)
        {
            if (!IsSpooling || Inhibited || MaxFTLSpeed < LightSpeedConstant) return;

            JumpTimer -= timeStep.FixedTime;

            if (ShipEngines.ReadyForFormationWarp < Status.Poor)
            {
                Log.Info($"ship not ready for warp but spool timer was activated.\n " +
                            $"               warp Status: {ShipEngines.ReadyForFormationWarp} \n " +
                            $"               Fleet:       {fleet}\n " +
                            $"               Ship:        {this} ");
            }
            else
            {
                if (JumpTimer <= 4.0f)
                {
                    if (IsVisibleToPlayer
                        && !Empire.Universe.Paused && JumpSfx.IsStopped && JumpSfx.IsReadyToReplay)
                    {
                        JumpSfx.PlaySfxAsync(GetStartWarpCue(), SoundEmitter, replayTimeout: 4.0f);
                    }
                }

                if (JumpTimer <= 0.1f)
                {
                    if (engineState == MoveState.Sublight)
                    {
                        if (IsVisibleToPlayer)
                            FTLManager.EnterFTL(Center.ToVec3(), Direction3D, Radius);
                        engineState = MoveState.Warp;
                    }
                    IsSpooling = false;
                    ResetJumpTimer();
                }
            }
        }

        // todo: Move to action queue
        void UpdateInhibitedFromEnemyShips()
        {
            InhibitedByEnemy = false;
            foreach (Empire e in EmpireManager.Empires)
            {
                if (e.WillInhibit(loyalty))
                {
                    for (int i = 0; i < e.Inhibitors.Count; ++i)
                    {
                        Ship ship = e.Inhibitors[i];
                        if (ship != null && Center.InRadius(ship.Position, ship.InhibitionRadius))
                        {
                            Inhibited = true;
                            InhibitedByEnemy = true;
                            InhibitedTimer = 5f;
                            return;
                        }
                    }
                }
            }
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
