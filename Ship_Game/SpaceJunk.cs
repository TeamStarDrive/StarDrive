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
	public class SpaceJunk
	{
		public SceneObject JunkSO;

		public Vector3 Position;

		public static ContentManager contentManager;

		private float Xrotate;

		private float Yrotate;

		private float Zrotate;

		private float Xvel;

		private float Yvel;

		private float Zvel;

		private float spinx;

		private float spiny;

		private float spinz;

		private float Duration = 5f;

		public SolarSystem system;

		public static UniverseScreen universeScreen;

		private Vector2 initialVel = new Vector2();

		public bool wasAddedToScene;

		private float scale = 1f;

		public ParticleEmitter trailEmitter;

		public float zPos;

		public SpaceJunk()
		{
		}

		public SpaceJunk(Vector2 Position)
		{
			this.Position.X = RandomMath2.RandomBetween(Position.X - 20f, Position.X + 20f);
			this.Position.Y = RandomMath2.RandomBetween(Position.Y - 20f, Position.Y + 20f);
			this.Position.Z = RandomMath2.RandomBetween(-20f, 20f);
		}

		public SpaceJunk(Vector2 Position, GameplayObject source)
		{
			this.Position.X = RandomMath2.RandomBetween(Position.X - 20f, Position.X + 20f);
			this.Position.Y = RandomMath2.RandomBetween(Position.Y - 20f, Position.Y + 20f);
			this.Position.Z = RandomMath2.RandomBetween(-20f, 20f);
			this.initialVel = source.Velocity;
		}

		public void LoadContent(ContentManager Content)
		{
			Model junk;
			ModelMesh mesh;
			SpaceJunk spaceJunk;
			this.spinx = RandomMath2.RandomBetween(0.01f, 1.02f);
			this.spiny = RandomMath2.RandomBetween(0.01f, 1.02f);
			this.spinz = RandomMath2.RandomBetween(0.01f, 1.02f);
			this.Xrotate = RandomMath2.RandomBetween(0.01f, 1.02f);
			this.Yrotate = RandomMath2.RandomBetween(0.01f, 1.02f);
			this.Zrotate = RandomMath2.RandomBetween(0.01f, 1.02f);
			this.scale = RandomMath2.RandomBetween(0.5f, 1f);
			int random = (int)RandomMath2.RandomBetween(1f, 13f);
			this.Xvel = RandomMath2.RandomBetween(-2f, 2f);
			this.Yvel = RandomMath2.RandomBetween(-2f, 2f);
			this.Zvel = RandomMath2.RandomBetween(-2f, 2f);
			switch (random)
			{
				case 6:
				{
					this.Xvel = RandomMath2.RandomBetween(-2.5f, 2.5f);
					this.Yvel = RandomMath2.RandomBetween(-2.5f, 2.5f);
					this.spinx = RandomMath2.RandomBetween(0.01f, 0.5f);
					this.spiny = RandomMath2.RandomBetween(0.01f, 0.5f);
					this.spinz = RandomMath2.RandomBetween(0.01f, 0.5f);
					this.scale = RandomMath2.RandomBetween(0.3f, 0.8f);
					spaceJunk = this;
					spaceJunk.scale = spaceJunk.scale * 0.5f;
					junk = Ship_Game.ResourceManager.JunkModels[random];
					mesh = junk.Meshes[0];
					this.JunkSO = new SceneObject(mesh)
					{
						ObjectType = ObjectType.Dynamic,
						Visibility = ObjectVisibility.Rendered,
						World = Matrix.CreateTranslation(-1000000f, -1000000f, 0f)
					};
					return;
				}
				case 7:
				{
					this.Xvel = RandomMath2.RandomBetween(-2.5f, 2.5f);
					this.Yvel = RandomMath2.RandomBetween(-2.5f, 2.5f);
					this.spinx = RandomMath2.RandomBetween(0.01f, 0.5f);
					this.spiny = RandomMath2.RandomBetween(0.01f, 0.5f);
					this.spinz = RandomMath2.RandomBetween(0.01f, 0.5f);
					this.scale = RandomMath2.RandomBetween(0.3f, 0.8f);
					this.trailEmitter = new ParticleEmitter(SpaceJunk.universeScreen.fireParticles, 200f, this.Position);
					spaceJunk = this;
					spaceJunk.scale = spaceJunk.scale * 0.5f;
					junk = Ship_Game.ResourceManager.JunkModels[random];
					mesh = junk.Meshes[0];
					this.JunkSO = new SceneObject(mesh)
					{
						ObjectType = ObjectType.Dynamic,
						Visibility = ObjectVisibility.Rendered,
						World = Matrix.CreateTranslation(-1000000f, -1000000f, 0f)
					};
					return;
				}
				case 8:
				{
					this.Xvel = RandomMath2.RandomBetween(-5f, 5f);
					this.Yvel = RandomMath2.RandomBetween(-5f, 5f);
					this.Zvel = RandomMath2.RandomBetween(-5f, 5f);
					this.spinx = RandomMath2.RandomBetween(0.5f, 3.5f);
					this.spiny = RandomMath2.RandomBetween(0.5f, 3.5f);
					this.spinz = RandomMath2.RandomBetween(0.5f, 3.5f);
					this.scale = RandomMath2.RandomBetween(0.7f, 1f);
					this.Duration = 10f;
					this.trailEmitter = new ParticleEmitter(SpaceJunk.universeScreen.projectileTrailParticles, 200f, this.Position);
					spaceJunk = this;
					spaceJunk.scale = spaceJunk.scale * 0.5f;
					junk = Ship_Game.ResourceManager.JunkModels[random];
					mesh = junk.Meshes[0];
					this.JunkSO = new SceneObject(mesh)
					{
						ObjectType = ObjectType.Dynamic,
						Visibility = ObjectVisibility.Rendered,
						World = Matrix.CreateTranslation(-1000000f, -1000000f, 0f)
					};
					return;
				}
				case 9:
				case 10:
				{
					spaceJunk = this;
					spaceJunk.scale = spaceJunk.scale * 0.5f;
					junk = Ship_Game.ResourceManager.JunkModels[random];
					mesh = junk.Meshes[0];
					this.JunkSO = new SceneObject(mesh)
					{
						ObjectType = ObjectType.Dynamic,
						Visibility = ObjectVisibility.Rendered,
						World = Matrix.CreateTranslation(-1000000f, -1000000f, 0f)
					};
					return;
				}
				case 11:
				{
					this.Xvel = RandomMath2.RandomBetween(-5f, 5f);
					this.Yvel = RandomMath2.RandomBetween(-5f, 5f);
					this.Zvel = RandomMath2.RandomBetween(-5f, 5f);
					this.spinx = RandomMath2.RandomBetween(0.5f, 3.5f);
					this.spiny = RandomMath2.RandomBetween(0.5f, 3.5f);
					this.spinz = RandomMath2.RandomBetween(0.5f, 3.5f);
					this.scale = RandomMath2.RandomBetween(0.3f, 0.8f);
					this.Duration = 10f;
					this.trailEmitter = new ParticleEmitter(SpaceJunk.universeScreen.fireTrailParticles, 200f, this.Position);
					spaceJunk = this;
					spaceJunk.scale = spaceJunk.scale * 0.5f;
					junk = Ship_Game.ResourceManager.JunkModels[random];
					mesh = junk.Meshes[0];
					this.JunkSO = new SceneObject(mesh)
					{
						ObjectType = ObjectType.Dynamic,
						Visibility = ObjectVisibility.Rendered,
						World = Matrix.CreateTranslation(-1000000f, -1000000f, 0f)
					};
					return;
				}
				case 12:
				{
					this.Xvel = RandomMath2.RandomBetween(-2.5f, 2.5f);
					this.Yvel = RandomMath2.RandomBetween(-2.5f, 2.5f);
					this.Zvel = RandomMath2.RandomBetween(-5f, 5f);
					this.spinx = RandomMath2.RandomBetween(0.01f, 0.5f);
					this.spiny = RandomMath2.RandomBetween(0.01f, 0.5f);
					this.spinz = RandomMath2.RandomBetween(0.01f, 0.5f);
					this.scale = RandomMath2.RandomBetween(0.3f, 0.8f);
					this.trailEmitter = new ParticleEmitter(SpaceJunk.universeScreen.projectileTrailParticles, 200f, this.Position);
					spaceJunk = this;
					spaceJunk.scale = spaceJunk.scale * 0.5f;
					junk = Ship_Game.ResourceManager.JunkModels[random];
					mesh = junk.Meshes[0];
					this.JunkSO = new SceneObject(mesh)
					{
						ObjectType = ObjectType.Dynamic,
						Visibility = ObjectVisibility.Rendered,
						World = Matrix.CreateTranslation(-1000000f, -1000000f, 0f)
					};
					return;
				}
				case 13:
				{
					this.Xvel = RandomMath2.RandomBetween(-2.5f, 2.5f);
					this.Yvel = RandomMath2.RandomBetween(-2.5f, 2.5f);
					this.spinx = RandomMath2.RandomBetween(0.01f, 0.5f);
					this.spiny = RandomMath2.RandomBetween(0.01f, 0.5f);
					this.spinz = RandomMath2.RandomBetween(0.01f, 0.5f);
					this.scale = RandomMath2.RandomBetween(0.3f, 0.8f);
					spaceJunk = this;
					spaceJunk.scale = spaceJunk.scale * 0.5f;
					junk = Ship_Game.ResourceManager.JunkModels[random];
					mesh = junk.Meshes[0];
					this.JunkSO = new SceneObject(mesh)
					{
						ObjectType = ObjectType.Dynamic,
						Visibility = ObjectVisibility.Rendered,
						World = Matrix.CreateTranslation(-1000000f, -1000000f, 0f)
					};
					return;
				}
				default:
				{
					spaceJunk = this;
					spaceJunk.scale = spaceJunk.scale * 0.5f;
					junk = Ship_Game.ResourceManager.JunkModels[random];
					mesh = junk.Meshes[0];
					this.JunkSO = new SceneObject(mesh)
					{
						ObjectType = ObjectType.Dynamic,
						Visibility = ObjectVisibility.Rendered,
						World = Matrix.CreateTranslation(-1000000f, -1000000f, 0f)
					};
					return;
				}
			}
		}

		public static List<SpaceJunk> MakeJunk(int howMuchJunk, Vector2 Position, SolarSystem s)
		{
			List<SpaceJunk> JunkList = new List<SpaceJunk>();
			if (UniverseScreen.JunkList.Count > 200)
			{
				return JunkList;
			}
			for (int i = 0; i < howMuchJunk; i++)
			{
				SpaceJunk newJunk = new SpaceJunk(Position)
				{
					system = s
				};
				newJunk.LoadContent(SpaceJunk.contentManager);
				JunkList.Add(newJunk);
			}
			return JunkList;
		}

		public static List<SpaceJunk> MakeJunk(int howMuchJunk, Vector2 Position, SolarSystem s, GameplayObject source)
		{
			List<SpaceJunk> JunkList = new List<SpaceJunk>();
			for (int i = 0; i < howMuchJunk; i++)
			{
				SpaceJunk newJunk = new SpaceJunk(Position, source)
				{
					system = s
				};
				newJunk.LoadContent(SpaceJunk.contentManager);
				JunkList.Add(newJunk);
			}
			return JunkList;
		}

		public void Update(float elapsedTime)
		{
			SpaceJunk duration = this;
			duration.Duration = duration.Duration - elapsedTime;
			if (this.Duration > 0f)
			{
				this.Position.X = this.Position.X + this.Xvel;
				this.Position.Y = this.Position.Y + this.Yvel;
				this.Position.Z = this.Position.Z + this.Zvel;
				this.Position.X = this.Position.X + this.initialVel.X;
				this.Position.Y = this.Position.Y + this.initialVel.Y;
				if (this.trailEmitter != null)
				{
					this.trailEmitter.Update(elapsedTime, this.Position);
				}
				SpaceJunk xrotate = this;
				xrotate.Xrotate = xrotate.Xrotate + this.spinx * elapsedTime;
				SpaceJunk zrotate = this;
				zrotate.Zrotate = zrotate.Zrotate + this.spiny * elapsedTime;
				SpaceJunk yrotate = this;
				yrotate.Yrotate = yrotate.Yrotate + this.spinz * elapsedTime;
				this.JunkSO.World = ((((Matrix.Identity * Matrix.CreateScale(this.scale)) * Matrix.CreateRotationZ(this.Zrotate)) * Matrix.CreateRotationX(this.Xrotate)) * Matrix.CreateRotationY(this.Yrotate)) * Matrix.CreateTranslation(this.Position);
			}
			else if (this.wasAddedToScene)
			{
				UniverseScreen.JunkList.QueuePendingRemoval(this);
				lock (GlobalStats.ObjectManagerLocker)
				{
					SpaceJunk.universeScreen.ScreenManager.inter.ObjectManager.Remove(this.JunkSO);
				}
				this.JunkSO.Clear();
				return;
			}
		}
	}
}