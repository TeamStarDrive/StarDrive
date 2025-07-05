using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;
using Ship_Game.Ships.AI;

namespace Ship_Game
{
    public partial class Empire
    {
        [StarData] public Array<FleetPatrol> FleetPatrols { get; private set; } = new();

        public FleetPatrol AddPatrolRoute(Fleet fleet, WayPoints waypoints)
        {
            WayPoints clonedWayPoints = new WayPoints();
            clonedWayPoints.Set(waypoints.ToArray());
            FleetPatrol newPatrol = new FleetPatrol(GetNewPatrolName(fleet.Name), clonedWayPoints);
            FleetPatrols.Add(newPatrol);
            return newPatrol;
        }

        string GetNewPatrolName(string fleetName)
        {
            string baseName = fleetName;
            string uniqueName = $"{fleetName} patrol";
            int suffix = 1;
            while (FleetPatrols.Any(p => p.Name == uniqueName))
            {
                uniqueName = $"{baseName}_{suffix++}";
            }

            return uniqueName;
        }
    }
}
