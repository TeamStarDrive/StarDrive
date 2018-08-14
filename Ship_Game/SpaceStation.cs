using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.ObjectModel;

namespace Ship_Game
{
	public sealed class SpaceStation
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
			Name = "Random Station";
		}

		public SpaceStation()
		{
		}

		public void LoadContent(ScreenManager screenManager)
		{
			Model innerModel = Game1.Instance.Content.Load<Model>("Model/Stations/spacestation01_inner");
			Model outerModel = Game1.Instance.Content.Load<Model>("Model/Stations/spacestation01_outer");

			ModelMesh mesh = innerModel.Meshes[0];
			InnerSO = new SceneObject(mesh)
			{
				ObjectType = ObjectType.Dynamic,
				World = Matrix.Identity
			};

			ModelMesh mesh1 = outerModel.Meshes[0];
			OuterSO = new SceneObject(mesh1)
			{
				ObjectType = ObjectType.Dynamic,
				World = Matrix.Identity
			};


			Position = planet.Center;

            //The Doctor: Mod definable spaceport 'station' art scaling
            float scale = 0.8f;
            if (GlobalStats.ActiveMod != null)
            {
                scale = GlobalStats.ActiveModInfo.Spaceportscale;
            }

			if (InnerSO != null && OuterSO != null)
			{
				InnerSO.World = (((((Matrix.Identity * Matrix.CreateScale(scale)) *
                    Matrix.CreateRotationZ(1.57079637f)) 
                    * Matrix.CreateRotationX(20f.ToRadians())) 
                    * Matrix.CreateRotationY(65f.ToRadians())) 
                    * Matrix.CreateRotationZ(1.57079637f)) 
                    * Matrix.CreateTranslation(Position.X, Position.Y, 600f);
				OuterSO.World = (((((Matrix.Identity 
                    * Matrix.CreateScale(scale)) 
                    * Matrix.CreateRotationZ(1.57079637f)) 
                    * Matrix.CreateRotationX(20f.ToRadians())) 
                    * Matrix.CreateRotationY(65f.ToRadians())) 
                    * Matrix.CreateRotationZ(1.57079637f)) 
                    * Matrix.CreateTranslation(Position.X, Position.Y, 600f);
			}
            screenManager.AddObject(InnerSO);
            screenManager.AddObject(OuterSO);
		}

		public void SetVisibility(bool vis, ScreenManager screenManager, Planet p)
		{
			planet = p;
			if (InnerSO == null || OuterSO == null)
			{
				LoadContent(screenManager);
			}
			if (vis)
			{
				InnerSO.Visibility = ObjectVisibility.RenderedAndCastShadows;
				OuterSO.Visibility = ObjectVisibility.RenderedAndCastShadows;
				return;
			}
			InnerSO.Visibility = ObjectVisibility.None;
			OuterSO.Visibility = ObjectVisibility.None;
		}

		public void Update(float elapsedTime)
		{
			Position = planet.Center;
			SpaceStation zrotate = this;
			zrotate.Zrotate = zrotate.Zrotate + rotAmount * elapsedTime;

            //The Doctor: Mod definable spaceport 'station' art scaling
            float scale = 0.8f;
            if (GlobalStats.ActiveMod != null)
            {
                scale = GlobalStats.ActiveModInfo.Spaceportscale;
            }

			if (InnerSO != null && OuterSO != null && planet.SO.Visibility == ObjectVisibility.Rendered)
			{
				InnerSO.World = (((((Matrix.Identity * Matrix.CreateScale(scale)) * Matrix.CreateRotationZ(1.57079637f + Zrotate)) * Matrix.CreateRotationX(20f.ToRadians())) * Matrix.CreateRotationY(65f.ToRadians())) * Matrix.CreateRotationZ(1.57079637f)) * Matrix.CreateTranslation(Position.X, Position.Y, 600f);
				OuterSO.World = (((((Matrix.Identity * Matrix.CreateScale(scale)) * Matrix.CreateRotationZ(1.57079637f - Zrotate)) * Matrix.CreateRotationX(20f.ToRadians())) * Matrix.CreateRotationY(65f.ToRadians())) * Matrix.CreateRotationZ(1.57079637f)) * Matrix.CreateTranslation(Position.X, Position.Y, 600f);
			}
		}
	}
}