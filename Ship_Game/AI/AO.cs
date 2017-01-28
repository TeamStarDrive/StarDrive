using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{	
    // ReSharper disable once InconsistentNaming
	public sealed class AO : IDisposable
	{
        [XmlIgnore][JsonIgnore] private Planet CoreWorld;
        [XmlIgnore][JsonIgnore] private Array<Ship> OffensiveForcePool = new Array<Ship>();
        [XmlIgnore][JsonIgnore] private Fleet CoreFleet = new Fleet();
        [XmlIgnore][JsonIgnore] private readonly Array<Ship> ShipsWaitingForCoreFleet = new Array<Ship>();
        [XmlIgnore] [JsonIgnore] private Planet[] PlanetsInAo;
        [XmlIgnore] [JsonIgnore] private Planet[] OurPlanetsInAo;
        [XmlIgnore][JsonIgnore] public Vector2 Position => CoreWorld.Position;
        [XmlIgnore] [JsonIgnore] private Empire Owner => CoreWorld.Owner;

        [Serialize(0)] public int ThreatLevel;
        [Serialize(1)] public Guid CoreWorldGuid;
        [Serialize(2)] public Array<Guid> OffensiveForceGuids = new Array<Guid>();
        [Serialize(3)] public Array<Guid> ShipsWaitingGuids = new Array<Guid>();
        [Serialize(4)] public Guid FleetGuid;
        [Serialize(5)] public int WhichFleet = -1;
        //[Serialize(6)] private bool Flip; // @todo Change savegame version before reassigning Serialize indices
        [Serialize(7)] public float Radius;
        [Serialize(8)] public int TurnsToRelax;
        

	    public AO()
		{
		}

	    private bool CoreFleetFull()
	    {
	        return ThreatLevel < CoreFleet.GetStrength();            
	    }

	    private void CoreFleetAddShip(Ship ship)
	    {
            ship.GetAI().OrderQueue.Clear();
            ship.GetAI().HasPriorityOrder = false;
            CoreFleet.AddShip(ship);
        }
		public AO(Planet p, float radius)
		{
            Radius = radius;
            CoreWorld = p;
            CoreWorldGuid = p.guid;
            WhichFleet = p.Owner.GetUnusedKeyForFleet();
			p.Owner.GetFleetsDict().TryAdd(WhichFleet, CoreFleet);
            CoreFleet.Name = "Core Fleet";
            CoreFleet.Position = p.Position;
            CoreFleet.Owner = p.Owner;
            CoreFleet.IsCoreFleet = true;
            Array<Planet> tempPlanet = new Array<Planet>();
			foreach (Planet planet in Empire.Universe.PlanetsDict.Values)
			{
				if (!planet.Position.InRadius(CoreWorld.Position,radius)) continue;
    
                tempPlanet.Add(planet);
			}
            PlanetsInAo = tempPlanet.ToArray();
		}

		public bool AddShip(Ship ship)
		{
            if (ship.BaseStrength <1)
                return false;
            //@check for arbitraty comarison threatlevel
            if (CoreFleetFull() || ship.BombBays.Count > 0 || ship.hasAssaultTransporter || ship.HasTroopBay)
            {
                OffensiveForcePool.Add(ship);

            }
            
            //@corefleet speed less than 4k arbitrary logic means a likely problem 
		    if (CoreFleet.Task == null && ship.fleet == null && CoreFleet.speed < 4000 && !CoreFleetFull())
		    {
                CoreFleetAddShip(ship);
		        float strength = CoreFleet.GetStrength();
		        foreach (Ship waiting in ShipsWaitingForCoreFleet)
		        {
		            if (waiting.fleet != null)
		            {
		                continue;
		            }
		            strength += waiting.GetStrength();
		            if (ThreatLevel < strength) break;
		            CoreFleet.AddShip(waiting);
		            waiting.GetAI().OrderQueue.Clear();
		            waiting.GetAI().HasPriorityOrder = false;
		        }
		        CoreFleet.Position = CoreWorld.Position;
		        CoreFleet.AutoArrange();
		        CoreFleet.MoveToNow(Position, 0f, new Vector2(0f, -1f));
		        ShipsWaitingForCoreFleet.Clear();

		    }
		    else if (ship.fleet == null)
		    {
		        ShipsWaitingForCoreFleet.Add(ship);
		        OffensiveForcePool.Add(ship);
                return true;
		    }
            return false;
		}
        public bool RemoveShip(Ship ship)
        {
            if (ship.fleet?.IsCoreFleet ?? false)
                CoreFleet.RemoveShip(ship);
            ShipsWaitingForCoreFleet.Remove(ship);            
            return OffensiveForcePool.Remove(ship);
        }
        public Fleet GetCoreFleet() => CoreFleet;
		
		public Ship[] GetOffensiveForcePool() => OffensiveForcePool.ToArray();
		
		public Planet GetPlanet() => CoreWorld;

        public Planet[] GetPlanets() => OurPlanetsInAo;        

        public Ship[] GetWaitingShips() => ShipsWaitingForCoreFleet.ToArray();
		
        public void InitFromSave(UniverseData data, Empire owner)
        {
            foreach (SolarSystem sys in data.SolarSystemsList)
            {
                foreach (Planet p in sys.PlanetList)
                    if (p.guid == CoreWorldGuid)
                        SetPlanet(p);
            }
            Array<Planet> tempPlanet = new Array<Planet>();
            foreach (SolarSystem sys in data.SolarSystemsList)
            {
                foreach (Planet p in sys.PlanetList)
                    if (p.Position.InRadius(Position, Radius))
                        tempPlanet.Add(p);
            }
            PlanetsInAo = tempPlanet.ToArray();            
            foreach (Guid guid in OffensiveForceGuids)
            {
                foreach (Ship ship in data.MasterShipList)
                {
                    if (ship.guid != guid) continue;
                    OffensiveForcePool.Add(ship);
                    ship.AddedOnLoad = true;
                }
            }
            foreach (Guid guid in ShipsWaitingGuids)
            {
                foreach (Ship ship in data.MasterShipList)
                {
                    if (ship.guid != guid) continue;
                    ShipsWaitingForCoreFleet.Add(ship);
                    ship.AddedOnLoad = true;
                }
            }
            foreach (var kv in owner.GetFleetsDict())
            {
                if (kv.Value.guid == FleetGuid)
                    SetFleet(kv.Value);
            }
        }

		public void PrepareForSave()
		{
            OffensiveForceGuids.Clear();
            ShipsWaitingGuids.Clear();
			foreach (Ship ship in OffensiveForcePool)
			{
                OffensiveForceGuids.Add(ship.guid);
			}
			foreach (Ship ship in ShipsWaitingForCoreFleet)
			{
                ShipsWaitingGuids.Add(ship.guid);
			}
            FleetGuid = CoreFleet.guid;
		}

		public void SetFleet(Fleet f)
		{
            CoreFleet = f;
		}

		public void SetPlanet(Planet p)
		{
            CoreWorld = p;
		}

		public void Update()
		{
            Array<Planet> tempP = new Array<Planet>();
            foreach (Planet p in PlanetsInAo)
            {
                if (p.Owner != Owner) continue;
                tempP.Add(p);
            }
            OurPlanetsInAo = tempP.ToArray();
            for (int index = 0; index < OffensiveForcePool.Count; index++)
            {
                Ship ship = OffensiveForcePool[index];
                if (ship.Active && ship.fleet == null && ship.shipData.Role != ShipData.RoleName.troop &&
                    ship.GetStrength() > 0)
                    continue;

                OffensiveForcePool.Remove(ship);
            }
		    
            if (CoreFleet.speed > 4000) //@again arbitrary core fleet speed
                return;
            if (ShipsWaitingForCoreFleet.Count >0 && !CoreFleetFull()
                && (CoreFleet.Ships.Count ==0 || CoreFleet.Task == null))
			{
			    for (int index = 0; index < ShipsWaitingForCoreFleet.Count; index++)
			    {
			        Ship waiting = ShipsWaitingForCoreFleet[index];
			        if (waiting.fleet == null)
			        {
			            CoreFleetAddShip(waiting);
			        }
			        OffensiveForcePool.Remove(waiting);
			    }
			    ShipsWaitingForCoreFleet.Clear();
                CoreFleet.Position = CoreWorld.Position;
                CoreFleet.AutoArrange();
                CoreFleet.MoveToNow(Position, 0f, new Vector2(0f, -1f));
			}
			if (CoreFleet.Task == null)
			{
				TurnsToRelax += TurnsToRelax + 1;
			}
			if (ThreatLevel * ( 1-(TurnsToRelax / 10)) < CoreFleet.GetStrength())
			{
				if (CoreFleet.Task == null && !CoreWorld.Owner.isPlayer)
				{
					var clearArea = new MilitaryTask(this);
                    CoreFleet.Task = clearArea;
                    CoreFleet.TaskStep = 1;
				    if (CoreFleet.Owner == null)
				    {
				        CoreFleet.Owner = CoreWorld.Owner;
				        CoreFleet.Owner.GetGSAI().TaskList.Add(clearArea);
				    }
				    else CoreFleet.Owner.GetGSAI().TaskList.Add(clearArea);
				}
                TurnsToRelax = 1;
			}
		}        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AO() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            OffensiveForcePool = null;
            CoreFleet?.Dispose(ref CoreFleet);
        }
       
	}
}