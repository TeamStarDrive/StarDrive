using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Graphics.Particles;
using Vector2 = SDGraphics.Vector2;
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
        [StarData] readonly LaunchPlan LaunchPlan;
        const int DistanceMovementUp = 1000;
        const int MinSecondsToHalfScale = 2;
        const int MaxSecondsToHalfScale = 10;
        const int PlanetPlanRotationDegX = 75;
        const int HangarPlanRotationDegX = 45;


        [StarDataConstructor]
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
            StartingScale = 0.3f;
            RotationDegX = HangarPlanRotationDegX;
            TotalDuration = (RotationDegX / Owner.RotationRadsPerSecond.ToDegrees()).Clamped(2,5);
        }


        public void Update(bool visibleToPlayer, FixedSimTime timeStep)
        {
            Owner.AI.IgnoreCombat = true;
            switch (LaunchPlan)
            {
                case LaunchPlan.Planet: UpdatePlanetLaunch(timeStep); break;
                case LaunchPlan.Hangar: UpdateHangarLaunch(timeStep); break;
            }

            if (Scale >= 1 && !Owner.IsConstructor && !Owner.IsSupplyShuttle)
                Owner.AI.IgnoreCombat = false;

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

                //UpdateThrusters(timeStep);
            }
            else // auto-create scene objects if possible
            {
                Owner.Universe.Screen?.QueueSceneObjectCreation(Owner);
            }
        }

        void UpdatePlanetLaunch(FixedSimTime timeStep)
        {
            Owner.YRotation = 0;
            Owner.Rotation = RotationRadZ;
            Scale += timeStep.FixedTime / TotalDuration;
            if (Scale >= 0.5f)
                RotationDegX = PlanetPlanRotationDegX - ((Scale - 0.5f) / 0.5f * PlanetPlanRotationDegX);

            if (Scale >= 0.9f)
                Owner.UpdateThrusters(timeStep, Scale);
        }

        void UpdateHangarLaunch(FixedSimTime timeStep)
        {
            Owner.YRotation = 0;
            Owner.Rotation = RotationRadZ;
            Scale += timeStep.FixedTime / TotalDuration;
            RotationDegX = HangarPlanRotationDegX - (Scale * HangarPlanRotationDegX);
            Owner.UpdateThrusters(timeStep, Scale);
        }
    }

    public enum LaunchPlan
    {
        Planet,
        Hangar,
        Shipyard
    }

}