using SDGraphics;
using Ship_Game.Graphics.Particles;
using Ship_Game.Universe;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game
{
    public sealed class SpaceJunk
    {
        readonly UniverseState Universe;
        public SceneObject So;
        public Vector3 Position;
        Vector3 RotationRadians;
        Vector3 Velocity;
        Vector3 Spin;
        float Scale;
        float Duration;
        float MaxDuration;
        ParticleEmitter FlameTrail;
        ParticleEmitter ProjTrail;
        int ConstructionPartId;
        Planet TetherPlanet;        // anchors construction junk to a moving planet
        Vector2 TetherPointOffset;  // build point relative to planet center (stable)
        Vector3 ConstructionOffset; // junk position relative to tether point (grows during fade)
        bool IsFading;              // true once TryReset fires; render scale shrinks with remaining Duration
        const float ConstructionPartDuration = 10;
        const float FadeOutwardRate = 0.2f; // ~e^(0.2*5) = 2.7x outward growth over the 5s fade

        public SpaceJunk()
        {
        }

        public SpaceJunk(UniverseState universe, Vector2 parentPos, Vector2 parentVel,
                         float maxSize, bool ignite, int constructorId, Planet tetherPlanet, float parentZ = 0f)
        {
            Universe = universe;
            float spawnInRadius = maxSize + 25f;
            Position.X = Universe.Random.Float(parentPos.X - spawnInRadius, parentPos.X + spawnInRadius);
            Position.Y = Universe.Random.Float(parentPos.Y - spawnInRadius, parentPos.Y + spawnInRadius);
            Position.Z = parentZ + Universe.Random.Float(-spawnInRadius*0.5f, spawnInRadius*0.5f);
            if (constructorId > 0)
            {
                if (tetherPlanet != null)
                {
                    TetherPlanet = tetherPlanet;
                    // Build point relative to planet center: stable, follows planet orbit
                    TetherPointOffset = new Vector2(parentPos.X - tetherPlanet.Position.X,
                                                    parentPos.Y - tetherPlanet.Position.Y);
                    // Junk's spread around the build point: this is what grows during fade
                    ConstructionOffset = new Vector3(Position.X - parentPos.X,
                                                     Position.Y - parentPos.Y,
                                                     Position.Z);
                }
                CreateSceneObjectConstruction(parentPos, maxSize, constructorId);
            }
            else
            {
                CreateSceneObject(universe.Screen.Particles, parentPos, maxSize, ignite);
            }

            // inherit extra velocity from parent
            Velocity.X += parentVel.X;
            Velocity.Y += parentVel.Y;
        }

        void RandomValues(Vector2 pos, Range vel, Range spin, float scale, float scaleRandom)
        {
            var offsetFromParent = new Vector3(Position.X - pos.X, Position.Y - pos.Y, 1f);
            Velocity = Universe.Random.Vector3D(vel.Min, vel.Max) * offsetFromParent;
            Spin  = Universe.Random.Vector3D(spin.Min, spin.Max);
            Scale = Universe.Random.Float(scaleRandom*scale, scale);
        }

        static Range Range(float min, float max) => new Range(min, max);

        void CreateSceneObject(ParticleManager particles, Vector2 pos, float maxSize, bool ignite)
        {
            RotationRadians = Universe.Random.Vector3D(0.01f, 1.02f);
            MaxDuration = Universe.Random.Float(4f, 8f);
            Duration = MaxDuration;

            float flameParticles = 0f;
            float trailParticles = 0f;

            int junkIndex = Universe.Random.InRange(ResourceManager.NumJunkModels);
            var model = ResourceManager.GetJunkModel(junkIndex);
            float meshDiameter = 2f * ResourceManager.GetJunkModelRadius(junkIndex);

            // set lower bound to max size, otherwise we can't even see the junk
            float maxAllowedSize = maxSize.LowerBound(8f);
            float scale = (maxAllowedSize / meshDiameter);

            switch (junkIndex)
            {
                case 6: // meshRadius = 9.5
                    RandomValues(pos, vel:Range(-2.5f, 2.5f), spin:Range(0.01f, 0.5f), scale: scale, scaleRandom: 0.5f);
                    break;
                case 7: // meshRadius = 23.21
                    RandomValues(pos, vel:Range(-2.5f, 2.5f), spin:Range(0.01f, 0.5f), scale: scale, scaleRandom: 0.3f);
                    trailParticles = 60f;
                    if (ignite) flameParticles = 15f;
                    break;
                case 8: // meshRadius = 32.18
                    RandomValues(pos, vel:Range(-5f, 5f), spin:Range(0.5f, 3.5f), scale: scale, scaleRandom: 0.5f);
                    trailParticles = 60f;
                    if (ignite) flameParticles = 15f;
                    break;
                case 11: // meshRadius = 63.89
                    RandomValues(pos, vel:Range(-5f, 5f), spin:Range(0.5f, 3.5f), scale: scale, scaleRandom: 0.5f);
                    if (ignite) flameParticles = 15f;
                    break;
                case 12: // meshRadius = 39.29
                    RandomValues(pos, vel:Range(-3f, 3f), spin:Range(0.01f, 0.5f), scale: scale, scaleRandom: 0.3f);
                    break;
                default:
                    // [0] meshRadius = 9.5
                    // [1] meshRadius = 24.86
                    // [2] meshRadius = 43.50
                    // [3] meshRadius = 54.39
                    // [4] meshRadius = 53.57
                    // [5] meshRadius = 18.24
                    // -- 6 -- 7 -- 8 --
                    // [9] meshRadius = 48.21
                    // [10] meshRadius = 50.47
                    // -- 11 -- 12 --
                    RandomValues(pos, vel:Range(-2f, 2f), spin:Range(0.01f, 1.02f), scale: scale, scaleRandom: 0.5f);
                    if (ignite) flameParticles = 20f;
                    break;
            }

            if (trailParticles > 0f)
                ProjTrail = particles.ProjectileTrail.NewEmitter(trailParticles * Scale, Position, scale: Scale);

            if (flameParticles > 0f)
                FlameTrail = particles.Flame.NewEmitter(flameParticles * Scale, Position, scale: Scale * 0.5f);

            So = model.CreateSceneObject();
            So.Visibility = ObjectVisibility.Rendered;
            So.AffineTransform(Position, RotationRadians, Scale);
        }

        void CreateSceneObjectConstruction(Vector2 pos, float maxSize, int constructorId)
        {
            ConstructionPartId = constructorId;
            Duration = MaxDuration = ConstructionPartDuration;

            int junkIndex = Universe.Random.InRange(ResourceManager.NumJunkModels);
            var model = ResourceManager.GetJunkModel(junkIndex);
            float meshDiameter = 2f * ResourceManager.GetJunkModelRadius(junkIndex);

            // set lower bound to max size, otherwise we can't even see the junk
            float maxAllowedSize = maxSize.LowerBound(8f);
            float scale = (maxAllowedSize / meshDiameter);
            Scale = Universe.Random.Float(0.5f * scale, scale);
            So = model.CreateSceneObject();
            So.Visibility = ObjectVisibility.Rendered;
            So.AffineTransform(Position, RotationRadians, Scale);
        }


        public static void ConstructionPart(UniverseState universe, Vector2 position, Vector2 velocity,
            GameObject source, float maxSize, Planet tetherPlanet) => SpawnJunk(universe, 1, position, velocity, source, maxSize,
                                                    ignite: false, constructorId: source.Id, tetherPlanet: tetherPlanet);

        // a part of a build over something that is no game object, as a recovered derelict: at its depth, kept with the constructor's parts; the caller checks it is on screen
        public static void ConstructionPart(UniverseState universe, Vector3 position, int constructorId, float maxSize)
        {
            if (!CanSpawn(universe))
                return;
            var junk = new SpaceJunk(universe, position.ToVec2(), Vector2.Zero, maxSize, ignite: false, constructorId, tetherPlanet: null, parentZ: position.Z);
            universe.Screen.AddObject(junk.So);
            universe.JunkList.Add(junk);
        }

        /**
         * @param spawnRadius Spawned junk is spread around the given radius
         * @param scaleMod Applies additional scale modifier on the spawned junk
         */
        public static void SpawnJunk(UniverseState universe, int howMuchJunk, Vector2 position, Vector2 velocity,
                                     GameObject source, float maxSize, bool ignite, int constructorId = 0, Planet tetherPlanet = null)
        {
            if (universe == null)
            {
                Log.Error($"SpawnJunk {howMuchJunk} failed: {source}");
                return; // we can't spawn junk while loading the game :'/
            }

            if (!CanSpawn(universe))
                return;

            if (!source.IsInFrustum(universe.Screen))
                return; // not visible on the screen, so lets forget about it :)

            AddJunk(universe, howMuchJunk, position, velocity, maxSize, ignite, constructorId, tetherPlanet);
        }

        // a small piece pulled off something that is no game object, as a stripped derelict: at its depth, at just this velocity; the caller checks it is on screen
        public static void SpawnJunk(UniverseState universe, Vector3 position, Vector2 velocity, float maxSize)
        {
            if (!CanSpawn(universe))
                return;
            var junk = new SpaceJunk(universe, position.ToVec2(), Vector2.Zero, maxSize, ignite: false, constructorId: 0, tetherPlanet: null, parentZ: position.Z);
            junk.Velocity = new Vector3(velocity, 0f);
            universe.Screen.AddObject(junk.So);
            universe.JunkList.Add(junk);
        }

        static bool CanSpawn(UniverseState universe)
        {
            if (universe.JunkList.Count > 1000)
                return false; // don't allow too much junk

            return ResourceManager.NumJunkModels > 0; // junk models are not loaded in Unit Tests
        }

        static void AddJunk(UniverseState universe, int howMuchJunk, Vector2 position, Vector2 velocity,
                            float maxSize, bool ignite, int constructorId, Planet tetherPlanet)
        {
            var junk = new SpaceJunk[howMuchJunk];
            for (int i = 0; i < howMuchJunk; i++)
            {
                junk[i] = new SpaceJunk(universe, position, velocity, maxSize, ignite, constructorId, tetherPlanet);
            }

            // now add to scene
            foreach (SpaceJunk j in junk)
                universe.Screen.AddObject(j.So);
            universe.JunkList.AddRange(junk);
        }

        public void TryReset(int id)
        {
            if (id == ConstructionPartId)
            {
                lock (this)
                {
                    Duration = MaxDuration = ConstructionPartDuration * 0.5f;
                    IsFading = true;
                }
            }
        }

        public void Update(FixedSimTime timeStep)
        {
            Duration -= timeStep.FixedTime;
            if (Duration <= 0f || !Universe.Screen.IsActive)
            {
                RemoveFromScene();
                return;
            }
     
            if (!Universe.IsSystemViewOrCloser ||
                !Universe.Screen.IsInFrustum(Position, 10f))
                return;

            if (TetherPlanet != null)
            {
                if (IsFading)
                {
                    // Push outward from the tether point (build position), not planet center.
                    // Exponential so rate scales with current distance and the cluster visibly
                    // expands rather than rigidly translating.
                    float growth = 1f + (FadeOutwardRate * timeStep.FixedTime);
                    ConstructionOffset.X *= growth;
                    ConstructionOffset.Y *= growth;
                }
                Position.X = TetherPlanet.Position.X + TetherPointOffset.X + ConstructionOffset.X;
                Position.Y = TetherPlanet.Position.Y + TetherPointOffset.Y + ConstructionOffset.Y;
                Position.Z = ConstructionOffset.Z;
            }
            else
            {
                Position += Velocity * timeStep.FixedTime;
            }
            RotationRadians += Spin * timeStep.FixedTime;
            float renderScale = IsFading ? Scale * (Duration / MaxDuration) : Scale;
            So.AffineTransform(Position, RotationRadians, renderScale);

            FlameTrail?.Update(timeStep.FixedTime, Position);
            ProjTrail?.Update(timeStep.FixedTime, Position);

            //if (Universe.Debug)
            //{
            //    Universe.DebugWin?.DrawText(Position.ToVec2(), DebugText, Color.Red);
            //}
        }

        public void RemoveFromScene()
        {
            Universe.JunkList.Remove(this);
            RemoveSceneObject();
        }

        // Not synchronized, lock it yourself if needed
        public void RemoveSceneObject()
        {
            if (So != null)
            {
                Universe.Screen.RemoveObject(So);
                So = null;
            }
            FlameTrail = null;
            ProjTrail = null;
        }
    }
}