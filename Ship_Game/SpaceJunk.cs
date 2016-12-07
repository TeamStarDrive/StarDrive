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
		public SceneObject JunkSO;
		public Vector3 Position;

		private float Xrotate;
		private float Yrotate;
		private float Zrotate;
		private float Xvel;
		private float Yvel;
		private float Zvel;
		private float Spinx;
		private float Spiny;
		private float Spinz;
		private float Scale = 1f;
		private float Duration = 8f;

		//public SolarSystem system;
		public static UniverseScreen universeScreen;
		public ParticleEmitter trailEmitter;
		public float zPos;
		public bool wasAddedToScene;

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

            Xvel += source.Velocity.X;
            Yvel += source.Velocity.Y;
            //System.Diagnostics.Debug.WriteLine("SpaceJunk vx={0} vy={1} v={2}", Xvel, Yvel, new Vector2(Xvel,Yvel).Length());

		}

        private void RandomRotate(float rotateMin, float rotateMax)
        {
			Xrotate = RandomMath2.RandomBetween(rotateMin, rotateMax);
			Yrotate = RandomMath2.RandomBetween(rotateMin, rotateMax);
			Zrotate = RandomMath2.RandomBetween(rotateMin, rotateMax);
        }

        private void RandomValues(Vector2 center, float velMin, float velMax, float spinMin, float spinMax, float scaleMin, float scaleMax)
        {
            Vector2 fromCenterToSpawnPos = new Vector2(Position.X-center.X, Position.Y-center.Y);
            Xvel = RandomMath2.RandomBetween(velMin, velMax) * fromCenterToSpawnPos.X * 0.033f;
			Yvel = RandomMath2.RandomBetween(velMin, velMax) * fromCenterToSpawnPos.Y * 0.033f;
			Zvel = RandomMath2.RandomBetween(velMin, velMax);

            Spinx = RandomMath2.RandomBetween(spinMin, spinMax);
			Spiny = RandomMath2.RandomBetween(spinMin, spinMax);
			Spinz = RandomMath2.RandomBetween(spinMin, spinMax);

			Scale = RandomMath2.RandomBetween(scaleMin, scaleMax);
        }

		private void CreateSceneObject(Vector2 center)
		{
            RandomRotate(0.01f, 1.02f);
            
			Duration = RandomMath2.RandomBetween(Duration, Duration*2);
			int random = RandomMath2.InRange(ResourceManager.NumJunkModels);
			switch (random)
			{
				case 6:
                    RandomValues(center, -2.5f, 2.5f, 0.01f, 0.5f, 0.3f, 0.8f);
					break;
				case 7:
                    RandomValues(center, -2.5f, 2.5f, 0.01f, 0.5f, 0.3f, 0.8f);
					trailEmitter = new ParticleEmitter(universeScreen.fireParticles, 200f, Position);
					break;
				case 8:
                    RandomValues(center, -5f, 5f, 0.5f, 3.5f, 0.7f, 0.1f);
					trailEmitter = new ParticleEmitter(universeScreen.projectileTrailParticles, 200f, Position);
					break;
				case 11:
                    RandomValues(center, -5f, 5f, 0.5f, 3.5f, 0.3f, 0.8f);
					trailEmitter = new ParticleEmitter(universeScreen.fireTrailParticles, 200f, Position);
					break;
				case 12:
                    RandomValues(center, -3f, 3f, 0.01f, 0.5f, 0.3f, 0.8f);
					break;
				case 13:
                    RandomValues(center, -2.5f, 2.5f, 0.01f, 0.5f, 0.3f, 0.8f);
					break;
                default:
                    RandomValues(center, -2f, 2f, 0.01f, 1.02f, 0.5f, 1f);
            	    trailEmitter = new ParticleEmitter(universeScreen.fireTrailParticles, 200f, Position);
                    break;
			}

            ModelMesh mesh = ResourceManager.GetJunkModel(random).Meshes[0];
			JunkSO = new SceneObject(mesh)
			{
				ObjectType = ObjectType.Dynamic,
				Visibility = ObjectVisibility.Rendered,
				World = Matrix.CreateTranslation(-1000000f, -1000000f, 0f)
			};
		}

        private static readonly List<SpaceJunk> EmptyList = new List<SpaceJunk>();

		public static List<SpaceJunk> MakeJunk(int howMuchJunk, Vector2 position, SolarSystem s, 
                                               GameplayObject source, float spawnRadius = 1.0f, float scaleMod = 1.0f)
		{
			if (UniverseScreen.JunkList.Count > 800)
				return EmptyList;

			var junkList = new List<SpaceJunk>(howMuchJunk);
			for (int i = 0; i < howMuchJunk; i++)
			{
				SpaceJunk newJunk = new SpaceJunk(position, source, spawnRadius);
                newJunk.Scale *= scaleMod;
				junkList.Add(newJunk);
			}
			return junkList;
		}

		public void Update(float elapsedTime)
		{
			Duration -= elapsedTime;
			if (Duration > 0f)
			{
				Position.X += Xvel;
				Position.Y += Yvel;
				Position.Z += Zvel;
			    trailEmitter?.Update(elapsedTime, Position);
				Xrotate += Spinx * elapsedTime;
				Zrotate += Spiny * elapsedTime;
				Yrotate += Spinz * elapsedTime;
				JunkSO.World = ((((Matrix.Identity 
                    * Matrix.CreateScale(Scale)) 
                    * Matrix.CreateRotationZ(Zrotate)) 
                    * Matrix.CreateRotationX(Xrotate)) 
                    * Matrix.CreateRotationY(Yrotate)) 
                    * Matrix.CreateTranslation(Position);
			}
			else if (wasAddedToScene)
			{
				UniverseScreen.JunkList.QueuePendingRemoval(this);
				lock (GlobalStats.ObjectManagerLocker)
				{
					universeScreen.ScreenManager.inter.ObjectManager.Remove(JunkSO);
				}
				JunkSO.Clear();
			}
		}
	}
}