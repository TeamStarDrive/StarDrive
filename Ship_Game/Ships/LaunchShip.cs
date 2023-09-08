using NAudio.Wave;
using SDGraphics;
using Ship_Game.Data.Serialization;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Ships
{
    [StarDataType]
    public class LaunchShip
    {
        [StarData] readonly Ship Owner;
        [StarData] public float PosZ;
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
                case LaunchPlan.Planet: PlanetLaunch = new(owner, rotationDegZ); break;
                case LaunchPlan.Hangar: HangarLaunch = new(owner, rotationDegZ); break;
            }
        }
        
        public LaunchShip()
        {
        }

        public static Vector3 FlashPos(Ship ship, float scale, float posZ)
            => new Vector2(-ship.Direction * ship.Radius * scale * 0.5f + ship.Position).ToVec3(posZ);

        public void Update(bool visibleToPlayer, FixedSimTime timeStep)
        {
            Owner.AI.IgnoreCombat = true;
            float scale = 1;
            switch (LaunchPlan)
            {
                case LaunchPlan.Planet: PlanetLaunch.Update(timeStep, visibleToPlayer, ref PosZ, out scale); break;
                case LaunchPlan.Hangar: HangarLaunch.Update(timeStep, visibleToPlayer, ref PosZ); break;
            }

            if (PosZ <= 0)
            {
                Owner.XRotation = 0;
                if (!Owner.IsConstructor && !Owner.IsSupplyShuttle)
                    Owner.AI.IgnoreCombat = false;
            }

            if (!visibleToPlayer)
                return;

            var SO = Owner.GetSO();
            if (Owner.GetSO() != null)
            {
                SO.World = Matrix.CreateTranslation(new Vector3(Owner.ShipData.BaseHull.MeshOffset, 0f))
                             * Matrix.CreateRotationY(Owner.YRotation)
                             * Matrix.CreateRotationX(Owner.XRotation)
                             * Matrix.CreateRotationZ(Owner.Rotation)
                             * Matrix.CreateScale(scale)
                             * Matrix.CreateTranslation(new Vector3(Owner.Position, PosZ));
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
            [StarData] float Progress; // between 0 to 1
            [StarData] readonly Ship Owner;
            [StarData] readonly float SecondsHalfPosZ;
            [StarData] readonly float SecondsToZeroX;
            [StarData] readonly float TotalDuration;
            [StarData] readonly float RotationDegZ;
            [StarData] readonly Vector2 Velocity;
            const int MinSecondsToHalfScale = 5;
            const int MaxSecondsToHalfScale = 10;
            const int StartingPosZ = 1000;
            const float PlanetPlanRotationDegX = 75;

            public LaunchFromPlanet(Ship ship, float rotation)
            {
                Owner            = ship;
                Progress         = 0;
                RotationDegZ     = rotation;
                SecondsHalfPosZ = (StartingPosZ / ship.MaxSTLSpeed.LowerBound(100)).Clamped(MinSecondsToHalfScale, MaxSecondsToHalfScale);
                SecondsToZeroX   = PlanetPlanRotationDegX / ship.RotationRadsPerSecond.ToDegrees().LowerBound(5);
                TotalDuration    = SecondsHalfPosZ + SecondsToZeroX;

                float velRandom = ship.Universe.Random.Float(0.5f, 0.75f);
                Velocity = RotationDegZ.AngleToDirection() * Owner.MaxSTLSpeed * velRandom;
            }

            public void Update(FixedSimTime timeStep, bool visible, ref float posZ, out float scale)
            {
                Owner.Velocity = Velocity;
                Owner.YRotation = 0;
                Owner.RotationDegrees = RotationDegZ;
                Progress += timeStep.FixedTime / TotalDuration;
                scale = (Progress * 2).UpperBound(1);
                posZ = StartingPosZ * (1 - Progress);

                Owner.XRotation = Progress >= 0.5f 
                    ? (PlanetPlanRotationDegX - ((Progress - 0.5f) / 0.5f * PlanetPlanRotationDegX)).ToRadians() 
                    : PlanetPlanRotationDegX.ToRadians();

                if (visible && Progress.InRange(0f, 0.25f))
                    Owner.Universe.Screen.Particles.Flash.AddParticle(FlashPos(Owner, scale, posZ), Progress);

                if (Progress >= 0.9f)
                    Owner.UpdateThrusters(timeStep, Progress);
            }
        }

        [StarDataType]
        struct LaunchFromHangar
        {
            [StarData] float Progress; // between 0 to 1
            [StarData] readonly bool DoBarrelRoll;
            [StarData] readonly Ship Owner;
            [StarData] readonly float TotalDuration;
            [StarData] readonly float RelativeDegForBarrel;
            [StarData] readonly float RotationDegZ;
            [StarData] readonly Vector2 Velocity;
            const float InitialProgress = 0.3f;
            const int HangarPlanRotationDegX = 45;
            const int StartingPosZ = 200;


            public LaunchFromHangar(Ship ship, float rotation)
            {
                Owner         = ship;
                RotationDegZ  = rotation;
                DoBarrelRoll  = ShouldBarrelRoll();
                Progress      = InitialProgress;
                TotalDuration = (HangarPlanRotationDegX / ship.RotationRadsPerSecond.ToDegrees()).Clamped(2, 5);
                RelativeDegForBarrel = 3.6f / (1 - Progress);

                float velRandom = ship.Universe.Random.Float(1f, 2f);
                Velocity = RotationDegZ.AngleToDirection() * Owner.MaxSTLSpeed * velRandom;
            }

            bool ShouldBarrelRoll()
            {
                return Owner.DesignRole == RoleName.fighter && Owner.Universe.Random.RollDice(50)
                    || Owner.DesignRole == RoleName.corvette && Owner.Universe.Random.RollDice(25);
            }

            public void Update(FixedSimTime timeStep, bool visible, ref float posZ)
            {
                Owner.Velocity = Velocity;
                Progress += timeStep.FixedTime / TotalDuration;
                Owner.YRotation = DoBarrelRoll ? ((Progress - InitialProgress) * RelativeDegForBarrel * 100).ToRadians() : 0;
                Owner.RotationDegrees = RotationDegZ;
                Owner.XRotation = (HangarPlanRotationDegX - (Progress * HangarPlanRotationDegX)).ToRadians();
                posZ = StartingPosZ * (1 - Progress);

                if (Owner.DesignRole is RoleName.fighter or RoleName.colony || Progress >= 0.9f)
                    Owner.UpdateThrusters(timeStep, 1);

                if (visible && Progress.InRange(InitialProgress, InitialProgress + 0.1f))
                    Owner.Universe.Screen.Particles.Flash.AddParticle(FlashPos(Owner, 1, posZ), Progress);
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