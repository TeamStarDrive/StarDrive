using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDGraphics.Input;
using Ship_Game;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Kind = Ship_Game.EmpireAssetsPanel.AssetKind;

namespace UnitTests.UI;

[TestClass]
public class EmpireAssetsPanelTests : StarDriveTest
{
    readonly MockInputProvider Provider = new();
    readonly InputState Input;
    readonly EmpireAssetsPanel Panel;
    int FleetHotkeys;
    public TestContext TestContext { get; set; }

    public EmpireAssetsPanelTests()
    {
        Game.Tick(); // UITextEntry uses the game's current frame time.
        CreateUniverseAndPlayerEmpire();
        Universe.pieMenu = new PieMenu();
        Universe.aw = new AutomationWindow(Universe);
        Input = new InputState { Provider = Provider };
        Panel = new EmpireAssetsPanel(new RectF(8, 60, 350, 450), Universe,
            _ => { }, _ => ++FleetHotkeys, _ => false);
    }

    bool MoveTo(int x, int y, bool click = false)
    {
        Provider.SetMouse(x, y);
        Provider.LeftMouse = click ? ButtonState.Pressed : ButtonState.Released;
        Input.Update(new UpdateTimes(0.016f, 1));
        return Panel.HandleInput(Input);
    }

    [TestMethod]
    public void CollapsingReleasesMapAreaButKeepsLeftRailInteractive()
    {
        Assert.IsTrue(MoveTo(100, 220), "Expanded panel must capture map input.");
        Assert.IsTrue(MoveTo(100, 75, click: true));
        Assert.IsFalse(MoveTo(100, 220), "Collapsed body must release map input.");
        Assert.IsFalse(MoveTo(100, 75), "The former header area must also release map input.");
        Assert.IsTrue(MoveTo(25, 475, click: true), "Unused rail space must capture clicks.");
        Assert.IsFalse(MoveTo(100, 220), "Reserved icon space must not expand the panel.");
        Assert.IsTrue(MoveTo(25, 75, click: true), "The rail arrow must expand the panel.");
        Assert.IsTrue(MoveTo(100, 220));
        Assert.IsFalse(MoveTo(500, 220));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AutomationRailActionWorksInBothStates(bool collapsed)
    {
        if (collapsed)
        {
            Assert.IsTrue(MoveTo(25, 75, click: true));
            MoveTo(25, 75);
        }
        else
        {
            Assert.IsTrue(MoveTo(80, 110, click: true));
            Assert.IsTrue(GlobalStats.TakingInput);
            MoveTo(80, 110);
        }
        Assert.IsTrue(MoveTo(25, 115, click: true));
        Assert.IsTrue(Universe.aw.IsOpen, "Second icon must open the existing automation panel.");
        Assert.IsFalse(GlobalStats.TakingInput);
        MoveTo(25, 115);
        Assert.IsTrue(MoveTo(25, 115, click: true));
        Assert.IsFalse(Universe.aw.IsOpen);
        Assert.IsFalse(MoveTo(100, 220), "Clicking the active Automation icon must collapse its sidebar.");
        Assert.IsTrue(MoveTo(25, 75, click: true));
        Assert.IsTrue(MoveTo(100, 220), "The first icon must restore the assets view.");
    }

    [TestMethod]
    public void EmbeddedAutomationChangesSettingsAndPreservesAssetView()
    {
        Universe.aw.ToggleVisibility(); // Same entry point as H and the minimap button.
        Assert.IsTrue(Universe.aw.IsOpen);
        Player.AutoExplore = false;
        Assert.IsTrue(MoveTo(80, 130, click: true));
        Assert.IsTrue(Player.AutoExplore);
        MoveTo(80, 130);
        Assert.IsTrue(MoveTo(80, 100, click: true)); // Fold Ships & Stations.
        MoveTo(80, 100);
        Player.AutoResearch = false;
        Assert.IsTrue(MoveTo(80, 157, click: true));
        Assert.IsTrue(Player.AutoResearch);
        MoveTo(80, 157);
        Assert.IsTrue(MoveTo(80, 336, click: true)); // Unfold Alerts & Policies.
        MoveTo(80, 336);
        UState.P.SuppressOnBuildNotifications = false;
        Assert.IsTrue(MoveTo(80, 365, click: true));
        Assert.IsTrue(UState.P.SuppressOnBuildNotifications, "Turning off Building alerts must set the saved suppression flag.");
        MoveTo(80, 365);
        Assert.IsTrue(MoveTo(25, 75, click: true));
        Assert.IsFalse(Universe.aw.IsOpen, "Assets must replace Automation in the same sidebar.");
        Assert.IsTrue(Player.AutoExplore);
        Assert.IsTrue(Player.AutoResearch);
        Assert.IsTrue(MoveTo(100, 220));
    }

    [TestMethod]
    public void EmbeddedDesignSelectorSupportsAutomaticManualAndOutsideDismissal()
    {
        Player.AutoColonize = true;
        Player.AutoPickBestColonizer = false;
        Universe.aw.ToggleVisibility();
        Assert.IsTrue(MoveTo(120, 211, click: true)); // Colony design dropdown.
        MoveTo(120, 211);
        Assert.IsTrue(MoveTo(120, 234, click: true)); // Automatic.
        Assert.IsTrue(Player.AutoPickBestColonizer);
        MoveTo(120, 234);
        Assert.IsTrue(MoveTo(120, 211, click: true));
        MoveTo(120, 211);
        Assert.IsTrue(MoveTo(120, 257, click: true)); // First concrete design.
        Assert.IsFalse(Player.AutoPickBestColonizer);
        Assert.IsFalse(string.IsNullOrEmpty(Player.data.CurrentAutoColony));
        MoveTo(120, 257);
        Assert.IsTrue(MoveTo(120, 211, click: true));
        MoveTo(120, 211);
        Player.AutoExplore = false;
        Assert.IsTrue(MoveTo(80, 130, click: true));
        Assert.IsFalse(Player.AutoExplore, "Closing a popup must not toggle the row behind it.");
    }

    [TestMethod]
    public void AutomationScrollingKeepsAllSettingsReachableAndCapturesWheel()
    {
        Universe.aw.ToggleVisibility();
        for (int i = 0; i < 30; ++i)
        {
            Provider.ScrollWheel -= 120;
            Assert.IsTrue(MoveTo(100, 220));
        }
        // At the bottom the final collapsed Alerts header is visible. Scrolling back
        // to the top must restore Exploration and its input target exactly.
        for (int i = 0; i < 30; ++i)
        {
            Provider.ScrollWheel += 120;
            Assert.IsTrue(MoveTo(100, 220));
        }
        Player.AutoExplore = false;
        Assert.IsTrue(MoveTo(80, 130, click: true));
        Assert.IsTrue(Player.AutoExplore);
        Assert.IsFalse(MoveTo(500, 220), "The sidebar must release input outside its frame.");
    }

    [TestMethod]
    public void StationAutomationRequiresUnlockAndRushRunsOnSimulationThread()
    {
        Assert.IsFalse(Player.CanBuildResearchStations);
        Universe.aw.ToggleVisibility();
        Player.AutoBuildResearchStations = false;
        Assert.IsTrue(MoveTo(80, 354, click: true));
        Assert.IsFalse(Player.AutoBuildResearchStations);
        MoveTo(80, 354);
        LoadStarterShips("Basic Research Station");
        Player.MarkShipRolesUsableForEmpire(ResourceManager.GetShipTemplate("Basic Research Station").ShipData);
        Assert.IsTrue(MoveTo(80, 354, click: true));
        Assert.IsTrue(Player.AutoBuildResearchStations, "Unlocking a station while the panel is open must enable its existing row.");
        MoveTo(80, 354);
        Assert.IsTrue(MoveTo(120, 381, click: true)); // Newly enabled station design dropdown.
        MoveTo(120, 381);
        Assert.IsTrue(MoveTo(120, 402, click: true));
        Assert.IsTrue(Player.AutoPickBestResearchStation);
        MoveTo(120, 402);
        Assert.IsTrue(MoveTo(80, 100, click: true)); // Fold ship rows.
        MoveTo(80, 100);
        Player.RushAllConstruction = false;
        Assert.IsTrue(MoveTo(80, 246, click: true));
        Assert.IsTrue(Player.RushAllConstruction);
        Universe.InvokePendingSimThreadActions();
        Assert.IsTrue(Player.RushAllConstruction);
    }

    [TestMethod]
    public void SearchSuppressesFleetHotkeysAndHiddenPanelReleasesFocus()
    {
        try
        {
            Assert.IsTrue(MoveTo(80, 110, click: true));
            Assert.IsTrue(GlobalStats.TakingInput);
            Provider.KeysDown.Add(Keys.D1);
            MoveTo(80, 110);
            Assert.AreEqual(0, FleetHotkeys, "Typing digits must not select fleets.");
            Universe.LookingAtPlanet = true;
            Panel.Update(0.016f);
            Assert.IsFalse(GlobalStats.TakingInput, "Hiding the panel must release text capture.");
            Assert.IsFalse(MoveTo(100, 220));
        }
        finally
        {
            GlobalStats.TakingInput = false;
        }
    }

    [TestMethod]
    public void FleetHotkeysRemainAvailableWithCollapsedPanel()
    {
        MoveTo(100, 75, click: true);
        Provider.KeysDown.Add(Keys.D1);
        Assert.IsTrue(MoveTo(500, 220));
        Assert.AreEqual(1, FleetHotkeys);
    }

    [TestMethod]
    public void StationCategoriesAreExclusiveAndExcludeMobileShipsAndProjectors()
    {
        LoadStarterShips("Platform Base mk1-a", "Shipyard");
        Assert.AreEqual(Kind.Research, EmpireAssetsPanel.ClassifyStation(SpawnShip("Basic Research Station", Player, new Vector2(1000))));
        Assert.AreEqual(Kind.Mining, EmpireAssetsPanel.ClassifyStation(SpawnShip("Basic Mining Station", Player, new Vector2(2000))));
        Assert.AreEqual(Kind.Starbases, EmpireAssetsPanel.ClassifyStation(SpawnShip("Platform Base mk1-a", Player, new Vector2(3000))));
        Assert.AreEqual(Kind.Starbases, EmpireAssetsPanel.ClassifyStation(SpawnShip("Shipyard", Player, new Vector2(4000))));
        Assert.AreEqual(Kind.All, EmpireAssetsPanel.ClassifyStation(SpawnShip("Rocket Scout", Player, new Vector2(5000))));
        Assert.AreEqual(Kind.All, EmpireAssetsPanel.ClassifyStation(SpawnShip("Subspace Projector", Player, new Vector2(6000))));
        SpawnShip("Basic Research Station", Enemy, new Vector2(7000));
        UState.Objects.UpdateLists();
        Panel.Update(1);
        Assert.AreEqual(1, Panel.AssetCount(Kind.Research), "Enemy stations must not appear in quick access.");
        Assert.AreEqual(1, Panel.AssetCount(Kind.Mining));
        Assert.AreEqual(2, Panel.AssetCount(Kind.Starbases));
        Assert.AreEqual(4, Panel.AssetCount(Kind.All));
    }

    Planet ProductionPlanet()
    {
        Planet planet = AddHomeWorldToEmpire(new Vector2(2000), Player);
        planet.ProdHere = 50;
        Player.Money = 10000;
        Assert.IsTrue(planet.Construction.Enqueue(ResourceManager.GetBuildingTemplate("Nano Mine")));
        QueueItem item = planet.ConstructionQueue[0];
        item.Cost = 1000;
        item.ProductionSpent = 0;
        return planet;
    }

    [TestMethod]
    public void PlanetAssignmentPreservesFleetAndRecallsPlanet()
    {
        Planet planet = AddHomeWorldToEmpire(new Vector2(2000), Player);
        Universe.pInfoUI = new PlanetInfoUIElement(new SDGraphics.Rectangle(0, 500, 407, 242), Game.Manager, Universe);
        var fleet = Player.CreateFleet(1, "Existing fleet");
        fleet.AddShip(SpawnShip("Rocket Scout", Player, new Vector2(1000)));
        Universe.SelectedPlanet = planet;
        Provider.KeysDown.Add(Keys.LeftControl);
        Provider.KeysDown.Add(Keys.D1);
        Assert.IsTrue(MoveTo(500, 220));
        Assert.AreEqual(planet.Id, Universe.UState.PlanetHotkeyIds[0]);
        Assert.AreEqual(1, fleet.CountShips);
        Assert.AreEqual(0, FleetHotkeys);
        Provider.KeysDown.Clear();
        MoveTo(500, 220);
        Universe.SelectedPlanet = null;
        Provider.KeysDown.Add(Keys.D1);
        Assert.IsTrue(MoveTo(500, 220));
        Assert.AreSame(planet, Universe.SelectedPlanet);
        Assert.AreEqual(0, FleetHotkeys);
        Provider.KeysDown.Clear();
        MoveTo(500, 220);
        Universe.SelectedPlanet = null;
        Provider.KeysDown.Add(Keys.LeftControl);
        Provider.KeysDown.Add(Keys.D1);
        MoveTo(500, 220);
        Assert.AreEqual(0, Universe.UState.PlanetHotkeyIds[0]);
        Assert.AreEqual(1, FleetHotkeys, "Ctrl+number without a planet restores fleet handling.");
    }

    [TestMethod]
    public void PlanetKeysRejectForeignAndStalePlanets()
    {
        Planet foreign = AddHomeWorldToEmpire(new Vector2(2000), Enemy);
        Universe.SelectedPlanet = foreign;
        Provider.KeysDown.Add(Keys.LeftControl);
        Provider.KeysDown.Add(Keys.D1);
        MoveTo(500, 220);
        Assert.AreEqual(0, FleetHotkeys, "Foreign planet assignment must not clear fleets.");
        Assert.AreEqual(0, Universe.UState.PlanetHotkeyIds[0]);
        Universe.UState.PlanetHotkeyIds[0] = foreign.Id;
        Provider.KeysDown.Clear();
        MoveTo(500, 220);
        Provider.KeysDown.Add(Keys.D1);
        MoveTo(500, 220);
        Assert.AreEqual(0, Universe.UState.PlanetHotkeyIds[0]);
        Assert.AreEqual(1, FleetHotkeys);
    }

    [TestMethod]
    public void ResourceSortsUseNetIncomeAndReverseDirection()
    {
        Planet alpha = AddHomeWorldToEmpire(new Vector2(2000), Player);
        Planet beta = AddHomeWorldToEmpire(new Vector2(5000), Player);
        alpha.Name = "Alpha";
        beta.Name = "Beta";
        // Different labor allocations produce distinct net incomes for each resource.
        foreach (var sort in new[] { EmpireAssetsPanel.AssetSort.Food, EmpireAssetsPanel.AssetSort.Production,
                                     EmpireAssetsPanel.AssetSort.Research })
        {
            var a = sort == EmpireAssetsPanel.AssetSort.Food ? alpha.Food : sort == EmpireAssetsPanel.AssetSort.Production ? alpha.Prod : alpha.Res;
            var b = sort == EmpireAssetsPanel.AssetSort.Food ? beta.Food : sort == EmpireAssetsPanel.AssetSort.Production ? beta.Prod : beta.Res;
            a.Percent = 0;
            b.Percent = 1;
            a.Update(0);
            b.Update(0);
            Assert.IsTrue(a.NetIncome < b.NetIncome);
            Panel.SetSort(sort);
            CollectionAssert.AreEqual(new[] { beta, alpha }, Panel.ListedPlanets);
            Panel.SetSort(sort);
            CollectionAssert.AreEqual(new[] { alpha, beta }, Panel.ListedPlanets);
        }
        Panel.SetSort(EmpireAssetsPanel.AssetSort.Name);
        CollectionAssert.AreEqual(new[] { alpha, beta }, Panel.ListedPlanets);
    }

    [TestMethod]
    public void RushUsesStoredProductionAndPreservesColonyModifierBehavior()
    {
        Planet planet = ProductionPlanet();
        QueueItem item = planet.ConstructionQueue[0];
        Assert.IsTrue(EmpireAssetsPanel.RushItem(planet, Player, item, all: false, continuous: false));
        Assert.AreEqual(10f, item.ProductionSpent);
        Assert.AreEqual(40f, planet.ProdHere);
        Assert.IsTrue(EmpireAssetsPanel.RushItem(planet, Player, item, all: false, continuous: true));
        Assert.IsTrue(item.Rush);
        Assert.AreEqual(40f, planet.ProdHere, "Continuous rush toggle must not immediately spend production.");
        Assert.IsTrue(EmpireAssetsPanel.RushItem(planet, Player, item, all: true, continuous: false));
        Assert.AreEqual(50f, item.ProductionSpent);
        Assert.AreEqual(0f, planet.ProdHere);
    }

    [TestMethod]
    public void RushRejectsStaleQueueItemsAndLostOwnership()
    {
        Planet planet = ProductionPlanet();
        QueueItem item = planet.ConstructionQueue[0];
        Assert.IsFalse(EmpireAssetsPanel.RushItem(planet, Enemy, item, all: true, continuous: false));
        planet.Construction.Cancel(item);
        Assert.IsTrue(planet.Construction.Enqueue(ResourceManager.GetBuildingTemplate("Nano Mine")));
        QueueItem replacement = planet.ConstructionQueue[0];
        Assert.IsFalse(EmpireAssetsPanel.RushItem(planet, Player, item, all: true, continuous: true));
        Assert.IsFalse(replacement.Rush, "A stale click must never rush the replacement item.");
        Assert.AreEqual(0f, replacement.ProductionSpent);
    }

    [TestMethod]
    public void RushClickIsDeferredAndDoesNotSelectThePlanet()
    {
        Planet planet = ProductionPlanet();
        QueueItem item = planet.ConstructionQueue[0];
        Panel.Update(1);
        // Two compact section headers precede the planet row in the All tab.
        Assert.IsTrue(MoveTo(332, 235, click: true));
        Assert.AreEqual(0f, item.ProductionSpent, "UI thread must not mutate the construction queue.");
        Assert.IsNull(Universe.SelectedPlanet, "Rush must not also trigger row selection.");
        Universe.InvokePendingSimThreadActions();
        Assert.AreEqual(10f, item.ProductionSpent);
    }

    [TestMethod]
    [DataRow(false, 350)]
    [DataRow(true, 350)]
    [DataRow(false, 691)]
    [DataRow(true, 691)]
    public void EveryNavigationActionIsReachableWithoutMovingPinnedButtons(bool collapsed, int height)
    {
        Panel.RectF = new RectF(0, 110, 298, height);
        Panel.Update(1);
        if (collapsed) MoveTo(100, 125, click: true);
        var seen = new HashSet<EmpireAssetsPanel.SidebarAction>();
        for (int step = 0; step < 25; ++step)
        {
            var buttons = Panel.NavigationButtons().ToArray();
            Assert.AreEqual(EmpireAssetsPanel.SidebarAction.Assets, buttons[0].Action);
            Assert.AreEqual(EmpireAssetsPanel.SidebarAction.Automation, buttons[1].Action);
            Assert.AreEqual(110f, buttons[0].Rect.Y);
            Assert.AreEqual(152f, buttons[1].Rect.Y);
            foreach (var (action, rect) in buttons)
            {
                Assert.IsTrue(rect.X >= 0 && rect.Right <= 48);
                Assert.IsTrue(rect.Bottom <= 110 + height);
                seen.Add(action);
            }
            Provider.ScrollWheel -= 120;
            Assert.IsTrue(MoveTo(20, 230), "Rail scrolling must not leak into the map.");
        }
        Assert.AreEqual(Enum.GetValues<EmpireAssetsPanel.SidebarAction>().Length, seen.Count);
        Assert.IsTrue(seen.Contains(EmpireAssetsPanel.SidebarAction.Menu));
        Assert.IsTrue(seen.Contains(EmpireAssetsPanel.SidebarAction.Help));
    }

    [TestMethod]
    public void ConstructionActionWorksAfterScrollingCollapsedRail()
    {
        MoveTo(100, 75, click: true);
        for (int step = 0; step < 20; ++step)
        {
            Provider.ScrollWheel -= 120;
            MoveTo(20, 230);
        }
        var button = Panel.NavigationButtons().First(b => b.Action == EmpireAssetsPanel.SidebarAction.Construction);
        bool before = Universe.DeepSpaceBuildWindow.Visible;
        Assert.IsTrue(MoveTo((int)button.Rect.X + 16, (int)button.Rect.Y + 16, click: true));
        Assert.AreEqual(!before, Universe.DeepSpaceBuildWindow.Visible);
    }

    [TestMethod]
    public void GalaxyTopBarDoesNotKeepInvisibleMouseNavigation()
    {
        var overlay = new EmpireUIOverlay(Player, Game.GraphicsDevice, Universe);
        Provider.SetMouse(50, 15);
        Provider.LeftMouse = ButtonState.Pressed;
        Input.Update(new UpdateTimes(0.016f, 1));
        int screens = Universe.ScreenManager.NumScreens;
        Assert.IsFalse(overlay.HandleInput(Input));
        Assert.AreEqual(screens, Universe.ScreenManager.NumScreens);
    }

    [TestMethod]
    public void DashboardSpeedButtonsKeepPauseStateAndCaptureOnlyBar()
    {
        var overlay = new EmpireUIOverlay(Player, Game.GraphicsDevice, Universe);
        bool At(RectF rect, bool click)
        {
            Provider.SetMouse((int)rect.X + 10, (int)rect.Y + 10);
            Provider.LeftMouse = click ? ButtonState.Pressed : ButtonState.Released;
            Input.Update(new UpdateTimes(.016f, 1));
            return overlay.HandleDashboardInput(Input);
        }
        UState.Paused = true;
        foreach (var pair in new[] { (1, .5f), (2, 1f), (3, 2f), (4, 4f) })
        {
            At(overlay.DashboardSpeedRect(pair.Item1), false);
            Assert.IsTrue(At(overlay.DashboardSpeedRect(pair.Item1), true));
            Assert.AreEqual(pair.Item2, UState.GameSpeed);
            Assert.IsTrue(UState.Paused);
        }
        At(overlay.DashboardSpeedRect(0), false);
        At(overlay.DashboardSpeedRect(0), true);
        Assert.IsFalse(UState.Paused);
        Assert.IsFalse(At(new RectF(500, 80, 20, 20), false));
        Assert.IsTrue(At(overlay.DashboardItemRect(EmpireUIOverlay.DashboardItem.Food), false));
        Provider.KeysDown.Add(Keys.R);
        Assert.IsFalse(At(overlay.DashboardItemRect(EmpireUIOverlay.DashboardItem.Food), false), "Hover must allow keyboard shortcuts through.");
        Provider.KeysDown.Clear();
    }

    [TestMethod]
    public void DashboardAggregatesLocalStorageAndHandlesNoFreighters()
    {
        Planet first = ProductionPlanet();
        Planet second = AddHomeWorldToEmpire(new Vector2(5000), Player);
        first.FoodHere = 20; second.FoodHere = 30;
        first.ProdHere = 40; second.ProdHere = 60;
        var overlay = new EmpireUIOverlay(Player, Game.GraphicsDevice, Universe);
        overlay.RefreshDashboard();
        Assert.AreEqual("50", overlay.DashboardValue(EmpireUIOverlay.DashboardItem.Food));
        Assert.AreEqual("100", overlay.DashboardValue(EmpireUIOverlay.DashboardItem.Production));
        Assert.AreEqual("2", overlay.DashboardValue(EmpireUIOverlay.DashboardItem.Empire));
        Assert.AreEqual("--", overlay.DashboardValue(EmpireUIOverlay.DashboardItem.Freight));
        StringAssert.Contains(overlay.DashboardTip(EmpireUIOverlay.DashboardItem.Food), "held locally");
        Assert.IsFalse(overlay.DashboardTip(EmpireUIOverlay.DashboardItem.Freight).Contains("NaN"));
    }

    [TestMethod]
    public void DashboardMusicPauseSurvivesAutomaticPlaybackAndTrackSkip()
    {
        var overlay = new EmpireUIOverlay(Player, Game.GraphicsDevice, Universe);
        var manager = Universe.ScreenManager;
        bool previousPause = manager.AmbientMusicPaused;
        float speed = UState.GameSpeed;
        bool paused = UState.Paused;
        void Click(int index)
        {
            RectF rect = overlay.DashboardMusicRect(index);
            Provider.SetMouse((int)rect.X + 12, (int)rect.Y + 12);
            Provider.LeftMouse = ButtonState.Released;
            Input.Update(new UpdateTimes(.016f, 1));
            overlay.HandleDashboardInput(Input);
            Provider.LeftMouse = ButtonState.Pressed;
            Input.Update(new UpdateTimes(.016f, 1));
            Assert.IsTrue(overlay.HandleDashboardInput(Input));
        }
        try
        {
            if (!manager.AmbientMusicPaused) Click(1);
            manager.StartMusic("AmbientMusic");
            Assert.IsTrue(manager.AmbientMusicPaused);
            Click(2);
            Assert.IsTrue(manager.AmbientMusicPaused);
            Click(0);
            Assert.IsTrue(manager.AmbientMusicPaused);
            Assert.AreEqual(speed, UState.GameSpeed);
            Assert.AreEqual(paused, UState.Paused);
            Assert.IsTrue(overlay.DashboardMusicRect(2).Right <= overlay.DashboardSpeedRect(0).X);
        }
        finally
        {
            if (manager.AmbientMusicPaused != previousPause) manager.ToggleAmbientMusic();
        }
    }

    [TestMethod]
    public void RenderCompactDashboard()
    {
        ProductionPlanet();
        var overlay = new EmpireUIOverlay(Player, Game.GraphicsDevice, Universe);
        var device = Game.GraphicsDevice;
        using var target = new RenderTarget2D(device, Universe.ScreenWidth, 100, false, SurfaceFormat.Color, DepthFormat.None);
        var previous = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(target);
            device.Clear(new Color(6, 8, 10));
            using var batch = new SpriteBatch(device);
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            overlay.Draw(batch, sidebarNavigation: true);
            batch.End();
        }
        finally { device.SetRenderTargets(previous); }
        var pixels = new Color[target.Width * target.Height];
        target.GetData(pixels);
        Assert.IsTrue(Array.Exists(pixels, c => c.R > 150 && c.G > 100));
        for (int y = EmpireUIOverlay.DashboardHeight; y < target.Height; ++y)
            Assert.AreEqual(new Color(6, 8, 10), pixels[y * target.Width + target.Width / 2], "HUD must stay within its compact row.");
        string path = Path.GetFullPath(Path.Combine(StarDriveTestContext.StarDriveAbsolutePath, "../UnitTests/TestResults/EmpireDashboard-preview.png"));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var file = File.Create(path)) target.SaveAsPng(file, target.Width, target.Height);
        TestContext.AddResultFile(path);
    }

    [TestMethod]
    public void DashboardChartsHandleEmptyAndOverfilledValues()
    {
        Assert.AreEqual(0f, EmpireUIOverlay.DashboardMeterFraction(10, 0));
        Assert.AreEqual(0f, EmpireUIOverlay.DashboardMeterFraction(-10, 100));
        Assert.AreEqual(1f, EmpireUIOverlay.DashboardMeterFraction(150, 100));
        Assert.AreEqual(.25f, EmpireUIOverlay.DashboardMeterFraction(25, 100));
        Assert.AreEqual("Shield recharge", EmpireUIOverlay.DashboardBonusEffect(ExoticBonusType.ShieldRecharge));
        var overlay = new EmpireUIOverlay(Player, Game.GraphicsDevice, Universe);
        overlay.RefreshDashboard();
        Assert.IsTrue(overlay.DashboardRows(EmpireUIOverlay.DashboardItem.Freight).Count >= 6);
        Assert.IsTrue(overlay.DashboardRows(EmpireUIOverlay.DashboardItem.Money).Any(r => r.Label == "Expenses / turn" && r.Meter));
        var exoticRows = overlay.DashboardRows(EmpireUIOverlay.DashboardItem.Resources);
        Assert.AreEqual("EMPIRE EFFECTS", exoticRows[0].Label);
        Assert.IsTrue(exoticRows.Any(r => r.Label == "Warp speed" && r.Value.EndsWith("%")));
        Assert.IsFalse(exoticRows.Any(r => r.Detail != null && r.Detail.Contains("Demand")));
        Player.Research.SetTopic("FrigateConstruction");
        overlay.RefreshDashboard();
        Assert.IsFalse(overlay.DashboardValue(EmpireUIOverlay.DashboardItem.Research).Contains("·"));
        Assert.IsTrue(overlay.DashboardRows(EmpireUIOverlay.DashboardItem.Research).Any(r => r.Meter));
    }

    [TestMethod]
    public void RenderDashboardVisualPopovers()
    {
        ProductionPlanet();
        Player.UpdateNetPlanetIncomes();
        Player.Research.SetTopic("FrigateConstruction");
        Player.Research.Current.Progress = Player.Research.Current.TechCost * .65f;
        var overlay = new EmpireUIOverlay(Player, Game.GraphicsDevice, Universe);
        overlay.RefreshDashboard();
        var device = Game.GraphicsDevice;
        using var target = new RenderTarget2D(device, 1320, 700, false, SurfaceFormat.Color, DepthFormat.None);
        var previous = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(target);
            device.Clear(new Color(6, 8, 10));
            using var batch = new SpriteBatch(device);
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            overlay.Draw(batch, sidebarNavigation: true);
            overlay.DrawDashboardCard(batch, EmpireUIOverlay.DashboardItem.Money, new RectF(10, 60, 390, 380));
            overlay.DrawDashboardCard(batch, EmpireUIOverlay.DashboardItem.Freight, new RectF(415, 60, 390, 380));
            overlay.DrawDashboardCard(batch, EmpireUIOverlay.DashboardItem.Resources, new RectF(820, 60, 480, 600));
            overlay.DrawDashboardCard(batch, EmpireUIOverlay.DashboardItem.Research, new RectF(10, 450, 390, 185));
            batch.End();
        }
        finally { device.SetRenderTargets(previous); }
        string path = Path.GetFullPath(Path.Combine(StarDriveTestContext.StarDriveAbsolutePath, "../UnitTests/TestResults/EmpireDashboard-popovers.png"));
        using (var file = File.Create(path)) target.SaveAsPng(file, target.Width, target.Height);
        TestContext.AddResultFile(path);
    }

    [TestMethod]
    public void RailArtworkAndButtonsStayAlignedAcrossStatesAndWidths()
    {
        Panel.RectF = new RectF(0, 110, 298, 550);
        Panel.Update(1);
        var device = Game.GraphicsDevice;
        using var target = new RenderTarget2D(device, 400, 700, false, SurfaceFormat.Color, DepthFormat.None);
        Color[] Render()
        {
            MoveTo(500, 220); // Keep hover highlights out of the comparison.
            var previous = device.GetRenderTargets();
            try
            {
                device.SetRenderTarget(target);
                device.Clear(new Color(6, 8, 10));
                using var batch = new SpriteBatch(device);
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                Panel.Draw(batch, new DrawTimes());
                batch.End();
            }
            finally { device.SetRenderTargets(previous); }
            var pixels = new Color[target.Width * target.Height];
            target.GetData(pixels);
            return pixels;
        }
        Color[] expanded = Render();
        MoveTo(100, 125, click: true);
        Color[] collapsed = Render();
        MoveTo(19, 125, click: true);
        Panel.RectF = new RectF(0, 110, 350, 550);
        Color[] wider = Render();
        for (int y = 60; y < 680; ++y)
        {
            for (int x = 0; x < 48; ++x)
            {
                int p = y * target.Width + x;
                Assert.AreEqual(expanded[p], wider[p], $"Resizing moved rail pixel {x}, {y}.");
                // The first button intentionally changes its selected-state colors.
                if (y < 110 || y > 142)
                    Assert.AreEqual(expanded[p], collapsed[p], $"Collapsing moved rail pixel {x}, {y}.");
            }
        }
    }

    [TestMethod]
    [DataRow(691)]
    [DataRow(1051)]
    [DataRow(1771)]
    public void SidebarFrameEndCapsDoNotStretchOnTallDisplays(int height)
    {
        var device = Game.GraphicsDevice;
        using var target = new RenderTarget2D(device, 370, height + 160, false, SurfaceFormat.Color, DepthFormat.None);
        Color[] Render(int panelHeight)
        {
            Panel.RectF = new RectF(0, 110, 298, panelHeight);
            Panel.PerformLayout();
            var previous = device.GetRenderTargets();
            try
            {
                device.SetRenderTarget(target);
                device.Clear(Color.Black);
                using var batch = new SpriteBatch(device);
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                Panel.Draw(batch, new DrawTimes());
                batch.End();
            }
            finally { device.SetRenderTargets(previous); }
            var pixels = new Color[target.Width * target.Height];
            target.GetData(pixels);
            return pixels;
        }
        Color[] reference = Render(550), taller = Render(height);
        for (int x = 0; x < 48; ++x)
        {
            for (int y = 60; y < 108; ++y)
                Assert.AreEqual(reference[y * 370 + x], taller[y * 370 + x], "Top cap stretched.");
            for (int offset = 1; offset <= 18; ++offset)
                Assert.AreEqual(reference[(684 - offset) * 370 + x], taller[(height + 134 - offset) * 370 + x], "Bottom cap stretched.");
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void RenderPopulatedPanelWithGameTextures(bool collapsed, bool automation)
    {
        Planet planet = ProductionPlanet();
        planet.Name = "Sol";
        planet.ConstructionQueue[0].ProductionSpent = 420;
        planet.Construction.Enqueue(ResourceManager.GetShipTemplate("Rocket Scout").ShipData, QueueItemType.CombatShip);
        planet.Construction.Enqueue(ResourceManager.GetShipTemplate("Colony Ship").ShipData, QueueItemType.ColonyShip);
        var fleet = Player.CreateFleet(1, "1st Defense Fleet");
        for (int i = 0; i < 7; ++i)
            fleet.AddShip(SpawnShip(i < 4 ? "Rocket Scout" : "Vulcan Scout", Player, new Vector2(1000 + i * 100)));
        var largeFleet = Player.CreateFleet(2, "2nd Strike Fleet");
        for (int i = 0; i < 24; ++i)
            largeFleet.AddShip(SpawnShip(i < 15 ? "Rocket Scout" : "Vulcan Scout", Player, new Vector2(2000 + i * 100)));
        LoadStarterShips("Shipyard");
        SpawnShip("Basic Research Station", Player, new Vector2(3000));
        SpawnShip("Basic Mining Station", Player, new Vector2(4000));
        SpawnShip("Shipyard", Player, new Vector2(5000));
        UState.Objects.UpdateLists();
        Panel.RectF = new RectF(0, 110, 298, 550);
        Panel.Update(1);
        if (automation)
        {
            Player.AutoExplore = true;
            Player.AutoFreighters = true;
            Player.AutoPickBestFreighter = true;
            Player.AutoResearch = true;
            Universe.aw.ToggleVisibility();
        }
        if (collapsed)
        {
            MoveTo(100, 125, click: true);
            Panel.Update(0.016f);
        }

        var device = Game.GraphicsDevice;
        using var target = new RenderTarget2D(device, 370, 700, false, SurfaceFormat.Color, DepthFormat.None);
        var previous = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(target);
            device.Clear(new Color(6, 8, 10));
            using var batch = new SpriteBatch(device);
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Panel.Draw(batch, new DrawTimes());
            batch.End();
        }
        finally
        {
            device.SetRenderTargets(previous);
        }
        var pixels = new Color[target.Width * target.Height];
        target.GetData(pixels);
        Assert.IsTrue(Array.Exists(pixels, c => c.R > 150 && c.G > 100), "Textured panel must render visible text and icons.");
        string results = Path.GetFullPath(Path.Combine(StarDriveTestContext.StarDriveAbsolutePath, "../UnitTests/TestResults"));
        Directory.CreateDirectory(results);
        string path = Path.Combine(results, automation ? "AutomationSidebar-preview.png"
                                                     : collapsed ? "EmpireAssetsPanel-collapsed-preview.png"
                                                     : "EmpireAssetsPanel-preview.png");
        using (var file = File.Create(path)) target.SaveAsPng(file, target.Width, target.Height);
        TestContext.AddResultFile(path);
    }
}
