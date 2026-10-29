using System;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Universe;

namespace Ship_Game
{
    public sealed class AutomationWindow : GameScreen
    {
        public bool IsOpen { get; private set; }
        readonly UniverseScreen Screen;
        UniverseState UState => Screen.UState;
        Submenu ConstructionSubMenu;
        UIList Ticks;
        UIList Rest;
        UIList Alerts;
        int SelectedTab;
        DropOptions<int> FreighterDropDown;
        DropOptions<int> ColonyShipDropDown;
        DropOptions<int> ScoutDropDown;
        DropOptions<int> ConstructorDropDown;
        DropOptions<int> ResearchStationDropDown;
        DropOptions<int> MiningStationDropDown;
        DropOptions<int> SalvageShipDropDown;
        bool ResearchStationsEnabled;
        bool MiningOpsEnabled;
        bool SalvageOpsEnabled;

        public AutomationWindow(UniverseScreen screen) : base(screen, toPause: null)
        {
            Screen = screen;
            const int windowWidth = 280;
            Rect = new Rectangle(ScreenWidth - 15 - windowWidth, 119, windowWidth, 710);
            CanEscapeFromScreen = false;
        }

        class CheckedDropdown : UIElementV2
        {
            UICheckBox Check;
            DropOptions<int> Options;
            public DropOptions<int> Create(Expression<Func<bool>> binding, LocalizedText title, LocalizedText tooltip)
            {
                Check = new UICheckBox(-200f, -200f, binding, Fonts.Arial12Bold, title, tooltip);
                Options = new DropOptions<int>(new Vector2(-200f, -200f), 190, 18);
                return Options;
            }
            public override void PerformLayout()
            {
                Check.Pos = Pos;
                Check.PerformLayout();
                Options.Pos = new Vector2(Pos.X, Pos.Y + 16f);
                Options.PerformLayout();
                Height = Options.Bottom - Pos.Y;
            }
            public override bool HandleInput(InputState input)
            {
                return Check.HandleInput(input) || Options.Visible && Options.HandleInput(input); // a hidden dropdown takes no clicks
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                Check.Draw(batch, elapsed);
                Options.Draw(batch, elapsed);
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RemoveAll();

            ResearchStationsEnabled = !Screen.Player.Universe.P.DisableResearchStations;
            MiningOpsEnabled = !Screen.Player.Universe.P.DisableMiningOps;
            SalvageOpsEnabled = Screen.Player.Universe.P.SalvageOps;

            RectF win = new(Rect);
            ConstructionSubMenu = new(win, new LocalizedText[] { GameText.Automation, GameText.Alerts });
            float top = ConstructionSubMenu.ClientArea.Y + 1f;

            // the salvage ship's row adds to the table above
            UIList rest = Rest = AddList(new(win.X + 10f, top + (SalvageOpsEnabled ? 308f : 264f)));
            rest.Padding = new(2f, 10f);
            rest.AddCheckbox(() => UState.Player.AutoPickConstructors,  title: GameText.AutoPickConstructorsName, tooltip: GameText.AutoPickConstructorsTip);
            rest.AddCheckbox(() => UState.Player.AutoPickBestColonizer, title: GameText.AutoPickColonyShip, tooltip: GameText.TheBestColonyShipWill);
            rest.AddCheckbox(() => UState.Player.AutoPickBestFreighter, title: GameText.AutoPickFreighter, tooltip: GameText.IfAutoTradeIsChecked);
            rest.AddCheckbox(() => UState.Player.AutoResearch,          title: GameText.AutoResearch, tooltip: GameText.YourEmpireWillAutomaticallySelect);
            rest.AddCheckbox(() => UState.Player.AutoBuildTerraformers, title: GameText.AutoBuildTerraformers, tooltip: GameText.AutoBuildTerraformersTip);
            rest.AddCheckbox(() => UState.Player.AutoTaxes,             title: GameText.AutoTaxes, tooltip: GameText.YourEmpireWillAutomaticallyManage3);

            if (ResearchStationsEnabled && Screen.Player.CanBuildResearchStations)
                rest.AddCheckbox(() => UState.Player.AutoPickBestResearchStation, title: GameText.AutoPickResearchStation, tooltip: GameText.AutoPickResearchStationTip);

            if (MiningOpsEnabled && Screen.Player.CanBuildMiningStations)
                rest.AddCheckbox(() => UState.Player.AutoPickBestMiningStation, title: GameText.AutoPickMiningStation, tooltip: GameText.AutoPickMiningStationTip);

            if (SalvageOpsEnabled && Screen.Player.CanBuildSalvageShips)
                rest.AddCheckbox(() => UState.Player.AutoPickBestSalvageShip, title: GameText.AutoPickSalvageShip, tooltip: GameText.AutoPickSalvageShipTip);

            rest.AddCheckbox(() => RushConstruction,                      title: GameText.RushAllConstruction, tooltip: GameText.RushAllConstructionTip);
            rest.AddCheckbox(() => UState.P.AllowPlayerInterTrade,        title: GameText.AllowPlayerInterTradeTitle, tooltip: GameText.AllowPlayerInterTradeTip);
            rest.AddCheckbox(() => UState.P.PrioitizeProjectors,          title: GameText.PrioritizeProjector, tooltip: GameText.PrioritizeProjectorTip);

            UIList alerts = Alerts = AddList(new(win.X + 10f, top));
            alerts.Padding = new(2f, 10f);
            alerts.AddCheckbox(() => !UState.P.SuppressOnBuildNotifications, v => UState.P.SuppressOnBuildNotifications = !v,
                               title: GameText.BuildingAlerts, tooltip: GameText.NormallyWhenYouManuallyAdd);
            alerts.AddCheckbox(() => !UState.P.DisableInhibitionWarning, v => UState.P.DisableInhibitionWarning = !v,
                               title: GameText.InhibitionAlerts, tooltip: GameText.InhibitionAlertsAreDisplayedWhen);
            alerts.AddCheckbox(() => !UState.P.DisableVolcanoWarning, v => UState.P.DisableVolcanoWarning = !v,
                               title: GameText.VolcanoAlerts, tooltip: GameText.VolcanoAlertsTip);
            alerts.AddCheckbox(() => !UState.P.DisableCrashSiteWarning, v => UState.P.DisableCrashSiteWarning = !v,
                               title: GameText.ShipCrashAlerts, tooltip: GameText.ShipCrashAlertsTip);
            alerts.AddCheckbox(() => !UState.P.DisableStationBuiltWarning, v => UState.P.DisableStationBuiltWarning = !v,
                               title: GameText.StationBuiltAlerts, tooltip: GameText.StationBuiltAlertsTip);
            if (SalvageOpsEnabled)
            {
                alerts.AddCheckbox(() => !UState.P.DisableDerelictWarning, v => UState.P.DisableDerelictWarning = !v,
                                   title: GameText.DerelictAlerts, tooltip: GameText.DerelictAlertsTip);
                alerts.AddCheckbox(() => !UState.P.DisableSalvageWarning, v => UState.P.DisableSalvageWarning = !v,
                                   title: GameText.SalvageAlerts, tooltip: GameText.SalvageAlertsTip);
            }
            alerts.AddCheckbox(() => UState.P.EnableStarvationWarning, title: GameText.StarvationAlerts, tooltip: GameText.EnableStarvationWarningTip);

            UIList ticks = Ticks = AddList(new Vector2(win.X + 10f, top));
            ticks.Padding = new Vector2(2f, 10f);

            ScoutDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => Screen.Player.AutoExplore, title:GameText.Autoexplore, tooltip:GameText.YourEmpireWillAutomaticallyManage);

            ColonyShipDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => Screen.Player.AutoColonize, title:GameText.Autocolonize, tooltip:GameText.YourEmpireWillAutomaticallyCreate);

            ConstructorDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => Screen.Player.AutoBuildSpaceRoads, Localizer.Token(GameText.Autobuild) + " Projectors", GameText.YourEmpireWillAutomaticallyCreate2);

            FreighterDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => Screen.Player.AutoFreighters, title: GameText.AutomaticTrade, tooltip: GameText.YourEmpireWillAutomaticallyManage2);

            if (ResearchStationsEnabled)
                ResearchStationDropDown = ticks.Add(new CheckedDropdown())
                    .Create(() => Screen.Player.AutoBuildResearchStations, title: GameText.AutoBuildResearchStation, tooltip: GameText.AutoBuildResearchStationTip);

            if (MiningOpsEnabled)
                MiningStationDropDown = ticks.Add(new CheckedDropdown())
                    .Create(() => Screen.Player.AutoBuildMiningStations, title: GameText.AutoBuildMiningStation, tooltip: GameText.AutoBuildMiningStationTip);

            if (SalvageOpsEnabled)
                SalvageShipDropDown = ticks.Add(new CheckedDropdown())
                    .Create(() => Screen.Player.AutoSalvage, title: GameText.AutoSalvage, tooltip: GameText.AutoSalvageTip);

            // draw ordering is still imperfect, this is a hack
            ticks.ReverseZOrder();
            UpdateDropDowns();

            PerformLayout();
            Bottom = Math.Max(Rest.Bottom, Alerts.Bottom) + 20f;
            ConstructionSubMenu.Bottom = Bottom;
            ConstructionSubMenu.PerformLayout();

            ConstructionSubMenu.OnTabChange = OnTabChange;
            ConstructionSubMenu.SelectedIndex = SelectedTab;
        }

        void OnTabChange(int tab)
        {
            SelectedTab = tab;
            Ticks.Visible = Rest.Visible = tab == 0;
            Alerts.Visible = tab == 1;
            foreach (DropOptions<int> dropDown in new[] { ScoutDropDown, ColonyShipDropDown, ConstructorDropDown,
                                                          FreighterDropDown, ResearchStationDropDown, MiningStationDropDown,
                                                          SalvageShipDropDown })
                if (dropDown != null) { dropDown.Open = false; dropDown.Reset(); }
        }

        public void ToggleVisibility()
        {
            GameAudio.AcceptClick();
            IsOpen = !IsOpen;
            if (IsOpen)
                LoadContent();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            Rectangle r = ConstructionSubMenu.Rect;
            r.Y += 25;
            r.Height -= 25;
            var sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(batch, elapsed);
            ConstructionSubMenu.Draw(batch, elapsed);

            ConstructorDropDown.Visible = !Screen.Player.AutoPickConstructors;
            FreighterDropDown.Visible  = !Screen.Player.AutoPickBestFreighter;
            ColonyShipDropDown.Visible = !Screen.Player.AutoPickBestColonizer;

            // Re-populate the Research/Mining dropdowns the first time CanBuild* flips to
            // true. LoadContent only ran InitDropOptions once at window-open time, so if
            // the player completes the gating tech while AutomationWindow is still open
            // the dropdown becomes Visible (below) but stays empty — clicking it then
            // crashed on Options[0] access via ActiveName. Lazy-init on demand here.
            if (ResearchStationsEnabled)
            {
                bool canBuild = Screen.Player.CanBuildResearchStations;
                if (canBuild && ResearchStationDropDown.Count == 0)
                {
                    EmpireData pd = Screen.Player.data;
                    InitDropOptions(ResearchStationDropDown, ref pd.CurrentResearchStation, pd.DefaultResearchStation,
                        ship => ship.IsShipGoodToBuild(Screen.Player) && ship.IsResearchStation);
                }
                ResearchStationDropDown.Visible = !Screen.Player.AutoPickBestResearchStation && canBuild;
            }
            if (MiningOpsEnabled)
            {
                bool canBuild = Screen.Player.CanBuildMiningStations;
                if (canBuild && MiningStationDropDown.Count == 0)
                {
                    EmpireData pd = Screen.Player.data;
                    InitDropOptions(MiningStationDropDown, ref pd.CurrentMiningStation, pd.DefaultMiningStation,
                        ship => ship.IsShipGoodToBuild(Screen.Player) && ship.IsMiningStation);
                }
                MiningStationDropDown.Visible = !Screen.Player.AutoPickBestMiningStation && canBuild;
            }
            if (SalvageOpsEnabled)
            {
                bool canBuild = Screen.Player.CanBuildSalvageShips;
                if (canBuild && SalvageShipDropDown.Count == 0)
                    InitSalvageShipDropDown();
                SalvageShipDropDown.Visible = !Screen.Player.AutoPickBestSalvageShip && canBuild;
            }
            base.Draw(batch, elapsed);
        }

        public override bool HandleInput(InputState input)
        {
            if (!IsOpen)
                return false;

            if (ConstructionSubMenu.HandleInput(input))
                return true;

            if (base.HandleInput(input))
            {
                EmpireData playerData = Screen.Player.data;
                playerData.CurrentAutoFreighter   = FreighterDropDown.ActiveName;
                playerData.CurrentAutoColony      = ColonyShipDropDown.ActiveName;
                playerData.CurrentConstructor     = ConstructorDropDown.ActiveName;
                playerData.CurrentAutoScout       = ScoutDropDown.ActiveName;

                if (ResearchStationsEnabled && Screen.Player.CanBuildResearchStations)
                    playerData.CurrentResearchStation = ResearchStationDropDown.ActiveName;
                if (MiningOpsEnabled && Screen.Player.CanBuildMiningStations)
                    playerData.CurrentMiningStation   = MiningStationDropDown.ActiveName;
                if (SalvageOpsEnabled && SalvageShipDropDown.NotEmpty)
                    playerData.CurrentSalvageShip     = SalvageShipDropDown.ActiveName;

                return true;
            }
            return false;
        }

        void WarnBuildableShips()
        {
            var sb = new StringBuilder("Player.ShipsWeCanBuild = {\n");

            foreach (IShipDesign ship in Screen.Player.ShipsWeCanBuildSnapshot)
                sb.Append("  '").Append(ship.Name).Append("',\n");
            sb.Append("}");

            Log.Warning(sb.ToString());
        }

        void InitDropOptions(DropOptions<int> options, ref string automationShip, string defaultShip, Func<IShipDesign, bool> predicate)
        {
            if (options == null)
                return;
            options.Clear();


            foreach (IShipDesign ship in Screen.Player.ShipsWeCanBuildSnapshot)
            {
                if (predicate(ship))
                    options.AddOption(ship.Name, 0);
            }

            if (!options.SetActiveEntry(automationShip)) // try set the current automationShip active
            {
                if (!options.SetActiveEntry(defaultShip)) // we can't build a default ship??? wtf
                {
                    Log.Warning($"Failed to enable default automation ship '{defaultShip}' for player {Screen.Player}");
                    WarnBuildableShips();
                    options.AddOption(defaultShip, 0);
                }

                // always set to default ship
                automationShip = defaultShip;
            }
        }

        void InitSalvageShipDropDown()
        {
            InitDropOptions(SalvageShipDropDown, ref Screen.Player.data.CurrentSalvageShip, Screen.Player.data.DefaultSalvageShip,
                ship => ship.IsShipGoodToBuild(Screen.Player) && ShipBuilder.IsUsableSalvageShip(ship));
        }

        public void UpdateDropDowns()
        {
            EmpireData playerData = Screen.Player.data;
            if (SalvageOpsEnabled && Screen.Player.CanBuildSalvageShips)
                InitSalvageShipDropDown();

            if (MiningOpsEnabled && Screen.Player.CanBuildMiningStations)
            {
                InitDropOptions(MiningStationDropDown, ref playerData.CurrentMiningStation, playerData.DefaultMiningStation,
                    ship => ship.IsShipGoodToBuild(Screen.Player) && ship.IsMiningStation);
            }

            if (ResearchStationsEnabled && Screen.Player.CanBuildResearchStations)
            {
                InitDropOptions(ResearchStationDropDown, ref playerData.CurrentResearchStation, playerData.DefaultResearchStation,
                    ship => ship.IsShipGoodToBuild(Screen.Player) && ship.IsResearchStation);
            }

            InitDropOptions(FreighterDropDown, ref playerData.CurrentAutoFreighter, playerData.DefaultSmallTransport, 
                ship => ship.IsShipGoodToBuild(Screen.Player) && ship.IsFreighter);

            InitDropOptions(ColonyShipDropDown, ref playerData.CurrentAutoColony, playerData.DefaultColonyShip, 
                ship => ship.IsShipGoodToBuild(Screen.Player) && ship.IsColonyShip);

            InitDropOptions(ConstructorDropDown, ref playerData.CurrentConstructor, playerData.DefaultConstructor,
                ship => ship.IsShipGoodToBuild(Screen.Player) && ship.IsConstructor);

            InitDropOptions(ScoutDropDown, ref playerData.CurrentAutoScout, playerData.StartingScout, 
                ship =>
                {
                    if (GlobalStats.Defaults.ReconDropDown)
                        return ship.IsShipGoodToBuild(Screen.Player) && 
                              (ship.Role == RoleName.scout || 
                               ship.ShipCategory == ShipCategory.Recon);

                    return ship.IsShipGoodToBuild(Screen.Player) && 
                          (ship.Role == RoleName.scout ||
                           ship.Role == RoleName.fighter ||
                           ship.ShipCategory == ShipCategory.Recon);
                });
        }

        bool RushConstruction
        {
            get => Screen.Player.RushAllConstruction;
            set // used in the rush construction checkbox at start
            {
                Screen.Player.RushAllConstruction = value;
                Screen.RunOnSimThread(() => Screen.Player.SwitchRushAllConstruction(value));
            }
        }
    }
}
