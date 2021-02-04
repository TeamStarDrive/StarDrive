using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;
using System;

namespace Ship_Game
{
    public sealed class RuleOptionsScreen : GameScreen
    {
        public bool isOpen;
        private Menu2 MainMenu;
        private FloatSlider FTLPenaltySlider;
        private FloatSlider EnemyFTLPenaltySlider;
        private FloatSlider GravityWellSize;
        private FloatSlider extraPlanets;
        private FloatSlider IncreaseMaintenance;
        private FloatSlider MinimumWarpRange;
        private FloatSlider StartingRichness;
        private FloatSlider TurnTimer;
        private FloatSlider CustomMineralDecay;
        public Ship itemToBuild;

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
                GlobalStats.ExtraPlanets             = (int)extraPlanets.AbsoluteValue;
                GlobalStats.MinimumWarpRange         = MinimumWarpRange.AbsoluteValue;
                GlobalStats.ShipMaintenanceMulti     = IncreaseMaintenance.AbsoluteValue;
                GlobalStats.StartingPlanetRichness   = StartingRichness.AbsoluteValue;
                GlobalStats.TurnTimer                = (byte)TurnTimer.AbsoluteValue;
                GlobalStats.CustomMineralDecay       = (CustomMineralDecay.AbsoluteValue).RoundToFractionOf10();
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
            FTLPenaltySlider = SliderPercent(ftlRect, Localizer.Token(4007), 0f, 1f, GlobalStats.FTLInSystemModifier);

            var eftlRect = new Rectangle(x, leftRect.Y + 150, 270, 50);
            EnemyFTLPenaltySlider = SliderPercent(eftlRect, Localizer.Token(6139), 0f, 1f, GlobalStats.EnemyFTLInSystemModifier);
            int indent = (int)(width / 4.5f); 
            Checkbox(ftlRect.X + indent, ftlRect.Y, () => GlobalStats.PlanetaryGravityWells, title: 4008, tooltip: 2288);
            Checkbox(ftlRect.X + indent, ftlRect.Y + 25, () => GlobalStats.PreventFederations,    title: 6022, tooltip: 7011);
            Checkbox(ftlRect.X + indent, ftlRect.Y + 50,() => GlobalStats.WarpInSystem,          title: 6178, tooltip: 6178);
            Checkbox(ftlRect.X + indent, ftlRect.Y + 75, () => GlobalStats.FixedPlayerCreditCharge, title: 1861, tooltip: 1862);
            Checkbox(ftlRect.X + indent, ftlRect.Y + 100, () => GlobalStats.DisablePirates, title: 1868, tooltip: 1869);
            Checkbox(ftlRect.X + indent, ftlRect.Y + 125, () => GlobalStats.DisableRemnantStory, title: 1844, tooltip: 1845);

            var mdRect = new Rectangle(ftlRect.X + indent+2, ftlRect.Y + 170, 270, 50);
            CustomMineralDecay = SliderDecimal1(mdRect, Localizer.Token(4115), 0.5f, 3, GlobalStats.CustomMineralDecay);

            var gwRect = new Rectangle(x, leftRect.Y + 210, 270, 50);
            var epRect = new Rectangle(x, leftRect.Y + 270, 270, 50);
            var richnessRect = new Rectangle(x, leftRect.Y + 330, 270, 50);

            GravityWellSize  = Slider(gwRect,  new LocalizedText(6002).Text, 0, 20000, GlobalStats.GravityWellRange);
            extraPlanets     = Slider(epRect, new LocalizedText(6002).Text, 0, 3f, GlobalStats.ExtraPlanets);
            StartingRichness = Slider(richnessRect, new LocalizedText(4121).Text, 0, 5f, GlobalStats.StartingPlanetRichness);


            var optionTurnTimer  = new Rectangle(x, leftRect.Y + 390, 270, 50);
            var minimumWarpRange = new Rectangle(x, leftRect.Y + 450, 270, 50);
            var maintenanceRect  = new Rectangle(x, leftRect.Y + 510, 270, 50);

            TurnTimer           = Slider(optionTurnTimer,  new LocalizedText(4125).Text, 2, 18f,      GlobalStats.TurnTimer);
            MinimumWarpRange    = Slider(minimumWarpRange, new LocalizedText(4123).Text, 0, 1200000f, GlobalStats.MinimumWarpRange);
            IncreaseMaintenance = Slider(maintenanceRect,  new LocalizedText(4127).Text, 1, 10f,      GlobalStats.ShipMaintenanceMulti);

            FTLPenaltySlider.Tip      = 2286;
            EnemyFTLPenaltySlider.Tip = 7041;
            GravityWellSize.Tip       = 6003;
            CustomMineralDecay.Tip    = 4116;

            string extraPlanetsTip = new LocalizedText(4119).Text;
            if (GlobalStats.ModChangeResearchCost)
                extraPlanetsTip = $"{extraPlanetsTip} {new LocalizedText(4120).Text}";

            extraPlanets.Tip        = extraPlanetsTip;
            MinimumWarpRange.Tip    = new LocalizedText(4124).Text;
            IncreaseMaintenance.Tip = new LocalizedText(4128).Text;
            TurnTimer.Tip           = new LocalizedText(4126).Text;
            StartingRichness.Tip    = new LocalizedText(4122).Text;



            Label(MainMenu.Menu.X + 40, MainMenu.Menu.Y + 40, new LocalizedText(4129).Text, Fonts.Arial20Bold);
            string text = Fonts.Arial12.ParseText(Localizer.Token(2289), MainMenu.Menu.Width - 80);
            Label(MainMenu.Menu.X + 40, MainMenu.Menu.Y + 40 + Fonts.Arial20Bold.LineSpacing + 2, text, Fonts.Arial12);
        }
    }
}