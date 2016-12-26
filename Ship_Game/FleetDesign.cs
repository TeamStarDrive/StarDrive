using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.Gameplay;

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
        [Serialize(11)] public Orders orders;
        [Serialize(12)] public CombatState CombatState;
        [Serialize(13)] public Vector2 OrdersOffset;
        [Serialize(14)] public float OrdersRadius = 0.5f;
    }

    public sealed class FleetDesign
	{
        [Serialize(0)] public Array<FleetDataNode> Data = new Array<FleetDataNode>();
        [Serialize(1)] public int FleetIconIndex;
        [Serialize(2)] public string Name;

		public void Rotate(float facing)
		{
			foreach (FleetDataNode node in this.Data)
			{
				float angle = Math.Abs(Vector2.Zero.AngleToTarget(node.FleetOffset)) + MathHelper.ToDegrees(facing);
				angle = angle.ToRadians();
				float distance = node.FleetOffset.Length();
				node.FleetOffset = Vector2.Zero.PointFromRadians(angle, distance);
			}
		}
	}
}