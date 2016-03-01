using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class EmpireManager
	{
		public static List<Empire> EmpireList;

		static EmpireManager()
		{
			EmpireManager.EmpireList = new List<Empire>();
		}

		public EmpireManager()
		{
		}

		public static Empire GetEmpireByName(string name)
        {
            foreach (Empire empire in EmpireManager.EmpireList)
            {
                if (string.Equals(empire.data.Traits.Name, name))
                    return empire;
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
	}
}