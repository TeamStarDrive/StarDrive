using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI.CombatTactics.UI
{
    public class ShipStanceButtons : StanceButtons
    {
        Ship[] SelectedShips = Empty<Ship>.Array;
        public ShipStanceButtons(GameScreen screen, Vector2 position) : base(screen, position){}
        
        public void ResetButtons(Ship ship) => ResetButtons(new Array<Ship>() { ship });

        public void ResetButtons(Array<Ship> ships)
        {
            // filter out ships where the order buttons should not be shown.
            SelectedShips = ships.Filter(s => s.Active && s.Loyalty.isPlayer && !s.IsConstructor && !s.IsMiningShip && !s.IsSupplyShuttle
                                              && s.DesignRole != RoleName.ssp);
            if (SelectedShips.Length == 0)
                Reset(new CombatState[0]);
            else
                Reset(SelectedShips.Select(s => s.AI.CombatState));
        }

        protected override void ApplyStance(CombatState stance)
        {
            var ships = SelectedShips;
            if (ships.Length == 0)
                return;

            ships[0].Universe.Screen?.RunOnSimThread(() =>
            {
                for (int i = 0; i < ships.Length; i++)
                {
                    var ship = ships[i];
                    if (ship.Active)
                        ship.SetCombatStance(stance);
                }
            });
        }

        protected override void OnOrderButtonHovered(OrdersToggleButton b)
        {
            // Filter ships that should not be shown.
            var ships = SelectedShips.Filter(s => s.Active && s.IsVisibleToPlayer);
            
            // sort the ships so that when draw limited the circles wont jump around as much when the list order changes. 
            ships = ships.SortedDescending(s => $"{s.Fleet != null} - {(int)s.Radius : 000000} - {s.Id : 00000000}");
            
            // too many circles will cause perf issues, oom crashes, and UI horror.
            // always draw the first 20 in the list. there after skip some so we limit to about 40
            int drawLimit = 20;
            int numberToDraw = 1 + (ships.Length - drawLimit).LowerBound(1) / drawLimit;

            for (int i = 0; i < ships.Length; i += i > 20 ? numberToDraw : 1)
            {
                ships[i].DrawWeaponRanges(Screen, b.CombatState);
            }
        }
    }
}