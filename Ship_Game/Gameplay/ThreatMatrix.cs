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
	public sealed class ThreatMatrix
	{
		//public Dictionary<Guid, ThreatMatrix.Pin> Pins = new Dictionary<Guid, ThreatMatrix.Pin>();
        public ConcurrentDictionary<Guid, ThreatMatrix.Pin> Pins = new ConcurrentDictionary<Guid, ThreatMatrix.Pin>();
        //public Dictionary<Guid, Ship> ship = new Dictionary<Guid, Ship>();
        public ConcurrentDictionary<Guid, Ship> ship = new ConcurrentDictionary<Guid, Ship>();
		private object thislock = new object();
        List<Guid> purge = new List<Guid>();
		public ThreatMatrix()
		{
		}

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
                if(!empire.GetRelations().TryGetValue(emp,out rel))
                    continue;
               if(rel.Treaty_Alliance)
                   continue;
                str+=pin.Value.Strength;               

            }
            return str;// *count;
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
      
        public List<GameplayObject> PingRadarOBJ(Vector2 Position, float Radius)
        {
            List<GameplayObject> retList = new List<GameplayObject>();
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if (Vector2.Distance(Position, pin.Value.Position) >= Radius)
                {
                    continue;
                }
                retList.Add(pin.Value.ship);
            }
            return retList;
        }
        public List<Ship> PingRadarShip(Vector2 Position, float Radius)
        {
            List<Ship> retList = new List<Ship>();
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if (Vector2.Distance(Position, pin.Value.Position) >= Radius)
                {
                    continue;
                }
                retList.Add(pin.Value.ship);
            }
            return retList;
        }
        public List<Ship> PingRadarShip(Vector2 Position, float Radius,Empire empire)
        {
            List<Ship> retList = new List<Ship>();
            Ship ship;
            Relationship rel;
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if (Vector2.Distance(Position, pin.Value.Position) >= Radius)
                {
                    continue;
                }              
                ship = pin.Value.ship;
                if (ship == null)
                    continue;                
                if (!empire.GetRelations().TryGetValue(ship.loyalty, out rel) ||  ship.loyalty != empire
                             && (ship.loyalty.isFaction || rel.AtWar)
                             || (pin.Value.InBorders && !rel.Treaty_OpenBorders)
                             || (ship.isColonyShip && 
                             ( ship.GetSystem() != null && rel.WarnedSystemsList.Contains(ship.GetSystem().guid )))
                    
                             )
                    
                    retList.Add(pin.Value.ship);
            }
            return retList;
        }
        public Dictionary<Vector2,List<Ship>> PingRadarClusters(Vector2 Position, float Radius, float granularity,Empire empire)
        {
            Dictionary<Vector2, List<Ship>> retList = new Dictionary<Vector2, List<Ship>>();
            List<Ship> pings = new List<Ship>(PingRadarShip(Position,Radius,empire));
            HashSet<Ship > filter = new HashSet<Ship>();
            
            foreach(Ship ship in pings)
            {
                if (ship == null || filter.Contains(ship) || retList.ContainsKey(ship.Center))
                    continue;
              
                        List<Ship> cluster = PingRadarShip(ship.Center, granularity,empire);
                if (cluster.Count == 0)
                    continue;
                retList.Add(ship.Center, cluster);                
                filter.UnionWith(cluster);

            }
            return retList;

        }
        public Dictionary<Vector2, float> PingRadarThreatClusters(Vector2 Position, float Radius, float granularity, Empire empire)
        {
            Dictionary<Vector2, float> retList = new Dictionary<Vector2, float>();
            List<Ship> pings = new List<Ship>(PingRadarShip(Position, Radius, empire));
            HashSet<Ship> filter = new HashSet<Ship>();

            foreach (Ship ship in pings)
            {
                if (ship == null || filter.Contains(ship) || retList.ContainsKey(ship.Center))
                    continue;

                List<Ship> cluster = PingRadarShip(ship.Center, granularity, empire);
                if (cluster.Count == 0)
                    continue;
                retList.Add(ship.Center, cluster.Sum(str=> str.GetStrength()));
                filter.UnionWith(cluster);

            }
            return retList;

        }
        public Vector2 PingRadarAvgPos(Vector2 Position, float Radius, Empire Us)
		{
			Vector2 pos = new Vector2();
			int num = 0;
			foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
			{
                if (string.Equals(Us.data.Traits.Name, pin.Value.EmpireName) || Vector2.Distance(Position, pin.Value.Position) >= Radius)
                    continue;
                Empire them = EmpireManager.GetEmpireByName(pin.Value.EmpireName);
                if (!Us.isFaction && !them.isFaction && !Us.GetRelations()[them].AtWar)
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

        public void ClearPinsInSensorRange2(Vector2 Position, float Radius)
        {
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {

                if (pin.Value.InBorders || pin.Value.Position == Vector2.Zero || Vector2.Distance(Position, pin.Value.Position) > Radius)
                    continue;

                //lock (pin.Value)
                {
                    if (false && pin.Value.ship == null)
                    {

                        foreach (Ship ship in Ship.universeScreen.MasterShipList)
                        {
                            if (ship.guid == pin.Key)
                            {
                                pin.Value.ship = ship;
                                break;
                            }
                        }
                        if (pin.Value.ship == null)
                        {
                            pin.Value.Position = Vector2.Zero;
                            pin.Value.ship = null;
                            pin.Value.Strength = 0;
                            pin.Value.InBorders = false;
                            pin.Value.EmpireName = string.Empty;
                            continue;
                        }
                    }

                    if (false & pin.Value.ship != null && Vector2.Distance(Position, pin.Value.ship.Center) <= Radius)
                        continue;
                }

            #if false // disabled for now
                pin.Value.Position = Vector2.Zero;
                pin.Value.ship = null;
                pin.Value.Strength = 0;
                pin.Value.InBorders = false;
                pin.Value.EmpireName = string.Empty;
            #endif

            }


        }
        public void ClearPinsInSensorRange(Vector2 Position, float Radius)
        {


            // List<Guid> removepin = new List<Guid>();

            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {

                if (pin.Value.ship == null || Vector2.Distance(Position, pin.Value.Position) > Radius)
                    continue;
                bool insensor = Vector2.Distance(pin.Value.ship.Center, Position) <= Radius;
                this.UpdatePin(pin.Value.ship, pin.Value.InBorders, insensor);
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
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)            
            {
                if (string.Equals(Us.data.Traits.Name, pin.Value.EmpireName) || Vector2.Distance(Position, pin.Value.Position) >= Radius)
                    continue;                     
                Empire them = EmpireManager.GetEmpireByName(pin.Value.EmpireName);

                Relationship test;
                if (Us.GetRelations().TryGetValue(them, out test) && test.Treaty_NAPact)
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
                    //|| ( (!them.isFaction && !Us.isFaction) && Us.GetRelations()[them].Treaty_NAPact)
                    )
                    continue;

                Relationship test;
                if (Us.GetRelations().TryGetValue(them, out test) && test.Treaty_NAPact)
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
                    ship = ship,
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
                if (pin.ship != ship)
                    pin.ship = ship;
                return;
            }
        }
        public void ClearBorders ()
        {
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if (pin.Value.InBorders)
                    if (pin.Value.ship != null && pin.Value.ship.Active && pin.Value.ship.Name == "Subspace Projector" )
                        continue;
                    pin.Value.InBorders = false;
            }
        }
        public void UpdatePinShip(Ship ship, Guid guid)
        {
            if(ship != null && !ship.Active)
            {
                Pin test;
                this.Pins.TryRemove(guid,out test);
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
        public void ScrubMatrix()
        {

            List<Guid> PinsToRemove = new List<Guid>();
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Pins)
            {
                if (pin.Value.EmpireName == string.Empty)
                    PinsToRemove.Add(pin.Key);
            }
            foreach(Guid removme in PinsToRemove)
            {
                ThreatMatrix.Pin pin;
                this.Pins.TryRemove(removme, out pin);
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