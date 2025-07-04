using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;
using Ship_Game.Ships.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public partial class Empire
    {
        [StarData] public Array<FleetPatrol> FleetPatrols { get; private set; } = new();

        public void AddPatrolRoute(Fleet fleet, WayPoints waypoints)
        {
            WayPoints clonedWayPoints = new WayPoints();
            clonedWayPoints.Set(waypoints.ToArray());
            // todo create custom names and check if exists
            FleetPatrols.Add(new FleetPatrol(fleet.Name, clonedWayPoints));
        }

        public FleetPatrol GetLastPatrolRoute() => FleetPatrols.LastOrDefault();
    }
}
