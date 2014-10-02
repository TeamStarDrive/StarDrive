using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Ship_Game;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;

namespace Ship_Game.Gameplay
{
	public class Asteroid : GameplayObject
	{
		public Vector3 Position3D;

		public float scale;

		public float spinx;

		public float spiny;

		public float spinz;

		public float Xrotate;

		public float Yrotate;

		public float Zrotate;

		public int whichRoid;

		public static UniverseScreen universeScreen;

		private SceneObject AsteroidSO;

		public Matrix WorldMatrix;

		private BoundingSphere bs;

		public Asteroid()
		{
			this.spinx = RandomMath.RandomBetween(0.01f, 0.2f);
			this.spiny = RandomMath.RandomBetween(0.01f, 0.2f);
			this.spinz = RandomMath.RandomBetween(0.01f, 0.2f);
			this.Xrotate = RandomMath.RandomBetween(0.01f, 1.02f);
			this.Yrotate = RandomMath.RandomBetween(0.01f, 1.02f);
			this.Zrotate = RandomMath.RandomBetween(0.01f, 1.02f);
		}

		public SceneObject GetSO()
		{
			return this.AsteroidSO;
		}

		public override void Initialize()
		{
			base.Initialize();
			this.AsteroidSO = new SceneObject(Ship_Game.ResourceManager.GetModel(string.Concat("Model/Asteroids/asteroid", this.whichRoid)).Meshes[0])
			{
				ObjectType = ObjectType.Static,
				Visibility = ObjectVisibility.Rendered
			};
			this.WorldMatrix = ((((Matrix.Identity * Matrix.CreateScale(this.scale)) * Matrix.CreateRotationX(this.Xrotate)) * Matrix.CreateRotationY(this.Yrotate)) * Matrix.CreateRotationZ(this.Zrotate)) * Matrix.CreateTranslation(this.Position3D);
			this.AsteroidSO.World = this.WorldMatrix;
			base.Radius = this.AsteroidSO.ObjectBoundingSphere.Radius * this.scale * 0.65f;
			int radius = (int)base.Radius / 5;
            base.Position = new Vector2(this.Position3D.X, this.Position3D.Y);
            this.Position3D.X = base.Position.X;
            this.Position3D.Y = base.Position.Y;
            this.Center = base.Position;
		}

		public override void Update(float elapsedTime)
		{
			ContainmentType currentContainmentType = ContainmentType.Disjoint;
			this.bs = new BoundingSphere(new Vector3(base.Position, 0f), 200f);
			if (Asteroid.universeScreen.Frustum != null)
			{
				currentContainmentType = Asteroid.universeScreen.Frustum.Contains(this.bs);
			}
			if (this.Active)
			{
				if (currentContainmentType != ContainmentType.Disjoint && Asteroid.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
				{
                    this.Xrotate += this.spinx * elapsedTime;
                    this.Zrotate += this.spiny * elapsedTime;
                    this.Yrotate += this.spinz * elapsedTime;
					this.WorldMatrix = ((((Matrix.Identity * Matrix.CreateScale(this.scale)) * Matrix.CreateRotationX(this.Xrotate)) * Matrix.CreateRotationY(this.Yrotate)) * Matrix.CreateRotationZ(this.Zrotate)) * Matrix.CreateTranslation(this.Position3D);
					if (this.AsteroidSO != null)
					{
						this.AsteroidSO.World = this.WorldMatrix;
					}
				}
				base.Update(elapsedTime);
			}
		}
	}
}