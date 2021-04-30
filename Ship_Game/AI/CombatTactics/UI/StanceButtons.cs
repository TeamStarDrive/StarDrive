using Microsoft.Xna.Framework;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game.AI.CombatTactics.UI
{
    /// <summary>
    /// Use this class to add buttons or change button function for combat stances buttons.
    /// Structure is that standard UI update and handle input is used.
    /// to add this to something.
    /// use a parent class.
    /// design stance buttons to change shipdata combatstate
    /// fleetStance buttons to change fleet stance
    /// shipstance to change ai combatstate.
    /// create as new.
    /// load content.
    /// use reset to switch targets
    /// </summary>
    public abstract class StanceButtons : UIElementContainer
    {
        readonly Array<OrdersToggleButton> OrdersButtons = new Array<OrdersToggleButton>();
        Vector2 OrdersBarPos;
        public CombatState CurrentState = CombatState.AttackRuns;


        protected StanceButtons(GameScreen parent, Vector2 topLeft) : base(parent.Rect)
        {
            OrdersBarPos = topLeft;
            Visible = false;
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

            OrdersBarPos = new Vector2(OrdersBarPos.X - 3 * 25f, OrdersBarPos.Y + 25f);
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
                other.IsToggled = false;
            }
            b.IsToggled = true;
            ApplyStance(state);

            GameAudio.EchoAffirmative();
        }

        protected abstract void ApplyStance(CombatState stance);

        protected void Reset(CombatState[] states)
        {
            foreach (var button in OrdersButtons) 
                button.IsToggled = states.Contains(button.CombatState);
            Visible = states.Length > 0;
        }

        protected void Reset( CombatState state)
        {
            foreach (var button in OrdersButtons)
                button.IsToggled = state == button.CombatState;
        }

        private class OrdersToggleButton : ToggleButton
        {
            public new CombatState CombatState;
            public  OrdersToggleButton(Vector2 pos, ToggleButtonStyle style, string icon) : base (pos, style, icon){}
        }
    }
}