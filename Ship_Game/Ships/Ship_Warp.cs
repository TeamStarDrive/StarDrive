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
        /// <summary>
        /// While at warp check 10 * a second.
        /// we can adjust this as needed but it should give a visually perfect and functional inhibition.
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
            // TODO: protect inhibitionTimer and states.
            InhibitedTimer -= timeStep.FixedTime;

            // if inhibited timer has gone below zero start looking to see if the ship is inhibited
            // ships that cant be effected by inhibiting should not be checked.
            // this will exclude all stations, platforms, SSP's, and engine damaged ships.
            if (InhibitedTimer <= 0 && MaxFTLSpeed >= LightSpeedConstant)
            {
                // Always check if already inhibited and inhibition timer has run out to ensure that the timer is accurate.
                // Else check that timer is below engine state check frequency.
                // We are using the negative side of the inhibited Timer here which saves
                // some memory by not using another float for all ships.
                if (Inhibited || engineState == MoveState.Warp && InhibitedTimer <= -InhibitedAtWarpCheckFrequency ||
                                 engineState == MoveState.Sublight && InhibitedTimer <= -InhibitedAtSubLightCheckFrequency)
                {
                    // All general inhibition sources should be below.
                    // NOTE: if a source cant be in this list reasonably we will need a force inhibition method.
                    // this method would simply set the states as below and it will work.
                    // similar to what is in the TestShip Class. Such as a directed weapon that inhibits a specific ship.

                    // in future if we have more event inhibiting effects we should have a method that iterates them
                    if (RandomEventManager.ActiveEvent?.InhibitWarp == true)
                    {
                        InhibitedTimer   = 5f;
                        Inhibited        = true;
                        InhibitedByEnemy = false;
                    }
                    else if (System != null && IsInhibitedByUnfriendlyGravityWell)
                    {
                        InhibitedTimer   = 0.5f;
                        Inhibited        = true;
                        InhibitedByEnemy = false;
                    }
                    else if (IsInhibitedFromEnemyShips())
                    {
                        InhibitedByEnemy = true;
                        InhibitedTimer   = 5f;
                        Inhibited        = true;
                    }
                    // nothing is inhibiting so reset timer and states
                    else
                    {
                        InhibitedTimer = 0;
                        // to avoid constantly setting the inhibited state check that it needs to be set.
                        if (Inhibited)
                        {
                            InhibitedByEnemy = false;
                            Inhibited        = false;
                        }
                    }
                }
            }

            // TODO: this lightspeed constant check isnt in the right place. Its buried here. 
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
