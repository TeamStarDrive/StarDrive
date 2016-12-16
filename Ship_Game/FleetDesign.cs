using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using MsgPack.Serialization;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed class FleetDataNode
    {
        [MessagePackIgnore] public Ship Ship { get; set; }

        [MessagePackMember(0)] public Guid ShipGuid;
        [MessagePackMember(1)] public Guid GoalGUID;
        [MessagePackMember(2)] public string ShipName;
        [MessagePackMember(3)] public Vector2 FleetOffset;
        [MessagePackMember(4)] public float VultureWeight = 0.5f;
        [MessagePackMember(5)] public float AttackShieldedWeight = 0.5f;
        [MessagePackMember(6)] public float AssistWeight = 0.5f;
        [MessagePackMember(7)] public float DefenderWeight = 0.5f;
        [MessagePackMember(8)] public float DPSWeight = 0.5f;
        [MessagePackMember(9)] public float SizeWeight = 0.5f;
        [MessagePackMember(10)] public float ArmoredWeight = 0.5f;
        [MessagePackMember(11)] public Orders orders;
        [MessagePackMember(12)] public CombatState CombatState;
        [MessagePackMember(13)] public Vector2 OrdersOffset;
        [MessagePackMember(14)] public float OrdersRadius = 0.5f;
    }

    public sealed class FleetDesign
	{
        [MessagePackMember(0)] public List<FleetDataNode> Data = new List<FleetDataNode>();
        [MessagePackMember(1)] public int FleetIconIndex;
        [MessagePackMember(2)] public string Name;

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