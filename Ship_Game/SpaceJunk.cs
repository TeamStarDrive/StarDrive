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
		private float Duration = 5f;
		private Vector2 InitialVel;

		public SolarSystem system;
		public static UniverseScreen universeScreen;
		public ParticleEmitter trailEmitter;
		public float zPos;
		public bool wasAddedToScene;

		public SpaceJunk()
		{
		}

		public SpaceJunk(Vector2 pos)
		{
			Position.X = RandomMath2.RandomBetween(pos.X - 20f, pos.X + 20f);
			Position.Y = RandomMath2.RandomBetween(pos.Y - 20f, pos.Y + 20f);
			Position.Z = RandomMath2.RandomBetween(-20f, 20f);
            LoadContent();
		}

		public SpaceJunk(Vector2 pos, GameplayObject source)
		{
			Position.X = RandomMath2.RandomBetween(pos.X - 20f, pos.X + 20f);
			Position.Y = RandomMath2.RandomBetween(pos.Y - 20f, pos.Y + 20f);
			Position.Z = RandomMath2.RandomBetween(-20f, 20f);
			InitialVel = source.Velocity;
            LoadContent();
		}

        private void SetSpaceJunk(int random)
        {
			ModelMesh mesh = ResourceManager.GetJunkModel(random).Meshes[0];
			JunkSO = new SceneObject(mesh)
			{
				ObjectType = ObjectType.Dynamic,
				Visibility = ObjectVisibility.Rendered,
				World = Matrix.CreateTranslation(-1000000f, -1000000f, 0f)
			};
        }

        private void RandomSpin(float spinMin, float spinMax)
        {
            Spinx = RandomMath2.RandomBetween(spinMin, spinMax);
			Spiny = RandomMath2.RandomBetween(spinMin, spinMax);
			Spinz = RandomMath2.RandomBetween(spinMin, spinMax);
        }
        private void RandomRotate(float rotateMin, float rotateMax)
        {
			Xrotate = RandomMath2.RandomBetween(rotateMin, rotateMax);
			Yrotate = RandomMath2.RandomBetween(rotateMin, rotateMax);
			Zrotate = RandomMath2.RandomBetween(rotateMin, rotateMax);
        }
        private void RandomVelocity(float velMin, float velMax)
        {
            Xvel = RandomMath2.RandomBetween(velMin, velMax);
			Yvel = RandomMath2.RandomBetween(velMin, velMax);
			Zvel = RandomMath2.RandomBetween(velMin, velMax);
        }
        private void RandomScale(float scaleMin, float scaleMax)
        {
			Scale = RandomMath2.RandomBetween(scaleMin, scaleMax);
        }
        private void RandomValues(float velMin, float velMax, float spinMin, float spinMax, float scaleMin, float scaleMax)
        {
            RandomVelocity(velMin, velMax);
            RandomSpin(spinMin, spinMax);
			RandomScale(scaleMin, scaleMax);
        }

		public void LoadContent()
		{
            RandomRotate(0.01f, 1.02f);
            RandomValues(-2f, 2f, 0.01f, 1.02f, 0.5f, 1f);

			int random = (int)RandomMath2.RandomBetween(0, ResourceManager.NumJunkModels);
			switch (random)
			{
				case 6:
                    RandomValues(-2.5f, 2.5f, 0.01f, 0.5f, 0.3f, 0.8f);
					break;
				case 7:
                    RandomValues(-2.5f, 2.5f, 0.01f, 0.5f, 0.3f, 0.8f);
					trailEmitter = new ParticleEmitter(universeScreen.fireParticles, 200f, Position);
					break;
				case 8:
					Duration = 10f;
                    RandomValues(-5f, 5f, 0.5f, 3.5f, 0.7f, 0.1f);
					trailEmitter = new ParticleEmitter(universeScreen.projectileTrailParticles, 200f, Position);
					break;
				case 11:
					Duration = 10f;
                    RandomValues(-5f, 5f, 0.5f, 3.5f, 0.3f, 0.8f);
					trailEmitter = new ParticleEmitter(universeScreen.fireTrailParticles, 200f, Position);
					break;
				case 12:
                    RandomValues(-3f, 3f, 0.01f, 0.5f, 0.3f, 0.8f);
					break;
				case 13:
                    RandomValues(-2.5f, 2.5f, 0.01f, 0.5f, 0.3f, 0.8f);
					break;
                default:
            	    trailEmitter = new ParticleEmitter(universeScreen.fireTrailParticles, 200f, Position);
                    break;
			}
			SetSpaceJunk(random);
		}

        private static readonly List<SpaceJunk> EmptyList = new List<SpaceJunk>();

		public static List<SpaceJunk> MakeJunk(int howMuchJunk, Vector2 position, SolarSystem s, float scaleMod = 1.0f)
		{
			if (UniverseScreen.JunkList.Count > 200)
				return EmptyList;

			var junkList = new List<SpaceJunk>(howMuchJunk);
			for (int i = 0; i < howMuchJunk; i++)
			{
				SpaceJunk newJunk = new SpaceJunk(position)
				{
					system = s
				};
				newJunk.LoadContent();
                newJunk.Scale *= scaleMod;
				junkList.Add(newJunk);
			}
			return junkList;
		}

		public static List<SpaceJunk> MakeJunk(int howMuchJunk, Vector2 position, SolarSystem s, GameplayObject source)
		{
			var junkList = new List<SpaceJunk>(howMuchJunk);
			for (int i = 0; i < howMuchJunk; i++)
			{
				SpaceJunk newJunk = new SpaceJunk(position, source)
				{
					system = s
				};
				newJunk.LoadContent();
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
				Position.X += InitialVel.X;
				Position.Y += InitialVel.Y;
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