using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed class AutomationWindow : GameScreen
    {
        public bool IsOpen { get; private set; }
        private readonly Submenu ConstructionSubMenu;
        private readonly UniverseScreen Universe;
        private readonly DropOptions<int> AutoFreighterDropDown;
        private readonly DropOptions<int> ColonyShipDropDown;
        private readonly DropOptions<int> ScoutDropDown;
        private readonly DropOptions<int> ConstructorDropDown;

        public AutomationWindow(UniverseScreen universe) : base(universe)
        {
            Universe = universe;
            const int windowWidth = 210;
            Rect = new Rectangle(ScreenWidth - 115 - windowWidth, 490, windowWidth, 300);
            Rectangle win = Rect;
            ConstructionSubMenu = new Submenu(win, true);
            ConstructionSubMenu.AddTab(Localizer.Token(304));

            ScoutDropDown      = DropOptions<int>(win.X + 12, win.Y + 25 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18);
            ColonyShipDropDown = DropOptions<int>(win.X + 12, win.Y + 65 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18);

            Checkbox(win.X, win.Y + 25,  () => EmpireManager.Player.AutoExplore,    title:305, tooltip:2226);
            Checkbox(win.X, win.Y + 65,  () => EmpireManager.Player.AutoColonize,   title:306, tooltip:2227);
            Checkbox(win.X, win.Y + 105, () => EmpireManager.Player.AutoFreighters, title:308, tooltip:2229);

            AutoFreighterDropDown = DropOptions<int>(win.X + 12, win.Y + 105 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18);

            Label(win.X + 29, win.Y + 155, titleId:6181);
            ConstructorDropDown = DropOptions<int>(win.X + 12, win.Y + 155 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18);

            Checkbox(win.X, win.Y + 210, () => EmpireManager.Player.AutoBuild, Localizer.Token(307) + " Projectors", 2228);

            int YPos(int i) => win.Y + 210 + Fonts.Arial12Bold.LineSpacing * i + 3 * i;
            Checkbox(win.X, YPos(1), () => GlobalStats.AutoCombat,              title:2207, tooltip:2230);
            Checkbox(win.X, YPos(2), () => EmpireManager.Player.AutoResearch,   title:6136, tooltip:7039);
            Checkbox(win.X, YPos(3), () => EmpireManager.Player.data.AutoTaxes, title:6138, tooltip:7040);

            UpdateDropDowns();
        }


        public void ToggleVisibility()
        {
            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
            IsOpen = !IsOpen;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Rectangle r = ConstructionSubMenu.Menu;
            r.Y = r.Y + 25;
            r.Height = r.Height - 25;
            var sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(ScreenManager.SpriteBatch);
            ConstructionSubMenu.Draw();

            base.Draw(spriteBatch);
        }


        public override bool HandleInput(InputState input)
        {
            if (/*!ConstructionSubMenu.Menu.HitTest(input.CursorPosition) || */input.RightMouseClick)
            {
                IsOpen = false;
                return false;
            }

            if (base.HandleInput(input))
            {
                EmpireData playerData = EmpireManager.Player.data;
                playerData.CurrentAutoFreighter = AutoFreighterDropDown.ActiveName;
                playerData.CurrentAutoColony    = ColonyShipDropDown.ActiveName;
                playerData.CurrentConstructor   = ConstructorDropDown.ActiveName;
                playerData.CurrentAutoScout     = ScoutDropDown.ActiveName;
                return true;
            }
            return false;
        }


        private static void InitDropOptions(DropOptions<int> options, ref string automationShip, string defaultShip, Func<Ship, bool> predicate)
        {
            options.Clear();

            foreach (string ship in EmpireManager.Player.ShipsWeCanBuild)
            {
                if (!ResourceManager.GetShipTemplate(ship, out Ship template) && !predicate(template))
                    continue;
                options.AddOption(template.Name, 0);
            }

            if (!options.SetActiveEntry(automationShip)) // try set the current automationShip active
            {
                if (!options.SetActiveEntry(defaultShip)) // we can't build a default ship??? wtf
                {
                    Log.Warning("Failed to enable default automation ship '{0}' for player {1}", defaultShip, EmpireManager.Player);
                    Log.Warning("Buildable ships: {0}", EmpireManager.Player.ShipsWeCanBuild);
                    options.AddOption(defaultShip, 0);
                }

                // always set to default ship
                automationShip = defaultShip;
            }
        }

        public void UpdateDropDowns()
        {
            EmpireData playerData = Universe.player.data;

            InitDropOptions(AutoFreighterDropDown, ref playerData.CurrentAutoFreighter, playerData.DefaultSmallTransport, 
                (ship) => ship.Thrust > 0f && !ship.isColonyShip && ship.CargoSpaceMax > 0f);

            InitDropOptions(ColonyShipDropDown, ref playerData.CurrentAutoColony, playerData.DefaultColonyShip, 
                (ship) => ship.Thrust > 0f && ship.isColonyShip);

            InitDropOptions(ConstructorDropDown, ref playerData.CurrentConstructor, playerData.DefaultConstructor, 
                (ship) =>
                {
                    if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.ConstructionModule)
                        return ship.Thrust > 0f && (ship.isConstructor || ship.Name == playerData.DefaultConstructor);

                    return ship.Thrust > 0f && !ship.isColonyShip && ship.CargoSpaceMax > 0f && 
                            (ship.isConstructor || ship.shipData.Role == ShipData.RoleName.freighter);
                });

            InitDropOptions(ScoutDropDown, ref playerData.CurrentAutoScout, playerData.StartingScout, 
                (ship) =>
                {
                    if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.reconDropDown)
                        return ship.Thrust > 0f && 
                              (ship.shipData?.Role == ShipData.RoleName.scout || 
                               ship.shipData?.ShipCategory == ShipData.Category.Recon);

                    return ship.Thrust > 0f && 
                          (ship.shipData?.Role == ShipData.RoleName.scout ||
                           ship.shipData?.Role == ShipData.RoleName.fighter ||
                           ship.shipData?.ShipCategory == ShipData.Category.Recon);
                });
        }
    }
}