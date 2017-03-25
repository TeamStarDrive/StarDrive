using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ship_Game.AI;

namespace Ship_Game
{
	public sealed class ShipInfoUIElement : UIElement
	{
		public Array<ToggleButton> CombatStatusButtons = new Array<ToggleButton>();

		public Array<OrdersButton> Orders = new Array<OrdersButton>();

		private Array<ShipInfoUIElement.TippedItem> ToolTipItems = new Array<ShipInfoUIElement.TippedItem>();

		private Rectangle SliderRect;

		private UniverseScreen screen;

		public Ship ship;

		private Selector sel;

		public Rectangle LeftRect;

		public Rectangle RightRect;

		public Rectangle Housing;

		public Rectangle ShipInfoRect;

		public ToggleButton gridbutton;

		public Rectangle Power;

		public Rectangle Shields;

		public Rectangle Ordnance;

		private ProgressBar pBar;

		private ProgressBar sBar;

		private ProgressBar oBar;

		public UITextEntry ShipNameArea;

		private SlidingElement sliding_element;

		private Rectangle DefenseRect;

		private Rectangle TroopRect;

        private Rectangle FlagRect;  //fbedard

		private bool CanRename = true;

		private bool ShowModules = true;

		private string fmt = "0";
        private float DoubleClickTimer = .25f;

		public ShipInfoUIElement(Rectangle r, Ship_Game.ScreenManager sm, UniverseScreen screen)
		{
			this.screen = screen;
			this.ScreenManager = sm;
			this.ElementRect = r;
            this.FlagRect = new Rectangle(r.X + 150, r.Y + 50, 40, 40);
			this.sel = new Selector(this.ScreenManager, r, Color.Black);
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			this.SliderRect = new Rectangle(r.X - 100, r.Y + r.Height - 140, 530, 130);
			this.sliding_element = new SlidingElement(this.SliderRect);
			this.Housing = r;
			this.LeftRect = new Rectangle(r.X, r.Y + 44, 180, r.Height - 44);
			this.RightRect = new Rectangle(this.LeftRect.X + this.LeftRect.Width, this.LeftRect.Y, 220, this.LeftRect.Height);
			this.ShipNameArea = new UITextEntry()
			{
				ClickableArea = new Rectangle(this.Housing.X + 41, this.Housing.Y + 65, 200, Fonts.Arial20Bold.LineSpacing)
			};
			int spacing = 2;
			this.Power = new Rectangle(this.Housing.X + 187, this.Housing.Y + 110, 20, 20);
			Rectangle pbarrect = new Rectangle(this.Power.X + this.Power.Width + 15, this.Power.Y, 150, 18);
			this.pBar = new ProgressBar(pbarrect)
			{
				color = "green"
			};
			ShipInfoUIElement.TippedItem ti = new ShipInfoUIElement.TippedItem()
			{
				r = this.Power,
				TIP_ID = 27
			};
			this.ToolTipItems.Add(ti);
			this.Shields = new Rectangle(this.Housing.X + 187, this.Housing.Y + 110 + 20 + spacing, 20, 20);
			Rectangle pshieldsrect = new Rectangle(this.Shields.X + this.Shields.Width + 15, this.Shields.Y, 150, 18);
			this.sBar = new ProgressBar(pshieldsrect)
			{
				color = "blue"
			};
			ti = new ShipInfoUIElement.TippedItem()
			{
				r = this.Shields,
				TIP_ID = 28
			};
			this.ToolTipItems.Add(ti);
			this.Ordnance = new Rectangle(this.Housing.X + 187, this.Housing.Y + 110 + 20 + spacing + 20 + spacing, 20, 20);
			Rectangle pordrect = new Rectangle(this.Ordnance.X + this.Ordnance.Width + 15, this.Ordnance.Y, 150, 18);
			this.oBar = new ProgressBar(pordrect);
			ti = new ShipInfoUIElement.TippedItem()
			{
				r = this.Ordnance,
				TIP_ID = 29
			};
			this.ToolTipItems.Add(ti);
			this.DefenseRect = new Rectangle(this.Housing.X + 13, this.Housing.Y + 112, 22, 22);
			ti = new ShipInfoUIElement.TippedItem()
			{
				r = this.DefenseRect,
				TIP_ID = 30
			};
			this.ToolTipItems.Add(ti);
			this.TroopRect = new Rectangle(this.Housing.X + 13, this.Housing.Y + 137, 22, 22);
			ti = new ShipInfoUIElement.TippedItem()
			{
				r = this.TroopRect,
				TIP_ID = 37
			};
			this.ToolTipItems.Add(ti);
			this.ShipInfoRect = new Rectangle(this.Housing.X + 60, this.Housing.Y + 110, 115, 115);
			Rectangle gridRect = new Rectangle(this.Housing.X + 16, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 45, 34, 24);
			this.gridbutton = new ToggleButton(gridRect, "SelectionBox/button_grid_active", "SelectionBox/button_grid_inactive", "SelectionBox/button_grid_hover", "SelectionBox/button_grid_pressed", "SelectionBox/icon_grid")
			{
				Active = true
			};
			Vector2 OrdersBarPos = new Vector2((float)(this.Power.X + 15), (float)(this.Ordnance.Y + this.Ordnance.Height + spacing + 3));
            
            OrdersBarPos.X = pordrect.X - 15;
			ToggleButton AttackRuns = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_headon");
			this.CombatStatusButtons.Add(AttackRuns);
			AttackRuns.Action = "attack";
			AttackRuns.HasToolTip = true;
			AttackRuns.WhichToolTip = 1;
			
            OrdersBarPos.X = OrdersBarPos.X + 25f;
            ToggleButton ShortRange = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_grid");
            this.CombatStatusButtons.Add(ShortRange);
            ShortRange.Action = "short";
            ShortRange.HasToolTip = true;
            ShortRange.WhichToolTip = 228;
            
            OrdersBarPos.X = OrdersBarPos.X + 25f;
            ToggleButton Artillery = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_aft");
            this.CombatStatusButtons.Add(Artillery);
            Artillery.Action = "arty";
            Artillery.HasToolTip = true;
            Artillery.WhichToolTip = 2;
			
            OrdersBarPos.X = OrdersBarPos.X + 25f;
			ToggleButton HoldPos = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_x");
			this.CombatStatusButtons.Add(HoldPos);
			HoldPos.Action = "hold";
			HoldPos.HasToolTip = true;
			HoldPos.WhichToolTip = 65;
			OrdersBarPos.X = OrdersBarPos.X + 25f;
			ToggleButton OrbitLeft = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_left");
			this.CombatStatusButtons.Add(OrbitLeft);
			OrbitLeft.Action = "orbit_left";
			OrbitLeft.HasToolTip = true;
			OrbitLeft.WhichToolTip = 3;
            OrdersBarPos.Y = OrdersBarPos.Y + 25f;

            ToggleButton BroadsideLeft = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_bleft");
            this.CombatStatusButtons.Add(BroadsideLeft);
            BroadsideLeft.Action = "broadside_left";
            BroadsideLeft.HasToolTip = true;
            BroadsideLeft.WhichToolTip = 159;
            OrdersBarPos.Y = OrdersBarPos.Y - 25f;
            OrdersBarPos.X = OrdersBarPos.X + 25f;

			ToggleButton OrbitRight = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_right");
			this.CombatStatusButtons.Add(OrbitRight);
			OrbitRight.Action = "orbit_right";
			OrbitRight.HasToolTip = true;
			OrbitRight.WhichToolTip = 4;
            OrdersBarPos.Y = OrdersBarPos.Y + 25f;

            ToggleButton BroadsideRight = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_bright");
            this.CombatStatusButtons.Add(BroadsideRight);
            BroadsideRight.Action = "broadside_right";
            BroadsideRight.HasToolTip = true;
            BroadsideRight.WhichToolTip = 160;
            OrdersBarPos.Y = OrdersBarPos.Y - 25f;
            OrdersBarPos.X = OrdersBarPos.X + 25f;

			ToggleButton Evade = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_stop");
			this.CombatStatusButtons.Add(Evade);
			Evade.Action = "evade";
			Evade.HasToolTip = true;
			Evade.WhichToolTip = 6;
		}

		public override void Draw(GameTime gameTime)
		{
            if (this.screen.SelectedShip == null) return;  //fbedard

            string longName;
			float transitionOffset = MathHelper.SmoothStep(0f, 1f, base.TransitionPosition);
			int columns = this.Orders.Count / 2 + this.Orders.Count % 2;
			this.sliding_element.Draw(this.ScreenManager, (int)((float)(columns * 55) * (1f - base.TransitionPosition)) + (this.sliding_element.Open ? 20 - columns : 0));
			foreach (OrdersButton ob in this.Orders)
			{
				Rectangle r = ob.clickRect;
				r.X = r.X - (int)(transitionOffset * 300f);
				ob.Draw(this.ScreenManager, r);
			}
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/unitselmenu_main"], this.Housing, Color.White);
			this.gridbutton.Draw(this.ScreenManager);
			Vector2 NamePos = new Vector2((float)(this.Housing.X + 30), (float)(this.Housing.Y + 63));
			string name = (!string.IsNullOrEmpty(this.ship.VanityName) ? this.ship.VanityName : this.ship.Name);
			SpriteFont TitleFont = Fonts.Arial14Bold;
            Vector2 ShipSuperName = new Vector2((float)(this.Housing.X + 30), (float)(this.Housing.Y + 79));
			if (Fonts.Arial14Bold.MeasureString(name).X > 180f)
			{
				TitleFont = Fonts.Arial12Bold;
                NamePos.X = NamePos.X - 8;
				NamePos.Y = NamePos.Y + 1;
			}
			this.ShipNameArea.Draw(TitleFont, this.ScreenManager.SpriteBatch, NamePos, gameTime, this.tColor);
            //Added by McShooterz:
            //longName = string.Concat(this.ship.Name, " - ", Localizer.GetRole(this.ship.shipData.Role, this.ship.loyalty));
            longName = string.Concat(this.ship.Name, " - ", this.ship.shipData.GetRole());
            if (this.ship.shipData.ShipCategory != ShipData.Category.Unclassified)
                longName += string.Concat(" - ", this.ship.shipData.GetCategory());
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Visitor10, longName, ShipSuperName, Color.Orange);

			string text = HelperFunctions.ParseText(Fonts.Arial10, ShipListScreenEntry.GetStatusText(this.ship), 155f);
			Vector2 ShipStatus = new Vector2((float)(this.sel.Menu.X + this.sel.Menu.Width - 170), this.Housing.Y + 68);
			text = HelperFunctions.ParseText(Fonts.Arial10, ShipListScreenEntry.GetStatusText(this.ship), 155f);
			HelperFunctions.ClampVectorToInt(ref ShipStatus);
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial10, text, ShipStatus, this.tColor);
			ShipStatus.Y = ShipStatus.Y + Fonts.Arial12Bold.MeasureString(text).Y;
			this.ship.RenderOverlay(this.ScreenManager.SpriteBatch, this.ShipInfoRect, this.ShowModules);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Modules/NuclearReactorMedium"], this.Power, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Modules/Shield_1KW"], this.Shields, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Modules/Ordnance"], this.Ordnance, Color.White);
			this.pBar.Max = this.ship.PowerStoreMax;
			this.pBar.Progress = this.ship.PowerCurrent;
			this.pBar.Draw(this.ScreenManager.SpriteBatch);
			this.sBar.Max = this.ship.shield_max;
			this.sBar.Progress = this.ship.shield_power;
			this.sBar.Draw(this.ScreenManager.SpriteBatch);
			this.oBar.Max = this.ship.OrdinanceMax;
			this.oBar.Progress = this.ship.Ordinance;
			this.oBar.Draw(this.ScreenManager.SpriteBatch);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_shield"], this.DefenseRect, Color.White);
			Vector2 defPos = new Vector2((float)(this.DefenseRect.X + this.DefenseRect.Width + 2), (float)(this.DefenseRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
			SpriteBatch spriteBatch = this.ScreenManager.SpriteBatch;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			float mechanicalBoardingDefense = this.ship.MechanicalBoardingDefense + this.ship.TroopBoardingDefense;
			spriteBatch.DrawString(arial12Bold, mechanicalBoardingDefense.ToString(this.fmt), defPos, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_troop_shipUI"], this.TroopRect, Color.White);
			Vector2 troopPos = new Vector2((float)(this.TroopRect.X + this.TroopRect.Width + 2), (float)(this.TroopRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(this.ship.TroopList.Count, "/", this.ship.TroopCapacity), troopPos, Color.White);
			if (this.ship.loyalty == EmpireManager.Player)
			{
				foreach (ToggleButton button in this.CombatStatusButtons)
				{
					button.Draw(this.ScreenManager);
				}
			}
            else  //fbedard: Display race icon of enemy ship in Ship UI
            {
                Rectangle FlagShip = new Rectangle(this.FlagRect.X + 190, this.FlagRect.Y + 130, 40, 40);
                SpriteBatch spriteBatch1 = base.ScreenManager.SpriteBatch;
                KeyValuePair<string, Texture2D> keyValuePair = ResourceManager.FlagTextures[this.ship.loyalty.data.Traits.FlagIndex];
                spriteBatch1.Draw(keyValuePair.Value, FlagShip, this.ship.loyalty.EmpireColor);
            }

			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
            //Added by McShooterz: new experience level display
            Rectangle star = new Rectangle(this.TroopRect.X, this.TroopRect.Y + 23, 22, 22);
            Vector2 levelPos = new Vector2((float)(star.X + star.Width + 2), (float)(star.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_experience_shipUI"], star, Color.White);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.ship.Level.ToString(), levelPos, Color.White);
            if (HelperFunctions.CheckIntersection(star, MousePos))
            {
                ToolTip.CreateTooltip(161, this.ScreenManager);
            }
            //Added by McShooterz: kills display
            star = new Rectangle(star.X, star.Y + 19, 22, 22);
            levelPos = new Vector2((float)(star.X + star.Width + 2), (float)(star.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
            this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_kills_shipUI"], star, Color.White);
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.ship.kills.ToString(), levelPos, Color.White);
			Vector2 StatusArea = new Vector2((float)(this.Housing.X + 175), (float)(this.Housing.Y + 15));
			int numStatus = 0;
			if (this.ship.loyalty.data.Traits.Pack)
			{
				Rectangle PackRect = new Rectangle((int)StatusArea.X, (int)StatusArea.Y, 48, 32);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["StatusIcons/icon_pack"], PackRect, Color.White);
				Vector2 TextPos = new Vector2((float)(PackRect.X + 26), (float)(PackRect.Y + 15));
				float damageModifier = this.ship.DamageModifier * 100f;
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(damageModifier.ToString("0"), "%"), TextPos, Color.White);
				numStatus++;
				if (HelperFunctions.CheckIntersection(PackRect, MousePos))
				{
					ToolTip.CreateTooltip(Localizer.Token(2245), this.ScreenManager);
				}
			}
			foreach (KeyValuePair<string, float> entry in this.ship.GetCargo())
			{
				if (entry.Value <= 0f)
				{
					continue;
				}
				Rectangle GoodRect = new Rectangle((int)StatusArea.X + numStatus * 53, (int)StatusArea.Y, 32, 32);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Goods/", entry.Key)], GoodRect, Color.White);
				Vector2 TextPos = new Vector2((float)(GoodRect.X + 32), (float)(GoodRect.Y + 16 - Fonts.Arial12.LineSpacing / 2));
				SpriteBatch spriteBatch1 = this.ScreenManager.SpriteBatch;
				SpriteFont arial12 = Fonts.Arial12;
				float item = this.ship.GetCargo()[entry.Key];
                if (entry.Key == "Colonists_1000")
                {
                    item = this.ship.GetCargo()[entry.Key] * this.ship.loyalty.data.Traits.PassengerModifier;
                }
				spriteBatch1.DrawString(arial12, item.ToString("0"), TextPos, Color.White);
				if (HelperFunctions.CheckIntersection(GoodRect, MousePos))
				{
					ToolTip.CreateTooltip(string.Concat(ResourceManager.GoodsDict[entry.Key].Name, "\n\n", ResourceManager.GoodsDict[entry.Key].Description), this.ScreenManager);
				}
				numStatus++;
			}
            if(this.ship.GetFTLmodifier <1 && !this.ship.Inhibited)
            {
                //if (this.ship.GetSystem() != null)
                //{
					Rectangle FoodRect = new Rectangle((int)StatusArea.X + numStatus * 53, (int)StatusArea.Y, 48, 32);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["StatusIcons/icon_boosted"], FoodRect, Color.PaleVioletRed);
					if (HelperFunctions.CheckIntersection(FoodRect, MousePos))
					{
                        string EState = this.ship.engineState == Ship.MoveState.Warp ? "FTL" : "Sublight";
                        ToolTip.CreateTooltip(string.Concat(Localizer.Token(6179), String.Format("{0:P0}", 1f - this.ship.GetFTLmodifier), "\n\nEngine State: ", EState), this.ScreenManager);
					}
					numStatus++;
                //}

            }
            if (this.ship.GetFTLmodifier > 1 && !this.ship.Inhibited && this.ship.engineState == Ship.MoveState.Warp)
            {
                //if (this.ship.inborders)
                //{
                    Rectangle FoodRect = new Rectangle((int)StatusArea.X + numStatus * 53, (int)StatusArea.Y, 48, 32);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["StatusIcons/icon_boosted"], FoodRect, Color.LightGreen);
                    if (HelperFunctions.CheckIntersection(FoodRect, MousePos))
                    {

                        ToolTip.CreateTooltip(string.Concat(Localizer.Token(6180), String.Format("{0:P0}", this.ship.GetFTLmodifier - 1f), "\n\nEngine State: FTL"), this.ScreenManager);
                    }
                    numStatus++;
                //}

            }

			if (this.ship.Inhibited )
			{
				bool Planet = false;
				if (this.screen.GravityWells && this.ship.System!= null)
				{
					foreach (Ship_Game.Planet p in this.ship.System.PlanetList)
					{
                        if (Vector2.Distance(p.Position, this.ship.Position) >= (GlobalStats.GravityWellRange * (1 + ((Math.Log(p.scale)) / 1.5))))
						{
							continue;
						}
						Planet = true;
					}
				}
				if (Planet)
				{
					Rectangle FoodRect = new Rectangle((int)StatusArea.X + numStatus * 53, (int)StatusArea.Y, 48, 32);
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["StatusIcons/icon_gravwell"], FoodRect, Color.White);
					if (HelperFunctions.CheckIntersection(FoodRect, MousePos))
					{
						ToolTip.CreateTooltip(Localizer.Token(2287), this.ScreenManager);
					}
					numStatus++;
				}

				else if (RandomEventManager.ActiveEvent == null || !RandomEventManager.ActiveEvent.InhibitWarp)
				{
					Rectangle FoodRect = new Rectangle((int)StatusArea.X + numStatus * 53, (int)StatusArea.Y, 48, 32);
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["StatusIcons/icon_inhibited"], FoodRect, Color.White);
					if (HelperFunctions.CheckIntersection(FoodRect, MousePos))
					{
						ToolTip.CreateTooltip(117, this.ScreenManager);
					}
					numStatus++;
				}
				else
				{
					Rectangle FoodRect = new Rectangle((int)StatusArea.X + numStatus * 53, (int)StatusArea.Y, 48, 32);
					this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["StatusIcons/icon_flux"], FoodRect, Color.White);
					if (HelperFunctions.CheckIntersection(FoodRect, MousePos))
					{
						ToolTip.CreateTooltip(Localizer.Token(2285), this.ScreenManager);
					}
					numStatus++;
				}
			}
			if (this.ship.disabled)
			{
				Rectangle FoodRect = new Rectangle((int)StatusArea.X + numStatus * 53, (int)StatusArea.Y, 48, 32);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["StatusIcons/icon_disabled"], FoodRect, Color.White);
				if (HelperFunctions.CheckIntersection(FoodRect, MousePos))
				{
					ToolTip.CreateTooltip(116, this.ScreenManager);
				}
				numStatus++;
			}
		}

        public override bool HandleInput(InputState input)
        {
            if (this.screen.SelectedShip == null) return false;  //fbedard
            
            if (this.sliding_element.HandleInput(input))
            {
                if (this.sliding_element.Open)
                    this.State = UIElement.ElementState.TransitionOn;
                else
                    this.State = UIElement.ElementState.TransitionOff;
                return true;
            }
           
            else
            {
                if (HelperFunctions.CheckIntersection(this.ShipNameArea.ClickableArea, input.CursorPosition))
                {
                    this.ShipNameArea.Hover = true;
                    if (input.InGameSelect && this.CanRename)
                        this.ShipNameArea.HandlingInput = true;
                }
                else
                    this.ShipNameArea.Hover = false;
                if (this.ShipNameArea.HandlingInput)
                {
                    GlobalStats.TakingInput = true;
                    this.ShipNameArea.HandleTextInput(ref this.ship.VanityName, input);
                    this.ShipNameArea.Text = this.ship.VanityName;
                }
                else
                    GlobalStats.TakingInput = false;
                if (HelperFunctions.CheckIntersection(this.gridbutton.r, input.CursorPosition))
                    ToolTip.CreateTooltip(Localizer.Token(2204), this.ScreenManager);
                if (this.gridbutton.HandleInput(input))
                {
                    AudioManager.PlayCue("sd_ui_accept_alt3");
                    this.ShowModules = !this.ShowModules;
                    this.gridbutton.Active = this.ShowModules;
                    return true;
                }
                else
                {
                    if (this.ship == null)
                        return false;
                    if (this.DoubleClickTimer > 0)
                        this.DoubleClickTimer -= 0.01666f;
                    if (HelperFunctions.CheckIntersection(this.ShipInfoRect, input.CursorPosition) && input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released && this.DoubleClickTimer > 0)
                    {
                        Empire.Universe.ViewingShip = false;
                        Empire.Universe.AdjustCamTimer = 0.5f;
                        Empire.Universe.transitionDestination.X = this.ship.Center.X;
                        Empire.Universe.transitionDestination.Y = this.ship.Center.Y;
                        if (Empire.Universe.viewState < UniverseScreen.UnivScreenState.SystemView)
                            Empire.Universe.transitionDestination.Z = Empire.Universe.GetZfromScreenState(UniverseScreen.UnivScreenState.SystemView);
                    }
                    else if (HelperFunctions.CheckIntersection(this.ElementRect, input.CursorPosition) && input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
                        this.DoubleClickTimer = 0.25f;    
                    if (this.ship.loyalty == EmpireManager.Player && !this.ship.isConstructor)
                    {
                        foreach (ToggleButton toggleButton in this.CombatStatusButtons)
                        {
                            if (HelperFunctions.CheckIntersection(toggleButton.r, input.CursorPosition))
                            {
                                toggleButton.Hover = true;
                                if (toggleButton.HasToolTip)
                                    ToolTip.CreateTooltip(toggleButton.WhichToolTip, this.ScreenManager);
                                if (input.InGameSelect)
                                {
                                    AudioManager.PlayCue("sd_ui_accept_alt3");
                                    switch (toggleButton.Action)
                                    {
                                        case "attack":
                                            this.ship.AI.CombatState = CombatState.AttackRuns;
                                            break;
                                        case "arty":
                                            this.ship.AI.CombatState = CombatState.Artillery;
                                            break;
                                        case "hold":
                                            this.ship.AI.CombatState = CombatState.HoldPosition;
                                            this.ship.AI.OrderAllStop();
                                            break;
                                        case "orbit_left":
                                            this.ship.AI.CombatState = CombatState.OrbitLeft;
                                            break;
                                        case "broadside_left":
                                            this.ship.AI.CombatState = CombatState.BroadsideLeft;
                                            break;
                                        case "orbit_right":
                                            this.ship.AI.CombatState = CombatState.OrbitRight;
                                            break;
                                        case "broadside_right":
                                            this.ship.AI.CombatState = CombatState.BroadsideRight;
                                            break;
                                        case "evade":
                                            this.ship.AI.CombatState = CombatState.Evade;
                                            break;
                                        case "short":
                                            this.ship.AI.CombatState = CombatState.ShortRange;
                                            break;
                                    }
                                    if (toggleButton.Action != "hold" && this.ship.AI.State == AIState.HoldPosition)
                                        this.ship.AI.State = AIState.AwaitingOrders;
                                }
                            }
                            else
                                toggleButton.Hover = false;
                            switch (toggleButton.Action)
                            {
                                case "attack":
                                    toggleButton.Active = this.ship.AI.CombatState == CombatState.AttackRuns;
                                    continue;
                                case "arty":
                                    toggleButton.Active = this.ship.AI.CombatState == CombatState.Artillery;
                                    continue;
                                case "hold":
                                    toggleButton.Active = this.ship.AI.CombatState == CombatState.HoldPosition;
                                    continue;
                                case "orbit_left":
                                    toggleButton.Active = this.ship.AI.CombatState == CombatState.OrbitLeft;
                                    continue;
                                case "broadside_left":
                                    toggleButton.Active = this.ship.AI.CombatState == CombatState.BroadsideLeft;
                                    continue;
                                case "orbit_right":
                                    toggleButton.Active = this.ship.AI.CombatState == CombatState.OrbitRight;
                                    continue;
                                case "broadside_right":
                                    toggleButton.Active = this.ship.AI.CombatState == CombatState.BroadsideRight;
                                    continue;
                                case "evade":
                                    toggleButton.Active = this.ship.AI.CombatState == CombatState.Evade;
                                    continue;
                                case "short":
                                    toggleButton.Active = this.ship.AI.CombatState == CombatState.ShortRange;
                                    continue;
                                default:
                                    continue;
                            }
                        }
                    }
                    foreach (ShipInfoUIElement.TippedItem tippedItem in this.ToolTipItems)
                    {
                        if (HelperFunctions.CheckIntersection(tippedItem.r, input.CursorPosition))
                            ToolTip.CreateTooltip(tippedItem.TIP_ID, this.ScreenManager);
                    }
                    if (HelperFunctions.CheckIntersection(this.ElementRect, input.CursorPosition))
                        return true;
                    if (this.State == UIElement.ElementState.Open)
                    {
                        bool flag = false;
                        foreach (OrdersButton ordersButton in this.Orders)
                        {
                            if (ordersButton.HandleInput(input, this.ScreenManager))
                            {
                                flag = true;
                                return true;
                            }
                        }
                        if (flag)
                            return true;
                    }
                
                    
                    return false;
                }
            }
        }

		public void SetShip(Ship s)
		{
			if (s.loyalty == EmpireManager.Player)
			{
				this.CanRename = true;
			}
			else
			{
				this.CanRename = false;
			}
			this.ShipNameArea.HandlingInput = false;
			this.ShipNameArea.Text = s.VanityName;
			this.Orders.Clear();
			this.ship = s;
			if (this.ship.loyalty != EmpireManager.Player)
			{
				return;
			}
			if (ship.AI.OrderQueue.NotEmpty)
			{
				try
				{
					if (ship.AI.OrderQueue.PeekLast.Plan == ArtificialIntelligence.Plan.DeployStructure)
					{
						return;
					}
				}
				catch
				{
					return;
				}
			}
            if (this.ship.shipData.Role > ShipData.RoleName.station)
            {
                OrdersButton resupply = new OrdersButton(this.ship, Vector2.Zero, OrderType.OrderResupply, 149)
                {
                    ValueToModify = new Ref<bool>(() => this.ship.DoingResupply, (bool x) => this.ship.DoingResupply = x)
                };
                this.Orders.Add(resupply);
            }
            if (this.ship.shipData.Role != ShipData.RoleName.troop && this.ship.AI.State != AIState.Colonize && this.ship.shipData.Role != ShipData.RoleName.station && ship.Mothership == null)
			{
				OrdersButton ao = new OrdersButton(this.ship, Vector2.Zero, OrderType.DefineAO, 15)
				{
					ValueToModify = new Ref<bool>(() => this.screen.DefiningAO, (bool x) => {
						this.screen.DefiningAO = x;
						this.screen.AORect = new Rectangle(0, 0, 0, 0);
					})
				};
                this.Orders.Add(ao);
            }
            if (this.ship.CargoSpace_Max > 0f && this.ship.shipData.Role != ShipData.RoleName.troop && this.ship.AI.State != AIState.Colonize && this.ship.shipData.Role != ShipData.RoleName.station && ship.Mothership == null)
			{
				OrdersButton tf = new OrdersButton(this.ship, Vector2.Zero, OrderType.TradeFood, 16)
				{
					ValueToModify = new Ref<bool>(() => this.ship.DoingTransport, (bool x) => this.ship.DoingTransport = x),
					RightClickValueToModify = new Ref<bool>(() => this.ship.TransportingFood, (bool x) => this.ship.TransportingFood = x)
				};
				this.Orders.Add(tf);
				OrdersButton tp = new OrdersButton(this.ship, Vector2.Zero, OrderType.TradeProduction, 17)
				{
					ValueToModify = new Ref<bool>(() => this.ship.DoingTransport, (bool x) => this.ship.DoingTransport = x),
					RightClickValueToModify = new Ref<bool>(() => this.ship.TransportingProduction, (bool x) => this.ship.TransportingProduction = x)
				};
				this.Orders.Add(tp);
				OrdersButton tpass = new OrdersButton(this.ship, Vector2.Zero, OrderType.PassTran, 137)
				{
					ValueToModify = new Ref<bool>(() => this.ship.DoingPassTransport, (bool x) => this.ship.DoingPassTransport = x)
				};
				this.Orders.Add(tpass);
			}
			if (this.ship.shield_max > 0f)
			{
				OrdersButton ob = new OrdersButton(this.ship, Vector2.Zero, OrderType.ShieldToggle, 18)
				{
					ValueToModify = new Ref<bool>(() => this.ship.ShieldsUp, (bool x) => this.ship.ShieldsUp = x)
				};
				this.Orders.Add(ob);
			}
            if (this.ship.GetHangars().Count > 0 && ship.Mothership == null)
			{
				bool hasTroops = false;
				bool hasFighters = false;
				foreach (ShipModule hangar in this.ship.GetHangars())
				{
					if (hangar.TroopCapacity != 0 || hangar.IsSupplyBay)
					{
						if (!hangar.IsTroopBay)
						{
							continue;
						}
						hasTroops = true;
					}
					else
					{
						hasFighters = true;
					}
				}
				if (hasFighters)
				{
					OrdersButton ob = new OrdersButton(this.ship, Vector2.Zero, OrderType.FighterToggle, 19)
					{
                        ValueToModify = new Ref<bool>(() => this.ship.FightersOut, (bool x) =>
                        {
							this.ship.FightersOut = x;
                            /// !this.ship.ManualHangarOverride;
						})
					};
					this.Orders.Add(ob);
               
				}
				if (hasTroops)
				{
					OrdersButton ob = new OrdersButton(this.ship, Vector2.Zero, OrderType.TroopToggle, 225)
					{
						ValueToModify = new Ref<bool>(() => this.ship.TroopsOut, (bool x) => {
							this.ship.TroopsOut = x;
							//this.ship.ManualHangarOverride = true;
						})
					};
					this.Orders.Add(ob);
				}
                //if (this.ship.shipData.Role != ShipData.RoleName.station)
                {
                    OrdersButton ob2 = new OrdersButton(this.ship, Vector2.Zero, OrderType.FighterRecall, 146)
                    {
                        ValueToModify = new Ref<bool>(() => this.ship.RecallFightersBeforeFTL, (bool x) =>
                        {
                            this.ship.RecallFightersBeforeFTL = x;
                            this.ship.ManualHangarOverride = !x;
                        }
                            )
                    };
                    this.Orders.Add(ob2);
                }
			}
            if (this.ship.shipData.Role >= ShipData.RoleName.fighter && ship.Mothership == null && this.ship.AI.State != AIState.Colonize && ship.shipData.ShipCategory != ShipData.Category.Civilian)
            {
			    OrdersButton exp = new OrdersButton(this.ship, Vector2.Zero, OrderType.Explore, 136)
			    {
				    ValueToModify = new Ref<bool>(() => this.ship.DoingExplore, (bool x) => this.ship.DoingExplore = x)
			    };
			    this.Orders.Add(exp);
			    OrdersButton SystemDefense = new OrdersButton(this.ship, Vector2.Zero, OrderType.EmpireDefense, 150)
			    {
				    ValueToModify = new Ref<bool>(() => this.ship.DoingSystemDefense, (bool x) => this.ship.DoingSystemDefense = x),
				    Active = false
			    };
			    this.Orders.Add(SystemDefense);
            }
            if (ship.Mothership == null)
            {
                OrdersButton rf = new OrdersButton(this.ship, Vector2.Zero, OrderType.Refit, 158)
                {
                    ValueToModify = new Ref<bool>(() => this.ship.doingRefit, (bool x) => this.ship.doingRefit = x),
                    Active = false
                };
                this.Orders.Add(rf);
                //Added by McShooterz: scrap order
                OrdersButton sc = new OrdersButton(this.ship, Vector2.Zero, OrderType.Scrap, 157)
                {
                    ValueToModify = new Ref<bool>(() => this.ship.doingScrap, (bool x) => this.ship.doingScrap = x),
                    Active = false
                };
                this.Orders.Add(sc);
            }

			int ex = 0;
			int y = 0;
			for (int i = 0; i < this.Orders.Count; i++)
			{
				OrdersButton ob = this.Orders[i];
				if (i % 2 == 0 && i > 0)
				{
					ex++;
					y = 0;
				}
				ob.clickRect.X = this.ElementRect.X + this.ElementRect.Width + 2 + 52 * ex;
				ob.clickRect.Y = this.sliding_element.Housing.Y + 15 + y * 52;
				y++;
			}
		}

		private struct TippedItem
		{
			public Rectangle r;

			public int TIP_ID;
		}
	}
}