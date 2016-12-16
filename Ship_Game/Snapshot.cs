using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MsgPack.Serialization;

namespace Ship_Game
{
    public sealed class NRO
    {
        [MessagePackMember(0)] public Vector2 Node;
        [MessagePackMember(1)] public float Radius;
        [MessagePackMember(2)] public float StarDateMade;
    }

    public sealed class Snapshot
	{
		[MessagePackMember(0)] public List<NRO> EmpireNodes = new List<NRO>();
        [MessagePackMember(1)] public int ShipCount;
        [MessagePackMember(2)] public float MilitaryStrength;
        [MessagePackMember(3)] public float Population;
        [MessagePackMember(4)] public List<string> Events = new List<string>();
        [MessagePackMember(5)] public float TaxRate;
        [MessagePackMember(6)] public float StarDate;
        [MessagePackMember(7)] public int TotalShips;
        [MessagePackMember(8)] public int TotalShipsKilled;
        [MessagePackMember(9)] public int TotalShipsLost;
        [MessagePackMember(10)] public int TotalGroundTroopsKilled;
        [MessagePackMember(11)] public int TotalGroundTroopsLost;
        [MessagePackMember(12)] public int TotalMoney;
        [MessagePackMember(13)] public int TotalMaintenance;
        [MessagePackMember(14)] public int TotalPopulation;

		public Snapshot(float date)
		{
			StarDate = date;
		}

		public Snapshot()
		{
		}
	}
}