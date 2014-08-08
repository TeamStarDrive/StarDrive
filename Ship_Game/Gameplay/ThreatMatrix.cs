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

        public float StrengthOfAllEmpireShipsInBorders(Empire them)
        {
            float str = 0f;
            int count = 0;
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if (EmpireManager.GetEmpireByName(pin.Value.EmpireName) == them  && pin.Value.InBorders)   
                str = str + pin.Value.Strength +1;
                count++;
            }
            return str*count;
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

        public void UpdatePin(Ship ship, bool ShipinBorders)
        {
            if (!ShipinBorders && !this.Pins.ContainsKey(ship.guid))
                return;
            
            if ( !this.Pins.ContainsKey(ship.guid))
            {
                ThreatMatrix.Pin pin = new ThreatMatrix.Pin()
                {
                    Position = ship.Center,
                    Strength = ship.GetStrength(),
                    EmpireName = ship.loyalty.data.Traits.Name,
                    InBorders = ShipinBorders
                };
                this.Pins.Add(ship.guid, pin);
                return;
            }
            this.Pins[ship.guid].Velocity = ship.Center - this.Pins[ship.guid].Position;
            this.Pins[ship.guid].Position = ship.Center;
            if (ship.Role == "Subspace Projector" && this.Pins[ship.guid].InBorders && ship.Active)
                return;
            this.Pins[ship.guid].InBorders = ShipinBorders;
        }
        public void ClearBorders ()
        {
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if (pin.Value.InBorders)
                    pin.Value.InBorders = false;
            }
        }
        public void ScrubMatrix(bool scrubborders)
        {
            if (!scrubborders && Empire.universeScreen.MasterShipList.Count > this.Pins.Count + 10f)
                return;
            List<Guid> guids = new List<Guid>();
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                
                
                if(Empire.universeScreen.MasterShipList.Select(guid=>guid.guid).Contains(pin.Key))
                {
                    pin.Value.InBorders =false;
                    continue;
                }
                guids.Add(pin.Key);
            }

            foreach (Guid kill in guids)
            {
                this.Pins.Remove(kill);
            }
            guids.Clear();
        }

        public bool ShipInOurBorders(Ship ship)
        {
            if(!this.Pins.Keys.Contains(ship.guid))
                return false;
            if(this.Pins[ship.guid].InBorders)
            return true;
            return false;
        }
        public List<Guid> GetAllGUIDsInOurBorders()
        {
            if(this.Pins.Values.Where(inborder => inborder.InBorders).Count()==0)
                return null;
            
            List<Guid> temp = new List<Guid>();
            foreach(Guid guid in this.Pins.Keys)
            {
               if(!this.Pins[guid].InBorders)
                   continue;
                temp.Add(guid);

            }

            
            return temp;


        }

		public class Pin
		{
			public Vector2 Position;

			public float Strength;

			public Vector2 Velocity;

			public string EmpireName;

            public bool InBorders;

			public Pin()
			{
			}
		}
	}
}