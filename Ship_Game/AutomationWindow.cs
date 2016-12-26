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
		private List<Checkbox> Checkboxes = new List<Checkbox>();
		private DropOptions AutoFreighterDropDown;
		private DropOptions ColonyShipDropDown;
		private DropOptions ScoutDropDown;
        private DropOptions ConstructorDropDown;
        private Vector2 ConstructorTitle;
        private string ConstructorString;

        private void Checkbox(float x, float y, Expression<Func<bool>> binding, int title, int tooltip)
        {
            Checkboxes.Add(new Checkbox(win.X, win.Y + 25, binding, Fonts.Arial12Bold, title, tooltip));
        }
        private void Checkbox(float x, float y, Expression<Func<bool>> binding, string title, int tooltip)
        {
            Checkboxes.Add(new Checkbox(win.X, win.Y + 25, binding, Fonts.Arial12Bold, title, tooltip));
        }

        public AutomationWindow(ScreenManager screenManager, UniverseScreen universe)
		{
			Universe = universe;
			ScreenManager = screenManager;
			const int windowWidth = 210;
			win = new Rectangle(screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 115 - windowWidth, 490, windowWidth, 300);
			ConstructionSubMenu = new Submenu(screenManager, win, true);
			ConstructionSubMenu.AddTab(Localizer.Token(304));

            ScoutDropDown      = new DropOptions(new Rectangle(win.X + 12, win.Y + 25 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18));
            ColonyShipDropDown = new DropOptions(new Rectangle(win.X + 12, win.Y + 65 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18));

            Checkbox(win.X, win.Y + 25,  () => EmpireManager.Player.AutoExplore,    title:305, tooltip:2226);
            Checkbox(win.X, win.Y + 65,  () => EmpireManager.Player.AutoColonize,   title:306, tooltip:2227);
            Checkbox(win.X, win.Y + 105, () => EmpireManager.Player.AutoFreighters, title:308, tooltip:2229);

			AutoFreighterDropDown = new DropOptions(new Rectangle(win.X + 12, win.Y + 105 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18));

            ConstructorTitle = new Vector2(win.X + 29, win.Y + 155);
            ConstructorString = Localizer.Token(6181);
            ConstructorDropDown = new DropOptions(new Rectangle(this.win.X + 12, this.win.Y + 155 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18));

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
			Selector sel = new Selector(ScreenManager, r, new Color(0, 0, 0, 210));
			sel.Draw();
			ConstructionSubMenu.Draw();
			foreach (Checkbox cb in Checkboxes)
			{
				cb.Draw(ScreenManager);
			}
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ConstructorString, ConstructorTitle, Color.White);
            ConstructorDropDown.Draw(ScreenManager.SpriteBatch);
			AutoFreighterDropDown.Draw(ScreenManager.SpriteBatch);
			ColonyShipDropDown.Draw(ScreenManager.SpriteBatch);
			ScoutDropDown.Draw(ScreenManager.SpriteBatch);
		}


        public bool HandleInput(InputState input)
        {
            var empire = EmpireManager.Player;
            if (!ColonyShipDropDown.Open && !ScoutDropDown.Open && !ConstructorDropDown.Open)
            {
                AutoFreighterDropDown.HandleInput(input);
            }
            try
            {
                empire.data.CurrentAutoFreighter = AutoFreighterDropDown.Options[AutoFreighterDropDown.ActiveIndex].Name;
            }
            catch
            {
                AutoFreighterDropDown.ActiveIndex = 0;
            }


            if (!AutoFreighterDropDown.Open && !ScoutDropDown.Open && !ConstructorDropDown.Open)
            {
                ColonyShipDropDown.HandleInput(input);
            }
            try
            {
                empire.data.CurrentAutoColony = ColonyShipDropDown.Options[ColonyShipDropDown.ActiveIndex].Name;
            }
            catch
            {
                ColonyShipDropDown.ActiveIndex = 0;
            }


            if (!ColonyShipDropDown.Open && !AutoFreighterDropDown.Open && !ConstructorDropDown.Open)
            {
                ScoutDropDown.HandleInput(input);
            }
            try
            {
                empire.data.CurrentAutoScout = ScoutDropDown.Options[ScoutDropDown.ActiveIndex].Name;
            }
            catch
            {
                ScoutDropDown.ActiveIndex = 0;
            }

            if (!ColonyShipDropDown.Open && !AutoFreighterDropDown.Open && !ScoutDropDown.Open)
            {
                ConstructorDropDown.HandleInput(input);
            }
            try
            {
                empire.data.CurrentConstructor = ConstructorDropDown.Options[ConstructorDropDown.ActiveIndex].Name;
            }
            catch
            {
                ConstructorDropDown.ActiveIndex = 0;
            }

            if (Checkboxes.Any(checkbox => checkbox.HandleInput(input)))
                return true;

            if (!HelperFunctions.CheckIntersection(ConstructionSubMenu.Menu, input.CursorPosition) || !input.RightMouseClick)
                return false;
            isOpen = false;
            return true;
        }

		public void SetDropDowns()
		{
            ResetDropDowns();
            var playerData = Universe.player.data;
		    string current = !string.IsNullOrEmpty(playerData.CurrentAutoFreighter) ? playerData.CurrentAutoFreighter : playerData.DefaultSmallTransport;

            var empire = EmpireManager.Player;
			foreach (string ship in empire.ShipsWeCanBuild)
			{                
                if (!ResourceManager.ShipsDict.TryGetValue(ship, out Ship automation) 
                        || automation.isColonyShip || automation.CargoSpace_Max <= 0f || automation.Thrust <= 0f 
                        || ResourceManager.ShipRoles[automation.shipData.Role].Protected)
					continue;
				AutoFreighterDropDown.AddOption(automation.Name, 0);
			}
			foreach (Entry e in AutoFreighterDropDown.Options)
			{
				if (e.Name != current)
					continue;
				AutoFreighterDropDown.ActiveIndex = AutoFreighterDropDown.Options.IndexOf(e);
				empire.data.CurrentAutoFreighter  = AutoFreighterDropDown.Options[AutoFreighterDropDown.ActiveIndex].Name;
			}

            string currentColony = !string.IsNullOrEmpty(playerData.CurrentAutoColony) ? playerData.CurrentAutoColony : playerData.DefaultColonyShip;
            
			foreach (string ship in empire.ShipsWeCanBuild)
			{
				if (!ResourceManager.ShipsDict.TryGetValue(ship, out Ship automation) || !automation.isColonyShip || automation.Thrust <= 0f)
					continue;
				ColonyShipDropDown.AddOption(ResourceManager.ShipsDict[ship].Name, 0);
			}
			if (string.IsNullOrEmpty(empire.data.CurrentAutoColony) || !ResourceManager.ShipsDict.ContainsKey(empire.data.CurrentAutoColony))
			{
				empire.data.CurrentAutoColony = ColonyShipDropDown.Options[ColonyShipDropDown.ActiveIndex].Name;
			}
			else
			{
				foreach (Entry e in ColonyShipDropDown.Options)
				{
					if (e.Name != currentColony)
						continue;
					ColonyShipDropDown.ActiveIndex = ColonyShipDropDown.Options.IndexOf(e);
					empire.data.CurrentAutoColony  = ColonyShipDropDown.Options[ColonyShipDropDown.ActiveIndex].Name;
				}
			}


            string constructor;
            if (!string.IsNullOrEmpty(playerData.CurrentConstructor))
                constructor = playerData.CurrentConstructor;
            else
                constructor = string.IsNullOrEmpty(playerData.DefaultConstructor) ? playerData.DefaultSmallTransport : playerData.DefaultConstructor;

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
                        || ship.CargoSpace_Max <= 0f || ship.Thrust <= 0f || ship.isColonyShip)
                        continue;
                    ConstructorDropDown.AddOption(ship.Name, 0);
                }
            }
            if (string.IsNullOrEmpty(empire.data.CurrentConstructor) 
                || !ResourceManager.ShipsDict.ContainsKey(empire.data.CurrentConstructor))
            {
                // @todo This is just a temporary measure. It does not fix the problem with ActiveIndex being invalid at times
                if (ConstructorDropDown.ActiveIndex < ConstructorDropDown.Options.Count)
                    empire.data.CurrentConstructor = ConstructorDropDown.Options[ConstructorDropDown.ActiveIndex].Name;
            }
            else
            {
                foreach (Entry e in ConstructorDropDown.Options)
                {
                    if (e.Name != constructor)
                        continue;
                    ConstructorDropDown.ActiveIndex = ConstructorDropDown.Options.IndexOf(e);
                    empire.data.CurrentConstructor = ConstructorDropDown.Options[ConstructorDropDown.ActiveIndex].Name;
                }
            }



			string currentScout = !string.IsNullOrEmpty(playerData.CurrentAutoScout) ? playerData.CurrentAutoScout : playerData.StartingScout;
			if (ScoutDropDown.Options.Count > 0)
				currentScout = ScoutDropDown.Options[ScoutDropDown.ActiveIndex].Name;

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

			if (string.IsNullOrEmpty(empire.data.CurrentAutoScout) || !ResourceManager.ShipsDict.ContainsKey(empire.data.CurrentAutoScout))
			{
				empire.data.CurrentAutoScout = ScoutDropDown.Options[ScoutDropDown.ActiveIndex].Name;
			}
			else
			{
				foreach (Entry e in ScoutDropDown.Options)
				{
					if (e.Name != currentScout)
						continue;
					ScoutDropDown.ActiveIndex = ScoutDropDown.Options.IndexOf(e);
				}
			}
		}

        private void ResetDropDowns()
        {
            AutoFreighterDropDown.Options.Clear();
            ColonyShipDropDown.Options.Clear();
            ScoutDropDown.Options.Clear();
            ConstructorDropDown.Options.Clear();
        }
	}
}