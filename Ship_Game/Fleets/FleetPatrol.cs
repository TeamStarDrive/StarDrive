using Ship_Game.Data.Serialization;
using Ship_Game.Ships.AI;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Fleets
{
    [StarDataType]
    public class FleetPatrol
    {
        [StarData] public string Name { get; private set; }
        public WayPoints WayPoints { get; private set; } = new();
        [StarData] int CurrentWaypointIndex;

        public FleetPatrol(string name, WayPoints waypoints)
        {
            Name = name;
            WayPoints = waypoints;
            CurrentWaypointIndex = 0;
        }

        [StarDataConstructor] FleetPatrol() { }

        [StarData]
        WayPoint[] WayPointsSave
        {
            get => WayPoints.ToArray();
            set => WayPoints.Set(value);
        }

        public Vector2 ChangeToNextWaypoint()
        {
            CurrentWaypointIndex = (CurrentWaypointIndex + 1) % WayPoints.Count;
            return WayPoints.ElementAt(CurrentWaypointIndex).Position;
        }

        public Vector2 CurrentWaypoint => WayPoints.ElementAt(CurrentWaypointIndex).Position;

        public void ChangeName(string newName)
        {
            Name = newName;
        }
    }
}