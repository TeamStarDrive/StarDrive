using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public sealed class SpaceJunk
    {
        public SceneObject So;
        public Vector3 Position;
        private Vector3 RotationRadians;
        private Vector3 Velocity;
        private Vector3 Spin;
        private readonly float ScaleMod;
        private float Scale    = 1f;
        private float Duration = 8f;
        private float MaxDuration;
        private ParticleEmitter FlameTrail;
        private ParticleEmitter ProjTrail;
        private ParticleEmitter StaticSmoke;
        private readonly bool UseStaticSmoke; // Leaving for now. I may wire this in later to turn off some effects. 

        public SpaceJunk()
        {
        }

        public SpaceJunk(Vector2 parentPos, Vector2 parentVel, float spawnRadius, float scaleMod, bool useStaticSmoke, bool ignite = true)
        {
            float radius = spawnRadius + 25f;
            ScaleMod = scaleMod;                        
            UseStaticSmoke = useStaticSmoke;
            Position.X = RandomMath2.RandomBetween(parentPos.X - radius, parentPos.X + radius);
            Position.Y = RandomMath2.RandomBetween(parentPos.Y - radius, parentPos.Y + radius);
            Position.Z = RandomMath2.RandomBetween(-radius*0.5f, radius*0.5f);
            CreateSceneObject(parentPos, ignite);

            // inherit extra velocity from parent
            Velocity.X += parentVel.X;
            Velocity.Y += parentVel.Y;
        }

        void RandomValues(Vector2 parentPos, float velMin, float velMax, float spinMin, float spinMax, float scaleMin, float scaleMax)
        {
            var offsetFromParent = new Vector3(Position.X - parentPos.X, Position.Y - parentPos.Y, 1f);
            Velocity = RandomMath.Vector3D(velMin, velMax) * offsetFromParent;
            Spin  = RandomMath.Vector3D(spinMin, spinMax);
            Scale = RandomMath2.RandomBetween(scaleMin, scaleMax) * ScaleMod;
        }

        void CreateSceneObject(Vector2 parentPos, bool ignite)
        {
            RotationRadians  = RandomMath.Vector3D(0.01f, 1.02f);
            Duration         = RandomMath2.RandomBetween(0, Duration * 1f) * Scale;
            MaxDuration      = Duration;
            int random       = RandomMath2.InRange(ResourceManager.NumJunkModels);
            float flameParts = 0;

            switch (random)
            {
                case 6:
                    RandomValues(parentPos, -2.5f, 2.5f, 0.01f, 0.5f, 0.5f, 1f);
                    ignite = false;
                    break;
                case 7:
                    RandomValues(parentPos, -2.5f, 2.5f, 0.01f, 0.5f, 0.3f, 0.8f);
                    flameParts = 500f;
                    ProjTrail  = Empire.Universe.projectileTrailParticles.NewEmitter(200f, Position);
                    break;
                case 8:
                    RandomValues(parentPos, -5f, 5f, 0.5f, 3.5f, 0.7f, 0.1f);
                    flameParts = 30f;
                    ProjTrail  = Empire.Universe.projectileTrailParticles.NewEmitter(200f * Scale, Position);
                    break;
                case 11:
                    RandomValues(parentPos, -5f, 5f, 0.5f, 3.5f, 0.5f, 0.8f);
                    flameParts = 200f;
                    break;
                case 12:
                    RandomValues(parentPos, -3f, 3f, 0.01f, 0.5f, 0.3f, 0.8f);
                    ignite = false;
                    break;
                case 13:
                    RandomValues(parentPos, -2.5f, 2.5f, 0.01f, 0.5f, 0.3f, 0.8f);
                    ignite = false;
                    break;
                default:
                    RandomValues(parentPos, -2f, 2f, 0.01f, 1.02f, 0.5f, 2f);
                    flameParts = 30f;
                    break;
            }

            if (ignite)
                FlameTrail = Empire.Universe.flameParticles.NewEmitter(flameParts * Scale, Position);

            // special Emitter that will degrade faster than the others and doesnt move from the original spawn locaton. 
            if (UseStaticSmoke)
                StaticSmoke = Empire.Universe.smokePlumeParticles.NewEmitter(60 * Scale, Position);

            ModelMesh mesh = ResourceManager.GetJunkModel(random).Meshes[0];
            So = new SceneObject(mesh)
            {
                ObjectType = ObjectType.Dynamic,
                Visibility = ObjectVisibility.Rendered,
                World = Matrix.CreateTranslation(-1000000f, -1000000f, 0f)
            };
        }

        /**
         * @param spawnRadius Spawned junk is spread around the given radius
         * @param scaleMod Applies additional scale modifier on the spawned junk
         */
        public static void SpawnJunk(int howMuchJunk, Vector2 position, Vector2 velocity,
                                     GameplayObject source, float spawnRadius = 1.0f, float scaleMod = 1.0f,
                                     bool staticSmoke = false, bool ignite = true)
        {
            if (Empire.Universe == null)
            {
                Log.Error($"SpawnJunk {howMuchJunk} failed: {source}");
                return; // we can't spawn junk while loading the game :'/
            }

            if (UniverseScreen.JunkList.Count > 800)
                return; // don't allow too much junk

            if (!source.IsInFrustum)
                return; // not visible on the screen, so lets forget about it :)

            var junk = new SpaceJunk[howMuchJunk];
            for (int i = 0; i < howMuchJunk; i++)
            {
                junk[i] = new SpaceJunk(position, velocity, spawnRadius, scaleMod, staticSmoke, ignite);
            }

            // now lock and add to scene
            foreach (SpaceJunk j in junk) Empire.Universe.AddObject(j.So);
            UniverseScreen.JunkList.AddRange(junk);
        }

        public void Update(FixedSimTime timeStep)
        {
            Duration -= timeStep.FixedTime;
            if (Duration <= 0f || !Empire.Universe.IsActive)
            {
                RemoveFromScene();
                return;
            }
     
            if (!Empire.Universe.IsSystemViewOrCloser 
                || !Empire.Universe.Frustum.Contains(Position, 10f))
                return;

            Position        += Velocity * timeStep.FixedTime;
            RotationRadians += Spin * timeStep.FixedTime;
            So.AffineTransform(Position, RotationRadians, Scale);

            FlameTrail?.Update(timeStep.FixedTime, Position);
            ProjTrail?.Update(timeStep.FixedTime, Position);

            if (UseStaticSmoke && (Duration / MaxDuration) > 0.9f)
                StaticSmoke.Update(timeStep.FixedTime);

        }

        public void RemoveFromScene()
        {
            UniverseScreen.JunkList.QueuePendingRemoval(this);
            DestroySceneObject();
        }

        // Not synchronized, lock it yourself if needed
        public void DestroySceneObject()
        {
            Empire.Universe.RemoveObject(So);
            So = null;
            FlameTrail = null;
            ProjTrail = null;
            StaticSmoke = null;
        }
    }
}