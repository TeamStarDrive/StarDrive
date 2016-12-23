using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.Commands.MilitaryTasks;


namespace Ship_Game.Gameplay
{
	public sealed class AO : IDisposable
	{
        [XmlIgnore][JsonIgnore] private Planet CoreWorld;
        [XmlIgnore][JsonIgnore] private BatchRemovalCollection<Ship> OffensiveForcePool = new BatchRemovalCollection<Ship>();
        [XmlIgnore][JsonIgnore] private BatchRemovalCollection<Ship> DefensiveForcePool = new BatchRemovalCollection<Ship>();
        [XmlIgnore][JsonIgnore] private Fleet CoreFleet = new Fleet();
        [XmlIgnore][JsonIgnore] private readonly List<Ship> ShipsWaitingForCoreFleet = new List<Ship>();
        [XmlIgnore][JsonIgnore] private List<Planet> PlanetsInAO = new List<Planet>();
        [XmlIgnore][JsonIgnore] private bool disposed;
        [XmlIgnore][JsonIgnore] public Vector2 Position => CoreWorld.Position;

        [Serialize(0)] public int ThreatLevel;
        [Serialize(1)] public Guid CoreWorldGuid;
        [Serialize(2)] public List<Guid> OffensiveForceGuids = new List<Guid>();
        [Serialize(3)] public List<Guid> ShipsWaitingGuids = new List<Guid>();
        [Serialize(4)] public Guid fleetGuid;
        [Serialize(5)] public int WhichFleet = -1;
        [Serialize(6)] private bool Flip;
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
			foreach (Planet planet in p.Owner.GetPlanets())
			{
				if (!planet.Position.InRadius(CoreWorld.Position,radius))
				{
					continue;
				}
                PlanetsInAO.Add(planet);
			}
		}

		public void AddShip(Ship ship)
		{
            if (ship.BaseStrength <1)
                return;
            //@check for arbitraty comarison threatlevel
            if (CoreFleetFull() || ship.BombBays.Count >0 || ship.hasAssaultTransporter || ship.HasTroopBay)
			{
                OffensiveForcePool.Add(ship);
                Flip = !Flip;
				return;
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
		    }
		    Flip = !Flip;
		}

		public Fleet GetCoreFleet()
		{
			return CoreFleet;
		}

		public BatchRemovalCollection<Ship> GetOffensiveForcePool()
		{
			return OffensiveForcePool;
		}

		public Planet GetPlanet()
		{
			return CoreWorld;
		}

		public List<Planet> GetPlanets()
		{
			return PlanetsInAO;
		}

		public List<Ship> GetWaitingShips()
		{
			return ShipsWaitingForCoreFleet;
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
            fleetGuid = CoreFleet.guid;
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
			
            foreach (Ship ship in OffensiveForcePool)
			{
                if (ship.Active && ship.fleet == null && ship.shipData.Role != ShipData.RoleName.troop && ship.GetStrength() >0)
				{
					continue;
				}                
                OffensiveForcePool.QueuePendingRemoval(ship);
			}
            OffensiveForcePool.ApplyPendingRemovals();
            if (CoreFleet.speed > 4000) //@again arbitrary core fleet speed
                return;
            if (ShipsWaitingForCoreFleet.Any() && !CoreFleetFull()
                && (!CoreFleet.Ships.Any() || CoreFleet.Task == null))
			{
				foreach (Ship waiting in ShipsWaitingForCoreFleet)
				{
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
				AO turnsToRelax = this;
				turnsToRelax.TurnsToRelax = turnsToRelax.TurnsToRelax + 1;
			}
			if (ThreatLevel * ( 1-(TurnsToRelax / 10)) < CoreFleet.GetStrength())
			{
				if (CoreFleet.Task == null && !CoreWorld.Owner.isPlayer)
				{
					var clearArea = new CohesiveClearAreaOfEnemies(this);
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
            if (!disposed)
            {
                if (disposing)
                {
                    OffensiveForcePool?.Dispose();
                    DefensiveForcePool?.Dispose();
                    CoreFleet?.Dispose();
                }
                OffensiveForcePool = null;
                DefensiveForcePool = null;
                CoreFleet = null;
                disposed = true;
            }
        }
       
	}
}