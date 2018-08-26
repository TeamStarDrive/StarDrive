using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class EncounterPopup : PopupWindow
	{
		public bool fade = true;

		public bool FromGame;

		private UniverseScreen screen;

		public string UID;

		public Encounter encounter;

		public EncounterPopup(UniverseScreen s, Empire playerEmpire, Empire targetEmp, SolarSystem tarSys, Encounter e) : base(s, 600, 600)
		{
			screen = s;
			encounter = e;
			encounter.CurrentMessage = 0;
			encounter.SetPlayerEmpire(playerEmpire);
			encounter.SetSys(tarSys);
			encounter.SetTarEmp(targetEmp);
			fade = true;
			IsPopup = true;
			FromGame = true;
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0);
		}

		public override void Draw(SpriteBatch batch)
		{
			if (fade)
			{
				ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
			}
			base.Draw(batch);

			ScreenManager.SpriteBatch.Begin();
			encounter.Draw(ScreenManager);
			ScreenManager.SpriteBatch.End();
		}

		public override bool HandleInput(InputState input)
		{
			encounter.HandleInput(input, this);
			return base.HandleInput(input);
		}

		public override void LoadContent()
		{
			TitleText = encounter.Name;
			MiddleText = encounter.DescriptionText;
			base.LoadContent();
			encounter.LoadContent(screen.ScreenManager, new Rectangle(TitleRect.X - 4, TitleRect.Y + TitleRect.Height + MidContainer.Height + 10, TitleRect.Width, 600 - (TitleRect.Height + MidContainer.Height)));
		}
	}
}