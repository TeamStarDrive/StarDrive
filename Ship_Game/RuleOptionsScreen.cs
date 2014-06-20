using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
	public class RuleOptionsScreen : GameScreen, IDisposable
	{
		public bool isOpen;

		private List<Checkbox> Checkboxes = new List<Checkbox>();

		private Menu2 MainMenu;

		private bool LowRes;

		private FloatSlider FTLPenaltySlider;

		private CloseButton close;

		public Ship itemToBuild;

		public RuleOptionsScreen()
		{
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
			this.MainMenu.Draw(Color.Black);
			Vector2 TitlePos = new Vector2((float)(this.MainMenu.Menu.X + 40), (float)(this.MainMenu.Menu.Y + 40));
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, "Advanced Rule Options", TitlePos, Color.White);
			TitlePos.Y = TitlePos.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);
			string text = Localizer.Token(2289);
			text = HelperFunctions.parseText(Fonts.Arial12, text, (float)(this.MainMenu.Menu.Width - 80));
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, TitlePos, Color.White);
			this.FTLPenaltySlider.DrawDecimal(base.ScreenManager);
			this.close.Draw(base.ScreenManager);
			foreach (Checkbox cb in this.Checkboxes)
			{
				cb.Draw(base.ScreenManager);
			}
			ToolTip.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
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
        ~RuleOptionsScreen() {
            //should implicitly do the same thing as the original bad finalize
        }

		public override void HandleInput(InputState input)
		{
			if (input.Escaped || input.RightMouseClick || this.close.HandleInput(input))
			{
				this.ExitScreen();
			}
			if (HelperFunctions.CheckIntersection(this.FTLPenaltySlider.ContainerRect, input.CursorPosition))
			{
				ToolTip.CreateTooltip(Localizer.Token(2286), base.ScreenManager);
			}
			this.FTLPenaltySlider.HandleInput(input);
			GlobalStats.FTLInSystemModifier = this.FTLPenaltySlider.amount;
			foreach (Checkbox cb in this.Checkboxes)
			{
				cb.HandleInput(input);
			}
		}

		public override void LoadContent()
		{
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1366 || base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight <= 720)
			{
				this.LowRes = true;
			}
			Rectangle titleRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 203, (this.LowRes ? 10 : 44), 406, 80);
			Rectangle nameRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - (int)((float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.5f) / 2, titleRect.Y + titleRect.Height + 5, (int)((float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.5f), 150);
			Rectangle leftRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - (int)((float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.5f) / 2, nameRect.Y + nameRect.Height + 5, (int)((float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * 0.5f), base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - (titleRect.Y + titleRect.Height) - (int)(0.28f * (float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight));
			if (leftRect.Height > 580)
			{
				leftRect.Height = 580;
			}
			this.close = new CloseButton(new Rectangle(leftRect.X + leftRect.Width - 40, leftRect.Y + 20, 20, 20));
			Rectangle ftlRect = new Rectangle(leftRect.X + 60, leftRect.Y + 100, 270, 50);
			this.FTLPenaltySlider = new FloatSlider(ftlRect, Localizer.Token(4007));
			this.FTLPenaltySlider.SetAmount(GlobalStats.FTLInSystemModifier);
			this.FTLPenaltySlider.amount = GlobalStats.FTLInSystemModifier;
			Ref<bool> acomRef = new Ref<bool>(() => GlobalStats.PlanetaryGravityWells, (bool x) => GlobalStats.PlanetaryGravityWells = x);
			Checkbox cb = new Checkbox(new Vector2((float)ftlRect.X, (float)(ftlRect.Y + 65)), Localizer.Token(4008), acomRef, Fonts.Arial12Bold);
			this.Checkboxes.Add(cb);
			cb.Tip_Token = 2288;
			this.MainMenu = new Menu2(base.ScreenManager, leftRect);
		}
	}
}