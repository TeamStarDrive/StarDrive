using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using System.Collections.Concurrent;
using System.Threading.Tasks;
namespace Ship_Game.Gameplay
{
	public class ThreatMatrix
	{
		//public Dictionary<Guid, ThreatMatrix.Pin> Pins = new Dictionary<Guid, ThreatMatrix.Pin>();
        public ConcurrentDictionary<Guid, ThreatMatrix.Pin> Pins = new ConcurrentDictionary<Guid, ThreatMatrix.Pin>();
        //public Dictionary<Guid, Ship> ship = new Dictionary<Guid, Ship>();
        public ConcurrentDictionary<Guid, Ship> ship = new ConcurrentDictionary<Guid, Ship>();
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
				this.Pins.TryAdd(ship.guid, pin);
				return;
			}
			this.Pins[ship.guid].Velocity = ship.Center - this.Pins[ship.guid].Position;
			this.Pins[ship.guid].Position = ship.Center;
		}

        public void UpdatePin(Ship ship, bool ShipinBorders)
        {
            ThreatMatrix.Pin pin = null;
            bool exists = this.Pins.TryGetValue(ship.guid, out pin);

            if (pin == null)
            {
                pin = new ThreatMatrix.Pin()
                {
                    Position = ship.Center,
                    Strength = ship.GetStrength(),
                    EmpireName = ship.loyalty.data.Traits.Name,
                    ship = ship,
                    InBorders = ShipinBorders
                };
                if (exists)
                {
                    this.Pins[ship.guid] = pin;
                    return;
                }

            }
            else
            {
                this.Pins[ship.guid].Velocity = ship.Center - this.Pins[ship.guid].Position;
                this.Pins[ship.guid].Position = ship.Center;

                this.Pins[ship.guid].InBorders = ShipinBorders;
                if (this.Pins[ship.guid].ship == null) //ShipinBorders &&
                    this.Pins[ship.guid].ship = ship;
                return;
            }


            //lock (this.Pins)            
                this.Pins.TryAdd(ship.guid, pin);

            


        }
        public void ClearBorders ()
        {
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if (pin.Value.InBorders)
                    if (pin.Value.ship != null && pin.Value.ship.Active && pin.Value.ship.Role == "Subspace Projector" )
                        continue;
                    pin.Value.InBorders = false;
            }
        }
        public void UpdatePinShip(Ship ship, Guid guid)
        {
            if (!this.Pins.ContainsKey(ship.guid))
            {
                ThreatMatrix.Pin pin = new ThreatMatrix.Pin()
                {
                    Position = ship.Center,
                    Strength = ship.GetStrength(),
                    EmpireName = ship.loyalty.data.Traits.Name
                };
                this.Pins.TryAdd(ship.guid, pin);
                return;
            }
            this.Pins[ship.guid].Velocity = ship.Center - this.Pins[ship.guid].Position;
            this.Pins[ship.guid].Position = ship.Center;
        }
        public void ScrubMatrix()
        {

            if (this.Pins.Count < Empire.universeScreen.MasterShipList.Count)
                return;
            HashSet<Ship> shiphash = new HashSet<Ship>(Empire.universeScreen.MasterShipList);
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)

                if (shiphash.Select(guid => guid.guid).Contains(pin.Key))
                    continue;
                else
                {

                    ThreatMatrix.Pin remove = null;
                    this.Pins.TryRemove(pin.Key, out remove);
                }


        }

        public bool ShipInOurBorders(Ship ship)
        {
            if(!this.Pins.Keys.Contains(ship.guid))
                return false;
            if(this.Pins[ship.guid].InBorders)
            return true;
            return false;
        }
        public List<Ship> GetAllShipsInOurBorders()
        {
            List<Ship> temp = new List<Ship>();


            foreach(Pin ship in this.Pins.Values)
            {
               if(ship.InBorders && ship.ship !=null)
                   temp.Add(ship.ship);
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
            [XmlIgnore]
            public Ship ship;

			public Pin()
			{
			}
		}
	}
}