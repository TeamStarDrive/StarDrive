using Microsoft.Xna.Framework;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game.AI.CombatTactics.UI
{
    public class StanceButtons : UIElementContainer
    {
        readonly Array<OrdersToggleButton> OrdersButtons = new Array<OrdersToggleButton>();
        Vector2 OrdersBarPos;
        Array<Ship> SelectedShips = new Array<Ship>();
        Array<FleetDataNode> SelectedNodes = new Array<FleetDataNode>();

        public bool NothingSelected => SelectedNodes.IsEmpty && SelectedShips.IsEmpty;

        public StanceButtons(GameScreen parent, Vector2 topLeft) : base(parent.Rect)
        {
            OrdersBarPos = topLeft;
        }

        public void LoadContent()
        {

            AddOrderBtn("SelectionBox/icon_formation_headon", CombatState.AttackRuns, toolTip: GameText.ShipWillMakeHeadonAttack);
            AddOrderBtn("SelectionBox/icon_grid", CombatState.ShortRange, toolTip: GameText.ShipWillRotateSoThat2);
            AddOrderBtn("SelectionBox/icon_formation_aft", CombatState.Artillery, toolTip: GameText.ShipWillRotateSoThat);
            AddOrderBtn("SelectionBox/icon_formation_x", CombatState.HoldPosition, toolTip: GameText.ShipWillAttemptToHold);
            AddOrderBtn("SelectionBox/icon_formation_left", CombatState.OrbitLeft, toolTip: GameText.ShipWillManeuverToKeep);
            AddOrderBtn("SelectionBox/icon_formation_right", CombatState.OrbitRight, toolTip: GameText.ShipWillManeuverToKeep2);
            AddOrderBtn("SelectionBox/icon_formation_stop", CombatState.Evade, toolTip: GameText.ShipWillAvoidEngagingIn);

            OrdersBarPos = new Vector2(OrdersBarPos.X + 4 * 25f, OrdersBarPos.Y + 25f);
            AddOrderBtn("SelectionBox/icon_formation_bleft", CombatState.BroadsideLeft, toolTip: GameText.ShipWillMoveWithinMaximum);
            AddOrderBtn("SelectionBox/icon_formation_bright", CombatState.BroadsideLeft, toolTip: GameText.ShipWillMoveWithinMaximum2);

        }

        void AddOrderBtn(string icon, CombatState state, LocalizedText toolTip)
        {
            var button = new OrdersToggleButton(OrdersBarPos, ToggleButtonStyle.Formation, icon)
            {
                CombatState = state, Tooltip = toolTip,
                OnClick = (b) => OnOrderButtonClicked(b, state)
            };
            Add(button);
            OrdersButtons.Add(button);
            OrdersBarPos.X += 25f;
        }

        void OnOrderButtonClicked(ToggleButton b, CombatState state)
        {
            for (int i = 0; i < OrdersButtons.Count; i++)
            {
                ToggleButton other = OrdersButtons[i];
                if (other != b) other.IsToggled = false;
            }

            for (int i = 0; i < SelectedShips.Count; i++)
            {
                var ship = SelectedShips[i];
                ship.AI.CombatState = state;
                if (state == CombatState.HoldPosition)
                    ship.AI.OrderAllStop();
                ship.shipStatusChanged = true;

                // @todo Is this some sort of bug fix?
                if (state != CombatState.HoldPosition && ship.AI.State == AIState.HoldPosition)
                    ship.AI.State = AIState.AwaitingOrders;
            }

            for (int i = 0; i < SelectedNodes.Count; i++)
            {
                var node = SelectedNodes[i];
                node.CombatState = state;
            }

            if (!NothingSelected)
            {
                GameAudio.EchoAffirmative();
            }
        }

        public void UpdateSelectedItems(Array<Ship> ships, Array<FleetDataNode> nodes)
        {
            SelectedNodes = nodes?.NotEmpty == true ? nodes : new Array<FleetDataNode>();
            SelectedShips = ships?.IsEmpty  == true ? ships : new Array<Ship>();

            for (int i = 0; i < OrdersButtons.Count; i++)
            {
                ToggleButton button = OrdersButtons[i];
                button.Visible = !NothingSelected;
                button.IsToggled = false;
            }

        }

        public void HandleInput(float fixedDeltaTime)
        {
            Visible = !NothingSelected;
            for (int i = 0; i < OrdersButtons.Count; i++)
            {
                ToggleButton button = OrdersButtons[i];
                button.Visible = !NothingSelected;
                button.IsToggled = false;
            }

            base.Update(fixedDeltaTime);
        }

        public override bool HandleInput(InputState input)
        {
            return base.HandleInput(input);
        }

        public bool HandleInput(InputState input, Array<FleetDataNode> nodes)
        {
            var ships = new Array<Ship>();
            foreach(var node in nodes)
            {
                if (node.Ship != null)
                    ships.Add(node.Ship);
            }
            return HandleInput(input, ships, nodes);
        }

        public bool HandleInput(InputState input, Ship ship) => HandleInput(input, new Array<Ship>() { ship }, null);

        public bool HandleInput(InputState input, FleetDataNode node)
        {
            return HandleInput(input, new Array<Ship>() { node.Ship }, new Array<FleetDataNode>() { node});
        }

        public void Reset()
        {
 
        }

        private class OrdersToggleButton : ToggleButton
        {
            public new CombatState CombatState;
            public  OrdersToggleButton(Vector2 pos, ToggleButtonStyle style, string icon) : base (pos, style, icon){}
        }
    }
}