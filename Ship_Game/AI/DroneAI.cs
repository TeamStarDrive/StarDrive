using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI
{
    [StarDataType]
    public sealed class DroneAI
    {
        [StarData] public readonly Projectile Drone;
        [StarData] public Ship DroneTarget { get; private set; }
        [StarData] float ThinkTimer;
        [StarData] float OrbitalAngle;
        [StarData] Beam Beam;
        public Weapon DroneWeapon;

        public DroneAI(Projectile drone)
        {
            Drone = drone;
            DroneWeapon = CreateWeapon(drone);
        }

        [StarDataConstructor] DroneAI() {}

        // depends on the parent `Drone` projectile to be initialized
        [StarDataDeserialized(typeof(Projectile))]
        public void OnDeserialized()
        {
            DroneWeapon = CreateWeapon(Drone);
        }

        Weapon CreateWeapon(Projectile drone)
        {
            return ResourceManager.CreateWeapon("RepairBeam", drone.Owner, null, null);
        }

        public void ChooseTarget()
        {
            var target = Drone.Owner?.AI.FriendliesNearby
                .FindMinFiltered(ship => !ship.IsDeadOrDying
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

        // TODO: concurrency issue here, 
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
                // We want to immediately kill the beam, since there is a possibility it is infinite.
                // very strange implementation for drone repair logic
                if (Beam is { Active: true } beam)
                {
                    beam.Die(null, false); 
                }
            }
            else
            {
                TryFireDroneBeam();
                OrbitShip(DroneTarget, timeStep);
            }
        }

        void TryFireDroneBeam()
        {
            if (Beam == null &&
                DroneTarget.Health < DroneTarget.HealthMax &&
                DroneWeapon.CooldownTimer <= 0f &&
                DroneTarget != null &&
                Drone.Position.Distance(DroneTarget.Position) <= DroneWeapon.BaseRange)
            {
                // NOTE: Beam projectile is updated by universe
                DroneBeam droneBeam = DroneWeapon.FireDroneBeam(this);
                Beam = droneBeam;
                DroneWeapon.CooldownTimer = DroneWeapon.NetFireDelay;
            }
        }

        public void ClearBeam()
        {
            Beam = null;
        }
    }
}