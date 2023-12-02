using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Universe;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game;

public sealed class RuleOptionsScreen : GameScreen
{
    readonly UniverseParams P;

    Menu2 MainMenu;
    FloatSlider FTLPenaltySlider;
    FloatSlider EnemyFTLPenaltySlider;
    FloatSlider GravityWellSize;
    FloatSlider ExtraPlanets;
    FloatSlider IncreaseMaintenance;
    FloatSlider MinAcceptableShipWarpRange;
    FloatSlider StartingRichness;
    FloatSlider TurnTimer;
    FloatSlider CustomMineralDecay;
    FloatSlider VolcanicActivity;

    public RuleOptionsScreen(GameScreen parent, UniverseParams settings) : base(parent, toPause: null)
    {
        P = settings;
        IsPopup = true;
        TransitionOnTime  = 0.25f;
        TransitionOffTime = 0.25f;
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
        batch.SafeBegin();
        base.Draw(batch, elapsed);
        batch.SafeEnd();
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
        FTLPenaltySlider = Add(new FloatSlider(SliderStyle.Percent, ftlRect,
                                               GameText.InsystemFtlSpeedModifier, 0.1f, 1f, P.FTLModifier));
        FTLPenaltySlider.OnChange = (s) => P.FTLModifier = s.AbsoluteValue;

        var eftlRect = new Rectangle(x, leftRect.Y + 150, 270, 50);
        EnemyFTLPenaltySlider = Add(new FloatSlider(SliderStyle.Percent, eftlRect, 
                                                    GameText.InsystemEnemyFtlSpeedModifier, 0.1f, 1f, P.EnemyFTLModifier));
        EnemyFTLPenaltySlider.OnChange = (s) => P.EnemyFTLModifier = s.AbsoluteValue;
            
        int indent = (int)(width / 4.5f);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 25*0, () => P.PreventFederations, title: GameText.PreventAiFederations, tooltip: GameText.PreventsAiEmpiresFromMerging);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 25*1, () => P.FixedPlayerCreditCharge, title: GameText.FixedShipAndBuildingsCost, tooltip: GameText.KeepFixedCreditCostOf);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 25*2, () => P.AIUsesPlayerDesigns, title: GameText.UsePlayerDesignsTitle, tooltip: GameText.UsePlayerDesignsTip);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 25*3, () => P.DisablePirates, title: GameText.DisablePirates, tooltip: GameText.DisablesAllPirateFactionsFor);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 25*4, () => P.DisableRemnantStory, title: GameText.DisableRemnantStory, tooltip: GameText.IfCheckedRemnantForcesIn);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 25*5, () => P.DisableAlternateAITraits, title: GameText.DisableAlternateTraits, tooltip: GameText.DisableAlternateTraitsTip);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 25*6, () => P.DisableResearchStations, title: GameText.DisableResearchStationsName, tooltip: GameText.DisableResearchStationsTip);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 25*7, () => P.DisableMiningOps, title: GameText.DisableMiningOpsName, tooltip: GameText.DisableMiningOpsTip);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 25*8, () => P.UseUpkeepByHullSize, title: GameText.RuleOptionsUseHullUpkeepName, tooltip: GameText.RuleOptionsUseHullUpkeepTip);

        var mdRect = new Rectangle(ftlRect.X + indent+2, ftlRect.Y + 230, 270, 50);
        CustomMineralDecay = SliderDecimal1(mdRect, GameText.MineralDecayRate, 0.2f, 3, P.CustomMineralDecay);
        CustomMineralDecay.OnChange = (s) => P.CustomMineralDecay = (s.AbsoluteValue).RoundToFractionOf10();

        var vaRect = new Rectangle(ftlRect.X + indent + 2, ftlRect.Y + 290, 270, 50);
        VolcanicActivity = SliderDecimal1(vaRect, GameText.VolcanicActivity, 0.5f, 3, P.VolcanicActivity);
        VolcanicActivity.OnChange = (s) => P.VolcanicActivity = (s.AbsoluteValue).RoundToFractionOf10();

        var gwRect = new Rectangle(x, leftRect.Y + 210, 270, 50);
        var epRect = new Rectangle(x, leftRect.Y + 270, 270, 50);
        var richnessRect = new Rectangle(x, leftRect.Y + 330, 270, 50);

        GravityWellSize = Slider(gwRect, GameText.GravityWellRadius, 0, 20000, P.GravityWellRange);
        GravityWellSize.OnChange = (s) => P.GravityWellRange = s.AbsoluteValue;

        ExtraPlanets = Slider(epRect, GameText.ExtraPlanets, 0, 3f, P.ExtraPlanets);
        ExtraPlanets.OnChange = (s) => P.ExtraPlanets = (int)s.AbsoluteValue;

        StartingRichness = Slider(richnessRect, GameText.StartingPlanetRichnessBonus, 0, 5f, P.StartingPlanetRichnessBonus);
        StartingRichness.OnChange = (s) => P.StartingPlanetRichnessBonus = s.AbsoluteValue;


        var optionTurnTimer  = new Rectangle(x, leftRect.Y + 390, 270, 50);
        var minimumWarpRange = new Rectangle(x, leftRect.Y + 450, 270, 50);
        var maintenanceRect  = new Rectangle(x, leftRect.Y + 510, 270, 50);

        TurnTimer = Slider(optionTurnTimer,  GameText.SecondsPerTurn, 2, 18f, P.TurnTimer);
        TurnTimer.OnChange = (s) => P.TurnTimer = (int)s.AbsoluteValue;

        MinAcceptableShipWarpRange = Slider(minimumWarpRange, GameText.MinAcceptableShipWarpRange, 0, 1200000f, P.MinAcceptableShipWarpRange);
        MinAcceptableShipWarpRange.OnChange = (s) => P.MinAcceptableShipWarpRange = s.AbsoluteValue;

        IncreaseMaintenance = Slider(maintenanceRect,  GameText.MaintenanceMultiplier, 1, 10f, P.ShipMaintenanceMultiplier);
        IncreaseMaintenance.OnChange = (s) => P.ShipMaintenanceMultiplier = s.AbsoluteValue;

        EnemyFTLPenaltySlider.Tip = GameText.UsingThisSliderYouCan2;
        CustomMineralDecay.Tip = GameText.HigherMineralDecayIncreasesThe;
        VolcanicActivity.Tip = GameText.ThisWillControlTheChances;
        FTLPenaltySlider.Tip = GameText.UsingThisSliderYouCan;
        GravityWellSize.Tip = GameText.DefinesTheRadiusOfPlanetary;

        string extraPlanetsTip = Localizer.Token(GameText.AddExtraPlanetsToEach);
        if (GlobalStats.Defaults.ChangeResearchCostBasedOnSize)
            extraPlanetsTip = $"{extraPlanetsTip} {Localizer.Token(GameText.ThisWillSlightlyIncreaseResearch)}";

        ExtraPlanets.Tip = extraPlanetsTip;
        MinAcceptableShipWarpRange.Tip = GameText.MinAcceptableWarpRangeAShip;
        IncreaseMaintenance.Tip = GameText.MultiplyGlobalMaintenanceCostBy;
        StartingRichness.Tip = GameText.AddToAllStartingEmpire;
        TurnTimer.Tip = GameText.TimeInSecondsPerTurn;


        Label(MainMenu.Menu.X + 40, MainMenu.Menu.Y + 40, GameText.AdvancedRuleOptions, Fonts.Arial20Bold);
        string text = Fonts.Arial12.ParseText(GameText.InThisPanelYouMay, MainMenu.Menu.Width - 80);
        Label(MainMenu.Menu.X + 40, MainMenu.Menu.Y + 40 + Fonts.Arial20Bold.LineSpacing + 2, text, Fonts.Arial12);
    }
}