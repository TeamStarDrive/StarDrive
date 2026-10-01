using SDGraphics;
using SDUtils;
using Ship_Game.AI;
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
        [StarData] public readonly Planet Planet;
        [StarData] public readonly Ship Shipyard;
        [StarData] readonly bool OnSpacePort;
        [StarData] LandOnPlanet PlanetLanding;
        [StarData] LandOnShipyard ShipyardLanding;

        public LandShip(Ship owner, LandPlan landPlan, Planet planet, Ship shipyard)
        {
            Owner = owner;
            LandPlan = landPlan;
            Planet = planet;
            if (shipyard != null)
            {
                Shipyard = shipyard;
                ShipyardLanding = new(owner, planet, shipyard.TetherOffset, shipyard.Position);
            }
            else if (LandsOnSpacePort(landPlan, planet))
            {
                OnSpacePort = true;
                ShipyardLanding = new(owner, planet, Vector2.Zero, planet.Position);
            }
            else
            {
                PlanetLanding = new(owner, planet);
            }
        }

        public LandShip()
        {
        }

        public bool WaitsForGoal => LandPlan switch
        {
            LandPlan.Scrap => Owner.Loyalty.AI.HasGoal(GoalType.ScrapShip, Owner),
            LandPlan.Refit => Owner.Loyalty.AI.HasGoal(GoalType.Refit, Owner),
            _ => false
        };

        public bool TakesOffIfAbandoned => LandPlan == LandPlan.Refit;

        public void HandOverToPlanet()
        {
            bool stillOurs = Planet.Owner == Owner.Loyalty;
            switch (LandPlan)
            {
                case LandPlan.Builder when stillOurs:     Planet.LandBuilderShip();       break;
                case LandPlan.HomeDefense when stillOurs: Planet.LandDefenseShip(Owner);  break;
                case LandPlan.HomeDefense:                Owner.Loyalty.RefundCreditsPostRemoval(Owner, percentOfAmount: 1f); break;
            }
        }

        public static bool UsesShipyards(LandPlan landPlan) => landPlan is not (LandPlan.HomeDefense or LandPlan.Supply);

        public static bool LandsOnSpacePort(LandPlan landPlan, Planet planet) => landPlan == LandPlan.Supply && planet.HasSpacePort;

        public static float ShipyardLandingRange(Ship ship)
            => (LaunchShip.ShipyardSpeed(ship) * LaunchShip.ShipyardDuration(ship, LaunchShip.ShipyardRotationDegX(ship))).LowerBound(300);

        public void Update(bool visibleToPlayer, FixedSimTime timeStep)
        {
            if (Done)
                return;

            float scale;
            if (Shipyard != null || OnSpacePort)
            {
                ShipyardLanding.Update(timeStep, visibleToPlayer, ref PosZ, out scale);
                Done = ShipyardLanding.Done;
            }
            else
            {
                PlanetLanding.Update(timeStep, visibleToPlayer, ref PosZ, out scale);
                Done = PlanetLanding.Done;
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

        [StarDataType]
        struct LandOnShipyard
        {
            [StarData] float Progress; // between 0 to 1
            [StarData] readonly Ship Owner;
            [StarData] readonly Planet Planet;
            [StarData] readonly Vector2 ShipyardOffset;
            [StarData] readonly Vector2 StartOffset;
            [StarData] readonly float PathShape;
            [StarData] readonly float TotalDuration;
            [StarData] readonly float StartRotationDegZ;
            [StarData] readonly float TurnDegZ;
            [StarData] readonly float StartRotationY;
            [StarData] readonly int MaxRotationDegX;
            const int EndPosZ = 400;
            const float TurnPart = 0.2f;

            public LandOnShipyard(Ship ship, Planet planet, Vector2 offsetFromPlanet, Vector2 landAt)
            {
                Owner = ship;
                Progress = 0;
                Planet = planet;
                ShipyardOffset = offsetFromPlanet;
                StartOffset = ship.Position - landAt;
                MaxRotationDegX = LaunchShip.ShipyardRotationDegX(ship);
                TotalDuration = LaunchShip.ShipyardDuration(ship, MaxRotationDegX);
                StartRotationDegZ = ship.RotationDegrees;
                StartRotationY = ship.YRotation;
                float distance = StartOffset.Length();
                if (distance > 1f)
                {
                    float speedIn = ship.Velocity.Dot(-StartOffset / distance).LowerBound(0);
                    PathShape = (speedIn * TotalDuration / distance).Clamped(0, 2);
                    TurnDegZ = (ship.Position.AngleToTarget(landAt) - StartRotationDegZ + 540f) % 360f - 180f;
                }
                else
                {
                    PathShape = 1f;
                    TurnDegZ = 0f;
                }
            }

            public void Update(FixedSimTime timeStep, bool visible, ref float posZ, out float scale)
            {
                Progress = (Progress + timeStep.FixedTime / TotalDuration).UpperBound(1);
                float remaining = 1 - Progress;
                float travelled = PathShape * Progress + (1 - PathShape) * Progress * Progress;
                float turn = (Progress / TurnPart).UpperBound(1);
                Owner.Velocity = Vector2.Zero;
                Owner.Position = Planet.Position + ShipyardOffset + StartOffset * (1 - travelled);
                Owner.Rotation = (StartRotationDegZ + TurnDegZ * turn).ToRadians().AsNormalizedRadians();
                Owner.YRotation = StartRotationY * (1 - turn);
                scale = remaining;
                posZ = EndPosZ * Progress;
                float pitch = Progress <= 0.5f ? Progress * 2 : remaining * 2;
                Owner.XRotation = -(MaxRotationDegX * pitch).ToRadians();

                if (visible && (Progress < 0.05f || Progress.InRange(0.49f, 0.51f) || Progress.InRange(0.75f, 0.9f)))
                    Owner.Universe.Screen.Particles.Flash.AddParticle(LaunchShip.FlashPos(Owner, scale, posZ), scale);
            }

            public bool Done => Progress >= 1f;
        }
    }

    public enum LandPlan
    {
        Colonize,
        Scrap,
        Refit,
        Builder,
        HomeDefense,
        Supply
    }
}
