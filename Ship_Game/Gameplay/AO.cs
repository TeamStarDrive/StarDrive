using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.Gameplay
{
	public sealed class AO : IDisposable
	{
		public int ThreatLevel;

		private Planet CoreWorld;

		public Guid CoreWorldGuid;

		public List<Guid> OffensiveForceGuids = new List<Guid>();

		public List<Guid> ShipsWaitingGuids = new List<Guid>();

		public Guid fleetGuid;

		private BatchRemovalCollection<Ship> OffensiveForcePool = new BatchRemovalCollection<Ship>();

		private BatchRemovalCollection<Ship> DefensiveForcePool = new BatchRemovalCollection<Ship>();

		private Fleet CoreFleet = new Fleet();

		private List<Ship> ShipsWaitingForCoreFleet = new List<Ship>();

		public int WhichFleet = -1;

		private bool Flip;

		public float Radius;

		private List<Planet> PlanetsInAO = new List<Planet>();

		public int TurnsToRelax;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		public Vector2 Position
		{
			get
			{
				return this.CoreWorld.Position;
			}
		}

		public AO()
		{
		}

		public AO(Planet p, float radius)
		{
			this.Radius = radius;
			this.CoreWorld = p;
			this.CoreWorldGuid = p.guid;
			this.WhichFleet = p.Owner.GetUnusedKeyForFleet();
			p.Owner.GetFleetsDict().TryAdd(this.WhichFleet, this.CoreFleet);
			this.CoreFleet.Name = "Core Fleet";
			this.CoreFleet.Position = p.Position;
			this.CoreFleet.Owner = p.Owner;
			this.CoreFleet.IsCoreFleet = true;
			foreach (Planet planet in p.Owner.GetPlanets())
			{
				if (Vector2.Distance(planet.Position, this.CoreWorld.Position) >= radius)
				{
					continue;
				}
				this.PlanetsInAO.Add(planet);
			}
		}

		public void AddShip(Ship ship)
		{
            if (ship.BaseStrength == 0)
                return;

            

            if (this.ThreatLevel <=
                this.CoreFleet.GetStrength() || ship.BombBays.Count >0 || ship.hasAssaultTransporter || ship.HasTroopBay)
			{
				this.OffensiveForcePool.Add(ship);
				this.Flip = !this.Flip;
				return;
			}
			if (this.CoreFleet.Task == null && ship.fleet == null && this.CoreFleet.speed <4000)
			{
              
                ship.GetAI().OrderQueue.Clear();
				ship.GetAI().HasPriorityOrder = false;
				this.CoreFleet.AddShip(ship);
                foreach (Ship waiting in this.ShipsWaitingForCoreFleet)
                {
                    if (waiting.fleet != null || this.ThreatLevel < this.CoreFleet.GetStrength())
                    {
                        continue;
                    }
                    
                    this.CoreFleet.AddShip(waiting);
                    
                    waiting.GetAI().OrderQueue.Clear();                    
                    waiting.GetAI().HasPriorityOrder = false;
                }
				this.CoreFleet.Position = this.CoreWorld.Position;
				this.CoreFleet.AutoArrange();
				this.CoreFleet.MoveToNow(this.Position, 0f, new Vector2(0f, -1f));
				this.ShipsWaitingForCoreFleet.Clear();
                   
			}
			else if (ship.fleet == null)
			{
				this.ShipsWaitingForCoreFleet.Add(ship);
				this.OffensiveForcePool.Add(ship);
			}
			this.Flip = !this.Flip;
		}

		public Fleet GetCoreFleet()
		{
			return this.CoreFleet;
		}

		public BatchRemovalCollection<Ship> GetOffensiveForcePool()
		{
			return this.OffensiveForcePool;
		}

		public Planet GetPlanet()
		{
			return this.CoreWorld;
		}

		public List<Planet> GetPlanets()
		{
			return this.PlanetsInAO;
		}

		public List<Ship> GetWaitingShips()
		{
			return this.ShipsWaitingForCoreFleet;
		}

		public void PrepareForSave()
		{
			this.OffensiveForceGuids.Clear();
			this.ShipsWaitingGuids.Clear();
			foreach (Ship ship in this.OffensiveForcePool)
			{
				this.OffensiveForceGuids.Add(ship.guid);
			}
			foreach (Ship ship in this.ShipsWaitingForCoreFleet)
			{
				this.ShipsWaitingGuids.Add(ship.guid);
			}
			this.fleetGuid = this.CoreFleet.guid;
		}

		public void SetFleet(Fleet f)
		{
			this.CoreFleet = f;
		}

		public void SetPlanet(Planet p)
		{
			this.CoreWorld = p;
		}

		public void Update()
		{
			
            foreach (Ship ship in this.OffensiveForcePool)
			{
                if (ship.Active && ship.fleet == null && ship.shipData.Role != ShipData.RoleName.troop && ship.GetStrength() >0)
				{
					continue;
				}
                //this.OffensiveForcePool.Remove(ship);
                this.OffensiveForcePool.QueuePendingRemoval(ship);
			}
			this.OffensiveForcePool.ApplyPendingRemovals();
            if (this.CoreFleet.speed > 4000)
                return;
            if (this.ShipsWaitingForCoreFleet.Count > 0 && this.CoreFleet.GetStrength() < this.ThreatLevel  
                && (this.CoreFleet.Ships.Count == 0 || this.CoreFleet.Task == null))
			{
				foreach (Ship waiting in this.ShipsWaitingForCoreFleet)
				{
					if (waiting.fleet == null)
					{
						this.CoreFleet.AddShip(waiting);
                           
						waiting.GetAI().OrderQueue.Clear();
                        
						waiting.GetAI().HasPriorityOrder = false;
					}
					this.OffensiveForcePool.Remove(waiting);
				}
				this.ShipsWaitingForCoreFleet.Clear();
				this.CoreFleet.Position = this.CoreWorld.Position;
				this.CoreFleet.AutoArrange();
				this.CoreFleet.MoveToNow(this.Position, 0f, new Vector2(0f, -1f));
			}
			if (this.CoreFleet.Task == null)
			{
				AO turnsToRelax = this;
				turnsToRelax.TurnsToRelax = turnsToRelax.TurnsToRelax + 1;
			}
			if (this.ThreatLevel  * ( 1-( this.TurnsToRelax /10)) < this.CoreFleet.GetStrength())
			{
				if (this.CoreFleet.Task == null && !this.CoreWorld.Owner.isPlayer)
				{
					MilitaryTask clearArea = new MilitaryTask(this.CoreFleet.Owner)
					{
						AO = this.Position,
						AORadius = this.Radius,
						type = MilitaryTask.TaskType.CohesiveClearAreaOfEnemies,
						WhichFleet = this.WhichFleet,
						IsCoreFleetTask = true
					};
					this.CoreFleet.Task = clearArea;
					this.CoreFleet.TaskStep = 1;
					if (this.CoreFleet.Owner == null)
					{
						this.CoreFleet.Owner = this.CoreWorld.Owner;
						//lock (GlobalStats.TaskLocker)
						{
							this.CoreFleet.Owner.GetGSAI().TaskList.Add(clearArea);
						}
					}
					else
					{
						//lock (GlobalStats.TaskLocker)
						{
							this.CoreFleet.Owner.GetGSAI().TaskList.Add(clearArea);
						}
					}
				}
				this.TurnsToRelax = 1;
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
                    if (this.OffensiveForcePool != null)
                        this.OffensiveForcePool.Dispose();
                    if (this.DefensiveForcePool != null)
                        this.DefensiveForcePool.Dispose();
                    if (this.CoreFleet != null)
                        this.CoreFleet.Dispose();
                }
                this.OffensiveForcePool = null;
                this.DefensiveForcePool = null;
                this.CoreFleet = null;
                this.disposed = true;
            }
        }
	}
}