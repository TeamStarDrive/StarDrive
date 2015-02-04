using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
	public class AutomationWindow
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

		public AutomationWindow(Ship_Game.ScreenManager ScreenManager, UniverseScreen screen)
		{
			this.screen = screen;
			this.ScreenManager = ScreenManager;
			int WindowWidth = 210;
			this.win = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 115 - WindowWidth, 490, WindowWidth, 250);
			Rectangle rectangle = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 5 - WindowWidth + 20, 225, WindowWidth - 40, 455);
			this.ConstructionSubMenu = new Submenu(ScreenManager, this.win, true);
			this.ConstructionSubMenu.AddTab(Localizer.Token(304));

			Ref<bool> aeRef = new Ref<bool>(() => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoExplore, (bool x) => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoExplore = x);
			Checkbox cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 25)), Localizer.Token(305), aeRef, Fonts.Arial12Bold);
			this.Checkboxes.Add(cb);
			cb.Tip_Token = 2226;

            this.ScoutDropDown = new DropOptions(new Rectangle(this.win.X + 15, this.win.Y + 25 + Fonts.Arial12Bold.LineSpacing + 7, 150, 18));

			Ref<bool> acRef = new Ref<bool>(() => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoColonize, (bool x) => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoColonize = x);
			cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 65)), Localizer.Token(306), acRef, Fonts.Arial12Bold);
			this.Checkboxes.Add(cb);
			cb.Tip_Token = 2227;

			this.ColonyShipDropDown = new DropOptions(new Rectangle(this.win.X + 15, this.win.Y + 65 + Fonts.Arial12Bold.LineSpacing + 7, 150, 18));

			Ref<bool> afRef = new Ref<bool>(() => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoFreighters, (bool x) => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoFreighters = x);
			cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 105)), Localizer.Token(308), afRef, Fonts.Arial12Bold);
			this.Checkboxes.Add(cb);
			cb.Tip_Token = 2229;

			this.AutoFreighterDropDown = new DropOptions(new Rectangle(this.win.X + 15, this.win.Y + 105 + Fonts.Arial12Bold.LineSpacing + 7, 150, 18));

            Ref<bool> abRef = new Ref<bool>(() => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoBuild, (bool x) => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoBuild = x);
            cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 160)), string.Concat(Localizer.Token(307), " Projectors"), abRef, Fonts.Arial12Bold);
            this.Checkboxes.Add(cb);
            cb.Tip_Token = 2228;

			Ref<bool> acomRef = new Ref<bool>(() => GlobalStats.AutoCombat, (bool x) => GlobalStats.AutoCombat = x);
            cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 160 + Fonts.Arial12Bold.LineSpacing + 3)), Localizer.Token(2207), acomRef, Fonts.Arial12Bold);
			this.Checkboxes.Add(cb);
			cb.Tip_Token = 2230;

            Ref<bool> arRef = new Ref<bool>(() => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoResearch, (bool x) => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoResearch = x);
            cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 160 + Fonts.Arial12Bold.LineSpacing * 2 + 6)), Localizer.Token(6136), arRef, Fonts.Arial12Bold);
            this.Checkboxes.Add(cb);
            cb.Tip_Token = 7039;

            Ref<bool> atRef = new Ref<bool>(() => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoTaxes, (bool x) => EmpireManager.GetEmpireByName(screen.PlayerLoyalty).AutoTaxes = x);
            cb = new Checkbox(new Vector2((float)this.win.X, (float)(this.win.Y + 160 + Fonts.Arial12Bold.LineSpacing * 3 + 9)), Localizer.Token(6138), atRef, Fonts.Arial12Bold);
            this.Checkboxes.Add(cb);
            cb.Tip_Token = 7040;

			this.SetDropDowns();
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public void Draw(GameTime gameTime)
		{
			Rectangle r = this.ConstructionSubMenu.Menu;
			r.Y = r.Y + 25;
			r.Height = r.Height - 25;
			Selector sel = new Selector(this.ScreenManager, r, new Color(0, 0, 0, 210));
			sel.Draw();
			this.ConstructionSubMenu.Draw();
			foreach (Checkbox cb in this.Checkboxes)
			{
				cb.Draw(this.ScreenManager);
			}
			this.AutoFreighterDropDown.Draw(this.ScreenManager.SpriteBatch);
			this.ColonyShipDropDown.Draw(this.ScreenManager.SpriteBatch);
			this.ScoutDropDown.Draw(this.ScreenManager.SpriteBatch);
		}

		~AutomationWindow()
		{
			this.Dispose(false);
		}

        public bool HandleInput(InputState input)
        {
            if (!this.ColonyShipDropDown.Open)
            {
                if (!this.ScoutDropDown.Open)
                    this.AutoFreighterDropDown.HandleInput(input);
            }
            try
            {
                EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.CurrentAutoFreighter = this.AutoFreighterDropDown.Options[this.AutoFreighterDropDown.ActiveIndex].Name;
            }
            catch
            {
                this.AutoFreighterDropDown.ActiveIndex = 0;
            }
            if (!this.AutoFreighterDropDown.Open)
            {
                if (!this.ScoutDropDown.Open)
                    this.ColonyShipDropDown.HandleInput(input);
            }
            try
            {
                EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.CurrentAutoColony = this.ColonyShipDropDown.Options[this.ColonyShipDropDown.ActiveIndex].Name;
            }
            catch
            {
                this.ColonyShipDropDown.ActiveIndex = 0;
            }
            if (!this.ColonyShipDropDown.Open)
            {
                if (!this.AutoFreighterDropDown.Open)
                    this.ScoutDropDown.HandleInput(input);
            }
            try
            {
                EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.CurrentAutoScout = this.ScoutDropDown.Options[this.ScoutDropDown.ActiveIndex].Name;
            }
            catch
            {
                this.ScoutDropDown.ActiveIndex = 0;
            }
            foreach (Checkbox checkbox in this.Checkboxes)
            {
                if (checkbox.HandleInput(input))
                    return true;
            }
            if (!HelperFunctions.CheckIntersection(this.ConstructionSubMenu.Menu, input.CursorPosition) || !input.RightMouseClick)
                return false;
            this.isOpen = false;
            return true;
        }

		public void SetDropDowns()
		{
            this.resetDropDowns();
			string Current;
            if (!String.IsNullOrEmpty(this.screen.player.data.CurrentAutoFreighter))
                Current = this.screen.player.data.CurrentAutoFreighter;
            else
                Current = this.screen.player.data.DefaultSmallTransport;
			foreach (string ship in EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).ShipsWeCanBuild)
			{
				if (ResourceManager.ShipsDict[ship].isColonyShip || ResourceManager.ShipsDict[ship].CargoSpace_Max <= 0f || ResourceManager.ShipsDict[ship].Thrust <= 0f || ResourceManager.ShipRoles[ResourceManager.ShipsDict[ship].Role].Protected)
				{
					continue;
				}
				this.AutoFreighterDropDown.AddOption(ResourceManager.ShipsDict[ship].Name, 0);
			}
			foreach (Entry e in this.AutoFreighterDropDown.Options)
			{
				if (e.Name != Current)
				{
					continue;
				}
				this.AutoFreighterDropDown.ActiveIndex = this.AutoFreighterDropDown.Options.IndexOf(e);
				EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.CurrentAutoFreighter = this.AutoFreighterDropDown.Options[this.AutoFreighterDropDown.ActiveIndex].Name;
			}
			string CurrentColony;
            if (!string.IsNullOrEmpty(this.screen.player.data.CurrentAutoColony))
                CurrentColony = this.screen.player.data.CurrentAutoColony;
            else
                CurrentColony = this.screen.player.data.DefaultColonyShip;
			foreach (string ship in EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).ShipsWeCanBuild)
			{
				if (!ResourceManager.ShipsDict[ship].isColonyShip || ResourceManager.ShipsDict[ship].Thrust <= 0f)
				{
					continue;
				}
				this.ColonyShipDropDown.AddOption(ResourceManager.ShipsDict[ship].Name, 0);
			}
			if (string.IsNullOrEmpty(EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.CurrentAutoColony) || !ResourceManager.ShipsDict.ContainsKey(EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.CurrentAutoColony))
			{
				EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.CurrentAutoColony = this.ColonyShipDropDown.Options[this.ColonyShipDropDown.ActiveIndex].Name;
			}
			else
			{
				foreach (Entry e in this.ColonyShipDropDown.Options)
				{
					if (e.Name != CurrentColony)
					{
						continue;
					}
					this.ColonyShipDropDown.ActiveIndex = this.ColonyShipDropDown.Options.IndexOf(e);
					EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.CurrentAutoColony = this.ColonyShipDropDown.Options[this.ColonyShipDropDown.ActiveIndex].Name;
				}
			}
			string CurrentScout;
            if(!string.IsNullOrEmpty(this.screen.player.data.CurrentAutoScout))
                CurrentScout = this.screen.player.data.CurrentAutoScout;
            else
                CurrentScout = this.screen.player.data.StartingScout;
			if (this.ScoutDropDown.Options.Count > 0)
			{
				CurrentScout = this.ScoutDropDown.Options[this.ScoutDropDown.ActiveIndex].Name;
			}

            if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.reconDropDown)
            {
                foreach (string ship in EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).ShipsWeCanBuild)
                {
                    if (!(ResourceManager.ShipsDict[ship].Role == "scout") && (ResourceManager.ShipsDict[ship].shipData == null || ResourceManager.ShipsDict[ship].shipData.ShipCategory != ShipData.Category.Recon) || ResourceManager.ShipsDict[ship].Thrust <= 0f)
                    {
                        continue;
                    }
                    this.ScoutDropDown.AddOption(ResourceManager.ShipsDict[ship].Name, 0);
                }
            }
            else
            {
                foreach (string ship in EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).ShipsWeCanBuild)
                {
                    if (!(ResourceManager.ShipsDict[ship].Role == "scout") && !(ResourceManager.ShipsDict[ship].Role == "fighter") && (ResourceManager.ShipsDict[ship].shipData == null || ResourceManager.ShipsDict[ship].shipData.ShipCategory != ShipData.Category.Recon) || ResourceManager.ShipsDict[ship].Thrust <= 0f)
                    {
                        continue;
                    }
                    this.ScoutDropDown.AddOption(ResourceManager.ShipsDict[ship].Name, 0);
                }
            }

			if (string.IsNullOrEmpty(EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.CurrentAutoScout) || !ResourceManager.ShipsDict.ContainsKey(EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.CurrentAutoScout))
			{
				EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.CurrentAutoScout = this.ScoutDropDown.Options[this.ScoutDropDown.ActiveIndex].Name;
			}
			else
			{
				foreach (Entry e in this.ScoutDropDown.Options)
				{
					if (e.Name != CurrentScout)
					{
						continue;
					}
					this.ScoutDropDown.ActiveIndex = this.ScoutDropDown.Options.IndexOf(e);
				}
			}
		}

        private void resetDropDowns()
        {
            this.AutoFreighterDropDown.Options.Clear();
            this.ColonyShipDropDown.Options.Clear();
            this.ScoutDropDown.Options.Clear();
        }
	}
}