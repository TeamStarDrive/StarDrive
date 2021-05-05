using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game.AI.CombatTactics.UI
{
    public class DesignStanceButtons : StanceButtons
    {
        Array<Ship> SelectedShips = new Array<Ship>();
        public DesignStanceButtons(GameScreen screen, Vector2 position) : base(screen, position){}
        
        public void ResetButtons(Ship ship)
        {
            ResetButtons(new Array<Ship>() { ship });
        }

        public void ResetButtons(Array<Ship> ships)
        {
            SelectedShips = ships;
            if (ships.IsEmpty)
                Reset(new CombatState[0]);
            else
                Reset(ships.Select(s => s.shipData.CombatState));

        }

        protected override void ApplyStance(CombatState stance)
        {
            foreach (var ship in SelectedShips)
                ship.shipData.CombatState = stance;
        }

        protected override void OnOrderButtonHovered(OrdersToggleButton b) { }
    }
}