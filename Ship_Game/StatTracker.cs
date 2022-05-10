using System;
using System.Collections.Generic;
using SDUtils;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed class StatTracker
    {
        static SerializableDictionary<string, SerializableDictionary<int, Snapshot>> SnapsMap =
           new SerializableDictionary<string, SerializableDictionary<int, Snapshot>>();

        public static int NumRecordedTurns => SnapsMap.Count;
        public static SerializableDictionary<int, Snapshot>[] Snapshots => SnapsMap.Values.ToArr();
        public static IReadOnlyDictionary<string, SerializableDictionary<int, Snapshot>> SnapshotsMap => SnapsMap;

        public static void Reset()
        {
            SnapsMap.Clear();
        }

        public static void SetSnapshots(SerializableDictionary<string, SerializableDictionary<int, Snapshot>> snapshots)
        {
            SnapsMap = snapshots;
        }

        public static bool ContainsDate(float starDate)
        {
            return SnapsMap.ContainsKey(starDate.StarDateString());
        }

        public static void StatUpdateStarDate(float starDate)
        {
            string starDateStr = starDate.StarDateString();
            if (SnapsMap.ContainsKey(starDateStr))
                return;

            var snapshots = new SerializableDictionary<int, Snapshot>();
            SnapsMap[starDateStr] = snapshots;

            foreach (Empire empire in EmpireManager.Empires)
            {
                int empireIndex = EmpireManager.Empires.IndexOf(empire);
                snapshots[empireIndex] = new Snapshot(starDate);
            }
        }

        public static bool GetAllSnapshotsFor(float starDate, out SerializableDictionary<int, Snapshot> snapshots)
        {
            string starDateStr = starDate.StarDateString();
            return SnapsMap.TryGetValue(starDateStr, out snapshots);
        }
        
        // This will lazily initialize snapshot entries
        // BEWARE: SIDE EFFECTS
        public static bool GetSnapshot(float starDate, Empire owner, out Snapshot snapshot)
        {
            string starDateStr = starDate.StarDateString();
            if (!SnapsMap.TryGetValue(starDateStr, out var snapshots))
            {
                snapshots = new SerializableDictionary<int, Snapshot>();
                SnapsMap[starDateStr] = snapshots;
            }

            int empireIndex = EmpireManager.Empires.IndexOf(owner);
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

        public static void UpdateEmpire(float starDate, Empire empire)
        {
            GetSnapshot(starDate, empire, out Snapshot _);
        }

        public static void StatAddRoad(float starDate, RoadNode node, Empire owner)
        {
            if (GetSnapshot(starDate, owner, out Snapshot snapshot))
                snapshot.EmpireNodes.Add(new NRO(node.Position));
        }

        public static void StatAddPlanetNode(float starDate, Planet planet)
        {
            if (GetSnapshot(starDate, planet.Owner, out Snapshot snapshot))
                snapshot.EmpireNodes.Add(new NRO(planet.Center));
        }

        public static void StatAddColony(float starDate, Planet planet)
        {
            if (GetSnapshot(starDate, planet.Owner, out Snapshot snapshot))
            {
                snapshot.Events.Add(planet.Owner.data.Traits.Name + " colonized " + planet.Name);
                snapshot.EmpireNodes.Add(new NRO(planet.Center));
            }
        }
    }
}