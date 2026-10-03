using SDGraphics;
using SDUtils;
using Ship_Game.ExtensionMethods;
using Ship_Game.Graphics.Particles;
using Ship_Game.Utils;
using Color = Microsoft.Xna.Framework.Color;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game
{
    /// <summary>
    /// Small cargo shuttles flying up from a planet to its space port while a freighter lands there to trade,
    /// and back down as it takes off. They are only a visual effect: nothing is saved and nothing waits for them.
    /// </summary>
    public sealed class CargoShuttles
    {
        public const float CargoPerShuttle = 20f;
        public const int MaxShuttles = 20;
        const float LaunchPart = 0.35f;
        const float MinFlightPart = 0.42f;
        const float MaxFlightPart = 0.6f;
        const float MaxBend = 0.25f;
        const float SurfaceLift = 30f;
        const float PortSpread = 50f;
        const float FlashScale = 0.3f;

        readonly struct Request
        {
            public readonly Planet Planet;
            public readonly Color TrailColor;
            public readonly int Count;
            public readonly float Seconds;
            public readonly bool ToPort;

            public Request(Planet planet, Color trailColor, int count, float seconds, bool toPort)
            {
                Planet = planet;
                TrailColor = trailColor;
                Count = count;
                Seconds = seconds;
                ToPort = toPort;
            }
        }

        sealed class Shuttle
        {
            public Planet Planet;
            public Color TrailColor;
            public Vector3 Surface;
            public Vector3 Port;
            public float Bend;
            public float Delay;
            public float Flight;
            public float Age;
            public bool ToPort;

            public Vector3 PositionAt(float progress)
            {
                float p = ToPort ? progress : 1f - progress;
                float across = p * p * (3f - 2f * p);
                float up = 1f - (1f - p) * (1f - p);
                Vector2 surface = Surface.ToVec2();
                Vector2 port = Port.ToVec2();
                Vector2 side = (port - surface).LeftVector() * (Bend * RadMath.Sin(p * RadMath.PI));
                Vector2 offset = surface.LerpTo(port, across) + side;
                return (Planet.Position + offset).ToVec3(Surface.Z.LerpTo(Port.Z, up));
            }
        }

        readonly Array<Request> Requests = new();
        readonly Array<Shuttle> Flying = new();
        readonly RandomBase Random = new SeededRandom();

        public static int ShuttlesFor(float cargo) => ((int)(cargo / CargoPerShuttle)).Clamped(1, MaxShuttles);

        public void Send(Planet planet, Color trailColor, int shuttles, float seconds, bool toPort)
        {
            lock (Requests)
                Requests.Add(new(planet, trailColor, shuttles, seconds, toPort));
        }

        internal int InFlight(Planet planet, bool toPort)
        {
            int count = 0;
            for (int i = 0; i < Flying.Count; ++i)
                if (Flying[i].Planet == planet && Flying[i].ToPort == toPort)
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
            float radius = planet.Radius;
            for (int i = 0; i < request.Count; ++i)
            {
                Flying.Add(new()
                {
                    Planet = planet,
                    TrailColor = request.TrailColor,
                    Surface = (Random.Direction2D() * radius).ToVec3(planet.Position3D.Z - SurfaceLift),
                    Port = Vector2.Zero.GenerateRandomPointInsideCircle(PortSpread, Random).ToVec3(SpaceStation.PosZ),
                    Bend = Random.Float(-MaxBend, MaxBend),
                    Delay = Random.Float(0f, LaunchPart) * request.Seconds,
                    Flight = Random.Float(MinFlightPart, MaxFlightPart) * request.Seconds,
                    ToPort = request.ToPort,
                });
            }
        }
    }
}
