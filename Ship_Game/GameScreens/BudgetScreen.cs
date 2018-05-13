using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;

namespace Ship_Game.GameScreens
{
    public sealed class BudgetScreen : GameScreen
    {
        private Vector2 Cursor = Vector2.Zero;

        private CloseButton close;

        private UniverseScreen screen;

        private Menu2 window;

        private Rectangle TaxRateRect;

        private Rectangle CostRect;

        private Rectangle IncomesRect;

        private Rectangle TradeRect;

        private GenericSlider TaxSlider;
        private GenericSlider TreasuryGoal;

        private MouseState currentMouse;

        private MouseState previousMouse;

        //private float transitionElapsedTime;

        public BudgetScreen(UniverseScreen screen) : base(screen)
        {
            this.screen = screen;
            base.IsPopup = true;
          
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
            base.ScreenManager.SpriteBatch.Begin();
            this.window.Draw();
            string title = Localizer.Token(310);
            Vector2 Cursor = new Vector2((float)(this.window.Menu.X + this.window.Menu.Width / 2) - Fonts.Arial12Bold.MeasureString(title).X / 2f, (float)(this.window.Menu.Y + 20));
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, title, Cursor, Color.White);
            base.ScreenManager.SpriteBatch.FillRectangle(this.TaxRateRect, new Color(17, 21, 28));
            base.ScreenManager.SpriteBatch.FillRectangle(this.IncomesRect, new Color(18, 29, 29));
            base.ScreenManager.SpriteBatch.FillRectangle(this.TradeRect, new Color(30, 26, 19));
            base.ScreenManager.SpriteBatch.FillRectangle(this.CostRect, new Color(27, 22, 25));
            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing * 2);
            Cursor.X = (float)(this.window.Menu.X + 30);
            this.TaxSlider.UpdatePosition(Cursor, 313, 12, string.Concat(Localizer.Token(311), " : "));
            this.TaxSlider.amount = EmpireManager.Player.data.TaxRate;
            this.TaxSlider.DrawPct(base.ScreenManager);

            //treasury Slider
            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing * 2);
            Cursor.X = (float)(this.window.Menu.X + 30);
            this.TreasuryGoal.UpdatePosition(Cursor, 313, 12, string.Concat("Auto Tax Treasury Goal : ", (int)(100 * EmpireManager.Player.GrossTaxes * this.TreasuryGoal.amount)));
            this.TreasuryGoal.amount = EmpireManager.Player.data.treasuryGoal;
            this.TreasuryGoal.DrawPct(base.ScreenManager);

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
            float flatMoney = EmpireManager.Player.data.FlatMoneyBonus;
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
            float totalBuildingMaintenance = EmpireManager.Player.GetTotalBuildingMaintenance();
            ColumnB.X = x - arial12Bold.MeasureString(totalBuildingMaintenance.ToString("#.0")).X;

            SpriteFont spriteFont = Fonts.Arial12Bold;
            float single = EmpireManager.Player.GetTotalBuildingMaintenance();
            spriteBatch.DrawString(spriteFont, single.ToString("#.0"), ColumnB, Color.White);
            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            ColumnB = Cursor;
            ColumnB.X = Cursor.X + 150f;
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(317), ": "), Cursor, Color.White);
            float x1 = ColumnB.X;
            SpriteFont arial12Bold1 = Fonts.Arial12Bold;
            float totalShipMaintenance = EmpireManager.Player.GetTotalShipMaintenance();
            ColumnB.X = x1 - arial12Bold1.MeasureString(totalShipMaintenance.ToString("#.0")).X;
            SpriteBatch spriteBatch1 = base.ScreenManager.SpriteBatch;
            SpriteFont spriteFont1 = Fonts.Arial12Bold;
            float totalShipMaintenance1 = EmpireManager.Player.GetTotalShipMaintenance();
            spriteBatch1.DrawString(spriteFont1, totalShipMaintenance1.ToString("#.0"), ColumnB, Color.White);
            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            Cursor = new Vector2((float)(this.CostRect.X + this.CostRect.Width - 75), (float)(this.CostRect.Y + this.CostRect.Height - Fonts.Arial12Bold.LineSpacing - 5));
            SpriteBatch spriteBatch2 = base.ScreenManager.SpriteBatch;
            SpriteFont arial12Bold2 = Fonts.Arial12Bold;
            string str = Localizer.Token(320);
            float totalBuildingMaintenance1 = EmpireManager.Player.GetTotalBuildingMaintenance() + EmpireManager.Player.GetTotalShipMaintenance();
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
            int averageTradeIncome = EmpireManager.Player.GetAverageTradeIncome();
            ColumnB.X = single1 - spriteFont2.MeasureString(averageTradeIncome.ToString("#.0")).X;
            SpriteBatch spriteBatch3 = base.ScreenManager.SpriteBatch;
            SpriteFont arial12Bold3 = Fonts.Arial12Bold;
            int num = EmpireManager.Player.GetAverageTradeIncome();
            spriteBatch3.DrawString(arial12Bold3, num.ToString("#.0"), ColumnB, Color.White);
            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(323), ": "), Cursor, Color.White);
            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            Cursor.X = Cursor.X + 5f;
            float TotalTradeIncome = 0f;
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in EmpireManager.Player.AllRelations)
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
            TotalTradeIncome = TotalTradeIncome + (float)EmpireManager.Player.GetAverageTradeIncome();
            Cursor = new Vector2((float)(this.TradeRect.X + this.TradeRect.Width - 75), (float)(this.TradeRect.Y + this.TradeRect.Height - Fonts.Arial12Bold.LineSpacing - 5));
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(320), ":  ", TotalTradeIncome.ToString("#.0")), Cursor, Color.White);
            Cursor = new Vector2((float)(this.window.Menu.X + this.window.Menu.Width - 170), (float)(this.window.Menu.Y + this.window.Menu.Height - 47));
            float net = incomes + (float)EmpireManager.Player.GetAverageTradeIncome() - (EmpireManager.Player.GetTotalBuildingMaintenance() + EmpireManager.Player.GetTotalShipMaintenance());
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
            this.close.Draw(ScreenManager.SpriteBatch);
            ToolTip.Draw(ScreenManager.SpriteBatch);
            base.ScreenManager.SpriteBatch.End();
        }

        public override void ExitScreen()
        {
            base.ExitScreen();
        }

        //protected override void Finalize()
        //{
        //    try
        //    {
        //        this.Dispose(false);
        //    }
        //    catch
        //    {
        //        //base.Finalize();
        //    }
        //}

        public override bool HandleInput(InputState input)
        {
            currentMouse = input.MouseCurr;
            if (input.KeysCurr.IsKeyDown(Keys.T) && !input.KeysPrev.IsKeyDown(Keys.T) && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                ExitScreen();
                return true;
            }
            if (close.HandleInput(input))
            {
                ExitScreen();
                return true;
            }
            TreasuryGoal.HandleInput(input);

            Empire empire = EmpireManager.Player;
            empire.data.treasuryGoal = TreasuryGoal.amount;
            TaxSlider.HandleInput(input);
            empire.data.TaxRate = TaxSlider.amount;
            empire.UpdatePlanetIncomes();
            if (input.Escaped)
            {
                ExitScreen();
                return true;
            }
            if (TaxSlider.rect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(66);
            }
            if (TreasuryGoal.rect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(66);
            }
            if (input.MouseCurr.RightButton == ButtonState.Released && input.MousePrev.RightButton == ButtonState.Pressed)
            {
                ExitScreen();
                return true;
            }
            previousMouse = input.MousePrev;
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            this.window = new Menu2(new Rectangle(ScreenWidth / 2 - 197, ScreenHeight / 2 - 225, 394, 450));
            this.close = new CloseButton(this, new Rectangle(this.window.Menu.X + this.window.Menu.Width - 40, this.window.Menu.Y + 20, 20, 20));
            Rectangle rectangle = new Rectangle();
            this.TaxSlider = new GenericSlider(rectangle, Localizer.Token(309), 0f, 100f)
            {
                amount = EmpireManager.Player.data.TaxRate
            };
            Rectangle rectangle2 = new Rectangle(rectangle.X + 20, rectangle.Y + 37, rectangle.Width, rectangle.Height);
            this.TreasuryGoal = new GenericSlider(rectangle2, "Treasury Goal", 1f, 20f)
            {
                amount = EmpireManager.Player.data.treasuryGoal
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