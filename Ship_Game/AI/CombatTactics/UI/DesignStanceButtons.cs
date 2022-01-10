using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game.AI.CombatTactics.UI
{
    public class DesignStanceButtons : StanceButtons
    {
        ShipDesign Design;

        public DesignStanceButtons(GameScreen screen, Vector2 position) : base(screen, position){}
        
        public void ResetButtons(ShipDesign design)
        {
            Design = design;
            Reset(new []{ design.DefaultCombatState });
        }

        protected override void ApplyStance(CombatState stance)
        {
            if (Design != null)
                Design.DefaultCombatState = stance;
        }

        protected override void OnOrderButtonHovered(OrdersToggleButton b) { }
    }
}