using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;

namespace Ship_Game.Ships
{
    [StarDataType]
    public class LandShip
    {
        [StarData] public bool Done { get; private set; }
        [StarData] readonly Ship Owner;
        [StarData] float PosZ;
        [StarData] readonly LandPlan LandPlan;
        [StarData] LandOnPlanet PlanetLanding;

        public LandShip(Ship owner, LandPlan landPlan, Planet planet)
        {
            Owner = owner;
            LandPlan = landPlan;
            switch (LandPlan)
            {
                case LandPlan.Colonize: PlanetLanding = new(owner, planet); break;
            }
        }

        public LandShip()
        {
        }

        public void Update(bool visibleToPlayer, FixedSimTime timeStep)
        {
            float scale = 1;
            switch (LandPlan)
            {
                case LandPlan.Colonize:
                    PlanetLanding.Update(timeStep, visibleToPlayer, ref PosZ, out scale);
                    Done = PlanetLanding.Done;
                    break;
            }

            if (visibleToPlayer && !Done)
                LaunchShip.UpdateSceneObject(Owner, scale, PosZ, timeStep);
        }

        [StarDataType]
        struct LandOnPlanet
        {
            [StarData] float Progress; // between 0 to 1
            [StarData] readonly Ship Owner;
            [StarData] readonly float TotalDuration;
            [StarData] readonly float StartRotationDegZ;
            [StarData] readonly float TurnDegZ;
            [StarData] readonly float StartRotationY;
            [StarData] readonly Vector2 Velocity;
            const int EndPosZ = 2000;
            const float MaxRotationDegX = 75;

            public LandOnPlanet(Ship ship, Planet planet)
            {
                Owner = ship;
                Progress = 0;
                float secondsToHalfPosZ = (EndPosZ / ship.MaxSTLSpeed.LowerBound(100)).Clamped(5, 20);
                float secondsToMaxX = MaxRotationDegX / ship.RotationRadsPerSecond.ToDegrees().LowerBound(5);
                TotalDuration = secondsToHalfPosZ + secondsToMaxX;
                StartRotationDegZ = ship.RotationDegrees;
                StartRotationY = ship.YRotation;
                Vector2 toCenter = planet.Position - ship.Position;
                TurnDegZ = toCenter.Length() > 1f
                    ? (ship.Position.AngleToTarget(planet.Position) - StartRotationDegZ + 540f) % 360f - 180f
                    : 0f;
                Velocity = toCenter * (0.5f / TotalDuration);
            }

            public void Update(FixedSimTime timeStep, bool visible, ref float posZ, out float scale)
            {
                Progress = (Progress + timeStep.FixedTime / TotalDuration).UpperBound(1);
                float turn = (Progress / 0.5f).UpperBound(1);
                Owner.Velocity = Velocity * (2 * (1 - Progress));
                scale = 1 - Progress;
                posZ = EndPosZ * Progress;
                Owner.Rotation = (StartRotationDegZ + TurnDegZ * turn).ToRadians().AsNormalizedRadians();
                Owner.YRotation = StartRotationY * (1 - turn);
                Owner.XRotation = -(MaxRotationDegX * turn).ToRadians();

                if (visible && (Progress < 0.05f || Progress.InRange(0.48f, 0.52f) || Progress >= 0.75f))
                    Owner.Universe.Screen.Particles.Flash.AddParticle(LaunchShip.FlashPos(Owner, scale, posZ), scale);
            }

            public bool Done => Progress >= 1f;
        }
    }

    public enum LandPlan
    {
        Colonize
    }
}
