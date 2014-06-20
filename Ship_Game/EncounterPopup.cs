using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public class EncounterPopup : PopupWindow
	{
		public bool fade = true;

		public bool FromGame;

		private UniverseScreen screen;

		public string UID;

		public Encounter encounter;

		public EncounterPopup(UniverseScreen s, Empire playerEmpire, Empire targetEmp, SolarSystem tarSys, Encounter e)
		{
			this.screen = s;
			this.encounter = e;
			this.encounter.CurrentMessage = 0;
			this.encounter.SetPlayerEmpire(playerEmpire);
			this.encounter.SetSys(tarSys);
			this.encounter.SetTarEmp(targetEmp);
			this.fade = true;
			base.IsPopup = true;
			this.FromGame = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0);
			this.r = new Rectangle(0, 0, 600, 600);
		}

		public override void Draw(GameTime gameTime)
		{
			if (this.fade)
			{
				base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			}
			base.DrawBase(gameTime);
			base.ScreenManager.SpriteBatch.Begin();
			this.encounter.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
		}

		public override void HandleInput(InputState input)
		{
			this.encounter.HandleInput(input, this);
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			this.TitleText = this.encounter.Name;
			this.MiddleText = this.encounter.DescriptionText;
			base.LoadContent();
			this.encounter.LoadContent(this.screen.ScreenManager, new Rectangle(this.TitleRect.X - 4, this.TitleRect.Y + this.TitleRect.Height + this.MidContainer.Height + 10, this.TitleRect.Width, 600 - (this.TitleRect.Height + this.MidContainer.Height)));
		}
	}
}