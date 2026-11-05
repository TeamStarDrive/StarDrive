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
    // a hull part blown off a ship, flying off as its model drew it: it drifts out turning flat, burns and smokes, blasts a few times and breaks into junk
    public sealed class HullPartDebris : SpaceJunk
    {
        // its flight, after which it breaks into junk with a flash
        public const float MinSeconds = 8f;
        public const float MaxSeconds = 12f;
        // it burns this long, and smokes all its flight
        public const float FireSeconds = 3f;
        // its speed outward from the ship, on top of the ship's velocity
        public const float MinSpeed = 15f;
        public const float MaxSpeed = 30f;
        // its secondary blasts, at random times in its flight
        public const int MinBlasts = 1;
        public const int MaxBlasts = 3;
        // the junk it breaks into
        public const int MinPieces = 3;
        public const int MaxPieces = 5;
        // it turns flat at up to this many radians a second and tilts no more than this, so its top stays toward the camera
        public const float MaxTurnSpeed = 0.6f;
        public const float MaxTilt = 0.35f;
        const float FireParticles = 20f;
        const float SmokeParticles = 8f;

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
        ParticleEmitter Fire;
        readonly ParticleEmitter Smoke;

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
            Drift = ship.Velocity + outward * random.Float(MinSpeed, MaxSpeed);
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
            }
            ExplosionManager.AddExplosion(universe.Screen, StartPos, Drift, Radius * 0.6f, 2.5f, ExplosionType.Projectile);
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
            Position = new Vector3(StartPos.X + moved.X, StartPos.Y + moved.Y, StartPos.Z);
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
                return;
            }

            XnaMatrix turn = XnaMatrix.CreateRotationZ(TurnSpeed * Age)
                           * XnaMatrix.CreateRotationY(TiltY * MathF.Sin(WobbleY * Age))
                           * XnaMatrix.CreateRotationX(TiltX * MathF.Sin(WobbleX * Age));
            So.World = XnaMatrix.CreateTranslation(-Center) * turn * XnaMatrix.CreateTranslation(Center)
                     * BrokeOff * XnaMatrix.CreateTranslation(moved.X, moved.Y, 0f);
            So.Visibility = GlobalStats.ShipVisibility;

            if (Age < FireSeconds)
                Fire?.Update(dt, Position);
            else
                Fire = null;
            Smoke?.Update(dt, Position);

            if (BlastsLeft > 0 && (NextBlast -= dt) <= 0f)
            {
                --BlastsLeft;
                NextBlast = Universe.Random.Float(0.5f, 3f);
                Vector3 at = Position + Universe.Random.Vector2D(Radius * 0.5f).ToVec3();
                Universe.Screen.Particles?.Flash.AddParticle(at);
                ExplosionManager.AddExplosion(Universe.Screen, at, Drift, Radius * 0.4f, 2.5f, ExplosionType.Projectile);
            }
        }

        // a flash, and the few pieces of junk it leaves, flying on from it
        void BreakUp()
        {
            Universe.Screen.Particles?.Flash.AddParticle(Position);
            RandomBase random = Universe.Random;
            int pieces = random.Int(MinPieces, MaxPieces);
            for (int i = 0; i < pieces; ++i)
            {
                Vector3 at = Position + random.Vector2D(Radius * 0.5f).ToVec3();
                SpawnJunk(Universe, at, Drift + random.Direction2D() * random.Float(20f, 60f), Radius * 0.25f);
            }
        }
    }
}
