using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
	public sealed class ThreatMatrix
	{
        public ConcurrentDictionary<Guid, Pin> Pins = new ConcurrentDictionary<Guid, Pin>();
        public ConcurrentDictionary<Guid, Ship> Ship = new ConcurrentDictionary<Guid, Ship>();

        public float StrengthOfAllEmpireShipsInBorders(Empire them)
        {
            float str = 0f;
            //int count = 0;
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if (EmpireManager.GetEmpireByName(pin.Value.EmpireName) == them  && pin.Value.InBorders)   
                str = str + pin.Value.Strength +1;
                
            }
            return str;// *count;
        }
        public float StrengthOfAllThreats(Empire empire)
        {
            float str = 0f;
            //int count = 0;
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if(pin.Value.EmpireName == string.Empty)
                    continue;
                Relationship rel;
                Empire emp = EmpireManager.GetEmpireByName(pin.Value.EmpireName);
                if(!empire.TryGetRelations(emp,out rel))
                    continue;
               if(rel.Treaty_Alliance)
                   continue;
                str+=pin.Value.Strength;               

            }
            return str;// *count;
        }

		public Vector2 GetPositionOfNearestEnemyWithinRadius(Vector2 Position, float Radius, Empire Us)
		{
			Array<ThreatMatrix.Pin> Enemies = new Array<ThreatMatrix.Pin>();
			foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
			{
				if (Vector2.Distance(Position, pin.Value.Position) >= Radius || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == Us 
                    || !Us.isFaction && !EmpireManager.GetEmpireByName(pin.Value.EmpireName).isFaction 
                    && !Us.GetRelations(EmpireManager.GetEmpireByName(pin.Value.EmpireName)).AtWar)
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

		public Array<ThreatMatrix.Pin> PingRadar(Vector2 Position, float Radius)
		{
			Array<ThreatMatrix.Pin> retList = new Array<ThreatMatrix.Pin>();
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
      
        public Array<GameplayObject> PingRadarOBJ(Vector2 Position, float Radius)
        {
            Array<GameplayObject> retList = new Array<GameplayObject>();
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if (Vector2.Distance(Position, pin.Value.Position) >= Radius)
                {
                    continue;
                }
                retList.Add(pin.Value.Ship);
            }
            return retList;
        }
        public Array<Ship> PingRadarShip(Vector2 Position, float Radius)
        {
            Array<Ship> retList = new Array<Ship>();
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if (Vector2.Distance(Position, pin.Value.Position) >= Radius)
                {
                    continue;
                }
                retList.Add(pin.Value.Ship);
            }
            return retList;
        }
        public Ship[] PingRadarShip(Vector2 position, float radius,Empire empire)
        {
            Array<Ship> retList = new Array<Ship>();            
            Ship ship;            
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if (position.OutsideRadius(pin.Value.Position , radius)) continue;
                
                ship = pin.Value.Ship;
                if (ship == null) continue;

                if (!empire.IsEmpireAttackable(ship.loyalty, ship)) continue;
                retList.Add(pin.Value.Ship);
                
            }
            return retList.ToArray();
        }
        public Map<Vector2, Ship[]> PingRadarShipClustersByVector(Vector2 position, float radius, float granularity,Empire empire)
        {
            var retList = new Map<Vector2, Ship[]>();
            Ship[] pings = PingRadarShip(position,radius,empire);
            HashSet<Ship > filter = new HashSet<Ship>();
            
            foreach(Ship ship in pings)
            {
                if (ship == null || filter.Contains(ship) || retList.ContainsKey(ship.Center))
                    continue;

                Ship[] cluster = PingRadarShip(ship.Center, granularity,empire);
                if (cluster.Length == 0)
                    continue;
                retList.Add(ship.Center, cluster);                
                filter.UnionWith(cluster);

            }
            return retList;

        }
        public Map<Ship, Ship[]> PingRadarShipClustersByShip(Vector2 position, float radius, float granularity, Empire empire)
        {
            var retList = new Map<Ship, Ship[]>();
            Ship[] pings = PingRadarShip(position, radius, empire);
            HashSet<Ship> filter = new HashSet<Ship>();

            foreach (Ship ship in pings)
            {
                if (ship == null || filter.Contains(ship) || retList.ContainsKey(ship))
                    continue;

                Ship[] cluster = PingRadarShip(ship.Center, granularity, empire);
                if (cluster.Length == 0)
                    continue;
                retList.Add(ship, cluster);
                filter.UnionWith(cluster);

            }
            return retList;

        }
        public Map<Vector2, float> PingRadarStrengthClusters(Vector2 Position, float Radius, float granularity, Empire empire)
        {
            Map<Vector2, float> retList = new Map<Vector2, float>();
            Ship[] pings = PingRadarShip(Position, Radius, empire);
            HashSet<Ship> filter = new HashSet<Ship>();

            foreach (Ship ship in pings)
            {
                if (ship == null || filter.Contains(ship) || retList.ContainsKey(ship.Center))
                    continue;

                Ship[] cluster = PingRadarShip(ship.Center, granularity, empire);
                if (cluster.Length == 0)
                    continue;
                retList.Add(ship.Center, cluster.Sum(str=> str.GetStrength()));
                filter.UnionWith(cluster);

            }
            return retList;

        }
        public Vector2 PingRadarAvgPos(Vector2 position, float radius, Empire Us)
		{
			Vector2 pos = new Vector2();
			int num = 0;
			foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
			{
                if (string.Equals(Us.data.Traits.Name, pin.Value.EmpireName) || Vector2.Distance(position, pin.Value.Position) >= radius)
                    continue;
                Empire them = EmpireManager.GetEmpireByName(pin.Value.EmpireName);
                if (!Us.isFaction && !them.isFaction && !Us.GetRelations(them).AtWar)
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


        public void ClearPinsInSensorRange(Vector2 position, float radius)
        {


            // Array<Guid> removepin = new Array<Guid>();

            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {

                if (pin.Value.Ship == null || Vector2.Distance(position, pin.Value.Position) > radius)
                    continue;
                bool insensor = Vector2.Distance(pin.Value.Ship.Center, position) <= radius;
                this.UpdatePin(pin.Value.Ship, pin.Value.InBorders, insensor);
                if (insensor)
                    continue;
                //pin.Value.Velocity = Vector2.Zero;
                //pin.Value.Position = Vector2.Zero;
                //pin.Value.Strength = 0;
                //pin.Value.EmpireName = string.Empty;
                //pin.Value.ship = null;
            }


        }
        public float PingRadarStr(Vector2 Position, float Radius, Empire Us)
		{
			float str = 0f;
            foreach (KeyValuePair<Guid, Pin> pin in this.Pins)            
            {
                if (string.Equals(Us.data.Traits.Name, pin.Value.EmpireName) || Vector2.Distance(Position, pin.Value.Position) >= Radius)
                    continue;                     
                Empire them = EmpireManager.GetEmpireByName(pin.Value.EmpireName);

                Relationship test;
                if (Us.TryGetRelations(them, out test) && test.Treaty_NAPact)
                    continue;                    

                str = str + pin.Value.Strength;
            }
			return str;
		}

        public float PingRadarStr(Vector2 Position, float Radius, Empire Us, bool factionOnly)
        {
            float str = 0f;
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if (string.Equals(Us.data.Traits.Name, pin.Value.EmpireName) || Vector2.Distance(Position, pin.Value.Position) >= Radius)
                    continue;
                Empire them = EmpireManager.GetEmpireByName(pin.Value.EmpireName);
                if (them == Us || Vector2.Distance(Position, pin.Value.Position) >= Radius
                    || (factionOnly && !them.isFaction)
                    //|| (!Us.isFaction && !EmpireManager.GetEmpireByName(pin.Value.EmpireName).isFaction 
                    //&& !Us.GetRelations()[EmpireManager.GetEmpireByName(pin.Value.EmpireName)].Treaty_NAPact))                     
                    //|| ( (!them.isFaction && !Us.isFaction) && Us.GetRelations(them).Treaty_NAPact)
                    )
                    continue;

                Relationship test;
                if (Us.TryGetRelations(them, out test) && test.Treaty_NAPact)
                    continue;

                str = str + pin.Value.Strength;
            }
            return str;
        }

		public void UpdatePin(Ship ship)
		{
            if (ship != null && !ship.Active)
            {
                Pin test;
                this.Pins.TryRemove(ship.guid, out test);
                return;
            }
            
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
  
    
        public void UpdatePin(Ship ship, bool ShipinBorders,bool InSensorRadius)
        {
            //if (!InSensorRadius)
            //    return;
            ThreatMatrix.Pin pin = null;
            bool exists = false;
            //try {
                exists = this.Pins.TryGetValue(ship.guid, out pin);
            //}
            //catch
            //{

            //    return;
            //}
            if (pin == null && InSensorRadius)
            {
                pin = new ThreatMatrix.Pin()
                {
                    Position = ship.Center,
                    Strength = ship.GetStrength(),
                    EmpireName = ship.loyalty.data.Traits.Name,
                    Ship = ship,
                    InBorders = ShipinBorders
                };
                if (exists)
                {
                    this.Pins[ship.guid] = pin;
                    return;
                }
                else
                {
                    this.Pins.TryAdd(ship.guid, pin);
                }

            }
            else if(pin != null )
            {
                if (InSensorRadius)
                {
                    pin.Velocity = ship.Center - pin.Position;
                    pin.Position = ship.Center;
                    pin.Strength = ship.GetStrength();
                    pin.EmpireName = ship.loyalty.data.Traits.Name;
                }
                pin.InBorders = ShipinBorders;
                if (pin.Ship != ship)
                    pin.Ship = ship;
                return;
            }
        }
        public void ClearBorders ()
        {
            foreach (KeyValuePair<Guid, Pin> pin in Pins)
            {
                if (pin.Value.InBorders)
                    if (pin.Value.Ship != null && pin.Value.Ship.Active && pin.Value.Ship.Name == "Subspace Projector" )
                        continue;
                    pin.Value.InBorders = false;
            }
        }
        public void UpdatePinShip(Ship s, Guid guid)
        {
            if (!s.Active)
            {
                Pins.TryRemove(guid, out Pin _);
            }
            else if (!Pins.TryGetValue(s.guid, out Pin pin))
            {
                Pins.TryAdd(s.guid, new Pin
                {
                    Position   = s.Center,
                    Strength   = s.GetStrength(),
                    EmpireName = s.loyalty.data.Traits.Name
                });
            }
            else
            {
                pin.Velocity = s.Center - pin.Position;
                pin.Position = s.Center;
            }

        }
        public void ScrubMatrix()
        {
            var pinsToRemove = new Array<Guid>();
            foreach (var pin in Pins)
                if (pin.Value.EmpireName == string.Empty)
                    pinsToRemove.Add(pin.Key);
            foreach (Guid removme in pinsToRemove)
                Pins.TryRemove(removme, out Pin pin);
        }

        public bool ShipInOurBorders(Ship s)
        {
            if (!Pins.TryGetValue(s.guid ,out Pin pin)) return false;
            return pin.InBorders;
        }
        public Array<Ship> FindShipsInOurBorders()
        {
            var temp = new Array<Ship>();
            foreach (Pin p in Pins.Values)
               if (p.InBorders && p.Ship !=null)
                   temp.Add(p.Ship);
            return temp;
        }

        

		public class Pin
		{
            [Serialize(0)] public Vector2 Position;
            [Serialize(1)] public Vector2 Velocity;
            [Serialize(2)] public float Strength;
            [Serialize(3)] public string EmpireName;
            [Serialize(4)] public bool InBorders;

            [XmlIgnore][JsonIgnore] public Ship Ship;
		}
	}
}