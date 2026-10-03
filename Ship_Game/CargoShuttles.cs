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
    /// and back down as it takes off; from a space port to a troop ship unloading onto the colony, and back; and down
    /// from an assault shuttle dropping its troop. They are only a visual effect: nothing is saved and nothing waits for them.
    /// </summary>
    public sealed class CargoShuttles
    {
        public const float CargoPerShuttle = 20f;
        public const int MaxShuttles = 20;
        public const int TroopShuttles = 3;
        public const float TroopShuttleSeconds = 5f;
        const float LaunchPart = 0.35f;
        const float DropLaunchPart = 0.1f;
        const float MinFlightPart = 0.42f;
        const float MaxFlightPart = 0.6f;
        const float MaxBend = 0.25f;
        const float SurfaceLift = 30f;
        const float PortSpread = 50f;
        const float DropSpread = 30f;
        const float MaxDropPart = 0.9f;
        const float FlashScale = 0.3f;

        enum Route
        {
            Trade,
            PortToShip,
            Drop
        }

        readonly struct Request
        {
            public readonly Route Route;
            public readonly Planet Planet;
            public readonly Ship Ship;
            public readonly Vector3 From;
            public readonly Color TrailColor;
            public readonly int Count;
            public readonly float Seconds;
            public readonly bool Rising;

            public Request(Route route, Planet planet, Ship ship, Vector3 from, Color trailColor, int count, float seconds, bool rising)
            {
                Route = route;
                Planet = planet;
                Ship = ship;
                From = from;
                TrailColor = trailColor;
                Count = count;
                Seconds = seconds;
                Rising = rising;
            }
        }

        sealed class Shuttle
        {
            public Planet Planet;
            public Ship Ship;
            public Color TrailColor;
            public Vector3 Low;
            public Vector3 High;
            public float Bend;
            public float Delay;
            public float Flight;
            public float Age;
            public bool Rising;
            public bool ComesBack;

            Vector3 HighEnd => Ship != null ? (Ship.Position - Planet.Position).ToVec3(0f) : High;

            public Vector3 PositionAt(float progress)
            {
                float p = Rising ? progress : 1f - progress;
                float across = p * p * (3f - 2f * p);
                float up = 1f - (1f - p) * (1f - p);
                Vector3 high = HighEnd;
                Vector2 low2 = Low.ToVec2();
                Vector2 high2 = high.ToVec2();
                Vector2 side = (high2 - low2).LeftVector() * (Bend * RadMath.Sin(p * RadMath.PI));
                Vector2 offset = low2.LerpTo(high2, across) + side;
                return (Planet.Position + offset).ToVec3(Low.Z.LerpTo(high.Z, up));
            }
        }

        readonly Array<Request> Requests = new();
        readonly Array<Shuttle> Flying = new();
        readonly RandomBase Random = new SeededRandom();

        public static int ShuttlesFor(float cargo) => ((int)(cargo / CargoPerShuttle)).Clamped(1, MaxShuttles);

        public void Send(Planet planet, Color trailColor, int shuttles, float seconds, bool toPort)
            => Queue(new(Route.Trade, planet, null, Vector3.Zero, trailColor, shuttles, seconds, rising: toPort));

        public void SendToShip(Planet planet, Ship ship, Color trailColor, int shuttles, float seconds)
            => Queue(new(Route.PortToShip, planet, ship, Vector3.Zero, trailColor, shuttles, seconds, rising: true));

        public void SendDown(Planet planet, Vector3 from, Color trailColor, int shuttles, float seconds)
            => Queue(new(Route.Drop, planet, null, from, trailColor, shuttles, seconds, rising: false));

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

                bool visible = closeEnough && shuttle.Planet.InFrustum;
                if (visible && launches)
                    particles.Flash.AddParticle(shuttle.PositionAt(0f), Vector3.Zero, FlashScale, Color.White);

                if (progress >= 1f || float.IsNaN(progress))
                {
                    if (shuttle.ComesBack)
                    {
                        shuttle.ComesBack = false;
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
                    particles.CargoShuttleTrail.AddParticle(position, Vector3.Zero, 1f, shuttle.TrailColor);
                    particles.CargoShuttle.AddParticle(position, Vector3.Zero, 1f, Color.White);
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
