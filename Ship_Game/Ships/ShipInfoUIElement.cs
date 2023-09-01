using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.CombatTactics.UI;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.Ships
{
    public sealed class ShipInfoUIElement : UIElement
    {
        public ShipStanceButtons OrdersButtons;
        private readonly Array<TippedItem> ToolTipItems = new Array<TippedItem>();
        public Array<OrdersButton> Orders = new Array<OrdersButton>();

        private readonly UniverseScreen Universe;
        Empire Player => Universe.Player;
        Ship Ship;
        private readonly Selector Sel;
        public Rectangle LeftRect;
        public Rectangle RightRect;
        public Rectangle Housing;
        public Rectangle ShipInfoRect;
        public ToggleButton GridButton;
        public Rectangle Power;
        public Rectangle Shields;
        public Rectangle Ordnance;
        public Rectangle ConstructionRect;
        private readonly ProgressBar PBar;
        private readonly ProgressBar SBar;
        private readonly ProgressBar OBar;
        private readonly ProgressBar ConstructionBar;
        UITextEntry ShipNameArea;
        private readonly SlidingElement SlidingElement;
        private readonly Rectangle DefenseRect;
        private readonly Rectangle TroopRect;
        private readonly Rectangle FlagRect;  //fbedard
        private bool CanRename   = true;
        private bool ShowModules = true;
        private Vector2 StatusArea;

        public ShipInfoUIElement(Rectangle r, ScreenManager sm, UniverseScreen universe)
        {
            Universe = universe;
            ScreenManager = sm;
            ElementRect = r;
            FlagRect = new Rectangle(r.X + 365, r.Y + 71, 18, 18);
            Sel = new Selector(r, Color.Black);
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            SlidingElement = new SlidingElement(new Rectangle(r.X - 100, r.Y + r.Height - 140, 530, 130));
            Housing = r;
            LeftRect = new Rectangle(r.X, r.Y + 44, 180, r.Height - 44);
            RightRect = new Rectangle(LeftRect.X + LeftRect.Width, LeftRect.Y, 220, LeftRect.Height);
            int spacing = 2;
            ShipNameArea = new UITextEntry(Housing.X + 41, Housing.Y + 65, 200, Fonts.Arial14Bold, "");
            ShipNameArea.OnTextChanged = (text) =>
            {
                if (Ship != null)
                    Ship.VanityName = text;
            };
            ShipNameArea.Color = tColor;
            
            Power = new Rectangle(Housing.X + 187, Housing.Y + 110, 20, 20);
            PBar = new ProgressBar(Power.X + Power.Width + 15, Power.Y, 150, 18) { color = "green" };
            ToolTipItems.Add(new TippedItem(Power, GameText.IndicatesThisShipsCurrentPower));

            Shields = new Rectangle(Housing.X + 187, Housing.Y + 110 + 20 + spacing, 20, 20);
            SBar = new ProgressBar(Shields.X + Shields.Width + 15, Shields.Y, 150, 18) { color = "blue" };
            ToolTipItems.Add(new TippedItem(Shields, GameText.IndicatesTheTotalPowerOf));

            Ordnance = new Rectangle(Housing.X + 187, Housing.Y + 110 + 20 + spacing + 20 + spacing, 20, 20);
            OBar = new ProgressBar(Ordnance.X + Ordnance.Width + 15, Ordnance.Y, 150, 18);
            ToolTipItems.Add(new TippedItem(Ordnance, GameText.IndicatesThisShipsCurrentStores));

            ConstructionRect = new Rectangle(Housing.X + 187, Housing.Y + 110 + 20 + spacing*3 + 60, 20, 20);
            ConstructionBar = new ProgressBar(ConstructionRect.X + ConstructionRect.Width + 15, ConstructionRect.Y, 150, 18);

            DefenseRect = new Rectangle(Housing.X + 13, Housing.Y + 112, 22, 22);
            ToolTipItems.Add(new TippedItem(DefenseRect, GameText.IndicatesThisShipsCurrentDefense));
            
            TroopRect = new Rectangle(Housing.X + 13, Housing.Y + 137, 22, 22);
            ToolTipItems.Add(new TippedItem(TroopRect, GameText.IndicatesTheNumberOfTroops));

            ShipInfoRect = new Rectangle(Housing.X + 60, Housing.Y + 110, 115, 115);
            Vector2 gridRect = new Vector2(Housing.X + 16, Universe.ScreenHeight - 45);
            GridButton = new ToggleButton(gridRect, ToggleButtonStyle.Grid, "SelectionBox/icon_grid")
            {
                IsToggled = true
            };

            float startX = OBar.pBar.X - 15;
            var ordersBarPos = new Vector2(startX, (Ordnance.Y + Ordnance.Height + spacing + 3));

            OrdersButtons = new ShipStanceButtons(universe, ordersBarPos);
        }

        void DrawOrderButtons(SpriteBatch batch, float transitionOffset)
        {
            foreach (OrdersButton ob in Orders)
            {
                Rectangle r = ob.ClickRect;
                r.X -= (int)(transitionOffset * 300f);
                ob.Draw(batch, ScreenManager.input.CursorPosition, r);
            }
        }
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Ship s = Ship;
            if (Universe.SelectedShip == null || s?.ShipData == null)
                return;  //fbedard

            float transitionOffset = 0f.SmoothStep(1f, TransitionPosition);
            int columns = Orders.Count / 2 + Orders.Count % 2;
            SlidingElement.Draw(ScreenManager, (int)(columns * 55 * (1f - TransitionPosition)) + (SlidingElement.Open ? 20 - columns : 0));

            DrawOrderButtons(batch, transitionOffset);
            batch.Draw(ResourceManager.Texture("SelectionBox/unitselmenu_main"), Housing, Color.White);
            GridButton.Draw(batch, elapsed);

            Vector2 namePos       = new(Housing.X + 30, Housing.Y + 63);
            Vector2 shipSuperName = new(Housing.X + 30, Housing.Y + 79);
            ShipNameArea.SetPos(namePos);
            ShipNameArea.Draw(batch, elapsed);

            //Added by McShooterz:
            string longName = s.Name + " - " + Localizer.GetRole(s.DesignRole, s.Loyalty);
            if (s.ShipData.ShipCategory != ShipCategory.Unclassified)
                longName += " - "+s.ShipData.ShipCategory;

            batch.DrawString(Fonts.Visitor10, longName, shipSuperName, Color.Orange);

            var shipStatus = new Vector2(Sel.Rect.X + Sel.Rect.Width - 168, Housing.Y + 64).ToFloored();
            string text = Fonts.TahomaBold9.ParseText(ShipListScreenItem.GetStatusText(s), 120);
            batch.DrawString(Fonts.TahomaBold9, text, shipStatus, tColor);

            s.RenderOverlay(batch, ShipInfoRect, ShowModules);
            batch.Draw(ResourceManager.Texture("Modules/NuclearReactorMedium"), Power, Color.White);
            batch.Draw(ResourceManager.Texture("Modules/Shield_1KW"), Shields, Color.White);
            batch.Draw(ResourceManager.Texture("Modules/Ordnance"), Ordnance, Color.White);

            PBar.Max      = s.PowerStoreMax;
            PBar.Progress = s.PowerCurrent;
            SBar.Max      = s.ShieldMax;
            SBar.Progress = s.ShieldPower;
            OBar.Max      = s.OrdinanceMax;
            OBar.Progress = s.Ordinance;
            PBar.Draw(batch);
            SBar.Draw(batch);
            OBar.Draw(batch);
            batch.Draw(ResourceManager.Texture("UI/icon_shield"), DefenseRect, Color.White);
            var defPos = new Vector2(DefenseRect.X + DefenseRect.Width + 2, DefenseRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
            float totalBoardingDefense = s.MechanicalBoardingDefense + s.TroopBoardingDefense;
            batch.DrawString(Fonts.Arial12Bold, totalBoardingDefense.String(0), defPos, Color.White);
            batch.Draw(ResourceManager.Texture("UI/icon_troop_shipUI"), TroopRect, Color.White);
            DrawTroopStatus(s);
            OrdersButtons.Draw(batch, elapsed);
            //fbedard: Display race icon
            batch.Draw(ResourceManager.Flag(s.Loyalty), FlagRect, s.Loyalty.EmpireColor);

            Vector2 mousePos = Universe.Input.CursorPosition;

            //Added by McShooterz: new experience level display
            var star     = new Rectangle(TroopRect.X, TroopRect.Y + 23, 22, 22);
            var levelPos = new Vector2(star.X + star.Width + 2, star.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
            batch.Draw(ResourceManager.Texture("UI/icon_experience_shipUI"), star, Color.White);
            batch.DrawString(Fonts.Arial12Bold, s.Level.ToString(), levelPos, Color.White);
            if (star.HitTest(mousePos))
                ToolTip.CreateTooltip(GameText.IndicatesAShipsExperienceLevel2);

            //Added by McShooterz: kills display
            star       = new Rectangle(star.X, star.Y + 19, 22, 22);
            levelPos   = new Vector2(star.X + star.Width + 2, star.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
            StatusArea = new Vector2(Housing.X + 175, Housing.Y + 15);
            batch.Draw(ResourceManager.Texture("UI/icon_kills_shipUI"), star, Color.White);
            batch.DrawString(Fonts.Arial12Bold, s.Kills.ToString(), levelPos, Color.White);
            int numStatus = 0;

            // FB - limit data display to non player ships
            if (HelperFunctions.DataVisibleToPlayer(s.Loyalty))
            {
                DrawConstructionStatus(batch);
                DrawCarrierStatus(mousePos, s);
                DrawResupplyReason(batch, s);
                DrawRadiationDamageWarning(s);
                DrawPack(batch, mousePos, s, ref numStatus);
                DrawFTL(batch, mousePos, s, ref numStatus);
                DrawInhibited(batch, mousePos, s, ref numStatus);
                DrawEmp(batch, mousePos, s, ref numStatus);
                DrawStructuralIntegrity(batch, mousePos, s, ref numStatus);
            }
            DrawCargoUsed(batch, mousePos, s, ref numStatus);
        }

        void DrawIconWithTooltip(SpriteBatch batch, SubTexture icon, Func<string> tooltip, Vector2 mousePos, Color color, int numStatus)
        {
            var rect = new Rectangle((int)StatusArea.X + numStatus * 53, (int)StatusArea.Y, 48, 32);
            batch.Draw(icon, rect, color);
            if (rect.HitTest(mousePos)) ToolTip.CreateTooltip(tooltip());
        }

        void DrawPack(SpriteBatch batch, Vector2 mousePos, Ship ship, ref int numStatus)
        {
            SubTexture iconPack = ResourceManager.Texture("StatusIcons/icon_pack");

            if (!ship.Loyalty.HavePackMentality)
                return;

            var packRect = new Rectangle((int)StatusArea.X, (int)StatusArea.Y, 48, 32);
            batch.Draw(iconPack, packRect, Color.White);
            var textPos          = new Vector2(packRect.X + 26, packRect.Y + 15);
            float damageModifier = ship.PackDamageModifier * 100f;
            batch.DrawString(Fonts.Arial12, damageModifier.ToString("0")+"%", textPos, Color.White);
            if (packRect.HitTest(mousePos))
                ToolTip.CreateTooltip(Localizer.Token(GameText.IndicatesThisShipsCurrentBonus));

            numStatus++;
        }

        void DrawFTL(SpriteBatch batch, Vector2 mousePos, Ship ship, ref int numStatus)
        {
            SubTexture iconBoosted = ResourceManager.Texture("StatusIcons/icon_boosted");
            if (ship.FTLModifier < 1f && !ship.Inhibited)
            {
                DrawIconWithTooltip(batch, iconBoosted,
                    () => $"{Localizer.Token(GameText.FtlSpeedReducedInThis)}{1f-ship.FTLModifier:P0}\n\nEngine State: {ship.WarpState}",
                    mousePos, Color.PaleVioletRed, numStatus);
            }

            if (ship.FTLModifier >= 1f && !ship.Inhibited && ship.engineState == Ship.MoveState.Warp)
            {
                DrawIconWithTooltip(batch, iconBoosted,
                    () => $"{Localizer.Token(GameText.FtlSpeedIncreasedInThis)}{ship.FTLModifier-1f:P0}\n\nEngine State: FTL",
                    mousePos, Color.LightGreen, numStatus);
            }
            numStatus++;
        }

        void DrawEmp(SpriteBatch batch, Vector2 mousePos, Ship ship, ref int numStatus)
        {
            if (!ship.EMPDisabled)
                return;

            SubTexture iconDisabled = ResourceManager.Texture("StatusIcons/icon_disabled");
            DrawIconWithTooltip(batch, iconDisabled, () => Localizer.Token(GameText.EmpOverloadShipIsDisabled), mousePos,
                Color.White, numStatus);

            var textPos    = new Vector2((int)StatusArea.X + 25 + numStatus * 53, (int)StatusArea.Y);
            float empState = ship.EMPDamage / ship.EmpTolerance;
            batch.DrawString(Fonts.Arial12, empState.String(1), textPos, Color.White);
            numStatus++;
        }

        void DrawStructuralIntegrity(SpriteBatch batch, Vector2 mousePos, Ship ship, ref int numStatus)
        {
            if (ship.HealthPercent > 0.999f)
                return;

            SubTexture iconStructure = ResourceManager.Texture("StatusIcons/icon_structure");
            DrawIconWithTooltip(batch, iconStructure, () => Localizer.Token(GameText.StructuralIntegrityOfTheShip), mousePos,
                Color.White, numStatus);

            var textPos = new Vector2((int)StatusArea.X + 33 + numStatus * 53, (int)StatusArea.Y + 15);
            int health = (int)(ship.HealthPercent * 100);

            float repairPerSec = ship.CurrentRepairPerSecond;
            float timeUntilRepaired = (ship.HealthMax - ship.Health) / repairPerSec;
            string integrityText = $"{health}% (+{(int)repairPerSec}HP/s ETA:{timeUntilRepaired.TimeString()})";
            batch.DrawString(Fonts.Arial12, integrityText, textPos, Color.White);
            numStatus++;
        }

        void DrawInhibited(SpriteBatch batch, Vector2 mousePos, Ship ship, ref int numStatus)
        {
            if (!ship.Inhibited)
                return;

            SubTexture icon;
            GameText text;

            switch (ship.InhibitionSource)
            {
                case Ship.InhibitionType.GlobalEvent:
                    {
                        icon = ResourceManager.Texture("StatusIcons/icon_flux");
                        text = GameText.IndicatesThatThisShipCannot4;
                        break;
                    }
                case Ship.InhibitionType.GravityWell:
                    {
                        icon = ResourceManager.Texture("StatusIcons/icon_gravwell");
                        text = GameText.IndicatesThatThisShipCannot4;
                        DrawInhibitWarning(batch, numStatus, mousePos, ship);
                        break;
                    }
                case Ship.InhibitionType.EnemyShip:
                    {
                        icon = ResourceManager.Texture("StatusIcons/icon_inhibited");
                        text = GameText.IndicatesThatThisShipCannot3;
                        break;
                    }
                default:
                    return;
            }

            DrawIconWithTooltip(batch, icon, () => Localizer.Token(text), mousePos, Color.White, numStatus);
            numStatus++;
        }

        void DrawInhibitWarning(SpriteBatch batch, int numStatus, Vector2 mousePos, Ship ship)
        {
            if (Universe.UState.P.DisableInhibitionWarning || Universe.ShowingFTLOverlay)
                return;

            string text = "Inhibited";
            Graphics.Font font = Fonts.Arial20Bold;
            RectF rect = new(StatusArea.X + numStatus * 53, StatusArea.Y - 24, font.MeasureString(text));

            batch.DrawString(font, text, rect.Pos, Universe.CurrentFlashColorRed);
            if (rect.HitTest(mousePos))
                ToolTip.CreateTooltip(GameText.ThisShipIsInhibitedAnd);

            Planet p = ship.System?.IdentifyGravityWell(ship);
            if (p != null)
                Universe.DrawCircleProjected(p.Position, p.GravityWellRadius, Universe.CurrentFlashColorRed);
        }

        void DrawCargoUsed(SpriteBatch batch, Vector2 mousePos, Ship ship, ref int numStatus)
        {
            if (ship.CargoSpaceUsed.AlmostZero()) 
                return;

            foreach (Cargo cargo in ship.EnumLoadedCargo())
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

        void DrawResupplyReason(SpriteBatch batch, Ship ship)
        {
            string text = "";
            Color color = Color.Red;
            if (ship.ScuttleTimer > 0)
                text = $"Ship will be Scuttled in {(int)ship.ScuttleTimer} seconds";
            else
                switch (ship.Supply.Resupply())
                {
                    case ResupplyReason.NotNeeded:
                        if (ship.HealthPercent < ShipResupply.RepairDoneThreshold && (ship.AI.State == AIState.Resupply || ship.AI.State == AIState.ResupplyEscort))
                        {
                            text = $"Repairing Ship by Resupply ({(int)(ship.HealthPercent * 100)}%)";
                        }
                        else if (ship.CanRepair && ship.HealthPercent < 1f)
                        {
                            text = $"Self Repairing Ship ({(int)(ship.HealthPercent * 100)}%)";
                            color = Color.Yellow;
                        }
                        else
                        {
                            return;
                        }
                        break;
                    case ResupplyReason.LowOrdnanceNonCombat:
                    case ResupplyReason.LowOrdnanceCombat:      text = "Ammo Reserves Critical";           break;
                    case ResupplyReason.NoCommand:              text = "No Command, Cannot Attack";        break;
                    case ResupplyReason.FighterReactorsDamaged: text = "Reactors Damaged";                 break;
                    case ResupplyReason.LowHealth:              text = "Structural Integrity Compromised"; break;
                    case ResupplyReason.LowTroops:
                        text = "Need Troops";
                        int numTroopRebasing = ship.NumTroopsRebasingHere;
                        if (numTroopRebasing > 0)
                            text += " (" + numTroopRebasing + " on route)";
                        break;
                }
            var supplyTextPos = new Vector2(Housing.X + 175, Housing.Y + 5);
            batch.DrawString(Fonts.Arial12, text, supplyTextPos, color);
        }

        void DrawRadiationDamageWarning(Ship ship)
        {
            if (ship.System == null || !ship.System.ShipWithinRadiationRadius(ship) || ship.IsGuardian)
                return;

            var radiationTextPos = new Vector2(Housing.X + 50, Housing.Y - Fonts.Arial12.LineSpacing);
            string text = "Ship is taking radiation damage from a nearby star!";
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, radiationTextPos, Color.Red);
        }

        void DrawTroopStatus(Ship s) // Expanded by Fat Bastard
        {
            var troopPos     = new Vector2(TroopRect.X + TroopRect.Width + 2, TroopRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2);
            int playerTroops = s.NumPlayerTroopsOnShip;
            int enemyTroops  = s.NumAiTroopsOnShip;
            int allTroops    = playerTroops + enemyTroops;
            if (s.TroopsAreBoardingShip)
            {
                DrawHorizontalValues(enemyTroops, Color.Red, ref troopPos, withSlash: false);
                DrawHorizontalValues(playerTroops, Color.LightGreen, ref troopPos);
            }
            else
            {
                Color statusColor = s.Loyalty == Player ? Color.LightGreen : Color.Red;
                DrawHorizontalValues(allTroops, statusColor, ref troopPos, withSlash: false);
            }

            DrawHorizontalValues(s.TroopCapacity, Color.White, ref troopPos);
            if (s.Carrier.HasActiveTroopBays)
                DrawHorizontalValues(s.Carrier.AvailableAssaultShuttles, Color.CadetBlue, ref troopPos);
        }

        void DrawConstructionStatus(SpriteBatch batch)
        {
            if (Ship.IsConstructing)
            {
                ConstructionBar.Max = Ship.Construction.ConstructionNeeded;
                ConstructionBar.Progress = Ship.Construction.ConstructionAdded;
                ConstructionBar.Draw(batch);
                batch.Draw(ResourceManager.Texture("NewUI/icon_production"), ConstructionRect, Color.White);
            }
        }

        void DrawCarrierStatus(Vector2 mousePos, Ship ship)  // Added by Fat Bastard - display hangar status
        {
            if (ship.Carrier.AllFighterHangars?.Length > 0)
            {
                CarrierBays.HangarInfo currentHangarStatus = ship.Carrier.GrossHangarStatus;
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
            if (Universe.SelectedShip == null || Universe.LookingAtPlanet)
            {
                ShipNameArea.StopInput();
                return false;
            }

            if (SlidingElement.HandleInput(input))
            {
                State = SlidingElement.Open ? ElementState.TransitionOn : ElementState.TransitionOff;
                return true;
            }

            if (ShipNameArea.HandleInput(input))
                return true;

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
            
            Ship s = Ship;
            if (s == null)
                return false;
            
            if (FlagRect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip(s.Loyalty.Name);
            }

            if (input.LeftMouseDoubleClick && ShipInfoRect.HitTest(input.CursorPosition))
            {
                // TODO: should not modify UniverseScreen state directly
                Universe.ViewingShip = false;
                Universe.AdjustCamTimer = 0.5f;
                Universe.CamDestination.X = s.Position.X;
                Universe.CamDestination.Y = s.Position.Y;
                if (Universe.viewState < UniverseScreen.UnivScreenState.SystemView)
                    Universe.CamDestination.Z = Universe.GetZfromScreenState(UniverseScreen.UnivScreenState.SystemView);
            }
            if (OrdersButtons.HandleInput(input))
                return true;
            foreach (TippedItem tippedItem in ToolTipItems)
            {
                if (tippedItem.Rect.HitTest(input.CursorPosition))
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
            if (Ship == s || s == null)
                return;

            Ship = s;
            CanRename = s.Loyalty == Player;
            ShipNameArea.Enabled = CanRename;
            ShipNameArea.Reset(s.ShipName);

            Orders.Clear();
            OrdersButtons.ResetButtons(s);
            if (s.Loyalty != Player)
                return;

            if (s.AI.OrderQueue.TryPeekLast(out var goal) && goal.Plan == ShipAI.Plan.DeployStructure)
                return;

            if (s.ShipData.Role > RoleName.station && !s.IsConstructor)
            {
                OrdersButton resupply = new(s, OrderType.OrderResupply, GameText.OrdersSelectedShipOrShips)
                {
                    ValueToModify = new(() => s.DoingResupply, x => s.DoingResupply = x)
                };
                Orders.Add(resupply);
            }

            if (s.IsFreighter)
            {
                var ao = new OrdersButton(s, OrderType.DefineAO, GameText.AllowsYouToCustomizeAn)
                {
                    ValueToModify = new(() => Universe.DefiningAO, x => {
                        Universe.DefiningAO = x;
                        Universe.AORect = Rectangle.Empty;
                    })
                };
                Orders.Add(ao);
                var tradeFood = new OrdersButton(s, OrderType.TradeFood, GameText.ManualTradeOrdersThisFreighter2)
                {
                    ValueToModify = new(() => s.TransportingFood),
                };
                Orders.Add(tradeFood);
                var tradeProduction = new OrdersButton(s, OrderType.TradeProduction, GameText.ManualTradeOrdersThisFreighter3)
                {
                    ValueToModify = new(() => s.TransportingProduction)
                };
                Orders.Add(tradeProduction);
                var transportColonists = new OrdersButton(s, OrderType.TransportColonists, GameText.ManualTradeOrdersThisFreighter)
                {
                    ValueToModify = new(() => s.TransportingColonists)
                };
                Orders.Add(transportColonists);
                var allowInterEmpireTrade = new OrdersButton(s, OrderType.AllowInterTrade, GameText.ManualTradeAllowSelectedFreighters)
                {
                    ValueToModify = new(() => s.AllowInterEmpireTrade)
                };
                Orders.Add(allowInterEmpireTrade);
                var tradeRoutes = new OrdersButton(s, OrderType.DefineTradeRoutes, GameText.ChooseAListOfPlanets)
                {
                    ValueToModify = new(() => Universe.DefiningTradeRoutes, x => { Universe.DefiningTradeRoutes = x; })
                };
                Orders.Add(tradeRoutes);
            }
            if (s.Carrier.HasTroopBays)
            {
                var ob = new OrdersButton(s, OrderType.SendTroops, GameText.SendTroopsToThisShip)
                {
                    ValueToModify = new(() => s.Carrier.SendTroopsToShip)
                };
                Orders.Add(ob);

                var ob2 = new OrdersButton(s, OrderType.TroopToggle, GameText.TogglesWhetherThisShipsAssault)
                {
                    ValueToModify = new(() => s.Carrier.TroopsOut, x => {
                        s.Carrier.TroopsOut = !s.Carrier.TroopsOut;
                    }),
                    RightClickValueToModify = new(() => s.Carrier.AllowBoardShip, x => {
                        s.Carrier.AllowBoardShip = !s.Carrier.AllowBoardShip;
                    }),
                };
                Orders.Add(ob2);
            }

            if (s.Carrier.HasFighterBays)
            {
                var ob = new OrdersButton(s, OrderType.FighterToggle, GameText.WhenActiveAllAvailableFighters)
                {
                    ValueToModify = new(() => s.Carrier.FightersOut, x =>
                    {
                        s.Carrier.FightersOut = !s.Carrier.FightersOut;
                    })
                };
                Orders.Add(ob);
            }

            if (s.ShipData.Role != RoleName.station && (s.Carrier.HasTroopBays || s.Carrier.HasFighterBays))
            {
                var ob2 = new OrdersButton(s, OrderType.FighterRecall, GameText.ClickToToggleWhetherThis)
                {
                    ValueToModify = new(() => s.Carrier.RecallFightersBeforeFTL, x =>
                        {
                            s.Carrier.SetRecallFightersBeforeFTL(x);
                            s.ManualHangarOverride = !x;
                        }
                    )
                };
                Orders.Add(ob2);
            }

            if (s.ShipData.Role >= RoleName.fighter && s.Mothership == null && s.AI.State != AIState.Colonize && s.ShipData.ShipCategory != ShipCategory.Civilian)
            {
                var exp = new OrdersButton(s, OrderType.Explore, GameText.OrdersThisShipToExplore)
                {
                    ValueToModify = new(() => s.DoingExplore, x => s.DoingExplore = x)
                };
                Orders.Add(exp);
            }
            if (s.CanBeScrapped)
            {
                if (!s.IsConstructor)
                {
                    var rf = new OrdersButton(s, OrderType.Refit, GameText.OrderShipRefit)
                    {
                        ValueToModify = new(() => s.DoingRefit, x => s.DoingRefit = x),
                        Active = false
                    };
                    Orders.Add(rf);
                }
                var sc = new OrdersButton(s, OrderType.Scrap, GameText.OrderShipBackToThe)
                {
                    ValueToModify = new(() => s.DoingScrap, x => s.DoingScrap = x),
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
    }
}
