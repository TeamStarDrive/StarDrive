using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public sealed class NRO
    {
        [Serialize(0)] public Vector2 Node;
        [Serialize(1)] public float Radius;
        [Serialize(2)] public float StarDateMade;
    }

    public sealed class Snapshot
	{
		[Serialize(0)] public Array<NRO> EmpireNodes = new Array<NRO>();
        [Serialize(1)] public int ShipCount;
        [Serialize(2)] public float MilitaryStrength;
        [Serialize(3)] public float Population;
        [Serialize(4)] public Array<string> Events = new Array<string>();
        [Serialize(5)] public float TaxRate;
        [Serialize(6)] public float StarDate;
        [Serialize(7)] public int TotalShips;
        [Serialize(8)] public int TotalShipsKilled;
        [Serialize(9)] public int TotalShipsLost;
        [Serialize(10)] public int TotalGroundTroopsKilled;
        [Serialize(11)] public int TotalGroundTroopsLost;
        [Serialize(12)] public int TotalMoney;
        [Serialize(13)] public int TotalMaintenance;
        [Serialize(14)] public int TotalPopulation;

		public Snapshot(float date)
		{
			StarDate = date;
		}

		public Snapshot()
		{
		}
	}
}