using Microsoft.Xna.Framework.Graphics;
using SDGraphics.Input;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.UI;
using System;
using SDUtils;
using Ship_Game.ExtensionMethods;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.GameScreens
{
    public sealed class BudgetScreen : GameScreen
    {
        readonly Empire Player;
        Menu2 Window;

        FloatSlider TaxSlider;
        FloatSlider TreasuryGoal;
        UILabel EmpireNetIncome;

        public BudgetScreen(UniverseScreen screen) : base(screen, toPause: screen)
        {
            Player            = screen.Player;
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
        }

        class SummaryPanel : UIList
        {
            public SummaryPanel(LocalizedText title, in Rectangle rect, Color c) : base(rect, c)
            {
                if (title.NotEmpty)
                {
                    Header = new UILabel(title, Fonts.Arial14Bold, Color.Wheat)
                    {
                        DropShadow = true
                    };
                }
                Padding     = new Vector2(4f, 2f);
                LayoutStyle = ListLayoutStyle.Fill;
            }

            public void AddItem(LocalizedText text, Func<float> getValue) => AddItem(text, getValue, Color.White);
            public void AddItem(LocalizedText text, Func<float> getValue, Color keyColor)
            {
                AddSplit(new UILabel(text.Text + ":", keyColor),
                         new UILabel(DynamicText(getValue, f => f.MoneyString())) );
            }

            public void SetTotalFooter(Func<float> getValue)
            {
                Footer = new SplitElement(new UILabel(Localizer.Token(GameText.Total2) + ":"),
                                          new UILabel(DynamicText(getValue, f => f.MoneyString())) );
            }

            public FloatSlider AddSlider(LocalizedText title, float value)
            {
                return Add(new FloatSlider(SliderStyle.Percent, new Vector2(100,32), title, 0f, 1f, value));
            }
        }

        public override void LoadContent()
        {
            Window = Add(new Menu2(new Rectangle(ScreenWidth / 2 - 197, ScreenHeight / 2 - 234, 394, 468)));
            CloseButton(Window.Menu.Right - 40, Window.Menu.Y + 20);

            //Setup containers
            var taxRect    = new Rectangle(Window.Menu.X + 20, Window.Menu.Y + 37, 350, 84); // top area for tax rate slider
            var incomeRect = new Rectangle(taxRect.X, taxRect.Bottom + 6, 168, 150); // Middle-Left
            var costRect   = new Rectangle(incomeRect.Right + 12, incomeRect.Y, 168, 150); // Middle-Right
            var tradeRect  = new Rectangle(taxRect.X, incomeRect.Bottom + 6, 168, 166); // Bottom left
            var budgetRect = new Rectangle(costRect.X, costRect.Bottom + 6, 168, 112); // Bottom right
            var footerRect = new Rectangle(budgetRect.X, budgetRect.Bottom + 6, 168, 86);

            //Screen Title
            string title   = Localizer.Token(GameText.EconomicOverview);
            Label(Window.Menu.CenterTextX(title), Window.Menu.Y + 20, title);

            // background panels for TaxRate, incomes, cost, trade: 6138
            SummaryPanel tax = Add(new SummaryPanel("", taxRect, new Color(17, 21, 28)));
            var taxTitle = Player.AutoTaxes ? GameText.AutoTaxes : GameText.TaxRate;

            TaxSlider = tax.AddSlider(Localizer.Token(taxTitle), Player.data.TaxRate);
            TaxSlider.Tip = GameText.TaxesAreCollectedFromYour;
            TaxSlider.OnChange = TaxSliderOnChange;

            TreasuryGoal          = tax.AddSlider(GameText.TreasuryGoal, Player.data.treasuryGoal);
            TreasuryGoal.Tip      = GameText.TreasuryGoalIsTheTarget;
            TreasuryGoal.OnChange = TreasurySliderOnChange;

            TreasuryGoal.RelativeValue = Player.data.treasuryGoal; // trigger updates
            TaxSlider.RelativeValue    = Player.data.TaxRate;

            AutoTaxCheckBox(footerRect);

            IncomesTab(incomeRect);
            CostsTab(costRect);
            TradeTab(tradeRect);
            BudgetTab(budgetRect);

            EmpireNetIncome = Label(Window.Menu.Right - 200,Window.Menu.Bottom - 47,
                                    text:GameText.NetGain, Fonts.Arial20Bold);
            EmpireNetIncome.DropShadow  = true;
            EmpireNetIncome.DynamicText = DynamicText(
                ()   => Player.NetIncome-Player.MoneySpendOnProductionNow,
                (f) => $"{( f >= 0f ? Localizer.Token(GameText.NetGain) : Localizer.Token(GameText.NetLoss) )} : {f.MoneyString()}");

            base.LoadContent();
        }

        private UICheckBox AutoTaxCheckBox(Rectangle footerRect)
        {
            var autoTax = Checkbox(new Vector2(footerRect.X, footerRect.Y), () => Player.AutoTaxes, 
                                   GameText.AutoTaxes, GameText.YourEmpireWillAutomaticallyManage3);
            autoTax.OnChange = cb =>
            {
                if (cb.Checked)
                {
                    Player.AI.RunEconomicPlanner();
                    TaxSlider.RelativeValue = Player.data.TaxRate;
                }
                TaxSlider.Enabled = !cb.Checked;
                TaxSlider.Text = Player.AutoTaxes ? GameText.AutoTaxes : GameText.TaxRate;
            };
            TaxSlider.Enabled = !autoTax.Checked;
            return autoTax;
        }

        private void BudgetTab(Rectangle budgetRect)
        {
            SummaryPanel budget = Add(new SummaryPanel(GameText.GovernorBudget, budgetRect, new Color(30, 26, 19)));
            budget.AddItem("Colony", () => Player.AI.ColonyBudget);
            budget.AddItem("SpaceRoad", () => Player.AI.SSPBudget);
            budget.AddItem("Defense", () => Player.AI.DefenseBudget);
            budget.SetTotalFooter(() => Player.AI.ColonyBudget + Player.AI.SSPBudget + Player.AI.DefenseBudget);
        }

        private void TradeTab(Rectangle tradeRect)
        {
            SummaryPanel trade = Add(new SummaryPanel(GameText.Trade, tradeRect, new Color(30, 26, 19)));

            trade.AddItem(GameText.MercantilismAvg, () => Player.AverageTradeIncome); // "Mercantilism (Avg)"
            trade.AddItem(GameText.TradeTreaties, () => Player.TotalTradeTreatiesIncome()); // "Trade Treaties"

            foreach (Relationship r in Player.TradeRelations)
                trade.AddItem($"   {r.Them.data.Traits.Plural}", () => r.TradeIncome(Player), r.Them.EmpireColor);

            trade.SetTotalFooter(() => Player.TotalAvgTradeIncome); // "Total"
        }

        private void CostsTab(Rectangle costRect)
        {
            SummaryPanel costs = Add(new SummaryPanel(GameText.Expenditure, costRect, new Color(27, 22, 25)));

            costs.AddItem(GameText.BuildingMaint, () => -Player.TotalBuildingMaintenance); // "Building Maint."
            costs.AddItem(GameText.ShipMaint, () => -Player.TotalShipMaintenance); // "Ship Maint."
            costs.AddItem(GameText.TroopMaint, () => -Player.GetTroopMaintThisTurn()); // "Troop Maint."
            costs.AddItem(GameText.ProductionFees, () => -(Player.MoneySpendOnProductionThisTurn+Player.MoneySpendOnProductionNow)); // "production costs."
            if (Player.NewEspionageEnabled)
                costs.AddItem("Espionage", () => -Player.EspionageCostLastTurn);

            costs.SetTotalFooter(() => -(Player.AllSpending+Player.MoneySpendOnProductionNow)); // "Total"
        }

        private void IncomesTab(Rectangle incomeRect)
        {
            SummaryPanel income = Add(new SummaryPanel(GameText.Income, incomeRect, new Color(18, 29, 29)));

            income.AddItem(GameText.PlanetaryTaxes, () => Player.GrossPlanetIncome); // "Planetary Taxes"
            income.AddItem("Trade Cargo", () => Player.TotalTradeMoneyAddedThisTurn);
            income.AddItem("Excess Goods", () => Player.ExcessGoodsMoneyAddedThisTurn);
            income.AddItem("Money Leeched", () => Player.TotalMoneyLeechedLastTurn);
            income.AddItem(GameText.Other, () => Player.data.FlatMoneyBonus);
            income.SetTotalFooter(() => Player.GrossIncome); // "Total"
        }

        private void TaxSliderOnChange(FloatSlider s)
        {
            Player.data.TaxRate = s.RelativeValue;
            Player.UpdateNetPlanetIncomes();
        }

        private void TreasurySliderOnChange(FloatSlider s)
        {
            Player.data.treasuryGoal = s.RelativeValue;
            Player.data.treasuryGoal = s.AbsoluteValue;
            
            int goal = (int)Player.AI.TreasuryGoal(Player.Money) / 2;
            s.Text = $"{Localizer.Token(GameText.TreasuryGoal)} : {goal}";
            Player.AI.RunEconomicPlanner();

            if (Player.AutoTaxes)
                TaxSlider.RelativeValue = Player.data.TaxRate;
        }

        // Dynamic Text label; this is invoked every time MoneyLabels are drawn
        static Func<UILabel, string> DynamicText(Func<float> getValue,
                                                 Func<float, string> stringify)
        {
            return (label) =>
            {
                float f = getValue(); // update money color based on value:
                label.Color = f > 0f ? Color.ForestGreen :
                              f < 0f ? Color.Red : Color.Gray;
                return stringify(f);
            };
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.SafeBegin();
            base.Draw(batch, elapsed);
            batch.SafeEnd();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.KeyPressed(Keys.T) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        public override void Update(float fixedDeltaTime)
        {
            TreasuryGoal.Text = $"{Localizer.Token(GameText.TreasuryGoal)} : {Player.AI.ProjectedMoney:0.00}";
            base.Update(fixedDeltaTime);
        }
    }
}
