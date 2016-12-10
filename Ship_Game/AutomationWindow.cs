using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
	public sealed class AutomationWindow
	{
		public bool isOpen;
		private Ship_Game.ScreenManager ScreenManager;
		private Submenu ConstructionSubMenu;
		private UniverseScreen screen;
		private Rectangle win;
		private List<Checkbox> Checkboxes = new List<Checkbox>();
		private DropOptions AutoFreighterDropDown;
		private DropOptions ColonyShipDropDown;
		private DropOptions ScoutDropDown;
        private DropOptions ConstructorDropDown;
        private Vector2 ConstructorTitle;
        private string ConstructorString;

		public AutomationWindow(Ship_Game.ScreenManager ScreenManager, UniverseScreen screen)
		{
			this.screen = screen;
			this.ScreenManager = ScreenManager;
			int WindowWidth = 210;
			this.win = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 115 - WindowWidth, 490, WindowWidth, 300);
			Rectangle rectangle = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 5 - WindowWidth + 20, 225, WindowWidth - 40, 455);
			this.ConstructionSubMenu = new Submenu(ScreenManager, this.win, true);
			this.ConstructionSubMenu.AddTab(Localizer.Token(304));

			Ref<bool> aeRef = new Ref<bool>(() => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoExplore, (bool x) => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoExplore = x);
			Checkbox cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 25)), Localizer.Token(305), aeRef, Fonts.Arial12Bold);
			this.Checkboxes.Add(cb);
			cb.Tip_Token = 2226;

            this.ScoutDropDown = new DropOptions(new Rectangle(this.win.X + 12, this.win.Y + 25 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18));

			Ref<bool> acRef = new Ref<bool>(() => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoColonize, (bool x) => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoColonize = x);
			cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 65)), Localizer.Token(306), acRef, Fonts.Arial12Bold);
			this.Checkboxes.Add(cb);
			cb.Tip_Token = 2227;

			this.ColonyShipDropDown = new DropOptions(new Rectangle(this.win.X + 12, this.win.Y + 65 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18));

			Ref<bool> afRef = new Ref<bool>(() => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoFreighters, (bool x) => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoFreighters = x);
			cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 105)), Localizer.Token(308), afRef, Fonts.Arial12Bold);
			this.Checkboxes.Add(cb);
			cb.Tip_Token = 2229;

			this.AutoFreighterDropDown = new DropOptions(new Rectangle(this.win.X + 12, this.win.Y + 105 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18));

            this.ConstructorTitle = new Vector2((float)this.win.X + 29, (float)(this.win.Y + 155));
            this.ConstructorString = Localizer.Token(6181);
            this.ConstructorDropDown = new DropOptions(new Rectangle(this.win.X + 12, this.win.Y + 155 + Fonts.Arial12Bold.LineSpacing + 7, 190, 18));


            Ref<bool> abRef = new Ref<bool>(() => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoBuild, (bool x) => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoBuild = x);
            cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 210)), string.Concat(Localizer.Token(307), " Projectors"), abRef, Fonts.Arial12Bold);
            this.Checkboxes.Add(cb);
            cb.Tip_Token = 2228;

			Ref<bool> acomRef = new Ref<bool>(() => GlobalStats.AutoCombat, (bool x) => GlobalStats.AutoCombat = x);
            cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 210 + Fonts.Arial12Bold.LineSpacing + 3)), Localizer.Token(2207), acomRef, Fonts.Arial12Bold);
			this.Checkboxes.Add(cb);
			cb.Tip_Token = 2230;

            Ref<bool> arRef = new Ref<bool>(() => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoResearch, (bool x) => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoResearch = x);
            cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 210 + Fonts.Arial12Bold.LineSpacing * 2 + 6)), Localizer.Token(6136), arRef, Fonts.Arial12Bold);
            this.Checkboxes.Add(cb);
            cb.Tip_Token = 7039;

            Ref<bool> atRef = new Ref<bool>(() => EmpireManager.Player.data.AutoTaxes, (bool x) => EmpireManager.Player.data.AutoTaxes = x);
            cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 210 + Fonts.Arial12Bold.LineSpacing * 3 + 9)), Localizer.Token(6138), atRef, Fonts.Arial12Bold);
            this.Checkboxes.Add(cb);
            cb.Tip_Token = 7040;

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
            var empire = EmpireManager.GetEmpireByName(screen.PlayerLoyalty);
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
            var playerData = screen.player.data;
		    string current = !string.IsNullOrEmpty(playerData.CurrentAutoFreighter) ? playerData.CurrentAutoFreighter : playerData.DefaultSmallTransport;

            var empire = EmpireManager.GetEmpireByName(screen.PlayerLoyalty);
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
            if (string.IsNullOrEmpty(empire.data.CurrentConstructor) || !ResourceManager.ShipsDict.ContainsKey(empire.data.CurrentConstructor))
            {
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