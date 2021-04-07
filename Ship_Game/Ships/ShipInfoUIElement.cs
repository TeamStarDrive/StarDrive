using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Audio;

namespace Ship_Game.Ships
{
    public sealed class ShipInfoUIElement : UIElement
    {
        public Array<ToggleButton> CombatStatusButtons = new Array<ToggleButton>();
        public Array<OrdersButton> Orders = new Array<OrdersButton>();
        private readonly Array<TippedItem> ToolTipItems = new Array<TippedItem>();
        private readonly UniverseScreen Screen;
        public Ship Ship;
        private readonly Selector Sel;
        public Rectangle LeftRect;
        public Rectangle RightRect;
        public Rectangle Housing;
        public Rectangle ShipInfoRect;
        public ToggleButton GridButton;
        public Rectangle Power;
        public Rectangle Shields;
        public Rectangle Ordnance;
        private readonly ProgressBar PBar;
        private readonly ProgressBar SBar;
        private readonly ProgressBar OBar;
        public UITextEntry ShipNameArea;
        private readonly SlidingElement SlidingElement;
        private readonly Rectangle DefenseRect;
        private readonly Rectangle TroopRect;
        private readonly Rectangle FlagRect;  //fbedard
        private bool CanRename   = true;
        private bool ShowModules = true;
        private Vector2 StatusArea;

        public ShipInfoUIElement(Rectangle r, ScreenManager sm, UniverseScreen screen)
        {
            Screen               = screen;
            ScreenManager        = sm;
            ElementRect          = r;
            FlagRect             = new Rectangle(r.X + 365, r.Y + 71, 18, 18);
            Sel                  = new Selector(r, Color.Black);
            TransitionOnTime     = TimeSpan.FromSeconds(0.25);
            TransitionOffTime    = TimeSpan.FromSeconds(0.25);
            Rectangle sliderRect = new Rectangle(r.X - 100, r.Y + r.Height - 140, 530, 130);
            SlidingElement       = new SlidingElement(sliderRect);
            Housing              = r;
            LeftRect             = new Rectangle(r.X, r.Y + 44, 180, r.Height - 44);
            RightRect            = new Rectangle(LeftRect.X + LeftRect.Width, LeftRect.Y, 220, LeftRect.Height);
            int spacing          = 2;
            Power                = new Rectangle(Housing.X + 187, Housing.Y + 110, 20, 20);
            Rectangle pBarRect   = new Rectangle(Power.X + Power.Width + 15, Power.Y, 150, 18);
            ShipNameArea         = new UITextEntry
            {
                ClickableArea = new Rectangle(Housing.X + 41, Housing.Y + 65, 200, Fonts.Arial20Bold.LineSpacing)
            };

            PBar = new ProgressBar(pBarRect)
            {
                color = "green"
            };
            var ti = new TippedItem
            {
                r = Power,
                Tooltip = GameText.IndicatesThisShipsCurrentPower
            };
            ToolTipItems.Add(ti);
            Shields = new Rectangle(Housing.X + 187, Housing.Y + 110 + 20 + spacing, 20, 20);
            var pShieldsRect = new Rectangle(Shields.X + Shields.Width + 15, Shields.Y, 150, 18);
            SBar = new ProgressBar(pShieldsRect)
            {
                color = "blue"
            };
            ti = new TippedItem
            {
                r = Shields,
                Tooltip = GameText.IndicatesTheTotalPowerOf
            };
            ToolTipItems.Add(ti);
            Ordnance           = new Rectangle(Housing.X + 187, Housing.Y + 110 + 20 + spacing + 20 + spacing, 20, 20);
            Rectangle pOrderRect = new Rectangle(Ordnance.X + Ordnance.Width + 15, Ordnance.Y, 150, 18);
            OBar               = new ProgressBar(pOrderRect);
            ti                 = new TippedItem
            {
                r = Ordnance,
                Tooltip = GameText.IndicatesThisShipsCurrentStores
            };
            ToolTipItems.Add(ti);
            DefenseRect = new Rectangle(Housing.X + 13, Housing.Y + 112, 22, 22);
            ti = new TippedItem
            {
                r = DefenseRect,
                Tooltip = GameText.IndicatesThisShipsCurrentDefense
            };
            ToolTipItems.Add(ti);
            TroopRect = new Rectangle(Housing.X + 13, Housing.Y + 137, 22, 22);
            ti = new TippedItem
            {
                r = TroopRect,
                Tooltip = GameText.IndicatesTheNumberOfTroops
            };
            ToolTipItems.Add(ti);
            ShipInfoRect = new Rectangle(Housing.X + 60, Housing.Y + 110, 115, 115);
            Vector2 gridRect = new Vector2(Housing.X + 16, Screen.ScreenHeight - 45);
            GridButton = new ToggleButton(gridRect, ToggleButtonStyle.Grid, "SelectionBox/icon_grid")
            {
                IsToggled = true
            };
            OrderButtons(spacing, pOrderRect);
        }

        public void OrderButtons(int spacing, Rectangle pOrderRect)
        {
            float startX = pOrderRect.X - 15;
            var ordersBarPos = new Vector2(startX, (Ordnance.Y + Ordnance.Height + spacing + 3));
            void AddOrderBtn(string icon, CombatState state, LocalizedText toolTip)
            {
                var button = new ToggleButton(ordersBarPos, ToggleButtonStyle.Formation, icon)
                {
                    CombatState   = state,
                    Tooltip = toolTip,
                };
                button.OnClick = (b) => OnOrderButtonClicked(state);
                CombatStatusButtons.Add(button);
                ordersBarPos.X += 25f;
            }

            AddOrderBtn("SelectionBox/icon_formation_headon", CombatState.AttackRuns, toolTip: GameText.ShipWillMakeHeadonAttack);
            AddOrderBtn("SelectionBox/icon_grid", CombatState.ShortRange,          toolTip: GameText.ShipWillRotateSoThat2);
            AddOrderBtn("SelectionBox/icon_formation_aft", CombatState.Artillery, toolTip: GameText.ShipWillRotateSoThat);
            AddOrderBtn("SelectionBox/icon_formation_x", CombatState.HoldPosition, toolTip: GameText.ShipWillAttemptToHold);
            AddOrderBtn("SelectionBox/icon_formation_left", CombatState.OrbitLeft,  toolTip: GameText.ShipWillManeuverToKeep);
            AddOrderBtn("SelectionBox/icon_formation_right", CombatState.OrbitRight, toolTip: GameText.ShipWillManeuverToKeep2);
            AddOrderBtn("SelectionBox/icon_formation_stop", CombatState.Evade,  toolTip: GameText.ShipWillAvoidEngagingIn);

            ordersBarPos = new Vector2(startX + 4*25f, ordersBarPos.Y + 25f);
            AddOrderBtn("SelectionBox/icon_formation_bleft", CombatState.BroadsideLeft,  toolTip: GameText.ShipWillMoveWithinMaximum);
            AddOrderBtn("SelectionBox/icon_formation_bright", CombatState.BroadsideLeft, toolTip: GameText.ShipWillMoveWithinMaximum2);
        }

        void OnOrderButtonClicked(CombatState state)
        {
            Ship.AI.CombatState = state;
            if (state == CombatState.HoldPosition)
                Ship.AI.OrderAllStop();

            // @todo Is this some sort of bug fix?
            if (state != CombatState.HoldPosition && Ship.AI.State == AIState.HoldPosition)
                Ship.AI.State = AIState.AwaitingOrders;

            Ship.shipStatusChanged = true;
        }

        void DrawOrderButtons(float transitionOffset)
        {
            foreach (OrdersButton ob in Orders)
            {
                Rectangle r = ob.ClickRect;
                r.X -= (int)(transitionOffset * 300f);
                ob.Draw(ScreenManager, r);
            }
        }

        void UpdateOrderButtonToggles()
        {
            foreach (ToggleButton toggleButton in CombatStatusButtons)
                toggleButton.IsToggled = (Ship.AI.CombatState == toggleButton.CombatState);
        }

        bool OrderButtonInput(InputState input)
        {
            if (Ship.loyalty != EmpireManager.Player || Ship.IsConstructor)
                return false;

            bool inputCaptured = CombatStatusButtons.Any(b => b.HandleInput(input));
            UpdateOrderButtonToggles();
            return inputCaptured;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (Screen.SelectedShip == null) return;  //fbedard

            float transitionOffset = MathHelper.SmoothStep(0f, 1f, TransitionPosition);
            int columns = Orders.Count / 2 + Orders.Count % 2;
            SlidingElement.Draw(ScreenManager, (int)(columns * 55 * (1f - TransitionPosition)) + (SlidingElement.Open ? 20 - columns : 0));
            DrawOrderButtons(transitionOffset);

            batch.Draw(ResourceManager.Texture("SelectionBox/unitselmenu_main"), Housing, Color.White);
            GridButton.Draw(batch, elapsed);
            var namePos           = new Vector2(Housing.X + 30, Housing.Y + 63);
            string name           = (!string.IsNullOrEmpty(Ship.VanityName) ? Ship.VanityName : Ship.Name);
            SpriteFont titleFont  = Fonts.Arial14Bold;
            Vector2 shipSuperName = new Vector2(Housing.X + 30, Housing.Y + 79);
            if (Fonts.Arial14Bold.MeasureString(name).X > 180f)
            {
                titleFont = Fonts.Arial12Bold;
                namePos.X = namePos.X - 8;
                namePos.Y = namePos.Y + 1;
            }
            ShipNameArea.Draw(batch, elapsed, titleFont, namePos, tColor);
            //Added by McShooterz:
            //longName = string.Concat(ship.Name, " - ", Localizer.GetRole(ship.shipData.Role, ship.loyalty));
            string longName = string.Concat(Ship.Name, " - ", Localizer.GetRole(Ship.DesignRole, Ship.loyalty));
            if (Ship.shipData.ShipCategory != ShipData.Category.Unclassified)
                longName += " - "+Ship.shipData.GetCategory();

            batch.DrawString(Fonts.Visitor10, longName, shipSuperName, Color.Orange);

            string text;
            Vector2 shipStatus              = new Vector2(Sel.Rect.X + Sel.Rect.Width - 168, Housing.Y + 64);
            text                            = Fonts.TahomaBold9.ParseText(ShipListScreenItem.GetStatusText(Ship), 120);
            HelperFunctions.ClampVectorToInt(ref shipStatus);
            batch.DrawString(Fonts.TahomaBold9, text, shipStatus, tColor);
            shipStatus.Y += Fonts.Arial12Bold.MeasureString(text).Y;

            Ship.RenderOverlay(batch, ShipInfoRect, ShowModules);
            batch.Draw(ResourceManager.Texture("Modules/NuclearReactorMedium"), Power, Color.White);
            batch.Draw(ResourceManager.Texture("Modules/Shield_1KW"), Shields, Color.White);
            batch.Draw(ResourceManager.Texture("Modules/Ordnance"), Ordnance, Color.White);

            PBar.Max      = Ship.PowerStoreMax;
            PBar.Progress = Ship.PowerCurrent;
            SBar.Max      = Ship.shield_max;
            SBar.Progress = Ship.shield_power;
            OBar.Max      = Ship.OrdinanceMax;
            OBar.Progress = Ship.Ordinance;
            PBar.Draw(batch);
            SBar.Draw(batch);
            OBar.Draw(batch);
            batch.Draw(ResourceManager.Texture("UI/icon_shield"), DefenseRect, Color.White);
            var defPos = new Vector2(DefenseRect.X + DefenseRect.Width + 2, DefenseRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
            float totalBoardingDefense = Ship.MechanicalBoardingDefense + Ship.TroopBoardingDefense;
            batch.DrawString(Fonts.Arial12Bold, totalBoardingDefense.String(0), defPos, Color.White);
            batch.Draw(ResourceManager.Texture("UI/icon_troop_shipUI"), TroopRect, Color.White);
            DrawTroopStatus();

            if (Ship.loyalty == EmpireManager.Player)
            {
                foreach (ToggleButton button in CombatStatusButtons)
                {
                    button.Draw(batch, elapsed);
                    if (button.Hover) Ship.DrawWeaponRangeCircles(Screen, button.CombatState);
                }
            }

            //fbedard: Display race icon
            batch.Draw(ResourceManager.Flag(Ship.loyalty), FlagRect, Ship.loyalty.EmpireColor);

            Vector2 mousePos = Screen.Input.CursorPosition;

            //Added by McShooterz: new experience level display
            var star     = new Rectangle(TroopRect.X, TroopRect.Y + 23, 22, 22);
            var levelPos = new Vector2(star.X + star.Width + 2, star.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
            batch.Draw(ResourceManager.Texture("UI/icon_experience_shipUI"), star, Color.White);
            batch.DrawString(Fonts.Arial12Bold, Ship.Level.ToString(), levelPos, Color.White);
            if (star.HitTest(mousePos))
                ToolTip.CreateTooltip(GameText.IndicatesAShipsExperienceLevel2);

            //Added by McShooterz: kills display
            star       = new Rectangle(star.X, star.Y + 19, 22, 22);
            levelPos   = new Vector2(star.X + star.Width + 2, star.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
            StatusArea = new Vector2(Housing.X + 175, Housing.Y + 15);
            batch.Draw(ResourceManager.Texture("UI/icon_kills_shipUI"), star, Color.White);
            batch.DrawString(Fonts.Arial12Bold, Ship.kills.ToString(), levelPos, Color.White);
            int numStatus = 0;

            // FB - limit data display to non player ships
            if (HelperFunctions.DataVisibleToPlayer(Ship.loyalty))
            {
                DrawCarrierStatus(mousePos);
                DrawResupplyReason(Ship);
                DrawRadiationDamageWarning(Ship);
                DrawPack(batch, mousePos, ref numStatus);
                DrawFTL(batch, mousePos, ref numStatus);
                DrawInhibited(batch, mousePos, ref numStatus);
                DrawEmp(batch, mousePos, ref numStatus);
                DrawStructuralIntegrity(batch, mousePos, ref numStatus);
            }
            DrawCargoUsed(batch, mousePos, ref numStatus);
        }

        void DrawIconWithTooltip(SpriteBatch batch, SubTexture icon, Func<string> tooltip, Vector2 mousePos, Color color, int numStatus)
        {
            var rect = new Rectangle((int)StatusArea.X + numStatus * 53, (int)StatusArea.Y, 48, 32);
            batch.Draw(icon, rect, color);
            if (rect.HitTest(mousePos)) ToolTip.CreateTooltip(tooltip());
        }

        void DrawPack(SpriteBatch batch, Vector2 mousePos, ref int numStatus)
        {
            SubTexture iconPack = ResourceManager.Texture("StatusIcons/icon_pack");

            if (!Ship.loyalty.data.Traits.Pack)
                return;

            var packRect = new Rectangle((int)StatusArea.X, (int)StatusArea.Y, 48, 32);
            batch.Draw(iconPack, packRect, Color.White);
            var textPos          = new Vector2(packRect.X + 26, packRect.Y + 15);
            float damageModifier = Ship.PackDamageModifier * 100f;
            batch.DrawString(Fonts.Arial12, damageModifier.ToString("0")+"%", textPos, Color.White);
            if (packRect.HitTest(mousePos))
                ToolTip.CreateTooltip(Localizer.Token(GameText.IndicatesThisShipsCurrentBonus));

            numStatus++;
        }

        void DrawFTL(SpriteBatch batch, Vector2 mousePos, ref int numStatus)
        {
            SubTexture iconBoosted = ResourceManager.Texture("StatusIcons/icon_boosted");
            if (Ship.FTLModifier < 1f && !Ship.Inhibited)
            {
                DrawIconWithTooltip(batch, iconBoosted,
                    () => $"{Localizer.Token(GameText.FtlSpeedReducedInThis)}{1f-Ship.FTLModifier:P0}\n\nEngine State: {Ship.WarpState}",
                    mousePos, Color.PaleVioletRed, numStatus);
            }

            if (Ship.FTLModifier >= 1f && !Ship.Inhibited && Ship.engineState == Ship.MoveState.Warp)
            {
                DrawIconWithTooltip(batch, iconBoosted,
                    () => $"{Localizer.Token(GameText.FtlSpeedIncreasedInThis)}{Ship.FTLModifier-1f:P0}\n\nEngine State: FTL",
                    mousePos, Color.LightGreen, numStatus);
            }
            numStatus++;
        }

        void DrawEmp(SpriteBatch batch, Vector2 mousePos, ref int numStatus)
        {
            if (!Ship.EMPdisabled)
                return;

            SubTexture iconDisabled = ResourceManager.Texture("StatusIcons/icon_disabled");
            DrawIconWithTooltip(batch, iconDisabled, () => Localizer.Token(GameText.EmpOverloadShipIsDisabled), mousePos,
                Color.White, numStatus);

            var textPos    = new Vector2((int)StatusArea.X + 25 + numStatus * 53, (int)StatusArea.Y);
            float empState = Ship.EMPDamage / Ship.EmpTolerance;
            batch.DrawString(Fonts.Arial12, empState.String(1), textPos, Color.White);
            numStatus++;
        }

        void DrawStructuralIntegrity(SpriteBatch batch, Vector2 mousePos, ref int numStatus)
        {
            if (Ship.InternalSlotsHealthPercent.AlmostEqual(1))
                return;

            SubTexture iconStructure = ResourceManager.Texture("StatusIcons/icon_structure");
            DrawIconWithTooltip(batch, iconStructure, () => Localizer.Token(GameText.StructuralIntegrityOfTheShip), mousePos,
                Color.White, numStatus);

            var textPos              = new Vector2((int)StatusArea.X + 33 + numStatus * 53, (int)StatusArea.Y + 15);
            float structureIntegrity = (1 + (Ship.InternalSlotsHealthPercent - 1) / ShipResupply.ShipDestroyThreshold) * 100;
            structureIntegrity = Math.Max(1, structureIntegrity);
            batch.DrawString(Fonts.Arial12, structureIntegrity.String(0) + "%", textPos, Color.White);
            numStatus++;
        }

        void DrawInhibited(SpriteBatch batch, Vector2 mousePos, ref int numStatus)
        {
            if (!Ship.Inhibited)
                return;

            SubTexture iconGravwell  = ResourceManager.Texture("StatusIcons/icon_gravwell");
            SubTexture iconInhibited = ResourceManager.Texture("StatusIcons/icon_inhibited");
            SubTexture iconFlux      = ResourceManager.Texture("StatusIcons/icon_flux");

            if (Ship.IsInhibitedByUnfriendlyGravityWell) // planet well
            {
                DrawInhibitWarning(batch, numStatus, mousePos);
                DrawIconWithTooltip(batch, iconGravwell, () => Localizer.Token(GameText.IndicatesThatThisShipCannot4), mousePos,
                    Color.White, numStatus);
            }
            else if (RandomEventManager.ActiveEvent == null || !RandomEventManager.ActiveEvent.InhibitWarp) // event
            {
                DrawIconWithTooltip(batch, iconInhibited, () => Localizer.Token(GameText.IndicatesThatThisShipCannot), mousePos,
                    Color.White, numStatus);
            }
            else // ship inhibitor
            {
                DrawIconWithTooltip(batch, iconFlux, () => Localizer.Token(GameText.IndicatesThatThisShipCannot3), mousePos,
                    Color.White, numStatus);
            }

            numStatus++;
        }

        void DrawInhibitWarning(SpriteBatch batch, int numStatus, Vector2 mousePos)
        {
            if (GlobalStats.DisableInhibitionWarning || Screen.showingFTLOverlay)
                return;

            string text     = "Inhibited";
            SpriteFont font = Fonts.Arial20Bold;
            var rect        = new Rectangle((int)StatusArea.X + numStatus * 53, (int)StatusArea.Y - 24,
                        (int)font.MeasureString(text).X, (int)font.MeasureString(text).Y);

            batch.DrawString(Fonts.Arial20Bold, text, rect.PosVec(), Screen.CurrentFlashColorRed);
            if (rect.HitTest(mousePos)) ToolTip.CreateTooltip(GameText.ThisShipIsInhibitedAnd);

            Planet p = Ship.System?.IdentifyGravityWell(Ship);
            if (p == null)
                return;

            Screen.DrawCircleProjected(p.Center, p.GravityWellRadius, Screen.CurrentFlashColorRed);
        }

        void DrawCargoUsed(SpriteBatch batch, Vector2 mousePos, ref int numStatus)
        {
            if (Ship.CargoSpaceUsed.AlmostZero()) 
                return;

            foreach (Cargo cargo in Ship.EnumLoadedCargo())
            {
                SubTexture texture = ResourceManager.Texture("Goods/" + cargo.CargoId);
                var goodRect = new Rectangle((int)StatusArea.X + numStatus * 53, (int)StatusArea.Y, 32, 32);
                batch.Draw(texture, goodRect, Color.White);

                var textPos = new Vector2(goodRect.X + 32, goodRect.Y + 16 - Fonts.Arial12.LineSpacing / 2);
                batch.DrawString(Fonts.Arial12, cargo.Amount.ToString("0"), textPos, Color.White);

                if (goodRect.HitTest(mousePos))
                {
                    Good good = ResourceManager.GoodsDict[cargo.CargoId];
                    ToolTip.CreateTooltip($"{good.Name}\n\n{good.Description}");
                }
                numStatus++;
            }
        }

        void DrawResupplyReason(Ship ship)
        {
            string text = "";
            Color color = Color.Red;
            if (ship.ScuttleTimer > 0)
                text = $"Ship will be Scuttled in {(int)ship.ScuttleTimer} seconds";
            else
                switch (ship.Supply.Resupply(forceSupplyStateCheck: true))
                {
                    case ResupplyReason.NotNeeded:
                        if (ship.HealthPercent < ShipResupply.RepairDoneThreshold && (ship.AI.State == AIState.Resupply || ship.AI.State == AIState.ResupplyEscort))
                            text = $"Repairing Ship by Resupply ({(int)(ship.HealthPercent * 100)}%)";
                        else if (!ship.InCombat && ship.HealthPercent.Less(1))
                        {
                            text = $"Self Repairing Ship ({(int)(ship.HealthPercent * 100)}%)";
                            color = Color.Yellow;
                        }
                        else
                            return;

                        break;
                    case ResupplyReason.LowOrdnanceNonCombat:
                    case ResupplyReason.LowOrdnanceCombat:      text = "Ammo Reserves Critical";           break;
                    case ResupplyReason.NoCommand:              text = "No Command, Cannot Attack";        break;
                    case ResupplyReason.FighterReactorsDamaged: text = "Reactors Damaged";                 break;
                    case ResupplyReason.LowHealth:              text = "Structural Integrity Compromised"; break;
                    case ResupplyReason.LowTroops:
                        text                 = "Need Troops";
                        int numTroopRebasing = ship.NumTroopsRebasingHere;
                        if (numTroopRebasing > 0)
                            text += " (" + numTroopRebasing + " on route)";
                        break;
                }
            var supplyTextPos = new Vector2(Housing.X + 175, Housing.Y + 5);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, supplyTextPos, color);

        }

        void DrawRadiationDamageWarning(Ship ship)
        {
            if (ship.System == null || !ship.System.ShipWithinRadiationRadius(ship) || ship.IsGuardian)
                return;

            var radiationTextPos = new Vector2(Housing.X + 50, Housing.Y - Fonts.Arial12.LineSpacing);
            string text = "Ship is taking radiation damage from a nearby star!";
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, radiationTextPos, Color.Red);
        }
        
        void DrawTroopStatus() // Expanded by Fat Bastard
        {
            var troopPos     = new Vector2(TroopRect.X + TroopRect.Width + 2, TroopRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
            int playerTroops = Ship.NumPlayerTroopsOnShip;
            int enemyTroops  = Ship.NumAiTroopsOnShip;
            int allTroops    = playerTroops + enemyTroops;
            if (Ship.TroopsAreBoardingShip)
            {
                DrawHorizontalValues(enemyTroops, Color.Red, ref troopPos, withSlash: false);
                DrawHorizontalValues(playerTroops, Color.LightGreen, ref troopPos);
            }
            else
            {
                Color statusColor = Ship.loyalty == EmpireManager.Player ? Color.LightGreen : Color.Red;
                DrawHorizontalValues(allTroops, statusColor, ref troopPos, withSlash: false);
            }

            DrawHorizontalValues(Ship.TroopCapacity, Color.White, ref troopPos);
            if (Ship.Carrier.HasActiveTroopBays)
                DrawHorizontalValues(Ship.Carrier.AvailableAssaultShuttles, Color.CadetBlue, ref troopPos);
        }

        void DrawCarrierStatus(Vector2 mousePos)  // Added by Fat Bastard - display hangar status
        {
            if (Ship.Carrier.AllFighterHangars.Length > 0)
            {
                CarrierBays.HangarInfo currentHangarStatus = Ship.Carrier.GrossHangarStatus;
                var hangarRect = new Rectangle(Housing.X + 180, Housing.Y + 210, 26, 20);
                if (hangarRect.HitTest(mousePos))
                    ToolTip.CreateTooltip(Localizer.Token(GameText.ThisShowsTheHangarStatus));

                var hangarTextPos = new Vector2(hangarRect.X + hangarRect.Width + 4, hangarRect.Y + 9 - Fonts.Arial12Bold.LineSpacing / 2);
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_hangar"), hangarRect, Color.White);
                DrawHorizontalValues(currentHangarStatus.Launched, Color.Green, ref hangarTextPos, withSlash: false);
                DrawHorizontalValues(currentHangarStatus.ReadyToLaunch, Color.White, ref hangarTextPos);
                DrawHorizontalValues(currentHangarStatus.Refitting, Color.Red, ref hangarTextPos);
            }
        }

        void DrawHorizontalValues(int value, Color color, ref Vector2 textVector, bool withSlash = true)
        {
            if (withSlash)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "/", textVector, Color.White);
                textVector.X += "/".Length * 4 + 1;
            }
            string text = value.ToString();
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, textVector, color);
            textVector.X += text.Length * 7;
        }
   
        public override bool HandleInput(InputState input)
        {
            if (Screen.SelectedShip == null)
                return false;

            if (FlagRect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(Ship.loyalty.Name);
            }
            
            if (SlidingElement.HandleInput(input))
            {
                State = SlidingElement.Open ? ElementState.TransitionOn : ElementState.TransitionOff;
                return true;
            }

            if (ShipNameArea.ClickableArea.HitTest(input.CursorPosition))
            {
                ShipNameArea.Hover = true;
                if (input.InGameSelect && CanRename)
                    ShipNameArea.HandlingInput = true;
            }
            else
            {
                ShipNameArea.Hover = false;
            }

            if (ShipNameArea.HandlingInput)
            {
                GlobalStats.TakingInput = true;
                ShipNameArea.HandleTextInput(ref Ship.VanityName, input);
                ShipNameArea.Text = Ship.VanityName;
            }
            else
            {
                GlobalStats.TakingInput = false;
            }

            if (GridButton.Rect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(Localizer.Token(GameText.ToggleTheModuleGridOverlay));

            if (GridButton.HandleInput(input))
            {
                if (input.LeftMouseClick)
                {
                    GameAudio.AcceptClick();
                    ShowModules = !ShowModules;
                    GridButton.IsToggled = ShowModules;
                }
                return true;
            }

            if (Ship == null)
                return false;

            if (input.LeftMouseDoubleClick && ShipInfoRect.HitTest(input.CursorPosition))
            {
                Empire.Universe.ViewingShip = false;
                Empire.Universe.AdjustCamTimer = 0.5f;
                Empire.Universe.CamDestination.X = Ship.Center.X;
                Empire.Universe.CamDestination.Y = Ship.Center.Y;
                if (Empire.Universe.viewState < UniverseScreen.UnivScreenState.SystemView)
                    Empire.Universe.CamDestination.Z = Empire.Universe.GetZfromScreenState(UniverseScreen.UnivScreenState.SystemView);
            }

            if (OrderButtonInput(input))
                return true;

            foreach (TippedItem tippedItem in ToolTipItems)
            {
                if (tippedItem.r.HitTest(input.CursorPosition))
                    ToolTip.CreateTooltip(tippedItem.Tooltip);
            }
               
            if (ElementRect.HitTest(input.CursorPosition))
                return true;

            if (State == ElementState.Open)
            {                    
                foreach (OrdersButton ordersButton in Orders)
                {
                    if (ordersButton.HandleInput(input, ScreenManager))
                        return true;
                }
                if (SlidingElement.ButtonHousing.HitTest(input.CursorPosition))
                    return true;
            }
            return false;
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
                OrdersButton resupply = new OrdersButton(Ship, Vector2.Zero, OrderType.OrderResupply, GameText.OrdersSelectedShipOrShips)
                {
                    ValueToModify = new Ref<bool>(() => Ship.DoingResupply, x => Ship.DoingResupply = x)
                };
                Orders.Add(resupply);
            }

            if (!Ship.CanBeScrapped)
                return;

            if (Ship.IsFreighter)
            {
                var ao = new OrdersButton(Ship, Vector2.Zero, OrderType.DefineAO, GameText.AllowsYouToCustomizeAn)
                {
                    ValueToModify = new Ref<bool>(() => Screen.DefiningAO, x => {
                        Screen.DefiningAO = x;
                        Screen.AORect     = Rectangle.Empty;
                    })
                };
                Orders.Add(ao);
                var tradeFood = new OrdersButton(Ship, Vector2.Zero, OrderType.TradeFood, GameText.ManualTradeOrdersThisFreighter2)
                {
                    ValueToModify = new Ref<bool>(() => Ship.TransportingFood),
                };
                Orders.Add(tradeFood);
                var tradeProduction = new OrdersButton(Ship, Vector2.Zero, OrderType.TradeProduction, GameText.ManualTradeOrdersThisFreighter3)
                {
                    ValueToModify = new Ref<bool>(() => Ship.TransportingProduction)
                };
                Orders.Add(tradeProduction);
                var transportColonists = new OrdersButton(Ship, Vector2.Zero, OrderType.TransportColonists, GameText.ManualTradeOrdersThisFreighter)
                {
                    ValueToModify = new Ref<bool>(() => Ship.TransportingColonists)
                };
                Orders.Add(transportColonists);
                var allowInterEmpireTrade = new OrdersButton(Ship, Vector2.Zero, OrderType.AllowInterTrade, GameText.ManualTradeAllowSelectedFreighters)
                {
                    ValueToModify = new Ref<bool>(() => Ship.AllowInterEmpireTrade)
                };
                Orders.Add(allowInterEmpireTrade);
                var tradeRoutes = new OrdersButton(Ship, Vector2.Zero, OrderType.DefineTradeRoutes, GameText.ChooseAListOfPlanets)
                {
                    ValueToModify = new Ref<bool>(() => Screen.DefiningTradeRoutes, x => { Screen.DefiningTradeRoutes = x; })
                };
                Orders.Add(tradeRoutes);
            }
            if (Ship.Carrier.HasTroopBays)
            {
                var ob = new OrdersButton(Ship, Vector2.Zero, OrderType.SendTroops, GameText.SendTroopsToThisShip)
                {
                    ValueToModify = new Ref<bool>(() => Ship.Carrier.SendTroopsToShip)
                };
                Orders.Add(ob);

                var ob2 = new OrdersButton(Ship, Vector2.Zero, OrderType.TroopToggle, GameText.TogglesWhetherThisShipsAssault)
                {
                    ValueToModify = new Ref<bool>(() => Ship.Carrier.TroopsOut, x => {
                        Ship.Carrier.TroopsOut = !Ship.Carrier.TroopsOut;
                    })
                };
                Orders.Add(ob2);
            }
            
            if (Ship.Carrier.HasFighterBays)
            {
                var ob = new OrdersButton(Ship, Vector2.Zero, OrderType.FighterToggle, GameText.WhenActiveAllAvailableFighters)
                {
                    ValueToModify = new Ref<bool>(() => Ship.Carrier.FightersOut, x =>
                    {
                        Ship.Carrier.FightersOut = !Ship.Carrier.FightersOut;
                    })
                };
                Orders.Add(ob);
            }

            if (Ship.shipData.Role != ShipData.RoleName.station && (Ship.Carrier.HasTroopBays || Ship.Carrier.HasFighterBays))
            {
                var ob2 = new OrdersButton(Ship, Vector2.Zero, OrderType.FighterRecall, GameText.ClickToToggleWhetherThis)
                {
                    ValueToModify = new Ref<bool>(() => Ship.Carrier.RecallFightersBeforeFTL, x =>
                        {
                            Ship.Carrier.SetRecallFightersBeforeFTL(x);
                            Ship.ManualHangarOverride = !x;
                        }
                    )
                };
                Orders.Add(ob2);
            }

            if (Ship.shipData.Role >= ShipData.RoleName.fighter && Ship.Mothership == null && Ship.AI.State != AIState.Colonize && Ship.shipData.ShipCategory != ShipData.Category.Civilian)
            {
                var exp = new OrdersButton(Ship, Vector2.Zero, OrderType.Explore, GameText.OrdersThisShipToExplore)
                {
                    ValueToModify = new Ref<bool>(() => Ship.DoingExplore, x => Ship.DoingExplore = x)
                };
                Orders.Add(exp);
                /* FB: does not do anything useful from what i've seen. Disabling it for now
                var systemDefense = new OrdersButton(Ship, Vector2.Zero, OrderType.EmpireDefense, 150)
                {
                    ValueToModify = new Ref<bool>(() => Ship.DoingSystemDefense, x => Ship.DoingSystemDefense = x),
                    Active = false
                };
                Orders.Add(systemDefense);
                */
            }
            if (Ship.CanBeScrapped)
            {
                var rf = new OrdersButton(Ship, Vector2.Zero, OrderType.Refit, GameText.OrderShipRefit)
                {
                    ValueToModify = new Ref<bool>(() => Ship.DoingRefit, x => Ship.DoingRefit = x),
                    Active = false
                };
                Orders.Add(rf);
                var sc = new OrdersButton(Ship, Vector2.Zero, OrderType.Scrap, GameText.OrderShipBackToThe)
                {
                    ValueToModify = new Ref<bool>(() => Ship.DoingScrap, x => Ship.DoingScrap = x),
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
                ob.ClickRect.X = ElementRect.X + ElementRect.Width + 2 + 52 * ex;
                ob.ClickRect.Y = SlidingElement.Housing.Y + 15 + y * 52;
                y++;
            }
        }

        private struct TippedItem
        {
            public Rectangle r;
            public LocalizedText Tooltip;
        }
    }
}
