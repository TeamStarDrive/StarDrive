using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public class EncounterScreen : GameScreen, IDisposable
	{
		private Vector2 Cursor = Vector2.Zero;

		public Encounter encounter;

		private UniverseScreen screen;

		//private float transitionElapsedTime;

		public EncounterScreen(UniverseScreen screen, Empire playerEmpire, Empire targetEmp, SolarSystem tarSys, Encounter e)
		{
			this.encounter = e;
			this.encounter.CurrentMessage = 0;
			this.encounter.SetPlayerEmpire(playerEmpire);
			this.encounter.SetSys(tarSys);
			this.encounter.SetTarEmp(targetEmp);
			this.screen = screen;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
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
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			this.encounter.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
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
        ~EncounterScreen() {
            //should implicitly do the same thing as the original bad finalize
            this.Dispose(false);
        }

		public override void HandleInput(InputState input)
		{
			this.encounter.HandleInput(input, this);
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}