using System;
using Ship_Game.Gameplay;

namespace Ship_Game
{
	public sealed class StatTracker
	{
		public static SerializableDictionary<string, SerializableDictionary<int, Snapshot>> SnapshotsDict;

		static StatTracker()
		{
			SnapshotsDict = new SerializableDictionary<string, SerializableDictionary<int, Snapshot>>();
		}

	    public static void StatAddColony(Object add,Empire owner,UniverseScreen universeScreen)
	    {
            string starDate = universeScreen.StarDateString;
	        if (!SnapshotsDict.TryGetValue(starDate, out var stat))
	            return;
            Planet planet = add as Planet;
	        if (planet != null )
	        {
                SnapshotsDict[starDate][EmpireManager.Empires.IndexOf(owner)].Events.Add(
                        string.Concat(owner.data.Traits.Name, " colonized ", planet.Name));
                var nro = new NRO
                {
                    Node = planet.Center,
                    Radius = 300000f,
                    StarDateMade = universeScreen.StarDate
                };
                SnapshotsDict[starDate][EmpireManager.Empires.IndexOf(owner)].EmpireNodes.Add(nro);

            }	        	           	        
	    }

	    public static void StatAddRoad(Object add, Empire owner)
	    {
            string starDate = Empire.Universe.StarDateString;
	        if (!SnapshotsDict.ContainsKey(starDate))
	            return;
	        RoadNode node = add as RoadNode;
	        if (node is null) return;
            var nro = new NRO
            {
                Node = node.Position,
                Radius = 300000f,
                StarDateMade = Empire.Universe.StarDate
            };
            SnapshotsDict[starDate][EmpireManager.Empires.IndexOf(owner)].EmpireNodes.Add(nro);
        }
	}
}