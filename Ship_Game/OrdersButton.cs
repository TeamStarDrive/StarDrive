using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using Ship_Game.AI;
using Ship_Game.Ships;

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
            this.ID_tip = tipid;
            this.orderType = ot;
            this.clickRect = new Rectangle((int)Location.X, (int)Location.Y, 48, 48);
        }

        public OrdersButton(Array<Ship> shiplist, Vector2 Location, OrderType ot, int tipid)
        {
            this.ID_tip = tipid;
            this.ShipList = shiplist;
            this.orderType = ot;
            this.clickRect = new Rectangle((int)Location.X, (int)Location.Y, 48, 48);
        }

        public OrdersButton(Vector2 Location, OrderType ot, int tipid)
        {
            this.ID_tip = tipid;
            this.orderType = ot;
            this.clickRect = new Rectangle((int)Location.X, (int)Location.Y, 48, 48);
        }

        public void Draw(Ship_Game.ScreenManager ScreenManager, Rectangle r)
        {
            Selector selector = new Selector(r, Color.TransparentBlack);
            Rectangle iconRect = new Rectangle(r.X + 6, r.Y + 6, 44, 44);
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            if (this.SimpleToggle)
            {
                if (!r.HitTest(MousePos))
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/button_action_disabled"], r, Color.White);
                }
                else
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/button_action_hover"], r, Color.White);
                }
                switch (this.orderType)
                {
                    case OrderType.FighterToggle:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OrderButtons/UI_Fighters"], iconRect, Color.White);
                        return;
                    }
                    case OrderType.FighterRecall:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OrderButtons/UI_FighterRecall"], iconRect, Color.White);
                        return;
                    }
                    case OrderType.ShieldToggle:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OrderButtons/UI_Shields"], iconRect, Color.White);
                        return;
                    }
                    case OrderType.DefineAO:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OrderButtons/UI_AO"], iconRect, Color.White);
                        return;
                    }
                    case OrderType.TradeFood:
                    {
                        var icon = ResourceManager.TextureDict["NewUI/icon_food"];
                        iconRect = new Rectangle(r.X + r.Width / 2 - icon.Width / 2, r.Y + r.Height / 2 - icon.Height / 2, icon.Width, icon.Height);
                        ScreenManager.SpriteBatch.Draw(icon, iconRect, Color.White);
                        return;
                    }
                    case OrderType.TradeProduction:
                    {
                        var icon = ResourceManager.TextureDict["NewUI/icon_production"];
                        iconRect = new Rectangle(r.X + r.Width / 2 - icon.Width / 2, r.Y + r.Height / 2 - icon.Height / 2, icon.Width, icon.Height);
                        ScreenManager.SpriteBatch.Draw(icon, iconRect, Color.White);
                        return;
                    }
                    case OrderType.PassTran:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.TextureDict["UI/icon_passtran"].Width / 2, r.Y + r.Height / 2 - ResourceManager.TextureDict["UI/icon_passtran"].Height / 2, ResourceManager.TextureDict["UI/icon_passtran"].Width, ResourceManager.TextureDict["UI/icon_passtran"].Height);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_passtran"], iconRect, Color.White);
                        return;
                    }
                    case OrderType.TroopToggle:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 13, r.Y + r.Height / 2 - 14, 23, 28);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_troop"], iconRect, Color.White);
                        return;
                    }
                    case OrderType.Explore:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.TextureDict["UI/icon_explore"].Width / 2, r.Y + r.Height / 2 - ResourceManager.TextureDict["UI/icon_explore"].Height / 2, ResourceManager.TextureDict["UI/icon_explore"].Width, ResourceManager.TextureDict["UI/icon_explore"].Height);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_explore"], iconRect, Color.White);
                        return;
                    }
                    case OrderType.OrderResupply:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 16, r.Y + r.Height / 2 - 16, 32, 32);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Modules/Ordnance"], iconRect, Color.White);
                        return;
                    }
                    case OrderType.EmpireDefense:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.TextureDict["UI/icon_shield"].Width / 2, r.Y + r.Height / 2 - ResourceManager.TextureDict["UI/icon_shield"].Height / 2, ResourceManager.TextureDict["UI/icon_shield"].Width, ResourceManager.TextureDict["UI/icon_shield"].Height);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_shield"], iconRect, Color.White);
                        return;
                    }
                    case OrderType.Scrap:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.TextureDict["UI/icon_planetslist"].Width / 2, r.Y + r.Height / 2 - ResourceManager.TextureDict["UI/icon_planetslist"].Height / 2, ResourceManager.TextureDict["UI/icon_planetslist"].Width, ResourceManager.TextureDict["UI/icon_planetslist"].Height);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_planetslist"], iconRect, Color.White);
                        return;
                    }
                    case OrderType.Refit:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.TextureDict["UI/icon_dsbw"].Width / 2, r.Y + r.Height / 2 - ResourceManager.TextureDict["UI/icon_dsbw"].Height / 2, ResourceManager.TextureDict["UI/icon_dsbw"].Width, ResourceManager.TextureDict["UI/icon_dsbw"].Height);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_dsbw"], iconRect, Color.White);
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
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/button_action_hover"], r, Color.White);
            }
            else if (this.RightClickValueToModify != null && !this.RightClickValueToModify.Value)
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/button_action_disabled"], r, Color.LightPink);
            }
            else if (!this.ValueToModify.Value)
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/button_action_disabled"], r, Color.White);
            }
            else
            {
                ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["SelectionBox/button_action"], r, Color.White);
            }
            switch (this.orderType)
            {
                case OrderType.FighterToggle:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OrderButtons/UI_Fighters"], iconRect, Color.White);
                    return;
                }
                case OrderType.FighterRecall:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OrderButtons/UI_FighterRecall"], iconRect, Color.White);
                    return;
                }
                case OrderType.ShieldToggle:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OrderButtons/UI_Shields"], iconRect, Color.White);
                    return;
                }
                case OrderType.DefineAO:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["OrderButtons/UI_AO"], iconRect, Color.White);
                    return;
                }
                case OrderType.TradeFood:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.TextureDict["NewUI/icon_food"].Width / 2, r.Y + r.Height / 2 - ResourceManager.TextureDict["NewUI/icon_food"].Height / 2, ResourceManager.TextureDict["NewUI/icon_food"].Width, ResourceManager.TextureDict["NewUI/icon_food"].Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_food"], iconRect, Color.White);
                    return;
                }
                case OrderType.TradeProduction:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.TextureDict["NewUI/icon_production"].Width / 2, r.Y + r.Height / 2 - ResourceManager.TextureDict["NewUI/icon_production"].Height / 2, ResourceManager.TextureDict["NewUI/icon_production"].Width, ResourceManager.TextureDict["NewUI/icon_production"].Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], iconRect, Color.White);
                    return;
                }
                case OrderType.PassTran:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.TextureDict["UI/icon_passtran"].Width / 2, r.Y + r.Height / 2 - ResourceManager.TextureDict["UI/icon_passtran"].Height / 2, ResourceManager.TextureDict["UI/icon_passtran"].Width, ResourceManager.TextureDict["UI/icon_passtran"].Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_passtran"], iconRect, Color.White);
                    return;
                }
                case OrderType.TroopToggle:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - 13, r.Y + r.Height / 2 - 14, 23, 28);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_troop"], iconRect, Color.White);
                    return;
                }
                case OrderType.Explore:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.TextureDict["UI/icon_explore"].Width / 2, r.Y + r.Height / 2 - ResourceManager.TextureDict["UI/icon_explore"].Height / 2, ResourceManager.TextureDict["UI/icon_explore"].Width, ResourceManager.TextureDict["UI/icon_explore"].Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_explore"], iconRect, Color.White);
                    return;
                }
                case OrderType.OrderResupply:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - 16, r.Y + r.Height / 2 - 16, 32, 32);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Modules/Ordnance"], iconRect, Color.White);
                    return;
                }
                case OrderType.EmpireDefense:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.TextureDict["UI/icon_shield"].Width / 2, r.Y + r.Height / 2 - ResourceManager.TextureDict["UI/icon_shield"].Height / 2, ResourceManager.TextureDict["UI/icon_shield"].Width, ResourceManager.TextureDict["UI/icon_shield"].Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_shield"], iconRect, Color.White);
                    return;
                }
                case OrderType.Scrap:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.TextureDict["UI/icon_planetslist"].Width / 2, r.Y + r.Height / 2 - ResourceManager.TextureDict["UI/icon_planetslist"].Height / 2, ResourceManager.TextureDict["UI/icon_planetslist"].Width, ResourceManager.TextureDict["UI/icon_planetslist"].Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_planetslist"], iconRect, Color.White);
                    return;
                }
                case OrderType.Refit:
                {
                    iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.TextureDict["UI/icon_dsbw"].Width / 2, r.Y + r.Height / 2 - ResourceManager.TextureDict["UI/icon_dsbw"].Height / 2, ResourceManager.TextureDict["UI/icon_dsbw"].Width, ResourceManager.TextureDict["UI/icon_dsbw"].Height);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_dsbw"], iconRect, Color.White);
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
            if (!this.clickRect.HitTest(input.CursorPosition))
            {
                //this.Hover = false;
                this.hovering = false;
            }
            else
            {
                //this.Hover = true;
                ToolTip.CreateTooltip(this.ID_tip);
                if (this.SimpleToggle && input.InGameSelect)
                {
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    switch (this.orderType)
                    {
                        case OrderType.TradeFood:
                        {
                            for (int i = 0; i < this.ShipList.Count; i++)
                            {
                                    if (ShipList[i].AI.State != AIState.SystemTrader)
                                    {
                                        ShipList[i].AI.start = null;
                                        ShipList[i].AI.end = null;
                                        ShipList[i].AI.State = AIState.SystemTrader;
                                    }                        
                                
                                ShipList[i].AI.OrderTrade(0);
                                

                                }
                            return true;
                        }
                        case OrderType.TradeProduction:
                        {
                            for (int i = 0; i < this.ShipList.Count; i++)
                            {
                                ShipList[i].AI.start = null;
                                ShipList[i].AI.end = null;
                                ShipList[i].AI.FoodOrProd = "Prod";
                                ShipList[i].AI.OrderTrade(0);
                                //    ShipList[i].AI.State = AIState.SystemTrader;


                                }
                            return true;
                        }
                        case OrderType.PassTran:
                        {
                            for (int i = 0; i < this.ShipList.Count; i++)
                            {
                                this.ShipList[i].AI.OrderTransportPassengers(5f);
                            }
                            return true;
                        }
                        case OrderType.TroopToggle:
                        {
                            return true;
                        }
                        case OrderType.Explore:
                        {
                            for (int i = 0; i < this.ShipList.Count; i++)
                            {
                                this.ShipList[i].AI.OrderExplore();
                            }
                            return true;
                        }
                        case OrderType.OrderResupply:
                        {
                            for (int i = 0; i < this.ShipList.Count; i++)
                            {
                                this.ShipList[i].AI.OrderResupplyNearest(true);
                            }
                            return true;
                        }
                        case OrderType.EmpireDefense:
                        {
                            for (int i = 0; i < this.ShipList.Count; i++)
                            {
                                Ship ship = this.ShipList[i];
                                lock (ship)
                                {
                                    if (!EmpireManager.Player.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Contains(ship))
                                    {
                                        EmpireManager.Player.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Add(ship);
                                        ship.AI.OrderQueue.Clear();
                                        ship.AI.HasPriorityOrder = false;
                                        ship.AI.SystemToDefend = null;
                                        ship.AI.SystemToDefendGuid = Guid.Empty;
                                        ship.AI.State = AIState.SystemDefender;
                                    }
                                    else
                                    {
                                        EmpireManager.Player.GetGSAI().DefensiveCoordinator.Remove(ship);
                                        ship.AI.OrderQueue.Clear();
                                        ship.AI.HasPriorityOrder = false;
                                        ship.AI.SystemToDefend = null;
                                        ship.AI.SystemToDefendGuid = Guid.Empty;
                                        ship.AI.State = AIState.AwaitingOrders;
                                    }
                                }
                            }
                            return true;
                        }
                        case OrderType.Scrap:
                        {
                            for (int i = 0; i < this.ShipList.Count; i++)
                            {
                                this.ShipList[i].AI.OrderScrapShip();
                            }
                            return true;
                        }
                        default:
                        {
                            return true;
                        }
                    }
                }
                if (input.InGameSelect)
                {
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    this.ValueToModify.Value = !this.ValueToModify.Value;
                    return true;
                }
                if (input.RightMouseClick)
                {
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    if (this.RightClickValueToModify != null)
                    {
                        this.RightClickValueToModify.Value = !this.RightClickValueToModify.Value;
                    }
                    return true;
                }
            }
            return this.hovering;
        }
    }
}