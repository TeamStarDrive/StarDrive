using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class EmpireManager
	{
		public static List<Empire> EmpireList;

        private static Dictionary<string, Empire> EmpireDict; 

		static EmpireManager()
		{
			EmpireManager.EmpireList = new List<Empire>();
            EmpireManager.EmpireDict = new Dictionary<string, Empire>();
        }

		public EmpireManager()
		{
		}

		public static Empire GetEmpireByName(string name)
        {
            Empire e = null;
            if (name != null && EmpireDict.TryGetValue(name, out e))
            {
                return e;
            }
            else
            foreach (Empire empire in EmpireManager.EmpireList)
            {
                    if (string.Equals(empire.data.Traits.Name, name))
                    {
                        EmpireDict.Add(name, empire);
                        return empire;
                    }
            }
            return (Empire)null;
        }

        public static Empire GetPlayerEmpire()
        {
            foreach (Empire empire in EmpireManager.EmpireList)
            {
                if (empire.isPlayer)
                    return empire;
            }
            return (Empire)null;
        }
        public static List<Empire> GetAllies(Empire e)
        {
            Ship_Game.Gameplay.Relationship rel;
            List<Empire> allies = new List<Empire>();
            foreach (Empire empire in EmpireManager.EmpireList)
            {

                if (empire.isPlayer || e.isFaction || e.MinorRace )
                    continue;
                e.GetRelations().TryGetValue(empire, out rel);
                if (rel == null || !rel.Known || !rel.Treaty_Alliance)
                    continue;

                allies.Add(empire);
            }
            return allies;
        }
        public static List<Empire> GetTradePartners(Empire e)
        {
            Ship_Game.Gameplay.Relationship rel;
            List<Empire> allies = new List<Empire>();
            foreach (Empire empire in EmpireManager.EmpireList)
            {

                if (empire.isPlayer || e.isFaction || e.MinorRace)
                    continue;
                e.GetRelations().TryGetValue(empire, out rel);
                if (rel == null || !rel.Known || !rel.Treaty_Trade)
                    continue;

                allies.Add(empire);
            }
            return allies;
        }
	}
}