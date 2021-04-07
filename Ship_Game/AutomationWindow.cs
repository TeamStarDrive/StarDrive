using System;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Ships;

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

        public AutomationWindow(UniverseScreen universe) : base(universe, pause:false)
        {
            Universe = universe;
            const int windowWidth = 210;
            Rect = new Rectangle(ScreenWidth - 15 - windowWidth, 130, windowWidth, 420);
        }

        class CheckedDropdown : UIElementV2
        {
            UICheckBox Check;
            DropOptions<int> Options;
            public DropOptions<int> Create(Expression<Func<bool>> binding, int title, int tooltip)
                => Create(binding, Localizer.Token(title), tooltip);

            public DropOptions<int> Create(Expression<Func<bool>> binding, string title, int tooltip)
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
            rest.AddCheckbox(() => EmpireManager.Player.AutoPickBestColonizer, title: 1837, tooltip: 1838);
            rest.AddCheckbox(() => EmpireManager.Player.AutoPickBestFreighter, title: 1958, tooltip: 1959);
            rest.AddCheckbox(() => EmpireManager.Player.AutoResearch,          title: 6136, tooltip: 7039);
            rest.AddCheckbox(() => EmpireManager.Player.data.AutoTaxes,        title: 6138, tooltip: 7040);
            rest.AddCheckbox(() => RushConstruction,                           title: 1824, tooltip: 1825);
            rest.AddCheckbox(() => GlobalStats.SuppressOnBuildNotifications,   title: 1835, tooltip: 1836);
            rest.AddCheckbox(() => GlobalStats.DisableInhibitionWarning,       title: 1842, tooltip: 1843);
            rest.AddCheckbox(() => GlobalStats.DisableVolcanoWarning,          title: 4254, tooltip: 4255);

            UIList ticks = AddList(new Vector2(win.X + 10f, win.Y + 26f));
            ticks.Padding = new Vector2(2f, 10f);

            ScoutDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => EmpireManager.Player.AutoExplore, title:305, tooltip:2226);

            ColonyShipDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => EmpireManager.Player.AutoColonize, title:306, tooltip:2227);

            ConstructorDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => EmpireManager.Player.AutoBuild, Localizer.Token(GameText.Autobuild) + " Projectors", 2228);

            FreighterDropDown = ticks.Add(new CheckedDropdown())
                .Create(() => EmpireManager.Player.AutoFreighters, title: 308, tooltip: 2229);

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
            EmpireData playerData = Universe.player.data;

            InitDropOptions(FreighterDropDown, ref playerData.CurrentAutoFreighter, playerData.DefaultSmallTransport, 
                ship => ship.ShipGoodToBuild(EmpireManager.Player) && ship.IsFreighter);

            InitDropOptions(ColonyShipDropDown, ref playerData.CurrentAutoColony, playerData.DefaultColonyShip, 
                ship => ship.ShipGoodToBuild(EmpireManager.Player) && ship.isColonyShip);

            InitDropOptions(ConstructorDropDown, ref playerData.CurrentConstructor, playerData.DefaultConstructor,
                ship => ship.ShipGoodToBuild(EmpireManager.Player) && ship.DesignRole == ShipData.RoleName.construction);

            InitDropOptions(ScoutDropDown, ref playerData.CurrentAutoScout, playerData.StartingScout, 
                ship =>
                {
                    if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.reconDropDown)
                        return ship.ShipGoodToBuild(EmpireManager.Player) && 
                              (ship.DesignRole == ShipData.RoleName.scout || 
                               ship.shipData?.ShipCategory == ShipData.Category.Recon);

                    return ship.ShipGoodToBuild(EmpireManager.Player) && 
                          (ship.DesignRole == ShipData.RoleName.scout ||
                           ship.DesignRole == ShipData.RoleName.fighter ||
                           ship.shipData?.ShipCategory == ShipData.Category.Recon);
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
