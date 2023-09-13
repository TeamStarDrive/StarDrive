using SDGraphics;
using Ship_Game.Data.Serialization;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Ships
{
    [StarDataType]
    public class LaunchShip
    {
        [StarData] public bool Done { get; private set; }
        [StarData] readonly Ship Owner;
        [StarData] float PosZ;
        [StarData] readonly LaunchPlan LaunchPlan;
        [StarData] LaunchFromPlanet PlanetLaunch;
        [StarData] LaunchFromHangar HangarLaunch;
        [StarData] LaunchFromShipyardBig ShipyardBigLaunch;

        public LaunchShip(Ship owner, LaunchPlan launchPlan, float startingRotationDegrees = -1f)
        {
            Owner = owner;
            LaunchPlan = launchPlan;
            float rotationDegZ = startingRotationDegrees.Equals(-1f)
                ? owner.Universe.Random.RollDie(360)
                : (startingRotationDegrees + Owner.Universe.Random.Float(-10, 10));

            switch (LaunchPlan)
            {
                case LaunchPlan.Planet:      PlanetLaunch      = new(owner, rotationDegZ); break;
                case LaunchPlan.Hangar:      HangarLaunch      = new(owner, rotationDegZ); break;
                case LaunchPlan.ShipyardBig: ShipyardBigLaunch = new(owner, rotationDegZ); break;
            }
        }

        public LaunchShip()
        {
        }

        public static Vector3 FlashPos(Ship ship, float scale, float posZ)
            => new Vector2(-ship.Direction * ship.Radius * scale * 0.5f + ship.Position).ToVec3(posZ + 20);

        public static Vector2 StartingVelocity(Ship ship, float rotationDegZ, float randomModifier) 
            => rotationDegZ.AngleToDirection() * (ship.MaxSTLSpeed * randomModifier).UpperBound(500);


        public void Update(bool visibleToPlayer, FixedSimTime timeStep)
        {
            Owner.AI.IgnoreCombat = true;
            float scale = 1;
            switch (LaunchPlan)
            {
                case LaunchPlan.Planet:      PlanetLaunch.Update(timeStep, visibleToPlayer, ref PosZ, out scale);      break;
                case LaunchPlan.Hangar:      HangarLaunch.Update(timeStep, visibleToPlayer, ref PosZ);                 break;
                case LaunchPlan.ShipyardBig: ShipyardBigLaunch.Update(timeStep, visibleToPlayer, ref PosZ, out scale); break;
            }

            switch (LaunchPlan)
            {
                case LaunchPlan.Planet:      Done = PlanetLaunch.Done;      break;
                case LaunchPlan.Hangar:      Done = HangarLaunch.Done;      break;
                case LaunchPlan.ShipyardBig: Done = ShipyardBigLaunch.Done; break;
            }

            if (Done)
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
            const int MaxSecondsToHalfScale = 20;
            const int StartingPosZ = 2000;
            const float PlanetPlanRotationDegX = 75;

            public LaunchFromPlanet(Ship ship, float rotation)
            {
                Owner = ship;
                Progress = 0;
                RotationDegZ = rotation;
                SecondsHalfPosZ = (StartingPosZ / ship.MaxSTLSpeed.LowerBound(100)).Clamped(MinSecondsToHalfScale, MaxSecondsToHalfScale);
                SecondsToZeroX = PlanetPlanRotationDegX / ship.RotationRadsPerSecond.ToDegrees().LowerBound(5);
                TotalDuration = SecondsHalfPosZ + SecondsToZeroX;
                Velocity = StartingVelocity(ship, RotationDegZ, ship.Universe.Random.Float(0.5f, 0.75f));
            }

            public void Update(FixedSimTime timeStep, bool visible, ref float posZ, out float scale)
            {
                Owner.Velocity = Velocity;
                Progress += timeStep.FixedTime / TotalDuration;
                scale = Progress.UpperBound(1);
                posZ = StartingPosZ * (1 - Progress);

                Owner.XRotation = Progress >= 0.5f
                    ? (PlanetPlanRotationDegX - ((Progress - 0.5f) / 0.5f * PlanetPlanRotationDegX)).ToRadians()
                    : PlanetPlanRotationDegX.ToRadians();

                if (visible && (Progress.InRange(0f, 0.25f) || Progress.InRange(0.48f, 0.52f)))
                    Owner.Universe.Screen.Particles.Flash.AddParticle(FlashPos(Owner, scale, posZ), scale);

                if (Progress >= 0.9f)
                {
                    Owner.UpdateThrusters(timeStep, Progress);
                }
                else
                {
                    Owner.YRotation = 0;
                    Owner.RotationDegrees = RotationDegZ;
                }
            }

            public bool Done => Progress >= 1f;
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
            const int InitialRotationDegX = 45;
            const int StartingPosZ = 200;


            public LaunchFromHangar(Ship ship, float rotation)
            {
                Owner = ship;
                RotationDegZ = rotation;
                DoBarrelRoll = ShouldBarrelRoll();
                Progress = InitialProgress;
                TotalDuration = (InitialRotationDegX / ship.RotationRadsPerSecond.ToDegrees()).Clamped(2, 5);
                RelativeDegForBarrel = 3.6f / (1 - Progress);
                Velocity = StartingVelocity(ship, RotationDegZ, ship.Universe.Random.Float(1f, 1.2f));
            }

            bool ShouldBarrelRoll()
            {
                return Owner.DesignRole == RoleName.drone && Owner.Universe.Random.RollDice(75)
                    || Owner.DesignRole == RoleName.fighter && Owner.Universe.Random.RollDice(50)
                    || Owner.DesignRole == RoleName.corvette && Owner.Universe.Random.RollDice(25);
            }

            public void Update(FixedSimTime timeStep, bool visible, ref float posZ)
            {
                Owner.Velocity = Velocity;
                Progress += timeStep.FixedTime / TotalDuration;
                Owner.YRotation = DoBarrelRoll ? ((Progress - InitialProgress) * RelativeDegForBarrel * 100).ToRadians() : 0;
                Owner.RotationDegrees = RotationDegZ;
                Owner.XRotation = (InitialRotationDegX - (Progress * InitialRotationDegX)).ToRadians();
                posZ = StartingPosZ * (1 - Progress);

                if (Owner.DesignRole is RoleName.fighter or RoleName.corvette || Progress >= 0.9f)
                    Owner.UpdateThrusters(timeStep, 1);

                if (visible && Progress.InRange(InitialProgress, InitialProgress + 0.1f))
                    Owner.Universe.Screen.Particles.Flash.AddParticle(FlashPos(Owner, 1, posZ), Progress);
            }

            public bool Done => Progress >= 1f;
        }

        [StarDataType]
        struct LaunchFromShipyardBig
        {
            [StarData] float Progress; // between 0 to 1
            [StarData] readonly Ship Owner;
            [StarData] readonly float TotalDuration;
            [StarData] readonly float RotationDegZ;
            [StarData] readonly Vector2 Velocity;
            [StarData] readonly int MaxRotationDegX;
            const float InitialProgress = 0.1f;
            const int StartingPosZ = 400;

            public LaunchFromShipyardBig(Ship ship, float rotation)
            {
                Owner = ship;
                RotationDegZ = rotation;
                Progress = InitialProgress;
                float velRandom = ship.Universe.Random.Float(0.5f, 0.8f);
                Velocity = StartingVelocity(ship, RotationDegZ, ship.Universe.Random.Float(0.5f, 0.8f));
                switch (ship.ShipData.HullRole)
                {
                    case RoleName.fighter:
                    case RoleName.corvette:
                    case RoleName.frigate: MaxRotationDegX = 90; break;
                    case RoleName.cruiser: MaxRotationDegX = 80; break;
                    case RoleName.capital: MaxRotationDegX = 60; break;
                    default: MaxRotationDegX = 75; break;
                }

                TotalDuration = (MaxRotationDegX / (ship.RotationRadsPerSecond.ToDegrees() * 0.25f)).Clamped(5, 15);
            }

            public void Update(FixedSimTime timeStep, bool visible, ref float posZ, out float scale)
            {
                Owner.Velocity = Velocity;
                Progress += timeStep.FixedTime / TotalDuration;
                scale = Progress.UpperBound(1);
                posZ = StartingPosZ * (1 - Progress);
                float rotationXRatio = MaxRotationDegX * 2f; // progress is multiplied by 100 since its 0-1 so that why no div by 50
                Owner.XRotation = Progress <= 0.5 ? (Progress* rotationXRatio).ToRadians() : ((1 - Progress)* rotationXRatio).ToRadians();

                if (visible && (Progress.InRange(InitialProgress, 0.25f) || Progress.InRange(0.49f, 0.51f)))
                    Owner.Universe.Screen.Particles.Flash.AddParticle(FlashPos(Owner, scale, posZ), scale);

                if (Progress <= 0.8f)
                {
                    Owner.YRotation = 0;
                    Owner.RotationDegrees = RotationDegZ;
                }
                else
                {
                    Owner.UpdateThrusters(timeStep, scale);
                }
            }

            public bool Done => Progress >= 1f;
        }

    }
    
    public enum LaunchPlan
    {
        Planet,
        Hangar,
        ShipyardBig
    }

}