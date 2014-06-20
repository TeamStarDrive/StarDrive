using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Snapshot
	{
		public List<NRO> EmpireNodes = new List<NRO>();

		public int ShipCount;

		public float MilitaryStrength;

		public float Population;

		public List<string> Events = new List<string>();

		public float TaxRate;

		public float StarDate;

		public int TotalShips;

		public int TotalShipsKilled;

		public int TotalShipsLost;

		public int TotalGroundTroopsKilled;

		public int TotalGroundTroopsLost;

		public int TotalMoney;

		public int TotalMaintenance;

		public int TotalPopulation;

		public Snapshot(float date)
		{
			this.StarDate = date;
		}

		public Snapshot()
		{
		}
	}
}