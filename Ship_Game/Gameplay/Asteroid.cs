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

		public int CommonOreCount;

		public int RareOreCount;

		public int ExoticOreCount;

		public AudioEmitter emitter = new AudioEmitter();

		private BoundingSphere bs;

		private GameplayObject lastDamagedBy;

		public Asteroid()
		{
			this.spinx = RandomMath.RandomBetween(0.01f, 0.2f);
			this.spiny = RandomMath.RandomBetween(0.01f, 0.2f);
			this.spinz = RandomMath.RandomBetween(0.01f, 0.2f);
			this.Xrotate = RandomMath.RandomBetween(0.01f, 1.02f);
			this.Yrotate = RandomMath.RandomBetween(0.01f, 1.02f);
			this.Zrotate = RandomMath.RandomBetween(0.01f, 1.02f);
		}

		public override bool Damage(GameplayObject source, float damageAmount)
		{
			if (!(source is Beam))
			{
				Cue hit = AudioManager.GetCue("roid_impact");
				hit.Apply3D(GameplayObject.audioListener, this.emitter);
				hit.Play();
			}
			Asteroid health = this;
			health.Health = health.Health - damageAmount;
			if (!(source is Beam))
			{
				Projectile sourceAsProjectile = source as Projectile;
				if (sourceAsProjectile == null)
				{
					this.lastDamagedBy = source;
				}
				else
				{
					this.lastDamagedBy = sourceAsProjectile.Owner;
					for (int i = 0; i < 5; i++)
					{
						Asteroid.universeScreen.smokePlumeParticles.AddParticleThreadB(new Vector3(source.Center, 0f), new Vector3(RandomMath.RandomDirection() * 3f, RandomMath.RandomBetween(-5f, 5f)));
					}
				}
			}
			else
			{
				Vector2 direction = (source as Beam).ActualHitDestination - (source as Beam).Source;
				direction = Vector2.Normalize(direction);
				direction = Vector2.Negate(direction);
				Asteroid.universeScreen.smokePlumeParticles.AddParticleThreadB(new Vector3((source as Beam).ActualHitDestination + (direction * this.radius), 0f), new Vector3((direction * RandomMath.RandomBetween(5f, 25f)) + (RandomMath.RandomDirection() * 3f), RandomMath.RandomBetween(-5f, 5f)));
				for (int i = 0; i < 5; i++)
				{
					Asteroid.universeScreen.sparks.AddParticleThreadB(new Vector3((source as Beam).ActualHitDestination + (direction * this.AsteroidSO.WorldBoundingSphere.Radius), 0f), new Vector3((direction * RandomMath.RandomBetween(15f, 40f)) + (RandomMath.RandomDirection() * 3f), RandomMath.RandomBetween(-5f, 5f)));
				}
			}
			return true;
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
			base.Health = 50f * base.Radius;
			int radius = (int)base.Radius / 5;
			this.mass = 25f * base.Radius;
		}

		public override bool Touch(GameplayObject target)
		{
			if (target is Asteroid)
			{
				Vector2 playerAsteroidVector = base.Position - target.Position;
				if (playerAsteroidVector.LengthSquared() > 0f)
				{
					playerAsteroidVector.Normalize();
					float rammingSpeed = Vector2.Dot(playerAsteroidVector, target.Velocity) - Vector2.Dot(playerAsteroidVector, base.Velocity);
					float momentum = base.Mass * rammingSpeed;
					target.Damage(this, momentum * 0.007f);
				}
			}
			else if (target is Projectile)
			{
				return true;
			}
			return base.Touch(target);
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
				base.Position = new Vector2(this.Position3D.X, this.Position3D.Y);
				Vector2 movement = base.Velocity * elapsedTime;
				Asteroid position = this;
				position.Position = position.Position + movement;
				this.Position3D.X = base.Position.X;
				this.Position3D.Y = base.Position.Y;
				this.Center = base.Position;
				Asteroid xrotate = this;
				xrotate.Xrotate = xrotate.Xrotate + this.spinx * elapsedTime;
				Asteroid zrotate = this;
				zrotate.Zrotate = zrotate.Zrotate + this.spiny * elapsedTime;
				Asteroid yrotate = this;
				yrotate.Yrotate = yrotate.Yrotate + this.spinz * elapsedTime;
				if (currentContainmentType != ContainmentType.Disjoint && Asteroid.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
				{
					this.WorldMatrix = ((((Matrix.Identity * Matrix.CreateScale(this.scale)) * Matrix.CreateRotationX(this.Xrotate)) * Matrix.CreateRotationY(this.Yrotate)) * Matrix.CreateRotationZ(this.Zrotate)) * Matrix.CreateTranslation(this.Position3D);
					if (this.AsteroidSO != null)
					{
						this.AsteroidSO.World = this.WorldMatrix;
					}
				}
				if (base.Health <= 0f)
				{
					this.Die(this.lastDamagedBy, false);
				}
				this.emitter.Position = this.Position3D;
				base.Update(elapsedTime);
			}
		}
	}
}