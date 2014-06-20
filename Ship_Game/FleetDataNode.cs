using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;

namespace Ship_Game
{
	public class FleetDataNode
	{
		private Ship ship;

		public Guid ShipGuid;

		public Guid GoalGUID;

		public string ShipName;

		public Vector2 FleetOffset;

		public float VultureWeight = 0.5f;

		public float AttackShieldedWeight = 0.5f;

		public float AssistWeight = 0.5f;

		public float DefenderWeight = 0.5f;

		public float DPSWeight = 0.5f;

		public float SizeWeight = 0.5f;

		public float ArmoredWeight = 0.5f;

		public Orders orders;

		public Ship_Game.Gameplay.CombatState CombatState;

		public Vector2 OrdersOffset;

		public float OrdersRadius = 0.5f;

		public FleetDataNode()
		{
		}

		public Ship GetShip()
		{
			return this.ship;
		}

		public void SetShip(Ship s)
		{
			this.ship = s;
		}
	}
}