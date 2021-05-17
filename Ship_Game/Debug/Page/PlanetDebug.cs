using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Debug.Page
{
    internal class PlanetDebug : DebugPage
    {
        readonly UniverseScreen Screen;
        public PlanetDebug(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.Planets)
        {
            Screen = screen;
        }

        public override void Update(float fixedDeltaTime)
        {
            Planet planet = Screen.SelectedPlanet;
            if (planet == null)
            {
                var text = new Array<DebugTextBlock>();
                foreach (Empire empire in EmpireManager.Empires)
                {
                    if (!empire.isFaction && !empire.data.Defeated)
                        text.Add(empire.DebugEmpirePlanetInfo());
                }
                SetTextColumns(text);
            }
            else
            {
                SetTextColumns(new Array<DebugTextBlock>{ planet.DebugPlanetInfo() });
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
                    Color debugColor = Color.Aqua;
                    if (ship.GetCargo(Goods.Food) > 0)
                        debugColor = Color.GreenYellow;
                    else if (ship.GetCargo(Goods.Production) > 0)
                        debugColor = Color.SteelBlue;

                    switch (ai.OrderQueue.PeekLast.Plan)
                    {
                        case ShipAI.Plan.DropOffGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, debugColor, 6);
                            break;
                        case ShipAI.Plan.PickupGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, debugColor, 3);
                            break;
                    }
                }
            }
            base.Draw(batch, elapsed);
        }
    }
}