using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.ObjectModel;

namespace Ship_Game
{
	public class SpaceStation
	{
		public SceneObject InnerSO;

		public SceneObject OuterSO;

		public Vector2 Position;

		public Planet planet;

		public SolarSystem ParentSystem;

		public string Name;

		private float Zrotate;

		private float rotAmount = 0.1f;

		public SpaceStation(Vector2 Position)
		{
			this.Position = Position;
			this.Name = "Random Station";
		}

		public SpaceStation()
		{
		}

		public void LoadContent(Ship_Game.ScreenManager ScreenManager)
		{
			Model InnerModel = ScreenManager.Content.Load<Model>("Model/Stations/spacestation01_inner");
			Model OuterModel = ScreenManager.Content.Load<Model>("Model/Stations/spacestation01_outer");
			ModelMesh mesh = InnerModel.Meshes[0];
			this.InnerSO = new SceneObject(mesh)
			{
				ObjectType = ObjectType.Dynamic,
				World = Matrix.Identity
			};
			ModelMesh mesh1 = OuterModel.Meshes[0];
			this.OuterSO = new SceneObject(mesh1)
			{
				ObjectType = ObjectType.Dynamic,
				World = Matrix.Identity
			};
			this.Position = this.planet.Position;
			if (this.InnerSO != null && this.OuterSO != null)
			{
				this.InnerSO.World = (((((Matrix.Identity * Matrix.CreateScale(0.8f)) * Matrix.CreateRotationZ(1.57079637f)) * Matrix.CreateRotationX(MathHelper.ToRadians(20f))) * Matrix.CreateRotationY(MathHelper.ToRadians(65f))) * Matrix.CreateRotationZ(1.57079637f)) * Matrix.CreateTranslation(this.Position.X, this.Position.Y, 600f);
				this.OuterSO.World = (((((Matrix.Identity * Matrix.CreateScale(0.8f)) * Matrix.CreateRotationZ(1.57079637f)) * Matrix.CreateRotationX(MathHelper.ToRadians(20f))) * Matrix.CreateRotationY(MathHelper.ToRadians(65f))) * Matrix.CreateRotationZ(1.57079637f)) * Matrix.CreateTranslation(this.Position.X, this.Position.Y, 600f);
			}
			lock (GlobalStats.ObjectManagerLocker)
			{
				ScreenManager.inter.ObjectManager.Submit(this.InnerSO);
				ScreenManager.inter.ObjectManager.Submit(this.OuterSO);
			}
		}

		public void SetVisibility(bool vis, Ship_Game.ScreenManager ScreenManager, Planet p)
		{
			this.planet = p;
			if (this.InnerSO == null || this.OuterSO == null)
			{
				this.LoadContent(ScreenManager);
			}
			if (vis)
			{
				this.InnerSO.Visibility = ObjectVisibility.RenderedAndCastShadows;
				this.OuterSO.Visibility = ObjectVisibility.RenderedAndCastShadows;
				return;
			}
			this.InnerSO.Visibility = ObjectVisibility.None;
			this.OuterSO.Visibility = ObjectVisibility.None;
		}

		public void Update(float elapsedTime)
		{
			this.Position = this.planet.Position;
			SpaceStation zrotate = this;
			zrotate.Zrotate = zrotate.Zrotate + this.rotAmount * elapsedTime;
			if (this.InnerSO != null && this.OuterSO != null && this.planet.SO.Visibility == ObjectVisibility.Rendered)
			{
				this.InnerSO.World = (((((Matrix.Identity * Matrix.CreateScale(0.8f)) * Matrix.CreateRotationZ(1.57079637f + this.Zrotate)) * Matrix.CreateRotationX(MathHelper.ToRadians(20f))) * Matrix.CreateRotationY(MathHelper.ToRadians(65f))) * Matrix.CreateRotationZ(1.57079637f)) * Matrix.CreateTranslation(this.Position.X, this.Position.Y, 600f);
				this.OuterSO.World = (((((Matrix.Identity * Matrix.CreateScale(0.8f)) * Matrix.CreateRotationZ(1.57079637f - this.Zrotate)) * Matrix.CreateRotationX(MathHelper.ToRadians(20f))) * Matrix.CreateRotationY(MathHelper.ToRadians(65f))) * Matrix.CreateRotationZ(1.57079637f)) * Matrix.CreateTranslation(this.Position.X, this.Position.Y, 600f);
			}
		}
	}
}