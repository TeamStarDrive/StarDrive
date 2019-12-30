using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Audio;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        // This is the light speed threshold
        // Warp ships cannot go slower than this
        public const float LightSpeedConstant = 3000f;

        // This is the maximum STL speed that ships can achieve
        // This is both for balancing and for realism, since sub-light
        // ships should not get even close to light speed
        public const float MaxSubLightSpeed = 1800f;

        bool IsSpooling;
        public float InhibitedTimer;
        public bool Inhibited { get; private set; }
        public MoveState engineState;
        public float WarpThrust;
        public bool BaseCanWarp;
        public float NormalWarpThrust;

        public bool IsSpoolingOrInWarp => IsSpooling || engineState == MoveState.Warp;
        public bool IsInWarp => engineState == MoveState.Warp;
        // safe Warp out distance so the ship still has time to slow down
        public float WarpOutDistance => 3200f + MaxSTLSpeed * 3f;
        public string WarpState => engineState == MoveState.Warp ? "FTL" : "Sublight";

        public void ResetJumpTimer()
        {
            JumpTimer = FTLSpoolTime * loyalty.data.SpoolTimeModifier;
        }

        string GetStartWarpCue()
        {
            if (loyalty.data.WarpStart != null)
                return loyalty.data.WarpStart;
            if (SurfaceArea < 60)
                return "sd_warp_start_small";
            return SurfaceArea > 350 ? "sd_warp_start_large" : "sd_warp_start_02";
        }

        string GetEndWarpCue()
        {
            if (loyalty.data.WarpStart != null)
                return loyalty.data.WarpEnd;
            if (SurfaceArea < 60)
                return "sd_warp_stop_small";
            return SurfaceArea > 350 ? "sd_warp_stop_large" : "sd_warp_stop";
        }

        public void EngageStarDrive() // added by gremlin: Fighter recall and stuff
        {
            if (IsSpoolingOrInWarp)
                return;

            if (Carrier.RecallingFighters())
                return;

            if (EnginesKnockedOut || Inhibited)
            {
                HyperspaceReturn();
                return;
            }

            if (!IsSpoolingOrInWarp && (PowerCurrent / (PowerStoreMax + 0.01f)) > 0.10f)
            {
                IsSpooling = true;
                ResetJumpTimer();
            }
        }

        public void HyperspaceReturn()
        {
            if (!IsSpoolingOrInWarp)
                return;

            // stop the SFX and always reset the replay timeout
            JumpSfx.Stop();

            if (engineState == MoveState.Warp && InFrustum &&
                Empire.Universe != null &&
                Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                GameAudio.PlaySfxAsync(GetEndWarpCue(), SoundEmitter);
                FTLManager.ExitFTL(GetWarpEffectPosition, Direction3D, Radius);
            }

            engineState = MoveState.Sublight;
            IsSpooling = false;
            VelocityMaximum = MaxSTLSpeed;
            // feature: exit from hyperspace at ridiculous speeds
            Velocity = Velocity.Normalized() * Math.Min(MaxSTLSpeed, MaxSubLightSpeed);
            SpeedLimit = VelocityMaximum;
        }

        Vector3 GetWarpEffectPosition() => Center.ToVec3();

        
        void UpdateHyperspaceInhibited(float elapsedTime)
        {
            InhibitedTimer -= elapsedTime;
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

        void UpdateWarpSpooling(float elapsedTime)
        {
            JumpTimer -= elapsedTime;

            if (JumpTimer <= 4.0f)
            {
                if (InFrustum
                    && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView
                    && !Empire.Universe.Paused && JumpSfx.IsStopped && JumpSfx.IsReadyToReplay)
                {
                    JumpSfx.PlaySfxAsync(GetStartWarpCue(), SoundEmitter, replayTimeout:4.0f);
                }
            }
            if (JumpTimer <= 0.1f)
            {
                if (engineState == MoveState.Sublight)
                {
                    FTLManager.EnterFTL(Center.ToVec3(), Direction3D, Radius);
                    engineState = MoveState.Warp;
                }
                IsSpooling = false;
                ResetJumpTimer();
            }
        }

        void UpdateInhibitedFromEnemyShips()
        {
            foreach (Empire e in EmpireManager.Empires)
            {
                if (e != loyalty && !loyalty.GetRelations(e).Treaty_OpenBorders)
                {
                    for (int i = 0; i < e.Inhibitors.Count; ++i)
                    {
                        Ship ship = e.Inhibitors[i];
                        if (ship != null && Center.InRadius(ship.Position, ship.InhibitionRadius))
                        {
                            Inhibited = true;
                            InhibitedTimer = 5f;
                            return;
                        }
                    }
                }
            }
        }
        
        public ShipStatus WarpDuration(float neededRange = 300000)
        {
            float powerDuration = NetPower.PowerDuration(this, MoveState.Warp);
            if (powerDuration.AlmostEqual(float.MaxValue))
                return ShipStatus.Excellent;
            if (powerDuration * MaxFTLSpeed < neededRange)
                return ShipStatus.Critical;
            return ShipStatus.Good;
        }

        public ShipStatus ShipReadyForWarp()
        {
            if (MaxFTLSpeed < 1 || Inhibited || EnginesKnockedOut || !Active)
                return ShipStatus.NotApplicable;

            if (AI.HasPriorityOrder || AI.State == AIState.Resupply)
                return ShipStatus.NotApplicable;

            if (!IsSpooling && WarpDuration() < ShipStatus.Good)
                return ShipStatus.Critical;

            if (engineState == MoveState.Warp)
                return ShipStatus.Good;

            if (Carrier.HasActiveHangars)
                return ShipStatus.Poor;
            return ShipStatus.Excellent;
        }

        public ShipStatus ShipReadyForFormationWarp()
        {
            //the original logic here was confusing. If aistate was formation warp it ignored all other
            //cases and returned good. I am guessing that once the state is formation warp it is
            //expecting it has passes all other cases. But i can not verify that as the logic is spread out.
            //I believe what we need here is to centralize the engine and navigation logic.
            ShipStatus warpStatus = ShipReadyForWarp();
            if (warpStatus > ShipStatus.Poor && warpStatus != ShipStatus.NotApplicable)
                if (AI.State != AIState.FormationWarp) return ShipStatus.Good;
            return warpStatus;
        }

    }
}
