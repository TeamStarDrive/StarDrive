using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI.Budget;

namespace Ship_Game.GameScreens
{
    public sealed class BudgetScreen : GameScreen
    {
        readonly UniverseScreen Screen;
        Empire Player;
        CloseButton Close;
        Menu2 Window;
        Rectangle TaxRateRect;
        Rectangle CostRect;
        Rectangle IncomesRect;
        Rectangle TradeRect;

        FloatSlider TaxSlider;
        FloatSlider TreasuryGoal;

        public BudgetScreen(UniverseScreen screen) : base(screen)
        {
            Screen            = screen;
            Player            = screen.player;
            IsPopup           = true;          
            TransitionOnTime  = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        public override void LoadContent()
        {
            Window = new Menu2(new Rectangle(ScreenWidth / 2 - 197, ScreenHeight / 2 - 225, 394, 450));
            Close = new CloseButton(this, new Rectangle(Window.Menu.Right - 40, Window.Menu.Y + 20, 20, 20));

            // area for tax rate slider
            TaxRateRect = new Rectangle(Window.Menu.X + 20, Window.Menu.Y + 37, 350, 52);

            BeginVLayout(TaxRateRect.X + 10, TaxRateRect.Y + 5, ystep: 32);
                TaxSlider    = SliderPercent(312, 32, Localizer.Token(311), 0f, 1f, Player.data.TaxRate);
                TreasuryGoal = Slider(312, 32, "Auto Tax Treasury Goal", 1f, 20f, Player.data.treasuryGoal);
            EndLayout();

            //string tGoal = $"Auto Tax Treasury Goal : {(int)(100 * EmpireManager.Player.NetPlanetIncomes * TreasuryGoal.amount)}";

            ToolTip.CreateTooltip(66); // tax
            ToolTip.CreateTooltip(66); // reasury

            IncomesRect = new Rectangle(TaxRateRect.X, TaxRateRect.Y + TaxRateRect.Height + 6, 168, 118);
            TradeRect   = new Rectangle(TaxRateRect.X, IncomesRect.Y + IncomesRect.Height + 6, 168, 208);
            CostRect    = new Rectangle(TaxRateRect.X + 12 + IncomesRect.Width, IncomesRect.Y, 168, 118);
            base.LoadContent();
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            Window.Draw(batch);
            string title     = Localizer.Token(310);
            SpriteFont font = Fonts.Arial12Bold;
            float lineSpacing = font.LineSpacing;

            var cursor = new Vector2(Window.Menu.CenterTextX(title), Window.Menu.Y + 20);
            batch.DrawString(font, title, cursor, Color.White);
            cursor.Y += lineSpacing + 2;

            batch.FillRectangle(TaxRateRect, new Color(17, 21, 28));
            batch.FillRectangle(IncomesRect, new Color(18, 29, 29));
            batch.FillRectangle(TradeRect,   new Color(30, 26, 19));
            batch.FillRectangle(CostRect,    new Color(27, 22, 25));

            TaxSlider.Draw(batch);
            TreasuryGoal.Draw(batch);

            string titleIncomes        = Localizer.Token(312);
            string titlePlanetaryTaxes = Localizer.Token(313);
            string titleTotal          = Localizer.Token(314);
            string titleCosts          = Localizer.Token(315);
            string titleBuildingMaint  = Localizer.Token(316);
            string titleShipMaint      = Localizer.Token(317);
            string titleTotal2         = Localizer.Token(320);
            string titleTrade          = Localizer.Token(321);
            string titleMercantilism   = Localizer.Token(322);
            string titleTradeTreaties  = Localizer.Token(323);
            string titleNetGain        = Localizer.Token(324);
            string titleNetLoss        = Localizer.Token(325);            

            cursor = new Vector2(IncomesRect.X + 10, IncomesRect.Y + 8);
            HelperFunctions.DrawDropShadowText(ScreenManager, titleIncomes, cursor, font);
            cursor.Y += (lineSpacing + 2);

            Vector2 columnB = cursor;
            columnB.X += 150f;

            string strIncomes   = Player.NetPlanetIncomes.String(2);
            string strFlatMoney = Player.data.FlatMoneyBonus.String(2);

            batch.DrawString(font, $"{titlePlanetaryTaxes}: ", cursor, Color.White);

            columnB.X -= font.MeasureString(strIncomes).X;
            batch.DrawString(font, strIncomes, columnB, Color.White);
            cursor.Y += lineSpacing;

            columnB = cursor;
            columnB.X = cursor.X + 150f;
            batch.DrawString(font, "Bonus: ", cursor, Color.White);
            columnB.X -= font.MeasureString(strFlatMoney).X;
            batch.DrawString(font, strFlatMoney, columnB, Color.White);

            float totalIncome = Screen.player.NetIncome();
            cursor.Y += lineSpacing * 2;

            cursor = new Vector2(IncomesRect.X + IncomesRect.Width - 75, IncomesRect.Y + IncomesRect.Height - lineSpacing - 5);            
            batch.DrawString(font, $"{titleTotal}: {totalIncome:0.00}", cursor, Color.White);



            cursor = new Vector2(CostRect.X + 10, CostRect.Y + 8);
            HelperFunctions.DrawDropShadowText(ScreenManager, titleCosts, cursor, font);


            cursor.Y += (lineSpacing + 2);
            columnB                         = cursor;
            columnB.X                       = cursor.X + 150f;
            
            batch.DrawString(font, $"{titleBuildingMaint}: ", cursor, Color.White);            
            
            float totalBuildingMaintenance  = EmpireManager.Player.TotalBuildingMaintenance;
            columnB.X                       = columnB.X - font.MeasureString(totalBuildingMaintenance.ToString("#.0")).X;
                        
            batch.DrawString(font, totalBuildingMaintenance.String(), columnB, Color.White);
            cursor.Y                        = cursor.Y + (font.LineSpacing + 2);
            columnB                         = cursor;
            columnB.X                       = cursor.X + 150f;
            
            batch.DrawString(font, $"{titleShipMaint}: ", cursor, Color.White);
            
            float totalShipMaintenance      = EmpireManager.Player.TotalShipMaintenance;
            columnB.X                       = columnB.X - font.MeasureString(totalShipMaintenance.ToString("#.0")).X;            
            
            batch.DrawString(font, totalShipMaintenance.String(), columnB, Color.White);
            cursor.Y                        = cursor.Y + (font.LineSpacing + 2);
            cursor                          = new Vector2(CostRect.X + CostRect.Width - 75, CostRect.Y + CostRect.Height - font.LineSpacing - 5);                        
            
            float totalBuildingMaintenance1 = totalBuildingMaintenance + totalShipMaintenance;
            batch.DrawString(font, $"{titleTotal2}: {totalBuildingMaintenance1:#.0}", cursor, Color.White);            
            cursor                          = new Vector2(TradeRect.X + 10, TradeRect.Y + 8);
            
            HelperFunctions.DrawDropShadowText(ScreenManager, titleTrade, cursor, font);
            cursor.Y                        = cursor.Y + (font.LineSpacing + 2);
            columnB                         = cursor;
            columnB.X                       = cursor.X + 150f;
            
            batch.DrawString(font, $"{titleMercantilism}: ", cursor, Color.White);                      
            int averageTradeIncome          = EmpireManager.Player.GetAverageTradeIncome();
            columnB.X                       = columnB.X - font.MeasureString(averageTradeIncome.ToString("#.0")).X;
            SpriteBatch spriteBatch3        = batch;            
            
            spriteBatch3.DrawString(font, averageTradeIncome.ToString("#.0"), columnB, Color.White);
            cursor.Y                        = cursor.Y + (font.LineSpacing + 2);
            
            batch.DrawString(font, $"{titleTradeTreaties}: ", cursor, Color.White);
            cursor.Y                        = cursor.Y + (font.LineSpacing + 2);
            cursor.X                        = cursor.X + 5f;
            float totalTradeIncome          = DrawTotalTradeIncome(font, ref cursor, ref columnB);

            totalTradeIncome                = totalTradeIncome + EmpireManager.Player.GetAverageTradeIncome();
            cursor                          = new Vector2(TradeRect.X + TradeRect.Width - 75, TradeRect.Y + TradeRect.Height - font.LineSpacing - 5);
            batch.DrawString(font, $"{Localizer.Token(320)}: {totalTradeIncome:#.0}", cursor, Color.White);
            cursor                          = new Vector2(Window.Menu.X + Window.Menu.Width - 170, Window.Menu.Y + Window.Menu.Height - 47);
            float net                       = Screen.player.EstimateIncomeAtTaxRate(Screen.player.data.TaxRate);
            string words;

            if (net <= 0f)
            {
                words = $"{titleNetLoss}: {net:#.0}";
                HelperFunctions.DrawDropShadowText(ScreenManager, words, cursor, Fonts.Arial20Bold);
            }
            else
            {
                words = $"{titleNetGain} : {net:#.0}";
                HelperFunctions.DrawDropShadowText(ScreenManager, words, cursor, Fonts.Arial20Bold);
            }
            base.Draw(batch);
            ToolTip.Draw(batch);
            batch.End();
        }

        private float DrawTotalTradeIncome(SpriteFont arial12Bold, ref Vector2 cursor, ref Vector2 columnB)
        {
            float totalTradeIncome =0;
            foreach (var relationship in EmpireManager.Player.AllRelations)
            {
                if (!relationship.Value.Treaty_Trade)                
                    continue;                

                float tradeValue = CommonValues.TradeMoney(relationship.Value.Treaty_Trade_TurnsExisted);

                totalTradeIncome = totalTradeIncome + tradeValue;
                ScreenManager.SpriteBatch.DrawString(arial12Bold, $"{relationship.Key.data.Traits.Plural}: ", cursor,
                    relationship.Key.EmpireColor);
                columnB = cursor;
                columnB.X = cursor.X + 150f;
                columnB.X = columnB.X - arial12Bold.MeasureString(tradeValue.ToString("#.0")).X;
                ScreenManager.SpriteBatch.DrawString(arial12Bold, tradeValue.ToString("#.0"), columnB, Color.White);
                cursor.Y = cursor.Y + (arial12Bold.LineSpacing + 2);
            }

            return totalTradeIncome;
        }

        public override bool HandleInput(InputState input)
        {
            if (input.KeysCurr.IsKeyDown(Keys.T) && !input.KeysPrev.IsKeyDown(Keys.T) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }
            if (Close.HandleInput(input))
            {
                ExitScreen();
                return true;
            }

            
            if (TaxSlider.HandleInput(input) || TreasuryGoal.HandleInput(input))
            {
                Player.data.treasuryGoal = TreasuryGoal.AbsoluteValue;
                Player.data.TaxRate      = TaxSlider.RelativeValue;
                Player.UpdatePlanetIncomes();
            }

            if (input.Escaped)
            {
                ExitScreen();
                return true;
            }

            if (!input.RightMouseClick) return base.HandleInput(input);

            ExitScreen();
            return true;
        }
    }
}