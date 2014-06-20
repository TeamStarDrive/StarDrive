using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ship_Game.Gameplay
{
	public class ThreatMatrix
	{
		public Dictionary<Guid, ThreatMatrix.Pin> Pins = new Dictionary<Guid, ThreatMatrix.Pin>();

		private object thislock = new object();

		public ThreatMatrix()
		{
		}

		public Vector2 GetPositionOfNearestEnemyWithinRadius(Vector2 Position, float Radius, Empire Us)
		{
			List<ThreatMatrix.Pin> Enemies = new List<ThreatMatrix.Pin>();
			foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
			{
				if (Vector2.Distance(Position, pin.Value.Position) >= Radius || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == Us || !Us.isFaction && !EmpireManager.GetEmpireByName(pin.Value.EmpireName).isFaction && !Us.GetRelations()[EmpireManager.GetEmpireByName(pin.Value.EmpireName)].AtWar)
				{
					continue;
				}
				Enemies.Add(pin.Value);
			}
			if (Enemies.Count == 0)
			{
				return Vector2.Zero;
			}
			IOrderedEnumerable<ThreatMatrix.Pin> sortedList = 
				from pos in Enemies
				orderby Vector2.Distance(pos.Position, Position)
				select pos;
			return sortedList.First<ThreatMatrix.Pin>().Position;
		}

		public List<ThreatMatrix.Pin> PingRadar(Vector2 Position, float Radius)
		{
			List<ThreatMatrix.Pin> retList = new List<ThreatMatrix.Pin>();
			foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
			{
				if (Vector2.Distance(Position, pin.Value.Position) >= Radius)
				{
					continue;
				}
				retList.Add(pin.Value);
			}
			return retList;
		}

		public Vector2 PingRadarAvgPos(Vector2 Position, float Radius, Empire Us)
		{
			Vector2 pos = new Vector2();
			int num = 0;
			foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
			{
				if (Vector2.Distance(Position, pin.Value.Position) >= Radius || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == Us || !Us.isFaction && !EmpireManager.GetEmpireByName(pin.Value.EmpireName).isFaction && !Us.GetRelations()[EmpireManager.GetEmpireByName(pin.Value.EmpireName)].AtWar)
				{
					continue;
				}
				num++;
				pos = pos + pin.Value.Position;
			}
			if (num > 0)
			{
				pos = pos / (float)num;
			}
			return pos;
		}

		public float PingRadarStr(Vector2 Position, float Radius, Empire Us)
		{
			float str = 0f;
			foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
			{
				if (Vector2.Distance(Position, pin.Value.Position) >= Radius || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == Us || !Us.isFaction && !EmpireManager.GetEmpireByName(pin.Value.EmpireName).isFaction && !Us.GetRelations()[EmpireManager.GetEmpireByName(pin.Value.EmpireName)].AtWar)
				{
					continue;
				}
				str = str + pin.Value.Strength;
			}
			return str;
		}

		public void UpdatePin(Ship ship)
		{
			if (!this.Pins.ContainsKey(ship.guid))
			{
				ThreatMatrix.Pin pin = new ThreatMatrix.Pin()
				{
					Position = ship.Center,
					Strength = ship.GetStrength(),
					EmpireName = ship.loyalty.data.Traits.Name
				};
				this.Pins.Add(ship.guid, pin);
				return;
			}
			this.Pins[ship.guid].Velocity = ship.Center - this.Pins[ship.guid].Position;
			this.Pins[ship.guid].Position = ship.Center;
		}

		public class Pin
		{
			public Vector2 Position;

			public float Strength;

			public Vector2 Velocity;

			public string EmpireName;

			public Pin()
			{
			}
		}
	}
}