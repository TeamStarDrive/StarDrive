using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class ShipGroupProject
    {
        public Vector2 Start { get; private set; }
        public Vector2 End { get; private set; }
        public Vector2 Direction { get; private set; }
        public Vector2 FleetCenter { get; private set; }
        public bool Started;

        public void Update(UniverseScreen universe, Fleet fleet, Ship ship)
        {
            InputState input = universe.Input;
            if (!Started)
            {
                Start = universe.UnprojectToWorldPosition(input.StartRightHold);
                Started = true;
            }

            End = universe.UnprojectToWorldPosition(input.EndRightHold);
            Direction = Start.DirectionToTarget(End).LeftVector();

            if (fleet != null)
            {
                FleetCenter = fleet.GetProjectedMidPoint(Start, End, Vector2.Zero);
            }
        }
    }
}