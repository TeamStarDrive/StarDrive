using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed class AutomationWindow : GameScreen
    {
        public bool isOpen;
        private Submenu ConstructionSubMenu;
        private UniverseScreen Universe;
        private DropOptions<int> AutoFreighterDropDown;
        private DropOptions<int> ColonyShipDropDown;
        private DropOptions<int> ScoutDropDown;
        private DropOptions<int> ConstructorDropDown;

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

            Label(win.X + 29, win.Y + 155, localization:6181);
            ConstructorDropDown = DropOptions<int>(win.X + 12, win.Y + 155 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18);

            Checkbox(win.X, win.Y + 210, () => EmpireManager.Player.AutoBuild, Localizer.Token(307) + " Projectors", 2228);

            int YPos(int i) => win.Y + 210 + Fonts.Arial12Bold.LineSpacing * i + 3 * i;
            Checkbox(win.X, YPos(1), () => GlobalStats.AutoCombat,              title:2207, tooltip:2230);
            Checkbox(win.X, YPos(2), () => EmpireManager.Player.AutoResearch,   title:6136, tooltip:7039);
            Checkbox(win.X, YPos(3), () => EmpireManager.Player.data.AutoTaxes, title:6138, tooltip:7040);

            SetDropDowns();
        }


        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            Rectangle r = ConstructionSubMenu.Menu;
            r.Y = r.Y + 25;
            r.Height = r.Height - 25;
            Selector sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(ScreenManager.SpriteBatch);
            ConstructionSubMenu.Draw();

            base.Draw(spriteBatch);

            spriteBatch.End();
        }


        public override bool HandleInput(InputState input)
        {
            return base.HandleInput(input);
            //var empire = EmpireManager.Player;
            //if (!ColonyShipDropDown.Open && !ScoutDropDown.Open && !ConstructorDropDown.Open)
            //{
            //    AutoFreighterDropDown.HandleInput(input);
            //}
            //try
            //{
            //    empire.data.CurrentAutoFreighter = AutoFreighterDropDown.Options[AutoFreighterDropDown.ActiveIndex].Name;
            //}
            //catch
            //{
            //    AutoFreighterDropDown.ActiveIndex = 0;
            //}


            //if (!AutoFreighterDropDown.Open && !ScoutDropDown.Open && !ConstructorDropDown.Open)
            //{
            //    ColonyShipDropDown.HandleInput(input);
            //}
            //try
            //{
            //    empire.data.CurrentAutoColony = ColonyShipDropDown.Options[ColonyShipDropDown.ActiveIndex].Name;
            //}
            //catch
            //{
            //    ColonyShipDropDown.ActiveIndex = 0;
            //}


            //if (!ColonyShipDropDown.Open && !AutoFreighterDropDown.Open && !ConstructorDropDown.Open)
            //{
            //    ScoutDropDown.HandleInput(input);
            //}
            //try
            //{
            //    empire.data.CurrentAutoScout = ScoutDropDown.Options[ScoutDropDown.ActiveIndex].Name;
            //}
            //catch
            //{
            //    ScoutDropDown.ActiveIndex = 0;
            //}

            //if (!ColonyShipDropDown.Open && !AutoFreighterDropDown.Open && !ScoutDropDown.Open)
            //{
            //    ConstructorDropDown.HandleInput(input);
            //}
            //try
            //{
            //    empire.data.CurrentConstructor = ConstructorDropDown.Options[ConstructorDropDown.ActiveIndex].Name;
            //}
            //catch
            //{
            //    ConstructorDropDown.ActiveIndex = 0;
            //}

            //if (Checkboxes.Any(checkbox => checkbox.HandleInput(input)))
            //    return true;

            //if (!ConstructionSubMenu.Menu.HitTest(input.CursorPosition) || !input.RightMouseClick)
            //    return false;
            //isOpen = false;
            //return true;
        }

        public void SetDropDowns()
        {
            ResetDropDowns();
            var playerData = Universe.player.data;
            string currentFreighter = playerData.CurrentAutoFreighter.NotEmpty()
                                    ? playerData.CurrentAutoFreighter : playerData.DefaultSmallTransport;

            var empire = EmpireManager.Player;
            foreach (string ship in empire.ShipsWeCanBuild)
            {                
                if (!ResourceManager.ShipsDict.TryGetValue(ship, out Ship automation) 
                        || automation.isColonyShip || automation.CargoSpaceMax <= 0f || automation.Thrust <= 0f 
                        || ResourceManager.ShipRoles[automation.shipData.Role].Protected)
                    continue;
                AutoFreighterDropDown.AddOption(automation.Name, 0);
            }
            SetAutomationDropdown(AutoFreighterDropDown, currentFreighter, out empire.data.CurrentAutoFreighter);

            string currentColony = playerData.CurrentAutoColony.NotEmpty()
                                 ? playerData.CurrentAutoColony : playerData.DefaultColonyShip;
            
            foreach (string ship in empire.ShipsWeCanBuild)
            {
                if (!ResourceManager.ShipsDict.TryGetValue(ship, out Ship automation) ||
                    !automation.isColonyShip || automation.Thrust <= 0f)
                    continue;
                ColonyShipDropDown.AddOption(ResourceManager.ShipsDict[ship].Name, 0);
            }
            SetAutomationDropdown(ColonyShipDropDown, currentColony, out empire.data.CurrentAutoColony);

            string currentConstructor;
            if (playerData.CurrentConstructor.NotEmpty())
                currentConstructor = playerData.CurrentConstructor;
            else
                currentConstructor = playerData.DefaultConstructor.IsEmpty()
                                   ? playerData.DefaultSmallTransport : playerData.DefaultConstructor;

            foreach (string shipName in empire.ShipsWeCanBuild)
            {
                Ship ship = ResourceManager.ShipsDict[shipName];
                if (GlobalStats.ActiveMod != null && GlobalStats.ActiveModInfo.ConstructionModule)
                {
                    if ((!ship.isConstructor && shipName != playerData.DefaultConstructor) || ship.Thrust <= 0f)
                        continue;
                    ConstructorDropDown.AddOption(ship.Name, 0);
                }
                else
                {
                    if ((ship.shipData.Role != ShipData.RoleName.freighter && !ship.isConstructor) 
                        || ship.CargoSpaceMax <= 0f || ship.Thrust <= 0f || ship.isColonyShip)
                        continue;
                    ConstructorDropDown.AddOption(ship.Name, 0);
                }
            }
            SetAutomationDropdown(ConstructorDropDown, currentConstructor, out empire.data.CurrentConstructor);

            string currentScout = playerData.CurrentAutoScout.NotEmpty() ? playerData.CurrentAutoScout : playerData.StartingScout;
            if (ScoutDropDown.NotEmpty)
                currentScout = ScoutDropDown.ActiveName;

            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.reconDropDown)
            {
                foreach (string shipName in empire.ShipsWeCanBuild)
                {
                    Ship ship = ResourceManager.ShipsDict[shipName];
                    if (ship.shipData.Role != ShipData.RoleName.scout 
                        && (ship.shipData == null || ship.shipData.ShipCategory != ShipData.Category.Recon) || ship.Thrust <= 0f)
                        continue;
                    ScoutDropDown.AddOption(ship.Name, 0);
                }
            }
            else
            {
                foreach (string shipName in empire.ShipsWeCanBuild)
                {
                    Ship ship = ResourceManager.ShipsDict[shipName];
                    if (ship.shipData.Role != ShipData.RoleName.scout && ship.shipData.Role != ShipData.RoleName.fighter 
                        && (ship.shipData == null || ship.shipData.ShipCategory != ShipData.Category.Recon) || ship.Thrust <= 0f)
                        continue;
                    ScoutDropDown.AddOption(ship.Name, 0);
                }
            }
            SetAutomationDropdown(ScoutDropDown, currentScout, out empire.data.CurrentAutoScout);
        }

        private void SetAutomationDropdown(DropOptions<int> dropdown, string CurrentAutoShip, out string CurrentEmpireAutoShip)
        {
            if (dropdown.Count == 0)
                dropdown.AddOption("None", 0);

            CurrentEmpireAutoShip = dropdown.ActiveName;
            if (CurrentEmpireAutoShip.IsEmpty() || !ResourceManager.ShipsDict.ContainsKey(CurrentEmpireAutoShip))
            {
                CurrentEmpireAutoShip = dropdown.ActiveName;
            }
            else
            {
                if (dropdown.SetActiveEntry(CurrentAutoShip))
                    CurrentEmpireAutoShip = CurrentAutoShip;
            }
        }

        private void ResetDropDowns()
        {
            AutoFreighterDropDown.Clear();
            ColonyShipDropDown.Clear();
            ScoutDropDown.Clear();
            ConstructorDropDown.Clear();
        }
    }
}