using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
    public sealed class AutomationWindow
    {
        public bool isOpen;
        private ScreenManager ScreenManager;
        private Submenu ConstructionSubMenu;
        private UniverseScreen Universe;
        private Rectangle win;
        private Array<UICheckBox> Checkboxes = new Array<UICheckBox>();
        private DropOptions<int> AutoFreighterDropDown;
        private DropOptions<int> ColonyShipDropDown;
        private DropOptions<int> ScoutDropDown;
        private DropOptions<int> ConstructorDropDown;
        private Vector2 ConstructorTitle;
        private string ConstructorString;

        private void Checkbox(float x, float y, Expression<Func<bool>> binding, int title, int tooltip)
        {
            Checkboxes.Add(new UICheckBox(x, y, binding, Fonts.Arial12Bold, title, tooltip));
        }
        private void Checkbox(float x, float y, Expression<Func<bool>> binding, string title, int tooltip)
        {
            Checkboxes.Add(new UICheckBox(x, y, binding, Fonts.Arial12Bold, title, tooltip));
        }

        public AutomationWindow(ScreenManager screenManager, UniverseScreen universe)
        {
            Universe = universe;
            ScreenManager = screenManager;
            const int windowWidth = 210;
            win = new Rectangle(screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 115 - windowWidth, 490, windowWidth, 300);
            ConstructionSubMenu = new Submenu(win, true);
            ConstructionSubMenu.AddTab(Localizer.Token(304));

            ScoutDropDown      = new DropOptions<int>(new Rectangle(win.X + 12, win.Y + 25 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18));
            ColonyShipDropDown = new DropOptions<int>(new Rectangle(win.X + 12, win.Y + 65 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18));

            Checkbox(win.X, win.Y + 25,  () => EmpireManager.Player.AutoExplore,    title:305, tooltip:2226);
            Checkbox(win.X, win.Y + 65,  () => EmpireManager.Player.AutoColonize,   title:306, tooltip:2227);
            Checkbox(win.X, win.Y + 105, () => EmpireManager.Player.AutoFreighters, title:308, tooltip:2229);

            AutoFreighterDropDown = new DropOptions<int>(new Rectangle(win.X + 12, win.Y + 105 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18));

            ConstructorTitle = new Vector2(win.X + 29, win.Y + 155);
            ConstructorString = Localizer.Token(6181);
            ConstructorDropDown = new DropOptions<int>(new Rectangle(this.win.X + 12, this.win.Y + 155 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18));

            Checkbox(win.X, win.Y + 210, () => EmpireManager.Player.AutoBuild, Localizer.Token(307) + " Projectors", 2228);

            Func<int, int> yPos = i => win.Y + 210 + Fonts.Arial12Bold.LineSpacing * i + 3 * i;
            Checkbox(win.X, yPos(1), () => GlobalStats.AutoCombat,              title:2207, tooltip:2230);
            Checkbox(win.X, yPos(2), () => EmpireManager.Player.AutoResearch,   title:6136, tooltip:7039);
            Checkbox(win.X, yPos(3), () => EmpireManager.Player.data.AutoTaxes, title:6138, tooltip:7040);

            this.SetDropDowns();
        }


        public void Draw(GameTime gameTime)
        {
            Rectangle r = ConstructionSubMenu.Menu;
            r.Y = r.Y + 25;
            r.Height = r.Height - 25;
            Selector sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(ScreenManager.SpriteBatch);
            ConstructionSubMenu.Draw();
            foreach (UICheckBox cb in Checkboxes)
            {
                cb.Draw(ScreenManager.SpriteBatch);
            }
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ConstructorString, ConstructorTitle, Color.White);
            ConstructorDropDown.Draw(ScreenManager.SpriteBatch);
            AutoFreighterDropDown.Draw(ScreenManager.SpriteBatch);
            ColonyShipDropDown.Draw(ScreenManager.SpriteBatch);
            ScoutDropDown.Draw(ScreenManager.SpriteBatch);
        }


        public bool HandleInput(InputState input)
        {
            return false;
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
            //if for any reason the dropdown is empty, trying to get the selected item name causes an IooB crash.
            //make sure the dropdown has a dummy listing when no fitting designs are available
            if (AutoFreighterDropDown.Count == 0)
                AutoFreighterDropDown.AddOption("None", 0);

            if (AutoFreighterDropDown.SetActiveEntry(currentFreighter))
                empire.data.CurrentAutoFreighter = currentFreighter;

            string currentColony = playerData.CurrentAutoColony.NotEmpty()
                                 ? playerData.CurrentAutoColony : playerData.DefaultColonyShip;
            
            foreach (string ship in empire.ShipsWeCanBuild)
            {
                if (!ResourceManager.ShipsDict.TryGetValue(ship, out Ship automation) ||
                    !automation.isColonyShip || automation.Thrust <= 0f)
                    continue;
                ColonyShipDropDown.AddOption(ResourceManager.ShipsDict[ship].Name, 0);
            }
            //if for any reason the dropdown is empty, trying to get the selected item name causes an IooB crash.
            //make sure the dropdown has a dummy listing when no fitting designs are available
            if (ColonyShipDropDown.Count == 0)
                ColonyShipDropDown.AddOption("None", 0);

            empire.data.CurrentAutoColony = ColonyShipDropDown.ActiveName;
            if (empire.data.CurrentAutoColony.IsEmpty() || !ResourceManager.ShipsDict.ContainsKey(empire.data.CurrentAutoColony))
            {
                empire.data.CurrentAutoColony = ColonyShipDropDown.ActiveName;
            }
            else
            {
                if (ColonyShipDropDown.SetActiveEntry(currentColony))
                    empire.data.CurrentAutoColony = currentColony;
            }


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
            //if for any reason the dropdown is empty, trying to get the selected item name causes an IooB crash.
            //make sure the dropdown has a dummy listing when no fitting designs are available
            if (ConstructorDropDown.Count == 0)
                ConstructorDropDown.AddOption("None", 0);

            if (empire.data.CurrentConstructor.IsEmpty() || !ResourceManager.ShipsDict.ContainsKey(empire.data.CurrentConstructor))
            {
                empire.data.CurrentConstructor = ConstructorDropDown.ActiveName;
            }
            else
            {
                if (ConstructorDropDown.SetActiveEntry(currentConstructor))
                    empire.data.CurrentConstructor = currentConstructor;
            }


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
            //if for any reason the dropdown is empty, trying to get the selected item name causes an IooB crash.
            //make sure the dropdown has a dummy listing when no fitting designs are available
            if (ScoutDropDown.Count == 0)
                ScoutDropDown.AddOption("None", 0);

            if (empire.data.CurrentAutoScout.IsEmpty() || !ResourceManager.ShipsDict.ContainsKey(empire.data.CurrentAutoScout))
            {
                if (ScoutDropDown.NotEmpty)
                    empire.data.CurrentAutoScout = ScoutDropDown.ActiveName;
            }
            else
            {
                if (ScoutDropDown.SetActiveEntry(currentScout))
                    empire.data.CurrentAutoScout = currentScout;
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