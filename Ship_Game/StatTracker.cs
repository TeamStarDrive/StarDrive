using System;

namespace Ship_Game
{
	public class StatTracker
	{
		public static SerializableDictionary<string, SerializableDictionary<int, Snapshot>> SnapshotsDict;

		static StatTracker()
		{
			StatTracker.SnapshotsDict = new SerializableDictionary<string, SerializableDictionary<int, Snapshot>>();
		}

		public StatTracker()
		{
		}
	}
}