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
            var leftRect  = new Rectangle(width / 2 - height / 4, nameRect.Y + nameRect.Height + 5, width / 2,
                                              height - (titleRect.Y + titleRect.Height) - (int)(0.28f * height));
            if (leftRect.Height > 580)
                leftRect.Height = 580;

            int x = leftRect.X + 60;
            MainMenu = Add(new Menu2(leftRect, Color.Black));
            CloseButton(leftRect.X + leftRect.Width - 40, leftRect.Y + 20);

            var ftlRect = new Rectangle(x, leftRect.Y + 100, 270, 50);
            FTLPenaltySlider = SliderPercent(ftlRect, Localizer.Token(4007), 0f, 1f, GlobalStats.FTLInSystemModifier);

            var eftlRect = new Rectangle(x, leftRect.Y + 150, 270, 50);
            EnemyFTLPenaltySlider = SliderPercent(eftlRect, Localizer.Token(6139), 0f, 1f, GlobalStats.EnemyFTLInSystemModifier);

            Checkbox(ftlRect.X + 420, ftlRect.Y, () => GlobalStats.PlanetaryGravityWells, title: 4008, tooltip: 2288);
            Checkbox(ftlRect.X + 420, ftlRect.Y + 25, () => GlobalStats.PreventFederations,    title: 6022, tooltip: 7011);
            Checkbox(ftlRect.X + 420, ftlRect.Y + 50,() => GlobalStats.WarpInSystem,          title: 6178, tooltip: 6178);
            Checkbox(ftlRect.X + 420, ftlRect.Y + 75, () => GlobalStats.FixedPlayerCreditCharge, title: 1861, tooltip: 1862);
            Checkbox(ftlRect.X + 420, ftlRect.Y + 100, () => GlobalStats.DisablePirates, title: 1868, tooltip: 1869);
            Checkbox(ftlRect.X + 420, ftlRect.Y + 125, () => GlobalStats.DisableRemnantStory, title: 1844, tooltip: 1845);

            var gwRect = new Rectangle(x, leftRect.Y + 210, 270, 50);
            var epRect = new Rectangle(x, leftRect.Y + 270, 270, 50);
            var richnessRect = new Rectangle(x, leftRect.Y + 330, 270, 50);

            GravityWellSize  = Slider(gwRect, Localizer.Token(6002), 0, 20000, GlobalStats.GravityWellRange);
            extraPlanets     = Slider(epRect, "Extra Planets", 0, 3f, GlobalStats.ExtraPlanets);
            StartingRichness = Slider(richnessRect, "Starting Planet Richness Bonus", 0, 5f, GlobalStats.StartingPlanetRichness);


            var optionTurnTimer  = new Rectangle(x, leftRect.Y + 390, 270, 50);
            var minimumWarpRange = new Rectangle(x, leftRect.Y + 450, 270, 50);
            var maintenanceRect  = new Rectangle(x, leftRect.Y + 510, 270, 50);

            TurnTimer           = Slider(optionTurnTimer,  "Change Turn Timer",    2f, 18f,     GlobalStats.TurnTimer);
            MinimumWarpRange    = Slider(minimumWarpRange, "Minimum Warp Range",   0, 1200000f, GlobalStats.MinimumWarpRange);
            IncreaseMaintenance = Slider(maintenanceRect,  "Increase Maintenance", 1, 10f,      GlobalStats.ShipMaintenanceMulti);

            FTLPenaltySlider.Tip = 2286;
            EnemyFTLPenaltySlider.Tip = 7041;
            GravityWellSize.Tip = 6003;

            string extraPlanetsTip = "Add extra planets to each system, avoiding lone stars as well";
            if (GlobalStats.ModChangeResearchCost)
                extraPlanetsTip += ". This will slightly increase research cost per technology.";

            extraPlanets.Tip = extraPlanetsTip;
            MinimumWarpRange.Tip = "Minimum warp range a ship must have before it needs to recharge for the AI to build it";

            IncreaseMaintenance.Tip = "Multiply Global Maintenance Cost By  SSP's Are Not Affected";
            TurnTimer.Tip = "Time in seconds for turns";
            StartingRichness.Tip = "Add to all Starting Empire Planets this Value";



            Label(MainMenu.Menu.X + 40, MainMenu.Menu.Y + 40, "Advanced Rule Options", Fonts.Arial20Bold);
            string text = Fonts.Arial12.ParseText(Localizer.Token(2289), MainMenu.Menu.Width - 80);
            Label(MainMenu.Menu.X + 40, MainMenu.Menu.Y + 40 + Fonts.Arial20Bold.LineSpacing + 2, text, Fonts.Arial12);
        }
    }
}