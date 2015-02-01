using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ship_Game
{
	public class HaloTestScreen : GameScreen, IDisposable
	{
		private Effect effect;

		private Model Mesh;

		private Matrix View;

		private Matrix Projection;

		public HaloTestScreen()
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
			Matrix world = Matrix.CreateScale(4.05f);
			Matrix view = (world * this.View) * this.Projection;
			this.effect.Parameters["World"].SetValue(world);
			this.effect.Parameters["Projection"].SetValue(this.Projection);
			this.effect.Parameters["View"].SetValue(this.View);
			this.effect.Parameters["CameraPosition"].SetValue(new Vector3(0f, 0f, 1500f));
			this.effect.Parameters["DiffuseLightDirection"].SetValue(new Vector3(-0.98f, 0.425f, -0.4f));
			this.effect.CurrentTechnique = this.effect.Techniques["Planet"];
			this.effect.Begin();
			foreach (EffectPass pass in this.effect.CurrentTechnique.Passes)
			{
				pass.Begin();
				this.Mesh.Meshes[0].Draw();
				pass.End();
			}
			this.effect.End();
			base.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = false;
			base.ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
		}


		public override void LoadContent()
		{
			this.Mesh = base.ScreenManager.Content.Load<Model>("Model/sphere");
			this.effect = base.ScreenManager.Content.Load<Effect>("Effects/PlanetHalo");
			float width = (float)base.ScreenManager.GraphicsDevice.Viewport.Width;
			Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
			float aspectRatio = width / (float)viewport.Height;
			Vector3 camPos = new Vector3(0f, 0f, 1500f) * new Vector3(-1f, 1f, 1f);
			this.View = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
			this.Projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 10000f);
			base.LoadContent();
		}
	}
}