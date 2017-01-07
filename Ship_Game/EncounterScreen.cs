using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public sealed class EncounterScreen : GameScreen
	{
		private Vector2 Cursor = Vector2.Zero;

		public Encounter encounter;

		private UniverseScreen screen;

		//private float transitionElapsedTime;

		public EncounterScreen(UniverseScreen screen, Empire playerEmpire, Empire targetEmp, SolarSystem tarSys, Encounter e) : base(screen)
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