using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class FleetDataNode
    {
        [XmlIgnore][JsonIgnore] public Ship Ship { get; set; }

        [Serialize(0)] public Guid ShipGuid;
        [Serialize(1)] public Guid GoalGUID;
        [Serialize(2)] public string ShipName;
        [Serialize(3)] public Vector2 FleetOffset;
        [Serialize(4)] public float VultureWeight = 0.5f;
        [Serialize(5)] public float AttackShieldedWeight = 0.5f;
        [Serialize(6)] public float AssistWeight = 0.5f;
        [Serialize(7)] public float DefenderWeight = 0.5f;
        [Serialize(8)] public float DPSWeight = 0.5f;
        [Serialize(9)] public float SizeWeight = 0.5f;
        [Serialize(10)] public float ArmoredWeight = 0.5f;
        [XmlElement(ElementName = "orders")]
        [Serialize(11)] public Orders Order;
        [Serialize(12)] public CombatState CombatState;
        [Serialize(13)] public Vector2 OrdersOffset;
        [Serialize(14)] public float OrdersRadius = 500000;//0.5f;

        public float ApplyWeight(float shipStat, float statAvg, float fleetWeight)
        {
            if (fleetWeight > 0.49f && fleetWeight < 0.51f)
                return 0;

            return shipStat > statAvg
                ? 2 * (-0.5f + fleetWeight)
                : 4 * ( 0.5f - fleetWeight);
        }

        public float ApplyFleetWeight(Fleet fleet, Ship potential)
        {
            float weight = 0;           
            if ((DefenderWeight > 0.49f && DefenderWeight < 0.51f) ||
                (AssistWeight > 0.49f && AssistWeight < 0.51f))
                return weight;
            foreach (Ship ship in fleet.Ships)
            {
                if (potential.AI.Target == ship)
                    weight -= 0.5f + DefenderWeight;
                if (ship.AI.Target == potential)
                    weight -= 5.0f + AssistWeight;
            }
            return weight;
        }

        public FleetDataNode Clone()
        {
            return (FleetDataNode)MemberwiseClone();
        }
    }

    public sealed class FleetDesign
    {
        [Serialize(0)] public Array<FleetDataNode> Data = new Array<FleetDataNode>();
        [Serialize(1)] public int FleetIconIndex;
        [Serialize(2)] public string Name;

        public void Rotate(float facing)
        {
            foreach (FleetDataNode node in Data)
            {
                float radians = facing + Vector2.Zero.RadiansToTarget(node.FleetOffset);
                float distance = node.FleetOffset.Length();
                node.FleetOffset = Vector2.Zero.PointFromRadians(radians, distance);
            }
        }
    }
}