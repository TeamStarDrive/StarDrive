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

        private FloatSlider GravityWellSize;

        private FloatSlider extraPlanets;

        private FloatSlider OptionIncreaseShipMaintenance;
        private FloatSlider MinimumWarpRange;
        private FloatSlider MemoryLimiter;
        private FloatSlider StartingPlanetRichness;

		public Ship itemToBuild;

        //        public static float OptionIncreaseShipMaintenance;
        //public static float MinimumWarpRange;

        //public static float MemoryLimiter;

        //public static float StartingPlanetRichness;

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
            this.GravityWellSize.Draw(base.ScreenManager);
            this.extraPlanets.Draw(base.ScreenManager);
            this.OptionIncreaseShipMaintenance.Draw(base.ScreenManager);
            this.MinimumWarpRange.Draw(base.ScreenManager);
            this.MemoryLimiter.Draw(base.ScreenManager);
            this.StartingPlanetRichness.Draw(base.ScreenManager);
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
            if (HelperFunctions.CheckIntersection(this.MemoryLimiter.ContainerRect, input.CursorPosition))
            {
                ToolTip.CreateTooltip("Constrain the AI to only build offensive ships when this memory *10 is not exceeded", base.ScreenManager);
            }
            this.MemoryLimiter.HandleInput(input);
            GlobalStats.MemoryLimiter = this.MemoryLimiter.amountRange;

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
            GlobalStats.OptionIncreaseShipMaintenance = this.OptionIncreaseShipMaintenance.amountRange;
            
            if (HelperFunctions.CheckIntersection(this.StartingPlanetRichness.ContainerRect, input.CursorPosition))
            {
                ToolTip.CreateTooltip("Add to all Stating Empire Planets this Value", base.ScreenManager);
            }
            this.StartingPlanetRichness.HandleInput(input);
            GlobalStats.StartingPlanetRichness = this.StartingPlanetRichness.amountRange;

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
			Checkbox cb = new Checkbox(new Vector2((float)ftlRect.X, (float)(ftlRect.Y + 85)), Localizer.Token(4008), acomRef, Fonts.Arial12Bold);
			this.Checkboxes.Add(cb);
			cb.Tip_Token = 2288;
            //Added by McShooterz: new checkbox to prevent AI federations
            Ref<bool> pfRef = new Ref<bool>(() => GlobalStats.preventFederations, (bool x) => GlobalStats.preventFederations = x);
            Checkbox cb2 = new Checkbox(new Vector2((float)(ftlRect.X + 500), (float)(ftlRect.Y)), Localizer.Token(6022), pfRef, Fonts.Arial12Bold);
            this.Checkboxes.Add(cb2);
            cb2.Tip_Token = 7011;

            Rectangle gwRect = new Rectangle(leftRect.X + 60, leftRect.Y + 220, 270, 50);
            this.GravityWellSize = new FloatSlider(gwRect, Localizer.Token(6002),0,20000,GlobalStats.GravityWellRange);
            //this.GravityWellSize.SetAmountGW(GlobalStats.GravityWellRange);
            //this.GravityWellSize.amount = GlobalStats.GravityWellRange;
            
            //added by gremlin init extra planets slider
            Rectangle epRect = new Rectangle(leftRect.X + 60, leftRect.Y + 280, 270, 50);
            this.extraPlanets = new FloatSlider(epRect, "Extra Planets",0,6f,(float)GlobalStats.ExtraPlanets);

            Rectangle StartingPlanetRichness = new Rectangle(leftRect.X  + 60, leftRect.Y + 340, 270, 50);
            this.StartingPlanetRichness = new FloatSlider(StartingPlanetRichness, "Starting Planet Richness Bonus", 0, 5f, GlobalStats.StartingPlanetRichness);
            Rectangle MinimumWarpRange = new Rectangle(leftRect.X *2 + 60, leftRect.Y + 340, 270, 50);
            this.MinimumWarpRange = new FloatSlider(MinimumWarpRange, "Minimum Warp Range", 0, 1200000f, GlobalStats.MinimumWarpRange);
            Rectangle MemoryLimiter = new Rectangle(leftRect.X + 60, leftRect.Y + 400, 270, 50);
            this.MemoryLimiter = new FloatSlider(MemoryLimiter, "Memory Limit", 150000, 300000f, GlobalStats.MemoryLimiter);
            Rectangle OptionIncreaseShipMaintenance = new Rectangle(leftRect.X *2 + 60, leftRect.Y + 400, 270, 50);
            this.OptionIncreaseShipMaintenance = new FloatSlider(OptionIncreaseShipMaintenance, "Increase Maintenance", 1, 10f, GlobalStats.OptionIncreaseShipMaintenance);
           
			this.MainMenu = new Menu2(base.ScreenManager, leftRect);
		}
	}
}