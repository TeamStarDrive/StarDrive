using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI
{
    public sealed class DroneAI 
    {
        public readonly Projectile Drone;
        public Ship DroneTarget { get; private set; }
        private float ThinkTimer;
        public Weapon DroneWeapon;
        private float OrbitalAngle;
        private Beam Beam;

        public DroneAI(Projectile drone)
        {
            Drone = drone;
            DroneWeapon = ResourceManager.CreateWeapon("RepairBeam", drone.Owner, null, null);
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
                if (Beam?.Active == true)
                    Beam.Die(null, false); 
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