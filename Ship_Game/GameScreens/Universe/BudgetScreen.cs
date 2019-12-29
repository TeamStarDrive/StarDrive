using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.UI;
using System;
using System.Linq;

namespace Ship_Game.GameScreens
{
    public sealed class BudgetScreen : GameScreen
    {
        readonly Empire Player;
        Menu2 Window;

        FloatSlider TaxSlider;
        FloatSlider TreasuryGoal;
        UILabel EmpireNetIncome;

        public BudgetScreen(UniverseScreen screen) : base(screen)
        {
            Player            = screen.player;
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
        }

        class SummaryPanel : UIList
        {
            public SummaryPanel(int title, in Rectangle rect, Color c) : base(rect, c)
            {
                if (title != 0)
                {
                    Header = new UILabel(Localizer.Token(title), Fonts.Arial14Bold)
                    {
                        DropShadow = true
                    };
                }
                Padding     = new Vector2(4f, 2f);
                LayoutStyle = ListLayoutStyle.Fill;
            }

            public SummaryPanel(string title, in Rectangle rect, Color c) : base(rect, c)
            {
                if (title.NotEmpty())
                {
                    Header = new UILabel(title, Fonts.Arial14Bold)
                    {
                        DropShadow = true
                    };
                }
                Padding = new Vector2(4f, 2f);
                LayoutStyle = ListLayoutStyle.Fill;
            }

            public void AddItem(int textId,  Func<float> getValue) => AddItem(Localizer.Token(textId),
                                                                              getValue, Color.White);
            public void AddItem(string text, Func<float> getValue) => AddItem(text, getValue, Color.White);
            public void AddItem(string text, Func<float> getValue, Color keyColor)
            {
                AddSplit(new UILabel($"{text}:", keyColor),
                         new UILabel(DynamicText(getValue, f => f.MoneyString())) );
            }

            public void SetTotalFooter(Func<float> getValue)
            {
                Footer = new SplitElement(new UILabel($"{Localizer.Token(320)}:"),
                                          new UILabel(DynamicText(getValue, f => f.MoneyString())) );
            }

            public FloatSlider AddSlider(string title, float value)
            {
                return Add(new FloatSlider(this, SliderStyle.Percent,
                                           new Vector2(100,32), title, 0f, 1f, value));
            }
        }

        public override void LoadContent()
        {
            Window = Add(new Menu2(new Rectangle(ScreenWidth / 2 - 197, ScreenHeight / 2 - 225, 394, 450)));
            CloseButton(Window.Menu.Right - 40, Window.Menu.Y + 20);

            //Setup containers
            var taxRect    = new Rectangle(Window.Menu.X + 20, Window.Menu.Y + 37, 350, 84); // top area for tax rate slider
            var incomeRect = new Rectangle(taxRect.X, taxRect.Bottom + 6, 168, 112); // Middle-Left
            var costRect   = new Rectangle(incomeRect.Right + 12, incomeRect.Y, 168, 112); // Middle-Right
            var tradeRect  = new Rectangle(taxRect.X, incomeRect.Bottom + 6, 168, 112); // Bottom left
            var budgetRect = new Rectangle(costRect.X, costRect.Bottom + 6, 168, 112); // Bottom right
            var footerRect = new Rectangle(taxRect.X, budgetRect.Bottom + 6, 168, 86);

            //Screen Title
            string title   = Localizer.Token(310);
            Label(Window.Menu.CenterTextX(title), Window.Menu.Y + 20, title);

            // background panels for TaxRate, incomes, cost, trade: 6138
            SummaryPanel tax = Add(new SummaryPanel(0, taxRect, new Color(17, 21, 28)));
            var taxTitle     = Player.data.AutoTaxes ? 6138 : 311;

            TaxSlider    = tax.AddSlider(Localizer.Token(taxTitle), 0.25f);
            TaxSlider.TooltipId = 66;

            TreasuryGoal = tax.AddSlider(Localizer.TreasuryGoal, 0.20f);
            TreasuryGoal.TooltipId = 255;

            TreasuryGoal.OnChange = TreasurySliderOnChange;
            TaxSlider.OnChange    = TaxSliderOnChange;

            TreasuryGoal.RelativeValue = Player.data.treasuryGoal; // trigger updates
            TaxSlider.RelativeValue    = Player.data.TaxRate;

            AutoTaxCheckBox(footerRect);

            IncomesTab(incomeRect);
            CostsTab(costRect);
            TradeTab(tradeRect);
            BudgetTab(budgetRect);

            EmpireNetIncome = Label(Window.Menu.Right - 200,Window.Menu.Bottom - 47,
                                      titleId:324, Fonts.Arial20Bold);
            EmpireNetIncome.DropShadow  = true;
            EmpireNetIncome.DynamicText = DynamicText(
                ()   => Player.NetIncome,
                (f) => $"{Localizer.Token(f >= 0f ? 324 : 325)} : {f.MoneyString()}");

            base.LoadContent();
        }

        private void AutoTaxCheckBox(Rectangle footerRect)
        {
            var autoTax = Checkbox(new Vector2(footerRect.X, footerRect.Y)
                , () => Player.data.AutoTaxes
                , Localizer.Token(6138)
                , 7040);

            autoTax.OnChange = cb =>
            {
                if (cb.Checked)
                {
                    Player.GetEmpireAI().RunEconomicPlanner();
                    TaxSlider.RelativeValue = Player.data.TaxRate;
                }

                TaxSlider.Enabled = !cb.Checked;
                var taxChange = Player.data.AutoTaxes ? 6138 : 311;
                TaxSlider.Text = Localizer.Token(taxChange);
            };
        }

        private void BudgetTab(Rectangle budgetRect)
        {
            SummaryPanel budget = Add(new SummaryPanel(Localizer.GovernorBudget, budgetRect, new Color(30, 26, 19)));
            budget.AddItem("Colony", () => Player.data.ColonyBudget);
            budget.AddItem("SpaceRoad", () => Player.data.SSPBudget);
            budget.AddItem("Defense", () => Player.data.DefenseBudget);
            budget.SetTotalFooter(() => Player.data.ColonyBudget
                                        + Player.data.SSPBudget
                                        + Player.data.DefenseBudget);
        }

        private void TradeTab(Rectangle tradeRect)
        {
            SummaryPanel trade = Add(new SummaryPanel(Localizer.Trade, tradeRect, new Color(30, 26, 19)));
            trade.AddItem(322, () => Player.AverageTradeIncome); // "Mercantilism (Avg)"
            trade.AddItem(323, () => Player.TotalTradeTreatiesIncome()); // "Trade Treaties"
            var traders = Player.AllRelations.Where(kv => kv.Value.Treaty_Trade)
                .Select(kv => (Empire: kv.Key, Relation: kv.Value));

            foreach ((Empire e, Relationship r) in traders)
                trade.AddItem($"   {e.data.Traits.Plural}", () => r.TradeIncome(), e.EmpireColor);
            trade.SetTotalFooter(() => Player.TotalAvgTradeIncome); // "Total"
        }

        private void CostsTab(Rectangle costRect)
        {
            SummaryPanel costs = Add(new SummaryPanel(315, costRect, new Color(27, 22, 25)));
            costs.AddItem(316, () => -Player.TotalBuildingMaintenance); // "Building Maint."
            costs.AddItem(317, () => -Player.TotalShipMaintenance); // "Ship Maint."
            costs.SetTotalFooter(() => -Player.BuildingAndShipMaint); // "Total"
        }

        private void IncomesTab(Rectangle incomeRect)
        {
            SummaryPanel income = Add(new SummaryPanel(312, incomeRect, new Color(18, 29, 29)));
            income.AddItem(313, () => Player.GrossPlanetIncome); // "Planetary Taxes"
            income.AddItem("Other", () => Player.data.FlatMoneyBonus);
            income.AddItem("Excess Goods", () => Player.ExcessGoodsMoneyAddedThisTurn);
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
            int goal = (int) Player.GetEmpireAI().TreasuryGoal();
            Player.GetEmpireAI().RunEconomicPlanner();
            if (Player.data.AutoTaxes)
                TaxSlider.RelativeValue = Player.data.TaxRate;
            s.Text = $"{Localizer.TreasuryGoal} : {goal}";
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

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            base.Draw(batch);
            batch.End();
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
    }
}