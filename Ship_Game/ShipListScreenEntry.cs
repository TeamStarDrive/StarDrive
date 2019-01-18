using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class ShipListScreenEntry
    {
        public Ship ship;

        public Rectangle TotalEntrySize;

        public Rectangle SysNameRect;

        public Rectangle ShipNameRect;

        public Rectangle RoleRect;

        public Rectangle OrdersRect;

        public Rectangle RefitRect;

        public Rectangle STRRect;

        public Rectangle MaintRect;

        public Rectangle TroopRect;

        public Rectangle FTLRect;

        public Rectangle STLRect;

        public Rectangle RemainderRect;

        private Rectangle ShipIconRect;

        private UITextEntry ShipNameEntry = new UITextEntry();

        private TexturedButton RefitButton;

        private TexturedButton ScrapButton;

        private TexturedButton PatrolButton; //System Defence button for ShipListScreen

        private TexturedButton ExploreButton; //Auto-explore button for ShipListScreen

        public ShipListScreen screen;

        public string Status_Text;

        private bool isScuttle;
        private bool isCombat;  //fbedard

        public bool Selected = false;  //fbedard: for multi-select

        public ShipListScreenEntry(Ship s, int x, int y, int width1, int height, ShipListScreen caller)
        {
            screen = caller;
            ship = s;
            TotalEntrySize = new Rectangle(x, y, width1 - 60, height);
            SysNameRect = new Rectangle(x, y, (int)(TotalEntrySize.Width * 0.10f), height);
            ShipNameRect = new Rectangle(x + SysNameRect.Width, y, (int)(TotalEntrySize.Width * 0.175f), height);
            RoleRect = new Rectangle(x + SysNameRect.Width + ShipNameRect.Width, y, (int)(TotalEntrySize.Width * 0.05f), height);
            OrdersRect = new Rectangle(x + SysNameRect.Width + ShipNameRect.Width + RoleRect.Width, y, (int)(TotalEntrySize.Width * 0.175f), height);
            RefitRect = new Rectangle(OrdersRect.X + OrdersRect.Width, y, 125, height);
            STRRect = new Rectangle(RefitRect.X + RefitRect.Width, y, 60, height);
            MaintRect = new Rectangle(STRRect.X + STRRect.Width, y, 60, height);
            TroopRect = new Rectangle(MaintRect.X + MaintRect.Width, y, 60, height);
            FTLRect = new Rectangle(TroopRect.X + TroopRect.Width, y, 60, height);
            STLRect = new Rectangle(FTLRect.X + FTLRect.Width, y, 60, height);
            Status_Text = GetStatusText(ship);
            ShipIconRect = new Rectangle(ShipNameRect.X + 5, ShipNameRect.Y + 2, 28, 28);
            string shipName = (!string.IsNullOrEmpty(ship.VanityName) ? ship.VanityName : ship.Name);
            ShipNameEntry.ClickableArea = new Rectangle(ShipIconRect.X + ShipIconRect.Width + 10, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2, (int)Fonts.Arial20Bold.MeasureString(shipName).X, Fonts.Arial20Bold.LineSpacing);
            ShipNameEntry.Text = shipName;
            float width = (int)(OrdersRect.Width * 0.8f);
            while (width % 10f != 0f)
            {
                width = width + 1f;
            }

            if (ship.shipData.Role != ShipData.RoleName.station && ship.Mothership == null && ship.shipData.Role != ShipData.RoleName.platform && ship.shipData.Role != ShipData.RoleName.troop && ship.AI.State != AIState.Colonize && ship.shipData.Role != ShipData.RoleName.freighter && ship.shipData.ShipCategory != ShipData.Category.Civilian)
                isCombat = true;

            Rectangle refit = new Rectangle(RefitRect.X + RefitRect.Width / 2 - 5 - ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover1").Width, RefitRect.Y + RefitRect.Height / 2 - ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2").Height / 2, ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2").Width, ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2").Height);

            if (isCombat)
            {
                ExploreButton = new TexturedButton(refit, "NewUI/icon_order_explore", "NewUI/icon_order_explore_hover1", "NewUI/icon_order_explore_hover2");
                PatrolButton = new TexturedButton(refit, "NewUI/icon_order_patrol", "NewUI/icon_order_patrol_hover1", "NewUI/icon_order_patrol_hover2");
            }
            RefitButton = new TexturedButton(refit, "NewUI/icon_queue_rushconstruction", "NewUI/icon_queue_rushconstruction_hover1", "NewUI/icon_queue_rushconstruction_hover2");			
            ScrapButton = new TexturedButton(refit, "NewUI/icon_queue_delete", "NewUI/icon_queue_delete_hover1", "NewUI/icon_queue_delete_hover2");

            if (ship.shipData.Role == ShipData.RoleName.station || ship.shipData.Role == ShipData.RoleName.platform || ship.Thrust <= 0f)
            {
                isScuttle = true;
            }
        }

        public void Draw(ScreenManager ScreenManager, GameTime gameTime)
        {
            Color TextColor = new Color(255, 239, 208);
            string sysname = (ship.System!= null ? ship.System.Name : Localizer.Token(150));
            if (Fonts.Arial20Bold.MeasureString(sysname).X <= SysNameRect.Width)
            {
                Vector2 SysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - Fonts.Arial12Bold.MeasureString(sysname).X / 2f, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, sysname, SysNameCursor, TextColor);
            }
            else
            {
                Vector2 SysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - Fonts.Arial12Bold.MeasureString(sysname).X / 2f, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, sysname, SysNameCursor, TextColor);
            }
            ScreenManager.SpriteBatch.Draw(ship.shipData.Icon, ShipIconRect, Color.White);
            Vector2 rpos = new Vector2
            {
                X = ShipNameEntry.ClickableArea.X,
                Y = ShipNameEntry.ClickableArea.Y
            };
            ShipNameEntry.Draw(Fonts.Arial12Bold, ScreenManager.SpriteBatch, rpos, gameTime, TextColor);
            Vector2 rolePos = new Vector2(RoleRect.X + RoleRect.Width / 2 - Fonts.Arial12Bold.MeasureString(Localizer.GetRole(ship.shipData.Role, ship.loyalty)).X / 2f, RoleRect.Y + RoleRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            HelperFunctions.ClampVectorToInt(ref rolePos);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.GetRole(ship.shipData.Role, ship.loyalty), rolePos, TextColor);
            
            Vector2 StatusPos = new Vector2(OrdersRect.X + OrdersRect.Width / 2 - Fonts.Arial12Bold.MeasureString(Status_Text).X / 2f, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial12Bold.MeasureString(Status_Text).Y / 2f);
            HelperFunctions.ClampVectorToInt(ref StatusPos);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, Status_Text, StatusPos, TextColor);

            Vector2 MainPos = new Vector2(MaintRect.X + MaintRect.Width / 2, MaintRect.Y + MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            Empire e = EmpireManager.Player;
            float Maint = 1f;
            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
            {
                Maint = ship.GetMaintCostRealism();
            }
            else
            {
                Maint = ship.GetMaintCost();
            }
            Maint = Maint + e.data.Traits.MaintMod * Maint;
            MainPos.X = MainPos.X - Fonts.Arial12.MeasureString(Maint.ToString("F2")).X / 2f + 6;
            HelperFunctions.ClampVectorToInt(ref MainPos);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, Maint.ToString("F2"), MainPos, Maint > 0.00 ? Color.Salmon : Color.White);
            Vector2 StrPos = new Vector2(STRRect.X + STRRect.Width / 2, MaintRect.Y + MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            float x = StrPos.X;
            SpriteFont arial12Bold = Fonts.Arial12;
            float strength = ship.GetStrength();
            StrPos.X = x - Fonts.Arial12.MeasureString(strength.ToString("0")).X / 2f + 6;
            HelperFunctions.ClampVectorToInt(ref StrPos);
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont arial12 = Fonts.Arial12;
            float single = ship.GetStrength();
            spriteBatch.DrawString(arial12, single.ToString("0"), StrPos, Color.White);
            Vector2 TroopPos = new Vector2(TroopRect.X + TroopRect.Width / 2f, MaintRect.Y + MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            //{
            TroopPos.X = TroopPos.X - Fonts.Arial12.MeasureString(string.Concat(ship.TroopList.Count, "/", ship.TroopCapacity)).X / 2f + 6;
            //};
            HelperFunctions.ClampVectorToInt(ref TroopPos);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(ship.TroopList.Count, "/", ship.TroopCapacity), TroopPos, Color.White);
            Vector2 FTLPos = new Vector2(FTLRect.X + FTLRect.Width / 2f, MaintRect.Y + MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            float x1 = FTLPos.X;
            SpriteFont spriteFont = Fonts.Arial12;
            float fTLSpeed = ship.GetmaxFTLSpeed / 1000f;
            FTLPos.X = x1 - spriteFont.MeasureString(string.Concat(fTLSpeed.ToString("0"), "k")).X / 2f + 6;
            HelperFunctions.ClampVectorToInt(ref FTLPos);
            SpriteBatch spriteBatch1 = ScreenManager.SpriteBatch;
            SpriteFont arial121 = Fonts.Arial12;
            float fTLSpeed1 = ship.GetmaxFTLSpeed / 1000f;
            spriteBatch1.DrawString(arial121, string.Concat(fTLSpeed1.ToString("0"), "k"), FTLPos, Color.White);
            Vector2 STLPos = new Vector2(STLRect.X + STLRect.Width / 2f, MaintRect.Y + MaintRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            float single1 = STLPos.X;
            SpriteFont arial12Bold1 = Fonts.Arial12;
            float sTLSpeed = ship.GetSTLSpeed();
            STLPos.X = single1 - arial12Bold1.MeasureString(sTLSpeed.ToString("0")).X / 2f + 6;
            HelperFunctions.ClampVectorToInt(ref STLPos);
            SpriteBatch spriteBatch2 = ScreenManager.SpriteBatch;
            SpriteFont spriteFont1 = Fonts.Arial12;
            float sTLSpeed1 = ship.GetSTLSpeed();
            spriteBatch2.DrawString(spriteFont1, sTLSpeed1.ToString("0"), STLPos, Color.White);
            if (isScuttle)
            {
                float scuttleTimer = ship.ScuttleTimer;
            }

            if (isCombat)
            {
                ExploreButton.Draw(ScreenManager);
                PatrolButton.Draw(ScreenManager);
            }
            RefitButton.Draw(ScreenManager);
            ScrapButton.Draw(ScreenManager);
            
        }

        public static string GetStatusText(Ship ship)
        {
            string str;
            string text = string.Empty;
            if (ship.AI == null)  //fbedard: prevent crash ?
                return text;
            switch (ship.AI.State)
            {
                case AIState.DoNothing:
                {
                    text = Localizer.Token(183);
                    break;
                }
                case AIState.Combat:
                {
                    if (ship.AI.Intercepting)
                    {
                        if (ship.AI.Target == null)
                        {
                            break;
                        }
                        text = string.Concat(Localizer.Token(157), " ", (ship.AI.Target as Ship).VanityName);
                        break;
                    }

                    if (ship.AI.Target == null)
                    {
                        text = Localizer.Token(155);
                        text = string.Concat(text, "\n", Localizer.Token(156));
                        break;
                    }

                    text = string.Concat(Localizer.Token(158), " ", (ship.AI.Target as Ship).loyalty.data.Traits.Name);
                    break;
                }
                case AIState.HoldPosition:
                {
                    text = Localizer.Token(180);
                    break;
                }
                case AIState.ManualControl:
                {
                    text = Localizer.Token(171);
                    break;
                }
                case AIState.AwaitingOrders:
                {
                    return Localizer.Token(153);
                }
                case AIState.AttackTarget:
                {
                    if (ship.AI.Target == null)
                    {
                        text = Localizer.Token(155);
                        text = string.Concat(text, "\n", Localizer.Token(156));
                        break;
                    }

                    text = string.Concat(Localizer.Token(154), " ", (ship.AI.Target as Ship).VanityName);
                    break;
                }
                case AIState.Escort:
                {
                    if (ship.AI.EscortTarget == null)
                    {
                        break;
                    }
                    text = string.Concat(Localizer.Token(179), " ", ship.AI.EscortTarget.Name);
                    break;
                }
                case AIState.SystemTrader:
                {
                    if (ship.AI.OrderQueue.IsEmpty)
                    {
                        text = string.Concat(Localizer.Token(164), "\n", Localizer.Token(165));
                        break;
                    }

                    switch (ship.AI.OrderQueue.PeekLast.Plan)
                    {
                        case ShipAI.Plan.PickupGoods:
                        {
                            if(ship.AI.start !=null)
                                text = string.Concat(text, Localizer.Token(159), " ", ship.AI.start.Name);
                            string pickingup = Localizer.Token(160);
                            string str1 = text;
                            string[] strArrays = { str1, "\n", pickingup, " ", null };
                            strArrays[4] = (ship.AI.IsFood ? Localizer.Token(161) : Localizer.Token(162));
                            text = string.Concat(strArrays);
                            break;
                        }
                        case ShipAI.Plan.DropOffGoods:
                        {
                            if(ship.AI.end != null)
                                text = string.Concat(text, Localizer.Token(159), " ", ship.AI.end.Name);
                            string delivering = Localizer.Token(163);
                            string str2 = text;
                            string[] strArrays1 = { str2, "\n", delivering, " ", null };
                            strArrays1[4] = (ship.AI.IsFood ? Localizer.Token(161) : Localizer.Token(162));
                            text = string.Concat(strArrays1);
                            break;
                        }
                    }
                    break;
                }
                case AIState.AttackRunner:
                case AIState.PatrolSystem:
                case AIState.Flee:                
                {
                    if (ship.AI.OrbitTarget == null)
                    {
                        text = Localizer.Token(182);
                        break;
                    }

                    text = string.Concat("Fleeing to", " ", ship.AI.OrbitTarget.Name);
                    break;
                }
                case AIState.PirateRaiderCarrier:
                case AIState.AwaitingOffenseOrders:
                case AIState.MineAsteroids:
                case AIState.Intercept:
                case AIState.AssaultPlanet:
                case AIState.Exterminate:
                {
                    if (ship.AI.OrderQueue.IsEmpty)
                    {
                        text = ship.AI.State.ToString();
                        break;
                    }
                    var first = ship.AI.OrderQueue.PeekFirst;
                    if (first.TargetPlanet == null)
                        text = first.Plan.ToString();
                    else
                        text = first.Plan + " to " + first.TargetPlanet.Name;
                    break;
                }
                case AIState.Orbit:
                {
                    if (ship.AI.OrbitTarget == null)
                    {
                        text = Localizer.Token(182);
                        break;
                    }

                    text = string.Concat(Localizer.Token(182), " ", ship.AI.OrbitTarget.Name);
                    break;
                }
                case AIState.PassengerTransport:
                {
                    if (ship.AI.OrderQueue.IsEmpty)
                    {
                        text = string.Concat(Localizer.Token(168), "\n", Localizer.Token(165));
                        break;
                    }

                    try
                    {
                        switch (ship.AI.OrderQueue.PeekLast.Plan)
                        {
                            case ShipAI.Plan.PickupPassengers:
                            {
                                text = string.Concat(text, Localizer.Token(159), " ", ship.AI.start.Name);
                                text = string.Concat(text, "\n", Localizer.Token(166));
                                break;
                            }
                            case ShipAI.Plan.DropoffPassengers:
                            {
                                text = string.Concat(text, Localizer.Token(159), " ", ship.AI.end.Name);
                                text = string.Concat(text, "\n", Localizer.Token(167));
                                break;
                            }
                        }
                        break;
                    }
                    catch
                    {
                        str = "";
                    }
                    return str;
                }
                case AIState.Colonize:
                {
                    if (ship.AI.ColonizeTarget == null)
                    {
                        break;
                    }
                    text = string.Concat(Localizer.Token(169), " ", ship.AI.ColonizeTarget.Name);
                    break;
                }
                case AIState.MoveTo:
                {
                    if (!(ship.Velocity == Vector2.Zero) || ship.isTurning)
                    {
                        text = string.Concat(Localizer.Token(187), " ");
                        if (ship.AI.OrderQueue.IsEmpty)
                        {
                            IOrderedEnumerable<SolarSystem> sortedList = 
                                from system in UniverseScreen.SolarSystemList
                                orderby Vector2.Distance(ship.AI.MovePosition, system.Position)
                                select system;
                            text = string.Concat(text, Localizer.Token(189), " ", sortedList.First().Name);
                            if (sortedList.First().IsExploredBy(EmpireManager.Player))
                            {
                                break;
                            }
                            text = Localizer.Token(174);
                            break;
                        }

                        if (ship.AI.OrderQueue.PeekLast.Plan != ShipAI.Plan.DeployStructure)
                        {
                            IOrderedEnumerable<SolarSystem> sortedList = 
                                from system in UniverseScreen.SolarSystemList
                                orderby Vector2.Distance(ship.AI.MovePosition, system.Position)
                                select system;
                            text = string.Concat(text, sortedList.First().Name);
                            if (sortedList.First().IsExploredBy(EmpireManager.Player))
                            {
                                break;
                            }
                            text = Localizer.Token(174);
                            break;
                        }

                        text = string.Concat(text, Localizer.Token(188), " ", ResourceManager.ShipsDict[ship.AI.OrderQueue.PeekLast.goal.ToBuildUID].Name);
                        break;
                    }

                    text = Localizer.Token(180);
                    break;
                }
                case AIState.Explore:
                {
                    text = Localizer.Token(174);
                    break;
                }
                case AIState.SystemDefender:
                {
                    text = Localizer.Token(170);
                    break;
                }
                case AIState.Resupply:
                {
                    Planet resupplyTarget = ship.AI.ResupplyTarget;
                    if (resupplyTarget == null)
                    {                        
                        text = Localizer.Token(173);
                        break;
                    }

                    text = string.Concat(Localizer.Token(172), " ", resupplyTarget.Name);
                    break;
                }
                case AIState.Rebase:
                {
                    var planetName = ship.AI.OrderQueue.PeekLast?.TargetPlanet.Name;                    
                    text = Localizer.Token(178) + $" to {planetName ?? "ERROR" }";  //fbedard
                    break;
                }
                case AIState.Bombard:
                {
                    if (ship.AI.OrderQueue.IsEmpty || ship.AI.OrderQueue.PeekFirst.TargetPlanet == null)
                    {
                        break;
                    }
                    if (Vector2.Distance(ship.Center, ship.AI.OrderQueue.PeekFirst.TargetPlanet.Center) >= 2500f)
                    {
                        text = string.Concat(Localizer.Token(176), " ", ship.AI.OrderQueue.PeekFirst.TargetPlanet.Name);
                        break;
                    }

                    text = string.Concat(Localizer.Token(175), " ", ship.AI.OrderQueue.PeekFirst.TargetPlanet.Name);
                    break;
                }
                case AIState.BombardTroops:
                {
                    if (ship.AI.OrderQueue.IsEmpty || ship.AI.OrderQueue.PeekFirst.TargetPlanet == null)
                    {
                        break;
                    }
                    if (ship.Center.OutsideRadius(ship.AI.OrderQueue.PeekFirst.TargetPlanet.Center, 2500f))
                    {
                        text = "Soften " + ship.AI.OrderQueue.PeekFirst.TargetPlanet.Name;
                        break;
                    }

                    text = Localizer.Token(175) + " " + ship.AI.OrderQueue.PeekFirst.TargetPlanet.Name;
                    break;
                }
                case AIState.Boarding:
                {
                    text = Localizer.Token(177);
                    break;
                }
                case AIState.ReturnToHangar:
                {
                    text = Localizer.Token(181);
                    break;
                }
                case AIState.Ferrying:
                {
                    text = Localizer.Token(185);
                    break;
                }
                case AIState.Refit:
                {
                    text = Localizer.Token(184);
                    break;
                }
                case AIState.Scrap:
                {
                    text = Localizer.Token(186);
                    break;
                }
                case AIState.FormationWarp:
                {
                    text = "Moving in Formation";
                    break;
                }
                case AIState.Scuttle:
                {
                    text = "Self Destruct: " + ship.ScuttleTimer.ToString("#");
                    break;
                }
                case AIState.ReturnHome:
                {
                        text = "Defense Ship Returning Home";
                        break;
                }
                default:
                {
                    goto case AIState.Exterminate;
                }
            }
            return text;
        }

        public void HandleInput(InputState input)
        {
            if (isCombat)
            {
                // Explore button for ship list
                if (ExploreButton.HandleInput(input))
                {
                    if (ship.AI.State == AIState.Explore)
                    {
                        ship.AI.State = AIState.AwaitingOrders;
                        ship.AI.OrderQueue.Clear();
                    }
                    else
                    {
                        ship.AI.OrderExplore();
                    }
                    Status_Text = GetStatusText(ship);
                }

                // System defence button for ship list
                if (PatrolButton.HandleInput(input))
                {
                    if (ship.AI.State == AIState.SystemDefender || ship.DoingSystemDefense)
                    {
                        ship.DoingSystemDefense = false;
                        ship.AI.State = AIState.AwaitingOrders;
                        ship.AI.OrderQueue.Clear();
                    }
                    else
                    {
                        ship.DoingSystemDefense = true;
                    }
                    Status_Text = GetStatusText(ship);
                }
            }
            if (RefitButton.HandleInput(input))
            {
                GameAudio.EchoAffirmative();
                screen.ScreenManager.AddScreen(new RefitToWindow(screen, this));
            }

            if (ScrapButton.HandleInput(input))
            {
                if (!isScuttle)
                {
                    Status_Text = GetStatusText(ship);
                }
                else
                {
                    Status_Text = GetStatusText(ship);
                }
                GameAudio.EchoAffirmative();
                if (!isScuttle)
                {
                    if (ship.AI.State == AIState.Scrap)
                    {
                        ship.AI.State = AIState.AwaitingOrders;
                        ship.AI.OrderQueue.Clear();
                    }
                    else
                    {
                        ship.AI.OrderScrapShip();
                    }
                    Status_Text = GetStatusText(ship);
                }
                else
                {
                    if (ship.ScuttleTimer != -1f)
                    {
                        ship.ScuttleTimer = -1f;
                        ship.AI.State = AIState.AwaitingOrders;
                        ship.AI.HasPriorityOrder = false;
                        ship.AI.OrderQueue.Clear();
                    }
                    else
                    {
                        ship.ScuttleTimer = 10f;
                        ship.AI.State = AIState.Scuttle;
                        ship.AI.HasPriorityOrder = true;
                        ship.AI.OrderQueue.Clear();
                    }
                    Status_Text = GetStatusText(ship);
                }
            }
            if (!ShipNameEntry.ClickableArea.HitTest(input.CursorPosition))
            {
                ShipNameEntry.Hover = false;
            }
            else
            {
                ShipNameEntry.Hover = true;
                if (input.InGameSelect)
                {
                    ShipNameEntry.HandlingInput = true;
                }
            }
            if (!ShipNameEntry.HandlingInput)
            {
                GlobalStats.TakingInput = false;
                return;
            }
            GlobalStats.TakingInput = true;
            ShipNameEntry.HandleTextInput(ref ship.VanityName, input);
            ShipNameEntry.Text = ship.VanityName;
        }

        public void SetNewPos(int x, int y)
        {
            TotalEntrySize = new Rectangle(x, y, TotalEntrySize.Width, TotalEntrySize.Height);
            SysNameRect = new Rectangle(x, y, (int)(TotalEntrySize.Width * 0.10f), TotalEntrySize.Height);
            ShipNameRect = new Rectangle(x + SysNameRect.Width, y, (int)(TotalEntrySize.Width * 0.2f), TotalEntrySize.Height);
            RoleRect = new Rectangle(x + SysNameRect.Width + ShipNameRect.Width, y, (int)(TotalEntrySize.Width * 0.05f), TotalEntrySize.Height);
            OrdersRect = new Rectangle(x + SysNameRect.Width + ShipNameRect.Width + RoleRect.Width, y, (int)(TotalEntrySize.Width * 0.2f), TotalEntrySize.Height);
            RefitRect = new Rectangle(OrdersRect.X + OrdersRect.Width, y, 125, TotalEntrySize.Height);
            STRRect = new Rectangle(RefitRect.X + RefitRect.Width, y, 60, TotalEntrySize.Height);
            MaintRect = new Rectangle(STRRect.X + STRRect.Width, y, 60, TotalEntrySize.Height);
            TroopRect = new Rectangle(MaintRect.X + MaintRect.Width, y, 60, TotalEntrySize.Height);
            FTLRect = new Rectangle(TroopRect.X + TroopRect.Width, y, 60, TotalEntrySize.Height);
            STLRect = new Rectangle(FTLRect.X + FTLRect.Width, y, 60, TotalEntrySize.Height);
            ShipIconRect = new Rectangle(ShipNameRect.X + 5, ShipNameRect.Y + 2, 28, 28);
            string shipName = (!string.IsNullOrEmpty(ship.VanityName) ? ship.VanityName : ship.Name);
            ShipNameEntry.ClickableArea = new Rectangle(ShipIconRect.X + ShipIconRect.Width + 10, 2 + SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2, (int)Fonts.Arial20Bold.MeasureString(shipName).X, Fonts.Arial20Bold.LineSpacing);

            if (isCombat)
            {
                Rectangle explore = new Rectangle(RefitRect.X + RefitRect.Width / 4 + 5 - ResourceManager.Texture("NewUI/icon_order_explore_hover1").Width, RefitRect.Y + RefitRect.Height / 2 - ResourceManager.Texture("NewUI/icon_order_explore_hover1").Height / 2, ResourceManager.Texture("NewUI/icon_order_explore_hover1").Width, ResourceManager.Texture("NewUI/icon_order_explore_hover1").Height);
                Rectangle patrol = new Rectangle(RefitRect.X + RefitRect.Width / 4 + 10, RefitRect.Y + RefitRect.Height / 2 - ResourceManager.Texture("NewUI/icon_order_patrol_hover2").Height / 2, ResourceManager.Texture("NewUI/icon_order_patrol_hover2").Width, ResourceManager.Texture("NewUI/icon_order_patrol_hover2").Height);
                ExploreButton.r = explore;
                PatrolButton.r = patrol;
                ExploreButton.LocalizerTip = 2171;
                PatrolButton.LocalizerTip = 7080;
            }

            Rectangle refit = new Rectangle(RefitRect.X + RefitRect.Width / 4 + 15 + ResourceManager.Texture("NewUI/icon_order_patrol_hover1").Width, RefitRect.Y + RefitRect.Height / 2 - ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2").Height / 2, ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2").Width, ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover2").Height);
            Rectangle scrap = new Rectangle(RefitRect.X + RefitRect.Width / 4 + 20 + ResourceManager.Texture("NewUI/icon_order_patrol_hover1").Width + ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover1").Width, RefitRect.Y + RefitRect.Height / 2 - ResourceManager.Texture("NewUI/icon_queue_delete_hover1").Height / 2, ResourceManager.Texture("NewUI/icon_queue_delete_hover1").Width, ResourceManager.Texture("NewUI/icon_queue_delete_hover1").Height);                       
            RefitButton.r = refit;
            ScrapButton.r = scrap;
            RefitButton.LocalizerTip = 2213;
            ScrapButton.LocalizerTip = 2214;

            float width = (int)(OrdersRect.Width * 0.8f);
            while (width % 10f != 0f)
            {
                width = width + 1f;
            }
        }
    }
}