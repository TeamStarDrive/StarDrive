using System;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using Ship_Game.Ships.AI;

namespace Ship_Game.Fleets
{
    [StarDataType]
    public class FleetPatrol
    {
        public string PatrolName { get; private set; }
        public WayPoints WayPoints { get; private set; } = new();

        public FleetPatrol(string name, WayPoints waypoints)
        {
            PatrolName = name;
            WayPoints = waypoints;
        }

        [StarDataConstructor] FleetPatrol() { }
    }
}