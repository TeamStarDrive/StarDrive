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
        // Setting a default value here to reduce impact of ship respawn loops.
        public float WarpInhibitionCheckTimer = Ship.InhibitedAtWarpCheckFrequency;
        /// <summary>
        /// While at warp check 10 * a second.
        /// we can adjust this as needed but it should give a nearly visually perfect and functional inhibition.
        /// </summary>
        public const float InhibitedAtWarpCheckFrequency = 0.1f;
        // While at sublight there is no need for quick checks.
        public const float InhibitedAtSubLightCheckFrequency = 1f;

        public bool Inhibited { get; protected set; }
        public bool InhibitedByEnemy { get; protected set; }
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

        Vector3 GetWarpEffectPosition() => Position.ToVec3();

        /// <summary>
        /// Updates timers and see that inhibition needs to be checked.
        /// if so checks all inhibition sources in order of easiest checks. Only the first positive source will be used.
        /// if inhibited call hyperspace return.
        /// </summary>
        protected void UpdateHyperspaceInhibited(FixedSimTime timeStep)
        {
            // ships that cant be effected by inhibiting should not be checked.
            if (MaxFTLSpeed < LightSpeedConstant)
                return;

            // TODO: protect inhibitionTimer and states.
            WarpInhibitionCheckTimer -= timeStep.FixedTime;

            if (WarpInhibitionCheckTimer <= 0)
            {
                // All general inhibition sources should be below.
                // NOTE: if a source cant be in this list reasonably use SetInhibted

                if (RandomEventManager.ActiveEvent?.InhibitWarp == true)
                {
                    SetWarpInhibited(sourceEnemyShip: false, secondsToInhibit: 5f);
                }
                else if (System != null && IsInhibitedByUnfriendlyGravityWell)
                {
                    SetWarpInhibited(sourceEnemyShip: false, secondsToInhibit: 0.5f);
                }
                else if (IsInhibitedFromEnemyShips())
                {
                    SetWarpInhibited(sourceEnemyShip: true, secondsToInhibit: 5f);
                }
                // nothing is inhibiting so reset timer and states
                else
                {
                    // to avoid constantly setting the inhibited state check that it needs to be set.
                    if (Inhibited)
                    {
                        InhibitedByEnemy = false;
                        Inhibited = false;
                    }
                    // reset timer to engine state timers.
                    float unInhibitedCheckTime = engineState == MoveState.Warp ? InhibitedAtWarpCheckFrequency : InhibitedAtSubLightCheckFrequency;
                    WarpInhibitionCheckTimer = unInhibitedCheckTime;
                }
            }

            // TODO: this lightspeed constant check isnt in the right place. Its buried here.
            if (IsSpoolingOrInWarp && (Inhibited || MaxFTLSpeed < LightSpeedConstant))
                HyperspaceReturn();
        }

        /// <summary>
        /// Sets the warp inhibited state of the ship.
        /// by setting the timer to 0f the inhibted flag will be set to false.
        /// NOTE! this can not be used safely outside of the UpdateHyperspaceInhibited method.
        /// The ui portion of the inhibited logic is not setup yet to use it.
        /// </summary>
        protected void SetWarpInhibited(bool sourceEnemyShip, float secondsToInhibit)
        {
            WarpInhibitionCheckTimer = secondsToInhibit;
            Inhibited                = secondsToInhibit > 0;
            InhibitedByEnemy         = Inhibited && sourceEnemyShip;
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
                if (e.WillInhibit(loyalty))
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
