using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ship_Game.Gameplay
{
	public class Loot : GameplayObject
	{
		public static UniverseScreen universeScreen;

		public float scale = 0.3f;

		public float spinx;

		public float spiny;

		public float spinz;

		public float Xrotate;

		public float Yrotate;

		public float Zrotate;

		public SceneObject LootSO;

		public Matrix WorldMatrix;

		public string ModelPath;

		public Good good;

		public int quantity;

		public Loot(SolarSystem s, string modelPath, GameplayObject source)
		{
			this.system = s;
			this.system.LootList.Add(this);
			base.Position = source.Position;
			this.Center = source.Position;
			this.ModelPath = modelPath;
			this.radius = 5f;
			this.spinx = RandomMath.RandomBetween(0.01f, 0.2f);
			this.spiny = RandomMath.RandomBetween(0.01f, 0.2f);
			this.spinz = RandomMath.RandomBetween(0.01f, 0.2f);
			this.Xrotate = RandomMath.RandomBetween(0.01f, 1.02f);
			this.Yrotate = RandomMath.RandomBetween(0.01f, 1.02f);
			this.Zrotate = RandomMath.RandomBetween(0.01f, 1.02f);
			Loot position = this;
			position.Position = position.Position + new Vector2(RandomMath.RandomBetween(-source.Radius, source.Radius), RandomMath.RandomBetween(-source.Radius, source.Radius));
			base.Velocity = source.Velocity + new Vector2(RandomMath.RandomBetween(-7f, 7f), RandomMath.RandomBetween(-7f, 7f));
		}

		public void LoadContent(string modelPath)
		{
			Model lootModel = Loot.universeScreen.ScreenManager.Content.Load<Model>(modelPath);
			ModelMesh mesh1 = lootModel.Meshes[0];
			this.LootSO = new SceneObject(mesh1)
			{
				ObjectType = ObjectType.Dynamic
			};
		}

		public override bool Touch(GameplayObject target)
		{
			if (target is Ship)
			{
				Ship picker = target as Ship;
				if (picker.CargoSpace_Used < picker.CargoSpace_Max)
				{
					AudioManager.GetCue("stone_smallimpact11").Play();
					this.system.LootList.QueuePendingRemoval(this);
					this.system.spatialManager.CollidableObjects.QueuePendingRemoval(this);
					Loot.universeScreen.ScreenManager.inter.ObjectManager.Remove(this.LootSO);
					return base.Touch(target);
				}
			}
			if (target is ShipModule)
			{
				ShipModule picker = target as ShipModule;
				if (picker.GetParent().CargoSpace_Used < picker.GetParent().CargoSpace_Max)
				{
					AudioManager.GetCue("stone_smallimpact11").Play();
					this.system.LootList.QueuePendingRemoval(this);
					this.system.spatialManager.CollidableObjects.QueuePendingRemoval(this);
					Loot.universeScreen.ScreenManager.inter.ObjectManager.Remove(this.LootSO);
					return base.Touch(target);
				}
			}
			return false;
		}

		public new void Update(float elapsedTime)
		{
			Vector2 movement = base.Velocity * elapsedTime;
			Loot position = this;
			position.Position = position.Position + movement;
			this.Center = base.Position;
			Loot xrotate = this;
			xrotate.Xrotate = xrotate.Xrotate + this.spinx * elapsedTime;
			Loot zrotate = this;
			zrotate.Zrotate = zrotate.Zrotate + this.spiny * elapsedTime;
			Loot yrotate = this;
			yrotate.Yrotate = yrotate.Yrotate + this.spinz * elapsedTime;
			this.WorldMatrix = ((((Matrix.Identity * Matrix.CreateScale(this.scale)) * Matrix.CreateRotationX(this.Xrotate)) * Matrix.CreateRotationY(this.Yrotate)) * Matrix.CreateRotationZ(this.Zrotate)) * Matrix.CreateTranslation(new Vector3(base.Position, 0f));
			if (this.LootSO != null)
			{
				this.LootSO.World = this.WorldMatrix;
			}
		}
	}
}