using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class ShipListInfoUIElement : UIElement
    {
        public readonly UniverseScreen Screen;
        public Array<ToggleButton> CombatStatusButtons = new Array<ToggleButton>();
        readonly Array<TippedItem> ToolTipItems = new Array<TippedItem>();
        Array<Ship> ShipList = new Array<Ship>();
        readonly Selector Selector;
        public Rectangle LeftRect;
        public Rectangle RightRect;
        public Rectangle ShipInfoRect;
        readonly ScrollList<SelectedShipListItem> SelectedShipsSL;
        public Rectangle Power;
        public Rectangle Shields;
        public Rectangle Ordnance;
        ProgressBar oBar;
        public ToggleButton GridButton;
        readonly Rectangle Housing;
        readonly SlidingElement SlidingElement;
        public Array<OrdersButton> Orders = new Array<OrdersButton>();
        readonly Rectangle DefenseRect;
        readonly Rectangle TroopRect;
        bool IsFleet;
        bool AllShipsMine = true;
        bool ShowModules = true;
        public Ship HoveredShip;
        public Ship HoveredShipLast;
        float HoverOff;

        public ShipListInfoUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen)
        {
            Housing = r;
            this.Screen = screen;
            ScreenManager = sm;
            ElementRect = r;
            Selector = new Selector(r, Color.Black);
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            var sliderRect = new Rectangle(r.X - 100, r.Y + r.Height - 140, 530, 130);
            LeftRect = new Rectangle(r.X, r.Y + 44, 180, r.Height - 44);
            SlidingElement = new SlidingElement(sliderRect);
            RightRect = new Rectangle(LeftRect.X + LeftRect.Width, LeftRect.Y, 220, LeftRect.Height);
            float spacing = LeftRect.Height - 26 - 96;
            Power = new Rectangle(RightRect.X, LeftRect.Y + 12, 20, 20);
            Shields = new Rectangle(RightRect.X, LeftRect.Y + 12 + 20 + (int)spacing, 20, 20);
            DefenseRect = new Rectangle(Housing.X + 13, Housing.Y + 112, 22, 22);
            TroopRect = new Rectangle(Housing.X + 13, Housing.Y + 137, 22, 22);

            var gridPos = new Vector2(Housing.X + 16f, Screen.Height - 45f);
            GridButton = new ToggleButton(gridPos, ToggleButtonStyle.Grid, "SelectionBox/icon_grid")
            {
                Pressed = true
            };
            ShipInfoRect = new Rectangle(Housing.X + 60, Housing.Y + 110, 115, 115);

            const float orderSize = 29f;
            float ordersStartX = Power.X - 3f;
            var ordersBarPos = new Vector2(ordersStartX, Screen.Height - 45f);

            void AddOrdersBarButton(CombatState state, string icon, int toolTip)
            {
                var button = new ToggleButton(ordersBarPos, ToggleButtonStyle.Formation, icon);
                CombatStatusButtons.Add(button);
                button.CombatState = state;
                button.Tooltip = toolTip;
                button.OnClick = (b) => OnCombatStatusButtonClicked(state);
                ordersBarPos.X += orderSize;
            }

            AddOrdersBarButton(CombatState.AttackRuns, "SelectionBox/icon_formation_headon", toolTip: 1);
            AddOrdersBarButton(CombatState.ShortRange, "SelectionBox/icon_grid", toolTip: 228);
            AddOrdersBarButton(CombatState.Artillery, "SelectionBox/icon_formation_aft", toolTip: 2);
            AddOrdersBarButton(CombatState.HoldPosition, "SelectionBox/icon_formation_x", toolTip: 65);
            AddOrdersBarButton(CombatState.OrbitLeft, "SelectionBox/icon_formation_left", toolTip: 3);
            AddOrdersBarButton(CombatState.OrbitRight, "SelectionBox/icon_formation_right", toolTip: 4);
            AddOrdersBarButton(CombatState.Evade, "SelectionBox/icon_formation_stop", toolTip: 6);

            ordersBarPos = new Vector2(ordersStartX + orderSize * 4, ordersBarPos.Y - orderSize);
            AddOrdersBarButton(CombatState.BroadsideLeft, "SelectionBox/icon_formation_bleft", toolTip: 159);
            AddOrdersBarButton(CombatState.BroadsideRight, "SelectionBox/icon_formation_bright", toolTip: 160);

            var slsubRect = new Rectangle(RightRect.X, Housing.Y + 110 - 35, RightRect.Width - 5, 140);
            SelectedShipsSL = new ScrollList<SelectedShipListItem>(new Submenu(slsubRect), 24);
        }

        void OnCombatStatusButtonClicked(CombatState state)
        {
            foreach(Ship ship in ShipList)
                ship.AI.CombatState = state;
        }

        public void ClearShipList()
        {
            ShipList.Clear();
            SelectedShipsSL.Reset();
        }

        public override void Draw(GameTime gameTime)
        {
            if (Screen.SelectedShipList == null)
                return;  //fbedard

            SpriteBatch batch = ScreenManager.SpriteBatch;

            float transitionOffset = MathHelper.SmoothStep(0f, 1f, TransitionPosition);
            int columns = Orders.Count / 2 + Orders.Count % 2;
            if (AllShipsMine)
            {
                SlidingElement.Draw(ScreenManager, (int)(columns * 55 * (1f - TransitionPosition)) + (SlidingElement.Open ? 20 - columns : 0));
                foreach (OrdersButton ob in Orders)
                {
                    Rectangle r = ob.ClickRect;
                    r.X = r.X - (int)(transitionOffset * 300f);
                    ob.Draw(ScreenManager, r);
                }
            }
            batch.Draw(ResourceManager.Texture("SelectionBox/unitselmenu_main"), Housing, Color.White);
            var namePos = new Vector2(Housing.X + 41, Housing.Y + 64);
            SelectedShipsSL.Draw(batch);

            string text;
            if (HoveredShip == null)
            {
                HoverOff += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (HoverOff > 0.5f)
                {
                    text = (!IsFleet || ShipList.Count <= 0 || ShipList.First.fleet == null ? "Multiple Ships" : ShipList.First.fleet.Name);
                    batch.DrawString(Fonts.Arial20Bold, text, namePos, tColor);
                    float fleetOrdnance = 0f;
                    float fleetOrdnanceMax = 0f;
                    foreach (Ship ship in ShipList)
                    {
                        fleetOrdnance = fleetOrdnance + ship.Ordinance;
                        fleetOrdnanceMax = fleetOrdnanceMax + ship.OrdinanceMax;
                    }
                    if (fleetOrdnanceMax > 0f)
                    {
                        var pordrect = new Rectangle(45, Housing.Y + 115, 130, 18);
                        oBar = new ProgressBar(pordrect)
                        {
                            Max = fleetOrdnanceMax,
                            Progress = fleetOrdnance,
                            color = "brown"
                        };
                        oBar.Draw(batch);
                        Ordnance = new Rectangle(pordrect.X - 25, pordrect.Y, 20, 20);
                        batch.Draw(ResourceManager.Texture("Modules/Ordnance"), Ordnance, Color.White);
                    }
                }
            }
            else
            {
                HoverOff = 0f;
                HoveredShip.RenderOverlay(batch, ShipInfoRect, ShowModules);
                text = HoveredShip.VanityName;
                Vector2 tpos = new Vector2(Housing.X + 30, Housing.Y + 63);
                string name = (!string.IsNullOrEmpty(HoveredShip.VanityName) ? HoveredShip.VanityName : HoveredShip.Name);
                SpriteFont TitleFont = Fonts.Arial14Bold;
                Vector2 ShipSuperName = new Vector2(Housing.X + 30, Housing.Y + 79);
                if (Fonts.Arial14Bold.MeasureString(name).X > 180f)
                {
                    TitleFont = Fonts.Arial12Bold;
                    tpos.Y = tpos.Y + 1;
                    tpos.X = tpos.X - 8;
                }
                batch.DrawString(TitleFont, (!string.IsNullOrEmpty(HoveredShip.VanityName) ? HoveredShip.VanityName : HoveredShip.Name), tpos, tColor);
                //Added by Doctor, adds McShooterz' class/hull data to the rollover in the list too:
                //this.batch.DrawString(Fonts.Visitor10, string.Concat(this.HoveredShip.Name, " - ", Localizer.GetRole(this.HoveredShip.shipData.Role, this.HoveredShip.loyalty)), ShipSuperName, Color.Orange);
                string longName = string.Concat(HoveredShip.Name, " - ", ShipData.GetRole(HoveredShip.DesignRole));
                if (HoveredShip.shipData.ShipCategory != ShipData.Category.Unclassified)
                    longName += " - "+HoveredShip.shipData.GetCategory();
                batch.DrawString(Fonts.Visitor10, longName, ShipSuperName, Color.Orange);
                batch.Draw(ResourceManager.Texture("UI/icon_shield"), DefenseRect, Color.White);
                Vector2 defPos = new Vector2(DefenseRect.X + DefenseRect.Width + 2, DefenseRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
                SpriteBatch spriteBatch = batch;
                SpriteFont arial12Bold = Fonts.Arial12Bold;
                float mechanicalBoardingDefense = HoveredShip.MechanicalBoardingDefense + HoveredShip.TroopBoardingDefense;
                spriteBatch.DrawString(arial12Bold, mechanicalBoardingDefense.String(), defPos, Color.White);
                text = Fonts.Arial10.ParseText(ShipListScreenItem.GetStatusText(HoveredShip), 155f);
                Vector2 ShipStatus = new Vector2(Selector.Rect.X + Selector.Rect.Width - 170, Housing.Y + 68);
                text = Fonts.Arial10.ParseText(ShipListScreenItem.GetStatusText(HoveredShip), 155f);
                HelperFunctions.ClampVectorToInt(ref ShipStatus);
                batch.DrawString(Fonts.Arial10, text, ShipStatus, tColor);
                ShipStatus.Y = ShipStatus.Y + Fonts.Arial12Bold.MeasureString(text).Y;
                batch.Draw(ResourceManager.Texture("UI/icon_troop_shipUI"), TroopRect, Color.White);
                Vector2 troopPos = new Vector2(TroopRect.X + TroopRect.Width + 2, TroopRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
                batch.DrawString(Fonts.Arial12Bold, string.Concat(HoveredShip.TroopList.Count, "/", HoveredShip.TroopCapacity), troopPos, Color.White);
                Rectangle star = new Rectangle(TroopRect.X, TroopRect.Y + 25, 22, 22);
                Vector2 levelPos = new Vector2(star.X + star.Width + 2, star.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
                batch.Draw(ResourceManager.Texture("UI/icon_experience_shipUI"), star, Color.White);
                batch.DrawString(Fonts.Arial12Bold, HoveredShip.Level.ToString(), levelPos, Color.White);
            }
            foreach (ToggleButton button in CombatStatusButtons)
            {
                button.Draw(ScreenManager);
            }

            GridButton.Draw(ScreenManager);
        }

        public override bool HandleInput(InputState input)
        {
            if (Screen.SelectedShipList == null)
                return false;  // fbedard

            foreach (SelectedShipListItem ship in SelectedShipsSL.AllEntries)
            {
                if (!ship.AllButtonsActive)
                {
                    SetShipList(ShipList, IsFleet);
                    break;
                }
            }

            if (Screen.SelectedShipList.Count == 0 || Screen.SelectedShipList.Count == 1)
                return false;
            if (ShipList == null || ShipList.Count == 0)
                return false;

            if (GridButton.HandleInput(input))
            {
                GameAudio.AcceptClick();
                ShowModules = !ShowModules;
                GridButton.Pressed = ShowModules;
                return true;
            }

            if (AllShipsMine)
            {
                foreach (ToggleButton button in CombatStatusButtons)
                {
                    button.Pressed = ShipList.All(ship => ship.AI.CombatState == button.CombatState);
                }

                foreach (ToggleButton button in CombatStatusButtons)
                    if (button.HandleInput(input))
                        return true;
                
                if (SlidingElement.HandleInput(input))
                {
                    State = !SlidingElement.Open ? ElementState.TransitionOff : ElementState.TransitionOn;
                    return true;
                }
                
                if (State == ElementState.Open)
                {
                    bool orderHover = false;
                    foreach (OrdersButton ob in Orders)
                    {
                        if (!ob.HandleInput(input, ScreenManager))
                        {
                            continue;
                        }
                        orderHover = true;
                    }
                    if (orderHover)
                    {
                        //this.screen.SelectedFleet.Ships.thisLock.EnterReadLock();      //Enter and Exit lock removed to stop crash -Gretman
                        if (Screen.SelectedFleet != null && Screen.SelectedFleet.Ships.Count >0 && Screen.SelectedFleet.Ships[0] != null)
                        {
                            bool flag = true;                            
                            foreach (Ship ship2 in Screen.SelectedFleet.Ships)
                                if (ship2.AI.State != AIState.Resupply)
                                    flag = false;
                            
                            if (flag)
                                Screen.SelectedFleet.Position = Screen.SelectedFleet.Ships[0].AI.OrbitTarget.Center;  //fbedard: center fleet on resupply planet
                            
                        }
                        //this.screen.SelectedFleet.Ships.thisLock.ExitReadLock();
                        return true;
                    }                  
                }
            }

            HoveredShipLast = HoveredShip;
            HoveredShip = null;

            if (SelectedShipsSL.HandleInput(input))
                return true;

            foreach (TippedItem ti in ToolTipItems)
            {
                if (ti.r.HitTest(input.CursorPosition))
                    ToolTip.CreateTooltip(ti.TIP_ID);
            }

            if (ElementRect.HitTest(input.CursorPosition))
                return true;
            if (SlidingElement.ButtonHousing.HitTest(input.CursorPosition))
                return true;
            return false;
        }

        void OnSelectedShipsListButtonClicked(SkinnableButton button)
        {
            // added by gremlin filter by selected ship in shiplist.
            if (Screen.Input.IsKeyDown(Keys.LeftShift))
            {
                foreach (Ship filter in Screen.SelectedShipList)
                {
                    if (filter.shipData.Role != HoveredShip.shipData.Role)
                        Screen.SelectedShipList.QueuePendingRemoval(filter);
                }

                Screen.SelectedShipList.ApplyPendingRemovals();
                SetShipList(Screen.SelectedShipList, false);
            }
            else
            {
                Screen.SelectedFleet = null;
                Screen.SelectedShipList.Clear();
                Screen.SelectedShip = HoveredShip; //fbedard: multi-select
                Screen.ShipInfoUIElement.SetShip(HoveredShip);
            }
        }

        public void SetShipList(Array<Ship> shipList, bool isFleet)
        {
            Orders.Clear();
            IsFleet  = isFleet;
            ShipList = shipList;
            SelectedShipsSL.Reset();
            AllShipsMine = true;
            bool allResupply    = true;
            bool allFreighters  = true;
            bool allCombat      = true;
            bool carriersHere   = false;
            bool troopShipsHere = false;
            var entry = new SelectedShipListItem(this, OnSelectedShipsListButtonClicked);
            for (int i = 0; i < shipList.Count; i++)
            {
                Ship ship = shipList[i];
                var button = new SkinnableButton(new Rectangle(0, 0, 20, 20), ship.GetTacticalIcon())
                {
                    IsToggle = false,
                    ReferenceObject = ship,
                    BaseColor = ship.loyalty.EmpireColor
                };

                if (entry.ShipButtons.Count < 8)
                    entry.ShipButtons.Add(button);

                if (entry.ShipButtons.Count == 8 || i == shipList.Count - 1)
                {
                    SelectedShipsSL.AddItem(entry);
                    entry = new SelectedShipListItem(this, OnSelectedShipsListButtonClicked);
                }

                if (ship.AI.State != AIState.Resupply)    allResupply    = false;
                if (ship.loyalty != EmpireManager.Player) AllShipsMine   = false;
                if (!ship.IsFreighter)                    allFreighters  = false;
                if (ship.Carrier.HasFighterBays)          carriersHere   = true;
                if (ship.Carrier.HasTroopBays)            troopShipsHere = true;
                if (ship.DesignRole < ShipData.RoleName.carrier || ship.shipData.ShipCategory == ShipData.Category.Civilian 
                                                                || ship.AI.State == AIState.Colonize 
                                                                || ship.Mothership != null)
                {
                    allCombat = false;
                }

            }

            OrdersButton resupply = new OrdersButton(shipList, Vector2.Zero, OrderType.OrderResupply, 149)
            {
                SimpleToggle = true,
                Active = allResupply
            };
            Orders.Add(resupply);

            if (allCombat)
            {  
                OrdersButton explore = new OrdersButton(shipList, Vector2.Zero, OrderType.Explore, 136)
                {
                    SimpleToggle = true,
                    Active = false
                };
                Orders.Add(explore);
            }

            if (carriersHere)
            {
                OrdersButton launchFighters = new OrdersButton(shipList, Vector2.Zero, OrderType.FighterToggle, 19)
                {
                    SimpleToggle = true,
                    Active = false
                };
                Orders.Add(launchFighters);
                OrdersButton waitForFighters = new OrdersButton(shipList, Vector2.Zero, OrderType.FighterRecall, 146)
                {
                    SimpleToggle = true,
                    Active = true
                };
                Orders.Add(waitForFighters);
            }

            if (troopShipsHere)
            {
                OrdersButton launchTroops = new OrdersButton(shipList, Vector2.Zero, OrderType.TroopToggle, 225)
                {
                    SimpleToggle = true,
                    Active = true
                };
                Orders.Add(launchTroops);

                OrdersButton sendTroops = new OrdersButton(shipList, Vector2.Zero, OrderType.SendTroops, 18)
                {
                    SimpleToggle = true,
                    Active = true
                };
                Orders.Add(sendTroops);

                if (!carriersHere)
                {
                    OrdersButton waitForTroops = new OrdersButton(shipList, Vector2.Zero, OrderType.FighterRecall, 146)
                    {
                        SimpleToggle = true,
                        Active = true
                    };
                    Orders.Add(waitForTroops);
                }
            }

            if (allFreighters)
            {
                OrdersButton tradeFood = new OrdersButton(shipList, Vector2.Zero, OrderType.TradeFood, 16)
                {
                    SimpleToggle = true
                };
                Orders.Add(tradeFood);
                OrdersButton tradeProduction = new OrdersButton(shipList, Vector2.Zero, OrderType.TradeProduction, 16)
                {
                    SimpleToggle = true
                };
                Orders.Add(tradeProduction);
                OrdersButton transportColonists = new OrdersButton(shipList, Vector2.Zero, OrderType.TransportColonists, 152)
                {
                    SimpleToggle = true
                };
                Orders.Add(transportColonists);
                OrdersButton allowInterEmpireTrade = new OrdersButton(shipList, Vector2.Zero, OrderType.AllowInterTrade, 252)
                {
                    SimpleToggle = true
                };
                Orders.Add(allowInterEmpireTrade);
            }

            //Added by McShooterz: fleet scrap button
            OrdersButton scrap = new OrdersButton(shipList, Vector2.Zero, OrderType.Scrap, 157)
            {
                SimpleToggle = true,
                Active = false
            };
            Orders.Add(scrap);

            int ex = 0;
            int y = 0;
            for (int i = 0; i < Orders.Count; i++)
            {
                OrdersButton ob = Orders[i];
                if (i % 2 == 0 && i > 0)
                {
                    ex++;
                    y = 0;
                }
                ob.ClickRect.X = ElementRect.X + ElementRect.Width + 2 + 52 * ex;
                ob.ClickRect.Y = SlidingElement.Housing.Y + 15 + y * 52;
                y++;
            }
        }

        struct TippedItem
        {
            public Rectangle r;

            public int TIP_ID;
        }
    }
}