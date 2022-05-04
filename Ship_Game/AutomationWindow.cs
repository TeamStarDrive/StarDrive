using System;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public sealed class AutomationWindow : GameScreen
    {
        public bool IsOpen { get; private set; }
        readonly UniverseScreen Universe;
        Submenu ConstructionSubMenu;
        DropOptions<int> FreighterDropDown;
        DropOptions<int> ColonyShipDropDown;
        DropOptions<int> ScoutDropDown;
        DropOptions<int> ConstructorDropDown;

        public AutomationWindow(UniverseScreen universe) : base(universe, toPause: null)
        {
            Universe = universe;
            const int windowWidth = 210;
            Rect = new Rectangle(ScreenWidth - 15 - windowWidth, 130, windowWidth, 420);
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

            Rectangle win = Rect;
            ConstructionSubMenu = new Submenu(win);
            ConstructionSubMenu.AddTab(Localizer.Token(GameText.Automation));

            UIList rest = AddList(new Vector2(win.X + 10f, win.Y + 200f));
            rest.Padding = new Vector2(2f, 10f);
            rest.AddCheckbox(() => EmpireManager.Player.AutoPickBestColonizer, title: GameText.AutoPickColonyShip, tooltip: GameText.TheBestColonyShipWill);
            rest.AddCheckbox(() => EmpireManager.Player.AutoPickBestFreighter, title: GameText.AutoPickFreighter, tooltip: GameText.IfAutoTradeIsChecked);
            rest.AddCheckbox(() => EmpireManager.Player.AutoResearch,          title: GameText.AutoResearch, tooltip: GameText.YourEmpireWillAutomaticallySelect);
            rest.AddCheckbox(() => EmpireManager.Player.data.AutoTaxes,        title: GameText.AutoTaxes, tooltip: GameText.YourEmpireWillAutomaticallyManage3);
            rest.AddCheckbox(() => RushConstruction,                           title: GameText.RushAllConstruction, tooltip: GameText.RushAllConstructionTip);
            rest.AddCheckbox(() => GlobalStats.SuppressOnBuildNotifications,   title: GameText.DisableBuildingAlerts, tooltip: GameText.NormallyWhenYouManuallyAdd);
            rest.AddCheckbox(() => GlobalStats.DisableInhibitionWarning,       title: GameText.DisableInhibitionAlerts, tooltip: GameText.InhibitionAlertsAreDisplayedWhen);
            rest.AddCheckbox(() => GlobalStats.DisableVolcanoWarning,          title: GameText.DisableVolcanoAlerts, tooltip: GameText.DisableVolcanoActivationOrDeactivation);

            UIList ticks = AddList(new Vector2(win.X + 10f, win.Y + 26f));
            ticks.Padding = new Vector2(2f, 10f);

            ScoutDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => EmpireManager.Player.AutoExplore, title:GameText.Autoexplore, tooltip:GameText.YourEmpireWillAutomaticallyManage);

            ColonyShipDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => EmpireManager.Player.AutoColonize, title:GameText.Autocolonize, tooltip:GameText.YourEmpireWillAutomaticallyCreate);

            ConstructorDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => EmpireManager.Player.AutoBuild, Localizer.Token(GameText.Autobuild) + " Projectors", GameText.YourEmpireWillAutomaticallyCreate2);

            FreighterDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => EmpireManager.Player.AutoFreighters, title: GameText.AutomaticTrade, tooltip: GameText.YourEmpireWillAutomaticallyManage2);

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

            FreighterDropDown.Visible  = !EmpireManager.Player.AutoPickBestFreighter;
            ColonyShipDropDown.Visible = !EmpireManager.Player.AutoPickBestColonizer;

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
                EmpireData playerData = EmpireManager.Player.data;
                playerData.CurrentAutoFreighter = FreighterDropDown.ActiveName;
                playerData.CurrentAutoColony    = ColonyShipDropDown.ActiveName;
                playerData.CurrentConstructor   = ConstructorDropDown.ActiveName;
                playerData.CurrentAutoScout     = ScoutDropDown.ActiveName;
                return true;
            }
            return false;
        }

        static void WarnBuildableShips()
        {
            var sb = new StringBuilder("Player.ShipsWeCanBuild = {\n");

            foreach (string ship in EmpireManager.Player.ShipsWeCanBuild)
                sb.Append("  '").Append(ship).Append("',\n");
            sb.Append("}");

            Log.Warning(sb.ToString());
        }

        static void InitDropOptions(DropOptions<int> options, ref string automationShip, string defaultShip, Func<Ship, bool> predicate)
        {
            if (options == null)
                return;
            options.Clear();


            foreach (string ship in EmpireManager.Player.ShipsWeCanBuild)
            {
                if (ResourceManager.GetShipTemplate(ship, out Ship template) && predicate(template))
                    options.AddOption(template.Name, 0);
            }

            if (!options.SetActiveEntry(automationShip)) // try set the current automationShip active
            {
                if (!options.SetActiveEntry(defaultShip)) // we can't build a default ship??? wtf
                {
                    Log.Warning($"Failed to enable default automation ship '{defaultShip}' for player {EmpireManager.Player}");
                    WarnBuildableShips();
                    options.AddOption(defaultShip, 0);
                }

                // always set to default ship
                automationShip = defaultShip;
            }
        }

        public void UpdateDropDowns()
        {
            EmpireData playerData = Universe.Player.data;

            InitDropOptions(FreighterDropDown, ref playerData.CurrentAutoFreighter, playerData.DefaultSmallTransport, 
                ship => ship.ShipGoodToBuild(EmpireManager.Player) && ship.IsFreighter);

            InitDropOptions(ColonyShipDropDown, ref playerData.CurrentAutoColony, playerData.DefaultColonyShip, 
                ship => ship.ShipGoodToBuild(EmpireManager.Player) && ship.ShipData.IsColonyShip);

            InitDropOptions(ConstructorDropDown, ref playerData.CurrentConstructor, playerData.DefaultConstructor,
                ship => ship.ShipGoodToBuild(EmpireManager.Player) && ship.IsConstructor);

            InitDropOptions(ScoutDropDown, ref playerData.CurrentAutoScout, playerData.StartingScout, 
                ship =>
                {
                    if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.reconDropDown)
                        return ship.ShipGoodToBuild(EmpireManager.Player) && 
                              (ship.DesignRole == RoleName.scout || 
                               ship.ShipData?.ShipCategory == ShipCategory.Recon);

                    return ship.ShipGoodToBuild(EmpireManager.Player) && 
                          (ship.DesignRole == RoleName.scout ||
                           ship.DesignRole == RoleName.fighter ||
                           ship.ShipData?.ShipCategory == ShipCategory.Recon);
                });
        }

        bool RushConstruction
        {
            get => EmpireManager.Player.RushAllConstruction;
            set // used in the rush construction checkbox at start
            {
                EmpireManager.Player.RushAllConstruction = value;
                RunOnEmpireThread(() => EmpireManager.Player.SwitchRushAllConstruction(value));
            }
        }
    }
}
