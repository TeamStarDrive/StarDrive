using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI.Budget;
using Ship_Game.Gameplay;

namespace Ship_Game.GameScreens
{
    public sealed class BudgetScreen : GameScreen
    {
        Empire Player;
        CloseButton Close;
        Menu2 Window;
        Rectangle TaxRateRect;
        Rectangle CostRect;
        Rectangle IncomesRect;
        Rectangle TradeRect;

        FloatSlider TaxSlider;
        FloatSlider TreasuryGoal;
        UILabel EmpireNetIncome;

        public BudgetScreen(UniverseScreen screen) : base(screen)
        {
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
            TaxRateRect = new Rectangle(Window.Menu.X + 20, Window.Menu.Y + 37, 350, 84);

            string title = Localizer.Token(310);
            Label(Window.Menu.CenterTextX(title), Window.Menu.Y + 20, title);

            BeginVLayout(TaxRateRect.X + 10, TaxRateRect.Y + 5, ystep: 40);
                TaxSlider    = SliderPercent(312, 32, Localizer.Token(311), 0f, 1f, Player.data.TaxRate);
                TreasuryGoal = SliderPercent(312, 32, "Auto Tax Treasury Goal", 0f, 1f, Player.data.treasuryGoal);
                TaxSlider.TooltipId = 66;
                TreasuryGoal.TooltipId = 66;
                TreasuryGoal.OnChange = (s) => Player.data.treasuryGoal = s.AbsoluteValue;
                TaxSlider.OnChange = (s) => {
                    Player.data.TaxRate = s.RelativeValue;
                    Player.UpdateNetPlanetIncomes();
                };
            EndLayout();

            IncomesRect = new Rectangle(TaxRateRect.X, TaxRateRect.Bottom + 6, 168, 104); // Middle-Left
            CostRect    = new Rectangle(IncomesRect.Right + 12, IncomesRect.Y, 168, 104); // Middle-Right
            TradeRect   = new Rectangle(TaxRateRect.X, IncomesRect.Bottom + 6, 168, 188); // Bottom left

            float lineSpacing = Fonts.Arial12Bold.LineSpacing + 4; // = 18

            void Title(int titleId)        => Label(titleId).DropShadow = true;
            void LabelT(int token)         => Label($"{Localizer.Token(token)}: ");
            void MoneyLabel(Func<float> g) => Label(() => g().MoneyString(), alignRight:true);

            // Incomes tab
            BeginVLayout(IncomesRect.X + 10, IncomesRect.Y + 8, lineSpacing);
                Title(312);       // "Incomes"
                LabelT(313);      // "Planetary Taxes"
                Label("Other: "); // Flat money bonus
                SkipLayoutStep(); //  ------
                LabelT(320);      // "Total"
            EndLayout();
            BeginVLayout(IncomesRect.Right - 5, IncomesRect.Y + 26, lineSpacing);
                MoneyLabel(() => Player.NetPlanetIncomes);
                MoneyLabel(() => Player.data.FlatMoneyBonus);
                SkipLayoutStep();
                MoneyLabel(() => Player.GrossIncome());
            EndLayout();

            // Costs tab
            BeginVLayout(CostRect.X + 10, CostRect.Y + 8, lineSpacing);
                Title(315);  // "Costs"
                LabelT(316); // "Building Maint."
                LabelT(317); // "Ship Maint."
                SkipLayoutStep(); //  ------
                LabelT(320); // "Total"
            EndLayout();
            BeginVLayout(CostRect.Right - 5, CostRect.Y + 26, lineSpacing);
                MoneyLabel(() => Player.TotalBuildingMaintenance);
                MoneyLabel(() => Player.TotalShipMaintenance);
                SkipLayoutStep();
                MoneyLabel(() => Player.BuildingAndShipMaint);
            EndLayout();

            var traders = Player.AllRelations.Where(kv => kv.Value.Treaty_Trade)
                                             .Select(kv => (Empire:kv.Key, Relation:kv.Value))
                                             .ToArray();
            // Trade tab
            BeginVLayout(TradeRect.X + 10, TradeRect.Y + 8, lineSpacing);
                Title(321);  // "Trade"
                LabelT(322); // "Mercantilism (Avg)"
                LabelT(323); // "Trade Treaties"
                foreach ((Empire e, Relationship r) in traders)
                    Label($"   {e.data.Traits.Plural}: ", e.EmpireColor);
                LabelT(320); // "Total"
            EndLayout();
            BeginVLayout(TradeRect.Right - 5, TradeRect.Y + 26, lineSpacing);
                MoneyLabel(() => Player.GetAverageTradeIncome()); // Mercantilism
                MoneyLabel(() => Player.GetTotalTradeIncome());   // Trade Treaties
                foreach ((Empire e, Relationship r) in traders)
                    MoneyLabel(() => r.TradeIncome());
                MoneyLabel(() => Player.TotalAvgTradeIncome); // Total
            EndLayout();

            EmpireNetIncome = Label(Window.Menu.Right - 170, Window.Menu.Bottom - 47, titleId:324, Fonts.Arial20Bold);
            EmpireNetIncome.DropShadow = true;

            base.LoadContent();
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();

            Window.Draw(batch);
            batch.FillRectangle(TaxRateRect, new Color(17, 21, 28));
            batch.FillRectangle(IncomesRect, new Color(18, 29, 29));
            batch.FillRectangle(TradeRect,   new Color(30, 26, 19));
            batch.FillRectangle(CostRect,    new Color(27, 22, 25));

            int goal = (int) (100 * EmpireManager.Player.NetPlanetIncomes * TreasuryGoal.RelativeValue);
            TreasuryGoal.Text = $"Auto Tax Treasury Goal : {goal}";

            float net = Player.NetIncome();
            string netIncomeTitle = Localizer.Token(net >= 0f ? 324 : 325);
            EmpireNetIncome.Color = net >= 0f ? Color.GreenYellow : Color.OrangeRed;
            EmpireNetIncome.Text = $"{netIncomeTitle} : {net.MoneyString()}";

            base.Draw(batch);
            ToolTip.Draw(batch);
            batch.End();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.WasKeyPressed(Keys.T) && !GlobalStats.TakingInput)
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