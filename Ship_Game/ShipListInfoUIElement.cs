using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class ShipListInfoUIElement : UIElement
    {
        public Array<ToggleButton> CombatStatusButtons = new Array<ToggleButton>();

        private Array<TippedItem> ToolTipItems = new Array<TippedItem>();

        private Rectangle SliderRect;

        private Rectangle clickRect;

        private UniverseScreen screen;

        private Array<Ship> ShipList = new Array<Ship>();

        private Selector sel;

        public Rectangle LeftRect;

        public Rectangle RightRect;

        public Rectangle ShipInfoRect;

        private ScrollList SelectedShipsSL;

        public Rectangle Power;

        public Rectangle Shields;

        public Rectangle Ordnance;

        private ProgressBar pBar;

        private ProgressBar sBar;

        private ProgressBar oBar;

        public ToggleButton gridbutton;

        private Rectangle Housing;

        private SlidingElement sliding_element;

        public Array<OrdersButton> Orders = new Array<OrdersButton>();

        private Ship HoveredShip;

        private Rectangle DefenseRect;

        private Rectangle TroopRect;

        private bool isFleet;

        private bool AllShipsMine = true;

        private bool ShowModules = true;

        private Ship HoveredShipLast;
        private float HoverOff;
        private string fmt = "0";

        public ShipListInfoUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen)
        {
            Housing = r;
            this.screen = screen;
            ScreenManager = sm;
            ElementRect = r;
            sel = new Selector(r, Color.Black);
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            SliderRect = new Rectangle(r.X - 100, r.Y + r.Height - 140, 530, 130);
            LeftRect = new Rectangle(r.X, r.Y + 44, 180, r.Height - 44);
            sliding_element = new SlidingElement(SliderRect);
            RightRect = new Rectangle(LeftRect.X + LeftRect.Width, LeftRect.Y, 220, LeftRect.Height);
            float spacing = (float)(LeftRect.Height - 26 - 96);
            Power = new Rectangle(RightRect.X, LeftRect.Y + 12, 20, 20);
            var pbarrect = new Rectangle(Power.X + Power.Width + 15, Power.Y, 150, 18);
            pBar = new ProgressBar(pbarrect)
            {
                color = "green"
            };

            Shields = new Rectangle(RightRect.X, LeftRect.Y + 12 + 20 + (int)spacing, 20, 20);
            var pshieldsrect = new Rectangle(Shields.X + Shields.Width + 15, Shields.Y, 150, 18);
            sBar = new ProgressBar(pshieldsrect)
            {
                color = "blue"
            };
            DefenseRect = new Rectangle(Housing.X + 13, Housing.Y + 112, 22, 22);
            TroopRect = new Rectangle(Housing.X + 13, Housing.Y + 137, 22, 22);

            
            float screenHeight = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;

            var gridPos = new Vector2(Housing.X + 16f, screenHeight - 45f);
            gridbutton = new ToggleButton(gridPos, ToggleButtonStyle.Grid, "SelectionBox/icon_grid")
            {
                Active = true
            };
            clickRect = new Rectangle(ElementRect.X + ElementRect.Width - 16, ElementRect.Y + ElementRect.Height / 2 - 11, 11, 22);
            ShipInfoRect = new Rectangle(Housing.X + 60, Housing.Y + 110, 115, 115);
            

            const float orderSize = 29f;
            float ordersStartX = Power.X - 3f;
            var ordersBarPos = new Vector2(ordersStartX, screenHeight - 45f);

            void AddOrdersBarButton(CombatState state, string icon, int toolTip)
            {
                var button = new ToggleButton(ordersBarPos, ToggleButtonStyle.Formation, icon);			
                CombatStatusButtons.Add(button);
                button.Action = state.ToString();
                button.HasToolTip = true;
                button.WhichToolTip = toolTip;
                ordersBarPos.X += orderSize;
            }

            AddOrdersBarButton(CombatState.AttackRuns,   "SelectionBox/icon_formation_headon", toolTip: 1);
            AddOrdersBarButton(CombatState.ShortRange,   "SelectionBox/icon_grid",            toolTip: 228);
            AddOrdersBarButton(CombatState.Artillery,    "SelectionBox/icon_formation_aft",   toolTip: 2);
            AddOrdersBarButton(CombatState.HoldPosition, "SelectionBox/icon_formation_x",     toolTip: 65);
            AddOrdersBarButton(CombatState.OrbitLeft,    "SelectionBox/icon_formation_left",  toolTip: 3);
            AddOrdersBarButton(CombatState.OrbitRight,   "SelectionBox/icon_formation_right", toolTip: 4);
            AddOrdersBarButton(CombatState.Evade,        "SelectionBox/icon_formation_stop",  toolTip: 6);

            ordersBarPos = new Vector2(ordersStartX + orderSize * 4, ordersBarPos.Y - orderSize);
            AddOrdersBarButton(CombatState.BroadsideLeft, "SelectionBox/icon_formation_bleft", toolTip: 159);
            AddOrdersBarButton(CombatState.BroadsideRight, "SelectionBox/icon_formation_bright", toolTip: 160);

            var slsubRect = new Rectangle(RightRect.X, Housing.Y + 110 - 35, RightRect.Width - 5, 140);
            SelectedShipsSL = new ScrollList(new Submenu(slsubRect), 24);
        }

        public void ClearShipList()
        {
            ShipList.Clear();
            SelectedShipsSL.Reset();
        }

        public override void Draw(GameTime gameTime)
        {
            if (screen.SelectedShipList == null) return;  //fbedard

            float transitionOffset = MathHelper.SmoothStep(0f, 1f, TransitionPosition);
            int columns = Orders.Count / 2 + Orders.Count % 2;
            if (AllShipsMine)
            {
                sliding_element.Draw(ScreenManager, (int)((float)(columns * 55) * (1f - TransitionPosition)) + (sliding_element.Open ? 20 - columns : 0));
                foreach (OrdersButton ob in Orders)
                {
                    Rectangle r = ob.clickRect;
                    r.X = r.X - (int)(transitionOffset * 300f);
                    ob.Draw(ScreenManager, r);
                }
            }
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/unitselmenu_main"), Housing, Color.White);
            string text = (!isFleet || ShipList.Count <= 0 || ShipList.First.fleet == null) ? "Multiple Ships" : ShipList.First.fleet.Name;
            var namePos = new Vector2(Housing.X + 41, Housing.Y + 64);
            SelectedShipsSL.Draw(ScreenManager.SpriteBatch);

            foreach (ScrollList.Entry e in SelectedShipsSL.VisibleEntries)
            {
                var ship = (SelectedShipEntry)e.item;
                ship.Update(new Vector2(RightRect.X, e.Y));
                foreach (SkinnableButton button in ship.ShipButtons)
                {
                    if (HoveredShip == button.ReferenceObject)
                        button.Hover = true;
                    button.Draw(ScreenManager);
                }
            }
            if (HoveredShip == null)
            {
                HoverOff += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (HoverOff > 0.5f)
                {
                    text = (!isFleet || ShipList.Count <= 0 || ShipList.First.fleet == null ? "Multiple Ships" : ShipList.First.fleet.Name);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, text, namePos, tColor);
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
                        oBar.Draw(ScreenManager.SpriteBatch);
                        Ordnance = new Rectangle(pordrect.X - 25, pordrect.Y, 20, 20);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Modules/Ordnance"), Ordnance, Color.White);
                    }
                }
            }
            else
            {
                HoverOff = 0f;
                HoveredShip.RenderOverlay(ScreenManager.SpriteBatch, ShipInfoRect, ShowModules);
                text = HoveredShip.VanityName;
                Vector2 tpos = new Vector2((float)(Housing.X + 30), (float)(Housing.Y + 63));
                string name = (!string.IsNullOrEmpty(HoveredShip.VanityName) ? HoveredShip.VanityName : HoveredShip.Name);
                SpriteFont TitleFont = Fonts.Arial14Bold;
                Vector2 ShipSuperName = new Vector2((float)(Housing.X + 30), (float)(Housing.Y + 79));
                if (Fonts.Arial14Bold.MeasureString(name).X > 180f)
                {
                    TitleFont = Fonts.Arial12Bold;
                    tpos.Y = tpos.Y + 1;
                    tpos.X = tpos.X - 8;
                }
                ScreenManager.SpriteBatch.DrawString(TitleFont, (!string.IsNullOrEmpty(HoveredShip.VanityName) ? HoveredShip.VanityName : HoveredShip.Name), tpos, tColor);
                //Added by Doctor, adds McShooterz' class/hull data to the rollover in the list too:
                //this.ScreenManager.SpriteBatch.DrawString(Fonts.Visitor10, string.Concat(this.HoveredShip.Name, " - ", Localizer.GetRole(this.HoveredShip.shipData.Role, this.HoveredShip.loyalty)), ShipSuperName, Color.Orange);
                string longName = string.Concat(HoveredShip.Name, " - ", ShipData.GetRole(HoveredShip.DesignRole));
                if (HoveredShip.shipData.ShipCategory != ShipData.Category.Unclassified)
                    longName += string.Concat(" - ", HoveredShip.shipData.GetCategory());
                ScreenManager.SpriteBatch.DrawString(Fonts.Visitor10, longName, ShipSuperName, Color.Orange);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_shield"), DefenseRect, Color.White);
                Vector2 defPos = new Vector2((float)(DefenseRect.X + DefenseRect.Width + 2), (float)(DefenseRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
                SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
                SpriteFont arial12Bold = Fonts.Arial12Bold;
                float mechanicalBoardingDefense = HoveredShip.MechanicalBoardingDefense + HoveredShip.TroopBoardingDefense;
                spriteBatch.DrawString(arial12Bold, mechanicalBoardingDefense.ToString(fmt), defPos, Color.White);
                text = HelperFunctions.ParseText(Fonts.Arial10, ShipListScreenEntry.GetStatusText(HoveredShip), 155f);
                Vector2 ShipStatus = new Vector2((float)(sel.Rect.X + sel.Rect.Width - 170), Housing.Y + 68);
                text = HelperFunctions.ParseText(Fonts.Arial10, ShipListScreenEntry.GetStatusText(HoveredShip), 155f);
                HelperFunctions.ClampVectorToInt(ref ShipStatus);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial10, text, ShipStatus, tColor);
                ShipStatus.Y = ShipStatus.Y + Fonts.Arial12Bold.MeasureString(text).Y;
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_troop_shipUI"), TroopRect, Color.White);
                Vector2 troopPos = new Vector2((float)(TroopRect.X + TroopRect.Width + 2), (float)(TroopRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(HoveredShip.TroopList.Count, "/", HoveredShip.TroopCapacity), troopPos, Color.White);
                Rectangle star = new Rectangle(TroopRect.X, TroopRect.Y + 25, 22, 22);
                Vector2 levelPos = new Vector2((float)(star.X + star.Width + 2), (float)(star.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_experience_shipUI"), star, Color.White);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, HoveredShip.Level.ToString(), levelPos, Color.White);
            }
            foreach (ToggleButton button in CombatStatusButtons)
            {
                button.Draw(ScreenManager);
            }
            gridbutton.Draw(ScreenManager);
        }

        public override bool HandleInput(InputState input)
        {
            if (screen.SelectedShipList == null)
                return false;  // fbedard

            foreach (ScrollList.Entry e in SelectedShipsSL.VisibleEntries)
            {
                if (((SelectedShipEntry) e.item).AllButtonsActive)
                    continue;
                SetShipList(ShipList, isFleet);
                break;
            }

            if (screen.SelectedShipList.Count == 0 || screen.SelectedShipList.Count == 1)
                return false;
            if (ShipList == null || ShipList.Count == 0)
                return false;

            if (gridbutton.HandleInput(input))
            {
                GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                ShowModules = !ShowModules;
                gridbutton.Active = ShowModules;
                return true;
            }
            if (AllShipsMine)
            {
                foreach (ToggleButton button in CombatStatusButtons)
                {
                    if (button.HandleInput(input))
                    {
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");

                        CombatState action = (CombatState) Enum.Parse(typeof(CombatState), button.Action);
                        foreach(Ship ship in ShipList)
                            ship.AI.CombatState = action;
                    }
                    else
                    foreach (CombatState combatState in Enum.GetValues(typeof(CombatState)))
                    {
                        if (combatState.ToString() != button.Action)
                            continue;
                        button.Active = AllShipsInState(combatState);
                        if (button.Active)
                            break;
                    }
                }
                
                if (sliding_element.HandleInput(input))
                {
                    if (!sliding_element.Open)
                    {
                        State = ElementState.TransitionOff;
                    }
                    else
                    {
                        State = ElementState.TransitionOn;
                    }
                    return true;
                }
                
                if (State == ElementState.Open)
                {
                    bool orderhover = false;
                    foreach (OrdersButton ob in Orders)
                    {
                        if (!ob.HandleInput(input, ScreenManager))
                        {
                            continue;
                        }
                        orderhover = true;
                    }
                    if (orderhover)
                    {
                        //this.screen.SelectedFleet.Ships.thisLock.EnterReadLock();      //Enter and Exit lock removed to stop crash -Gretman
                        if (screen.SelectedFleet != null && screen.SelectedFleet.Ships.Count >0 && screen.SelectedFleet.Ships[0] != null)
                        {
                            bool flag = true;                            
                            foreach (Ship ship2 in (Array<Ship>)screen.SelectedFleet.Ships)
                                if (ship2.AI.State != AIState.Resupply)
                                    flag = false;
                            
                            if (flag)
                                screen.SelectedFleet.Position = screen.SelectedFleet.Ships[0].AI.OrbitTarget.Center;  //fbedard: center fleet on resupply planet
                            
                        }
                        //this.screen.SelectedFleet.Ships.thisLock.ExitReadLock();
                        return true;
                    }                  
                }
            }

            SelectedShipsSL.HandleInput(input);
            HoveredShipLast = HoveredShip;
            HoveredShip = null;

            foreach (ScrollList.Entry e in SelectedShipsSL.VisibleEntries)
            {
                foreach (SkinnableButton button in ((SelectedShipEntry)e.item).ShipButtons)
                {
                    if (!button.r.HitTest(input.CursorPosition))
                    {
                        button.Hover = false;
                    }
                    else
                    {
                        if (HoveredShipLast != (Ship)button.ReferenceObject)
                        {
                            GameAudio.PlaySfxAsync("sd_ui_mouseover");
                        }
                        button.Hover = true;
                        HoveredShip = (Ship)button.ReferenceObject;
                        if (!input.InGameSelect)
                            continue;

                        // added by gremlin filter by selected ship in shiplist.
                        if (input.KeysCurr.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift))
                        {
                            foreach(Ship filter in screen.SelectedShipList)
                            {
                                if (filter.shipData.Role != HoveredShip.shipData.Role)
                                    screen.SelectedShipList.QueuePendingRemoval(filter);
                            }
                            screen.SelectedShipList.ApplyPendingRemovals();
                            SetShipList(screen.SelectedShipList, false);
                            continue;
                        }
                        else
                        {
                            screen.SelectedFleet = null;
                            screen.SelectedShipList.Clear();
                            screen.SelectedShip = HoveredShip;  //fbedard: multi-select
                            screen.ShipInfoUIElement.SetShip(HoveredShip);
                        }
                        return true;
                    }
                }
            }
            foreach (TippedItem ti in ToolTipItems)
            {
                if (ti.r.HitTest(input.CursorPosition))
                    ToolTip.CreateTooltip(ti.TIP_ID);
            }
            if (ElementRect.HitTest(input.CursorPosition))
                return true;
            if (sliding_element.ButtonHousing.HitTest(input.CursorPosition))
                return true;
            return false;
        }

        private bool AllShipsInState(CombatState state)
        {
            foreach (Ship ship in ShipList)
            {
                if (ship.AI.CombatState != state)
                    return false;
            }
            return true;
        }

        public void SetShipList(Array<Ship> shipList, bool isFleet)
        {
            Orders.Clear();
            this.isFleet = isFleet;
            ShipList = shipList;
            SelectedShipsSL.Reset();
            SelectedShipEntry entry = new SelectedShipEntry();
            bool AllResupply = true;
            AllShipsMine = true;
            bool AllFreighters = true;
            bool AllCombat = true;
            for (int i = 0; i < shipList.Count; i++)
            {
                Ship ship = shipList[i];
                SkinnableButton button = new SkinnableButton(new Rectangle(0, 0, 20, 20), ship.GetTacticalIcon())
                {
                    IsToggle = false,
                    ReferenceObject = ship,
                    BaseColor = ship.loyalty.EmpireColor
                };
                if (entry.ShipButtons.Count < 8)
                {
                    entry.ShipButtons.Add(button);
                }
                if (entry.ShipButtons.Count == 8 || i == shipList.Count - 1)
                {
                    SelectedShipsSL.AddItem(entry);
                    entry = new SelectedShipEntry();
                }
                if (ship.AI.State != AIState.Resupply)
                {
                    AllResupply = false;
                }
                if (ship.loyalty != EmpireManager.Player)
                {
                    AllShipsMine = false;
                }
                //if (ship.CargoSpace_Max == 0f)
                if (ship.CargoSpaceMax == 0f || ship.shipData.Role == ShipData.RoleName.troop || ship.AI.State == AIState.Colonize || ship.shipData.Role == ShipData.RoleName.station || ship.Mothership != null)
                {
                    AllFreighters = false;
                }
                if (ship.shipData.Role < ShipData.RoleName.fighter || ship.shipData.ShipCategory == ShipData.Category.Civilian || ship.AI.State == AIState.Colonize || ship.Mothership != null)
                {
                    AllCombat = false;
                }
            }
            OrdersButton resupply = new OrdersButton(shipList, Vector2.Zero, OrderType.OrderResupply, 149)
            {
                SimpleToggle = true,
                Active = AllResupply
            };
            Orders.Add(resupply);


            if (AllCombat)
            {
            OrdersButton SystemDefense = new OrdersButton(shipList, Vector2.Zero, OrderType.EmpireDefense, 150)
            {
                SimpleToggle = true,
                Active = false
            };
            Orders.Add(SystemDefense);
            OrdersButton Explore = new OrdersButton(shipList, Vector2.Zero, OrderType.Explore, 136)
            {
                SimpleToggle = true,
                Active = false
            };
            Orders.Add(Explore);
            }

            if (AllFreighters)
            {
                //OrdersButton ao = new OrdersButton(shipList, Vector2.Zero, OrderType.DefineAO, 15)
                //{
                //  SimpleToggle = true,
                //  Active = false
                //};
                //this.Orders.Add(ao);
                OrdersButton tf = new OrdersButton(shipList, Vector2.Zero, OrderType.TradeFood, 16)
                {
                    SimpleToggle = true
                };
                Orders.Add(tf);
                OrdersButton tp = new OrdersButton(shipList, Vector2.Zero, OrderType.TradeProduction, 16)
                {
                    SimpleToggle = true
                };
                Orders.Add(tp);
                OrdersButton tpass = new OrdersButton(shipList, Vector2.Zero, OrderType.PassTran, 152)
                {
                    SimpleToggle = true
                };
                Orders.Add(tpass);
            }

            //Added by McShooterz: fleet scrap button
            OrdersButton Scrap = new OrdersButton(shipList, Vector2.Zero, OrderType.Scrap, 157)
            {
                SimpleToggle = true,
                Active = false
            };
            Orders.Add(Scrap);

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
                ob.clickRect.X = ElementRect.X + ElementRect.Width + 2 + 52 * ex;
                ob.clickRect.Y = sliding_element.Housing.Y + 15 + y * 52;
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