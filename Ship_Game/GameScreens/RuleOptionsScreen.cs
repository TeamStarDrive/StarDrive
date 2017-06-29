using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
	public sealed class RuleOptionsScreen : GameScreen
	{
		public bool isOpen;
		private Array<UICheckBox> Checkboxes = new Array<UICheckBox>();
		private Menu2 MainMenu;
		private bool LowRes;
		private FloatSlider FTLPenaltySlider;
        private FloatSlider EnemyFTLPenaltySlider;
		private CloseButton close;
        private FloatSlider GravityWellSize;
        private FloatSlider extraPlanets;
        private FloatSlider IncreaseMaintenance;
        private FloatSlider MinimumWarpRange;
        private FloatSlider StartingPlanetRichness;
        private FloatSlider TurnTimer;
		public Ship itemToBuild;

		public RuleOptionsScreen(GameScreen parent) : base(parent)
		{
			IsPopup = true;
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
			ScreenManager.SpriteBatch.Begin();
			MainMenu.Draw(Color.Black);

			var titlePos = new Vector2(MainMenu.Menu.X + 40, MainMenu.Menu.Y + 40);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, "Advanced Rule Options", titlePos, Color.White);
			titlePos.Y += Fonts.Arial20Bold.LineSpacing + 2;
			string text = Localizer.Token(2289);
			text = HelperFunctions.ParseText(Fonts.Arial12, text, MainMenu.Menu.Width - 80);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, titlePos, Color.White);

			FTLPenaltySlider      .DrawPercent(ScreenManager);
            EnemyFTLPenaltySlider .DrawPercent(ScreenManager);
            GravityWellSize       .DrawDecimal(ScreenManager);
            extraPlanets          .DrawDecimal(ScreenManager);
            IncreaseMaintenance   .DrawDecimal(ScreenManager);
            MinimumWarpRange      .DrawDecimal(ScreenManager);
            StartingPlanetRichness.DrawDecimal(ScreenManager);
            TurnTimer             .DrawDecimal(ScreenManager);
			close.Draw(ScreenManager);
			foreach (var cb in Checkboxes) cb.Draw(ScreenManager.SpriteBatch);
			ToolTip.Draw(ScreenManager.SpriteBatch);
			ScreenManager.SpriteBatch.End();
		}

	
		public override bool HandleInput(InputState input)
		{
			if (input.Escaped || input.RightMouseClick || close.HandleInput(input))
			{
				ExitScreen();
			    return true;
			}

            if (base.HandleInput(input))
                return true;

            var pos = input.CursorPosition;
			if (FTLPenaltySlider.HitTest(pos))      ToolTip.CreateTooltip(Localizer.Token(2286));
            if (EnemyFTLPenaltySlider.HitTest(pos)) ToolTip.CreateTooltip(Localizer.Token(7041));
            if (GravityWellSize.HitTest(pos))       ToolTip.CreateTooltip(Localizer.Token(6003));
            if (extraPlanets.HitTest(pos))          ToolTip.CreateTooltip("Add up to 6 random planets to each system");
            if (MinimumWarpRange.HitTest(pos))      ToolTip.CreateTooltip("Minumum warp range a ship must have before it needs to recharge for the AI to build it");
            if (IncreaseMaintenance.HitTest(pos))   ToolTip.CreateTooltip("Multiply Global Maintenance Cost By  SSP's Are Not Affected");
            if (StartingPlanetRichness.HitTest(pos))ToolTip.CreateTooltip("Add to all Starting Empire Planets this Value");
            if (TurnTimer.HitTest(pos))             ToolTip.CreateTooltip("Time in seconds for turns");

            foreach (var cb in Checkboxes)
                cb.HandleInput(input);
            FTLPenaltySlider.HandleInput(input);
            EnemyFTLPenaltySlider.HandleInput(input);
            GravityWellSize.HandleInput(input);
            extraPlanets.HandleInput(input);
            MinimumWarpRange.HandleInput(input);
            IncreaseMaintenance.HandleInput(input);
            StartingPlanetRichness.HandleInput(input);
            TurnTimer.HandleInput(input);

            GlobalStats.FTLInSystemModifier      = FTLPenaltySlider.Amount;
            GlobalStats.EnemyFTLInSystemModifier = EnemyFTLPenaltySlider.Amount;
            GlobalStats.GravityWellRange         = GravityWellSize.AmountRange; //amount replaced with amountRange
            GlobalStats.ExtraPlanets             = (int)extraPlanets.AmountRange;
            GlobalStats.MinimumWarpRange         = MinimumWarpRange.AmountRange;
            GlobalStats.ShipMaintenanceMulti     = IncreaseMaintenance.AmountRange;
            GlobalStats.StartingPlanetRichness   = StartingPlanetRichness.AmountRange;
            GlobalStats.TurnTimer                = (byte)TurnTimer.AmountRange;

            return false;
		}

        private void Checkbox(float x, float y, Expression<Func<bool>> binding, int title, int tooltip)
        {
            Checkboxes.Add(new UICheckBox(x, y, binding, Fonts.Arial12Bold, title, tooltip));
        }

		public override void LoadContent()
		{
            int width  = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;

            if (width <= 1366 || height <= 720)
				LowRes = true;
            var titleRect = new Rectangle(width / 2 - 203, (LowRes ? 10 : 44), 406, 80);
            var nameRect  = new Rectangle(width / 2 - height / 4, titleRect.Y + titleRect.Height + 5, width / 2, 150);
            var leftRect  = new Rectangle(width / 2 - height / 4, nameRect.Y + nameRect.Height + 5, width / 2,
                                              height - (titleRect.Y + titleRect.Height) - (int)(0.28f * height));
			if (leftRect.Height > 580)
				leftRect.Height = 580;

		    int x = leftRect.X + 60;

			close = new CloseButton(new Rectangle(leftRect.X + leftRect.Width - 40, leftRect.Y + 20, 20, 20));

            var ftlRect = new Rectangle(x, leftRect.Y + 100, 270, 50);
			FTLPenaltySlider = new FloatSlider(ftlRect, Localizer.Token(4007));
			FTLPenaltySlider.Amount = GlobalStats.FTLInSystemModifier;

            var eftlRect = new Rectangle(x, leftRect.Y + 150, 270, 50);
            EnemyFTLPenaltySlider = new FloatSlider(eftlRect, text:6139);
            EnemyFTLPenaltySlider.Amount = GlobalStats.EnemyFTLInSystemModifier;

            Checkboxes.Clear();
            Checkbox(ftlRect.X, ftlRect.Y + 100, () => GlobalStats.PlanetaryGravityWells, title: 4008, tooltip: 2288);
            Checkbox(ftlRect.X + 420, ftlRect.Y, () => GlobalStats.PreventFederations,    title: 6022, tooltip: 7011);
            Checkbox(ftlRect.X + 420, eftlRect.Y,() => GlobalStats.WarpInSystem,          title: 6178, tooltip: 6178);

            var gwRect = new Rectangle(x, leftRect.Y + 220, 270, 50);
            var epRect = new Rectangle(x, leftRect.Y + 280, 270, 50);
            var richnessRect = new Rectangle(x, leftRect.Y + 340, 270, 50);

            GravityWellSize = new FloatSlider(gwRect, Localizer.Token(6002), 0, 20000, GlobalStats.GravityWellRange);
            extraPlanets = new FloatSlider(epRect, "Extra Planets",0,6f, GlobalStats.ExtraPlanets);
            StartingPlanetRichness = new FloatSlider(richnessRect, "Starting Planet Richness Bonus", 0, 5f, GlobalStats.StartingPlanetRichness);


            var optionTurnTimer  = new Rectangle(leftRect.X + 460, leftRect.Y + 220, 270, 50);
            var minimumWarpRange = new Rectangle(leftRect.X + 460, leftRect.Y + 280, 270, 50);
            var maintenanceRect  = new Rectangle(leftRect.X + 460, leftRect.Y + 340, 270, 50);

            TurnTimer = new FloatSlider(optionTurnTimer, "Change Turn Timer", 2f, 18f, GlobalStats.TurnTimer);
            MinimumWarpRange = new FloatSlider(minimumWarpRange, "Minimum Warp Range", 0, 1200000f, GlobalStats.MinimumWarpRange);
            IncreaseMaintenance = new FloatSlider(maintenanceRect, "Increase Maintenance", 1, 10f, GlobalStats.ShipMaintenanceMulti);

			MainMenu = new Menu2(leftRect);
		}
	}
}