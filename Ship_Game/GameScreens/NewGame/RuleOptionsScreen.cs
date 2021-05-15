using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class RuleOptionsScreen : GameScreen
    {
        private Menu2 MainMenu;
        private FloatSlider FTLPenaltySlider;
        private FloatSlider EnemyFTLPenaltySlider;
        private FloatSlider GravityWellSize;
        private FloatSlider ExtraPlanets;
        private FloatSlider IncreaseMaintenance;
        private FloatSlider MinimumWarpRange;
        private FloatSlider StartingRichness;
        private FloatSlider TurnTimer;
        private FloatSlider CustomMineralDecay;
        private FloatSlider VolcanicActivity;

        public RuleOptionsScreen(GameScreen parent) : base(parent)
        {
            IsPopup = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            base.Draw(batch, elapsed);
            batch.End();
        }


        public override bool HandleInput(InputState input)
        {
            if (base.HandleInput(input))
            {
                GlobalStats.FTLInSystemModifier      = FTLPenaltySlider.RelativeValue;
                GlobalStats.EnemyFTLInSystemModifier = EnemyFTLPenaltySlider.RelativeValue;
                GlobalStats.GravityWellRange         = GravityWellSize.AbsoluteValue;
                GlobalStats.ExtraPlanets             = (int)ExtraPlanets.AbsoluteValue;
                GlobalStats.MinimumWarpRange         = MinimumWarpRange.AbsoluteValue;
                GlobalStats.ShipMaintenanceMulti     = IncreaseMaintenance.AbsoluteValue;
                GlobalStats.StartingPlanetRichness   = StartingRichness.AbsoluteValue;
                GlobalStats.TurnTimer                = (byte)TurnTimer.AbsoluteValue;
                GlobalStats.CustomMineralDecay       = (CustomMineralDecay.AbsoluteValue).RoundToFractionOf10();
                GlobalStats.VolcanicActivity         = (VolcanicActivity.AbsoluteValue).RoundToFractionOf10();
                return true;
            }
            return false;
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RemoveAll();
            int width  = ScreenWidth;
            int height = ScreenHeight;

            var titleRect = new Rectangle(width / 2 - 203, (LowRes ? 10 : 44), 406, 80);
            var nameRect  = new Rectangle(width / 2 - height / 4, titleRect.Y + titleRect.Height + 5, width / 2, 150);
            var leftRect  = new Rectangle(width / 2 - width / 4,  height /2 -(nameRect.Y + nameRect.Height + 5), width / 2, 580);
            int x = leftRect.X + 60;
            MainMenu = Add(new Menu2(leftRect, Color.Black));
            CloseButton(leftRect.X + leftRect.Width - 40, leftRect.Y + 20);

            var ftlRect = new Rectangle(x, leftRect.Y + 100, 270, 50);
            FTLPenaltySlider = SliderPercent(ftlRect, Localizer.Token(GameText.InsystemFtlSpeedModifier), 0f, 1f, GlobalStats.FTLInSystemModifier);

            var eftlRect = new Rectangle(x, leftRect.Y + 150, 270, 50);
            EnemyFTLPenaltySlider = SliderPercent(eftlRect, Localizer.Token(GameText.InsystemEnemyFtlSpeedModifier), 0f, 1f, GlobalStats.EnemyFTLInSystemModifier);
            int indent = (int)(width / 4.5f); 
            Checkbox(ftlRect.X + indent, ftlRect.Y, () => GlobalStats.PlanetaryGravityWells, title: GameText.PlanetaryGravityWells, tooltip: GameText.EnablesPlanetaryGravityWellsWhich);
            Checkbox(ftlRect.X + indent, ftlRect.Y + 25, ()  => GlobalStats.PreventFederations,    title: GameText.PreventAiFederations, tooltip: GameText.PreventsAiEmpiresFromMerging);
            Checkbox(ftlRect.X + indent, ftlRect.Y + 50,()   => GlobalStats.WarpInSystem,          title: GameText.TreatNeutralSystemsAsUnfriendly, tooltip: GameText.TreatNeutralSystemsAsUnfriendly);
            Checkbox(ftlRect.X + indent, ftlRect.Y + 75, ()  => GlobalStats.FixedPlayerCreditCharge, title: GameText.FixedShipAndBuildingsCost, tooltip: GameText.KeepFixedCreditCostOf);
            Checkbox(ftlRect.X + indent, ftlRect.Y + 100, () => GlobalStats.UsePlayerDesigns, title: GameText.UsePlayerDesignsTitle, tooltip: GameText.UsePlayerDesignsTip);
            Checkbox(ftlRect.X + indent, ftlRect.Y + 125, () => GlobalStats.DisablePirates, title: GameText.DisablePirates, tooltip: GameText.DisablesAllPirateFactionsFor);
            Checkbox(ftlRect.X + indent, ftlRect.Y + 150, () => GlobalStats.DisableRemnantStory, title: GameText.DisableRemnantStory, tooltip: GameText.IfCheckedRemnantForcesIn);

            var mdRect = new Rectangle(ftlRect.X + indent+2, ftlRect.Y + 195, 270, 50);
            CustomMineralDecay = SliderDecimal1(mdRect, Localizer.Token(GameText.MineralDecayRate), 0.5f, 3, GlobalStats.CustomMineralDecay);

            var vaRect = new Rectangle(ftlRect.X + indent + 2, ftlRect.Y + 255, 270, 50);
            VolcanicActivity = SliderDecimal1(vaRect, Localizer.Token(GameText.VolcanicActivity), 0.5f, 3, GlobalStats.VolcanicActivity);

            var gwRect = new Rectangle(x, leftRect.Y + 210, 270, 50);
            var epRect = new Rectangle(x, leftRect.Y + 270, 270, 50);
            var richnessRect = new Rectangle(x, leftRect.Y + 330, 270, 50);

            GravityWellSize  = Slider(gwRect,  GameText.GravityWellRadius, 0, 20000, GlobalStats.GravityWellRange);
            ExtraPlanets     = Slider(epRect, GameText.ExtraPlanets, 0, 3f, GlobalStats.ExtraPlanets);
            StartingRichness = Slider(richnessRect, GameText.StartingPlanetRichnessBonus, 0, 5f, GlobalStats.StartingPlanetRichness);


            var optionTurnTimer  = new Rectangle(x, leftRect.Y + 390, 270, 50);
            var minimumWarpRange = new Rectangle(x, leftRect.Y + 450, 270, 50);
            var maintenanceRect  = new Rectangle(x, leftRect.Y + 510, 270, 50);

            TurnTimer           = Slider(optionTurnTimer,  GameText.SecondsPerTurn, 2, 18f,      GlobalStats.TurnTimer);
            MinimumWarpRange    = Slider(minimumWarpRange, GameText.MinimumWarpRange, 0, 1200000f, GlobalStats.MinimumWarpRange);
            IncreaseMaintenance = Slider(maintenanceRect,  GameText.MaintenanceMultiplier, 1, 10f,      GlobalStats.ShipMaintenanceMulti);

            FTLPenaltySlider.Tip      = GameText.UsingThisSliderYouCan;
            EnemyFTLPenaltySlider.Tip = GameText.UsingThisSliderYouCan2;
            GravityWellSize.Tip       = GameText.DefinesTheRadiusOfPlanetary;
            CustomMineralDecay.Tip    = GameText.HigherMineralDecayIncreasesThe;
            VolcanicActivity.Tip      = GameText.ThisWillControlTheChances;

            string extraPlanetsTip = Localizer.Token(GameText.AddExtraPlanetsToEach);
            if (GlobalStats.ModChangeResearchCost)
                extraPlanetsTip = $"{extraPlanetsTip} {Localizer.Token(GameText.ThisWillSlightlyIncreaseResearch)}";

            ExtraPlanets.Tip        = extraPlanetsTip;
            MinimumWarpRange.Tip    = GameText.MinimumWarpRangeAShip;
            IncreaseMaintenance.Tip = GameText.MultiplyGlobalMaintenanceCostBy;
            TurnTimer.Tip           = GameText.TimeInSecondsPerTurn;
            StartingRichness.Tip    = GameText.AddToAllStartingEmpire;



            Label(MainMenu.Menu.X + 40, MainMenu.Menu.Y + 40, GameText.AdvancedRuleOptions, Fonts.Arial20Bold);
            string text = Fonts.Arial12.ParseText(Localizer.Token(GameText.InThisPanelYouMay), MainMenu.Menu.Width - 80);
            Label(MainMenu.Menu.X + 40, MainMenu.Menu.Y + 40 + Fonts.Arial20Bold.LineSpacing + 2, text, Fonts.Arial12);
        }
    }
}
