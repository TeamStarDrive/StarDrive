using Microsoft.Xna.Framework;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    [StarDataType]
    public struct NRO
    {
        [StarData] public Vector2 Node;
        [StarData] public float Radius;
        public NRO(Vector2 pos, float radius = 300000f)
        {
            Node = pos;
            Radius = radius;
        }
    }

    [StarDataType]
    public sealed class Snapshot
    {
        [StarData] public Array<NRO> EmpireNodes = new Array<NRO>();
        [StarData] public int ShipCount;
        [StarData] public float MilitaryStrength;
        [StarData] public float Population;
        [StarData] public Array<string> Events = new Array<string>();
        [StarData] public float TaxRate;
        [StarData] public float StarDate;
        [StarData] public int TotalShips;
        [StarData] public int TotalShipsKilled;
        [StarData] public int TotalShipsLost;
        [StarData] public int TotalGroundTroopsKilled;
        [StarData] public int TotalGroundTroopsLost;
        [StarData] public int TotalMoney;
        [StarData] public int TotalMaintenance;
        [StarData] public int TotalPopulation;

        public Snapshot(float date)
        {
            StarDate = date;
        }

        public Snapshot()
        {
        }
    }
}