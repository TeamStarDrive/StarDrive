using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Debug.Page
{
    internal class TradeDebug : DebugPage
    {
        readonly UniverseScreen Screen;
        public TradeDebug(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.Trade)
        {
            Screen = screen;
        }

        public override void Update(float fixedDeltaTime)
        {
            Planet planet = Screen.SelectedPlanet;

            if (planet?.Owner == null)
            {
                var text = new Array<DebugTextBlock>();
                foreach (Empire empire in EmpireManager.Empires)
                {
                    if (!empire.isFaction && !empire.data.Defeated)
                        text.Add(empire.DebugEmpireTradeInfo());
                }
                SetTextColumns(text);
            }
            else
            {
                SetTextColumns(new Array<DebugTextBlock> { planet.DebugPlanetInfo() });
            }
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            foreach (Empire e in EmpireManager.Empires)
            {
                var ships = e.OwnedShips;
                foreach (Ship ship in ships)
                {
                    if (ship?.Active != true) continue;
                    ShipAI ai = ship.AI;
                    if (ai.State != AIState.SystemTrader) continue;
                    if (ai.OrderQueue.Count == 0) continue;
                    /*
                    switch (ai.OrderQueue.PeekLast.Plan)
                    {
                        case ShipAI.Plan.DropOffGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, ai.IsFood ? Color.GreenYellow : Color.SteelBlue, 6);
                            break;
                        case ShipAI.Plan.PickupGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, ai.IsFood ? Color.GreenYellow : Color.SteelBlue, 3);
                            break;
                        case ShipAI.Plan.PickupPassengers:
                        case ShipAI.Plan.DropoffPassengers:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, e.EmpireColor, 32);
                            break;
                    }*/
                }
            }
            base.Draw(batch, elapsed);
        }
    }
}