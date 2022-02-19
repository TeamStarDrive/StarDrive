using Microsoft.Xna.Framework;
using Ship_Game.AI;
using SynapseGaming.LightingSystem.Core;
using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.AI.Tasks;
using Ship_Game.Audio;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        // > 0 if ship is outside frustum
        float NotVisibleToPlayerTimer;

        // after X seconds of ships being invisible, we remove their scene objects
        const float RemoveInvisibleSceneObjectsAfterTime = 15f;

        public void ShowSceneObjectAt(Vector2 pos, float z)
        {
            if (ShipSO == null)
            {
                Log.Info("Showing SceneObject");
                CreateSceneObject();
            }

            ShipSO.World = Matrix.CreateTranslation(new Vector3(pos + ShipData.BaseHull.MeshOffset, z));
            ShipSO.Visibility = GlobalStats.ShipVisibility;
        }

        public bool IsVisibleToPlayer => InFrustum && InSensorRange
                                      && (Universe.Screen?.IsSystemViewOrCloser == true);

        // NOTE: This is called on the main UI Thread by UniverseScreen
        // check UniverseScreen.QueueShipSceneObject()
        public void CreateSceneObject()
        {
            if (StarDriveGame.Instance == null || ShipSO != null)
                return; // allow creating invisible ships in Unit Tests

            //Log.Info($"CreateSO {Id} {Name}");
            ShipData.LoadModel(out ShipSO, Universe.Screen.ContentManager);
            ShipSO.World = Matrix.CreateTranslation(new Vector3(Position + ShipData.BaseHull.MeshOffset, 0f));

            NotVisibleToPlayerTimer = 0;
            UpdateVisibilityToPlayer(FixedSimTime.Zero, forceVisible: true);
            ScreenManager.Instance.AddObject(ShipSO);
        }

        public void RemoveSceneObject()
        {
            SceneObject so = ShipSO;
            ShipSO = null;
            if (so != null)
            {
                ScreenManager.Instance.RemoveObject(so);
            }
        }

        void UpdateVisibilityToPlayer(FixedSimTime timeStep, bool forceVisible)
        {
            bool visibleToPlayer = forceVisible || IsVisibleToPlayer;
            if (visibleToPlayer) NotVisibleToPlayerTimer = 0f;
            else                 NotVisibleToPlayerTimer += timeStep.FixedTime;

            if (ShipSO != null) // allow null SceneObject to support ship.Update in UnitTests
            {
                if (!visibleToPlayer && NotVisibleToPlayerTimer > RemoveInvisibleSceneObjectsAfterTime)
                {
                    RemoveSceneObject();
                }
                else
                {
                    ShipSO.Visibility = visibleToPlayer ? GlobalStats.ShipVisibility : ObjectVisibility.None;
                }
            }
        }

        public override void Update(FixedSimTime timeStep)
        {
            if (Active && Health <= 0)
            {
                Die(null, cleanupOnly:true);
            }
            
            UpdateVisibilityToPlayer(timeStep, forceVisible: false);

            if (!Active)
                return;

            if (ScuttleTimer > -1f || ScuttleTimer < -1f)
            {
                ScuttleTimer -= timeStep.FixedTime;
                if (ScuttleTimer <= 0f) 
                    Die(null, cleanupOnly:true);
            }

            ShieldRechargeTimer += timeStep.FixedTime;

            if (TetheredTo != null)
            {
                Position = TetheredTo.Center + TetherOffset;
                VelocityMax = 0;
            }

            if (IsHangarShip && !Mothership.Active) //Problematic for drones...
                Mothership = null;

            if (!Dying) UpdateAlive(timeStep);
            else        UpdateDying(timeStep);
        }

        void UpdateAlive(FixedSimTime timeStep)
        {
            ExploreCurrentSystem(timeStep);

            if (EMPDisabled)
            {
                float third = Radius / 3f;
                for (int i = 0; i < 4; ++i)
                {
                    Vector3 randPos = UniverseRandom.Vector32D(third);
                    Universe.Screen.Particles.Lightning.AddParticle(Position.ToVec3() + randPos);
                }
            }

            if (!Active)
                return;

            if (timeStep.FixedTime > 0f)
            {
                UpdateShipStatus(timeStep);
                UpdateEnginesAndVelocity(timeStep);
            }

            if (IsVisibleToPlayer)
            {
                if (ShipSO != null)
                {
                    ShipSO.World = Matrix.CreateTranslation(new Vector3(ShipData.BaseHull.MeshOffset, 0f))
                                 * Matrix.CreateRotationY(YRotation)
                                 * Matrix.CreateRotationZ(Rotation)
                                 * Matrix.CreateTranslation(new Vector3(Position, 0f));
                    ShipSO.UpdateAnimation(timeStep.FixedTime);

                    UpdateThrusters(timeStep);
                }
                else // auto-create scene objects if possible
                {
                    Universe.Screen?.QueueSceneObjectCreation(this);
                }
            }

            SoundEmitter.Position = new Vector3(Position, 0);

            ResetFrameThrustState();
        }

        void ExploreCurrentSystem(FixedSimTime timeStep)
        {
            if (System != null && timeStep.FixedTime > 0f && Loyalty?.isFaction == false
                && !System.IsFullyExploredBy(Loyalty)
                && System.PlanetList != null) // Added easy out for fully explored systems
            {
                foreach (Planet p in System.PlanetList)
                {
                    if (p.IsExploredBy(Loyalty)) // already explored
                        continue;
                    if (p.Center.OutsideRadius(Position, 3000f))
                        continue;

                    if (p.EventsOnTiles())
                    {
                        if (Loyalty == EmpireManager.Player)
                        {
                            Universe.Screen.NotificationManager.AddFoundSomethingInteresting(p);
                        }
                        else if (p.Owner == null)
                        {
                            Loyalty.GetEmpireAI().SendExplorationFleet(p);
                            if (CurrentGame.Difficulty > GameDifficulty.Hard 
                                && PlanetRanker.IsGoodValueForUs(p, Loyalty)
                                && p.ParentSystem.GetKnownStrengthHostileTo(Loyalty).AlmostZero())
                            {
                                var task = MilitaryTask.CreateGuardTask(Loyalty, p);
                                Loyalty.GetEmpireAI().AddPendingTask(task);
                            }
                        }
                    }

                    p.SetExploredBy(Loyalty);
                    System.UpdateFullyExploredBy(Loyalty);
                }
            }
        }

        public void UpdateThrusters(FixedSimTime timeStep)
        {
            Color thrust0 = Loyalty.ThrustColor0;
            Color thrust1 = Loyalty.ThrustColor1;
            Color thrust2 = Loyalty.EmpireColor;
            float velocity = Velocity.Length();
            float velocityPercent = velocity / VelocityMax;
            bool notPaused = timeStep.FixedTime > 0f;

            Vector3 direction3d = Direction3D;

            for (int i = 0; i < ThrusterList.Length; ++i)
            {
                Thruster thruster = ThrusterList[i];
                thruster.UpdatePosition(Position, YRotation, direction3d);

                bool enginesOn = ThrustThisFrame == Ships.Thrust.Forward || ThrustThisFrame == Ships.Thrust.Reverse;
                if (enginesOn)
                {
                    if (notPaused && thruster.heat < velocityPercent)
                        thruster.heat += 0.06f;

                    if (engineState == MoveState.Warp)
                    {
                        thruster.Update(direction3d, thruster.heat, 0.004f, thrust0, thrust1);
                    }
                    else
                    {
                        if (thruster.heat > 0.6f)
                            thruster.heat = 0.6f;
                        thruster.Update(direction3d, thruster.heat, 0.002f, thrust0, thrust1);
                    }
                }
                else
                {
                    if (notPaused)
                        thruster.heat = 0.01f;
                    thruster.Update(direction3d, 0.1f, 1.0f / 500.0f, thrust0, thrust1);
                }

                if (GlobalStats.EnableEngineTrails && velocityPercent > 0.1f && notPaused)
                {
                    // tscale is in world units, engine-trail effect width at scale=1 is 32 units
                    float thrustScale = thruster.Scale / 32f;
                    float thrustPower = (thruster.heat * (Stats.Thrust / 32f)).Clamped(64, 320) * thrustScale;
                    EngineTrail.Update(Universe.Screen.Particles, thruster.WorldPos, direction3d, 
                                       thrustScale, thrustPower, thrust1, thrust2);
                }
            }
        }

        AudioHandle DeathSfx;

        void UpdateDying(FixedSimTime timeStep)
        {
            DestroyThrusters();

            DieTimer -= timeStep.FixedTime;
            if (DieTimer <= 1.9f && IsVisibleToPlayer && (DeathSfx == null || DeathSfx.IsStopped))
            {
                string cueName;
                if (SurfaceArea < 80) cueName = "sd_explosion_ship_warpdet_small";
                else if (SurfaceArea < 250) cueName = "sd_explosion_ship_warpdet_medium";
                else cueName = "sd_explosion_ship_warpdet_large";

                if (DeathSfx == null)
                    DeathSfx = new AudioHandle();
                DeathSfx.PlaySfxAsync(cueName, SoundEmitter);
            }
            if (DieTimer <= 0.0f)
            {
                ReallyDie = true;
                Die(LastDamagedBy, true);
                return;
            }

            if (ShipSO == null)
                return;

            // for a cool death effect, make the ship accelerate out of control:
            SubLightAccelerate(100f);
            UpdateVelocityAndPosition(timeStep);
            PlanetCrash?.Update(Universe.Screen.Particles, timeStep);

            bool visible = IsVisibleToPlayer;
            bool visibleAndNotPaused = visible && timeStep.FixedTime > 0f;
            float scale = PlanetCrash?.Scale ?? 1;
            float scaledRadius = Radius * scale;

            if (visibleAndNotPaused)
            {
                ShipSO.World = Matrix.CreateTranslation(new Vector3(ShipData.BaseHull.MeshOffset, 0f))
                             * Matrix.CreateScale(scale)
                             * Matrix.CreateRotationY(YRotation)
                             * Matrix.CreateRotationX(XRotation)
                             * Matrix.CreateRotationZ(Rotation)
                             * Matrix.CreateTranslation(new Vector3(Position, 0f));

                ShipSO.UpdateAnimation(timeStep.FixedTime);
            }

            if (visibleAndNotPaused && !IsMeteor)
            {
                int num1 = UniverseRandom.IntBetween(0, 100);
                if (num1 >= 99) // 1% chance
                {
                    Vector3 pos = (Position + RandomMath.Vector2D(scaledRadius*0.5f)).ToVec3();
                    ExplosionManager.AddExplosion(Universe.Screen, pos, Velocity, scaledRadius*0.5f, 2.5f, ExplosionType.Projectile);
                    Universe.Screen.Particles.Flash.AddParticle(pos);
                }
                if (num1 >= 50) // 50% chance
                {
                    Vector3 pos = (Position + RandomMath.Vector2D(scaledRadius*0.5f)).ToVec3();
                    Universe.Screen.Particles.Lightning.AddParticle(pos, Velocity.ToVec3(), 0.5f*scale, Color.White);
                }
            }

            YRotation += DieRotation.X * timeStep.FixedTime;
            XRotation += DieRotation.Y * timeStep.FixedTime;
            Rotation  += DieRotation.Z * timeStep.FixedTime;
            Rotation = Rotation.AsNormalizedRadians(); // [0; +2PI]

            // Spawn some junk when tumbling and the game is not paused
            if (visibleAndNotPaused && !IsMeteor && RandomMath.RollDice(10)) // X % chance
            {
                Vector2 pos = Position.GenerateRandomPointOnCircle(scaledRadius / 20);
                SpaceJunk.SpawnJunk(Universe, 1, pos, Velocity * scale, this,
                                    maxSize: scaledRadius * 0.1f, ignite: true);
            }

            SoundEmitter.Position = new Vector3(Position, 0);

            for (int i = 0; i < ModuleSlotList.Length; i++)
            {
                ModuleSlotList[i].UpdateWhileDying(timeStep, scale, visible);
            }
        }
    }
}