using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class OrdersButton
    {
        private OrderType orderType;

        public Ref<bool> ValueToModify;

        public Ref<bool> RightClickValueToModify;

        private Color brownish = new Color(96, 81, 49);

        public Rectangle clickRect;

        public bool SimpleToggle;

        public int ID_tip;

        //private bool Hover = true;

        private bool hovering;

        public Array<Ship> ShipList = new Array<Ship>();

        public bool Active;

        public OrdersButton(Ship ship, Vector2 Location, OrderType ot, int tipid)
        {
            ID_tip = tipid;
            orderType = ot;
            clickRect = new Rectangle((int)Location.X, (int)Location.Y, 48, 48);
        }

        public OrdersButton(Array<Ship> shiplist, Vector2 Location, OrderType ot, int tipid)
        {
            ID_tip = tipid;
            ShipList = shiplist;
            orderType = ot;
            clickRect = new Rectangle((int)Location.X, (int)Location.Y, 48, 48);
        }

        public OrdersButton(Vector2 Location, OrderType ot, int tipid)
        {
            ID_tip = tipid;
            orderType = ot;
            clickRect = new Rectangle((int)Location.X, (int)Location.Y, 48, 48);
        }

        public void Draw(ScreenManager ScreenManager, Rectangle r)
        {
            Selector selector = new Selector(r, Color.TransparentBlack);
            Rectangle iconRect = new Rectangle(r.X + 6, r.Y + 6, 44, 44);
            float x = Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, state.Y);
            if (SimpleToggle)
            {
                if (!r.HitTest(MousePos))
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_action_disabled"), r, Color.White);
                }
                else
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_action_hover"), r, Color.White);
                }
                switch (orderType)
                {
                    case OrderType.FighterToggle:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("OrderButtons/UI_Fighters"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.FighterRecall:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("OrderButtons/UI_FighterRecall"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.SendTroops:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/UI_SendTroops"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.DefineAO:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("OrderButtons/UI_AO"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.TradeFood:
                    {
                        var icon = ResourceManager.Texture("NewUI/icon_food");
                        iconRect = new Rectangle(r.X + r.Width / 2 - icon.Width / 2, r.Y + r.Height / 2 - icon.Height / 2, icon.Width, icon.Height);
                        ScreenManager.SpriteBatch.Draw(icon, iconRect, Color.White);
                        return;
                    }
                    case OrderType.TradeProduction:
                    {
                        var icon = ResourceManager.Texture("NewUI/icon_production");
                        iconRect = new Rectangle(r.X + r.Width / 2 - icon.Width / 2, r.Y + r.Height / 2 - icon.Height / 2, icon.Width, icon.Height);
                        ScreenManager.SpriteBatch.Draw(icon, iconRect, Color.White);
                        return;
                    }
                    case OrderType.PassTran:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_passtran").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_passtran").Height / 2, ResourceManager.Texture("UI/icon_passtran").Width, ResourceManager.Texture("UI/icon_passtran").Height);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_passtran"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.TroopToggle:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 13, r.Y + r.Height / 2 - 14, 23, 28);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_troop"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.Explore:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_explore").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_explore").Height / 2, ResourceManager.Texture("UI/icon_explore").Width, ResourceManager.Texture("UI/icon_explore").Height);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_explore"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.OrderResupply:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 16, r.Y + r.Height / 2 - 16, 32, 32);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Modules/Ordnance"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.EmpireDefense:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_shield").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_shield").Height / 2, ResourceManager.Texture("UI/icon_shield").Width, ResourceManager.Texture("UI/icon_shield").Height);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_shield"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.Scrap:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_planetslist").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_planetslist").Height / 2, ResourceManager.Texture("UI/icon_planetslist").Width, ResourceManager.Texture("UI/icon_planetslist").Height);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_planetslist"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.Refit:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_dsbw").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_dsbw").Height / 2, ResourceManager.Texture("UI/icon_dsbw").Width, ResourceManager.Texture("UI/icon_dsbw").Height);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_dsbw"), iconRect, Color.White);
                        return;
                    }
                    default:
                    {
                        return;
                    }
                }
            }
            if (r.HitTest(MousePos))
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_action_hover"), r, Color.White);
            }
            else if (RightClickValueToModify != null && !RightClickValueToModify.Value)
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_action_disabled"), r, Color.LightPink);
            }
            else if (!ValueToModify.Value)
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_action_disabled"), r, Color.White);
            }
            else
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_action"), r, Color.White);
            }
            switch (orderType)
            {
                case OrderType.FighterToggle:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("OrderButtons/UI_Fighters"), iconRect, Color.White);
                    return;
                }
                case OrderType.FighterRecall:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("OrderButtons/UI_FighterRecall"), iconRect, Color.White);
                    return;
                }
                case OrderType.SendTroops:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/UI_SendTroops"), iconRect, Color.White);
                    return;
                }
                case OrderType.DefineAO:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("OrderButtons/UI_AO"), iconRect, Color.White);
                    return;
                }
                case OrderType.TradeFood:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("NewUI/icon_food").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("NewUI/icon_food").Height / 2, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_food"), iconRect, Color.White);
                    return;
                }
                case OrderType.TradeProduction:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("NewUI/icon_production").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("NewUI/icon_production").Height / 2, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_production"), iconRect, Color.White);
                    return;
                }
                case OrderType.PassTran:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_passtran").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_passtran").Height / 2, ResourceManager.Texture("UI/icon_passtran").Width, ResourceManager.Texture("UI/icon_passtran").Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_passtran"), iconRect, Color.White);
                    return;
                }
                case OrderType.TroopToggle:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - 13, r.Y + r.Height / 2 - 14, 23, 28);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_troop"), iconRect, Color.White);
                    return;
                }
                case OrderType.Explore:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_explore").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_explore").Height / 2, ResourceManager.Texture("UI/icon_explore").Width, ResourceManager.Texture("UI/icon_explore").Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_explore"), iconRect, Color.White);
                    return;
                }
                case OrderType.OrderResupply:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - 16, r.Y + r.Height / 2 - 16, 32, 32);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Modules/Ordnance"), iconRect, Color.White);
                    return;
                }
                case OrderType.EmpireDefense:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_shield").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_shield").Height / 2, ResourceManager.Texture("UI/icon_shield").Width, ResourceManager.Texture("UI/icon_shield").Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_shield"), iconRect, Color.White);
                    return;
                }
                case OrderType.Scrap:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_planetslist").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_planetslist").Height / 2, ResourceManager.Texture("UI/icon_planetslist").Width, ResourceManager.Texture("UI/icon_planetslist").Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_planetslist"), iconRect, Color.White);
                    return;
                }
                case OrderType.Refit:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_dsbw").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_dsbw").Height / 2, ResourceManager.Texture("UI/icon_dsbw").Width, ResourceManager.Texture("UI/icon_dsbw").Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_dsbw"), iconRect, Color.White);
                    return;
                }
                default:
                {
                    return;
                }
            }
        }

        public bool HandleInput(InputState input, ScreenManager sm)
        {
            if (!clickRect.HitTest(input.CursorPosition))
            {
                //this.Hover = false;
                hovering = false;
            }
            else
            {
                //this.Hover = true;
                ToolTip.CreateTooltip(ID_tip);
                if (SimpleToggle && input.InGameSelect || input.RightMouseClick)
                {
                    GameAudio.AcceptClick();
                    for (int i = 0; i < ShipList.Count; i++)
                    {
                        Ship ship = ShipList[i];
                        switch (orderType)
                        {
                            case OrderType.TradeFood:       ship.TransportingFood         = !input.RightMouseClick; break;
                            case OrderType.TradeProduction: ship.TransportingProduction   = !input.RightMouseClick; break;
                            case OrderType.PassTran:        ship.TransportingColonists    = !input.RightMouseClick; break;
                            case OrderType.FighterToggle:   ship.FightersOut              = !input.RightMouseClick; break;
                            case OrderType.FighterRecall:   ship.RecallFightersBeforeFTL  = !input.RightMouseClick; break;
                            case OrderType.TroopToggle:     ship.TroopsOut                = !input.RightMouseClick; break;
                            case OrderType.SendTroops:      ship.Carrier.SendTroopsToShip = !input.RightMouseClick; break;
                            case OrderType.Explore:         ship.AI.OrderExplore();                                 break;
                            case OrderType.OrderResupply:   ship.Supply.ResupplyFromButton();                       break;
                            case OrderType.Scrap:           ship.AI.OrderScrapShip();                               break;
                            case OrderType.EmpireDefense:   AddOrRemoveFromForcePool(ship);                         break;
                        }
                    }

                    return true;
                }

                if (input.InGameSelect)
                {
                    GameAudio.AcceptClick();
                    ValueToModify.Value = !ValueToModify.Value;
                    return true;
                }
                if (input.RightMouseClick)
                {
                    GameAudio.AcceptClick();
                    if (RightClickValueToModify != null)
                    {
                        RightClickValueToModify.Value = !RightClickValueToModify.Value;
                    }
                    return true;
                }
            }
            return hovering;
        }

        void AddOrRemoveFromForcePool(Ship ship)
        {
            lock (ship)
            {
                if (!EmpireManager.Player.GetEmpireAI().DefensiveCoordinator.DefensiveForcePool.Contains(ship))
                {
                    EmpireManager.Player.GetEmpireAI().DefensiveCoordinator.DefensiveForcePool.Add(ship);
                    ship.AI.ClearOrders();
                    ship.AI.SystemToDefend = null;
                    ship.AI.SystemToDefendGuid = Guid.Empty;
                }
                else
                {
                    EmpireManager.Player.GetEmpireAI().DefensiveCoordinator.Remove(ship);
                    ship.AI.ClearOrders();
                    ship.AI.SystemToDefend = null;
                    ship.AI.SystemToDefendGuid = Guid.Empty;
                }
            }
        }
    }
}