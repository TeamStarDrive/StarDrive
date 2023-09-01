using System;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
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
        DropOptions<int> FreighterDropDown;
        DropOptions<int> ColonyShipDropDown;
        DropOptions<int> ScoutDropDown;
        DropOptions<int> ConstructorDropDown;
        DropOptions<int> ResearchStationDropDown;

        public AutomationWindow(UniverseScreen screen) : base(screen, toPause: null)
        {
            Screen = screen;
            const int windowWidth = 220;
            Rect = new Rectangle(ScreenWidth - 15 - windowWidth, 130, windowWidth, 575);
        }

        class CheckedDropdown : UIElementV2
        {
            UICheckBox Check;
            DropOptions<int> Options;
            public DropOptions<int> Create(Expression<Func<bool>> binding, LocalizedText title, LocalizedText tooltip)
            {
                Check = new UICheckBox(0f, 0f, binding, Fonts.Arial12Bold, title, tooltip);
                Options = new DropOptions<int>(new Vector2(0f, 25f), 190, 18);
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
                return Check.HandleInput(input) || Options.HandleInput(input);
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

            RectF win = new(Rect);
            ConstructionSubMenu = new(win, GameText.Automation);

            UIList rest = AddList(new(win.X + 10f, win.Y + 250f));
            rest.Padding = new(2f, 10f);
            rest.AddCheckbox(() => UState.Player.AutoPickConstructors,  title: GameText.AutoPickConstructorsName, tooltip: GameText.AutoPickConstructorsTip);
            rest.AddCheckbox(() => UState.Player.AutoPickBestColonizer, title: GameText.AutoPickColonyShip, tooltip: GameText.TheBestColonyShipWill);
            rest.AddCheckbox(() => UState.Player.AutoPickBestFreighter, title: GameText.AutoPickFreighter, tooltip: GameText.IfAutoTradeIsChecked);
            rest.AddCheckbox(() => UState.Player.AutoResearch,          title: GameText.AutoResearch, tooltip: GameText.YourEmpireWillAutomaticallySelect);
            rest.AddCheckbox(() => UState.Player.AutoBuildTerraformers, title: GameText.AutoBuildTerraformers, tooltip: GameText.AutoBuildTerraformersTip);
            rest.AddCheckbox(() => UState.Player.AutoTaxes,             title: GameText.AutoTaxes, tooltip: GameText.YourEmpireWillAutomaticallyManage3);

            if (Screen.Player.CanBuildResearchStations)
                rest.AddCheckbox(() => UState.Player.AutoPickBestResearchStation, title: GameText.AutoPickResearchStation, tooltip: GameText.AutoPickResearchStationTip);

            rest.AddCheckbox(() => RushConstruction,                      title: GameText.RushAllConstruction, tooltip: GameText.RushAllConstructionTip);
            rest.AddCheckbox(() => UState.P.AllowPlayerInterTrade,        title: GameText.AllowPlayerInterTradeTitle, tooltip: GameText.AllowPlayerInterTradeTip);
            rest.AddCheckbox(() => UState.P.SuppressOnBuildNotifications, title: GameText.DisableBuildingAlerts, tooltip: GameText.NormallyWhenYouManuallyAdd);
            rest.AddCheckbox(() => UState.P.DisableInhibitionWarning,     title: GameText.DisableInhibitionAlerts, tooltip: GameText.InhibitionAlertsAreDisplayedWhen);
            rest.AddCheckbox(() => UState.P.DisableVolcanoWarning,        title: GameText.DisableVolcanoAlerts, tooltip: GameText.DisableVolcanoActivationOrDeactivation);
            rest.AddCheckbox(() => UState.P.EnableStarvationWarning,      title: GameText.EnableStarvationWarning, tooltip: GameText.EnableStarvationWarningTip);

            UIList ticks = AddList(new Vector2(win.X + 10f, win.Y + 26f));
            ticks.Padding = new Vector2(2f, 10f);

            ScoutDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => Screen.Player.AutoExplore, title:GameText.Autoexplore, tooltip:GameText.YourEmpireWillAutomaticallyManage);

            ColonyShipDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => Screen.Player.AutoColonize, title:GameText.Autocolonize, tooltip:GameText.YourEmpireWillAutomaticallyCreate);

            ConstructorDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => Screen.Player.AutoBuildSpaceRoads, Localizer.Token(GameText.Autobuild) + " Projectors", GameText.YourEmpireWillAutomaticallyCreate2);

            FreighterDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => Screen.Player.AutoFreighters, title: GameText.AutomaticTrade, tooltip: GameText.YourEmpireWillAutomaticallyManage2);

            ResearchStationDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => Screen.Player.AutoBuildResearchStations, title: GameText.AutoBuildResearchStation, tooltip: GameText.AutoBuildResearchStationTip);
            

            // draw ordering is still imperfect, this is a hack
            ticks.ReverseZOrder();
            UpdateDropDowns();
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
            ResearchStationDropDown.Visible = !Screen.Player.AutoPickBestResearchStation
                && Screen.Player.CanBuildResearchStations;


            base.Draw(batch, elapsed);
        }

        public override bool HandleInput(InputState input)
        {
            if (!IsOpen)
                return false;

            if (input.RightMouseClick || input.Escaped)
            {
                IsOpen = false;
                return false;
            }

            if (base.HandleInput(input))
            {
                EmpireData playerData = Screen.Player.data;
                playerData.CurrentAutoFreighter   = FreighterDropDown.ActiveName;
                playerData.CurrentAutoColony      = ColonyShipDropDown.ActiveName;
                playerData.CurrentConstructor     = ConstructorDropDown.ActiveName;
                playerData.CurrentAutoScout       = ScoutDropDown.ActiveName;
                playerData.CurrentResearchStation = ResearchStationDropDown.ActiveName;
                return true;
            }
            return false;
        }

        void WarnBuildableShips()
        {
            var sb = new StringBuilder("Player.ShipsWeCanBuild = {\n");

            foreach (IShipDesign ship in Screen.Player.ShipsWeCanBuild)
                sb.Append("  '").Append(ship.Name).Append("',\n");
            sb.Append("}");

            Log.Warning(sb.ToString());
        }

        void InitDropOptions(DropOptions<int> options, ref string automationShip, string defaultShip, Func<IShipDesign, bool> predicate)
        {
            if (options == null)
                return;
            options.Clear();


            foreach (IShipDesign ship in Screen.Player.ShipsWeCanBuild)
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

        public void UpdateDropDowns()
        {
            EmpireData playerData = Screen.Player.data;

            InitDropOptions(ResearchStationDropDown, ref playerData.CurrentResearchStation, playerData.DefaultResearchStation,
                ship => ship.IsShipGoodToBuild(Screen.Player) && ship.IsResearchStation);

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
