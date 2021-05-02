using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game.AI.CombatTactics.UI
{
    public class ShipStanceButtons : StanceButtons
    {
        Array<Action> Draws = new Array<Action>();
        Array<Ship> SelectedShips = new Array<Ship>();
        public ShipStanceButtons(GameScreen screen, Vector2 position) : base(screen, position){}
        
        public void ResetButtons(Ship ship) => ResetButtons(new Array<Ship>() { ship });

        public void ResetButtons(Array<Ship> ships)
        {
            var filteredShips = ships?.Filter(s => s.Active && s.loyalty.isPlayer && !s.IsConstructor && s.DesignRole != ShipData.RoleName.ssp) ;

            SelectedShips = new Array<Ship>(filteredShips);
            if (SelectedShips.IsEmpty)
                Reset(new CombatState[0]);
            else
                Reset(SelectedShips.Select(s => s.AI.CombatState));
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            while (Draws.NotEmpty)
            {
                if (Draws.TryPopLast(out var draw))
                {
                    draw.Invoke();
                }
            }

            base.Draw(batch, elapsed);
        }

        protected override void ApplyStance(CombatState stance)
        {
            RunOnEmpireThread(() =>
                {
                    for (int i = 0; i < SelectedShips.Count; i++)
                    {
                        var ship = SelectedShips[i];
                        if (ship?.Active == true)
                            ship.SetCombatStance(stance);
                    }
                }
            );
        }

        protected override void OnOrderButtonHovered(OrdersToggleButton b)
        {
            // try to prevent null ship crashes
            var ships = SelectedShips.Filter(s => s.Active == true && s.IsVisibleToPlayer);

            // too many ships to draw circles for so reduce based on design role
            if (ships.Length > 20)
            {
                float largest = ships.FindMax(s => s.Radius).Radius;
                
                Map<ShipData.RoleName, Ship> uniqueShips = ships.GroupBy(s => s.DesignRole);

                foreach (var ship in uniqueShips.Values)
                {
                    Draws.Add(ship.GetDrawForWeaponRanges(Screen, b.CombatState));
                }
            }
            else
            {
                for (int i = 0; i < ships.Length; i++)
                {
                    Draws.Add(ships[i].GetDrawForWeaponRanges(Screen, b.CombatState));
                }
            }
        }
    }
}