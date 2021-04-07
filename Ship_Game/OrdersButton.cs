using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;
using System;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class OrdersButton // Cleaned Up by Fat Bastard - May, 22 2019
    {
        private readonly OrderType OrderType;
        private readonly Ship Ship;
        private bool Hovering;
        public Ref<bool> ValueToModify;
        public Ref<bool> RightClickValueToModify;
        public Rectangle ClickRect;
        public bool SimpleToggle;
        public LocalizedText IdTip;
        public Array<Ship> ShipList = new Array<Ship>();
        public bool Active;

        public OrdersButton(Ship ship, Vector2 location, OrderType ot, LocalizedText tipId)
        {
            IdTip     = tipId;
            OrderType = ot;
            Ship      = ship;
            ClickRect = new Rectangle((int)location.X, (int)location.Y, 48, 48);
        }

        public OrdersButton(Array<Ship> shipList, Vector2 location, OrderType ot, LocalizedText tipId)
        {
            IdTip     = tipId;
            ShipList  = shipList;
            OrderType = ot;
            ClickRect = new Rectangle((int)location.X, (int)location.Y, 48, 48);
        }

        public void Draw(SpriteBatch batch, Vector2 cursor, Rectangle rect)
        {
            bool hovering = rect.HitTest(cursor);
            if (SimpleToggle)
            {
                batch.Draw(!hovering
                    ? ResourceManager.Texture("SelectionBox/button_action_disabled")
                    : ResourceManager.Texture("SelectionBox/button_action_hover"), rect, Color.White);
            }
            else
            {
                if (hovering)
                    batch.Draw(ResourceManager.Texture("SelectionBox/button_action_hover"), rect, Color.White);
                else if (RightClickValueToModify != null && !RightClickValueToModify.Value)
                    batch.Draw(ResourceManager.Texture("SelectionBox/button_action_disabled"), rect, Color.LightPink);
                else if (!ValueToModify.Value)
                    batch.Draw(ResourceManager.Texture("SelectionBox/button_action_disabled"), rect, Color.White);
                else
                    batch.Draw(ResourceManager.Texture("SelectionBox/button_action"), rect, Color.White);
            }

            switch (OrderType)
            {
                case OrderType.FighterToggle:      DrawButton(batch, rect, ResourceManager.Texture("OrderButtons/UI_Fighters"));      break;
                case OrderType.FighterRecall:      DrawButton(batch, rect, ResourceManager.Texture("OrderButtons/UI_FighterRecall")); break;
                case OrderType.SendTroops:         DrawButton(batch, rect, ResourceManager.Texture("NewUI/UI_SendTroops"));           break;
                case OrderType.TradeFood:          DrawButton(batch, rect, ResourceManager.Texture("NewUI/icon_food"));               break;
                case OrderType.TradeProduction:    DrawButton(batch, rect, ResourceManager.Texture("NewUI/icon_production"));         break;
                case OrderType.TransportColonists: DrawButton(batch, rect, ResourceManager.Texture("UI/icon_passtran"));              break;
                case OrderType.TroopToggle:        DrawButton(batch, rect, ResourceManager.Texture("UI/icon_troop"));                 break;
                case OrderType.Explore:            DrawButton(batch, rect, ResourceManager.Texture("UI/icon_explore"));               break;
                case OrderType.OrderResupply:      DrawButton(batch, rect, ResourceManager.Texture("Modules/Ordnance"));              break;
                case OrderType.EmpireDefense:      DrawButton(batch, rect, ResourceManager.Texture("UI/icon_shield"));                break;
                case OrderType.Scrap:              DrawButton(batch, rect, ResourceManager.Texture("UI/icon_planetslist"));           break;
                case OrderType.Refit:              DrawButton(batch, rect, ResourceManager.Texture("UI/icon_dsbw"));                  break;
                case OrderType.AllowInterTrade:    DrawButton(batch, rect, ResourceManager.Texture("NewUI/icon_intertrade"));         break;
                case OrderType.DefineTradeRoutes:  DrawTradeRoutesButton(batch, rect, Ship);                                          break;
                case OrderType.DefineAO:           DrawAOButton(batch, rect, Ship);                                                   break;
            }
        }

        private void DrawTradeRoutesButton(SpriteBatch batch, Rectangle rect, Ship ship)
        {
            if (ship == null)
                return;

            DrawDynamicButton(batch, rect, ResourceManager.Texture("NewUI/icon_routes_Active"),
                                           ResourceManager.Texture("NewUI/icon_routes"),
                                           ship.TradeRoutes.Count);
        }

        private void DrawAOButton(SpriteBatch batch, Rectangle rect, Ship ship)
        {
            if (ship == null)
                return;

            DrawDynamicButton(batch, rect, ResourceManager.Texture("NewUI/UI_AO_Active"),
                                           ResourceManager.Texture("OrderButtons/UI_AO"),
                                           ship.AreaOfOperation.Count);
        }

        private void DrawDynamicButton(SpriteBatch batch, Rectangle rect, SubTexture activated, SubTexture deactivated, int counter)
        {
            SubTexture tex = counter > 0 ? activated : deactivated;
            DrawButton(batch, rect, tex);
        }

        private void DrawButton(SpriteBatch batch, Rectangle rect, SubTexture tex)
        {
            int texWidth       = Math.Min(32, tex.Width);
            int texHeight      = Math.Min(32, tex.Height);
            Rectangle iconRect = new Rectangle(rect.X + rect.Width / 2 - texWidth / 2, 
                                               rect.Y + rect.Height / 2 - texHeight / 2,
                                               texWidth,
                                               texHeight);

            batch.Draw(tex, iconRect, Color.White);
        }

        public bool HandleInput(InputState input, ScreenManager sm)
        {
            if (!ClickRect.HitTest(input.CursorPosition))
            {
                Hovering = false;
                return Hovering;
            }

            ToolTip.CreateTooltip(IdTip);
            if (SimpleToggle && (input.InGameSelect || input.RightMouseClick))
            {
                GameAudio.AcceptClick();
                for (int i = 0; i < ShipList.Count; i++)
                {
                    Ship ship = ShipList[i];
                    switch (OrderType)
                    {
                        case OrderType.TradeFood:          ship.TransportingFood         = !input.RightMouseClick; break;
                        case OrderType.TradeProduction:    ship.TransportingProduction   = !input.RightMouseClick; break;
                        case OrderType.TransportColonists: ship.TransportingColonists    = !input.RightMouseClick; break;
                        case OrderType.AllowInterTrade:    ship.AllowInterEmpireTrade    = !input.RightMouseClick; break;
                        case OrderType.FighterToggle:      ship.Carrier.FightersOut      = !input.RightMouseClick; break;
                        case OrderType.TroopToggle:        ship.Carrier.TroopsOut        = !input.RightMouseClick; break;
                        case OrderType.Explore:            ship.AI.OrderExplore();                                 break;
                        case OrderType.OrderResupply:      ship.Supply.ResupplyFromButton();                       break;
                        case OrderType.Scrap:              ship.AI.OrderScrapShip();                               break;
                        case OrderType.EmpireDefense:      AddOrRemoveFromForcePool(ship);                         break;
                        case OrderType.FighterRecall:      ship.Carrier.SetRecallFightersBeforeFTL(!input.RightMouseClick); break;
                        case OrderType.SendTroops:         ship.Carrier.SetSendTroopsToShip(!input.RightMouseClick);        break;
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
                else if (ValueToModify.Value)
                    ValueToModify.Value = !ValueToModify.Value; // this button has single functionality, so right click disables it as well

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