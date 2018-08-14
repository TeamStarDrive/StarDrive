using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;

namespace Ship_Game
{
	public sealed class RenderedScreen : GameScreen
	{
		private Matrix View;
		private Matrix Projection;
		private SceneObject ShipSO;
		private Vector2 ShipPosition = new Vector2(640f, 350f);
		private float Zrotate;

		public RenderedScreen() : base(null)
		{
			TransitionOnTime = TimeSpan.FromSeconds(1);
			TransitionOffTime = TimeSpan.FromSeconds(0.5);
		}

		public override void Draw(SpriteBatch batch)
		{
			if (SplashScreen.DisplayComplete)
			{
			    ScreenManager.HideSplashScreen();
                ScreenManager.BeginFrameRendering(Game1.Instance.GameTime, ref View, ref Projection);
                ScreenManager.RenderSceneObjects();
                ScreenManager.EndFrameRendering();
			}
		}

		public override void LoadContent()
		{
            ScreenManager.RemoveAllObjects();
            AssignLightRig("example/light_rig");
			ScreenManager.environment = TransientContent.Load<SceneEnvironment>("example/scene_environment");

		    var model = TransientContent.Load<Model>("Model/SpaceObjects/planet_22");
			ShipSO = new SceneObject(model.Meshes[0])
			{
				ObjectType = ObjectType.Dynamic,
				World = Matrix.Identity
			};
            AddObject(ShipSO);
            
			float aspectRatio = (float)Viewport.Width / Viewport.Height;
			var camPos = new Vector3(0f, 0f, 1500f);
			View = Matrix.CreateTranslation(0f, 0f, 0f)
                * Matrix.CreateRotationY(180f.ToRadians())
                * Matrix.CreateRotationX(0f.ToRadians()) 
                * Matrix.CreateLookAt(camPos, Vector3.Zero, new Vector3(0f, -1f, 0f));
			Projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 6000000f);
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			RenderedScreen milliseconds = this;
			float zrotate = milliseconds.Zrotate;
			TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
			milliseconds.Zrotate = zrotate + elapsedGameTime.Milliseconds / 20000f;
			ShipSO.World =  Matrix.CreateScale(2f)
                * Matrix.CreateRotationZ(1.57079637f - Zrotate)
                * Matrix.CreateRotationX(20f.ToRadians())
                * Matrix.CreateRotationY(65f.ToRadians())
                * Matrix.CreateRotationZ(1.57079637f)
                * Matrix.CreateTranslation(ShipPosition.X - 300f, ShipPosition.Y - 500f, 800f);
            ScreenManager.UpdateSceneObjects(gameTime);
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}