using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI.CombatTactics.UI
{
    public class FleetStanceButtons : StanceButtons
    {
        Array<FleetDataNode> SelectedNodes = new Array<FleetDataNode>();
        public FleetStanceButtons(GameScreen screen, Vector2 position) : base(screen, position){}
        
        public void ResetButtons(FleetDataNode node)
        {
            ResetButtons(new Array<FleetDataNode>() { node });
        }

        public void ResetButtons(Array<FleetDataNode> nodes)
        {
            SelectedNodes = nodes;
            if (nodes.IsEmpty)
                Reset(new CombatState[0]);
            else
                Reset(nodes.Select(n => n.CombatState));

        }

        protected override void ApplyStance(CombatState stance)
        {
            foreach (var node in SelectedNodes)
            {
                node.SetCombatStance(stance);
            }
        }

        protected override void OnOrderButtonHovered(OrdersToggleButton b) {}
    }
}