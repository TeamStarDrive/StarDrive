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

		EncounterPopup(UniverseScreen s, Empire playerEmpire, Empire targetEmp, Encounter e) : base(s, 600, 600)
		{
			screen = s;
			encounter = e;
			encounter.CurrentMessage = 0;
			encounter.SetPlayerEmpire(playerEmpire);
			encounter.SetSys(null);
			encounter.SetTarEmp(targetEmp);
			fade = true;
			IsPopup = true;
			FromGame = true;
			TransitionOnTime = 0.25f;
			TransitionOffTime = 0f;
		}

        public static void Show(UniverseScreen s, Empire player, Empire them, Encounter e)
        {
            var screen = new EncounterPopup(s, player, them, e);
            ScreenManager.Instance.AddScreenDeferred(screen);
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