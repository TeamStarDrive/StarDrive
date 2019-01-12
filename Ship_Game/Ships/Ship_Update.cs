using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using System;

namespace Ship_Game.Ships
{
    public sealed partial class Ship
    {

        public bool UpdateVisibility()
        {
            bool inFrustrum = (System == null || System.isVisible)
                && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView
                && (Empire.Universe.Frustum.Contains(Position, 2000f) ||AI.Target != null && Empire.Universe.Frustum.Contains(AI.Target.Position, maxWeaponsRange)) ;

            InFrustum = inFrustrum;
            ShipSO.Visibility = inFrustrum ? ObjectVisibility.Rendered : ObjectVisibility.None;
            return inFrustrum;
        }

        public void UpdateWorldTransform()
        {
            ShipSO.World = Matrix.CreateRotationY(yRotation)
                           * Matrix.CreateRotationZ(Rotation)
                           * Matrix.CreateTranslation(new Vector3(Center, 0.0f));
        }

        public override void Update(float elapsedTime)
        {
            if (!Active)
                return;

            if (ScuttleTimer > -1f || ScuttleTimer < -1f)
            {
                ScuttleTimer -= elapsedTime;
                if (ScuttleTimer <= 0f) Die(null, true);
            }

            UpdateVisibility();

            ShieldRechargeTimer += elapsedTime;
            InhibitedTimer      -= elapsedTime;
            Inhibited = InhibitedTimer > 0f;
            if ((Inhibited || maxFTLSpeed < 2500f) && engineState == MoveState.Warp)
                HyperspaceReturn();

            if (TetheredTo != null)
            {
                Position = TetheredTo.Center + TetherOffset;
                Center   = TetheredTo.Center + TetherOffset;
                velocityMaximum = 0;
            }
            if (Mothership != null && !Mothership.Active) //Problematic for drones...
                Mothership = null;

            if (dying) UpdateDying(elapsedTime);
            else       UpdateAlive(elapsedTime);
        }

        private void UpdateAlive(float elapsedTime)
        {
            if (System != null && elapsedTime > 0f && loyalty?.isFaction == false && !System.IsFullyExploredBy(loyalty)
                && System.PlanetList != null)  //Added easy out for fully explorered systems
            {
                foreach (Planet p in System.PlanetList)
                {
                    if (p.IsExploredBy(loyalty)) // already explored
                        continue;
                    if (p.Center.OutsideRadius(Center, 3000f))
                        continue;

                    if (loyalty == EmpireManager.Player)
                    {
                        for (int index = 0; index < p.BuildingList.Count; index++)
                        {
                            Building building = p.BuildingList[index];
                            if (!string.IsNullOrEmpty(building.EventTriggerUID))
                                Empire.Universe.NotificationManager.AddFoundSomethingInteresting(p);
                        }
                    }
                    p.SetExploredBy(loyalty);
                    System.UpdateFullyExploredBy(loyalty);
                    for (int i = 0; i < p.BuildingList.Count; i++)
                    {
                        Building building = p.BuildingList[i];
                        if (string.IsNullOrEmpty(building.EventTriggerUID) ||
                            loyalty == EmpireManager.Player || p.Owner != null) continue;

                        var militaryTask = new MilitaryTask
                        {
                            AO = p.Center,
                            AORadius = 50000f,
                            type = MilitaryTask.TaskType.Exploration
                        };
                        militaryTask.SetTargetPlanet(p);
                        militaryTask.SetEmpire(loyalty);
                        loyalty.GetEmpireAI().TaskList.Add(militaryTask);
                    }
                }
                if (AI.BadGuysNear && InCombat && System != null && LastDamagedBy != null)
                {
                    System.CombatInSystem = true;
                    System.combatTimer = 5f;
                }
            }

            if (EMPdisabled)
            {
                float third = Radius / 3f;
                for (int i = 5 - 1; i >= 0; --i)
                {
                    Vector3 randPos = UniverseRandom.Vector32D(third);
                    Empire.Universe.lightning.AddParticleThreadA(Center.ToVec3() + randPos, Vector3.Zero);
                }
            }

            Rotation += RotationalVelocity * elapsedTime;
            if (RotationalVelocity > 0 || RotationalVelocity < 0)
                isTurning = true;

            if (!isSpooling && Afterburner.IsPlaying)
                Afterburner.Stop();

            //ClickTimer -= elapsedTime;    //This is the only place ClickTimer is ever used, and thus is a waste of memory and CPU -Gretman
            //if (ClickTimer < 0f) ClickTimer = 10f;

            if (elapsedTime > 0f)
            {
                UpdateProjectiles(elapsedTime);
                UpdateBeams(elapsedTime);
                if (!EMPdisabled && Active) AI.Update(elapsedTime);
            }

            if (!Active) return;

            InCombatTimer -= elapsedTime;
            if (InCombatTimer > 0.0f)
            {

                InCombat = true;
            }
            else
            {
                if (InCombat)
                    InCombat = false;
                if (AI.State == AIState.Combat && loyalty != EmpireManager.Player)
                {
                    AI.State = AIState.AwaitingOrders;
                    AI.OrderQueue.Clear();
                }
            }

            Position += Velocity * elapsedTime;
            Center   += Velocity * elapsedTime;
            UpdateShipStatus(elapsedTime);

            if (InFrustum)
            {
                if (ShipSO == null)
                    return;
                UpdateWorldTransform();

                if (shipData.Animated && ShipMeshAnim != null)
                {
                    ShipSO.SkinBones = ShipMeshAnim.SkinnedBoneTransforms;
                    ShipMeshAnim.Update(Game1.Instance.TargetElapsedTime, Matrix.Identity);
                }
                UpdateThrusters();
            }

            if (isSpooling && !Inhibited && GetmaxFTLSpeed > 2500)
            {
                JumpTimer -= elapsedTime;
                //task gremlin move fighter recall here.

                if (JumpTimer <= 4.0) // let's see if we can sync audio to behaviour with new timers
                {
                    if (Empire.Universe.CamHeight < 250000 && Empire.Universe.CamPos.InRadius(Center, 100000f)
                                                           && JumpSfx.IsStopped)
                    {
                        JumpSfx.PlaySfxAsync(GetStartWarpCue(), SoundEmitter);
                    }
                }
                if (JumpTimer <= 0.1)
                {
                    if (engineState == MoveState.Sublight)
                    {
                        FTLManager.AddFTL(Center);
                        engineState = MoveState.Warp;
                    }
                    else engineState = MoveState.Sublight;
                    isSpooling = false;
                    ResetJumpTimer();
                }
            }
            if (PlayerShip)
            {
                if ((!isSpooling || !Active) && Afterburner.IsPlaying)
                {
                    Afterburner.Stop();
                }
                if (isThrusting && AI.State == AIState.ManualControl && DroneSfx.IsStopped)
                {
                    DroneSfx.PlaySfxAsync("starcruiser_drone01", SoundEmitter);
                }
                else if ((!isThrusting || !Active) && DroneSfx.IsPlaying)
                {
                    DroneSfx.Stop();
                }
            }
            SoundEmitter.Position = new Vector3(Center, 0);
        }

        private void UpdateThrusters()
        {
            foreach (Thruster thruster in ThrusterList)
                UpdateThruster(thruster);
        }

        private void UpdateThruster(Thruster thruster)
        {
            thruster.SetPosition();
            float velocityPercent = Velocity.Length() / velocityMaximum;
            if (isThrusting)
            {
                if (engineState == MoveState.Warp)
                {
                    if (thruster.heat < velocityPercent)
                        thruster.heat += 0.06f;
                    thruster.Update(Direction3D, thruster.heat, 0.004f, Empire.Universe.CamPos);
                }
                else
                {
                    if (thruster.heat < velocityPercent)
                        thruster.heat += 0.06f;
                    if (thruster.heat > 0.600000023841858)
                        thruster.heat = 0.6f;
                    thruster.Update(Direction3D, thruster.heat, 1.0f / 500.0f, Empire.Universe.CamPos);
                }
            }
            else
            {
                thruster.heat = 0.01f;
                thruster.Update(Direction3D, 0.1f, 1.0f / 500.0f, Empire.Universe.CamPos);
            }
        }

        private void UpdateDying(float elapsedTime)
        {
            ThrusterList.Clear();
            dietimer -= elapsedTime;
            if (dietimer <= 1.9f && InFrustum && DeathSfx.IsStopped)
            {
                string cueName;
                if (SurfaceArea < 80) cueName = "sd_explosion_ship_warpdet_small";
                else if (SurfaceArea < 250) cueName = "sd_explosion_ship_warpdet_medium";
                else cueName = "sd_explosion_ship_warpdet_large";
                DeathSfx.PlaySfxAsync(cueName, SoundEmitter);
            }
            if (dietimer <= 0.0f)
            {
                reallyDie = true;
                Die(LastDamagedBy, true);
                return;
            }

            if (Velocity.LengthSquared() > velocityMaximum*velocityMaximum) // RedFox: use SqLen instead of Len
                Velocity = Velocity.Normalized() * velocityMaximum;

            Vector2 deltaMove = Velocity * elapsedTime;
            Position += deltaMove;
            Center   += deltaMove;

            int num1 = UniverseRandom.IntBetween(0, 60);
            if (num1 >= 57 && InFrustum)
            {
                Vector3 position = UniverseRandom.Vector3D(0f, Radius);
                ExplosionManager.AddExplosion(position, ShipSO.WorldBoundingSphere.Radius, 2.5f);
                Empire.Universe.flash.AddParticleThreadA(position, Vector3.Zero);
            }
            if (num1 >= 40)
            {
                Vector3 position = UniverseRandom.Vector3D(0f, Radius);
                Empire.Universe.sparks.AddParticleThreadA(position, Vector3.Zero);
            }
            yRotation += xdie * elapsedTime;
            xRotation += ydie * elapsedTime;

            //Ship ship3 = this;
            //double num2 = (double)this.Rotation + (double)this.zdie * (double)elapsedTime;
            Rotation += zdie * elapsedTime;
            if (ShipSO == null)
                return;
            if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.ShipView && inSensorRange)
            {
                ShipSO.World = Matrix.CreateRotationY(yRotation)
                               * Matrix.CreateRotationX(xRotation)
                               * Matrix.CreateRotationZ(Rotation)
                               * Matrix.CreateTranslation(new Vector3(Center, 0.0f));
                if (shipData.Animated)
                {
                    ShipSO.SkinBones = ShipMeshAnim.SkinnedBoneTransforms;
                    ShipMeshAnim.Update(Game1.Instance.TargetElapsedTime, Matrix.Identity);
                }
            }
            for (int i = 0; i < projectiles.Count; ++i)
            {
                Projectile projectile = projectiles[i];
                if (projectile == null)
                    continue;
                if (projectile.Active)
                    projectile.Update(elapsedTime);
                else
                    projectiles.RemoveRef(projectile);
            }
            SoundEmitter.Position = new Vector3(Center, 0);
            for (int i = 0; i < ModuleSlotList.Length; i++)
            {
                ModuleSlotList[i].UpdateWhileDying(elapsedTime);
            }
        }



        private void UpdateProjectiles(float elapsedTime)
        {
            for (int i = projectiles.Count - 1; i >= 0; --i)
            {
                if (projectiles[i]?.Active == true)
                    projectiles[i].Update(elapsedTime);
                else
                    projectiles.RemoveAtSwapLast(i);
            }
        }

        private void UpdateBeams(float elapsedTime)
        {
            for (int i = 0; i < beams.Count; i++)
            {
                Beam beam = beams[i];
                if (beam.Module != null)
                {
                    ShipModule shipModule = beam.Module;

                    Vector2 slotForward  = (beam.Owner.Rotation + shipModule.Rotation.ToRadians()).RadiansToDirection();
                    Vector2 muzzleOrigin = shipModule.Center + slotForward * (shipModule.YSIZE * 8f);
                    int thickness = (int)UniverseRandom.RandomBetween(beam.Thickness*0.75f, beam.Thickness*1.1f);

                    beam.Update(muzzleOrigin, thickness, elapsedTime);

                    if (beam.Duration < 0f && !beam.Infinite)
                    {
                        beam.Die(null, false);
                        beams.RemoveRef(beam);
                    }
                }
                else
                {
                    beam.Die(null, false);
                }
            }
        }



        private void CheckAndPowerConduit(ShipModule module)
        {
            if (!module.Active)
                return;
            module.Powered = true;
            module.CheckedConduits = true;
            Vector2 center = module.LocalCenter;
            for (int x = 0; x < ModuleSlotList.Length; x++)
            {
                ShipModule slot = ModuleSlotList[x];
                if (slot == module || slot.ModuleType != ShipModuleType.PowerConduit || slot.CheckedConduits)
                    continue;
                var distanceX = (int) Math.Abs(center.X - slot.LocalCenter.X) ;
                var distanceY = (int) Math.Abs(center.Y - slot.LocalCenter.Y) ;
                if (distanceX + distanceY > 16)
                { if (distanceX + distanceY > 33)
                        continue;
                  if (distanceX + distanceY < 33)
                        continue;

                }

                CheckAndPowerConduit(slot);
            }
        }

        public void RecalculatePower()
        {
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule slot      = ModuleSlotList[i];
                slot.Powered         = false;
                slot.CheckedConduits = false;
            }

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                //better fix for modules that dont use power.
                if (module.PowerRadius < 1 && (module.PowerDraw <= 0 || module.AlwaysPowered))
                {
                    module.Powered = true;
                    continue;
                }
                //Filter by powerplants.
                if (!module.Is(ShipModuleType.PowerPlant) || !module.Active) continue;
                //This is a change. powerplants are now marked powered
                module.Powered = true;
                Vector2 moduleCenter = module.LocalCenter;
                //conduit check.
                foreach (ShipModule slot2 in ModuleSlotList)
                {
                    if (slot2.ModuleType != ShipModuleType.PowerConduit || slot2.Powered)
                        continue;

                    if (!IsAnyPartOfModuleInRadius(module, slot2.LocalCenter, 16)) continue;
                    CheckAndPowerConduit(slot2);
                }
            }
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule module = ModuleSlotList[i];
                if (!module.Active || module.PowerRadius < 1 || !module.Powered )
                    continue;

                float cx = module.LocalCenter.X;
                float cy = module.LocalCenter.Y;
                int powerRadius = module.PowerRadius * 16 + (int)module.Radius;

                foreach (ShipModule slot2 in ModuleSlotList)
                {
                    if (!slot2.Active || slot2.Powered  || slot2 == module || slot2.ModuleType == ShipModuleType.PowerConduit)
                        continue;

                    int distanceFromPowerX = (int)Math.Abs(cx - (slot2.Position.X + 8)) ;
                    int distanceFromPowerY = (int)Math.Abs(cy - (slot2.Position.Y + 8));
                    if (distanceFromPowerX + distanceFromPowerY <= powerRadius)
                    {
                        slot2.Powered = true;
                        continue;
                    }
                    //if its really far away dont bother.
                    if (distanceFromPowerX + distanceFromPowerY > slot2.Radius * 2 + powerRadius)
                        continue;
                    slot2.Powered = IsAnyPartOfModuleInRadius(slot2, new Vector2(cx, cy), powerRadius);
                }
            }
        }
        //not sure where to put this. I guess shipModule but its huge. Maybe an extension?
        private static bool IsAnyPartOfModuleInRadius(ShipModule moduleAreaToCheck, Vector2 pos, int radius)
        {
            float cx = pos.X;
            float cy = pos.Y;

            for (int y = 0; y < moduleAreaToCheck.YSIZE; ++y)
            {
                float sy = moduleAreaToCheck.Position.Y + (y * 16) +8;
                for (int x = 0; x < moduleAreaToCheck.XSIZE; ++x)
                {
                    if (y == moduleAreaToCheck.YSIZE * 16 && x == moduleAreaToCheck.XSIZE *16) continue;
                        float sx = moduleAreaToCheck.Position.X + (x * 16) +8;
                    if ((int) Math.Abs(cx - sx) + (int) Math.Abs(cy - sy) <= radius + 8)
                        return true;
                }
            }
            return false;
        }
    }
}