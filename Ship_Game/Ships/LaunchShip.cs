using SDGraphics;
using Ship_Game.Data.Serialization;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Ships
{
    [StarDataType]
    public class LaunchShip
    {
        [StarData] readonly Ship Owner;
        [StarData] public float Scale;
        [StarData] float RotationDegX;
        [StarData] float RotationRadZ;
        [StarData] float StartingScale;
        [StarData] float SecondsToZeroX;
        [StarData] float SecondsHalfScale;
        [StarData] float TotalDuration;
        [StarData] bool DoBarrelRoll;
        [StarData] readonly LaunchPlan LaunchPlan;
        const int DistanceMovementUp = 1000;
        const int MinSecondsToHalfScale = 2;
        const int MaxSecondsToHalfScale = 10;
        const int PlanetPlanRotationDegX = 75;
        const int HangarPlanRotationDegX = 45;

        public LaunchShip(Ship owner, LaunchPlan launchPlan, float startingRotationDegrees = -1f)
        {
            Owner = owner;
            LaunchPlan = launchPlan;
            switch (LaunchPlan)
            {
                case LaunchPlan.Planet: SetupPlanetLaunch(); break;
                case LaunchPlan.Hangar: SetupHangarLaunch(); break;
            }

            Scale = StartingScale;
            if (startingRotationDegrees.Equals(-1f))
                RotationRadZ = (45f * ((int)(owner.Universe.StarDate*10) % 10 % 8)).ToRadians();
            else
                RotationRadZ = (startingRotationDegrees + Owner.Universe.Random.Float(-10,10)).ToRadians();

            float velRandom = owner.Universe.Random.Float(0.7f, 1f);
            Owner.Velocity = RotationRadZ.RadiansToDirection() * Owner.MaxSTLSpeed * velRandom;
        }

        public LaunchShip()
        {
        }

        void SetupPlanetLaunch()
        {
            StartingScale = 0;
            RotationDegX = PlanetPlanRotationDegX;
            SecondsHalfScale = (DistanceMovementUp / Owner.MaxSTLSpeed.LowerBound(100)).Clamped(MinSecondsToHalfScale, MaxSecondsToHalfScale);
            SecondsToZeroX = RotationDegX / Owner.RotationRadsPerSecond.ToDegrees().LowerBound(5);
            TotalDuration = SecondsHalfScale + SecondsToZeroX;
        }

        void SetupHangarLaunch()
        {
            DoBarrelRoll = ShouldBarrelRoll();
            StartingScale = 0.3f;
            RotationDegX = HangarPlanRotationDegX;
            TotalDuration = (RotationDegX / Owner.RotationRadsPerSecond.ToDegrees()).Clamped(2,5);
        }

        bool ShouldBarrelRoll()
        {
            return Owner.DesignRole == RoleName.fighter && Owner.Universe.Random.RollDice(25)
                || Owner.DesignRole == RoleName.corvette && Owner.Universe.Random.RollDice(10);
        }

        Vector3 FlashPos => new Vector2(-Owner.Direction * Owner.Radius * Scale * 0.5f + Owner.Position).ToVec3();

        public void Update(bool visibleToPlayer, FixedSimTime timeStep)
        {
            Owner.AI.IgnoreCombat = true;
            switch (LaunchPlan)
            {
                case LaunchPlan.Planet: UpdatePlanetLaunch(timeStep, visibleToPlayer); break;
                case LaunchPlan.Hangar: UpdateHangarLaunch(timeStep, visibleToPlayer); break;
            }

            if (Scale >= 1 && !Owner.IsConstructor && !Owner.IsSupplyShuttle)
            {
                Owner.XRotation = 0;
                Owner.AI.IgnoreCombat = false;
            }

            if (!visibleToPlayer)
                return;

            var SO = Owner.GetSO();
            if (Owner.GetSO() != null)
            {
                SO.World = Matrix.CreateTranslation(new Vector3(Owner.ShipData.BaseHull.MeshOffset, 0f))
                             * Matrix.CreateScale(Scale)
                             * Matrix.CreateRotationY(Owner.YRotation)
                             * Matrix.CreateRotationX(RotationDegX.ToRadians())
                             * Matrix.CreateRotationZ(Owner.Rotation)
                             * Matrix.CreateTranslation(new Vector3(Owner.Position, 0f));
                SO.UpdateAnimation(timeStep.FixedTime);
            }
            else // auto-create scene objects if possible
            {
                Owner.Universe.Screen?.QueueSceneObjectCreation(Owner);
            }
        }

        void UpdatePlanetLaunch(FixedSimTime timeStep, bool visible)
        {
            Owner.YRotation = 0;
            Owner.Rotation = RotationRadZ;
            Scale += timeStep.FixedTime / TotalDuration;
            if (Scale >= 0.5f)
                RotationDegX = PlanetPlanRotationDegX - ((Scale - 0.5f) / 0.5f * PlanetPlanRotationDegX);

            if (visible && Scale.InRange(0f, 0.25f))
                Owner.Universe.Screen.Particles.Flash.AddParticle(FlashPos, Scale);

            if (Scale >= 0.9f)
                Owner.UpdateThrusters(timeStep, Scale);
        }

        void UpdateHangarLaunch(FixedSimTime timeStep, bool visible)
        {
            float relativeDeg = 3.6f / (1-StartingScale);
            Owner.YRotation = DoBarrelRoll ? ((Scale-StartingScale) * relativeDeg*100).ToRadians() : 0;
            Owner.Rotation = RotationRadZ;
            Scale += timeStep.FixedTime / TotalDuration;
            RotationDegX = HangarPlanRotationDegX - (Scale * HangarPlanRotationDegX);

            if (Owner.DesignRole is RoleName.fighter or RoleName.colony || Scale >= 0.9f)
                Owner.UpdateThrusters(timeStep, Scale);

            if (visible && Scale.InRange(StartingScale, StartingScale+0.1f))
                Owner.Universe.Screen.Particles.Flash.AddParticle(FlashPos, Scale);
        }
    }

    public enum LaunchPlan
    {
        Planet,
        Hangar,
        Shipyard
    }

}