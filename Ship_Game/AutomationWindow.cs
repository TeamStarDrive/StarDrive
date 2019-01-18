using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class AutomationWindow : GameScreen
    {
        public bool IsOpen { get; private set; }
        private readonly Submenu ConstructionSubMenu;
        private readonly UniverseScreen Universe;
        private readonly DropOptions<int> FreighterDropDown;
        private readonly DropOptions<int> ColonyShipDropDown;
        private readonly DropOptions<int> ScoutDropDown;
        private readonly DropOptions<int> ConstructorDropDown;

        public AutomationWindow(UniverseScreen universe) : base(universe, pause:false)
        {
            Universe = universe;
            const int windowWidth = 210;
            Rect = new Rectangle(ScreenWidth - 115 - windowWidth, 490, windowWidth, 300);
            Rectangle win = Rect;
            ConstructionSubMenu = new Submenu(win);
            ConstructionSubMenu.AddTab(Localizer.Token(304));

            BeginVLayout(win.X + 12, win.Y + 25, ystep: 45);
                Checkbox(() => EmpireManager.Player.AutoExplore,    title:305, tooltip:2226);
                Checkbox(() => EmpireManager.Player.AutoColonize,   title:306, tooltip:2227);
                Checkbox(() => EmpireManager.Player.AutoFreighters, title:308, tooltip:2229);
                Checkbox(() => EmpireManager.Player.AutoBuild, Localizer.Token(307) + " Projectors", 2228);
            EndLayout();

            BeginVLayout(win.X + 12, win.Y + 220, ystep: Fonts.Arial12Bold.LineSpacing + 3);
                Checkbox(() => GlobalStats.AutoCombat,              title:2207, tooltip:2230);
                Checkbox(() => EmpireManager.Player.AutoResearch,   title:6136, tooltip:7039);
                Checkbox(() => EmpireManager.Player.data.AutoTaxes, title:6138, tooltip:7040);
            EndLayout();

            BeginVLayout(win.X + 12, win.Y + 48, ystep: 45);
                ScoutDropDown       = DropOptions<int>(190, 18, zorder:4);
                ColonyShipDropDown  = DropOptions<int>(190, 18, zorder:3);
                FreighterDropDown   = DropOptions<int>(190, 18, zorder:2);
                ConstructorDropDown = DropOptions<int>(190, 18, zorder:1);
            EndLayout();

            UpdateDropDowns();
        }

        public void ToggleVisibility()
        {
            GameAudio.ButtonClick();
            IsOpen = !IsOpen;
        }

        public override void Draw(SpriteBatch batch)
        {
            if (!Visible)
                return;

            Rectangle r = ConstructionSubMenu.Menu;
            r.Y = r.Y + 25;
            r.Height = r.Height - 25;
            var sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(batch);
            ConstructionSubMenu.Draw(batch);

            base.Draw(batch);
        }

        public override bool HandleInput(InputState input)
        {
            if (input.RightMouseClick)
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

        private static void WarnBuildableShips()
        {
            var sb = new StringBuilder("Player.ShipsWeCanBuild = {\n");

            foreach (string ship in EmpireManager.Player.ShipsWeCanBuild)
                sb.Append("  '").Append(ship).Append("',\n");
            sb.Append("}");

            Log.Warning(sb.ToString());
        }

        private static void InitDropOptions(DropOptions<int> options, ref string automationShip, string defaultShip, Func<Ship, bool> predicate)
        {
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
                ship =>
                {
                    return ship.ShipGoodToBuild(EmpireManager.Player) && !ship.isColonyShip && ship.CargoSpaceMax > 0f;
                });

            InitDropOptions(ColonyShipDropDown, ref playerData.CurrentAutoColony, playerData.DefaultColonyShip, 
                ship =>
                {
                    return ship.ShipGoodToBuild(EmpireManager.Player) && ship.isColonyShip;
                });

            InitDropOptions(ConstructorDropDown, ref playerData.CurrentConstructor, playerData.DefaultConstructor, 
                ship =>
                {
                    if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.ConstructionModule)
                        return ship.ShipGoodToBuild(EmpireManager.Player) && (ship.isConstructor || ship.Name == playerData.DefaultConstructor);

                    return ship.ShipGoodToBuild(EmpireManager.Player) && !ship.isColonyShip && ship.CargoSpaceMax > 0f && 
                            (ship.isConstructor || ship.shipData.Role == ShipData.RoleName.freighter);
                });

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
    }
}