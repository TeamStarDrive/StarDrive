using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public sealed class DroneAI 
    {
        public readonly Projectile Drone;
        public Ship DroneTarget { get; private set; }
        private float ThinkTimer;
        public Weapon DroneWeapon;
        private float OrbitalAngle;
        readonly Array<DroneBeam> Beams = new Array<DroneBeam>();

        public DroneAI(Projectile drone)
        {
            Drone = drone;
            DroneWeapon = ResourceManager.CreateWeapon("RepairBeam");
            DroneWeapon.Owner = drone.Owner;
        }

        public void ChooseTarget()
        {
            var target = Drone.Owner?.AI.FriendliesNearby
                .FindMinFiltered(ship => ship.Active 
                                         && !ship.IsHangarShip
                                         && ship.HealthStatus < Status.Maximum
                                         && ship.Position.InRadius(Drone.Owner.Position, 10000)
                , ship =>
                {
                    var ownerCenter      = Drone.Owner.Position;
                    var distance         = ship.Position.Distance(ownerCenter);
                    var distanceWeight   = 1 + (int)(10 * distance / DroneWeapon.BaseRange);
                    var repairWeight     = (int)ship.HealthStatus;
                    float weight         = distanceWeight * repairWeight ;
                    return weight;
                });

            DroneTarget = target;
        }

        // the drone will orbit around the ship it's healing
        void OrbitShip(Ship ship, FixedSimTime timeStep)
        {
            Vector2 orbitalPos = ship.Position.PointFromAngle(OrbitalAngle, 1500f);
            if (orbitalPos.InRadius(Drone.Position, 1500f))
            {
                OrbitalAngle += 15f;
                if (OrbitalAngle >= 360f)
                    OrbitalAngle -= 360f;
                orbitalPos = ship.Position.PointFromAngle(OrbitalAngle, 2500f);
            }
            if (timeStep.FixedTime > 0f)
                Drone.GuidedMoveTowards(timeStep, DroneTarget?.Position ?? orbitalPos, 0f);
        }

        public void Think(FixedSimTime timeStep)
        {
            DroneWeapon.CooldownTimer -= timeStep.FixedTime;
            ThinkTimer -= timeStep.FixedTime;

            if (ThinkTimer <= 0f && (DroneTarget == null || !DroneTarget.Active 
                                                         || DroneTarget.HealthStatus >= Status.Maximum))
            {
                ChooseTarget();
                ThinkTimer = 2.5f;
            }

            if (DroneTarget == null)
            {
                if (Beams.NotEmpty)
                {
                    for (int i = 0; i < Beams.Count; ++i)
                    {
                        Beam beam = Beams[i];
                        if (beam.Active)
                            beam.Die(null, false);
                    }
                    Beams.Clear();
                }

                if (Drone.Owner != null)
                    OrbitShip(Drone.Owner, timeStep);
            }
            else
            {
                TryFireDroneBeam();
                OrbitShip(DroneTarget, timeStep);
            }
        }

        void TryFireDroneBeam()
        {
            if (Beams.Count == 0 &&
                DroneTarget.Health < DroneTarget.HealthMax &&
                DroneWeapon.CooldownTimer <= 0f &&
                DroneTarget != null &&
                Drone.Position.Distance(DroneTarget.Position) <= DroneWeapon.BaseRange)
            {
                // NOTE: Beam projectile is updated by universe
                DroneBeam droneBeam = DroneWeapon.FireDroneBeam(this);
                Beams.Add(droneBeam);
            }
        }
    }
}