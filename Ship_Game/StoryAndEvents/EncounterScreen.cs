using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
			encounter = e;
			encounter.CurrentMessage = 0;
			encounter.SetPlayerEmpire(playerEmpire);
			encounter.SetSys(tarSys);
			encounter.SetTarEmp(targetEmp);
			this.screen = screen;
			IsPopup = true;
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public override void Draw(SpriteBatch batch)
		{
			ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
			ScreenManager.SpriteBatch.Begin();
			encounter.Draw(ScreenManager);
			ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			base.ExitScreen();
		}

		public override bool HandleInput(InputState input)
		{
			encounter.HandleInput(input, this);
			return base.HandleInput(input);
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