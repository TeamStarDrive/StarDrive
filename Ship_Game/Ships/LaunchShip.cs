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
        [StarData] readonly LaunchPlan LaunchPlan;
        [StarData] LaunchFromPlanet PlanetLaunch;
        [StarData] LaunchFromHangar HangarLaunch;

        public LaunchShip(Ship owner, LaunchPlan launchPlan, float startingRotationDegrees = -1f)
        {
            Owner = owner;
            LaunchPlan = launchPlan;

            float rotationDegZ = startingRotationDegrees.Equals(-1f) 
                ? (45f * ((int)(owner.Universe.StarDate * 10) % 10 % 8)) 
                : (startingRotationDegrees + Owner.Universe.Random.Float(-10, 10)); 

            switch (LaunchPlan)
            {
                case LaunchPlan.Planet: PlanetLaunch = new(owner, rotationDegZ, out Scale); break;
                case LaunchPlan.Hangar: HangarLaunch = new(owner, rotationDegZ, out Scale); break;
            }

            float velRandom = owner.Universe.Random.Float(0.75f, 1.25f);
            Owner.Velocity = rotationDegZ.AngleToDirection() * Owner.MaxSTLSpeed * velRandom;
        }
        
        public LaunchShip()
        {
        }

        public static Vector3 FlashPos(Ship ship, float scale)
            => new Vector2(-ship.Direction * ship.Radius * scale * 0.5f + ship.Position).ToVec3();

        public void Update(bool visibleToPlayer, FixedSimTime timeStep)
        {
            Owner.AI.IgnoreCombat = true;
            switch (LaunchPlan)
            {
                case LaunchPlan.Planet: PlanetLaunch.Update(timeStep, visibleToPlayer, ref Scale); break;
                case LaunchPlan.Hangar: HangarLaunch.Update(timeStep, visibleToPlayer, ref Scale); break;
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
                             * Matrix.CreateRotationX(Owner.XRotation)
                             * Matrix.CreateRotationZ(Owner.Rotation)
                             * Matrix.CreateTranslation(new Vector3(Owner.Position, 0f));
                SO.UpdateAnimation(timeStep.FixedTime);
            }
            else // auto-create scene objects if possible
            {
                Owner.Universe.Screen?.QueueSceneObjectCreation(Owner);
            }
        }

        [StarDataType]
        struct LaunchFromPlanet
        {
            [StarData] readonly Ship Owner;
            [StarData] readonly float SecondsHalfScale;
            [StarData] readonly float SecondsToZeroX;
            [StarData] readonly float TotalDuration;
            [StarData] readonly float RotationDegZ;
            const int MinSecondsToHalfScale = 2;
            const int MaxSecondsToHalfScale = 10;
            const int DistanceMovementUp = 1000;
            const float PlanetPlanRotationDegX = 75;
            const float InitialScale = 0;

            public LaunchFromPlanet(Ship ship, float rotation, out float initialScale)
            {
                Owner = ship;
                RotationDegZ     = rotation;
                initialScale     = InitialScale;
                SecondsHalfScale = (DistanceMovementUp / ship.MaxSTLSpeed.LowerBound(100)).Clamped(MinSecondsToHalfScale, MaxSecondsToHalfScale);
                SecondsToZeroX   = PlanetPlanRotationDegX / ship.RotationRadsPerSecond.ToDegrees().LowerBound(5);
                TotalDuration    = SecondsHalfScale + SecondsToZeroX;
            }

            public void Update(FixedSimTime timeStep, bool visible, ref float scale)
            {
                Owner.YRotation = 0;
                Owner.RotationDegrees = RotationDegZ;
                scale += timeStep.FixedTime / TotalDuration;

                Owner.XRotation = scale >= 0.5f 
                    ? (PlanetPlanRotationDegX - ((scale - 0.5f) / 0.5f * PlanetPlanRotationDegX)).ToRadians() 
                    : PlanetPlanRotationDegX.ToRadians();

                if (visible && scale.InRange(0f, 0.25f))
                    Owner.Universe.Screen.Particles.Flash.AddParticle(FlashPos(Owner, scale), scale);

                if (scale >= 0.9f)
                    Owner.UpdateThrusters(timeStep, scale);
            }
        }

        [StarDataType]
        struct LaunchFromHangar
        {
            [StarData] readonly bool DoBarrelRoll;
            [StarData] readonly Ship Owner;
            [StarData] readonly float TotalDuration;
            [StarData] readonly float RelativeDegForBarrel;
            [StarData] readonly float RotationDegZ;
            const float InitialScale = 0.3f;
            const int HangarPlanRotationDegX = 45;


            public LaunchFromHangar(Ship ship, float rotation, out float initialScale)
            {
                Owner = ship;
                RotationDegZ  = rotation;
                DoBarrelRoll  = ShouldBarrelRoll();
                initialScale  = InitialScale;
                TotalDuration = (HangarPlanRotationDegX / ship.RotationRadsPerSecond.ToDegrees()).Clamped(2, 5);
                RelativeDegForBarrel = 3.6f / (1 - initialScale);
            }

            bool ShouldBarrelRoll()
            {
                return Owner.DesignRole == RoleName.fighter && Owner.Universe.Random.RollDice(50)
                    || Owner.DesignRole == RoleName.corvette && Owner.Universe.Random.RollDice(25);
            }

            public void Update(FixedSimTime timeStep, bool visible, ref float scale)
            {
                scale += timeStep.FixedTime / TotalDuration;
                Owner.YRotation = DoBarrelRoll ? ((scale - InitialScale) * RelativeDegForBarrel * 100).ToRadians() : 0;
                Owner.RotationDegrees = RotationDegZ;
                Owner.XRotation = (HangarPlanRotationDegX - (scale * HangarPlanRotationDegX)).ToRadians();

                if (Owner.DesignRole is RoleName.fighter or RoleName.colony || scale >= 0.9f)
                    Owner.UpdateThrusters(timeStep, scale);

                if (visible && scale.InRange(InitialScale, InitialScale + 0.1f))
                    Owner.Universe.Screen.Particles.Flash.AddParticle(FlashPos(Owner, scale), scale);
            }
        }
    }

    public enum LaunchPlan
    {
        Planet,
        Hangar,
        Shipyard
    }

}