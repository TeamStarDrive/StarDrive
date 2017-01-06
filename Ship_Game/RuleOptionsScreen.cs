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

		private Array<Checkbox> Checkboxes = new Array<Checkbox>();

		private Menu2 MainMenu;

		private bool LowRes;

		private FloatSlider FTLPenaltySlider;
        private FloatSlider EnemyFTLPenaltySlider;

		private CloseButton close;

        private FloatSlider GravityWellSize;

        private FloatSlider extraPlanets;

        private FloatSlider OptionIncreaseShipMaintenance;
        private FloatSlider MinimumWarpRange;
        private FloatSlider StartingPlanetRichness;

        private FloatSlider TurnTimer;

		public Ship itemToBuild;

        //        public static float OptionIncreaseShipMaintenance;
        //public static float MinimumWarpRange;

        //public static float MemoryLimiter;

        //public static float StartingPlanetRichness;

		public RuleOptionsScreen(GameScreen parent) : base(parent)
		{
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
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
			text = HelperFunctions.ParseText(Fonts.Arial12, text, (float)(this.MainMenu.Menu.Width - 80));
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, TitlePos, Color.White);
			this.FTLPenaltySlider.DrawDecimal(base.ScreenManager);
            this.EnemyFTLPenaltySlider.DrawDecimal(base.ScreenManager);
            this.GravityWellSize.Draw(base.ScreenManager);
            this.extraPlanets.Draw(base.ScreenManager);
            this.OptionIncreaseShipMaintenance.Draw(base.ScreenManager);
            this.MinimumWarpRange.Draw(base.ScreenManager);
            this.StartingPlanetRichness.Draw(base.ScreenManager);
            this.TurnTimer.Draw(base.ScreenManager);
			this.close.Draw(base.ScreenManager);
			foreach (Checkbox cb in this.Checkboxes)
			{
				cb.Draw(base.ScreenManager);
			}
			ToolTip.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
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
            
            if (HelperFunctions.CheckIntersection(this.EnemyFTLPenaltySlider.ContainerRect, input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(7041), base.ScreenManager);
            }
            this.EnemyFTLPenaltySlider.HandleInput(input);
            GlobalStats.EnemyFTLInSystemModifier = this.EnemyFTLPenaltySlider.amount;
			foreach (Checkbox cb in this.Checkboxes)
			{
				cb.HandleInput(input);
			}
            if (HelperFunctions.CheckIntersection(this.GravityWellSize.ContainerRect, input.CursorPosition))
            {
                ToolTip.CreateTooltip(Localizer.Token(6003), base.ScreenManager);
            }
            this.GravityWellSize.HandleInput(input);
            GlobalStats.GravityWellRange = this.GravityWellSize.amountRange; //amount replaced with amountRange
           
            //added by gremlin ExtraPlanets
            if (HelperFunctions.CheckIntersection(this.extraPlanets.ContainerRect, input.CursorPosition))
            {
                ToolTip.CreateTooltip("Add up to 6 random planets to each system", base.ScreenManager);
            }
            this.extraPlanets.HandleInput(input);
            GlobalStats.ExtraPlanets = (int)this.extraPlanets.amountRange;
//new options

            if (HelperFunctions.CheckIntersection(this.MinimumWarpRange.ContainerRect, input.CursorPosition))
            {
                ToolTip.CreateTooltip("Minumum warp range a ship must have before it needs to recharge for the AI to build it", base.ScreenManager);
            }
            this.MinimumWarpRange.HandleInput(input);
            GlobalStats.MinimumWarpRange = this.MinimumWarpRange.amountRange;
            
            if (HelperFunctions.CheckIntersection(this.OptionIncreaseShipMaintenance.ContainerRect, input.CursorPosition))
            {
                ToolTip.CreateTooltip("Multiply Global Maintenance Cost By This. SSP's Are Not Affected", base.ScreenManager);
            }
            this.OptionIncreaseShipMaintenance.HandleInput(input);
            GlobalStats.ShipMaintenanceMulti = this.OptionIncreaseShipMaintenance.amountRange;
            
            if (HelperFunctions.CheckIntersection(this.StartingPlanetRichness.ContainerRect, input.CursorPosition))
            {
                ToolTip.CreateTooltip("Add to all Starting Empire Planets this Value", base.ScreenManager);
            }
            this.StartingPlanetRichness.HandleInput(input);
            GlobalStats.StartingPlanetRichness = this.StartingPlanetRichness.amountRange;

            if (HelperFunctions.CheckIntersection(this.TurnTimer.ContainerRect, input.CursorPosition))
            {
                ToolTip.CreateTooltip("Time in seconds for turns", base.ScreenManager);
            }
            this.TurnTimer.HandleInput(input);
            GlobalStats.TurnTimer = (byte)this.TurnTimer.amountRange;
		}

        private void Checkbox(float x, float y, Expression<Func<bool>> binding, int title, int tooltip)
        {
            Checkboxes.Add(new Checkbox(x, y, binding, Fonts.Arial12Bold, title, tooltip));
        }

		public override void LoadContent()
		{
            int width  = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;

            if (width <= 1366 || height <= 720)
				LowRes = true;
			Rectangle titleRect = new Rectangle(width / 2 - 203, (LowRes ? 10 : 44), 406, 80);
			Rectangle nameRect = new Rectangle(width / 2 - height / 4, titleRect.Y + titleRect.Height + 5, width / 2, 150);
			Rectangle leftRect = new Rectangle(width / 2 - height / 4, nameRect.Y + nameRect.Height + 5, width / 2,
                                              height - (titleRect.Y + titleRect.Height) - (int)(0.28f * height));
			if (leftRect.Height > 580)
				leftRect.Height = 580;
			close = new CloseButton(new Rectangle(leftRect.X + leftRect.Width - 40, leftRect.Y + 20, 20, 20));

			Rectangle ftlRect = new Rectangle(leftRect.X + 60, leftRect.Y + 100, 270, 50);
			FTLPenaltySlider = new FloatSlider(ftlRect, Localizer.Token(4007));
			FTLPenaltySlider.SetAmount(GlobalStats.FTLInSystemModifier);

            Rectangle eftlRect = new Rectangle(leftRect.X + 60, leftRect.Y + 150, 270, 50);
            EnemyFTLPenaltySlider = new FloatSlider(eftlRect, text:6139);
            EnemyFTLPenaltySlider.SetAmount(GlobalStats.EnemyFTLInSystemModifier);

            Checkbox(ftlRect.X, ftlRect.Y + 100, () => GlobalStats.PlanetaryGravityWells, title: 4008, tooltip: 2288);
            Checkbox(ftlRect.X + 500, ftlRect.Y, () => GlobalStats.PreventFederations,    title: 6022, tooltip: 7011);
            Checkbox(ftlRect.X + 500, eftlRect.Y,() => GlobalStats.WarpInSystem,          title: 6178, tooltip: 6178);

            Rectangle gwRect = new Rectangle(leftRect.X + 60, leftRect.Y + 220, 270, 50);
            GravityWellSize = new FloatSlider(gwRect, Localizer.Token(6002),0,20000,GlobalStats.GravityWellRange);
            
            //added by gremlin init extra planets slider
            Rectangle epRect = new Rectangle(leftRect.X + 60, leftRect.Y + 280, 270, 50);
            extraPlanets = new FloatSlider(epRect, "Extra Planets",0,6f,(float)GlobalStats.ExtraPlanets);

            Rectangle richnessRect = new Rectangle(leftRect.X  + 60, leftRect.Y + 340, 270, 50);
            Rectangle minimumWarpRange = new Rectangle(leftRect.X * 2 + 60, leftRect.Y + 340, 270, 50);
            Rectangle maintenanceRect = new Rectangle(leftRect.X * 2 + 60, leftRect.Y + 400, 270, 50);

            StartingPlanetRichness = new FloatSlider(richnessRect, "Starting Planet Richness Bonus", 0, 5f, GlobalStats.StartingPlanetRichness);
            MinimumWarpRange = new FloatSlider(minimumWarpRange, "Minimum Warp Range", 0, 1200000f, GlobalStats.MinimumWarpRange);
            OptionIncreaseShipMaintenance = new FloatSlider(maintenanceRect, "Increase Maintenance", 1, 10f, GlobalStats.ShipMaintenanceMulti);
            
            //Added by McShooterz: slider to change time for turns
            Rectangle optionTurnTimer = new Rectangle(leftRect.X * 2 + 60, leftRect.Y + 275, 270, 50);
            TurnTimer = new FloatSlider(optionTurnTimer, "Change Turn Timer", 2f, 18f, GlobalStats.TurnTimer);
           
			MainMenu = new Menu2(base.ScreenManager, leftRect);
		}
	}
}