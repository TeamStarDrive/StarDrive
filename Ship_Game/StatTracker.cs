using System;
using System.Collections.Generic;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Universe;

namespace Ship_Game
{
    [StarDataType]
    public sealed class StatTracker
    {
        [StarData] Map<string, Map<int, Snapshot>> SnapsMap = new();
        [StarData] UniverseState Universe;

        public int NumRecordedTurns => SnapsMap.Count;
        public Map<int, Snapshot>[] Snapshots => SnapsMap.Values.ToArr();
        public IReadOnlyDictionary<string, Map<int, Snapshot>> SnapshotsMap => SnapsMap;

        [StarDataConstructor]
        public StatTracker(UniverseState us)
        {
            Universe = us;
        }

        public void Reset()
        {
            SnapsMap.Clear();
        }

        public void SetSnapshots(Map<string, Map<int, Snapshot>> snapshots)
        {
            SnapsMap = snapshots;
        }

        public bool ContainsDate(float starDate)
        {
            return SnapsMap.ContainsKey(starDate.StarDateString());
        }

        public void StatUpdateStarDate(float starDate)
        {
            string starDateStr = starDate.StarDateString();
            if (SnapsMap.ContainsKey(starDateStr))
                return;

            var snapshots = new Map<int, Snapshot>();
            SnapsMap[starDateStr] = snapshots;

            foreach (Empire empire in Universe.Empires)
            {
                int empireIndex = Universe.Empires.IndexOf(empire);
                snapshots[empireIndex] = new Snapshot(starDate);
            }
        }

        public bool GetAllSnapshotsFor(float starDate, out Map<int, Snapshot> snapshots)
        {
            string starDateStr = starDate.StarDateString();
            return SnapsMap.TryGetValue(starDateStr, out snapshots);
        }
        
        // This will lazily initialize snapshot entries
        // BEWARE: SIDE EFFECTS
        public bool GetSnapshot(float starDate, Empire owner, out Snapshot snapshot)
        {
            string starDateStr = starDate.StarDateString();
            if (!SnapsMap.TryGetValue(starDateStr, out var snapshots))
            {
                snapshots = new Map<int, Snapshot>();
                SnapsMap[starDateStr] = snapshots;
            }

            int empireIndex = Universe.Empires.IndexOf(owner);
            if (empireIndex != -1)
            {
                if (!snapshots.TryGetValue(empireIndex, out snapshot))
                {
                    snapshot = new Snapshot(starDate);
                    snapshots[empireIndex] = snapshot;
                }
                return true;
            }

            snapshot = null;
            return false;
        }

        public void UpdateEmpire(float starDate, Empire empire)
        {
            GetSnapshot(starDate, empire, out Snapshot _);
        }

        public void StatAddRoad(float starDate, RoadNode node, Empire owner)
        {
            if (GetSnapshot(starDate, owner, out Snapshot snapshot))
                snapshot.EmpireNodes.Add(new NRO(node.Position));
        }

        public void StatAddPlanetNode(float starDate, Planet planet)
        {
            if (GetSnapshot(starDate, planet.Owner, out Snapshot snapshot))
                snapshot.EmpireNodes.Add(new NRO(planet.Position));
        }

        public void StatAddColony(float starDate, Planet planet)
        {
            if (GetSnapshot(starDate, planet.Owner, out Snapshot snapshot))
            {
                snapshot.Events.Add(planet.Owner.data.Traits.Name + " colonized " + planet.Name);
                snapshot.EmpireNodes.Add(new NRO(planet.Position));
            }
        }
    }
}