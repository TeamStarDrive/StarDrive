using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;

namespace Ship_Game.Ships
{
    public sealed class ShipInfoUIElement : UIElement
    {
        public Array<ToggleButton> CombatStatusButtons = new Array<ToggleButton>();

        public Array<OrdersButton> Orders = new Array<OrdersButton>();

        private readonly Array<TippedItem> ToolTipItems = new Array<TippedItem>();

        private readonly Rectangle SliderRect;

        private UniverseScreen Screen;

        public Ship Ship;

        private readonly Selector Sel;

        public Rectangle LeftRect;

        public Rectangle RightRect;

        public Rectangle Housing;

        public Rectangle ShipInfoRect;

        public ToggleButton Gridbutton;

        public Rectangle Power;

        public Rectangle Shields;

        public Rectangle Ordnance;

        private readonly ProgressBar PBar;

        private readonly ProgressBar SBar;

        private readonly ProgressBar OBar;

        public UITextEntry ShipNameArea;

        private readonly SlidingElement SlidingElement;

        private Rectangle DefenseRect;

        private Rectangle TroopRect;

        private Rectangle FlagRect;  //fbedard

        private bool CanRename = true;

        private bool ShowModules = true;

        private const string Fmt = "0";
        private float DoubleClickTimer = .25f;

        public ShipInfoUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen)
        {
            Screen            = screen;
            ScreenManager     = sm;
            ElementRect       = r;
            FlagRect          = new Rectangle(r.X + 150, r.Y + 50, 40, 40);
            Sel               = new Selector(r, Color.Black);
            TransitionOnTime  = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            SliderRect        = new Rectangle(r.X - 100, r.Y + r.Height - 140, 530, 130);
            SlidingElement    = new SlidingElement(SliderRect);
            Housing           = r;
            LeftRect          = new Rectangle(r.X, r.Y + 44, 180, r.Height - 44);
            RightRect         = new Rectangle(LeftRect.X + LeftRect.Width, LeftRect.Y, 220, LeftRect.Height);
            ShipNameArea      = new UITextEntry()
            {
                ClickableArea = new Rectangle(Housing.X + 41, Housing.Y + 65, 200, Fonts.Arial20Bold.LineSpacing)
            };
            int spacing        = 2;
            Power              = new Rectangle(Housing.X + 187, Housing.Y + 110, 20, 20);
            Rectangle pbarrect = new Rectangle(Power.X + Power.Width + 15, Power.Y, 150, 18);
            PBar               = new ProgressBar(pbarrect)
            {
                color = "green"
            };
            var ti = new TippedItem()
            {
                r = Power,
                TIP_ID = 27
            };
            ToolTipItems.Add(ti);
            Shields = new Rectangle(Housing.X + 187, Housing.Y + 110 + 20 + spacing, 20, 20);
            var pshieldsrect = new Rectangle(Shields.X + Shields.Width + 15, Shields.Y, 150, 18);
            SBar = new ProgressBar(pshieldsrect)
            {
                color = "blue"
            };
            ti = new TippedItem()
            {
                r = Shields,
                TIP_ID = 28
            };
            ToolTipItems.Add(ti);
            Ordnance           = new Rectangle(Housing.X + 187, Housing.Y + 110 + 20 + spacing + 20 + spacing, 20, 20);
            Rectangle pordrect = new Rectangle(Ordnance.X + Ordnance.Width + 15, Ordnance.Y, 150, 18);
            OBar               = new ProgressBar(pordrect);
            ti                 = new TippedItem()
            {
                r = Ordnance,
                TIP_ID = 29
            };
            ToolTipItems.Add(ti);
            DefenseRect = new Rectangle(Housing.X + 13, Housing.Y + 112, 22, 22);
            ti = new TippedItem()
            {
                r = DefenseRect,
                TIP_ID = 30
            };
            ToolTipItems.Add(ti);
            TroopRect = new Rectangle(Housing.X + 13, Housing.Y + 137, 22, 22);
            ti = new TippedItem()
            {
                r = TroopRect,
                TIP_ID = 37
            };
            ToolTipItems.Add(ti);
            ShipInfoRect       = new Rectangle(Housing.X + 60, Housing.Y + 110, 115, 115);
            var gridRect = new Rectangle(Housing.X + 16, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 45, 34, 24);
            Gridbutton         = new ToggleButton(gridRect, "SelectionBox/button_grid_active", "SelectionBox/button_grid_inactive", "SelectionBox/button_grid_hover", "SelectionBox/button_grid_pressed", "SelectionBox/icon_grid")
            {
                Active = true
            };
            OrderButtons(spacing, pordrect);
        }

        public void OrderButtons(int spacing, Rectangle pordrect)
        {
            var ordersBarPos = new Vector2((float) (Power.X + 15), (float) (Ordnance.Y + Ordnance.Height + spacing + 3));

            ordersBarPos.X = pordrect.X - 15;
            ToggleButton AttackRuns = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_pressed",
                "SelectionBox/icon_formation_headon");
            CombatStatusButtons.Add(AttackRuns);
            AttackRuns.Action = "attack";
            AttackRuns.HasToolTip = true;
            AttackRuns.WhichToolTip = 1;

            ordersBarPos.X = ordersBarPos.X + 25f;
            ToggleButton ShortRange = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_pressed", "SelectionBox/icon_grid");
            CombatStatusButtons.Add(ShortRange);
            ShortRange.Action = "short";
            ShortRange.HasToolTip = true;
            ShortRange.WhichToolTip = 228;

            ordersBarPos.X = ordersBarPos.X + 25f;
            ToggleButton Artillery = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_pressed",
                "SelectionBox/icon_formation_aft");
            CombatStatusButtons.Add(Artillery);
            Artillery.Action = "arty";
            Artillery.HasToolTip = true;
            Artillery.WhichToolTip = 2;

            ordersBarPos.X = ordersBarPos.X + 25f;
            ToggleButton HoldPos = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_pressed",
                "SelectionBox/icon_formation_x");
            CombatStatusButtons.Add(HoldPos);
            HoldPos.Action = "hold";
            HoldPos.HasToolTip = true;
            HoldPos.WhichToolTip = 65;
            ordersBarPos.X = ordersBarPos.X + 25f;
            ToggleButton OrbitLeft = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_pressed",
                "SelectionBox/icon_formation_left");
            CombatStatusButtons.Add(OrbitLeft);
            OrbitLeft.Action = "orbit_left";
            OrbitLeft.HasToolTip = true;
            OrbitLeft.WhichToolTip = 3;
            ordersBarPos.Y = ordersBarPos.Y + 25f;

            ToggleButton BroadsideLeft = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_pressed",
                "SelectionBox/icon_formation_bleft");
            CombatStatusButtons.Add(BroadsideLeft);
            BroadsideLeft.Action = "broadside_left";
            BroadsideLeft.HasToolTip = true;
            BroadsideLeft.WhichToolTip = 159;
            ordersBarPos.Y = ordersBarPos.Y - 25f;
            ordersBarPos.X = ordersBarPos.X + 25f;

            ToggleButton OrbitRight = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_pressed",
                "SelectionBox/icon_formation_right");
            CombatStatusButtons.Add(OrbitRight);
            OrbitRight.Action = "orbit_right";
            OrbitRight.HasToolTip = true;
            OrbitRight.WhichToolTip = 4;
            ordersBarPos.Y = ordersBarPos.Y + 25f;

            ToggleButton BroadsideRight = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_pressed",
                "SelectionBox/icon_formation_bright");
            CombatStatusButtons.Add(BroadsideRight);
            BroadsideRight.Action = "broadside_right";
            BroadsideRight.HasToolTip = true;
            BroadsideRight.WhichToolTip = 160;
            ordersBarPos.Y = ordersBarPos.Y - 25f;
            ordersBarPos.X = ordersBarPos.X + 25f;

            ToggleButton Evade = new ToggleButton(new Rectangle((int) ordersBarPos.X, (int) ordersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_pressed",
                "SelectionBox/icon_formation_stop");
            CombatStatusButtons.Add(Evade);
            Evade.Action = "evade";
            Evade.HasToolTip = true;
            Evade.WhichToolTip = 6;
        }
        private void DrawOrderButtons(float transitionOffset)
        {
            foreach (OrdersButton ob in Orders)
            {
                Rectangle r = ob.clickRect;
                r.X = r.X - (int)(transitionOffset * 300f);
                ob.Draw(ScreenManager, r);
            }
        }
        private void OrderButtonInput(InputState input)
        {
            if (Ship.loyalty != EmpireManager.Player || Ship.isConstructor) return;
            foreach (ToggleButton toggleButton in CombatStatusButtons)
            {
                if (toggleButton.Rect.HitTest(input.CursorPosition))
                {
                    toggleButton.Hover = true;
                    if (toggleButton.HasToolTip)
                        ToolTip.CreateTooltip(toggleButton.WhichToolTip);
                    if (input.InGameSelect)
                    {
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        switch (toggleButton.Action)
                        {
                            case "attack":
                                Ship.AI.CombatState = CombatState.AttackRuns;
                                break;
                            case "arty":
                                Ship.AI.CombatState = CombatState.Artillery;
                                break;
                            case "hold":
                                Ship.AI.CombatState = CombatState.HoldPosition;
                                Ship.AI.OrderAllStop();
                                break;
                            case "orbit_left":
                                Ship.AI.CombatState = CombatState.OrbitLeft;
                                break;
                            case "broadside_left":
                                Ship.AI.CombatState = CombatState.BroadsideLeft;
                                break;
                            case "orbit_right":
                                Ship.AI.CombatState = CombatState.OrbitRight;
                                break;
                            case "broadside_right":
                                Ship.AI.CombatState = CombatState.BroadsideRight;
                                break;
                            case "evade":
                                Ship.AI.CombatState = CombatState.Evade;
                                break;
                            case "short":
                                Ship.AI.CombatState = CombatState.ShortRange;
                                break;
                        }
                        if (toggleButton.Action != "hold" && Ship.AI.State == AIState.HoldPosition)
                            Ship.AI.State = AIState.AwaitingOrders;
                    }
                }
                else
                    toggleButton.Hover = false;
                switch (toggleButton.Action)
                {
                    case "attack":
                        toggleButton.Active = Ship.AI.CombatState == CombatState.AttackRuns;
                        continue;
                    case "arty":
                        toggleButton.Active = Ship.AI.CombatState == CombatState.Artillery;
                        continue;
                    case "hold":
                        toggleButton.Active = Ship.AI.CombatState == CombatState.HoldPosition;
                        continue;
                    case "orbit_left":
                        toggleButton.Active = Ship.AI.CombatState == CombatState.OrbitLeft;
                        continue;
                    case "broadside_left":
                        toggleButton.Active = Ship.AI.CombatState == CombatState.BroadsideLeft;
                        continue;
                    case "orbit_right":
                        toggleButton.Active = Ship.AI.CombatState == CombatState.OrbitRight;
                        continue;
                    case "broadside_right":
                        toggleButton.Active = Ship.AI.CombatState == CombatState.BroadsideRight;
                        continue;
                    case "evade":
                        toggleButton.Active = Ship.AI.CombatState == CombatState.Evade;
                        continue;
                    case "short":
                        toggleButton.Active = Ship.AI.CombatState == CombatState.ShortRange;
                        continue;
                    default:
                        continue;
                }
            }
        }


        public override void Draw(GameTime gameTime)
        {
            if (Screen.SelectedShip == null) return;  //fbedard

            float transitionOffset = MathHelper.SmoothStep(0f, 1f, base.TransitionPosition);
            int columns = Orders.Count / 2 + Orders.Count % 2;
            SlidingElement.Draw(ScreenManager, (int)((float)(columns * 55) * (1f - base.TransitionPosition)) + (SlidingElement.Open ? 20 - columns : 0));
            DrawOrderButtons(transitionOffset);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/unitselmenu_main"), Housing, Color.White);
            Gridbutton.Draw(ScreenManager);
            var namePos           = new Vector2(Housing.X + 30, Housing.Y + 63);
            string name           = (!string.IsNullOrEmpty(Ship.VanityName) ? Ship.VanityName : Ship.Name);
            SpriteFont TitleFont  = Fonts.Arial14Bold;
            Vector2 ShipSuperName = new Vector2(Housing.X + 30, Housing.Y + 79);
            if (Fonts.Arial14Bold.MeasureString(name).X > 180f)
            {
                TitleFont = Fonts.Arial12Bold;
                namePos.X = namePos.X - 8;
                namePos.Y = namePos.Y + 1;
            }
            ShipNameArea.Draw(TitleFont, ScreenManager.SpriteBatch, namePos, gameTime, tColor);
            //Added by McShooterz:
            //longName = string.Concat(ship.Name, " - ", Localizer.GetRole(ship.shipData.Role, ship.loyalty));
            string longName = string.Concat(Ship.Name, " - ", Ship.DesignRole);
            if (Ship.shipData.ShipCategory != ShipData.Category.Unclassified)
                longName += string.Concat(" - ", Ship.shipData.GetCategory());
            ScreenManager.SpriteBatch.DrawString(Fonts.Visitor10, longName, ShipSuperName, Color.Orange);

            string text;
            Vector2 shipStatus              = new Vector2(Sel.Rect.X + Sel.Rect.Width - 170, Housing.Y + 68);
            text                            = HelperFunctions.ParseText(Fonts.Arial10, ShipListScreenEntry.GetStatusText(Ship), 155f);
            HelperFunctions.ClampVectorToInt(ref shipStatus);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial10, text, shipStatus, tColor);
            shipStatus.Y                    = shipStatus.Y + Fonts.Arial12Bold.MeasureString(text).Y;
            Ship.RenderOverlay(ScreenManager.SpriteBatch, ShipInfoRect, ShowModules);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Modules/NuclearReactorMedium"), Power, Color.White);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Modules/Shield_1KW"), Shields, Color.White);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Modules/Ordnance"), Ordnance, Color.White);
            PBar.Max                        = Ship.PowerStoreMax;
            PBar.Progress                   = Ship.PowerCurrent;
            PBar.Draw(ScreenManager.SpriteBatch);
            SBar.Max                        = Ship.shield_max;
            SBar.Progress                   = Ship.shield_power;
            SBar.Draw(ScreenManager.SpriteBatch);
            OBar.Max                        = Ship.OrdinanceMax;
            OBar.Progress                   = Ship.Ordinance;
            OBar.Draw(ScreenManager.SpriteBatch);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_shield"), DefenseRect, Color.White);
            Vector2 defPos                  = new Vector2(DefenseRect.X + DefenseRect.Width + 2, DefenseRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
            SpriteBatch spriteBatch         = ScreenManager.SpriteBatch;
            SpriteFont arial12Bold          = Fonts.Arial12Bold;
            float mechanicalBoardingDefense = Ship.MechanicalBoardingDefense + Ship.TroopBoardingDefense;
            spriteBatch.DrawString(arial12Bold, mechanicalBoardingDefense.ToString(Fmt), defPos, Color.White);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_troop_shipUI"), TroopRect, Color.White);
            Vector2 troopPos                = new Vector2(TroopRect.X + TroopRect.Width + 2, TroopRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(Ship.TroopList.Count, "/", Ship.TroopCapacity), troopPos, Color.White);
            if (Ship.loyalty == EmpireManager.Player)
            {
                foreach (ToggleButton button in CombatStatusButtons)
                {
                    button.Draw(ScreenManager);
                }
            }
            else  //fbedard: Display race icon of enemy ship in Ship UI
            {
                var flagShip = new Rectangle(FlagRect.X + 190, FlagRect.Y + 130, 40, 40);
                SpriteBatch spriteBatch1 = ScreenManager.SpriteBatch;
                KeyValuePair<string, Texture2D> keyValuePair = ResourceManager.FlagTextures[Ship.loyalty.data.Traits.FlagIndex];
                spriteBatch1.Draw(keyValuePair.Value, flagShip, Ship.loyalty.EmpireColor);
            }

            float x = Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 mousePos = new Vector2(x, (float)state.Y);
            //Added by McShooterz: new experience level display
            Rectangle star = new Rectangle(TroopRect.X, TroopRect.Y + 23, 22, 22);
            Vector2 levelPos = new Vector2(star.X + star.Width + 2, star.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_experience_shipUI"), star, Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Ship.Level.ToString(), levelPos, Color.White);
            if (star.HitTest(mousePos))
            {
                ToolTip.CreateTooltip(161);
            }
            //Added by McShooterz: kills display
            star = new Rectangle(star.X, star.Y + 19, 22, 22);
            levelPos = new Vector2(star.X + star.Width + 2, star.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_kills_shipUI"), star, Color.White);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Ship.kills.ToString(), levelPos, Color.White);
            Vector2 statusArea = new Vector2(Housing.X + 175, Housing.Y + 15);
            int numStatus = 0;
            if (Ship.loyalty.data.Traits.Pack)
            {
                var packRect         = new Rectangle((int)statusArea.X, (int)statusArea.Y, 48, 32);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("StatusIcons/icon_pack"), packRect, Color.White);
                var textPos          = new Vector2(packRect.X + 26, packRect.Y + 15);
                float damageModifier = Ship.DamageModifier * 100f;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(damageModifier.ToString("0"), "%"), textPos, Color.White);
                numStatus++;
                if (packRect.HitTest(mousePos))
                {
                    ToolTip.CreateTooltip(Localizer.Token(2245));
                }
            }

            if (Ship.CargoSpaceUsed > 0f)
            {
                foreach (Cargo cargo in Ship.EnumLoadedCargo())
                {
                    Texture2D texture = ResourceManager.Texture("Goods/" + cargo.CargoId);
                    var goodRect      = new Rectangle((int)statusArea.X + numStatus * 53, (int)statusArea.Y, 32, 32);
                    ScreenManager.SpriteBatch.Draw(texture, goodRect, Color.White);

                    var textPos = new Vector2(goodRect.X + 32, goodRect.Y + 16 - Fonts.Arial12.LineSpacing / 2);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, cargo.Amount.ToString("0"), textPos, Color.White);

                    if (goodRect.HitTest(mousePos))
                    {
                        Good good = ResourceManager.GoodsDict[cargo.CargoId];
                        ToolTip.CreateTooltip($"{good.Name}\n\n{good.Description}");
                    }
                    numStatus++;
                }
            }

            if(Ship.FTLModifier <1 && !Ship.Inhibited)
            {
                //if (ship.GetSystem() != null)
                //{
                    Rectangle foodRect = new Rectangle((int)statusArea.X + numStatus * 53, (int)statusArea.Y, 48, 32);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("StatusIcons/icon_boosted"), foodRect, Color.PaleVioletRed);
                    if (foodRect.HitTest(mousePos))
                    {
                        string eState = Ship.engineState == Ship.MoveState.Warp ? "FTL" : "Sublight";
                        ToolTip.CreateTooltip(string.Concat(Localizer.Token(6179), $"{1f - Ship.FTLModifier:P0}", "\n\nEngine State: ", eState));
                    }
                    numStatus++;
                //}

            }
            if (Ship.FTLModifier > 1 && !Ship.Inhibited && Ship.engineState == Ship.MoveState.Warp)
            {

                    var rect = new Rectangle((int)statusArea.X + numStatus * 53, (int)statusArea.Y, 48, 32);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("StatusIcons/icon_boosted"), rect, Color.LightGreen);
                    if (rect.HitTest(mousePos))
                    {

                        ToolTip.CreateTooltip(string.Concat(Localizer.Token(6180), $"{Ship.FTLModifier - 1f:P0}", "\n\nEngine State: FTL"));
                    }
                    numStatus++;
            }

            if (Ship.Inhibited )
            {
                bool planet = false;
                if (Screen.GravityWells && Ship.System!= null)
                {
                    foreach (Planet p in Ship.System.PlanetList)
                    {
                        if (p.Center.OutsideRadius(Ship.Position, p.GravityWellRadius))
                        {
                            continue;
                        }
                        planet = true;
                    }
                }
                if (planet)
                {
                    var foodRect = new Rectangle((int)statusArea.X + numStatus * 53, (int)statusArea.Y, 48, 32);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("StatusIcons/icon_gravwell"), foodRect, Color.White);
                    if (foodRect.HitTest(mousePos))
                    {
                        ToolTip.CreateTooltip(Localizer.Token(2287));
                    }
                    numStatus++;
                }

                else if (RandomEventManager.ActiveEvent == null || !RandomEventManager.ActiveEvent.InhibitWarp)
                {
                    var foodRect = new Rectangle((int)statusArea.X + numStatus * 53, (int)statusArea.Y, 48, 32);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("StatusIcons/icon_inhibited"), foodRect, Color.White);
                    if (foodRect.HitTest(mousePos))
                    {
                        ToolTip.CreateTooltip(117);
                    }
                    numStatus++;
                }
                else
                {
                    var foodRect = new Rectangle((int)statusArea.X + numStatus * 53, (int)statusArea.Y, 48, 32);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("StatusIcons/icon_flux"), foodRect, Color.White);
                    if (foodRect.HitTest(mousePos))
                    {
                        ToolTip.CreateTooltip(Localizer.Token(2285));
                    }
                    numStatus++;
                }
            }
            if (!Ship.EMPdisabled) return;
            {
                var foodRect = new Rectangle((int)statusArea.X + numStatus * 53, (int)statusArea.Y, 48, 32);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("StatusIcons/icon_disabled"), foodRect, Color.White);
                if (foodRect.HitTest(mousePos))
                {
                    ToolTip.CreateTooltip(116);
                }
                numStatus++;
            }
        }

   
        public override bool HandleInput(InputState input)
        {
            if (Screen.SelectedShip == null) return false;  //fbedard
            
            if (SlidingElement.HandleInput(input))
            {
                if (SlidingElement.Open)
                    State = ElementState.TransitionOn;
                else
                    State = ElementState.TransitionOff;
                return true;
            }

            if (ShipNameArea.ClickableArea.HitTest(input.CursorPosition))
            {
                ShipNameArea.Hover = true;
                if (input.InGameSelect && CanRename)
                    ShipNameArea.HandlingInput = true;
            }
            else
                ShipNameArea.Hover = false;
            if (ShipNameArea.HandlingInput)
            {
                GlobalStats.TakingInput = true;
                ShipNameArea.HandleTextInput(ref Ship.VanityName, input);
                ShipNameArea.Text = Ship.VanityName;
            }
            else
                GlobalStats.TakingInput = false;
            if (Gridbutton.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(Localizer.Token(2204));
            if (Gridbutton.HandleInput(input))
            {
                GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                ShowModules = !ShowModules;
                Gridbutton.Active = ShowModules;
                return true;
            }
            else
            {
                if (Ship == null)
                    return false;
                if (DoubleClickTimer > 0)
                    DoubleClickTimer -= 0.01666f;
                if (ShipInfoRect.HitTest(input.CursorPosition) && input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Released && DoubleClickTimer > 0)
                {
                    Empire.Universe.ViewingShip = false;
                    Empire.Universe.AdjustCamTimer = 0.5f;
                    Empire.Universe.CamDestination.X = Ship.Center.X;
                    Empire.Universe.CamDestination.Y = Ship.Center.Y;
                    if (Empire.Universe.viewState < UniverseScreen.UnivScreenState.SystemView)
                        Empire.Universe.CamDestination.Z = Empire.Universe.GetZfromScreenState(UniverseScreen.UnivScreenState.SystemView);
                }
                else if (ElementRect.HitTest(input.CursorPosition) && input.MouseCurr.LeftButton == ButtonState.Pressed && input.MousePrev.LeftButton == ButtonState.Released)
                    DoubleClickTimer = 0.25f;    
                OrderButtonInput(input);
                foreach (ShipInfoUIElement.TippedItem tippedItem in ToolTipItems)
                {
                    if (tippedItem.r.HitTest(input.CursorPosition))
                        ToolTip.CreateTooltip(tippedItem.TIP_ID);
                }
                if (ElementRect.HitTest(input.CursorPosition))
                    return true;
                if (State == UIElement.ElementState.Open)
                {
                    bool flag = false;
                    foreach (OrdersButton ordersButton in Orders)
                    {
                        if (ordersButton.HandleInput(input, ScreenManager))
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

        

        public void SetShip(Ship s)
        {
            CanRename = s.loyalty == EmpireManager.Player;
            ShipNameArea.HandlingInput = false;
            ShipNameArea.Text = s.VanityName;
            Orders.Clear();
            Ship = s;
            if (Ship.loyalty != EmpireManager.Player)
            {
                return;
            }
            if (Ship.AI.OrderQueue.NotEmpty)
            {
                try
                {
                    if (Ship.AI.OrderQueue.PeekLast.Plan == ShipAI.Plan.DeployStructure)
                    {
                        return;
                    }
                }
                catch
                {
                    return;
                }
            }
            if (Ship.shipData.Role > ShipData.RoleName.station)
            {
                OrdersButton resupply = new OrdersButton(Ship, Vector2.Zero, OrderType.OrderResupply, 149)
                {
                    ValueToModify = new Ref<bool>(() => Ship.DoingResupply, (bool x) => Ship.DoingResupply = x)
                };
                Orders.Add(resupply);
            }
            if (Ship.shipData.Role != ShipData.RoleName.troop && Ship.AI.State != AIState.Colonize && Ship.shipData.Role != ShipData.RoleName.station && Ship.Mothership == null)
            {
                OrdersButton ao = new OrdersButton(Ship, Vector2.Zero, OrderType.DefineAO, 15)
                {
                    ValueToModify = new Ref<bool>(() => Screen.DefiningAO, (bool x) => {
                        Screen.DefiningAO = x;
                        Screen.AORect = new Rectangle(0, 0, 0, 0);
                    })
                };
                Orders.Add(ao);
            }
            if (Ship.CargoSpaceMax > 0f && Ship.shipData.Role != ShipData.RoleName.troop && Ship.AI.State != AIState.Colonize && Ship.shipData.Role != ShipData.RoleName.station && Ship.Mothership == null)
            {
                OrdersButton tf = new OrdersButton(Ship, Vector2.Zero, OrderType.TradeFood, 16)
                {
                    ValueToModify = new Ref<bool>(() => Ship.DoingTransport, (bool x) => Ship.DoingTransport = x),
                    RightClickValueToModify = new Ref<bool>(() => Ship.TransportingFood, (bool x) => Ship.TransportingFood = x)
                };
                Orders.Add(tf);
                OrdersButton tp = new OrdersButton(Ship, Vector2.Zero, OrderType.TradeProduction, 17)
                {
                    ValueToModify = new Ref<bool>(() => Ship.DoingTransport, (bool x) => Ship.DoingTransport = x),
                    RightClickValueToModify = new Ref<bool>(() => Ship.TransportingProduction, (bool x) => Ship.TransportingProduction = x)
                };
                Orders.Add(tp);
                OrdersButton tpass = new OrdersButton(Ship, Vector2.Zero, OrderType.PassTran, 137)
                {
                    ValueToModify = new Ref<bool>(() => Ship.DoingPassTransport, (bool x) => Ship.DoingPassTransport = x)
                };
                Orders.Add(tpass);
            }
            if (Ship.shield_max > 0f)
            {
                OrdersButton ob = new OrdersButton(Ship, Vector2.Zero, OrderType.ShieldToggle, 18)
                {
                    ValueToModify = new Ref<bool>(() => Ship.ShieldsUp, (bool x) => Ship.ShieldsUp = x)
                };
                Orders.Add(ob);
            }
            if (Ship.GetHangars().Count > 0 && Ship.Mothership == null)
            {
                bool hasTroops = false;
                bool hasFighters = false;
                foreach (ShipModule hangar in Ship.GetHangars())
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
                    OrdersButton ob = new OrdersButton(Ship, Vector2.Zero, OrderType.FighterToggle, 19)
                    {
                        ValueToModify = new Ref<bool>(() => Ship.FightersOut, (bool x) =>
                        {
                            Ship.FightersOut = x;
                        })
                    };
                    Orders.Add(ob);
               
                }
                if (hasTroops)
                {
                    OrdersButton ob = new OrdersButton(Ship, Vector2.Zero, OrderType.TroopToggle, 225)
                    {
                        ValueToModify = new Ref<bool>(() => Ship.TroopsOut, (bool x) => {
                            Ship.TroopsOut = x;
                            //ship.ManualHangarOverride = true;
                        })
                    };
                    Orders.Add(ob);
                }
                //if (ship.shipData.Role != ShipData.RoleName.station)
                {
                    OrdersButton ob2 = new OrdersButton(Ship, Vector2.Zero, OrderType.FighterRecall, 146)
                    {
                        ValueToModify = new Ref<bool>(() => Ship.RecallFightersBeforeFTL, (bool x) =>
                        {
                            Ship.RecallFightersBeforeFTL = x;
                            Ship.ManualHangarOverride = !x;
                        }
                            )
                    };
                    Orders.Add(ob2);
                }
            }
            if (Ship.shipData.Role >= ShipData.RoleName.fighter && Ship.Mothership == null && Ship.AI.State != AIState.Colonize && Ship.shipData.ShipCategory != ShipData.Category.Civilian)
            {
                var exp = new OrdersButton(Ship, Vector2.Zero, OrderType.Explore, 136)
                {
                    ValueToModify = new Ref<bool>(() => Ship.DoingExplore, (bool x) => Ship.DoingExplore = x)
                };
                Orders.Add(exp);
                var systemDefense = new OrdersButton(Ship, Vector2.Zero, OrderType.EmpireDefense, 150)
                {
                    ValueToModify = new Ref<bool>(() => Ship.DoingSystemDefense, (bool x) => Ship.DoingSystemDefense = x),
                    Active = false
                };
                Orders.Add(systemDefense);
            }
            if (Ship.Mothership == null)
            {
                var rf = new OrdersButton(Ship, Vector2.Zero, OrderType.Refit, 158)
                {
                    ValueToModify = new Ref<bool>(() => Ship.doingRefit, (bool x) => Ship.doingRefit = x),
                    Active = false
                };
                Orders.Add(rf);
                //Added by McShooterz: scrap order
                var sc = new OrdersButton(Ship, Vector2.Zero, OrderType.Scrap, 157)
                {
                    ValueToModify = new Ref<bool>(() => Ship.doingScrap, (bool x) => Ship.doingScrap = x),
                    Active = false
                };
                Orders.Add(sc);
            }

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
                ob.clickRect.Y = SlidingElement.Housing.Y + 15 + y * 52;
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