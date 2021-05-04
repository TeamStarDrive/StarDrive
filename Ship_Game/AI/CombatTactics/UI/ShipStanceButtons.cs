using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game.AI.CombatTactics.UI
{
    public class ShipStanceButtons : StanceButtons
    {
        Array<Ship> SelectedShips = new Array<Ship>();
        public ShipStanceButtons(GameScreen screen, Vector2 position) : base(screen, position){}
        
        public void ResetButtons(Ship ship) => ResetButtons(new Array<Ship>() { ship });

        public void ResetButtons(Array<Ship> ships)
        {
            // filter out ships where the order buttons should not be shown.
            var filteredShips = ships.Filter(s => s.Active && s.loyalty.isPlayer && !s.IsConstructor && s.DesignRole != ShipData.RoleName.ssp);

            SelectedShips = new Array<Ship>(filteredShips);
            if (SelectedShips.IsEmpty)
                Reset(new CombatState[0]);
            else
                Reset(SelectedShips.Select(s => s.AI.CombatState));
        }

        protected override void ApplyStance(CombatState stance)
        {
            var ships = SelectedShips.ToArray();
            RunOnEmpireThread(() =>
                {
                    for (int i = 0; i < ships.Length; i++)
                    {
                        var ship = ships[i];
                        if (ship.Active)
                            ship.SetCombatStance(stance);
                    }
                }
            );
        }

        protected override void OnOrderButtonHovered(OrdersToggleButton b)
        {
            // Filter ships that should not be shown.
            var ships = SelectedShips.Filter(s => s.Active == true && s.IsVisibleToPlayer);
            
            // sort the ships so that when draw limited the circles wont jump around as much when the list order changes. 
            ships = ships.SortedDescending(s => $"{s.fleet != null} - {(int)s.Radius : 000000} - {s.Id : 00000000}");
            
            // too many circles will cause perf issues, oom crashes, and UI horror.
            // always draw the first 20 in the list. there after skip some so we limit to about 40
            int drawLimit = 20;
            int numberToDraw = 1 + (ships.Length - drawLimit).LowerBound(1) / drawLimit;

            {
                for (int i = 0; i < ships.Length; i += i > 20 ? numberToDraw : 1)
                {
                    ships[i].DrawWeaponRanges(b.CombatState);
                }
            }
        }
    }
}