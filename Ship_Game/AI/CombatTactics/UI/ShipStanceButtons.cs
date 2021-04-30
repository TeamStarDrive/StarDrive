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
            var filteredShips = ships?.Filter(s => s?.Active == true && s.loyalty.isPlayer && !s.IsConstructor && s.DesignRole != ShipData.RoleName.ssp) ;

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
            var ships = SelectedShips.Filter(s => s?.Active == true);

            // too many ships to draw circles for so reduce based on proximity to each other. 
            if (ships.Length > 10)
            {
                Map<int, Ship> uniqueShips = ships.GroupBy(s =>
                   {
                       Vector2 uniqueCenter = s.Center;
                       return (int)(uniqueCenter.X / 20000 * uniqueCenter.Y / 20000);
                   });
                ships = uniqueShips.Values.ToArray();
            }

            for (int i = 0; i < ships.Length; i++)
            {
                var ship = ships[i];
                float radius = ship.GetDesiredCombatRangeForState(b.CombatState);
                Draws.Add(() =>
                {
                    if (ship?.Active == true)
                        Screen.DrawCircleProjected(ship.Center, radius, Colors.CombatOrders());
                });
            }
        }
    }
}