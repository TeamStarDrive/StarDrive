using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Bomb
	{
		private const float trailParticlesPerSecond = 500f;

		public static UniverseScreen screen;

		private Matrix WorldMatrix;

		public Vector3 Position;

		public Vector3 Velocity;

		public string FireCueName;

		public string ExplodeCueName;

		public string WeaponName;

		public string TextureName = "projBall_02_orange";

		public string ModelName = "projBall";

		private Texture2D projTexture;

		private Planet TargetPlanet;

		public float PopulationDamage;

		public float OrdnanceCost;

		public float OreDamage;

		public float BuildingDestructionChance;

		public float TroopDamageChance;

		public float MinTroopDamage;

		public float MaxTroopDamage;

		private ParticleEmitter trailEmitter;

		private ParticleEmitter firetrailEmitter;

		public Empire owner;

		public float facing;

		private Model projModel;

		private float planetRadius;

		public Bomb(Vector3 Position, Empire e)
		{
			this.owner = e;
			this.projTexture = ResourceManager.ProjTextDict[this.TextureName];
			this.projModel = ResourceManager.ProjectileModelDict[this.ModelName];
			this.WeaponName = "NuclearBomb";
			this.Position = Position;
		}

		public void DoImpact()
		{
			this.TargetPlanet.DropBomb(this);
			Bomb.screen.BombList.QueuePendingRemoval(this);
		}

		public Model GetModel()
		{
			return this.projModel;
		}

		public Texture2D GetTexture()
		{
			return this.projTexture;
		}

		public Matrix GetWorld()
		{
			return this.WorldMatrix;
		}

		public void SetTarget(Planet p)
		{
			this.TargetPlanet = p;
			this.planetRadius = this.TargetPlanet.SO.WorldBoundingSphere.Radius;
			Vector3 vtt = (new Vector3(this.TargetPlanet.Position, 2500f) + new Vector3(RandomMath2.RandomBetween(-500f, 500f), RandomMath2.RandomBetween(-500f, 500f), 0f)) - this.Position;
			vtt = Vector3.Normalize(vtt);
			this.Velocity = vtt * 1350f;
		}

		public void Update(float elapsedTime)
		{
			Bomb position = this;
			position.Position = position.Position + (this.Velocity * elapsedTime);
			this.WorldMatrix = Matrix.CreateTranslation(this.Position) * Matrix.CreateRotationZ(this.facing);
			this.planetRadius = this.TargetPlanet.SO.WorldBoundingSphere.Radius;
			if (this.TargetPlanet.ShieldStrengthCurrent > 0f)
			{
				if (Vector3.Distance(this.Position, new Vector3(this.TargetPlanet.Position, 2500f)) < this.planetRadius + 100f)
				{
					this.DoImpact();
				}
			}
			else if (Vector3.Distance(this.Position, new Vector3(this.TargetPlanet.Position, 2500f)) < this.planetRadius + 30f)
			{
				this.DoImpact();
			}
			if (Vector3.Distance(this.Position, new Vector3(this.TargetPlanet.Position, 2500f)) < this.planetRadius + 1000f)
			{
				if (this.trailEmitter == null)
				{
					Bomb velocity = this;
					velocity.Velocity = velocity.Velocity * 0.65f;
					this.trailEmitter = new ParticleEmitter(Bomb.screen.projectileTrailParticles, 500f, this.Position);
					this.firetrailEmitter = new ParticleEmitter(Bomb.screen.fireTrailParticles, 500f, this.Position);
				}
				if (this.trailEmitter != null)
				{
					this.firetrailEmitter.Update(elapsedTime, this.Position);
					this.trailEmitter.Update(elapsedTime, this.Position);
				}
			}
		}
	}
}