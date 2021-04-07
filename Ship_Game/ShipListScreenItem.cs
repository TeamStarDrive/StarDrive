using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class ShipListScreenItem : ScrollListItem<ShipListScreenItem>
    {
        public Ship Ship;

        public Rectangle TotalEntrySize;
        public Rectangle SysNameRect;
        public Rectangle ShipNameRect;
        public Rectangle RoleRect;
        public Rectangle FleetRect;
        public Rectangle OrdersRect;
        public Rectangle RefitRect;
        public Rectangle StrRect;
        public Rectangle MaintRect;
        public Rectangle TroopRect;
        public Rectangle FTLRect;
        public Rectangle STLRect;
        public Rectangle RemainderRect;

        Rectangle ShipIconRect;
        readonly UITextEntry ShipNameEntry = new UITextEntry();
        readonly TexturedButton RefitButton;
        readonly TexturedButton ScrapButton;
        readonly TexturedButton ExploreButton; //Auto-explore button for ShipListScreen

        public ShipListScreen Screen;
        public string StatusText;
        readonly bool IsScuttle;
        readonly bool IsCombat;  //fbedard
        public bool Selected = false;  //fbedard: for multi-select
        private readonly string SystemName;
        private readonly SpriteFont Font12 = Fonts.Arial12Bold;
        private readonly SpriteFont Font8  = Fonts.Arial8Bold;

        public ShipListScreenItem(Ship s, int x, int y, int width1, int height, ShipListScreen caller)
        {
            Screen = caller;
            Ship = s;
            TotalEntrySize = new Rectangle(x, y, width1 - 60, height);
            SysNameRect = new Rectangle(x, y, (int)(TotalEntrySize.Width * 0.10f), height);
            ShipNameRect = new Rectangle(x + SysNameRect.Width, y, (int)(TotalEntrySize.Width * 0.175f), height);
            RoleRect = new Rectangle(x + SysNameRect.Width + ShipNameRect.Width, y, (int)(TotalEntrySize.Width * 0.05f), height);
            FleetRect = new Rectangle(x + SysNameRect.Width + ShipNameRect.Width, y, (int)(TotalEntrySize.Width * 0.075f), height);
            OrdersRect = new Rectangle(x + SysNameRect.Width + ShipNameRect.Width + RoleRect.Width + FleetRect.Width, y, (int)(TotalEntrySize.Width * 0.175f), height);
            RefitRect = new Rectangle(OrdersRect.X + OrdersRect.Width, y, 125, height);
            StrRect = new Rectangle(RefitRect.X + RefitRect.Width, y, 60, height);
            MaintRect = new Rectangle(StrRect.X + StrRect.Width, y, 60, height);
            TroopRect = new Rectangle(MaintRect.X + MaintRect.Width, y, 60, height);
            FTLRect = new Rectangle(TroopRect.X + TroopRect.Width, y, 60, height);
            STLRect = new Rectangle(FTLRect.X + FTLRect.Width, y, 60, height);
            StatusText = GetStatusText(Ship);
            ShipIconRect = new Rectangle(ShipNameRect.X + 5, ShipNameRect.Y + 2, 28, 28);
            string shipName = !string.IsNullOrEmpty(Ship.VanityName) ? Ship.VanityName : Ship.Name;
            SystemName = Ship.System?.Name ?? Localizer.Token(GameText.DeepSpace);
            ShipNameEntry.ClickableArea = new Rectangle(ShipIconRect.X + ShipIconRect.Width + 10, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2, (int)Fonts.Arial20Bold.MeasureString(shipName).X, Fonts.Arial20Bold.LineSpacing);
            ShipNameEntry.Text = shipName;
            float width = (int)(OrdersRect.Width * 0.8f);
            while (width % 10f != 0f)
            {
                width += 1f;
            }

            if (!Ship.IsPlatformOrStation && !Ship.IsHangarShip 
                                          && Ship.shipData.Role != ShipData.RoleName.troop 
                                          && Ship.AI.State != AIState.Colonize 
                                          && Ship.shipData.Role != ShipData.RoleName.freighter 
                                          && Ship.shipData.ShipCategory != ShipData.Category.Civilian)
                IsCombat = true;

            Rectangle refit = new Rectangle(RefitRect.X + RefitRect.Width / 2 - 5 - ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover1").Width, RefitRect.Y + RefitRect.Height / 2 - ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2").Height / 2, ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2").Width, ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2").Height);

            if (IsCombat)
            {
                ExploreButton = new TexturedButton(refit, "NewUI/icon_order_explore", "NewUI/icon_order_explore_hover1", "NewUI/icon_order_explore_hover2");
                //PatrolButton = new TexturedButton(refit, "NewUI/icon_order_patrol", "NewUI/icon_order_patrol_hover1", "NewUI/icon_order_patrol_hover2");
            }
            RefitButton = new TexturedButton(refit, "NewUI/icon_queue_rushconstruction", "NewUI/icon_queue_rushconstruction_hover1", "NewUI/icon_queue_rushconstruction_hover2");			
            ScrapButton = new TexturedButton(refit, "NewUI/icon_queue_delete", "NewUI/icon_queue_delete_hover1", "NewUI/icon_queue_delete_hover2");

            if (Ship.IsPlatformOrStation || Ship.Thrust <= 0f)
            {
                IsScuttle = true;
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            SetNewPos((int)X, (int)Y);

            if (Selected)
            {
                batch.FillRectangle(TotalEntrySize, Color.DarkGreen);
            }

            var textColor = Colors.Cream;

            if (Fonts.Arial20Bold.MeasureString(SystemName).X <= SysNameRect.Width)
            {
                var sysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - Font12.MeasureString(SystemName).X / 2f, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Font12.LineSpacing / 2);
                batch.DrawString(Font12, SystemName, sysNameCursor, textColor);
            }
            else
            {
                var sysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - Font12.MeasureString(SystemName).X / 2f, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Font12.LineSpacing / 2);
                batch.DrawString(Font12, SystemName, sysNameCursor, textColor);
            }

            batch.Draw(Ship.shipData.Icon, ShipIconRect, Color.White);
            ShipNameEntry.Draw(batch, elapsed, Font12, ShipNameEntry.ClickableArea.PosVec(), textColor);

            var rolePos = new Vector2(RoleRect.X + RoleRect.Width / 2 - Font12.MeasureString(Localizer.GetRole(Ship.shipData.Role, Ship.loyalty)).X / 2f, RoleRect.Y + RoleRect.Height / 2 - Font12.LineSpacing / 2);
            HelperFunctions.ClampVectorToInt(ref rolePos);
            batch.DrawString(Font12, Localizer.GetRole(Ship.shipData.Role, Ship.loyalty), rolePos, textColor);

            string fleetName     = Ship.fleet?.Name ?? "";
            SpriteFont fleetFont = Font12.MeasureString(fleetName).X > FleetRect.Width - 5 ? Font8 : Font12;
            var fleetPos = new Vector2(FleetRect.X + FleetRect.Width / 2 - fleetFont.MeasureString(fleetName).X / 2f, FleetRect.Y + FleetRect.Height / 2 - fleetFont.LineSpacing / 2);
            HelperFunctions.ClampVectorToInt(ref fleetPos);
            batch.DrawString(fleetFont, fleetName, fleetPos, textColor);

            var statusPos = new Vector2(OrdersRect.X + OrdersRect.Width / 2 - Fonts.Arial12.MeasureString(StatusText).X / 2f, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial12.MeasureString(StatusText).Y / 2f);
            HelperFunctions.ClampVectorToInt(ref statusPos);
            batch.DrawString(Fonts.Arial12, StatusText, statusPos, textColor);

            float maint = Ship.GetMaintCost();

            var mainPos = new Vector2(MaintRect.X + MaintRect.Width / 2, MaintRect.Y + MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            mainPos.X -= Fonts.Arial12.MeasureString(maint.ToString("F2")).X / 2f + 6;
            HelperFunctions.ClampVectorToInt(ref mainPos);
            batch.DrawString(Fonts.Arial12, maint.ToString("F2"), mainPos, maint > 0.00 ? Color.Salmon : Color.White);

            var strPos = new Vector2(StrRect.X + StrRect.Width / 2, MaintRect.Y + MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            strPos.X -= Fonts.Arial12.MeasureString(Ship.GetStrength().ToString("0")).X / 2f + 6;
            HelperFunctions.ClampVectorToInt(ref strPos);
            batch.DrawString(Fonts.Arial12, Ship.GetStrength().ToString("0"), strPos, Color.White);

            var troopPos = new Vector2(TroopRect.X + TroopRect.Width / 2f, MaintRect.Y + MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            troopPos.X -= Fonts.Arial12.MeasureString(string.Concat(Ship.TroopCount, "/", Ship.TroopCapacity)).X / 2f + 6;
            HelperFunctions.ClampVectorToInt(ref troopPos);
            batch.DrawString(Fonts.Arial12, string.Concat(Ship.TroopCount, "/", Ship.TroopCapacity), troopPos, Color.White);
            
            var ftlPos = new Vector2(FTLRect.X + FTLRect.Width / 2f, MaintRect.Y + MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            ftlPos.X -= Fonts.Arial12.MeasureString((Ship.MaxFTLSpeed / 1000f).ToString("0")+"k").X / 2f + 6;
            HelperFunctions.ClampVectorToInt(ref ftlPos);
            batch.DrawString(Fonts.Arial12, (Ship.MaxFTLSpeed / 1000f).ToString("0")+"k", ftlPos, Color.White);

            var stlPos = new Vector2(STLRect.X + STLRect.Width / 2f, MaintRect.Y + MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            stlPos.X -= Fonts.Arial12.MeasureString(Ship.MaxSTLSpeed.ToString("0")).X / 2f + 6;
            HelperFunctions.ClampVectorToInt(ref stlPos);
            batch.DrawString(Fonts.Arial12, Ship.MaxSTLSpeed.ToString("0"), stlPos, Color.White);

            if (IsCombat)
            {
                ExploreButton.Draw(batch);
                //PatrolButton.Draw(batch); // FB - Disabled until we make it better
            }
            RefitButton.Draw(batch);
            ScrapButton.Draw(batch);

            batch.DrawRectangle(TotalEntrySize, new Color(118, 102, 67, 50));
        }

        public static string GetStatusText(Ship ship)
        {
            if (ship.AI == null)  //fbedard: prevent crash ?
                return "";
            switch (ship.AI.State)
            {
                default:
                case AIState.PirateRaiderCarrier:
                case AIState.AwaitingOffenseOrders:
                case AIState.MineAsteroids:
                case AIState.Intercept:
                case AIState.AssaultPlanet:
                case AIState.Exterminate:
                    if (ship.AI.OrderQueue.TryPeekFirst(out ShipAI.ShipGoal first))
                    {
                        if (first.TargetPlanet == null)
                            return first.Plan.ToString();

                        if (first.Plan == ShipAI.Plan.LandTroop)
                            return $"{Localizer.Token(GameText.LandingTroopsOn)} {first.TargetPlanet.Name}";

                        return first.Plan + " to " + first.TargetPlanet.Name;
                    }
                    return ship.AI.State.ToString();

                case AIState.DoNothing: return Localizer.Token(GameText.AwaitingOrders2);
                case AIState.Combat:
                {
                    if (ship.AI.Intercepting)
                    {
                        if (ship.AI.Target == null)
                            return "";
                        return string.Concat(Localizer.Token(GameText.Intercepting), " ", (ship.AI.Target as Ship).VanityName);
                    }

                    if (ship.AI.Target == null)
                        return string.Concat(Localizer.Token(GameText.InCombat), "\n", Localizer.Token(GameText.SearchingForTargets));

                    return string.Concat(Localizer.Token(GameText.InCombatWith), " ", (ship.AI.Target as Ship).loyalty.data.Traits.Name);
                }
                case AIState.HoldPosition:   return Localizer.Token(GameText.HoldingPosition);
                case AIState.AwaitingOrders: return Localizer.Token(GameText.AwaitingOrders);
                case AIState.AttackTarget:
                    if (ship.AI.Target == null)
                        return string.Concat(Localizer.Token(GameText.InCombat), "\n", Localizer.Token(GameText.SearchingForTargets));
                    return string.Concat(Localizer.Token(GameText.Attacking), " ", (ship.AI.Target as Ship).VanityName);
                case AIState.Escort:
                    if (ship.AI.EscortTarget == null)
                        return "";
                    return string.Concat(Localizer.Token(GameText.Escorting), " ", ship.AI.EscortTarget.Name);
                case AIState.SystemTrader:
                    if (ship.AI.OrderQueue.TryPeekLast(out ShipAI.ShipGoal last2))
                    {
                        string goodsType = last2.Trade?.Goods.ToString();
                        string blockade  = last2.Trade?.BlockadeTimer < 120 ? Localizer.Token(GameText.Blockade) : "";
                        string status    = "";
                        switch (last2.Plan)
                        {
                            case ShipAI.Plan.PickupGoods:  status = Localizer.Token(GameText.PickingUp); break;
                            case ShipAI.Plan.DropOffGoods: status = Localizer.Token(GameText.Delivering); break;
                        }
                        return $"{status} {goodsType} from {last2.Trade?.ExportFrom.Name} to {last2.Trade?.ImportTo.Name} {blockade}";
                    }
                    return $"{Localizer.Token(GameText.TradingGoods)} \n {Localizer.Token(GameText.SeekingRoute)}";
                case AIState.AttackRunner:
                case AIState.PatrolSystem:
                case AIState.Flee:                
                    if (ship.AI.OrbitTarget == null)
                        return Localizer.Token(GameText.Orbiting);
                    return string.Concat("Fleeing to", " ", ship.AI.OrbitTarget.Name);
                case AIState.Orbit:
                    if (ship.AI.OrbitTarget == null)
                        return Localizer.Token(GameText.Orbiting);

                    Planet planet    = ship.AI.OrbitTarget;
                    string orbitText = $"{Localizer.Token(GameText.Orbiting)} ";
                    if (!ship.AI.HasPriorityOrder && ship.Center.Distance(planet.Center) > planet.ObjectRadius * 3)
                        orbitText = $"{Localizer.Token(GameText.Offensively)} {orbitText}"; // offensive move to orbit

                    return $"{orbitText} {planet.Name}";
                case AIState.Colonize:
                    if (ship.AI.ColonizeTarget == null)
                        return "";

                    return string.Concat(Localizer.Token(GameText.EnRouteToColonize), " ", ship.AI.ColonizeTarget.Name);
                case AIState.MoveTo:
                    if (ship.Velocity.NotZero() || ship.IsTurning)
                    {
                        string moveText = $"{Localizer.Token(GameText.MovingTo)} ";
                        if (!ship.AI.HasPriorityOrder)
                            moveText = $"{Localizer.Token(GameText.Offensively)} {moveText}"; // offensive move

                        if (!ship.AI.OrderQueue.TryPeekLast(out ShipAI.ShipGoal last))
                        {
                            SolarSystem system = UniverseScreen.SolarSystemList.FindMin(s => s.Position.Distance(ship.AI.MovePosition));
                            if (system.IsExploredBy(EmpireManager.Player))
                                return string.Concat(moveText, Localizer.Token(GameText.DeepSpaceNear), " ", system.Name);
                            return Localizer.Token(GameText.ExploringTheGalaxy);
                        }
                        if (last.Plan == ShipAI.Plan.DeployStructure || last.Plan == ShipAI.Plan.DeployOrbital)
                        {
                            moveText = moveText+Localizer.Token(GameText.Deploy);
                            if (last.Goal != null && ResourceManager.GetShipTemplate(last.Goal.ToBuildUID, out Ship toBuild))
                                moveText = string.Concat(moveText, " ", toBuild.Name);
                            return moveText;
                        }
                        else
                        {
                            SolarSystem system = UniverseScreen.SolarSystemList.FindMin(s => s.Position.Distance(ship.AI.MovePosition));
                            if (system.IsExploredBy(EmpireManager.Player))
                                return moveText + system.Name;
                            return Localizer.Token(GameText.ExploringTheGalaxy);
                        }
                    }
                    return Localizer.Token(GameText.HoldingPosition);
                case AIState.Explore:        return Localizer.Token(GameText.ExploringTheGalaxy);
                case AIState.SystemDefender: return Localizer.Token(GameText.SystemDefenseDuty);
                case AIState.Resupply:
                    if (ship.AI.ResupplyTarget == null)
                        return Localizer.Token(GameText.ReturningToBaseForResupply);
                    return string.Concat(Localizer.Token(GameText.ResupplyingAt), " ", ship.AI.ResupplyTarget.Name);
                case AIState.Rebase:
                    var planetName = ship.AI.OrderQueue.PeekLast?.TargetPlanet.Name;                    
                    return Localizer.Token(GameText.TransferringTroops) + $" to {planetName ?? "ERROR" }";  //fbedard
                case AIState.RebaseToShip:
                    return Localizer.Token(GameText.TransferringTroops) + $" to {ship.AI.EscortTarget?.VanityName ?? "ERROR" }";  
                case AIState.Bombard:
                    if (ship.AI.OrderQueue.IsEmpty || ship.AI.OrderQueue.PeekFirst.TargetPlanet == null)
                        return "";
                    if (ship.Center.Distance(ship.AI.OrderQueue.PeekFirst.TargetPlanet.Center) >= 2500f)
                        return string.Concat(Localizer.Token(GameText.EnRouteToBombard), " ", ship.AI.OrderQueue.PeekFirst.TargetPlanet.Name);
                    return string.Concat(Localizer.Token(GameText.Bombarding), " ", ship.AI.OrderQueue.PeekFirst.TargetPlanet.Name);
                case AIState.Boarding:         return Localizer.Token(GameText.ExecutingBoardingAssaultAction);
                case AIState.ReturnToHangar:   return Localizer.Token(GameText.ReturningToHangar);
                case AIState.Ferrying:         return Localizer.Token(GameText.FerryingOrdnance);
                case AIState.Refit:            return ship.IsPlatformOrStation ? Localizer.Token(GameText.WaitingForRefitShip) : Localizer.Token(GameText.MovingToShipyardForRefit);
                case AIState.FormationWarp:    return "Moving in Formation";
                case AIState.Scuttle:          return "Self Destruct: " + ship.ScuttleTimer.ToString("#");
                case AIState.ReturnHome:       return "Defense Ship Returning Home";
                case AIState.SupplyReturnHome: return "Supply Ship Returning Home";
                case AIState.Scrap:
                    string scrapInPlanet = ship.AI.OrbitTarget != null ? $" in {ship.AI.OrbitTarget.Name}" : "";
                    return Localizer.Token(GameText.ScrappingShip) + scrapInPlanet;
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (IsCombat)
            {
                // Explore button for ship list
                if (ExploreButton.HandleInput(input))
                {
                    if (Ship.AI.State == AIState.Explore)
                    {
                        Ship.AI.ClearOrders();
                    }
                    else
                    {
                        Ship.AI.OrderExplore();
                    }
                    StatusText = GetStatusText(Ship);
                    return true;
                }

                // FB - Disabled until we make it better
                // System defense button for ship list
                /*
                if (PatrolButton.HandleInput(input)) 
                {
                    if (ship.AI.State == AIState.SystemDefender || ship.DoingSystemDefense)
                    {
                        ship.DoingSystemDefense = false;
                        ship.AI.ClearOrders();
                    }
                    else
                    {
                        ship.DoingSystemDefense = true;
                    }
                    Status_Text = GetStatusText(ship);
                    return true;
                }*/
            }

            if (RefitButton.HandleInput(input))
            {
                GameAudio.EchoAffirmative();
                Screen.ScreenManager.AddScreen(new RefitToWindow(Screen, this));
                return true;
            }

            if (ScrapButton.HandleInput(input))
            {
                if (!IsScuttle)
                {
                    StatusText = GetStatusText(Ship);
                }
                else
                {
                    StatusText = GetStatusText(Ship);
                }
                GameAudio.EchoAffirmative();
                if (!IsScuttle)
                {
                    if (Ship.AI.State == AIState.Scrap)
                    {
                        Ship.AI.ClearOrders();
                    }
                    else
                    {
                        if (input.IsShiftKeyDown)
                        {
                            RunOnEmpireThread(() => Ship.loyalty.MassScrap(Ship));
                            RunOnEmpireThread(() => Screen.ResetStatus());
                        }
                        else
                        {
                            Ship.AI.OrderScrapShip();
                        }
                    }
                    StatusText = GetStatusText(Ship);
                }
                else
                {
                    if (Ship.ScuttleTimer != -1f)
                    {
                        Ship.ScuttleTimer = -1f;
                        Ship.AI.ClearOrders();
                    }
                    else
                    {
                        Ship.ScuttleTimer = 10f;
                        Ship.AI.ClearOrders(AIState.Scuttle, priority:true);
                    }
                    StatusText = GetStatusText(Ship);
                }
                return true;
            }

            if (ShipNameEntry.ClickableArea.HitTest(input.CursorPosition))
            {
                ShipNameEntry.Hover = true;
                if (input.InGameSelect)
                    ShipNameEntry.HandlingInput = true;
            }
            else
            {
                ShipNameEntry.Hover = false;
                if (!HitTest(input.CursorPosition) || input.LeftMouseClick)
                {
                    ShipNameEntry.HandlingInput = false;
                    GlobalStats.TakingInput = false;
                }
            }

            if (ShipNameEntry.HandlingInput)
            {
                GlobalStats.TakingInput = true;
                ShipNameEntry.HandleTextInput(ref Ship.VanityName, input);
                ShipNameEntry.Text = Ship.VanityName;
                return true;
            }

            GlobalStats.TakingInput = false;
            return base.HandleInput(input);
        }

        void SetNewPos(int x, int y)
        {
            TotalEntrySize = new Rectangle(x, y, TotalEntrySize.Width, TotalEntrySize.Height);
            SysNameRect = new Rectangle(x, y, (int)(TotalEntrySize.Width * 0.10f), TotalEntrySize.Height);
            ShipNameRect = new Rectangle(x + SysNameRect.Width, y, (int)(TotalEntrySize.Width * 0.2f), TotalEntrySize.Height);
            RoleRect = new Rectangle(x + SysNameRect.Width + ShipNameRect.Width, y, (int)(TotalEntrySize.Width * 0.05f), TotalEntrySize.Height);
            FleetRect = new Rectangle(x + SysNameRect.Width + ShipNameRect.Width + RoleRect.Width, y, (int)(TotalEntrySize.Width * 0.075f), TotalEntrySize.Height);
            OrdersRect = new Rectangle(x + SysNameRect.Width + ShipNameRect.Width + RoleRect.Width + FleetRect.Width, y, (int)(TotalEntrySize.Width * 0.2f), TotalEntrySize.Height);
            RefitRect = new Rectangle(OrdersRect.X + OrdersRect.Width, y, 125, TotalEntrySize.Height);
            StrRect = new Rectangle(RefitRect.X + RefitRect.Width, y, 60, TotalEntrySize.Height);
            MaintRect = new Rectangle(StrRect.X + StrRect.Width, y, 60, TotalEntrySize.Height);
            TroopRect = new Rectangle(MaintRect.X + MaintRect.Width, y, 60, TotalEntrySize.Height);
            FTLRect = new Rectangle(TroopRect.X + TroopRect.Width, y, 60, TotalEntrySize.Height);
            STLRect = new Rectangle(FTLRect.X + FTLRect.Width, y, 60, TotalEntrySize.Height);
            ShipIconRect = new Rectangle(ShipNameRect.X + 5, ShipNameRect.Y + 2, 28, 28);
            string shipName = (!string.IsNullOrEmpty(Ship.VanityName) ? Ship.VanityName : Ship.Name);
            ShipNameEntry.ClickableArea = new Rectangle(ShipIconRect.X + ShipIconRect.Width + 10, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2, (int)Fonts.Arial20Bold.MeasureString(shipName).X, Fonts.Arial20Bold.LineSpacing);

            if (IsCombat)
            {
                Rectangle explore = new Rectangle(RefitRect.X + RefitRect.Width / 4 + 5 - ResourceManager.Texture("NewUI/icon_order_explore_hover1").Width, RefitRect.Y + RefitRect.Height / 2 - ResourceManager.Texture("NewUI/icon_order_explore_hover1").Height / 2, ResourceManager.Texture("NewUI/icon_order_explore_hover1").Width, ResourceManager.Texture("NewUI/icon_order_explore_hover1").Height);
                Rectangle patrol = new Rectangle(RefitRect.X + RefitRect.Width / 4 + 10, RefitRect.Y + RefitRect.Height / 2 - ResourceManager.Texture("NewUI/icon_order_patrol_hover2").Height / 2, ResourceManager.Texture("NewUI/icon_order_patrol_hover2").Width, ResourceManager.Texture("NewUI/icon_order_patrol_hover2").Height);
                ExploreButton.r = explore;
                //PatrolButton.r = patrol; // FB - Disabled until we make it better
                ExploreButton.Tooltip = GameText.OrdersThisShipToExplore;
                //PatrolButton.LocalizerTip = 7080; // FB - Disabled until we make it better
            }

            Rectangle refit = new Rectangle(RefitRect.X + RefitRect.Width / 4 + 15 + ResourceManager.Texture("NewUI/icon_order_patrol_hover1").Width, RefitRect.Y + RefitRect.Height / 2 - ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2").Height / 2, ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2").Width, ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2").Height);
            Rectangle scrap = new Rectangle(RefitRect.X + RefitRect.Width / 4 + 20 + ResourceManager.Texture("NewUI/icon_order_patrol_hover1").Width + ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover1").Width, RefitRect.Y + RefitRect.Height / 2 - ResourceManager.Texture("NewUI/icon_queue_delete_hover1").Height / 2, ResourceManager.Texture("NewUI/icon_queue_delete_hover1").Width, ResourceManager.Texture("NewUI/icon_queue_delete_hover1").Height);                       
            RefitButton.r = refit;
            ScrapButton.r = scrap;
            RefitButton.Tooltip = GameText.OpensAMenuAllowingYou;
            ScrapButton.Tooltip = GameText.OrdersTheShipToReturn;

            float width = (int)(OrdersRect.Width * 0.8f);
            while (width % 10f != 0f)
            {
                width = width + 1f;
            }
        }
    }
}
