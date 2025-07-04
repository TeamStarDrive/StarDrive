using Ship_Game.Data.Serialization;
using Ship_Game.Ships.AI;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Fleets
{
    [StarDataType]
    public class FleetPatrol
    {
        public string PatrolName { get; private set; }
        public WayPoints WayPoints { get; private set; } = new();
        int CurrentWaypointNum;

        public FleetPatrol(string name, WayPoints waypoints)
        {
            PatrolName = name;
            WayPoints = waypoints;
            CurrentWaypointNum = 0;
        }

        [StarDataConstructor] FleetPatrol() { }

        public Vector2 ChangeToNextWaypoint()
        {
            CurrentWaypointNum = (CurrentWaypointNum + 1) % WayPoints.Count;
            return WayPoints.ElementAt(CurrentWaypointNum).Position;
        }

        public Vector2 CurrentWaypoint => WayPoints.ElementAt(CurrentWaypointNum).Position;
    }
}