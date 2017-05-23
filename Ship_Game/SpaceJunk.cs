using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ship_Game
{
    public sealed class SpaceJunk
    {
        public SceneObject So;
        public Vector3 Position;
        private Vector3 RotationRadians;
        private Vector3 Velocity;
        private Vector3 Spin;
        private float Scale    = 1f;
        private float Duration = 8f;
        private ParticleEmitter TrailEmitter;

        public SpaceJunk()
        {
        }

        public SpaceJunk(Vector2 pos, GameplayObject source, float spawnRadius)
        {
            float radius = spawnRadius + 25f;
            Position.X = RandomMath2.RandomBetween(pos.X - radius, pos.X + radius);
            Position.Y = RandomMath2.RandomBetween(pos.Y - radius, pos.Y + radius);
            Position.Z = RandomMath2.RandomBetween(-radius*0.5f, radius*0.5f);

            CreateSceneObject(pos);

            Velocity.X += source.Velocity.X;
            Velocity.Y += source.Velocity.Y;
            //Log.Info("SpaceJunk vx={0} vy={1} v={2}", Xvel, Yvel, new Vector2(Xvel,Yvel).Length());

        }

        private void RandomValues(Vector2 center, float velMin, float velMax, float spinMin, float spinMax, float scaleMin, float scaleMax)
        {
            Vector2 fromCenterToSpawnPos = new Vector2(Position.X-center.X, Position.Y-center.Y);
            Velocity = RandomMath.Vector3D(velMin, velMax);
            Velocity.X *= fromCenterToSpawnPos.X * 0.033f;
            Velocity.Y *= fromCenterToSpawnPos.Y * 0.033f;

            Spin  = RandomMath.Vector3D(spinMin, spinMax);
            Scale = RandomMath2.RandomBetween(scaleMin, scaleMax);
        }

        private void CreateSceneObject(Vector2 center)
        {
            RotationRadians = RandomMath.Vector3D(0.01f, 1.02f);
            
            Duration = RandomMath2.RandomBetween(Duration, Duration*2);
            int random = RandomMath2.InRange(ResourceManager.NumJunkModels);
            switch (random)
            {
                case 6:
                    RandomValues(center, -2.5f, 2.5f, 0.01f, 0.5f, 0.3f, 0.8f);
                    break;
                case 7:
                    RandomValues(center, -2.5f, 2.5f, 0.01f, 0.5f, 0.3f, 0.8f);
                    TrailEmitter = Empire.Universe.fireParticles.NewEmitter(200f, Position);
                    break;
                case 8:
                    RandomValues(center, -5f, 5f, 0.5f, 3.5f, 0.7f, 0.1f);
                    TrailEmitter = Empire.Universe.projectileTrailParticles.NewEmitter(200f, Position);
                    break;
                case 11:
                    RandomValues(center, -5f, 5f, 0.5f, 3.5f, 0.3f, 0.8f);
                    TrailEmitter = Empire.Universe.fireTrailParticles.NewEmitter(200f, Position);
                    break;
                case 12:
                    RandomValues(center, -3f, 3f, 0.01f, 0.5f, 0.3f, 0.8f);
                    break;
                case 13:
                    RandomValues(center, -2.5f, 2.5f, 0.01f, 0.5f, 0.3f, 0.8f);
                    break;
                default:
                    RandomValues(center, -2f, 2f, 0.01f, 1.02f, 0.5f, 1f);
                    TrailEmitter = Empire.Universe.fireTrailParticles.NewEmitter(200f, Position);
                    break;
            }

            ModelMesh mesh = ResourceManager.GetJunkModel(random).Meshes[0];
            So = new SceneObject(mesh)
            {
                ObjectType = ObjectType.Dynamic,
                Visibility = ObjectVisibility.Rendered,
                World = Matrix.CreateTranslation(-1000000f, -1000000f, 0f)
            };
        }

        private static readonly Array<SpaceJunk> EmptyList = new Array<SpaceJunk>();

        public static void SpawnJunk(int howMuchJunk, Vector2 position, SolarSystem s, 
                                     GameplayObject source, float spawnRadius = 1.0f, float scaleMod = 1.0f)
        {
            if (UniverseScreen.JunkList.Count > 800)
                return;

            // generate junk before locking
            var junk = new SpaceJunk[howMuchJunk];
            for (int i = 0; i < howMuchJunk; i++)
            {
                SpaceJunk newJunk = new SpaceJunk(position, source, spawnRadius);
                newJunk.Scale *= scaleMod;
                junk[i] = newJunk;
            }

            // now lock and add to scene
            foreach (var j in junk) Empire.Universe.AddObject(j.So);
            UniverseScreen.JunkList.AddRange(junk);
        }

        public void Update(float elapsedTime)
        {
            Duration -= elapsedTime;
            if (Duration <= 0f)
            {
                RemoveFromScene();
                return;
            }

            if (Empire.Universe.viewState > UniverseScreen.UnivScreenState.SystemView 
                || !Empire.Universe.Frustum.Contains(Position, 10f))
                return;
            Position        += Velocity;
            RotationRadians += Spin * elapsedTime;
            So.AffineTransform(Position, RotationRadians, Scale);

            TrailEmitter?.Update(elapsedTime, Position);
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
            So.Clear();
            So = null;
            TrailEmitter = null;
        }
    }
}