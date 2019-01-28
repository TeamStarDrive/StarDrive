using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public sealed class DroneAI: IDisposable
    {
        public readonly Projectile Drone;
        private Ship DroneTarget;
        private float ThinkTimer;
        public Weapon DroneWeapon;
        private float OrbitalAngle;
        public BatchRemovalCollection<Beam> Beams = new BatchRemovalCollection<Beam>();

        public DroneAI(Projectile drone)
        {
            Drone = drone;
            DroneWeapon = ResourceManager.CreateWeapon("RepairBeam");
        }

        public void ChooseTarget()
        {
            var target = Drone.Owner?.AI.FriendliesNearby
                .FindMinFiltered(ship => ship.Active && ship.Mothership == null 
                                                     && ship.Health < ship.HealthMax
                                                     && ship.Center.InRadius(Drone.Owner.Center, 10000)
                , ship =>
                {                                        
                    var ownerCenter      = Drone.Owner.Center;
                    var distance         = ship.Center.Distance(ownerCenter);
                    var distanceWieght   = 1 + (int)(10 * distance / DroneWeapon.Range);                    
                    var repairWeight     = (int)ship.HealthStatus;                    
                    float weight         = distanceWieght * repairWeight ;
                    return weight;
                });
            
            DroneTarget                  = target;
        }

        private void MoveTowardsPosition(float elapsedTime, Vector2 targetPos)
        {
            Vector2 droneForward  = Drone.Rotation.RadiansToDirection();
            Vector2 wantedForward = Drone.Center.DirectionToTarget(targetPos);

            float angleDiff = (float)Math.Acos(wantedForward.Dot(droneForward));
            if (angleDiff > 0.2f)
            {
                Vector2 droneRight = droneForward.RightVector();
                float rotationDir = wantedForward.Dot(droneRight) > 0f ? 1f : -1f;
                Drone.Rotation += Math.Min(angleDiff, rotationDir * elapsedTime * Drone.Speed / 350f);
            }

            Drone.Velocity = Drone.Rotation.RadiansToDirection() * (elapsedTime * Drone.Speed);
            if (Drone.Velocity.Length() > Drone.VelocityMax)
            {
                Drone.Velocity = Drone.Velocity.Normalized() * Drone.VelocityMax;
            }
        }

        // the drone will orbit around the ship it's healing
        private void OrbitShip(Ship ship, float elapsedTime)
        {
            Vector2 orbitalPos = ship.Center.PointOnCircle(OrbitalAngle, 1500f);
            if (orbitalPos.InRadius(Drone.Center, 1500f))
            {
                OrbitalAngle += 15f;
                if (OrbitalAngle >= 360f)
                    OrbitalAngle = OrbitalAngle - 360f;
                orbitalPos = ship.Center.PointOnCircle(OrbitalAngle, 2500f);
            }
            if (elapsedTime > 0f)
                Drone.GuidedMoveTowards(elapsedTime, DroneTarget?.Center ?? orbitalPos);
        }

        public void Think(float elapsedTime)
        {
            DroneWeapon.CooldownTimer -= elapsedTime;

            Beams.ApplyPendingRemovals();
            ThinkTimer -= elapsedTime;
            if (ThinkTimer <= 0f && (DroneTarget == null || !DroneTarget.Active || DroneTarget.HealthStatus < ShipStatus.NotApplicable))
            {
                ChooseTarget();
                ThinkTimer = 2.5f;
            }

            if (DroneTarget == null)
            {
                for (int i = 0; i < Beams.Count; ++i)
                    Beams[i].Die(null, true);
                if (Drone.Owner != null)
                    OrbitShip(Drone.Owner, elapsedTime);
            }
            else
            {
                if (Beams.Count ==0 && DroneTarget.Health < DroneTarget.HealthMax
                    && DroneWeapon.CooldownTimer <= 0f
                    && DroneTarget != null && Drone.Center.Distance(DroneTarget.Center) <= DroneWeapon.Range)
                {
                    DroneWeapon.FireDroneBeam(DroneTarget, this);                    
                }
                for (int i = 0; i < Beams.Count; ++i)
                {           
                    Beams[i].UpdateDroneBeam(Drone.Center, DroneTarget.Center, DroneWeapon.BeamThickness, elapsedTime);
                    
                }
                OrbitShip(DroneTarget, elapsedTime);
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~DroneAI() { Destroy(); }

        private void Destroy()
        {
            Beams?.Dispose(ref Beams);
        }
    }
}