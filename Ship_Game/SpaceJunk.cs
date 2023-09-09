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
        const float ConstructionPartDuration = 10;

        public SpaceJunk()
        {
        }

        public SpaceJunk(UniverseState universe, Vector2 parentPos, Vector2 parentVel,
                         float maxSize, bool ignite, int constructorId)
        {
            Universe = universe;
            float spawnInRadius = maxSize + 25f;
            Position.X = Universe.Random.Float(parentPos.X - spawnInRadius, parentPos.X + spawnInRadius);
            Position.Y = Universe.Random.Float(parentPos.Y - spawnInRadius, parentPos.Y + spawnInRadius);
            Position.Z = Universe.Random.Float(-spawnInRadius*0.5f, spawnInRadius*0.5f);
            if (constructorId > 0)
                CreateSceneObjectConstruction(parentPos, maxSize, constructorId);
            else
                CreateSceneObject(universe.Screen.Particles, parentPos, maxSize, ignite);

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
            So.AffineTransform(Position, RotationRadians, Scale);
        }


        public static void ConstructionPart(UniverseState universe, Vector2 position, Vector2 velocity,
            GameObject source, float maxSize) => SpawnJunk(universe, 1, position, velocity, source, maxSize, 
                                                    ignite: false, constructorId: source.Id);
        
        /**
         * @param spawnRadius Spawned junk is spread around the given radius
         * @param scaleMod Applies additional scale modifier on the spawned junk
         */
        public static void SpawnJunk(UniverseState universe, int howMuchJunk, Vector2 position, Vector2 velocity,
                                     GameObject source, float maxSize, bool ignite, int constructorId = 0)
        {
            if (universe == null)
            {
                Log.Error($"SpawnJunk {howMuchJunk} failed: {source}");
                return; // we can't spawn junk while loading the game :'/
            }

            if (universe.JunkList.Count > 800)
                return; // don't allow too much junk

            if (!source.IsInFrustum(universe.Screen))
                return; // not visible on the screen, so lets forget about it :)

            var junk = new SpaceJunk[howMuchJunk];
            for (int i = 0; i < howMuchJunk; i++)
            {
                junk[i] = new SpaceJunk(universe, position, velocity, maxSize, ignite, constructorId);
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
                    Duration -= ConstructionPartDuration - 1;
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

            Position += Velocity * timeStep.FixedTime;
            RotationRadians += Spin * timeStep.FixedTime;
            So.AffineTransform(Position, RotationRadians, Scale);

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