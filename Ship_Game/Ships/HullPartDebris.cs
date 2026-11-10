using System;
using SDGraphics;
using Ship_Game.Graphics.Particles;
using Ship_Game.Universe;
using Ship_Game.Utils;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

namespace Ship_Game.Ships
{
    // a hull part blown off a ship, flying off as its model drew it: it drifts out turning flat and sinking, burns, smokes and flashes all its flight, blasts a few times and breaks into junk that sinks on
    public sealed class HullPartDebris : SpaceJunk
    {
        // its flight, after which it breaks into junk with a blast
        public const float MinSeconds = 8f;
        public const float MaxSeconds = 12f;
        // its speed outward from the ship's middle, on top of what it keeps of the ship's velocity
        public const float MinSpeed = 15f;
        public const float MaxSpeed = 30f;
        // the blast takes this share of the ship's velocity off it and throws it outward faster by this share of the ship's speed
        public const float BlastShare = 0.2f;
        // its secondary blasts, at random times in its flight
        public const int MinBlasts = 1;
        public const int MaxBlasts = 3;
        // the junk it breaks into
        public const int MinPieces = 3;
        public const int MaxPieces = 5;
        // it turns flat at up to this many radians a second and tilts no more than this, so its top stays toward the camera
        public const float MaxTurnSpeed = 0.6f;
        public const float MaxTilt = 0.35f;
        // it sinks away from the camera at this speed all its flight, and so does the junk it breaks into
        public const float SinkSpeed = 50f;
        // thick smoke rises from this many random points on its surface
        public const int NumPlumes = 2;
        // it flashes all its flight, without a break, from one random point on its surface for each this much of its radius, from MinFlashPoints to MaxFlashPoints
        public const float FlashPointRadius = 40f;
        public const int MinFlashPoints = 2;
        public const int MaxFlashPoints = 5;
        // a flash on a part, flying off or under repair, is this share of its radius across, a quarter of its width, within these sizes
        public const float FlashShare = 0.5f;
        public const float MinFlashSize = 15f;
        public const float MaxFlashSize = 60f;
        // its blasts at its break off and its break-up, each this share of its radius
        public const float BreakOffBlastShare = 0.6f;
        public const float BreakUpBlastShare = 0.9f;
        const float BreakUpLight = 4f;
        const float FireParticles = 20f;
        const float SmokeParticles = 16f;
        const float PlumeParticles = 12f;
        const float FlashParticles = 10f; // one every 0.1 s, before the last is gone (0.15 s at its shortest), few enough to leave the shared Flash particles to the other blasts
        const float FlashParticleSize = 200f; // the Flash particle's size in Particles.yaml

        readonly XnaMatrix BrokeOff; // the ship's model as drawn when the part broke off
        readonly XnaVector3 Center;  // the part's center in the model, which it turns about
        readonly Vector3 StartPos;
        public readonly Vector2 Drift;
        readonly float Radius;
        readonly float TurnSpeed, TiltX, TiltY, WobbleX, WobbleY;
        public readonly float Lifetime;
        public float Age { get; private set; }
        int BlastsLeft;
        float NextBlast;
        readonly ParticleEmitter Fire;
        readonly ParticleEmitter Smoke;
        readonly ParticleEmitter[] Plumes, Flashes;
        readonly XnaVector3[] PlumePoints, FlashPoints; // in the model, turning with it
        bool WasHidden; // its emitters stayed where it was last shown

        // the points a part of this radius flashes from
        public static int FlashPointsFor(float radius)
            => ((int)MathF.Round(radius / FlashPointRadius, MidpointRounding.AwayFromZero)).Clamped(MinFlashPoints, MaxFlashPoints);

        // the Flash particle's scale for a flash on a part of this radius
        public static float FlashScale(float radius) => (radius * FlashShare).Clamped(MinFlashSize, MaxFlashSize) / FlashParticleSize;

        // the part flies off the ship's model as drawn now, with a blast where it was
        public static void ThrowOff(Ship ship, SceneObject model, int part)
        {
            UniverseState universe = ship.Universe;
            if (universe.Screen == null || universe.JunkList.Count > MaxJunk
                || !ship.BaseHull.GetPartBounds(part, out XnaVector3 center, out float radius, out int numParts))
                return;
            Add(universe, new HullPartDebris(universe, ship, model, part, numParts, center, radius));
        }

        HullPartDebris(UniverseState universe, Ship ship, SceneObject model, int part, int numParts, XnaVector3 center, float radius) : base(universe)
        {
            RandomBase random = universe.Random;
            BrokeOff = model.World;
            Center = center;
            XnaVector3 start = XnaVector3.Transform(center, BrokeOff);
            StartPos = new Vector3(start.X, start.Y, start.Z);
            Position = StartPos;
            Radius = radius * new XnaVector3(BrokeOff.M11, BrokeOff.M12, BrokeOff.M13).Length(); // a crashing ship's model is scaled

            Vector2 outward = new Vector2(start.X, start.Y) - ship.Position;
            outward = outward.Length() > 0.01f ? outward.Normalized() : random.Direction2D();
            Drift = ship.Velocity * (1f - BlastShare) + outward * (random.Float(MinSpeed, MaxSpeed) + BlastShare * ship.Velocity.Length());
            TurnSpeed = random.Float(-MaxTurnSpeed, MaxTurnSpeed);
            TiltX = random.Float(-MaxTilt, MaxTilt);
            TiltY = random.Float(-MaxTilt, MaxTilt);
            WobbleX = random.Float(0.5f, 1.2f);
            WobbleY = random.Float(0.5f, 1.2f);
            Lifetime = random.Float(MinSeconds, MaxSeconds);
            BlastsLeft = random.Int(MinBlasts, MaxBlasts);
            NextBlast = random.Float(0.5f, 3f);

            So = model.CopyMeshes();
            So.Visibility = GlobalStats.ShipVisibility;
            So.EmissiveScale = 0f; // torn off its power
            var shown = new float[numParts];
            shown[part] = 1f;
            So.PartShown = shown;

            ParticleManager particles = universe.Screen.Particles;
            if (particles != null)
            {
                float size = (Radius / 50f).Clamped(0.5f, 3f);
                Fire = particles.Fire.NewEmitter(FireParticles, StartPos, size);
                Smoke = particles.JunkSmoke.NewEmitter(SmokeParticles, StartPos, size);
                particles.Flash.AddParticle(StartPos);

                Plumes = EmittersOnPart(ship, part, random, particles.SmokePlume, NumPlumes, PlumeParticles, size, out PlumePoints);
                Flashes = EmittersOnPart(ship, part, random, particles.Flash, FlashPointsFor(Radius), FlashParticles, FlashScale(Radius), out FlashPoints);
            }
            ExplosionManager.AddExplosion(universe.Screen, StartPos, Drift, Radius * BreakOffBlastShare, 2.5f, ExplosionType.Projectile);
        }

        // emitters at random points on the part's surface, its center where it has none
        ParticleEmitter[] EmittersOnPart(Ship ship, int part, RandomBase random, IParticle particle, int count, float perSecond, float scale, out XnaVector3[] points)
        {
            points = new XnaVector3[count];
            var emitters = new ParticleEmitter[count];
            for (int i = 0; i < count; ++i)
            {
                points[i] = ship.BaseHull.RandomPointOnPart(part, random, out XnaVector3 point, out float _) ? point : Center;
                XnaVector3 at = XnaVector3.Transform(points[i], BrokeOff);
                emitters[i] = particle.NewEmitter(perSecond, new Vector3(at.X, at.Y, at.Z), scale);
            }
            return emitters;
        }

        // each emitter at its point on the part as its model is placed now
        void UpdateEmittersOnPart(ParticleEmitter[] emitters, XnaVector3[] points, float dt)
        {
            if (emitters == null)
                return;
            for (int i = 0; i < emitters.Length; ++i)
            {
                XnaVector3 at = XnaVector3.Transform(points[i], So.World);
                emitters[i].Update(dt, new Vector3(at.X, at.Y, at.Z));
            }
        }

        public override void Update(FixedSimTime timeStep)
        {
            float dt = timeStep.FixedTime;
            Age += dt;
            if (!Universe.Screen.IsActive)
            {
                RemoveFromScene();
                return;
            }
            Vector2 moved = Drift * Age;
            float sunk = SinkSpeed * Age;
            Position = new Vector3(StartPos.X + moved.X, StartPos.Y + moved.Y, StartPos.Z + sunk);
            bool shown = Universe.IsSystemViewOrCloser && Universe.Screen.IsInFrustum(Position, Radius);
            if (Age >= Lifetime)
            {
                if (shown)
                    BreakUp();
                RemoveFromScene();
                return;
            }
            if (!shown)
            {
                So.Visibility = ObjectVisibility.None; // not left hanging where it was last placed
                WasHidden = true;
                return;
            }

            XnaMatrix turn = XnaMatrix.CreateRotationZ(TurnSpeed * Age)
                           * XnaMatrix.CreateRotationY(TiltY * MathF.Sin(WobbleY * Age))
                           * XnaMatrix.CreateRotationX(TiltX * MathF.Sin(WobbleX * Age));
            So.World = XnaMatrix.CreateTranslation(-Center) * turn * XnaMatrix.CreateTranslation(Center)
                     * BrokeOff * XnaMatrix.CreateTranslation(moved.X, moved.Y, sunk);
            So.Visibility = GlobalStats.ShipVisibility;

            float emitting = WasHidden ? 0f : dt; // back in view, its emitters move to it without throwing puffs across the gap
            WasHidden = false;
            Fire?.Update(emitting, Position);
            Smoke?.Update(emitting, Position);
            UpdateEmittersOnPart(Plumes, PlumePoints, emitting);
            UpdateEmittersOnPart(Flashes, FlashPoints, emitting);

            if (BlastsLeft > 0 && (NextBlast -= dt) <= 0f)
            {
                --BlastsLeft;
                NextBlast = Universe.Random.Float(0.5f, 3f);
                Vector3 at = Position + Universe.Random.Vector2D(Radius * 0.5f).ToVec3();
                Universe.Screen.Particles?.Flash.AddParticle(at);
                ExplosionManager.AddExplosion(Universe.Screen, at, Drift, Radius * 0.4f, 2.5f, ExplosionType.Projectile);
            }
        }

        // a blast bigger than its break off's, and the few pieces of junk it leaves, flying on from it
        void BreakUp()
        {
            Universe.Screen.Particles?.Flash.AddParticle(Position);
            ExplosionManager.AddExplosion(Universe.Screen, Position, Drift, Radius * BreakUpBlastShare, BreakUpLight, ExplosionType.Projectile);
            RandomBase random = Universe.Random;
            int pieces = random.Int(MinPieces, MaxPieces);
            for (int i = 0; i < pieces; ++i)
            {
                Vector3 at = Position + random.Vector2D(Radius * 0.5f).ToVec3();
                SpawnJunk(Universe, at, Drift + random.Direction2D() * random.Float(20f, 60f), Radius * 0.25f, SinkSpeed);
            }
        }
    }
}
