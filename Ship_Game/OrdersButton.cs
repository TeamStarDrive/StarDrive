using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Ships;
using System;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class OrdersButton
    {
        private readonly OrderType OrderType;
        public Ref<bool> ValueToModify;
        public Ref<bool> RightClickValueToModify;
        public Rectangle ClickRect;
        public bool SimpleToggle;
        public int IdTip;
        private bool Hovering;
        public Array<Ship> ShipList = new Array<Ship>();
        public bool Active;

        public OrdersButton(Ship ship, Vector2 location, OrderType ot, int tipId)
        {
            IdTip     = tipId;
            OrderType = ot;
            ClickRect = new Rectangle((int)location.X, (int)location.Y, 48, 48);
        }

        public OrdersButton(Array<Ship> shipList, Vector2 location, OrderType ot, int tipId)
        {
            IdTip     = tipId;
            ShipList  = shipList;
            OrderType = ot;
            ClickRect = new Rectangle((int)location.X, (int)location.Y, 48, 48);
        }

        public void Draw(ScreenManager screenManager, Rectangle r)
        {
            MouseState state = Mouse.GetState();
            Vector2 mousePos = new Vector2(Mouse.GetState().X, state.Y);
            if (SimpleToggle)
            {
                screenManager.SpriteBatch.Draw(!r.HitTest(mousePos)
                        ? ResourceManager.Texture("SelectionBox/button_action_disabled")
                        : ResourceManager.Texture("SelectionBox/button_action_hover"), r, Color.White);

                Rectangle iconRect;
                switch (OrderType)
                {
                    case OrderType.FighterToggle:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                        screenManager.SpriteBatch.Draw(ResourceManager.Texture("OrderButtons/UI_Fighters"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.FighterRecall:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                        screenManager.SpriteBatch.Draw(ResourceManager.Texture("OrderButtons/UI_FighterRecall"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.SendTroops:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                        screenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/UI_SendTroops"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.DefineAO:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                        screenManager.SpriteBatch.Draw(ResourceManager.Texture("OrderButtons/UI_AO"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.TradeFood:
                    {
                        var icon = ResourceManager.Texture("NewUI/icon_food");
                        iconRect = new Rectangle(r.X + r.Width / 2 - icon.Width / 2, r.Y + r.Height / 2 - icon.Height / 2, icon.Width, icon.Height);
                        screenManager.SpriteBatch.Draw(icon, iconRect, Color.White);
                        return;
                    }
                    case OrderType.TradeProduction:
                    {
                        var icon = ResourceManager.Texture("NewUI/icon_production");
                        iconRect = new Rectangle(r.X + r.Width / 2 - icon.Width / 2, r.Y + r.Height / 2 - icon.Height / 2, icon.Width, icon.Height);
                        screenManager.SpriteBatch.Draw(icon, iconRect, Color.White);
                        return;
                    }
                    case OrderType.TransportColonists:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_passtran").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_passtran").Height / 2, ResourceManager.Texture("UI/icon_passtran").Width, ResourceManager.Texture("UI/icon_passtran").Height);
                        screenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_passtran"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.TroopToggle:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 13, r.Y + r.Height / 2 - 14, 23, 28);
                        screenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_troop"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.Explore:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_explore").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_explore").Height / 2, ResourceManager.Texture("UI/icon_explore").Width, ResourceManager.Texture("UI/icon_explore").Height);
                        screenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_explore"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.OrderResupply:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 16, r.Y + r.Height / 2 - 16, 32, 32);
                        screenManager.SpriteBatch.Draw(ResourceManager.Texture("Modules/Ordnance"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.EmpireDefense:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_shield").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_shield").Height / 2, ResourceManager.Texture("UI/icon_shield").Width, ResourceManager.Texture("UI/icon_shield").Height);
                        screenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_shield"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.Scrap:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_planetslist").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_planetslist").Height / 2, ResourceManager.Texture("UI/icon_planetslist").Width, ResourceManager.Texture("UI/icon_planetslist").Height);
                        screenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_planetslist"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.Refit:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture("UI/icon_dsbw").Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture("UI/icon_dsbw").Height / 2, ResourceManager.Texture("UI/icon_dsbw").Width, ResourceManager.Texture("UI/icon_dsbw").Height);
                        screenManager.SpriteBatch.Draw(ResourceManager.Texture("UI/icon_dsbw"), iconRect, Color.White);
                        return;
                    }
                    case OrderType.AllowInterTrade:
                    {
                        iconRect = new Rectangle(r.X + r.Width / 2 - 12, r.Y + r.Height / 2 - 12, 24, 24);
                        screenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_intertrade"), iconRect, Color.White);
                        return;
                    }
                    default:
                    {
                        return;
                    }
                }
            }
            if (r.HitTest(mousePos))
                screenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_action_hover"), r, Color.White);
            else if (RightClickValueToModify != null && !RightClickValueToModify.Value)
                screenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_action_disabled"), r, Color.LightPink);
            else if (!ValueToModify.Value)
                screenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_action_disabled"), r, Color.White);
            else
                screenManager.SpriteBatch.Draw(ResourceManager.Texture("SelectionBox/button_action"), r, Color.White);

            switch (OrderType)
            {
                case OrderType.FighterToggle:      DrawButton(screenManager, r, ResourceManager.Texture("OrderButtons/UI_Fighters"));      break;
                case OrderType.FighterRecall:      DrawButton(screenManager, r, ResourceManager.Texture("OrderButtons/UI_FighterRecall")); break;
                case OrderType.SendTroops:         DrawButton(screenManager, r, ResourceManager.Texture("NewUI/UI_SendTroops"));           break;
                case OrderType.DefineAO:           DrawButton(screenManager, r, ResourceManager.Texture("OrderButtons/UI_AO"));            break;
                case OrderType.TradeFood:          DrawButton(screenManager, r, ResourceManager.Texture("NewUI/icon_food"));               break;
                case OrderType.TradeProduction:    DrawButton(screenManager, r, ResourceManager.Texture("NewUI/icon_production"));         break;
                case OrderType.TransportColonists: DrawButton(screenManager, r, ResourceManager.Texture("UI/icon_passtran"));              break;
                case OrderType.TroopToggle:        DrawButton(screenManager, r, ResourceManager.Texture("UI/icon_troop"));                 break;
                case OrderType.Explore:            DrawButton(screenManager, r, ResourceManager.Texture("UI/icon_explore"));               break;
                case OrderType.OrderResupply:      DrawButton(screenManager, r, ResourceManager.Texture("Modules/Ordnance"));              break;
                case OrderType.EmpireDefense:      DrawButton(screenManager, r, ResourceManager.Texture("UI/icon_shield"));                break;
                case OrderType.Scrap:              DrawButton(screenManager, r, ResourceManager.Texture("UI/icon_planetslist"));           break;
                case OrderType.Refit:              DrawButton(screenManager, r, ResourceManager.Texture("UI/icon_dsbw"));                  break;
                case OrderType.AllowInterTrade:    DrawButton(screenManager, r, ResourceManager.Texture("NewUI/icon_intertrade"));         break;
            }
        }

        private void DrawButton(ScreenManager screenManager, Rectangle rect, SubTexture tex)
        {
            int texWidth       = Math.Min(32, tex.Width);
            int texHeight      = Math.Min(32, tex.Height);
            Rectangle iconRect = new Rectangle(rect.X + rect.Width / 2 - texWidth / 2, 
                                               rect.Y + rect.Height / 2 - texHeight / 2,
                                               texWidth,
                                               texHeight);

            screenManager.SpriteBatch.Draw(tex, iconRect, Color.White);
        }

        public bool HandleInput(InputState input, ScreenManager sm)
        {
            if (!ClickRect.HitTest(input.CursorPosition))
            {
                Hovering = false;
                return Hovering;
            }

            ToolTip.CreateTooltip(IdTip);
            if (SimpleToggle && input.InGameSelect || input.RightMouseClick)
            {
                GameAudio.AcceptClick();
                for (int i = 0; i < ShipList.Count; i++)
                {
                    Ship ship = ShipList[i];
                    switch (OrderType)
                    {
                        case OrderType.TradeFood: ship.TransportingFood = !input.RightMouseClick; break;
                        case OrderType.TradeProduction: ship.TransportingProduction = !input.RightMouseClick; break;
                        case OrderType.TransportColonists: ship.TransportingColonists = !input.RightMouseClick; break;
                        case OrderType.AllowInterTrade: ship.AllowInterEmpireTrade = !input.RightMouseClick; break;
                        case OrderType.FighterToggle: ship.FightersOut = !input.RightMouseClick; break;
                        case OrderType.FighterRecall: ship.RecallFightersBeforeFTL = !input.RightMouseClick; break;
                        case OrderType.TroopToggle: ship.TroopsOut = !input.RightMouseClick; break;
                        case OrderType.SendTroops: ship.Carrier.SendTroopsToShip = !input.RightMouseClick; break;
                        case OrderType.Explore: ship.AI.OrderExplore(); break;
                        case OrderType.OrderResupply: ship.Supply.ResupplyFromButton(); break;
                        case OrderType.Scrap: ship.AI.OrderScrapShip(); break;
                        case OrderType.EmpireDefense: AddOrRemoveFromForcePool(ship); break;
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
                    RightClickValueToModify.Value = !RightClickValueToModify.Value;

                return true;
            }
            return Hovering;
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