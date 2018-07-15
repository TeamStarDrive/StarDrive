using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Linq.Expressions;
using Ship_Game.Ships;

namespace Ship_Game
{
	public sealed class RuleOptionsScreen : GameScreen
	{
		public bool isOpen;
		private Menu2 MainMenu;
		private bool LowRes;
		private FloatSlider FTLPenaltySlider;
        private FloatSlider EnemyFTLPenaltySlider;
        private FloatSlider GravityWellSize;
        private FloatSlider extraPlanets;
        private FloatSlider IncreaseMaintenance;
        private FloatSlider MinimumWarpRange;
        private FloatSlider StartingRichness;
        private FloatSlider TurnTimer;
		public Ship itemToBuild;

		public RuleOptionsScreen(GameScreen parent) : base(parent)
		{
			IsPopup = true;
			TransitionOnTime  = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public override void Draw(SpriteBatch batch)
		{
			ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
		    batch.Begin();
		    {
		        MainMenu.Draw(Color.Black);
		        base.Draw(batch);
		    }
		    batch.End();
		}

	
		public override bool HandleInput(InputState input)
		{
            if (base.HandleInput(input))
            {
                GlobalStats.FTLInSystemModifier      = FTLPenaltySlider.RelativeValue;
                GlobalStats.EnemyFTLInSystemModifier = EnemyFTLPenaltySlider.RelativeValue;
                GlobalStats.GravityWellRange         = GravityWellSize.AbsoluteValue;
                GlobalStats.ExtraPlanets             = (int)extraPlanets.AbsoluteValue;
                GlobalStats.MinimumWarpRange         = MinimumWarpRange.AbsoluteValue;
                GlobalStats.ShipMaintenanceMulti     = IncreaseMaintenance.AbsoluteValue;
                GlobalStats.StartingPlanetRichness   = StartingRichness.AbsoluteValue;
                GlobalStats.TurnTimer                = (byte)TurnTimer.AbsoluteValue;
                return true;
            }
            return false;
		}

		public override void LoadContent()
		{
            base.LoadContent();
            RemoveAll();
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

			CloseButton(leftRect.X + leftRect.Width - 40, leftRect.Y + 20);

            var ftlRect = new Rectangle(x, leftRect.Y + 100, 270, 50);
			FTLPenaltySlider = SliderPercent(ftlRect, Localizer.Token(4007), 0f, 1f, GlobalStats.FTLInSystemModifier);

            var eftlRect = new Rectangle(x, leftRect.Y + 150, 270, 50);
            EnemyFTLPenaltySlider = SliderPercent(eftlRect, Localizer.Token(6139), 0f, 1f, GlobalStats.EnemyFTLInSystemModifier);

            Checkbox(ftlRect.X, ftlRect.Y + 100, () => GlobalStats.PlanetaryGravityWells, title: 4008, tooltip: 2288);
            Checkbox(ftlRect.X + 420, ftlRect.Y, () => GlobalStats.PreventFederations,    title: 6022, tooltip: 7011);
            Checkbox(ftlRect.X + 420, eftlRect.Y,() => GlobalStats.WarpInSystem,          title: 6178, tooltip: 6178);

            var gwRect = new Rectangle(x, leftRect.Y + 220, 270, 50);
            var epRect = new Rectangle(x, leftRect.Y + 280, 270, 50);
            var richnessRect = new Rectangle(x, leftRect.Y + 340, 270, 50);

            GravityWellSize  = Slider(gwRect, Localizer.Token(6002), 0, 20000, GlobalStats.GravityWellRange);
            extraPlanets     = Slider(epRect, "Extra Planets", 0, 6f, GlobalStats.ExtraPlanets);
            StartingRichness = Slider(richnessRect, "Starting Planet Richness Bonus", 0, 5f, GlobalStats.StartingPlanetRichness);


            var optionTurnTimer  = new Rectangle(leftRect.X + 460, leftRect.Y + 220, 270, 50);
            var minimumWarpRange = new Rectangle(leftRect.X + 460, leftRect.Y + 280, 270, 50);
            var maintenanceRect  = new Rectangle(leftRect.X + 460, leftRect.Y + 340, 270, 50);

            TurnTimer           = Slider(optionTurnTimer,  "Change Turn Timer",    2f, 18f,     GlobalStats.TurnTimer);
            MinimumWarpRange    = Slider(minimumWarpRange, "Minimum Warp Range",   0, 1200000f, GlobalStats.MinimumWarpRange);
            IncreaseMaintenance = Slider(maintenanceRect,  "Increase Maintenance", 1, 10f,      GlobalStats.ShipMaintenanceMulti);

		    FTLPenaltySlider.TooltipId = 2286;
		    EnemyFTLPenaltySlider.TooltipId = 7041;
		    GravityWellSize.TooltipId = 6003;
		    extraPlanets.Tooltip = "Add up to 6 random planets to each system";
		    MinimumWarpRange.Tooltip = "Minumum warp range a ship must have before it needs to recharge for the AI to build it";

            IncreaseMaintenance.Tooltip = "Multiply Global Maintenance Cost By  SSP's Are Not Affected";
		    TurnTimer.Tooltip = "Time in seconds for turns";
		    StartingRichness.Tooltip = "Add to all Starting Empire Planets this Value";

			MainMenu = new Menu2(leftRect);

            Label(MainMenu.Menu.X + 40, MainMenu.Menu.Y + 40, "Advanced Rule Options", Fonts.Arial20Bold);
		    string text = HelperFunctions.ParseText(Fonts.Arial12, Localizer.Token(2289), MainMenu.Menu.Width - 80);
            Label(MainMenu.Menu.X + 40, MainMenu.Menu.Y + 40 + Fonts.Arial20Bold.LineSpacing + 2, text, Fonts.Arial12);
		}
	}
}