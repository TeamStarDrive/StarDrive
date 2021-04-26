using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game.AI.CombatTactics.UI
{
    public class ShipStanceButtons : StanceButtons
    {
        Array<Ship> SelectedShips = new Array<Ship>();
        public ShipStanceButtons(GameScreen screen, Vector2 position) : base(screen, position){}
        
        public void ResetButtons(Ship ship)
        {
            ResetButtons(new Array<Ship>() { ship });
        }

        public void ResetButtons(Array<Ship> ships)
        {
            var filteredShips = ships?.Filter(s => s != null && s.loyalty.isPlayer && !s.IsConstructor && s.DesignRole != ShipData.RoleName.ssp) ;

            SelectedShips = new Array<Ship>(filteredShips);
            if (SelectedShips.IsEmpty)
                Reset(new CombatState[0]);
            else
                Reset(SelectedShips.Select(s => s.AI.CombatState));
        }

        protected override void ApplyStance(CombatState stance)
        {
            foreach (var ship in SelectedShips)
                ship.SetCombatStance(stance);
        }
    }
}