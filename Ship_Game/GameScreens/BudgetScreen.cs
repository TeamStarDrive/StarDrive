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

        private CloseButton Close;

        private readonly UniverseScreen Screen;

        private Menu2 Window;

        private Rectangle TaxRateRect;

        private Rectangle CostRect;

        private Rectangle IncomesRect;

        private Rectangle TradeRect;

        private GenericSlider TaxSlider;

        private GenericSlider TreasuryGoal;

        public BudgetScreen(UniverseScreen screen) : base(screen)
        {
            Screen = screen;
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            ScreenManager.SpriteBatch.Begin();
            Window.Draw();
            string title = Localizer.Token(310);
            Vector2 cursor = new Vector2(Window.Menu.X + Window.Menu.Width / 2 - Fonts.Arial12Bold.MeasureString(title).X / 2f, Window.Menu.Y + 20);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, title, cursor, Color.White);
            ScreenManager.SpriteBatch.FillRectangle(TaxRateRect, new Color(17, 21, 28));
            ScreenManager.SpriteBatch.FillRectangle(IncomesRect, new Color(18, 29, 29));
            ScreenManager.SpriteBatch.FillRectangle(TradeRect, new Color(30, 26, 19));
            ScreenManager.SpriteBatch.FillRectangle(CostRect, new Color(27, 22, 25));
            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing * 2;
            cursor.X = Window.Menu.X + 30;
            TaxSlider.UpdatePosition(cursor, 313, 12, string.Concat(Localizer.Token(311), " : "));
            TaxSlider.amount = EmpireManager.Player.data.TaxRate;
            TaxSlider.DrawPct(ScreenManager);
            
            //treasury Slider
            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing * 2;
            cursor.X = Window.Menu.X + 30;
            TreasuryGoal.UpdatePosition(cursor, 313, 12, string.Concat("Auto Tax Treasury Goal : ", (int)(100* EmpireManager.Player.GrossTaxes * TreasuryGoal.amount)));
            TreasuryGoal.amount = EmpireManager.Player.data.treasuryGoal;
            TreasuryGoal.DrawPct(ScreenManager);

            cursor = new Vector2(IncomesRect.X + 10, IncomesRect.Y + 8);
            HelperFunctions.DrawDropShadowText(ScreenManager, Localizer.Token(312), cursor, Fonts.Arial12Bold);
            cursor.Y = cursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
            Vector2 columnB = cursor;
            columnB.X = cursor.X + 150f;
            float incomes = 0f;
            incomes = incomes + Screen.player.GetPlanetIncomes();
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(313), ": "), cursor, Color.White);
            columnB.X = columnB.X - Fonts.Arial12Bold.MeasureString(incomes.ToString("#.0")).X;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, incomes.ToString("#.0"), columnB, Color.White);
            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
            columnB = cursor;
            columnB.X = cursor.X + 150f;
            float flatMoney = EmpireManager.Player.data.FlatMoneyBonus;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Bonus: ", cursor, Color.White);
            columnB.X = columnB.X - Fonts.Arial12Bold.MeasureString(flatMoney.ToString("#.0")).X;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, flatMoney.ToString("#.0"), columnB, Color.White);
            incomes = incomes + flatMoney;
            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
            cursor.Y = cursor.Y + Fonts.Arial12Bold.LineSpacing;
            cursor = new Vector2(IncomesRect.X + IncomesRect.Width - 75, IncomesRect.Y + IncomesRect.Height - Fonts.Arial12Bold.LineSpacing - 5);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(314), ": ", incomes.ToString("#.0")), cursor, Color.White);
            cursor = new Vector2(CostRect.X + 10, CostRect.Y + 8);
            HelperFunctions.DrawDropShadowText(ScreenManager, Localizer.Token(315), cursor, Fonts.Arial12Bold);
            cursor.Y = cursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
            columnB = cursor;
            columnB.X = cursor.X + 150f;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(316), ": "), cursor, Color.White);
            float x = columnB.X;
            SpriteFont arial12Bold = Fonts.Arial12Bold;
            float totalBuildingMaintenance = EmpireManager.Player.GetTotalBuildingMaintenance();
            columnB.X = x - arial12Bold.MeasureString(totalBuildingMaintenance.ToString("#.0")).X;

            SpriteFont spriteFont = Fonts.Arial12Bold;
            float single = EmpireManager.Player.GetTotalBuildingMaintenance();
            spriteBatch.DrawString(spriteFont, single.ToString("#.0"), columnB, Color.White);
            cursor.Y = cursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
            columnB = cursor;
            columnB.X = cursor.X + 150f;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(317), ": "), cursor, Color.White);
            float x1 = columnB.X;
            SpriteFont arial12Bold1 = Fonts.Arial12Bold;
            float totalShipMaintenance = EmpireManager.Player.GetTotalShipMaintenance();
            columnB.X = x1 - arial12Bold1.MeasureString(totalShipMaintenance.ToString("#.0")).X;
            SpriteBatch spriteBatch1 = ScreenManager.SpriteBatch;
            SpriteFont spriteFont1 = Fonts.Arial12Bold;
            float totalShipMaintenance1 = EmpireManager.Player.GetTotalShipMaintenance();
            spriteBatch1.DrawString(spriteFont1, totalShipMaintenance1.ToString("#.0"), columnB, Color.White);
            cursor.Y = cursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
            cursor = new Vector2(CostRect.X + CostRect.Width - 75, CostRect.Y + CostRect.Height - Fonts.Arial12Bold.LineSpacing - 5);
            SpriteBatch spriteBatch2 = ScreenManager.SpriteBatch;
            SpriteFont arial12Bold2 = Fonts.Arial12Bold;
            string str = Localizer.Token(320);
            float totalBuildingMaintenance1 = EmpireManager.Player.GetTotalBuildingMaintenance() + EmpireManager.Player.GetTotalShipMaintenance();
            spriteBatch2.DrawString(arial12Bold2, string.Concat(str, ": ", totalBuildingMaintenance1.ToString("#.0")), cursor, Color.White);
            cursor = new Vector2(TradeRect.X + 10, TradeRect.Y + 8);
            HelperFunctions.DrawDropShadowText(ScreenManager, Localizer.Token(321), cursor, Fonts.Arial12Bold);
            cursor.Y = cursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
            columnB = cursor;
            columnB.X = cursor.X + 150f;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(322), ": "), cursor, Color.White);
            float single1 = columnB.X;
            SpriteFont spriteFont2 = Fonts.Arial12Bold;
            int averageTradeIncome = EmpireManager.Player.GetAverageTradeIncome();
            columnB.X = single1 - spriteFont2.MeasureString(averageTradeIncome.ToString("#.0")).X;
            SpriteBatch spriteBatch3 = ScreenManager.SpriteBatch;
            SpriteFont arial12Bold3 = Fonts.Arial12Bold;
            int num = EmpireManager.Player.GetAverageTradeIncome();
            spriteBatch3.DrawString(arial12Bold3, num.ToString("#.0"), columnB, Color.White);
            cursor.Y = cursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(323), ": "), cursor, Color.White);
            cursor.Y = cursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
            cursor.X = cursor.X + 5f;
            float totalTradeIncome = 0f;
            foreach (KeyValuePair<Empire, Relationship> relationship in EmpireManager.Player.AllRelations)
            {
                if (!relationship.Value.Treaty_Trade)
                {
                    continue;
                }
                float tradeValue = -3f + 0.25f * relationship.Value.Treaty_Trade_TurnsExisted;
                if (tradeValue > 3f)
                {
                    tradeValue = 3f;
                }
                totalTradeIncome = totalTradeIncome + tradeValue;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(relationship.Key.data.Traits.Plural, ": "), cursor, relationship.Key.EmpireColor);
                columnB = cursor;
                columnB.X = cursor.X + 150f;
                columnB.X = columnB.X - Fonts.Arial12Bold.MeasureString(tradeValue.ToString("#.0")).X;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, tradeValue.ToString("#.0"), columnB, Color.White);
                cursor.Y = cursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
            }
            totalTradeIncome = totalTradeIncome + EmpireManager.Player.GetAverageTradeIncome();
            cursor = new Vector2(TradeRect.X + TradeRect.Width - 75, TradeRect.Y + TradeRect.Height - Fonts.Arial12Bold.LineSpacing - 5);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Localizer.Token(320), ":  ", totalTradeIncome.ToString("#.0")), cursor, Color.White);
            cursor = new Vector2(Window.Menu.X + Window.Menu.Width - 170, Window.Menu.Y + Window.Menu.Height - 47);
            //float net = incomes + EmpireManager.Player.GetAverageTradeIncome() - (EmpireManager.Player.GetTotalBuildingMaintenance() + EmpireManager.Player.GetTotalShipMaintenance());
            //net = net + totalTradeIncome;
            float net = Screen.player.EstimateIncomeAtTaxRate(Screen.player.data.TaxRate);
            string words;
            if (net <= 0f)
            {
                words = string.Concat(Localizer.Token(325), ": ", net.ToString("#.0"));
                HelperFunctions.DrawDropShadowText(ScreenManager, words, cursor, Fonts.Arial20Bold);
            }
            else
            {
                words = string.Concat(Localizer.Token(324), ": ", net.ToString("#.0"));
                HelperFunctions.DrawDropShadowText(ScreenManager, words, cursor, Fonts.Arial20Bold);
            }
            foreach (UIButton b in Buttons)
            {
                b.Draw(ScreenManager.SpriteBatch);
            }
            Close.Draw(ScreenManager.SpriteBatch);
            ToolTip.Draw(ScreenManager.SpriteBatch);
            ScreenManager.SpriteBatch.End();
        }

        //public override void ExitScreen()
        //{
        //    base.ExitScreen();
        //}

        public override bool HandleInput(InputState input)
        {
            if (input.KeysCurr.IsKeyDown(Keys.T) && !input.KeysPrev.IsKeyDown(Keys.T) && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                ExitScreen();
                return true;
            }
            if (Close.HandleInput(input))
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
            return HandleInput(input);
        }

        public override void LoadContent()
        {
            Window = new Menu2(new Rectangle(ScreenWidth / 2 - 197, ScreenHeight / 2 - 225, 394, 450));
            Close = new CloseButton(this, new Rectangle(Window.Menu.X + Window.Menu.Width - 40, Window.Menu.Y + 20, 20, 20));
            Rectangle rectangle = new Rectangle();
            TaxSlider = new GenericSlider(rectangle, Localizer.Token(309), 0f, 100f)
            {
                amount = EmpireManager.Player.data.TaxRate
            };
            Rectangle rectangle2 = new Rectangle(rectangle.X +20,rectangle.Y +37,rectangle.Width,rectangle.Height);
            TreasuryGoal = new GenericSlider(rectangle2, "Treasury Goal", 1f, 20f)
            {
                amount = EmpireManager.Player.data.treasuryGoal
            };
            TaxRateRect = new Rectangle(Window.Menu.X + 20, Window.Menu.Y + 37, 350, 52);
            IncomesRect = new Rectangle(TaxRateRect.X, TaxRateRect.Y + TaxRateRect.Height + 6, 168, 118);
            TradeRect = new Rectangle(TaxRateRect.X, IncomesRect.Y + IncomesRect.Height + 6, 168, 208);
            CostRect = new Rectangle(TaxRateRect.X + 12 + IncomesRect.Width, IncomesRect.Y, 168, 118);
            base.LoadContent();
        }

        //public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        //{
        //    base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        //}
    }
}