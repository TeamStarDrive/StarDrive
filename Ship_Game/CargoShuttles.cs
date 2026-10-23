using System;
using SDGraphics;
using SDUtils;
using Ship_Game.ExtensionMethods;
using Ship_Game.Graphics.Particles;
using Ship_Game.Ships;
using Ship_Game.Utils;
using Color = Microsoft.Xna.Framework.Color;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game
{
    /// <summary>
    /// Small cargo shuttles flying up from a planet to its space port while a freighter lands there to trade,
    /// and back down as it takes off; from a space port to a troop ship unloading onto the colony, and back; down
    /// from an assault shuttle dropping its troop; from a supply shuttle to the ship it rearms; from a salvage
    /// platform to the derelict it strips, and back; and from a salvage platform to its station with its load.
    /// They are only a visual effect: nothing is saved and nothing waits for them.
    /// </summary>
    public sealed class CargoShuttles
    {
        public const float CargoPerShuttle = 20f;
        public const int MaxShuttles = 20;
        public const int TroopShuttles = 3;
        public const float TroopShuttleSeconds = 5f;
        public const float OrdnancePerShuttle = 2f;
        public const int MaxOrdnanceShuttles = 5;
        public const float OrdnanceShuttleSeconds = 3f;
        public const float ProductionPerSalvageShuttle = 3f;
        public const float WorkPerRecoveryShuttle = 6f;
        // a salvage platform's shuttles launch 0.2 to 2 s after the turn's stripping, take 2 to 2.9 s each way, and turn back at once
        public const float MinHullLaunchDelay = 0.2f;
        public const float MaxHullLaunchDelay = 2f;
        public const float HullShuttleSeconds = 4.8f;
        public const float ProductionPerLoadShuttle = 15f;
        public const float ProductionPerRefundShuttle = 50f;
        public const float RefundShuttleSeconds = 5f;
        // a load's shuttles fly at the pace of the other cargo shuttles, three times the hull shuttles'
        public const float LoadShuttleSpeed = 600f;
        // a load's shuttles are twice the size of the others, their glow and trail alike
        public const float LoadShuttleSize = 2f;
        const float LoadFrustumRadius = 200f;
        const float LaunchPart = 0.35f;
        const float DropLaunchPart = 0.1f;
        const float MinFlightPart = 0.42f;
        const float MaxFlightPart = 0.6f;
        const float MeanFlightPart = (MinFlightPart + MaxFlightPart) / 2f;
        const float MaxBend = 0.25f;
        const float SurfaceLift = 30f;
        const float PortSpread = 50f;
        const float DropSpread = 30f;
        const float MaxDropPart = 0.9f;
        const float FlashScale = 0.3f;
        // the shuttle's glow, at half its particle's size
        const float GlowScale = 0.5f;
        const float TrailScale = 0.8f;

        enum Route
        {
            Trade,
            PortToShip,
            Drop,
            ShipToShip,
            ShipToHull,
            Load
        }

        readonly struct Request
        {
            public readonly Route Route;
            public readonly Planet Planet;
            public readonly Ship Ship;
            public readonly Ship FromShip;
            public readonly Vector3 From;
            public readonly Color TrailColor;
            public readonly int Count;
            public readonly float Seconds;
            public readonly bool Rising;
            public readonly float Spread;
            public readonly float Size;

            // size multiplies the glow's and the trail's scales
            public Request(Route route, Planet planet, Ship ship, Ship fromShip, Vector3 from, Color trailColor, int count, float seconds, bool rising,
                           float spread = 0f, float size = 1f)
            {
                Route = route;
                Planet = planet;
                Ship = ship;
                FromShip = fromShip;
                From = from;
                TrailColor = trailColor;
                Count = count;
                Seconds = seconds;
                Rising = rising;
                Spread = spread;
                Size = size;
            }
        }

        sealed class Shuttle
        {
            public Planet Planet;
            public Ship Ship;
            public Ship FromShip;
            public Ship Launcher; // the ship it launches from, on the routes that start at a ship
            public Color TrailColor;
            public Vector3 Low;
            public Vector3 High;
            public float Bend;
            public float Delay;
            public float Flight;
            public float Age;
            public bool Rising;
            public bool ComesBack;
            public bool SeenWhereItFlies;
            public float Size;

            Vector2 Origin => Planet?.Position ?? Vector2.Zero;
            Vector3 LowEnd => FromShip != null ? (FromShip.Position - Origin).ToVec3(0f) : Low;
            Vector3 HighEnd => Ship != null ? (Ship.Position - Origin).ToVec3(0f) : High;

            // a load's shuttle where it flies, the others by their planet or ship
            public bool IsInFrustum(UniverseScreen screen, float progress)
                => SeenWhereItFlies ? screen.IsInFrustum(PositionAt(progress.UpperBound(1f)), LoadFrustumRadius) : Planet?.InFrustum ?? Ship.InFrustum;

            public Vector3 PositionAt(float progress)
            {
                float p = Rising ? progress : 1f - progress;
                float across = p * p * (3f - 2f * p);
                float up = 1f - (1f - p) * (1f - p);
                Vector3 low = LowEnd;
                Vector3 high = HighEnd;
                Vector2 low2 = low.ToVec2();
                Vector2 high2 = high.ToVec2();
                Vector2 side = (high2 - low2).LeftVector() * (Bend * RadMath.Sin(p * RadMath.PI));
                Vector2 offset = low2.LerpTo(high2, across) + side;
                return (Origin + offset).ToVec3(low.Z.LerpTo(high.Z, up));
            }
        }

        readonly Array<Request> Requests = new();
        readonly Array<Shuttle> Flying = new();
        readonly RandomBase Random = new SeededRandom();

        public static int ShuttlesFor(float cargo) => ((int)(cargo / CargoPerShuttle)).Clamped(1, MaxShuttles);

        public static int OrdnanceShuttlesFor(float ordnance) => ((int)(ordnance / OrdnancePerShuttle)).Clamped(1, MaxOrdnanceShuttles);

        // one for every 3 production stripped, rounded up
        public static int SalvageShuttlesFor(float stripped) => ((int)Math.Ceiling(stripped / ProductionPerSalvageShuttle)).Clamped(1, MaxShuttles);

        // one for every 6 slots of a turn's recovery work, rounded up
        public static int RecoveryShuttlesFor(float work) => ((int)Math.Ceiling(work / WorkPerRecoveryShuttle)).Clamped(1, MaxShuttles);

        // one for every 15 production of the load, rounded up
        public static int LoadShuttlesFor(float load) => ((int)Math.Ceiling(load / ProductionPerLoadShuttle)).Clamped(1, MaxShuttles);

        // one for every 50 production a builder sends down, rounded up
        public static int RefundShuttlesFor(float production) => ((int)Math.Ceiling(production / ProductionPerRefundShuttle)).Clamped(1, MaxShuttles);

        public void Send(Planet planet, Color trailColor, int shuttles, float seconds, bool toPort)
            => Queue(new(Route.Trade, planet, null, null, Vector3.Zero, trailColor, shuttles, seconds, rising: toPort));

        public void SendToShip(Planet planet, Ship ship, Color trailColor, int shuttles, float seconds)
            => Queue(new(Route.PortToShip, planet, ship, null, Vector3.Zero, trailColor, shuttles, seconds, rising: true));

        public void SendDown(Planet planet, Vector3 from, Color trailColor, int shuttles, float seconds)
            => Queue(new(Route.Drop, planet, null, null, from, trailColor, shuttles, seconds, rising: false));

        public void SendShipToShip(Ship from, Ship to, Color trailColor, int shuttles, float seconds)
            => Queue(new(Route.ShipToShip, null, to, from, Vector3.Zero, trailColor, shuttles, seconds, rising: true));

        // each to a random point on the hull and back
        public void SendToHullAndBack(Ship from, Vector3 hull, float hullRadius, Color trailColor, int shuttles)
            => Queue(new(Route.ShipToHull, null, from, null, hull, trailColor, shuttles, HullShuttleSeconds, rising: false, spread: hullRadius * 0.5f));

        // one way, their flight timed by the distance
        public void SendLoad(Ship from, Ship to, Color trailColor, int shuttles)
        {
            float seconds = from.Position.Distance(to.Position) / (LoadShuttleSpeed * MeanFlightPart);
            Queue(new(Route.Load, null, to, from, Vector3.Zero, trailColor, shuttles, seconds, rising: true, size: LoadShuttleSize));
        }

        void Queue(in Request request)
        {
            lock (Requests)
                Requests.Add(request);
        }

        internal int InFlight(Planet planet, bool rising)
        {
            int count = 0;
            for (int i = 0; i < Flying.Count; ++i)
                if (Flying[i].Planet == planet && Flying[i].Rising == rising)
                    ++count;
            return count;
        }

        internal int InFlight(Planet planet)
        {
            int count = 0;
            for (int i = 0; i < Flying.Count; ++i)
                if (Flying[i].Planet == planet)
                    ++count;
            return count;
        }

        internal int InFlight(Ship from, Ship to)
        {
            int count = 0;
            for (int i = 0; i < Flying.Count; ++i)
                if (Flying[i].FromShip == from && Flying[i].Ship == to)
                    ++count;
            return count;
        }

        // launched from or to the ship at their high end, past their delay
        internal int Launched(Ship ship)
        {
            int count = 0;
            for (int i = 0; i < Flying.Count; ++i)
                if (Flying[i].Ship == ship && Flying[i].Age > Flying[i].Delay)
                    ++count;
            return count;
        }

        // flying towards the ship at their high end, as a salvage platform's shuttles coming back from the hull
        internal int InFlightTowards(Ship ship)
        {
            int count = 0;
            for (int i = 0; i < Flying.Count; ++i)
                if (Flying[i].Ship == ship && Flying[i].Rising)
                    ++count;
            return count;
        }

        public void Update(UniverseScreen screen, FixedSimTime timeStep)
        {
            TakeRequests();
            if (Flying.Count == 0)
                return;

            ParticleManager particles = screen.Particles;
            bool closeEnough = particles != null && screen.UState.IsPlanetViewOrCloser;
            for (int i = Flying.Count - 1; i >= 0; --i)
            {
                Shuttle shuttle = Flying[i];
                bool launches = shuttle.Age <= shuttle.Delay;
                shuttle.Age += timeStep.FixedTime;
                float progress = (shuttle.Age - shuttle.Delay) / shuttle.Flight;
                if (progress < 0f)
                    continue;

                if (launches && shuttle.Launcher is { Active: false }) // a shuttle whose ship is gone before it launches never launches
                {
                    Flying.RemoveAtSwapLast(i);
                    continue;
                }

                bool visible = closeEnough && shuttle.IsInFrustum(screen, progress);
                if (visible && launches)
                    particles.Flash.AddParticle(shuttle.PositionAt(0f), Vector3.Zero, FlashScale, Color.White);

                if (progress >= 1f || float.IsNaN(progress))
                {
                    if (shuttle.ComesBack)
                    {
                        shuttle.ComesBack = false;
                        shuttle.Launcher = null; // turning back is no launch
                        shuttle.Rising = !shuttle.Rising;
                        shuttle.Age = shuttle.Delay;
                        continue;
                    }

                    if (visible)
                        particles.Flash.AddParticle(shuttle.PositionAt(1f), Vector3.Zero, FlashScale, Color.White);
                    Flying.RemoveAtSwapLast(i);
                }
                else if (visible)
                {
                    Vector3 position = shuttle.PositionAt(progress);
                    particles.CargoShuttleTrail.AddParticle(position, Vector3.Zero, TrailScale * shuttle.Size, shuttle.TrailColor);
                    particles.CargoShuttle.AddParticle(position, Vector3.Zero, GlowScale * shuttle.Size, Color.White);
                }
            }
        }

        void TakeRequests()
        {
            lock (Requests)
            {
                for (int i = 0; i < Requests.Count; ++i)
                    Launch(Requests[i]);
                Requests.Clear();
            }
        }

        void Launch(in Request request)
        {
            Planet planet = request.Planet;
            for (int i = 0; i < request.Count; ++i)
            {
                Shuttle shuttle = new()
                {
                    Planet = planet,
                    TrailColor = request.TrailColor,
                    Bend = Random.Float(-MaxBend, MaxBend),
                    Flight = Random.Float(MinFlightPart, MaxFlightPart) * request.Seconds,
                    Rising = request.Rising,
                    Size = request.Size,
                };

                switch (request.Route)
                {
                    case Route.Trade:
                        shuttle.Low = (Random.Direction2D() * planet.Radius).ToVec3(planet.Position3D.Z - SurfaceLift);
                        shuttle.High = PortPoint();
                        shuttle.Delay = Random.Float(0f, LaunchPart) * request.Seconds;
                        break;
                    case Route.PortToShip:
                        shuttle.Low = PortPoint();
                        shuttle.Ship = request.Ship;
                        shuttle.ComesBack = true;
                        shuttle.Delay = Random.Float(0f, LaunchPart) * request.Seconds;
                        break;
                    case Route.Drop:
                        Vector2 from = request.From.ToVec2() - planet.Position;
                        shuttle.Low = SurfaceBelow(planet, from + Vector2.Zero.GenerateRandomPointInsideCircle(DropSpread, Random));
                        shuttle.High = from.ToVec3(request.From.Z);
                        shuttle.Delay = Random.Float(0f, DropLaunchPart) * request.Seconds;
                        break;
                    case Route.ShipToShip:
                    case Route.Load:
                        shuttle.FromShip = shuttle.Launcher = request.FromShip;
                        shuttle.Ship = request.Ship;
                        shuttle.Delay = Random.Float(0f, LaunchPart) * request.Seconds;
                        shuttle.SeenWhereItFlies = request.Route == Route.Load;
                        break;
                    case Route.ShipToHull:
                        Vector2 onHull = request.From.ToVec2().GenerateRandomPointInsideCircle(request.Spread, Random);
                        shuttle.Low = onHull.ToVec3(request.From.Z);
                        shuttle.Ship = shuttle.Launcher = request.Ship;
                        shuttle.ComesBack = true;
                        shuttle.Delay = Random.Float(MinHullLaunchDelay, MaxHullLaunchDelay);
                        break;
                }

                Flying.Add(shuttle);
            }
        }

        Vector3 PortPoint() => Vector2.Zero.GenerateRandomPointInsideCircle(PortSpread, Random).ToVec3(SpaceStation.PosZ);

        static Vector3 SurfaceBelow(Planet planet, Vector2 offset)
        {
            float radius = planet.Radius;
            float maxDistance = radius * MaxDropPart;
            float distance = offset.Length();
            if (distance > maxDistance)
            {
                offset *= maxDistance / distance;
                distance = maxDistance;
            }

            float height = (float)Math.Sqrt(radius * radius - distance * distance);
            return offset.ToVec3(planet.Position3D.Z - height - SurfaceLift);
        }
    }
}
