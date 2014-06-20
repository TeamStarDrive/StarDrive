using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using System;

namespace Ship_Game
{
	public class TestScreen : GameScreen, IDisposable
	{
		private Matrix worldMatrix = Matrix.Identity;

		private Matrix view;

		private Matrix projection;

		public Camera2d camera;

		private Background bg = new Background();

		public EmpireUIOverlay EmpireUI;

		//private Vector2 aspect;

		private Vector3 cameraPosition = new Vector3(500000f, 500000f, 1300f);

		private Starfield starfield;

		//private Ship playerShip;

		//private MouseState mouseStateCurrent;

		//private MouseState mouseStatePrevious;

		private Vector2 cameraVelocity = Vector2.Zero;

		//private Vector2 StartDragPos = new Vector2();

		private Vector2 starfieldPos = Vector2.Zero;

		//private int scrollPosition;

		public TestScreen()
		{
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.sceneState.BeginFrameRendering(this.view, this.projection, gameTime, base.ScreenManager.environment, true);
			base.ScreenManager.editor.BeginFrameRendering(base.ScreenManager.sceneState);
			base.ScreenManager.inter.BeginFrameRendering(base.ScreenManager.sceneState);
			this.bg.Draw(this, this.starfield);
			base.ScreenManager.inter.RenderManager.Render();
			base.ScreenManager.inter.EndFrameRendering();
			base.ScreenManager.editor.EndFrameRendering();
			base.ScreenManager.sceneState.EndFrameRendering();
		}

		public override void ExitScreen()
		{
			base.ExitScreen();
		}

		/*protected override void Finalize()
		{
			try
			{
				this.Dispose(false);
			}
			finally
			{
				base.Finalize();
			}
		}*/
        ~TestScreen() {
            //should implicitly do the same thing as the original bad finalize
        }

		public override void HandleInput(InputState input)
		{
			if (input.Escaped)
			{
				this.ExitScreen();
			}
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			LightRig rig = base.ScreenManager.Content.Load<LightRig>("example/NewGamelight_rig");
			base.ScreenManager.inter.LightManager.Clear();
			base.ScreenManager.inter.LightManager.Submit(rig);
			Ship playerShip = Ship_Game.ResourceManager.GetPlayerShip("Fuckyoumobile");
			playerShip.Position = new Vector2(500000f, 500000f);
			playerShip.Rotation = 0f;
			playerShip.SensorRange = 100000f;
			playerShip.Name = "Perseverence";
			playerShip.GetSO().World = (Matrix.CreateRotationY(playerShip.yBankAmount) * Matrix.CreateRotationZ(playerShip.Rotation)) * Matrix.CreateTranslation(new Vector3(playerShip.Center, 0f));
			base.ScreenManager.inter.ObjectManager.Submit(playerShip.GetSO());
			PrimitiveQuad.graphicsDevice = base.ScreenManager.GraphicsDevice;
			this.starfield = new Starfield(Vector2.Zero, base.ScreenManager.GraphicsDevice, base.ScreenManager.Content);
			this.starfield.LoadContent();
			float width = (float)base.ScreenManager.GraphicsDevice.Viewport.Width;
			Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
			float aspectRatio = width / (float)viewport.Height;
			Vector3 camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
			this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
			this.projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 100000f);
		}

		public void PlayNegativeSound()
		{
			AudioManager.GetCue("UI_Misc20").Play();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			Vector3 camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
			this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
			base.ScreenManager.inter.Update(gameTime);
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}