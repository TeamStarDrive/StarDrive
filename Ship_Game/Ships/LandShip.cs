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
        [StarData] public readonly bool OnSpacePort;
        [StarData] public readonly Ship Station;
        [StarData] public bool Docked { get; private set; }
        [StarData] float DockSeconds;
        [StarData] public int ReturningShuttles { get; set; }
        [StarData] public bool OwnerChanged { get; private set; }
        [StarData] public readonly Ship Mothership;
        [StarData] public readonly Ship Target;
        [StarData] LandOnPlanet PlanetLanding;
        [StarData] LandOnShipyard ShipyardLanding;
        [StarData] LandInHangar HangarLanding;
        [StarData] DiveToPlanet PlanetDive;

        public LandShip(Ship owner, LandPlan landPlan, Planet planet, Ship shipyard)
        {
            Owner = owner;
            LandPlan = landPlan;
            Planet = planet;
            if (shipyard != null)
            {
                Shipyard = shipyard;
                ShipyardLanding = new(owner, planet, shipyard.TetherOffset, shipyard.Position, DockLandingSeconds(owner));
            }
            else if (LandsOnSpacePort(landPlan, planet, owner.Loyalty))
            {
                OnSpacePort = true;
                ShipyardLanding = new(owner, planet, Vector2.Zero, planet.Position, SpacePortLandingSeconds(owner));
            }
            else if (landPlan == LandPlan.AssaultDive)
            {
                PlanetDive = new(owner, planet);
            }
            else
            {
                PlanetLanding = new(owner, planet);
            }
        }

        public LandShip(Ship owner, Ship station) : this(owner, station, LandPlan.Trade, DockLandingSeconds(owner))
        {
        }

        LandShip(Ship owner, Ship station, LandPlan landPlan, float seconds)
        {
            Owner = owner;
            LandPlan = landPlan;
            Station = station;
            Planet = station.GetTether();
            Vector2 offset = Planet != null ? station.TetherOffset : station.Position;
            ShipyardLanding = new(owner, Planet, offset, station.Position, seconds);
        }

        public static LandShip OnPirateBase(Ship owner, Ship pirateBase)
            => new(owner, pirateBase, LandPlan.PirateBase, SpacePortLandingSeconds(owner));

        public LandShip(Ship owner, LandPlan landPlan, Ship ship)
        {
            Owner = owner;
            LandPlan = landPlan;
            if (landPlan == LandPlan.Board)
            {
                Target = ship;
                HangarLanding = new(owner, ship, HullTouchdown(ship, owner.Position));
            }
            else
            {
                Mothership = ship;
                HangarLanding = new(owner, ship, Vector2.Zero);
            }
        }

        public LandShip()
        {
        }

        public bool WaitsForGoal => !OwnerChanged && LandPlan switch
        {
            LandPlan.Scrap => Owner.Loyalty.AI.HasGoal(GoalType.ScrapShip, Owner),
            LandPlan.Refit => Owner.Loyalty.AI.HasGoal(GoalType.Refit, Owner),
            _ => false
        };

        public bool TakesOffIfAbandoned => LandPlan == LandPlan.Refit || OwnerChanged;

        public void OnOwnerChanged()
        {
            if (LandPlan is LandPlan.Scrap or LandPlan.Refit)
                OwnerChanged = true;
        }

        public bool Trades => LandPlan == LandPlan.Trade;

        public bool InHangar => LandPlan == LandPlan.Hangar;

        public bool Boards => LandPlan == LandPlan.Board;

        public bool DropsTroops => LandPlan is LandPlan.Troops or LandPlan.AssaultDive;

        public bool Dives => LandPlan == LandPlan.AssaultDive;

        bool OnShip => InHangar || Boards;

        public bool OnDock => Shipyard != null || OnSpacePort || Station != null;

        public void Dock(float seconds)
        {
            Docked = true;
            DockSeconds = seconds;
        }

        public bool DockTimeIsUp(FixedSimTime timeStep)
        {
            DockSeconds -= timeStep.FixedTime;
            return DockSeconds <= 0f;
        }

        public void HandOver()
        {
            switch (LandPlan)
            {
                case LandPlan.Hangar when Mothership.Active:
                    if (Mothership.Loyalty == Owner.Loyalty) Owner.AI.ReturnToMothership(Mothership);
                    else                                     Mothership.OnLaunchedShipDie(Owner);
                    break;
                case LandPlan.Board:                         Owner.AI.LandTroopsAfterTouchdown(Target); break;
                case LandPlan.Troops:
                case LandPlan.AssaultDive:                   HandOverTroops();                        break;
                case LandPlan.Builder when PlanetIsOurs:     Planet.LandBuilderShip();                break;
                case LandPlan.HomeDefense when PlanetIsOurs: Planet.LandDefenseShip(Owner);           break;
                case LandPlan.HomeDefense:                   Owner.Loyalty.RefundCreditsPostRemoval(Owner, percentOfAmount: 1f); break;
                case LandPlan.PirateBase when Station is { Active: true, Dying: false } && Station.Loyalty == Owner.Loyalty:
                    Owner.Loyalty.Pirates.TakeInLandedShip(Owner);
                    break;
            }
        }

        void HandOverTroops()
        {
            if (!Owner.LandTroopsAfterTouchdown(Planet))
                return;

            if (Dives)
                Owner.SendTroopShuttlesDown(Planet, PosZ);
            else if (OnSpacePort)
                Owner.SendTroopShuttlesFromPort(Planet);
        }

        bool PlanetIsOurs => Planet.Owner == Owner.Loyalty;

        public static bool UsesShipyards(LandPlan landPlan)
            => landPlan is not (LandPlan.HomeDefense or LandPlan.Supply or LandPlan.Trade or LandPlan.Troops or LandPlan.AssaultDive);

        public static bool LandsOnSpacePort(LandPlan landPlan, Planet planet, Empire empire)
            => planet.HasSpacePort && (landPlan is LandPlan.Supply or LandPlan.Trade || landPlan == LandPlan.Troops && planet.Owner == empire);

        public const float TouchdownRadius = 100f;
        public const float SpacePortLandingPart = 0.7f;

        public static float ShipyardLandingRange(Ship ship) => GlideRange(ship, DockLandingSeconds(ship));

        public static float SpacePortLandingRange(Ship ship) => GlideRange(ship, SpacePortLandingSeconds(ship));

        static float GlideRange(Ship ship, float seconds) => (LaunchShip.ShipyardSpeed(ship) * seconds).LowerBound(300);

        public static float DockLandingSeconds(Ship ship) => LaunchShip.ShipyardDuration(ship, LaunchShip.ShipyardRotationDegX(ship));

        public static float SpacePortLandingSeconds(Ship ship) => DockLandingSeconds(ship) * SpacePortLandingPart;

        public static float TradeLandingSeconds(Ship ship, Planet planet)
            => LandsOnSpacePort(LandPlan.Trade, planet, ship.Loyalty) ? SpacePortLandingSeconds(ship) : LaunchShip.PlanetDuration(ship);

        public static float HangarLandingRange(Ship ship) => LaunchShip.HangarSpeed(ship) * LaunchShip.HangarDuration(ship);

        public static float BoardingRange(Ship target) => target.Radius + 300f;

        public const float DiveSeconds = 2f;
        public const float RunSeconds = 1f;

        public static float DiveRunSpeed(Ship ship) => LaunchShip.HangarSpeed(ship) * 0.5f;

        static float DiveSpeedIn(Ship ship) => ship.Velocity.Dot(ship.Direction).Clamped(0, ship.MaxSTLSpeed);

        public static float DiveDistance(Ship ship)
        {
            float runSpeed = DiveRunSpeed(ship);
            return (DiveSpeedIn(ship) + runSpeed) * 0.5f * DiveSeconds + runSpeed * RunSeconds;
        }

        static Vector2 HullTouchdown(Ship ship, Vector2 from)
        {
            Vector2 touchdown = Vector2.Zero;
            float nearest = float.MaxValue;
            foreach (ShipModule module in ship.Modules)
            {
                if (!module.Active || !module.IsExternal)
                    continue;

                float distance = (ship.Position + OnHull(module.LocalCenter, ship.Rotation)).SqDist(from);
                if (distance < nearest)
                {
                    nearest = distance;
                    touchdown = module.LocalCenter;
                }
            }
            return touchdown;
        }

        static Vector2 OnHull(Vector2 local, float rotation)
        {
            float cos = RadMath.Cos(rotation);
            float sin = RadMath.Sin(rotation);
            return new Vector2(local.X * cos - local.Y * sin, local.X * sin + local.Y * cos);
        }

        public void Update(bool visibleToPlayer, FixedSimTime timeStep)
        {
            if (Done)
            {
                if (OnShip)
                    HangarLanding.StayDown();
                else if (OnDock)
                    ShipyardLanding.StayDown();
                else if (Dives)
                    PlanetDive.StayDown();
                else
                    PlanetLanding.StayDown();
                return;
            }

            float scale;
            if (OnShip)
            {
                HangarLanding.Update(timeStep, visibleToPlayer, ref PosZ);
                Done = HangarLanding.Done;
                scale = 1f;
            }
            else if (OnDock)
            {
                ShipyardLanding.Update(timeStep, visibleToPlayer, ref PosZ, out scale);
                Done = ShipyardLanding.Done;
            }
            else if (Dives)
            {
                PlanetDive.Update(timeStep, visibleToPlayer, ref PosZ, out scale);
                Done = PlanetDive.Done;
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
            [StarData] readonly Planet Planet;
            [StarData] readonly float TotalDuration;
            [StarData] readonly float StartRotationDegZ;
            [StarData] readonly float TurnDegZ;
            [StarData] readonly float StartRotationY;
            [StarData] readonly Vector2 TouchdownOffset;
            [StarData] readonly Vector2 StartOffset;
            const int EndPosZ = 2000;
            const float MaxRotationDegX = 75;

            public LandOnPlanet(Ship ship, Planet planet)
            {
                Owner = ship;
                Progress = 0;
                Planet = planet;
                TotalDuration = LaunchShip.PlanetDuration(ship);
                StartRotationDegZ = ship.RotationDegrees;
                StartRotationY = ship.YRotation;
                TouchdownOffset = Vector2.Zero.GenerateRandomPointInsideCircle(TouchdownRadius, planet.Random);
                Vector2 touchdown = planet.Position + TouchdownOffset;
                StartOffset = ship.Position - touchdown;
                TurnDegZ = StartOffset.Length() > 1f
                    ? (ship.Position.AngleToTarget(touchdown) - StartRotationDegZ + 540f) % 360f - 180f
                    : 0f;
            }

            public void Update(FixedSimTime timeStep, bool visible, ref float posZ, out float scale)
            {
                Progress = (Progress + timeStep.FixedTime / TotalDuration).UpperBound(1);
                float remaining = 1 - Progress;
                float turn = (Progress / 0.5f).UpperBound(1);
                Owner.Velocity = Vector2.Zero;
                if (Planet != null)
                    Owner.Position = Planet.Position + TouchdownOffset + StartOffset * (remaining * remaining);
                scale = remaining;
                posZ = EndPosZ * Progress;
                Owner.Rotation = (StartRotationDegZ + TurnDegZ * turn).ToRadians().AsNormalizedRadians();
                Owner.YRotation = StartRotationY * (1 - turn);
                Owner.XRotation = -(MaxRotationDegX * turn).ToRadians();

                if (visible && (Progress < 0.05f || Progress.InRange(0.48f, 0.52f) || Progress >= 0.75f))
                    Owner.Universe.Screen.Particles.Flash.AddParticle(LaunchShip.FlashPos(Owner, scale, posZ), scale);
            }

            public void StayDown()
            {
                if (Planet != null)
                    Owner.Position = Planet.Position + TouchdownOffset;
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

            public LandOnShipyard(Ship ship, Planet planet, Vector2 offsetFromPlanet, Vector2 landAt, float seconds)
            {
                Vector2 touchdownSpread = Vector2.Zero.GenerateRandomPointInsideCircle(TouchdownRadius, ship.Universe.Random);
                landAt += touchdownSpread;
                Owner = ship;
                Progress = 0;
                Planet = planet;
                ShipyardOffset = offsetFromPlanet + touchdownSpread;
                StartOffset = ship.Position - landAt;
                MaxRotationDegX = LaunchShip.ShipyardRotationDegX(ship);
                TotalDuration = seconds;
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
                Owner.Position = DockAt + StartOffset * (1 - travelled);
                Owner.Rotation = (StartRotationDegZ + TurnDegZ * turn).ToRadians().AsNormalizedRadians();
                Owner.YRotation = StartRotationY * (1 - turn);
                scale = remaining;
                posZ = EndPosZ * Progress;
                float pitch = Progress <= 0.5f ? Progress * 2 : remaining * 2;
                Owner.XRotation = -(MaxRotationDegX * pitch).ToRadians();

                if (visible && (Progress < 0.05f || Progress.InRange(0.49f, 0.51f) || Progress.InRange(0.75f, 0.9f)))
                    Owner.Universe.Screen.Particles.Flash.AddParticle(LaunchShip.FlashPos(Owner, scale, posZ), scale);
            }

            Vector2 DockAt => Planet != null ? Planet.Position + ShipyardOffset : ShipyardOffset;

            public void StayDown() => Owner.Position = DockAt;

            public bool Done => Progress >= 1f;
        }

        [StarDataType]
        struct LandInHangar
        {
            [StarData] float Progress; // between 0 to 1
            [StarData] readonly Ship Owner;
            [StarData] readonly Ship Mothership;
            [StarData] readonly Vector2 TouchdownOffset;
            [StarData] readonly Vector2 StartOffset;
            [StarData] readonly float PathShape;
            [StarData] readonly float TotalDuration;
            [StarData] readonly float StartRotationDegZ;
            [StarData] readonly float TurnDegZ;
            [StarData] readonly float StartRotationY;
            [StarData] readonly bool DoBarrelRoll;
            const float EndPosZ = 140;
            const float MaxRotationDegX = 31.5f;
            const float TurnPart = 0.2f;
            const float FlashPart = 0.85f;

            public LandInHangar(Ship ship, Ship mothership, Vector2 touchdownOffset)
            {
                Owner = ship;
                Mothership = mothership;
                TouchdownOffset = touchdownOffset;
                Progress = 0;
                TotalDuration = LaunchShip.HangarDuration(ship);
                Vector2 touchdown = mothership.Position + OnHull(touchdownOffset, mothership.Rotation);
                StartOffset = ship.Position - touchdown;
                StartRotationDegZ = ship.RotationDegrees;
                StartRotationY = ship.YRotation;
                DoBarrelRoll = !ship.IsMiningShip && ship.HealthPercent >= 1f && LaunchShip.ShouldBarrelRoll(ship);
                float distance = StartOffset.Length();
                if (distance > 1f)
                {
                    float speedIn = (ship.Velocity - mothership.Velocity).Dot(-StartOffset / distance).LowerBound(0);
                    PathShape = (speedIn * TotalDuration / distance).Clamped(0, 2);
                    TurnDegZ = (ship.Position.AngleToTarget(touchdown) - StartRotationDegZ + 540f) % 360f - 180f;
                }
                else
                {
                    PathShape = 1f;
                    TurnDegZ = 0f;
                }
            }

            public void Update(FixedSimTime timeStep, bool visible, ref float posZ)
            {
                Progress = (Progress + timeStep.FixedTime / TotalDuration).UpperBound(1);
                float travelled = PathShape * Progress + (1 - PathShape) * Progress * Progress;
                float turn = (Progress / TurnPart).UpperBound(1);
                Owner.Velocity = Vector2.Zero;
                Owner.Position = TouchdownAt + StartOffset * (1 - travelled);
                Owner.Rotation = (StartRotationDegZ + TurnDegZ * turn).ToRadians().AsNormalizedRadians();
                Owner.YRotation = StartRotationY * (1 - turn) + (DoBarrelRoll ? (360 * (1 - Progress)).ToRadians() : 0);
                posZ = EndPosZ * Progress;
                Owner.XRotation = -(MaxRotationDegX * Progress).ToRadians();

                if (visible && Progress >= FlashPart)
                    Owner.Universe.Screen.Particles.Flash.AddParticle(LaunchShip.FlashPos(Owner, 1, posZ), 1 - Progress * 0.7f);
            }

            Vector2 TouchdownAt => Mothership.Position + OnHull(TouchdownOffset, Mothership.Rotation);

            public void StayDown() => Owner.Position = TouchdownAt;

            public bool Done => Progress >= 1f;
        }

        [StarDataType]
        struct DiveToPlanet
        {
            [StarData] float Progress; // between 0 to 1
            [StarData] float Travelled;
            [StarData] readonly Ship Owner;
            [StarData] readonly Planet Planet;
            [StarData] readonly Vector2 StartOffset;
            [StarData] readonly float RotationDegZ;
            [StarData] readonly float StartRotationY;
            [StarData] readonly float StartSpeed;
            const float TotalDuration = DiveSeconds + RunSeconds;
            const float DivePart = DiveSeconds / TotalDuration;
            const float EndPosZ = 200;
            const float EndScale = 0.3f;
            const float MaxRotationDegX = 60;
            const float PitchPart = 0.2f;
            const float FlashPart = 0.2f;

            public DiveToPlanet(Ship ship, Planet planet)
            {
                Owner = ship;
                Planet = planet;
                Progress = 0;
                Travelled = 0;
                StartOffset = ship.Position - planet.Position;
                RotationDegZ = ship.RotationDegrees;
                StartRotationY = ship.YRotation;
                StartSpeed = DiveSpeedIn(ship);
            }

            public void Update(FixedSimTime timeStep, bool visible, ref float posZ, out float scale)
            {
                Progress = (Progress + timeStep.FixedTime / TotalDuration).UpperBound(1);
                float dive = (Progress / DivePart).UpperBound(1);
                Travelled += StartSpeed.LerpTo(DiveRunSpeed(Owner), dive) * timeStep.FixedTime;
                Owner.Velocity = Vector2.Zero;
                Owner.Position = DivedTo;
                Owner.RotationDegrees = RotationDegZ;
                Owner.YRotation = StartRotationY * (1 - dive);
                float pitch = dive < PitchPart ? dive / PitchPart : (1 - dive) / (1 - PitchPart);
                Owner.XRotation = -(MaxRotationDegX * pitch).ToRadians();
                posZ = EndPosZ * dive;
                scale = 1 - (1 - EndScale) * dive;

                if (visible && dive < FlashPart)
                    Owner.Universe.Screen.Particles.Flash.AddParticle(LaunchShip.FlashPos(Owner, scale, posZ), scale);
            }

            Vector2 DivedTo => Planet.Position + StartOffset + RotationDegZ.AngleToDirection() * Travelled;

            public void StayDown() => Owner.Position = DivedTo;

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
        Supply,
        Trade,
        Hangar,
        Board,
        Troops,
        AssaultDive,
        PirateBase
    }
}
