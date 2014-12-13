using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class BudgetScreen : GameScreen, IDisposable
	{
		private Vector2 Cursor = Vector2.Zero;

		private CloseButton close;

		private UniverseScreen screen;

		private Menu2 window;

		private List<UIButton> Buttons = new List<UIButton>();

		private Rectangle TaxRateRect;

		private Rectangle CostRect;

		private Rectangle IncomesRect;

		private Rectangle TradeRect;

		private GenericSlider TaxSlider;

		private MouseState currentMouse;

		private MouseState previousMouse;

		//private float transitionElapsedTime;

		public BudgetScreen(UniverseScreen screen)
		{
			this.screen = screen;
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
			this.window.Draw();
			string title = Localizer.Token(310);
			Vector2 Cursor = new Vector2((float)(this.window.Menu.X + this.window.Menu.Width / 2) - Fonts.Arial12Bold.MeasureString(title).X / 2f, (float)(this.window.Menu.Y + 20));
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, title, Cursor, Color.White);
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.TaxRateRect, new Color(17, 21, 28));
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.IncomesRect, new Color(18, 29, 29));
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.TradeRect, new Color(30, 26, 19));
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.CostRect, new Color(27, 22, 25));
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing * 2);
			Cursor.X = (float)(this.window.Menu.X + 30);
			this.TaxSlider.UpdatePosition(Cursor, 313, 12, string.Concat(Localizer.Token(311), " : "));
			this.TaxSlider.amount = EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.TaxRate;
			this.TaxSlider.DrawPct(base.ScreenManager);
			Cursor = new Vector2((float)(this.IncomesRect.X + 10), (float)(this.IncomesRect.Y + 8));
			HelperFunctions.DrawDropShadowText(base.ScreenManager, Localizer.Token(312), Cursor, Fonts.Arial12Bold);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			Vector2 ColumnB = Cursor;
			ColumnB.X = Cursor.X + 150f;
			float incomes = 0f;
			incomes = incomes + this.screen.player.GetPlanetIncomes();
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(313), ": "), Cursor, Color.White);
			ColumnB.X = ColumnB.X - Fonts.Arial12Bold.MeasureString(incomes.ToString("#.0")).X;
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, incomes.ToString("#.0"), ColumnB, Color.White);
			Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
			ColumnB = Cursor;
			ColumnB.X = Cursor.X + 150f;
			float flatMoney = EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.FlatMoneyBonus;
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Bonus: ", Cursor, Color.White);
			ColumnB.X = ColumnB.X - Fonts.Arial12Bold.MeasureString(flatMoney.ToString("#.0")).X;
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, flatMoney.ToString("#.0"), ColumnB, Color.White);
			incomes = incomes + flatMoney;
			Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
			Cursor.Y = Cursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
			Cursor = new Vector2((float)(this.IncomesRect.X + this.IncomesRect.Width - 75), (float)(this.IncomesRect.Y + this.IncomesRect.Height - Fonts.Arial12Bold.LineSpacing - 5));
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(314), ": ", incomes.ToString("#.0")), Cursor, Color.White);
			Cursor = new Vector2((float)(this.CostRect.X + 10), (float)(this.CostRect.Y + 8));
			HelperFunctions.DrawDropShadowText(base.ScreenManager, Localizer.Token(315), Cursor, Fonts.Arial12Bold);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			ColumnB = Cursor;
			ColumnB.X = Cursor.X + 150f;
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(316), ": "), Cursor, Color.White);
			float x = ColumnB.X;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			float totalBuildingMaintenance = EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetTotalBuildingMaintenance();
			ColumnB.X = x - arial12Bold.MeasureString(totalBuildingMaintenance.ToString("#.0")).X;
			SpriteBatch spriteBatch = base.ScreenManager.SpriteBatch;
			SpriteFont spriteFont = Fonts.Arial12Bold;
			float single = EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetTotalBuildingMaintenance();
			spriteBatch.DrawString(spriteFont, single.ToString("#.0"), ColumnB, Color.White);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			ColumnB = Cursor;
			ColumnB.X = Cursor.X + 150f;
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(317), ": "), Cursor, Color.White);
			float x1 = ColumnB.X;
			SpriteFont arial12Bold1 = Fonts.Arial12Bold;
			float totalShipMaintenance = EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetTotalShipMaintenance();
			ColumnB.X = x1 - arial12Bold1.MeasureString(totalShipMaintenance.ToString("#.0")).X;
			SpriteBatch spriteBatch1 = base.ScreenManager.SpriteBatch;
			SpriteFont spriteFont1 = Fonts.Arial12Bold;
			float totalShipMaintenance1 = EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetTotalShipMaintenance();
			spriteBatch1.DrawString(spriteFont1, totalShipMaintenance1.ToString("#.0"), ColumnB, Color.White);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			Cursor = new Vector2((float)(this.CostRect.X + this.CostRect.Width - 75), (float)(this.CostRect.Y + this.CostRect.Height - Fonts.Arial12Bold.LineSpacing - 5));
			SpriteBatch spriteBatch2 = base.ScreenManager.SpriteBatch;
			SpriteFont arial12Bold2 = Fonts.Arial12Bold;
			string str = Localizer.Token(320);
			float totalBuildingMaintenance1 = EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetTotalBuildingMaintenance() + EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetTotalShipMaintenance();
			spriteBatch2.DrawString(arial12Bold2, string.Concat(str, ": ", totalBuildingMaintenance1.ToString("#.0")), Cursor, Color.White);
			int numTreaties = 0;
			Cursor = new Vector2((float)(this.TradeRect.X + 10), (float)(this.TradeRect.Y + 8));
			HelperFunctions.DrawDropShadowText(base.ScreenManager, Localizer.Token(321), Cursor, Fonts.Arial12Bold);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			ColumnB = Cursor;
			ColumnB.X = Cursor.X + 150f;
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(322), ": "), Cursor, Color.White);
			float single1 = ColumnB.X;
			SpriteFont spriteFont2 = Fonts.Arial12Bold;
			int averageTradeIncome = EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetAverageTradeIncome();
			ColumnB.X = single1 - spriteFont2.MeasureString(averageTradeIncome.ToString("#.0")).X;
			SpriteBatch spriteBatch3 = base.ScreenManager.SpriteBatch;
			SpriteFont arial12Bold3 = Fonts.Arial12Bold;
			int num = EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetAverageTradeIncome();
			spriteBatch3.DrawString(arial12Bold3, num.ToString("#.0"), ColumnB, Color.White);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(323), ": "), Cursor, Color.White);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			Cursor.X = Cursor.X + 5f;
			float TotalTradeIncome = 0f;
			foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations())
			{
				if (!Relationship.Value.Treaty_Trade)
				{
					continue;
				}
				numTreaties++;
				float TradeValue = -3f + 0.25f * (float)Relationship.Value.Treaty_Trade_TurnsExisted;
				if (TradeValue > 3f)
				{
					TradeValue = 3f;
				}
				TotalTradeIncome = TotalTradeIncome + TradeValue;
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Relationship.Key.data.Traits.Plural, ": "), Cursor, Relationship.Key.EmpireColor);
				ColumnB = Cursor;
				ColumnB.X = Cursor.X + 150f;
				ColumnB.X = ColumnB.X - Fonts.Arial12Bold.MeasureString(TradeValue.ToString("#.0")).X;
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, TradeValue.ToString("#.0"), ColumnB, Color.White);
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			TotalTradeIncome = TotalTradeIncome + (float)EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetAverageTradeIncome();
			Cursor = new Vector2((float)(this.TradeRect.X + this.TradeRect.Width - 75), (float)(this.TradeRect.Y + this.TradeRect.Height - Fonts.Arial12Bold.LineSpacing - 5));
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(320), ":  ", TotalTradeIncome.ToString("#.0")), Cursor, Color.White);
			Cursor = new Vector2((float)(this.window.Menu.X + this.window.Menu.Width - 170), (float)(this.window.Menu.Y + this.window.Menu.Height - 47));
			float net = incomes + (float)EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetAverageTradeIncome() - (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetTotalBuildingMaintenance() + EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetTotalShipMaintenance());
			net = net + TotalTradeIncome;
			net = this.screen.player.EstimateIncomeAtTaxRate(this.screen.player.data.TaxRate);
			string words = "";
			if (net <= 0f)
			{
				words = string.Concat(Localizer.Token(325), ": ", net.ToString("#.0"));
				HelperFunctions.DrawDropShadowText(base.ScreenManager, words, Cursor, Fonts.Arial20Bold);
			}
			else
			{
				words = string.Concat(Localizer.Token(324), ": ", net.ToString("#.0"));
				HelperFunctions.DrawDropShadowText(base.ScreenManager, words, Cursor, Fonts.Arial20Bold);
			}
			foreach (UIButton b in this.Buttons)
			{
				b.Draw(base.ScreenManager.SpriteBatch);
			}
			this.close.Draw(base.ScreenManager);
			ToolTip.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			base.ExitScreen();
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
        ~BudgetScreen() {
            //should implicitly do the same thing as the original bad finalize
        }

		public override void HandleInput(InputState input)
		{
			this.currentMouse = input.CurrentMouseState;
			Vector2 vector2 = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
            if (input.CurrentKeyboardState.IsKeyDown(Keys.T) && !input.LastKeyboardState.IsKeyDown(Keys.T) && !GlobalStats.TakingInput)
            {
                AudioManager.PlayCue("echo_affirm");
                this.ExitScreen();
                return;
            }
			if (this.close.HandleInput(input))
			{
				this.ExitScreen();
				return;
			}
			this.TaxSlider.HandleInput(input);
			EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.TaxRate = this.TaxSlider.amount;
			foreach (Planet p in EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetPlanets())
			{
				p.UpdateIncomes();
			}
			if (input.Escaped)
			{
				this.ExitScreen();
			}
			if (HelperFunctions.CheckIntersection(this.TaxSlider.rect, input.CursorPosition))
			{
				ToolTip.CreateTooltip(66, base.ScreenManager);
			}
			if (input.CurrentMouseState.RightButton == ButtonState.Released && input.LastMouseState.RightButton == ButtonState.Pressed)
			{
				this.ExitScreen();
			}
			this.previousMouse = input.LastMouseState;
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			this.window = new Menu2(base.ScreenManager, new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 197, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 225, 394, 450));
			this.close = new CloseButton(new Rectangle(this.window.Menu.X + this.window.Menu.Width - 40, this.window.Menu.Y + 20, 20, 20));
			Rectangle rectangle = new Rectangle();
			this.TaxSlider = new GenericSlider(rectangle, Localizer.Token(309), 0f, 100f)
			{
				amount = EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.TaxRate
			};
			this.TaxRateRect = new Rectangle(this.window.Menu.X + 20, this.window.Menu.Y + 37, 350, 52);
			this.IncomesRect = new Rectangle(this.TaxRateRect.X, this.TaxRateRect.Y + this.TaxRateRect.Height + 6, 168, 118);
			this.TradeRect = new Rectangle(this.TaxRateRect.X, this.IncomesRect.Y + this.IncomesRect.Height + 6, 168, 208);
			this.CostRect = new Rectangle(this.TaxRateRect.X + 12 + this.IncomesRect.Width, this.IncomesRect.Y, 168, 118);
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}