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
            
            DroneTarget = target;
        }

        // the drone will orbit around the ship it's healing
        void OrbitShip(Ship ship, float elapsedTime)
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
                Drone.GuidedMoveTowards(elapsedTime, DroneTarget?.Center ?? orbitalPos, 0f);
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
                    var droneBeam = Beams[i];
                    droneBeam.UpdateDroneBeam(Drone.Center, DroneTarget.Center, DroneWeapon.BeamThickness, elapsedTime);
                    if (!droneBeam.Active)
                        Beams.RemoveAtSwapLast(i);
                    
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