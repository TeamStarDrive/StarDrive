using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.ObjectModel;

namespace Ship_Game
{
	public class RenderedScreen : GameScreen
	{
		private Matrix worldMatrix = Matrix.Identity;

		private Matrix view;

		private Matrix projection;

		private Model model;

		private SceneObject shipSO;

		private Background bg = new Background();

		//private Model mode;

		private Vector2 ShipPosition = new Vector2(640f, 350f);

		private float Zrotate;

		private Vector3 cameraPosition = new Vector3(0f, 0f, 1600f);

		public RenderedScreen()
		{
			base.TransitionOnTime = TimeSpan.FromSeconds(1);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.5);
		}

		public override void Draw(GameTime gameTime)
		{
			if (SplashScreen.DisplayComplete)
			{
				base.ScreenManager.splashScreenGameComponent.Visible = false;
				base.ScreenManager.sceneState.BeginFrameRendering(this.view, this.projection, gameTime, base.ScreenManager.environment, true);
				base.ScreenManager.editor.BeginFrameRendering(base.ScreenManager.sceneState);
				base.ScreenManager.inter.BeginFrameRendering(base.ScreenManager.sceneState);
				base.ScreenManager.inter.RenderManager.Render();
				base.ScreenManager.inter.EndFrameRendering();
				base.ScreenManager.editor.EndFrameRendering();
				base.ScreenManager.sceneState.EndFrameRendering();
			}
		}

		public override void LoadContent()
		{
			base.ScreenManager.inter.ObjectManager.Clear();
			base.ScreenManager.inter.LightManager.Clear();
			this.model = base.ScreenManager.Content.Load<Model>("Model/SpaceObjects/planet_22");
			LightRig rig = base.ScreenManager.Content.Load<LightRig>("example/light_rig");
			base.ScreenManager.inter.LightManager.Submit(rig);
			base.ScreenManager.environment = base.ScreenManager.Content.Load<SceneEnvironment>("example/scene_environment");
			ModelMesh mesh = this.model.Meshes[0];
			this.shipSO = new SceneObject(mesh)
			{
				ObjectType = ObjectType.Dynamic,
				World = this.worldMatrix
			};
			base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
			float width = (float)base.ScreenManager.GraphicsDevice.Viewport.Width;
			Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
			float aspectRatio = width / (float)viewport.Height;
			Vector3 camPos = new Vector3(0f, 0f, 1500f) * new Vector3(-1f, 1f, 1f);
			this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
			this.projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 6000000f);
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			RenderedScreen milliseconds = this;
			float zrotate = milliseconds.Zrotate;
			TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
			milliseconds.Zrotate = zrotate + (float)elapsedGameTime.Milliseconds / 20000f;
			this.shipSO.World = (((((Matrix.Identity * Matrix.CreateScale(2f)) * Matrix.CreateRotationZ(1.57079637f - this.Zrotate)) * Matrix.CreateRotationX(MathHelper.ToRadians(20f))) * Matrix.CreateRotationY(MathHelper.ToRadians(65f))) * Matrix.CreateRotationZ(1.57079637f)) * Matrix.CreateTranslation(this.ShipPosition.X - 300f, this.ShipPosition.Y - 500f, 800f);
			base.ScreenManager.inter.Update(gameTime);
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}