using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Universe;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game;

public sealed class RuleOptionsScreen : GameScreen
{
    readonly UniverseParams Settings;

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
        Settings = settings;
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
        FTLPenaltySlider = SliderPercent(ftlRect, Localizer.Token(GameText.InsystemFtlSpeedModifier), 0f, 1f, Settings.FTLModifier);
        FTLPenaltySlider.OnChange = (s) => Settings.FTLModifier = s.AbsoluteValue;

        var eftlRect = new Rectangle(x, leftRect.Y + 150, 270, 50);
        EnemyFTLPenaltySlider = SliderPercent(eftlRect, Localizer.Token(GameText.InsystemEnemyFtlSpeedModifier), 0f, 1f, Settings.EnemyFTLModifier);
        EnemyFTLPenaltySlider.OnChange = (s) => Settings.EnemyFTLModifier = s.AbsoluteValue;
            
        int indent = (int)(width / 4.5f); 
        Checkbox(ftlRect.X + indent, ftlRect.Y, () => Settings.PreventFederations, title: GameText.PreventAiFederations, tooltip: GameText.PreventsAiEmpiresFromMerging);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 25,() => Settings.FTLInNeutralSystems, title: GameText.TreatNeutralSystemsAsUnfriendly, tooltip: GameText.TreatNeutralSystemsAsUnfriendly);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 50, () => Settings.FixedPlayerCreditCharge, title: GameText.FixedShipAndBuildingsCost, tooltip: GameText.KeepFixedCreditCostOf);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 75, () => Settings.AIUsesPlayerDesigns, title: GameText.UsePlayerDesignsTitle, tooltip: GameText.UsePlayerDesignsTip);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 100, () => Settings.DisablePirates, title: GameText.DisablePirates, tooltip: GameText.DisablesAllPirateFactionsFor);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 125, () => Settings.DisableRemnantStory, title: GameText.DisableRemnantStory, tooltip: GameText.IfCheckedRemnantForcesIn);
        Checkbox(ftlRect.X + indent, ftlRect.Y + 150, () => Settings.UseUpkeepByHullSize, title: GameText.RuleOptionsUseHullUpkeepName, tooltip: GameText.RuleOptionsUseHullUpkeepTip);

        var mdRect = new Rectangle(ftlRect.X + indent+2, ftlRect.Y + 230, 270, 50);
        CustomMineralDecay = SliderDecimal1(mdRect, Localizer.Token(GameText.MineralDecayRate), 0.5f, 3, Settings.CustomMineralDecay);
        CustomMineralDecay.OnChange = (s) => Settings.CustomMineralDecay = (s.AbsoluteValue).RoundToFractionOf10();

        var vaRect = new Rectangle(ftlRect.X + indent + 2, ftlRect.Y + 290, 270, 50);
        VolcanicActivity = SliderDecimal1(vaRect, Localizer.Token(GameText.VolcanicActivity), 0.5f, 3, Settings.VolcanicActivity);
        VolcanicActivity.OnChange = (s) => Settings.VolcanicActivity = (s.AbsoluteValue).RoundToFractionOf10();

        var gwRect = new Rectangle(x, leftRect.Y + 210, 270, 50);
        var epRect = new Rectangle(x, leftRect.Y + 270, 270, 50);
        var richnessRect = new Rectangle(x, leftRect.Y + 330, 270, 50);

        GravityWellSize = Slider(gwRect, GameText.GravityWellRadius, 0, 20000, Settings.GravityWellRange);
        GravityWellSize.OnChange = (s) => Settings.GravityWellRange = s.AbsoluteValue;

        ExtraPlanets = Slider(epRect, GameText.ExtraPlanets, 0, 3f, Settings.ExtraPlanets);
        ExtraPlanets.OnChange = (s) => Settings.ExtraPlanets = (int)s.AbsoluteValue;

        StartingRichness = Slider(richnessRect, GameText.StartingPlanetRichnessBonus, 0, 5f, Settings.StartingPlanetRichness);
        StartingRichness.OnChange = (s) => Settings.StartingPlanetRichness = s.AbsoluteValue;


        var optionTurnTimer  = new Rectangle(x, leftRect.Y + 390, 270, 50);
        var minimumWarpRange = new Rectangle(x, leftRect.Y + 450, 270, 50);
        var maintenanceRect  = new Rectangle(x, leftRect.Y + 510, 270, 50);

        TurnTimer = Slider(optionTurnTimer,  GameText.SecondsPerTurn, 2, 18f, Settings.TurnTimer);
        TurnTimer.OnChange = (s) => Settings.TurnTimer = (int)s.AbsoluteValue;

        MinAcceptableShipWarpRange = Slider(minimumWarpRange, GameText.MinAcceptableShipWarpRange, 0, 1200000f, Settings.MinAcceptableShipWarpRange);
        MinAcceptableShipWarpRange.OnChange = (s) => Settings.MinAcceptableShipWarpRange = s.AbsoluteValue;

        IncreaseMaintenance = Slider(maintenanceRect,  GameText.MaintenanceMultiplier, 1, 10f, Settings.ShipMaintenanceMultiplier);
        IncreaseMaintenance.OnChange = (s) => Settings.ShipMaintenanceMultiplier = s.AbsoluteValue;

        EnemyFTLPenaltySlider.Tip = GameText.UsingThisSliderYouCan2;
        CustomMineralDecay.Tip = GameText.HigherMineralDecayIncreasesThe;
        VolcanicActivity.Tip = GameText.ThisWillControlTheChances;
        FTLPenaltySlider.Tip = GameText.UsingThisSliderYouCan;
        GravityWellSize.Tip = GameText.DefinesTheRadiusOfPlanetary;

        string extraPlanetsTip = Localizer.Token(GameText.AddExtraPlanetsToEach);
        if (GlobalStats.Settings.ChangeResearchCostBasedOnSize)
            extraPlanetsTip = $"{extraPlanetsTip} {Localizer.Token(GameText.ThisWillSlightlyIncreaseResearch)}";

        ExtraPlanets.Tip = extraPlanetsTip;
        MinAcceptableShipWarpRange.Tip = GameText.MinAcceptableWarpRangeAShip;
        IncreaseMaintenance.Tip = GameText.MultiplyGlobalMaintenanceCostBy;
        StartingRichness.Tip = GameText.AddToAllStartingEmpire;
        TurnTimer.Tip = GameText.TimeInSecondsPerTurn;


        Label(MainMenu.Menu.X + 40, MainMenu.Menu.Y + 40, GameText.AdvancedRuleOptions, Fonts.Arial20Bold);
        string text = Fonts.Arial12.ParseText(Localizer.Token(GameText.InThisPanelYouMay), MainMenu.Menu.Width - 80);
        Label(MainMenu.Menu.X + 40, MainMenu.Menu.Y + 40 + Fonts.Arial20Bold.LineSpacing + 2, text, Fonts.Arial12);
    }
}