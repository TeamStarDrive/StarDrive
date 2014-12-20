using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ship_Game.Gameplay
{
	public class MilitaryTask
	{
		public bool IsCoreFleetTask;

		public bool WaitForCommand;

        public List<Guid> HeldGoals = new List<Guid>();

		public int Step;

		public Guid TargetPlanetGuid = Guid.Empty;

		public MilitaryTask.TaskType type;

		public Vector2 AO;

		public float AORadius;

		public float InitialEnemyStrength;

		public float EnemyStrength;

		public float StartingStrength;

		public float MinimumTaskForceStrength;

		private Planet TargetPlanet;

		public float TaskTimer;

		private Empire empire;

		public bool IsToughNut;

		public int NeededTroopStrength;

		private BatchRemovalCollection<Ship> TaskForce = new BatchRemovalCollection<Ship>();

		public int WhichFleet = -1;

		public MilitaryTask()
		{
		}

		public MilitaryTask(Vector2 location, float radius, List<Goal> GoalsToHold, Empire Owner)
		{
			this.type = MilitaryTask.TaskType.ClearAreaOfEnemies;
			this.AO = location;
			this.AORadius = radius;
			foreach (Goal g in GoalsToHold)
			{
				g.Held = true;
				this.HeldGoals.Add(g.guid);
			}
			foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in Owner.GetGSAI().ThreatMatrix.Pins)
			{
				if (Vector2.Distance(this.AO, pin.Value.Position) >= this.AORadius || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == Owner)
				{
					continue;
				}
				MilitaryTask initialEnemyStrength = this;
				initialEnemyStrength.InitialEnemyStrength = initialEnemyStrength.InitialEnemyStrength + pin.Value.Strength;
				MilitaryTask enemyStrength = this;
				enemyStrength.EnemyStrength = enemyStrength.EnemyStrength + pin.Value.Strength;
			}
			this.MinimumTaskForceStrength = this.EnemyStrength + 0.35f * this.EnemyStrength;
			this.empire = Owner;
		}

		public MilitaryTask(Planet target, Empire Owner)
		{
			this.type = MilitaryTask.TaskType.AssaultPlanet;
			this.TargetPlanet = target;
			this.TargetPlanetGuid = target.guid;
			this.AO = target.Position;
			this.AORadius = 35000f;
			this.empire = Owner;
		}

		public MilitaryTask(Empire Owner)
		{
			this.empire = Owner;
		}

		private void DoToughNutRequisitionORIG()
		{
			float EnemyTroopStr = this.GetEnemyTroopStr();
			float EnemyShipStr = this.GetEnemyStrAtTarget();
			IOrderedEnumerable<Ship_Game.Gameplay.AO> sorted = 
				from ao in this.empire.GetGSAI().AreasOfOperations
				orderby Vector2.Distance(this.AO, ao.Position)
				select ao;
			if (sorted.Count<Ship_Game.Gameplay.AO>() == 0)
			{
				return;
			}
			List<Ship> Bombers = new List<Ship>();
			List<Ship> EverythingElse = new List<Ship>();
			List<Troop> Troops = new List<Troop>();
			foreach (Ship_Game.Gameplay.AO area in sorted)
			{
				foreach (Ship ship in this.empire.GetShips())
				{
					if (ship.GetStrength() == 0f 
                        || Vector2.Distance(ship.Center, area.Position) >= area.Radius 
                        || ship.InCombat 
                        || ship.fleet != null ) //&& ship.fleet != null & ship.fleet.Task == null)
					{
						continue;
					}
					if (ship.BombBays.Count <= 0)
					{
						EverythingElse.Add(ship);
					}
					else
					{
						Bombers.Add(ship);
					}
				}
				foreach (Planet p in area.GetPlanets())
				{
					if (p.RecentCombat)
					{
						continue;
					}
					foreach (Troop t in p.TroopsHere)
					{
						if (t.GetOwner() != this.empire)
						{
							continue;
						}
						Troops.Add(t);
					}
				}
			}
			List<Ship> TaskForce = new List<Ship>();
			float strAdded = 0f;
			List<Ship>.Enumerator enumerator = EverythingElse.GetEnumerator();
			try
			{
				do
				{
					if (!enumerator.MoveNext())
					{
						break;
					}
					Ship ship = enumerator.Current;
					TaskForce.Add(ship);
					strAdded = strAdded + ship.GetStrength();
				}
				while (strAdded <= EnemyShipStr * 1.65f);
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			List<Ship> BombTaskForce = new List<Ship>();
			int numBombs = 0;
			foreach (Ship ship in Bombers)
			{
				if (numBombs >= 20)
				{
					continue;
				}
				BombTaskForce.Add(ship);
				numBombs = numBombs + ship.BombBays.Count;
			}
			List<Troop> PotentialTroops = new List<Troop>();
			float troopStr = 0f;
			List<Troop>.Enumerator enumerator1 = Troops.GetEnumerator();
            int numOfTroops=0;
			try
			{
				do
				{
					if (!enumerator1.MoveNext())
					{
						break;
					}
                    numOfTroops++;
                    Troop t = enumerator1.Current;
					PotentialTroops.Add(t);
					troopStr = troopStr + (float)t.Strength;
				}
				while (troopStr <= EnemyTroopStr * 1.25f || numOfTroops <15 );
			}
			finally
			{
				((IDisposable)enumerator1).Dispose();
			}
			if (strAdded > EnemyShipStr * 1.65f)
			{
				if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.empire.GetRelations().ContainsKey(this.TargetPlanet.Owner))
				{
					this.EndTask();
					return;
				}
				if (this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWar)
				{
					this.empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWarType);
				}
				Ship_Game.Gameplay.AO ClosestAO = sorted.First<Ship_Game.Gameplay.AO>();
				MilitaryTask assault = new MilitaryTask(this.empire)
				{
					AO = this.TargetPlanet.Position,
					AORadius = 75000f,
					type = MilitaryTask.TaskType.AssaultPlanet
				};
				ClosestAO.GetCoreFleet().Owner.GetGSAI().TasksToAdd.Add(assault);
				assault.WhichFleet = ClosestAO.WhichFleet;
				ClosestAO.GetCoreFleet().Task = assault;
				assault.IsCoreFleetTask = true;
				assault.Step = 1;
				assault.TargetPlanet = this.TargetPlanet;
				ClosestAO.GetCoreFleet().TaskStep = 0;
				ClosestAO.GetCoreFleet().Name = "Doom Fleet";
				foreach (Ship ship in TaskForce)
				{
					if (ship.fleet != null)
					{
						ship.fleet.Ships.Remove(ship);
					}
					ship.GetAI().OrderQueue.Clear();
					foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.empire.GetGSAI().DefensiveCoordinator.DefenseDict)
					{
						List<Ship> toRemove = new List<Ship>();
						foreach (KeyValuePair<Guid, Ship> defender in entry.Value.ShipsDict)
						{
							if (defender.Key != ship.guid)
							{
								continue;
							}
							toRemove.Add(defender.Value);
						}
						foreach (Ship s in toRemove)
						{
							entry.Value.ShipsDict.Remove(s.guid);
						}
					}
					ship.fleet = null;
				}
				foreach (Ship ship in TaskForce)
				{
					ClosestAO.GetCoreFleet().AddShip(ship);
				}
				foreach (Troop t in PotentialTroops)
				{
					if (t.GetPlanet() == null)
					{
						continue;
					}
					(new List<Troop>()).Add(t);
					Ship launched = t.Launch();
					ClosestAO.GetCoreFleet().AddShip(launched);
				}
				ClosestAO.GetCoreFleet().AutoArrange();
				if (Bombers.Count > 0 && numBombs > 6)
				{
					MilitaryTask GlassPlanet = new MilitaryTask(this.empire)
					{
						AO = this.TargetPlanet.Position,
						AORadius = 75000f,
						type = MilitaryTask.TaskType.GlassPlanet,
						TargetPlanet = this.TargetPlanet,
						WaitForCommand = true
					};
					Fleet bomberFleet = new Fleet()
					{
						Owner = this.empire
					};
					bomberFleet.Owner.GetGSAI().TasksToAdd.Add(GlassPlanet);
					GlassPlanet.WhichFleet = this.empire.GetUnusedKeyForFleet();
					this.empire.GetFleetsDict().Add(GlassPlanet.WhichFleet, bomberFleet);
					bomberFleet.Task = GlassPlanet;
					bomberFleet.Name = "Bomber Fleet";
					foreach (Ship ship in BombTaskForce)
					{
						if (ship.fleet != null)
						{
							ship.fleet.Ships.Remove(ship);
						}
						ship.GetAI().OrderQueue.Clear();
						foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.empire.GetGSAI().DefensiveCoordinator.DefenseDict)
						{
							List<Ship> toRemove = new List<Ship>();
							foreach (KeyValuePair<Guid, Ship> defender in entry.Value.ShipsDict)
							{
								if (defender.Key != ship.guid)
								{
									continue;
								}
								toRemove.Add(defender.Value);
							}
							foreach (Ship s in toRemove)
							{
								entry.Value.ShipsDict.Remove(s.guid);
							}
						}
						ship.fleet = null;
					}
					foreach (Ship ship in BombTaskForce)
					{
						bomberFleet.AddShip(ship);
					}
					bomberFleet.AutoArrange();
				}
				this.Step = 1;
				this.empire.GetGSAI().TaskList.QueuePendingRemoval(this);
			}
		}

        private void DoToughNutRequisition()
        {
            float EnemyTroopStr = this.GetEnemyTroopStr();
            float EnemyShipStr = this.GetEnemyStrAtTarget();
            IOrderedEnumerable<AO> sorted =
                from ao in this.empire.GetGSAI().AreasOfOperations
                //orderby ao.GetOffensiveForcePool().Sum(bombs => bombs.BombBays.Count) > 0 descending
                orderby ao.GetOffensiveForcePool().Where(combat=> !combat.InCombat).Sum(strength => strength.BaseStrength) >= this.MinimumTaskForceStrength descending
                orderby Vector2.Distance(this.AO, ao.Position)
                select ao;
            if (sorted.Count<AO>() == 0)
            {
                return;
            }
            List<Ship> Bombers = new List<Ship>();
            List<Ship> EverythingElse = new List<Ship>();
            List<Troop> Troops = new List<Troop>();
            foreach (AO area in sorted)
            {
                foreach (Ship ship in this.empire.GetShips().OrderBy(str=> str.BaseStrength))
                {
                    if ((ship.Role == "station" || ship.Role == "platform") 
                        || ship.BaseStrength == 0f 
                        || Vector2.Distance(ship.Center, area.Position) >= area.Radius 
                        || ship.InCombat
                        || ship.fleet != null )//&& ship.fleet.Task == null) //&& ship.fleet != null && ship.fleet.Task == null)
                    {
                        continue;
                    }
                    if (ship.BombBays.Count <= 0)
                    {
                        EverythingElse.Add(ship);
                    }
                    else
                    {
                        Bombers.Add(ship);
                    }
                }
                foreach (Planet p in area.GetPlanets())
                {
                    if (p.RecentCombat || p.ParentSystem.combatTimer>0)
                    {
                        continue;
                    }
                    foreach (Troop t in p.TroopsHere)
                    {
                        if (t.GetOwner() != this.empire)
                        {
                            continue;
                        }
                        Troops.Add(t);
                    }
                }
            }
            List<Ship> TaskForce = new List<Ship>();
            float strAdded = 0f;
            List<Ship>.Enumerator enumerator = EverythingElse.GetEnumerator();
            try
            {
                do
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }
                    Ship ship = enumerator.Current;
                    TaskForce.Add(ship);
                    strAdded = strAdded + ship.GetStrength();
                }
                while (strAdded <= EnemyShipStr * 1.65f);
            }
            finally
            {
                ((IDisposable)enumerator).Dispose();
            }
            List<Ship> BombTaskForce = new List<Ship>();
            int numBombs = 0;
            foreach (Ship ship in Bombers)
            {
                if (numBombs >= 20)
                {
                    continue;
                }
                BombTaskForce.Add(ship);
                numBombs = numBombs + ship.BombBays.Count;
            }
            List<Troop> PotentialTroops = new List<Troop>();
            float troopStr = 0f;
            List<Troop>.Enumerator enumerator1 = Troops.GetEnumerator();
            int numOfTroops =0;
            try
            {
                do
                {
                    if (numOfTroops >15||!enumerator1.MoveNext() )
                    {
                        break;
                    }
                    numOfTroops++;
                    Troop t = enumerator1.Current;
                    PotentialTroops.Add(t);
                    troopStr = troopStr + (float)t.Strength;
                }
                while (troopStr <= EnemyTroopStr * 1.25f );
            }
            finally
            {
                ((IDisposable)enumerator1).Dispose();
            }
            if (strAdded > EnemyShipStr * 1.65f)
            {
                if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.empire.GetRelations().ContainsKey(this.TargetPlanet.Owner))
                {
                    this.EndTask();
                    return;
                }
                if (this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWar)
                {
                    this.empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWarType);
                }
                AO ClosestAO = sorted.First<AO>();
                MilitaryTask assault = new MilitaryTask(this.empire)
                {
                    AO = this.TargetPlanet.Position,
                    AORadius = 75000f,
                    type = MilitaryTask.TaskType.AssaultPlanet
                };
                ClosestAO.GetCoreFleet().Owner.GetGSAI().TasksToAdd.Add(assault);
                assault.WhichFleet = ClosestAO.WhichFleet;
                ClosestAO.GetCoreFleet().Task = assault;
                assault.IsCoreFleetTask = true;
                assault.Step = 1;
                
                assault.TargetPlanet = this.TargetPlanet;
                ClosestAO.GetCoreFleet().TaskStep = 0;
                ClosestAO.GetCoreFleet().Name = "Doom Fleet";
                foreach (Ship ship in TaskForce)
                {
                    if (ship.fleet != null)
                    {
                        ship.fleet.Ships.Remove(ship);
                    }
                    ship.GetAI().OrderQueue.Clear();
                    foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.empire.GetGSAI().DefensiveCoordinator.DefenseDict)
                    {
                        List<Ship> toRemove = new List<Ship>();
                        foreach (KeyValuePair<Guid, Ship> defender in entry.Value.ShipsDict)
                        {
                            if (defender.Key != ship.guid)
                            {
                                continue;
                            }
                            toRemove.Add(defender.Value);
                        }
                        foreach (Ship s in toRemove)
                        {
                            entry.Value.ShipsDict.Remove(s.guid);
                        }
                    }
                    ship.fleet = null;
                }
                foreach (Ship ship in TaskForce)
                {
                    ClosestAO.GetCoreFleet().AddShip(ship);
                }
                foreach (Troop t in PotentialTroops)
                {
                    if (t.GetPlanet() == null)
                    {
                        continue;
                    }
                    (new List<Troop>()).Add(t);
                    Ship launched = t.Launch();
                    ClosestAO.GetCoreFleet().AddShip(launched);
                }
                ClosestAO.GetCoreFleet().AutoArrange();
                if (Bombers.Count > 0 && numBombs > 6)
                {
                    MilitaryTask GlassPlanet = new MilitaryTask(this.empire)
                    {
                        AO = this.TargetPlanet.Position,
                        AORadius = 75000f,
                        type = MilitaryTask.TaskType.GlassPlanet,
                        TargetPlanet = this.TargetPlanet,
                        WaitForCommand = true
                    };
                    

                    Fleet bomberFleet = new Fleet()
                    {
                        Owner = this.empire
                    };
                    bomberFleet.Owner.GetGSAI().TasksToAdd.Add(GlassPlanet);
                    GlassPlanet.WhichFleet = this.empire.GetUnusedKeyForFleet();
                    this.empire.GetFleetsDict().Add(GlassPlanet.WhichFleet, bomberFleet);
                    bomberFleet.Task = GlassPlanet;
                    bomberFleet.Name = "Bomber Fleet";
                    foreach (Ship ship in BombTaskForce)
                    {
                        if (ship.fleet != null)
                        {
                            ship.fleet.Ships.Remove(ship);
                        }
                        ship.GetAI().OrderQueue.Clear();
                        foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.empire.GetGSAI().DefensiveCoordinator.DefenseDict)
                        {
                            List<Ship> toRemove = new List<Ship>();
                            foreach (KeyValuePair<Guid, Ship> defender in entry.Value.ShipsDict)
                            {
                                if (defender.Key != ship.guid)
                                {
                                    continue;
                                }
                                toRemove.Add(defender.Value);
                            }
                            foreach (Ship s in toRemove)
                            {
                                entry.Value.ShipsDict.Remove(s.guid);
                            }
                        }
                        ship.fleet = null;
                    }
                    foreach (Ship ship in BombTaskForce)
                    {
                        bomberFleet.AddShip(ship);
                    }
                    bomberFleet.AutoArrange();
                }
                this.Step = 1;
                this.empire.GetGSAI().TaskList.QueuePendingRemoval(this);
            }
        }

		public void EndTask()
		{
			//if(UniverseScreen.debug)
            {
                //if (this.type.ToString() != DebugInfoScreen.canceledMTaskName)
                //    DebugInfoScreen.canceledMtasksCount = 0;
                
                DebugInfoScreen.canceledMtasksCount++;
                switch (this.type)
                {
                    case TaskType.Exploration:
                        {
                            DebugInfoScreen.canceledMtask1Count++;
                            DebugInfoScreen.canceledMTask1Name = TaskType.Exploration.ToString();
                            
                            break;
                        }
                    case TaskType.AssaultPlanet:
                        {
                            DebugInfoScreen.canceledMtask2Count++;
                            DebugInfoScreen.canceledMTask2Name = TaskType.AssaultPlanet.ToString();
                            
                            break;
                        }
                    case TaskType.CohesiveClearAreaOfEnemies:
                        {
                            DebugInfoScreen.canceledMtask3Count++;
                            DebugInfoScreen.canceledMTask3Name = TaskType.CohesiveClearAreaOfEnemies.ToString();
                        
                        break;
                        }
                     default:
                        {
                            DebugInfoScreen.canceledMtask4Count++;
                            DebugInfoScreen.canceledMTask4Name = this.type.ToString();
                            break;
                        }

                }
            }
            if (this.empire.isFaction)
			{
				this.FactionEndTask();
				return;
			}
			foreach (Guid goalGuid in this.HeldGoals)
			{
				foreach (Goal g in this.empire.GetGSAI().Goals)
				{
					if (g.guid != goalGuid)
					{
						continue;
					}
					g.Held = false;
				}
			}
			IOrderedEnumerable<Ship_Game.Gameplay.AO> sorted = 
				from ao in this.empire.GetGSAI().AreasOfOperations
                orderby Vector2.Distance(this.AO, ao.Position) 
				select ao;
			if (sorted.Count<Ship_Game.Gameplay.AO>() == 0)
			{
                if (!this.IsCoreFleetTask && this.WhichFleet != -1 && this.empire != Ship.universeScreen.player)
                    foreach (Ship ship in this.empire.GetFleetsDict()[this.WhichFleet].Ships)
                    {
                        this.empire.ForcePoolAdd(ship);
                    }
				return;
			}
			Ship_Game.Gameplay.AO ClosestAO = sorted.First<Ship_Game.Gameplay.AO>();
			if (this.WhichFleet != -1)
			{
				if (this.IsCoreFleetTask)
				{
					ClosestAO.GetCoreFleet().Task = null;
					ClosestAO.GetCoreFleet().MoveToDirectly(ClosestAO.Position, 0f, new Vector2(0f, -1f));
					ClosestAO.TurnsToRelax = 0;
				}
				else
				{
					if (!this.empire.GetFleetsDict().ContainsKey(this.WhichFleet))
					{
                        if (!this.IsCoreFleetTask && this.WhichFleet != -1 && this.empire != Ship.universeScreen.player)
                            foreach (Ship ship in this.empire.GetFleetsDict()[this.WhichFleet].Ships)
                            {
                                this.empire.ForcePoolAdd(ship);
                            }
                        return;
					}
					foreach (Ship ship in this.empire.GetFleetsDict()[this.WhichFleet].Ships)
					{
						ship.GetAI().OrderQueue.Clear();
						ship.GetAI().State = AIState.AwaitingOrders;
						ship.fleet = null;
						ship.HyperspaceReturn();
						ship.isSpooling = false;
						if (ship.Role != "troop")
						{
							ClosestAO.GetOffensiveForcePool().Add(ship);
							ship.GetAI().OrderResupplyNearest();
						}
						else
						{
							ship.GetAI().OrderRebaseToNearest();
						}
					}
					this.TaskForce.Clear();
					this.empire.GetGSAI().UsedFleets.Remove(this.WhichFleet);
					this.empire.GetFleetsDict()[this.WhichFleet].Reset();
				}
				if (this.type == MilitaryTask.TaskType.Exploration)
				{
					List<Troop> toLaunch = new List<Troop>();
					foreach (Troop t in this.TargetPlanet.TroopsHere)
					{
						if (t.GetOwner() != this.empire ||this.TargetPlanet.CombatTimer >0 ||t.AvailableAttackActions==0 ||t.MoveTimer>0) 
						{
							continue;
						}
						toLaunch.Add(t);
					}
					foreach (Troop t in toLaunch)
					{
						Ship troopship = t.Launch();
						if (troopship == null)
						{
							continue;
						}
						troopship.GetAI().OrderRebaseToNearest();
					}
					toLaunch.Clear();
				}
			}
			this.empire.GetGSAI().TaskList.QueuePendingRemoval(this);
		}

		public void EndTaskWithMove()
		{
			foreach (Guid goalGuid in this.HeldGoals)
			{
				foreach (Goal g in this.empire.GetGSAI().Goals)
				{
					if (g.guid != goalGuid)
					{
						continue;
					}
					g.Held = false;
				}
			}
			IOrderedEnumerable<Ship_Game.Gameplay.AO> sorted = 
				from ao in this.empire.GetGSAI().AreasOfOperations
				orderby Vector2.Distance(this.AO, ao.Position)
				select ao;
			if (sorted.Count<Ship_Game.Gameplay.AO>() == 0)
			{
				if(!this.IsCoreFleetTask && this.WhichFleet !=-1 && this.empire != Ship.universeScreen.player)
                    foreach (Ship ship in this.empire.GetFleetsDict()[this.WhichFleet].Ships)
                    {
                        this.empire.ForcePoolAdd(ship);
                    }
                return;
			}
			Ship_Game.Gameplay.AO ClosestAO = sorted.First<Ship_Game.Gameplay.AO>();
			if (this.WhichFleet != -1)
			{
				if (this.IsCoreFleetTask)
				{
					this.empire.GetFleetsDict()[this.WhichFleet].Task = null;
					this.empire.GetFleetsDict()[this.WhichFleet].MoveToDirectly(ClosestAO.Position, 0f, new Vector2(0f, -1f));
				}
				else
				{
					foreach (Ship ship in this.empire.GetFleetsDict()[this.WhichFleet].Ships)
					{
						ship.fleet = null;
						ClosestAO.AddShip(ship);
						ClosestAO.TurnsToRelax = 0;
					}
					this.TaskForce.Clear();
					this.empire.GetGSAI().UsedFleets.Remove(this.WhichFleet);
					this.empire.GetFleetsDict()[this.WhichFleet].Reset();
				}
			}
			this.empire.GetGSAI().TaskList.QueuePendingRemoval(this);
		}

        public void Evaluate(Empire e)
        {
            this.empire = e;
            switch (this.type)
            {
                case MilitaryTask.TaskType.ClearAreaOfEnemies:
                    switch (this.Step)
                    {
                        case 0:
                            this.RequisitionForces();
                            return;
                        case 1:
                            this.ExecuteAndAssess();
                            return;
                        default:
                            return;
                    }
                case MilitaryTask.TaskType.AssaultPlanet:
                    switch (this.Step)
                    {
                        case 0:
                            this.RequisitionAssaultForces();
                            return;
                        case 1:
                            //if (this.GetTargetPlanet().GetGroundStrength(this.empire) > 0)
                            //    return;
                            //else 
                            if (this.empire.GetFleetsDict().ContainsKey(this.WhichFleet) )
                            {
                                if (this.empire.GetFleetsDict()[this.WhichFleet].Ships.Count != 0)
                                    return;
                                
                                this.EndTask();
                                return;
                            }
                            else
                            {
                                
                                this.EndTask();
                                return;
                            }
                        default:
                            return;
                    }
                case MilitaryTask.TaskType.CorsairRaid:
                    if (this.Step != 0)
                        break;
                    this.empire.GetFleetsDict()[1].Reset();
                    foreach (Ship shiptoadd in (List<Ship>)this.empire.GetShips())
                    {
                        if (shiptoadd.Role != "platform")
                            this.empire.GetFleetsDict()[1].AddShip(shiptoadd);
                    }
                    if (this.empire.GetFleetsDict()[1].Ships.Count <= 0)
                        break;
                    this.empire.GetFleetsDict()[1].Name = "Corsair Raiders";
                    this.empire.GetFleetsDict()[1].AutoArrange();
                    this.empire.GetFleetsDict()[1].Task = this;
                    this.WhichFleet = 1;
                    this.Step = 1;
                    this.empire.GetFleetsDict()[1].FormationWarpTo(this.TargetPlanet.Position, 0.0f, Vector2.Zero);
                    break;
                case MilitaryTask.TaskType.CohesiveClearAreaOfEnemies:
                    switch (this.Step)
                    {
                        case 0:
                            this.RequisitionForces();
                            return;
                        case 1:
                            this.ExecuteAndAssess();
                            return;
                        default:
                            return;
                    }
                case MilitaryTask.TaskType.Exploration:
                    if (this.Step != 0)
                        break;
                    this.RequisitionExplorationForce();
                    break;
                case MilitaryTask.TaskType.DefendSystem:
                    switch (this.Step)
                    {
                        case 0:
                            this.RequisitionDefenseForce();
                            return;
                        case 1:
                            if (this.empire.GetFleetsDict().ContainsKey(this.WhichFleet))
                            {
                                if (this.empire.GetFleetsDict()[this.WhichFleet].Ships.Count != 0)
                                    return;
                                this.EndTask();
                                return;
                            }
                            else
                            {
                                this.EndTask();
                                return;
                            }
                        default:
                            return;
                    }
                case MilitaryTask.TaskType.DefendClaim:
                    switch (this.Step)
                    {
                        case 0:
                            if (this.TargetPlanet.Owner != null)
                                this.EndTask();
                            this.RequisitionClaimForce();
                            return;
                        case 1:
                            if (this.empire.GetFleetsDict().ContainsKey(this.WhichFleet))
                            {
                                if (this.empire.GetFleetsDict()[this.WhichFleet].Ships.Count == 0)
                                    this.EndTask();
                            }
                            else
                                this.EndTask();
                            if (this.TargetPlanet.Owner == null)
                                return;
                            this.EndTask();
                            return;
                        default:
                            return;
                    }
            }
        }


		private void ExecuteAndAssess()
		{
			if (this.WhichFleet == -1)
			{
				this.Step = 0;
				return;
			}
            if( this.type == TaskType.Exploration ||this.type ==TaskType.AssaultPlanet)
            {
                float groundstrength = this.GetTargetPlanet().GetGroundStrengthOther(this.empire);
                float ourGroundStrength = this.GetTargetPlanet().GetGroundStrength(this.empire);
                //if (this.GetTargetPlanet().TroopsHere.Where(troop => troop.GetOwner() == this.empire).Count()>0)
                if (ourGroundStrength > 0)
                {
                    if(this.type==TaskType.Exploration)
                    {
                        Planet p = this.GetTargetPlanet();
                        if (p.BuildingList.Where(relic => relic.EventTriggerUID != "").Count() > 0)
                        {
                            return;
                        }
                    }
                    else if (this.type == TaskType.AssaultPlanet)
                    {
                        if (groundstrength > 0)
                        return;
                    }
                }
            }
			if (this.empire.GetFleetsDict()[this.WhichFleet].Task == null )
			{
				this.EndTask();
				return;
			}
            
			float currentStrength = 0f;
			foreach (Ship ship in this.empire.GetFleetsDict()[this.WhichFleet].Ships)
			{
				if (!ship.Active)
				{
					this.empire.GetFleetsDict()[this.WhichFleet].Ships.QueuePendingRemoval(ship);
				}
				else
				{
					currentStrength = currentStrength + ship.GetStrength();
				}
			}
			this.empire.GetFleetsDict()[this.WhichFleet].Ships.ApplyPendingRemovals();
			float currentEnemyStrength = 0f;
			foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.empire.GetGSAI().ThreatMatrix.Pins)
			{
				if (Vector2.Distance(this.AO, pin.Value.Position) >= this.AORadius)
				{
					continue;
				}
				Empire pinEmp = EmpireManager.GetEmpireByName(pin.Value.EmpireName);
				if (pinEmp == this.empire || !pinEmp.isFaction && !this.empire.GetRelations()[pinEmp].AtWar)
				{
					continue;
				}
				currentEnemyStrength = currentEnemyStrength + pin.Value.Strength;
			}
			if (currentStrength < 0.15f * this.StartingStrength && currentEnemyStrength > currentStrength)
			{
				this.EndTask();
				return;
			}
			if (currentEnemyStrength == 0f || currentStrength == 0f)
			{
				this.EndTask();
			}
		}

		public void FactionEndTask()
		{
			if (this.WhichFleet != -1)
			{
				if (!this.IsCoreFleetTask)
				{
					if (!this.empire.GetFleetsDict().ContainsKey(this.WhichFleet))
					{
						return;
					}
					foreach (Ship ship in this.empire.GetFleetsDict()[this.WhichFleet].Ships)
					{
						ship.GetAI().OrderQueue.Clear();
						ship.GetAI().State = AIState.AwaitingOrders;
						ship.fleet = null;
						ship.HyperspaceReturn();
						ship.isSpooling = false;
						if (ship.Role != "troop")
						{
							ship.GetAI().OrderResupplyNearest();
						}
						else
						{
							ship.GetAI().OrderRebaseToNearest();
						}
					}
					this.TaskForce.Clear();
					this.empire.GetGSAI().UsedFleets.Remove(this.WhichFleet);
					this.empire.GetFleetsDict()[this.WhichFleet].Reset();
				}
				if (this.type == MilitaryTask.TaskType.Exploration)
				{
					List<Troop> toLaunch = new List<Troop>();
					foreach (Troop t in this.TargetPlanet.TroopsHere)
					{
						if (t.GetOwner() != this.empire)
						{
							continue;
						}
						toLaunch.Add(t);
					}
					foreach (Troop t in toLaunch)
					{
						Ship troopship = t.Launch();
						if (troopship == null)
						{
							continue;
						}
						troopship.GetAI().OrderRebaseToNearest();
					}
					toLaunch.Clear();
				}
			}
			this.empire.GetGSAI().TaskList.QueuePendingRemoval(this);
		}

		private float GetEnemyStrAtTarget()
		{
			float MinimumEscortStrength = 0f;
			foreach (Ship ship in this.TargetPlanet.system.ShipList)
			{
				if (ship.loyalty != this.TargetPlanet.Owner)
				{
					continue;
				}
                MinimumEscortStrength = MinimumEscortStrength + ship.BaseStrength;// GetStrength();
			}
			return MinimumEscortStrength;
		}

		private float GetEnemyTroopStr()
		{
            return this.TargetPlanet.GetGroundStrengthOther(this.empire);
            /*
            float EnemyTroopStrength = 0f;
			foreach (PlanetGridSquare pgs in this.TargetPlanet.TilesList)
			{
				if (pgs.TroopsHere.Count <= 0)
				{
					if (pgs.building == null || pgs.building.CombatStrength <= 0)
					{
						continue;
					}
					EnemyTroopStrength = EnemyTroopStrength + (float)(pgs.building.CombatStrength + 5);
				}
				else
				{
					EnemyTroopStrength = EnemyTroopStrength + (float)pgs.TroopsHere[0].Strength;
				}
			}
			if (EnemyTroopStrength < 20f)
			{
				EnemyTroopStrength = 50f;
			}
			return EnemyTroopStrength;
             */ 
		}

		public Planet GetTargetPlanet()
		{
			return this.TargetPlanet;
		}

		private void RequisitionAssaultForces()
		{
			List<Troop>.Enumerator enumerator;
			if (this.IsToughNut)
			{
				this.DoToughNutRequisition();
				return;
			}
			IOrderedEnumerable<Ship_Game.Gameplay.AO> sorted = 
				from ao in this.empire.GetGSAI().AreasOfOperations
                orderby ao.GetOffensiveForcePool().Sum(bombs => bombs.BombBays.Count) > 0 descending
                orderby ao.GetOffensiveForcePool().Sum(strength => strength.GetStrength()) >= this.MinimumTaskForceStrength descending
				orderby Vector2.Distance(this.AO, ao.Position)
				select ao;
			if (sorted.Count<Ship_Game.Gameplay.AO>() == 0)
			{
				return;
			}
			Ship_Game.Gameplay.AO ClosestAO = sorted.First<Ship_Game.Gameplay.AO>();
			if (this.TargetPlanet.Owner == null || !this.empire.GetRelations().ContainsKey(this.TargetPlanet.Owner))
			{
				this.EndTask();
				return;
			}
			if (this.empire.GetRelations()[this.TargetPlanet.Owner].Treaty_Peace)
			{
				this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWar = false;
				this.EndTask();
				return;
			}
			float EnemyTroopStrength = 0f;
			foreach (PlanetGridSquare pgs in this.TargetPlanet.TilesList)
			{
				if (pgs.TroopsHere.Count <= 0)
				{
					if (pgs.building == null || pgs.building.CombatStrength <= 0)
					{
						continue;
					}
					EnemyTroopStrength = EnemyTroopStrength + (float)(pgs.building.CombatStrength + 5);
				}
				else
				{
					EnemyTroopStrength = EnemyTroopStrength + (float)pgs.TroopsHere[0].Strength;
				}
			}
			if (EnemyTroopStrength < 20f)
			{
				EnemyTroopStrength = 25f;
			}
			List<Ship> PotentialAssaultShips = new List<Ship>();
			List<Troop> PotentialTroops = new List<Troop>();
			List<Ship> PotentialBombers = new List<Ship>();
            foreach (Ship ship in this.empire.GetShips().OrderBy(troops => Vector2.Distance(this.AO, troops.Position)))
			{
				if ((ship.TroopList.Count<=0 ||ship.fleet!=null) || (!ship.HasTroopBay && ship.Role != "troop" && !ship.hasTransporter) )
				{
					continue;
				}
				PotentialAssaultShips.Add(ship);
			}
			List<Planet> shipyards = new List<Planet>();
			foreach (Planet planet1 in ClosestAO.GetPlanets())
			{
				if (!planet1.HasShipyard)
				{
					continue;
				}
				shipyards.Add(planet1);
			}
			IOrderedEnumerable<Planet> planets = 
				from p in shipyards
				orderby Vector2.Distance(p.Position, this.TargetPlanet.Position)
				select p;
			if (planets.Count<Planet>() == 0)
			{
				return;
			}
			IOrderedEnumerable<Planet> sortedList = 
				from planet in ClosestAO.GetPlanets()
				orderby Vector2.Distance(planet.Position, planets.First<Planet>().Position)
				select planet;
			foreach (Planet planet2 in sortedList)
			{
				foreach (Troop t in planet2.TroopsHere)
				{
					if (t.GetOwner() != this.empire)
					{
						continue;
					}
					t.SetPlanet(planet2);
					PotentialTroops.Add(t);
				}
			}
			float ourAvailableStrength = 0f;
			foreach (Ship ship in PotentialAssaultShips)
			{
				foreach (Troop t in ship.TroopList)
				{
					ourAvailableStrength = ourAvailableStrength + (float)t.Strength;
				}
			}
			bool GoodToGo = false;
			foreach (Troop t in PotentialTroops)
			{
				ourAvailableStrength = ourAvailableStrength + (float)t.Strength;
			}
			float MinimumEscortStrength = 0f;
			int count = 0;
			float OurPresentStrength = 0f;
			foreach (Ship ship in this.TargetPlanet.system.ShipList)
			{
				if (ship.loyalty == this.TargetPlanet.Owner)
				{
					MinimumEscortStrength = MinimumEscortStrength + ship.GetStrength();
					count++;
				}
				if (ship.loyalty != this.empire)
				{
					continue;
				}
				OurPresentStrength = OurPresentStrength + ship.GetStrength();
			}
			MinimumEscortStrength *= 1.5f;
            // I'm unsure on ball-park figures for ship strengths. Given it used to build up to 1500, sticking flat +300 on seems a good start
            MinimumEscortStrength += 300;
			if (MinimumEscortStrength + OurPresentStrength < this.empire.MilitaryScore *.1f) //+1500
			{
                MinimumEscortStrength = this.empire.MilitaryScore * .15f - OurPresentStrength; //1500f - OurPresentStrength;
			}
            if (MinimumEscortStrength < this.empire.MilitaryScore * .15f)
			{
                MinimumEscortStrength = this.empire.MilitaryScore * .15f;
			}
			this.MinimumTaskForceStrength = MinimumEscortStrength;
			BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();
			float tfstrength = 0f;
			foreach (Ship ship in ClosestAO.GetOffensiveForcePool())
			{
				if (ship.InCombat || ship.fleet != null || tfstrength >= MinimumEscortStrength || ship.GetStrength() <= 0f)
				{
					continue;
				}
				tfstrength = tfstrength + ship.GetStrength();
				elTaskForce.Add(ship);
			}
			if (ourAvailableStrength > EnemyTroopStrength * 1.65f && tfstrength >= this.MinimumTaskForceStrength)
			{
				if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.empire.GetRelations().ContainsKey(this.TargetPlanet.Owner))
				{
					this.EndTask();
					return;
				}
				if (this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWar)
				{
					this.empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWarType);
				}
				GoodToGo = true;
				Fleet newFleet = new Fleet()
				{
					Owner = this.empire,
					Name = "Invasion Fleet"
				};
				int i = 1;
				while (i < 10)
				{
					if (this.empire.GetGSAI().UsedFleets.Contains(i))
					{
						i++;
					}
					else
					{
						float ForceStrength = 0f;
						List<Ship>.Enumerator enumerator1 = PotentialAssaultShips.GetEnumerator();
						try
						{
							do
							{
								if (!enumerator1.MoveNext())
								{
									break;
								}
								Ship ship = enumerator1.Current;
								newFleet.AddShip(ship);
								foreach (Troop t in ship.TroopList)
								{
									ForceStrength = ForceStrength + (float)t.Strength;
								}
							}
							while (ForceStrength <= EnemyTroopStrength * 2f);
						}
						finally
						{
							((IDisposable)enumerator1).Dispose();
						}
						List<Troop>.Enumerator enumerator2 = PotentialTroops.GetEnumerator();
						try
						{
							do
							{
							Label1:
								if (!enumerator2.MoveNext())
								{
									break;
								}
								Troop t = enumerator2.Current;
                                if (t.GetPlanet() != null && t.GetPlanet().ParentSystem.combatTimer<=0 && !t.GetPlanet().RecentCombat &&t.GetPlanet().TroopsHere.Count >t.GetPlanet().developmentLevel)
								{
									(new List<Troop>()).Add(t);
									if (t.GetOwner() != null)
									{
										newFleet.AddShip(t.Launch());
										ForceStrength = ForceStrength + (float)t.Strength;
									}
									else
									{
										goto Label1;
									}
								}
								else
								{
									goto Label1;
								}
							}
							while (ForceStrength <= EnemyTroopStrength + EnemyTroopStrength * 0.3f);
						}
						finally
						{
							((IDisposable)enumerator2).Dispose();
						}
						this.empire.GetFleetsDict()[i] = newFleet;
						this.empire.GetGSAI().UsedFleets.Add(i);
						this.WhichFleet = i;
						newFleet.Task = this;
						foreach (Ship ship in elTaskForce)
						{
							newFleet.AddShip(ship);
							ship.GetAI().OrderQueue.Clear();
							ship.GetAI().State = AIState.AwaitingOrders;
							ClosestAO.GetOffensiveForcePool().Remove(ship);
							ClosestAO.GetWaitingShips().Remove(ship);
						}
						newFleet.AutoArrange();
						break;
					}
				}
				this.Step = 1;
			}
			else if (ourAvailableStrength >= EnemyTroopStrength && tfstrength >= this.MinimumTaskForceStrength)
			{
				foreach (Ship ship in ClosestAO.GetOffensiveForcePool())
				{
					if (ship.BombBays.Count <= 0)
					{
						continue;
					}
					PotentialBombers.Add(ship);
					if (elTaskForce.Contains(ship))
					{
						continue;
					}
					elTaskForce.Add(ship);
				}
				if (PotentialBombers.Count > 0)
				{
					if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.empire.GetRelations().ContainsKey(this.TargetPlanet.Owner))
					{
						this.EndTask();
						return;
					}
					if (this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWar)
					{
						this.empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWarType);
					}
					GoodToGo = true;
					Fleet newFleet = new Fleet()
					{
						Owner = this.empire,
						Name = "Invasion Fleet"
					};
					int i = 1;
					while (i < 10)
					{
						if (this.empire.GetGSAI().UsedFleets.Contains(i))
						{
							i++;
						}
						else
						{
							float ForceStrength = 0f;
							List<Ship>.Enumerator enumerator3 = PotentialAssaultShips.GetEnumerator();
							try
							{
								do
								{
									if (!enumerator3.MoveNext())
									{
										break;
									}
									Ship ship = enumerator3.Current;
									newFleet.AddShip(ship);
									foreach (Troop t in ship.TroopList)
									{
										ForceStrength = ForceStrength + (float)t.Strength;
									}
								}
								while (ForceStrength <= EnemyTroopStrength * 2f);
							}
							finally
							{
								((IDisposable)enumerator3).Dispose();
							}
							enumerator = PotentialTroops.GetEnumerator();
							try
							{
								do
								{
								Label0:
									if (!enumerator.MoveNext())
									{
										break;
									}
									Troop t = enumerator.Current;
									if (t.GetPlanet() != null && t != null)
									{
										(new List<Troop>()).Add(t);
										Ship launched = t.Launch();
										ForceStrength = ForceStrength + (float)t.Strength;
										newFleet.AddShip(launched);
									}
									else
									{
										goto Label0;
									}
								}
								while (ForceStrength <= EnemyTroopStrength + EnemyTroopStrength * 0.3f);
							}
							finally
							{
								((IDisposable)enumerator).Dispose();
							}
							this.empire.GetFleetsDict()[i] = newFleet;
							this.empire.GetGSAI().UsedFleets.Add(i);
							this.WhichFleet = i;
							newFleet.Task = this;
							foreach (Ship ship in elTaskForce)
							{
								newFleet.AddShip(ship);
								ship.GetAI().OrderQueue.Clear();
								ship.GetAI().State = AIState.AwaitingOrders;
								ClosestAO.GetOffensiveForcePool().Remove(ship);
								ClosestAO.GetWaitingShips().Remove(ship);
							}
							newFleet.AutoArrange();
							break;
						}
					}
					this.Step = 1;
				}
			}
			else if (ourAvailableStrength > EnemyTroopStrength * 1.5f)
			{
				if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.empire.GetRelations().ContainsKey(this.TargetPlanet.Owner))
				{
					this.EndTask();
					return;
				}
				if (ClosestAO.GetCoreFleet().Task == null && ClosestAO.GetCoreFleet().GetStrength() > this.MinimumTaskForceStrength)
				{
					MilitaryTask clearArea = new MilitaryTask(ClosestAO.GetCoreFleet().Owner)
					{
						AO = this.TargetPlanet.Position,
						AORadius = 75000f,
						type = MilitaryTask.TaskType.ClearAreaOfEnemies
					};
					ClosestAO.GetCoreFleet().Owner.GetGSAI().TasksToAdd.Add(clearArea);
					clearArea.WhichFleet = ClosestAO.WhichFleet;
					ClosestAO.GetCoreFleet().Task = clearArea;
					clearArea.IsCoreFleetTask = true;
					ClosestAO.GetCoreFleet().TaskStep = 1;
					clearArea.Step = 1;
					if (this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWar)
					{
						this.empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWarType);
					}
				}
			}
			else if (EnemyTroopStrength > 100f)
			{
				this.IsToughNut = true;
			}
			if (!GoodToGo)
			{
				this.NeededTroopStrength = (int)(EnemyTroopStrength + EnemyTroopStrength * 0.3f - ourAvailableStrength);
			}
		}
        //added by gremlin assaultrequistion forces
        private void RequisitionAssaultForcesDevek()
        {
            List<Troop>.Enumerator enumerator;
            if (this.IsToughNut)
            {
                this.DoToughNutRequisition();
                return;
            }
            IOrderedEnumerable<AO> sorted =
                from ao in this.empire.GetGSAI().AreasOfOperations
                orderby Vector2.Distance(this.AO, ao.Position)
                select ao;
            if (sorted.Count<AO>() == 0)
            {
                return;
            }
            AO ClosestAO = sorted.First<AO>();
            if (this.TargetPlanet.Owner == null || !this.empire.GetRelations().ContainsKey(this.TargetPlanet.Owner))
            {
                this.EndTask();
                return;
            }
            if (this.empire.GetRelations()[this.TargetPlanet.Owner].Treaty_Peace)
            {
                this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWar = false;
                this.EndTask();
                return;
            }
            float EnemyTroopStrength = this.TargetPlanet.GetGroundStrengthOther(this.empire);
            //this.TargetPlanet.GetGroundStrengthOther(this.empire);
            //foreach (PlanetGridSquare pgs in this.TargetPlanet.TilesList)
            //{
                
                
            //    if (pgs.TroopsHere.Count <= 0)
            //    {
            //        if (pgs.building == null || pgs.building.CombatStrength <= 0)
            //        {
            //            continue;
            //        }
            //        EnemyTroopStrength = EnemyTroopStrength + (float)(pgs.building.CombatStrength + 5);
            //    }
            //    else if(pgs.TroopsHere[0].GetOwner() != this.empire)
            //    {
            //        EnemyTroopStrength = EnemyTroopStrength + (float)pgs.TroopsHere[0].Strength;
            //    }
            //}
            if (EnemyTroopStrength < 20f)
            {
                EnemyTroopStrength = 25f;
            }

            List<Ship> PotentialAssaultShips = new List<Ship>();
            List<Troop> PotentialTroops = new List<Troop>();
            List<Ship> PotentialBombers = new List<Ship>();
            //added by gremlin trying to get AI to use ships with assault bays.
            foreach (Ship ship in ClosestAO.GetOffensiveForcePool())
            {
                if ((!ship.HasTroopBay || ship.TroopList.Count <= 0) && !(ship.Role == "troop") || ship.fleet != null)
                {
                    if (!ship.HasTroopBay && !ship.hasTransporter || ship.TroopList.Count == 0 || ship.fleet == null)
                    {
                        continue;
                    }
                }
                PotentialAssaultShips.Add(ship);
            }
            List<Planet> shipyards = new List<Planet>();
            foreach (Planet planet1 in ClosestAO.GetPlanets())
            {
                if (!planet1.HasShipyard)
                {
                    continue;
                }
                shipyards.Add(planet1);
            }
            IOrderedEnumerable<Planet> planets =
                from p in shipyards
                orderby Vector2.Distance(p.Position, this.TargetPlanet.Position)
                select p;
            if (planets.Count<Planet>() == 0)
            {
                return;
            }
            IOrderedEnumerable<Planet> sortedList =
                from planet in ClosestAO.GetPlanets()
                orderby Vector2.Distance(planet.Position, planets.First<Planet>().Position)
                select planet;
            foreach (Planet planet2 in sortedList)
            {
                foreach (Troop t in planet2.TroopsHere)
                {
                    if (t.GetOwner() != this.empire || planet2.ParentSystem.combatTimer >0)
                    {
                        continue;
                    }
                    t.SetPlanet(planet2);
                    PotentialTroops.Add(t);
                }
            }
            float ourAvailableStrength = 0f;
            foreach (Ship ship in PotentialAssaultShips)
            {
                foreach (Troop t in ship.TroopList)
                {
                    ourAvailableStrength = ourAvailableStrength + (float)t.Strength;
                }
            }

            bool GoodToGo = false;
            foreach (Troop t in PotentialTroops)
            {
                ourAvailableStrength = ourAvailableStrength + (float)t.Strength;
            }
            float MinimumEscortStrength = 0f;
            int count = 0;
            float OurPresentStrength = 0f;
            foreach (Ship ship in this.TargetPlanet.system.ShipList)
            {
                if (ship.loyalty == this.TargetPlanet.Owner)
                {
                    MinimumEscortStrength = MinimumEscortStrength + ship.GetStrength();
                    count++;
                }
                if (ship.loyalty != this.empire)
                {
                    continue;
                }
                OurPresentStrength = OurPresentStrength + ship.GetStrength();
            }
            MinimumEscortStrength = MinimumEscortStrength + 0.4f * MinimumEscortStrength +EnemyTroopStrength ;

            this.MinimumTaskForceStrength = MinimumEscortStrength;
            BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();
            float tfstrength = 0f;
            foreach (Ship ship in ClosestAO.GetOffensiveForcePool())
            {
                if (ship.InCombat || ship.fleet != null || (ship.Role == "station" || ship.Role == "platform") || tfstrength >= MinimumEscortStrength || ship.GetStrength() <= 0f)
                {
                    continue;
                }
                tfstrength = tfstrength + ship.GetStrength();
                elTaskForce.Add(ship);
            }
            //&& this.TargetPlanet.GetGroundLandingSpots() <this.TargetPlanet.GetPotentialGroundTroops(this.empire) *.5 )
            if (ourAvailableStrength > EnemyTroopStrength * 1.65f && this.TargetPlanet.GetGroundLandingSpots() >5 && tfstrength >= this.MinimumTaskForceStrength)
            {
                if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.empire.GetRelations().ContainsKey(this.TargetPlanet.Owner))
                {
                    this.EndTask();
                    return;
                }
                if (this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWar)
                {
                    this.empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWarType);
                }
                GoodToGo = true;
                Fleet newFleet = new Fleet()
                {
                    Owner = this.empire,
                    Name = "Invasion Fleet"
                };
                int i = 1;
                while (i < 10)
                {
                    if (this.empire.GetGSAI().UsedFleets.Contains(i))
                    {
                        i++;
                    }
                    else
                    {
                        float ForceStrength = 0f;
                        List<Ship>.Enumerator enumerator1 = PotentialAssaultShips.GetEnumerator();
                        try
                        {
                            do
                            {
                                if (!enumerator1.MoveNext())
                                {
                                    break;
                                }
                                Ship ship = enumerator1.Current;
                                newFleet.AddShip(ship);
                                foreach (Troop t in ship.TroopList)
                                {
                                    ForceStrength = ForceStrength + (float)t.Strength;
                                }
                            }
                            while (ForceStrength <= EnemyTroopStrength * 2f);
                        }
                        finally
                        {
                            ((IDisposable)enumerator1).Dispose();
                        }
                        List<Troop>.Enumerator enumerator2 = PotentialTroops.GetEnumerator();
                        try
                        {
                            do
                            {
                            Label1:
                                if (!enumerator2.MoveNext())
                                {
                                    break;
                                }
                                Troop t = enumerator2.Current;
                                if (t.GetPlanet() != null)
                                {
                                    (new List<Troop>()).Add(t);
                                    if (t.GetOwner() != null)
                                    {
                                        newFleet.AddShip(t.Launch());
                                        ForceStrength = ForceStrength + (float)t.Strength;
                                    }
                                    else
                                    {
                                        goto Label1;
                                    }
                                }
                                else
                                {
                                    goto Label1;
                                }
                            }
                            while (ForceStrength <= EnemyTroopStrength + EnemyTroopStrength * 0.3f);
                        }
                        finally
                        {
                            ((IDisposable)enumerator2).Dispose();
                        }
                        this.empire.GetFleetsDict()[i] = newFleet;
                        this.empire.GetGSAI().UsedFleets.Add(i);
                        this.WhichFleet = i;
                        newFleet.Task = this;
                        foreach (Ship ship in elTaskForce)
                        {
                            newFleet.AddShip(ship);
                            ship.GetAI().OrderQueue.Clear();
                            ship.GetAI().State = AIState.AwaitingOrders;
                            ClosestAO.GetOffensiveForcePool().Remove(ship);
                            ClosestAO.GetWaitingShips().Remove(ship);
                        }
                        newFleet.AutoArrange();
                        break;
                    }
                }
                this.Step = 1;
            }
            else if (ourAvailableStrength >= EnemyTroopStrength && tfstrength >= this.MinimumTaskForceStrength)
            {
                foreach (Ship ship in ClosestAO.GetOffensiveForcePool())
                {
                    if ((ship.Role == "station" || ship.Role == "platform") || ship.BombBays.Count <= 0)
                    {
                        continue;
                    }
                    PotentialBombers.Add(ship);
                    if (elTaskForce.Contains(ship))
                    {
                        continue;
                    }
                    elTaskForce.Add(ship);
                }
                if (PotentialBombers.Count > 0)
                {
                    if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.empire.GetRelations().ContainsKey(this.TargetPlanet.Owner))
                    {
                        this.EndTask();
                        return;
                    }
                    if (this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWar)
                    {
                        this.empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWarType);
                    }
                    GoodToGo = true;
                    Fleet newFleet = new Fleet()
                    {
                        Owner = this.empire,
                        Name = "Invasion Fleet"
                    };
                    int i = 1;
                    while (i < 10)
                    {
                        if (this.empire.GetGSAI().UsedFleets.Contains(i))
                        {
                            i++;
                        }
                        else
                        {
                            float ForceStrength = 0f;
                            List<Ship>.Enumerator enumerator3 = PotentialAssaultShips.GetEnumerator();
                            try
                            {
                                do
                                {
                                    if (!enumerator3.MoveNext())
                                    {
                                        break;
                                    }
                                    Ship ship = enumerator3.Current;
                                    newFleet.AddShip(ship);
                                    foreach (Troop t in ship.TroopList)
                                    {
                                        ForceStrength = ForceStrength + (float)t.Strength;
                                    }
                                }
                                while (ForceStrength <= EnemyTroopStrength * 2f);
                            }
                            finally
                            {
                                ((IDisposable)enumerator3).Dispose();
                            }
                            enumerator = PotentialTroops.GetEnumerator();
                            try
                            {
                                do
                                {
                                Label0:
                                    if (!enumerator.MoveNext())
                                    {
                                        break;
                                    }
                                    Troop t = enumerator.Current;
                                    if (t.GetPlanet() != null && t != null)
                                    {
                                        (new List<Troop>()).Add(t);
                                        Ship launched = t.Launch();
                                        ForceStrength = ForceStrength + (float)t.Strength;
                                        newFleet.AddShip(launched);
                                    }
                                    else
                                    {
                                        goto Label0;
                                    }
                                }
                                while (ForceStrength <= EnemyTroopStrength + EnemyTroopStrength * 0.3f);
                            }
                            finally
                            {
                                ((IDisposable)enumerator).Dispose();
                            }
                            this.empire.GetFleetsDict()[i] = newFleet;
                            this.empire.GetGSAI().UsedFleets.Add(i);
                            this.WhichFleet = i;
                            newFleet.Task = this;
                            foreach (Ship ship in elTaskForce)
                            {
                                newFleet.AddShip(ship);
                                ship.GetAI().OrderQueue.Clear();
                                ship.GetAI().State = AIState.AwaitingOrders;
                                ClosestAO.GetOffensiveForcePool().Remove(ship);
                                ClosestAO.GetWaitingShips().Remove(ship);
                            }
                            newFleet.AutoArrange();
                            break;
                        }
                    }
                    this.Step = 1;
                }
            }
            else if (ourAvailableStrength > EnemyTroopStrength * 1.5f)
            {
                if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.empire.GetRelations().ContainsKey(this.TargetPlanet.Owner))
                {
                    this.EndTask();
                    return;
                }
                if (ClosestAO.GetCoreFleet().Task == null && ClosestAO.GetCoreFleet().GetStrength() > this.MinimumTaskForceStrength)
                {
                    MilitaryTask clearArea = new MilitaryTask(ClosestAO.GetCoreFleet().Owner)
                    {
                        AO = this.TargetPlanet.Position,
                        AORadius = 75000f,
                        type = MilitaryTask.TaskType.ClearAreaOfEnemies
                    };
                    ClosestAO.GetCoreFleet().Owner.GetGSAI().TasksToAdd.Add(clearArea);
                    clearArea.WhichFleet = ClosestAO.WhichFleet;
                    ClosestAO.GetCoreFleet().Task = clearArea;
                    clearArea.IsCoreFleetTask = true;
                    ClosestAO.GetCoreFleet().TaskStep = 1;
                    clearArea.Step = 1;
                    if (this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWar)
                    {
                        this.empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.empire.GetRelations()[this.TargetPlanet.Owner].PreparingForWarType);
                    }
                }
            }
            else if (EnemyTroopStrength > 100f)
            {
                this.IsToughNut = true;
            }
            if (!GoodToGo)
            {
                this.NeededTroopStrength = (int)(EnemyTroopStrength + EnemyTroopStrength * 0.3f - ourAvailableStrength);
                
            }
        }

		private void RequisitionClaimForceORIG()
		{
			IOrderedEnumerable<Ship_Game.Gameplay.AO> sorted = 
				from ao in this.empire.GetGSAI().AreasOfOperations
                orderby ao.GetOffensiveForcePool().Sum(strength => strength.GetStrength()) >= this.MinimumTaskForceStrength
				orderby Vector2.Distance(this.AO, ao.Position)
				select ao;
			if (sorted.Count<Ship_Game.Gameplay.AO>() == 0)
			{
				return;
			}
			Ship_Game.Gameplay.AO ClosestAO = sorted.First<Ship_Game.Gameplay.AO>();
			float tfstrength = 0f;
			BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();
			int shipCount = 0;
			foreach (Ship ship in ClosestAO.GetOffensiveForcePool())
			{
				if (tfstrength >= 500f && shipCount >= 3)
				{
					break;
				}
				if (ship.GetStrength() <= 0f || ship.InCombat || ship.fleet != null)
				{
					continue;
				}
				shipCount++;
				elTaskForce.Add(ship);
				tfstrength = tfstrength + ship.GetStrength();
			}
			if (shipCount < 3 || tfstrength < 500f)
			{
				return;
			}
			this.TaskForce = elTaskForce;
			this.StartingStrength = tfstrength;
			int i = 1;
			while (i < 10)
			{
				if (this.empire.GetGSAI().UsedFleets.Contains(i))
				{
					i++;
				}
				else
				{
					Fleet newFleet = new Fleet();
					foreach (Ship ship in this.TaskForce)
					{
						newFleet.AddShip(ship);
					}
					newFleet.Owner = this.empire;
					newFleet.Name = "Scout Fleet";
					newFleet.AutoArrange();
					this.empire.GetFleetsDict()[i] = newFleet;
					this.empire.GetGSAI().UsedFleets.Add(i);
					this.WhichFleet = i;
					newFleet.Task = this;
					List<Ship>.Enumerator enumerator = this.TaskForce.GetEnumerator();
					try
					{
						while (enumerator.MoveNext())
						{
							Ship ship = enumerator.Current;
							ClosestAO.GetOffensiveForcePool().Remove(ship);
							ClosestAO.GetWaitingShips().Remove(ship);
						}
						break;
					}
					finally
					{
						((IDisposable)enumerator).Dispose();
					}
				}
			}
			this.Step = 1;
		}
        //added by gremlin req claim forces
        private void RequisitionClaimForce()
        {
            IOrderedEnumerable<AO> sorted =
                from ao in this.empire.GetGSAI().AreasOfOperations
                orderby ao.GetOffensiveForcePool().Sum(strength => strength.GetStrength()) >= this.MinimumTaskForceStrength
                orderby Vector2.Distance(this.AO, ao.Position)
                select ao;
            if (sorted.Count<AO>() == 0)
            {
                return;
            }
            AO ClosestAO = sorted.First<AO>();
            float tfstrength = 0f;
            BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();
            int shipCount = 0;
            foreach (Ship ship in ClosestAO.GetOffensiveForcePool().OrderBy(str=>str.GetStrength()))
            {
                if (shipCount >= 3 && tfstrength >= this.empire.MilitaryScore*.1)
                {
                    break;
                }
                if (ship.GetStrength() <= 0f || ship.InCombat || ship.fleet != null)
                {
                    continue;
                }
                shipCount++;
                elTaskForce.Add(ship);
                tfstrength = tfstrength + ship.GetStrength();
            }
            if (shipCount < 3 && tfstrength < this.empire.MilitaryScore * .1)//|| tfstrength < 500f)
            {
                return;
            }
            this.TaskForce = elTaskForce;
            this.StartingStrength = tfstrength;
            int i = 1;
            while (i < 10)
            {
                if (this.empire.GetGSAI().UsedFleets.Contains(i))
                {
                    i++;
                }
                else
                {
                    Fleet newFleet = new Fleet();
                    foreach (Ship ship in this.TaskForce)
                    {
                        newFleet.AddShip(ship);
                    }
                    newFleet.Owner = this.empire;
                    newFleet.Name = "Scout Fleet";
                    newFleet.AutoArrange();
                    this.empire.GetFleetsDict()[i] = newFleet;
                    this.empire.GetGSAI().UsedFleets.Add(i);
                    this.WhichFleet = i;
                    newFleet.Task = this;
                    List<Ship>.Enumerator enumerator = this.TaskForce.GetEnumerator();
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            Ship ship = enumerator.Current;
                            ClosestAO.GetOffensiveForcePool().Remove(ship);
                            ClosestAO.GetWaitingShips().Remove(ship);
                        }
                        break;
                    }
                    finally
                    {
                        ((IDisposable)enumerator).Dispose();
                    }
                }
            }
            this.Step = 1;
        }
		private void RequisitionDefenseForce()
		{
			float forcePoolStr = 0f;
			float tfstrength = 0f;
			BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();
			foreach (Ship ship in this.empire.GetForcePool())
			{
				forcePoolStr = forcePoolStr + ship.GetStrength();
			}
			foreach (Ship ship in this.empire.GetForcePool().OrderBy(strength=> strength.GetStrength()))
			{
				if (ship.fleet != null)
				{
					continue;
				}
				if (tfstrength >= forcePoolStr / 2f)
				{
					break;
				}
				if (ship.GetStrength() <= 0f || ship.InCombat)
				{
					continue;
				}
				elTaskForce.Add(ship);
				tfstrength = tfstrength + ship.GetStrength();
			}
			this.TaskForce = elTaskForce;
			this.StartingStrength = tfstrength;
			int i = 1;
			while (i < 10)
			{
				if (this.empire.GetGSAI().UsedFleets.Contains(i))
				{
					i++;
				}
				else
				{
					Fleet newFleet = new Fleet();
					foreach (Ship ship in this.TaskForce)
					{
						newFleet.AddShip(ship);
					}
					newFleet.Owner = this.empire;
					newFleet.Name = "Defensive Fleet";
					newFleet.AutoArrange();
					this.empire.GetFleetsDict()[i] = newFleet;
					this.empire.GetGSAI().UsedFleets.Add(i);
					this.WhichFleet = i;
					newFleet.Task = this;
					List<Ship>.Enumerator enumerator = this.TaskForce.GetEnumerator();
					try
					{
						while (enumerator.MoveNext())
						{
							Ship ship = enumerator.Current;
							this.empire.ForcePoolRemove(ship);
						}
						break;
					}
					finally
					{
						((IDisposable)enumerator).Dispose();
					}
				}
			}
			this.Step = 1;
		}

		private void RequisitionExplorationForcebroke()
		{
			IOrderedEnumerable<Ship_Game.Gameplay.AO> sorted = 
				from ao in this.empire.GetGSAI().AreasOfOperations
				orderby Vector2.Distance(this.AO, ao.Position)
				select ao;
			if (sorted.Count<Ship_Game.Gameplay.AO>() == 0)
			{
				return;
			}
			Ship_Game.Gameplay.AO ClosestAO = sorted.First<Ship_Game.Gameplay.AO>();
			this.EnemyStrength = 0f;
			foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.empire.GetGSAI().ThreatMatrix.Pins)
			{
				if (Vector2.Distance(this.AO, pin.Value.Position) >= this.AORadius || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == this.empire)
				{
					continue;
				}
				MilitaryTask enemyStrength = this;
				enemyStrength.EnemyStrength = enemyStrength.EnemyStrength + pin.Value.Strength;
			}
			this.MinimumTaskForceStrength = this.EnemyStrength + 0.35f * this.EnemyStrength;
			if (this.MinimumTaskForceStrength == 0f)
			{
				this.MinimumTaskForceStrength = 500f;
			}
			foreach (KeyValuePair<Empire, Relationship> entry in this.empire.GetRelations())
			{
				if (!entry.Value.AtWar || entry.Key.isFaction || this.MinimumTaskForceStrength <= 1000f)
				{
					continue;
				}
				this.EndTask();
				return;
			}
			List<Ship> PotentialAssaultShips = new List<Ship>();
			List<Troop> PotentialTroops = new List<Troop>();
			foreach (Ship ship in ClosestAO.GetOffensiveForcePool())
			{
				if (ship.fleet != null || (!ship.HasTroopBay && !ship.hasTransporter || ship.TroopList.Count <= 0) && !(ship.Role == "troop") || ship.fleet != null)
				{
					continue;
				}
				PotentialAssaultShips.Add(ship);
			}
			List<Planet> shipyards = new List<Planet>();
			foreach (Planet planet1 in ClosestAO.GetPlanets())
			{
				if (!planet1.HasShipyard)
				{
					continue;
				}
				shipyards.Add(planet1);
			}
			IOrderedEnumerable<Planet> planets = 
				from p in shipyards
				orderby Vector2.Distance(p.Position, this.TargetPlanet.Position)
				select p;
			if (planets.Count<Planet>() != 0)
			{
				IOrderedEnumerable<Planet> sortedList = 
					from planet in ClosestAO.GetPlanets()
					orderby Vector2.Distance(planet.Position, planets.First<Planet>().Position)
					select planet;
				foreach (Planet planet2 in sortedList)
				{
					foreach (Troop t in planet2.TroopsHere)
					{
						if (t.GetOwner() != this.empire)
						{
							continue;
						}
						t.SetPlanet(planet2);
						PotentialTroops.Add(t);
					}
				}
				float ourAvailableStrength = 0f;
				foreach (Ship ship in PotentialAssaultShips)
				{
					if (ship.fleet != null)
					{
						continue;
					}
					foreach (Troop t in ship.TroopList)
					{
						ourAvailableStrength = ourAvailableStrength + (float)t.Strength;
					}
				}
				foreach (Troop t in PotentialTroops)
				{
					ourAvailableStrength = ourAvailableStrength + (float)t.Strength;
				}
				float tfstrength = 0f;
				BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();
				foreach (Ship ship in ClosestAO.GetOffensiveForcePool())
				{
					if (ship.InCombat || ship.fleet != null || tfstrength >= this.MinimumTaskForceStrength)
					{
						continue;
					}
					tfstrength = tfstrength + ship.GetStrength();
					elTaskForce.Add(ship);
				}
				if (tfstrength >= this.MinimumTaskForceStrength && ourAvailableStrength >= 20f)
				{
					this.TaskForce = elTaskForce;
					this.StartingStrength = tfstrength;
					int i = 1;
					while (i < 10)
					{
						if (this.empire.GetGSAI().UsedFleets.Contains(i))
						{
							i++;
						}
						else
						{
							Fleet newFleet = new Fleet();
							float ForceStrength = 0f;
							List<Ship>.Enumerator enumerator = PotentialAssaultShips.GetEnumerator();
							try
							{
								do
								{
									if (!enumerator.MoveNext())
									{
										break;
									}
									Ship ship = enumerator.Current;
									newFleet.AddShip(ship);
									foreach (Troop t in ship.TroopList)
									{
										ForceStrength = ForceStrength + (float)t.Strength;
									}
								}
								while (ForceStrength < 20f);
							}
							finally
							{
								((IDisposable)enumerator).Dispose();
							}
							List<Troop>.Enumerator enumerator1 = PotentialTroops.GetEnumerator();
							try
							{
								do
								{
								Label1:
									if (!enumerator1.MoveNext())
									{
										break;
									}
									Troop t = enumerator1.Current;
									if (t.GetPlanet() != null)
									{
										(new List<Troop>()).Add(t);
										Ship launched = t.Launch();
										ForceStrength = ForceStrength + (float)t.Strength;
										newFleet.AddShip(launched);
									}
									else
									{
										goto Label1;
									}
								}
								while (ForceStrength < 20f);
							}
							finally
							{
								((IDisposable)enumerator1).Dispose();
							}
							foreach (Ship ship in this.TaskForce)
							{
								ship.GetAI().OrderQueue.Clear();
								ship.GetAI().State = AIState.AwaitingOrders;
								newFleet.AddShip(ship);
								ClosestAO.GetOffensiveForcePool().Remove(ship);
								ClosestAO.GetWaitingShips().Remove(ship);
							}
							newFleet.Owner = this.empire;
							newFleet.Name = "Exploration Force";
							newFleet.AutoArrange();
							this.empire.GetFleetsDict()[i] = newFleet;
							this.empire.GetGSAI().UsedFleets.Add(i);
							this.WhichFleet = i;
							newFleet.Task = this;
							break;
						}
					}
					this.Step = 1;
				}
				return;
			}
			this.EndTask();
		}
        //added by gremlin Req Exploration forces
        private void RequisitionExplorationForce()
        {
            IOrderedEnumerable<AO> sorted =
                from ao in this.empire.GetGSAI().AreasOfOperations
                orderby Vector2.Distance(this.AO, ao.Position)
                select ao;
            if (sorted.Count<AO>() == 0)
            {
                return;
            }
            AO ClosestAO = sorted.First<AO>();
            this.EnemyStrength = 0f;
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.empire.GetGSAI().ThreatMatrix.Pins)
            {
                if (Vector2.Distance(this.AO, pin.Value.Position) >= this.AORadius || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == this.empire)
                {
                    continue;
                }
                MilitaryTask enemyStrength = this;
                enemyStrength.EnemyStrength = enemyStrength.EnemyStrength + pin.Value.Strength;
            }
            this.MinimumTaskForceStrength = this.EnemyStrength + 0.35f * this.EnemyStrength;
            if (this.MinimumTaskForceStrength == 0f)
            {
                this.MinimumTaskForceStrength = ClosestAO.GetOffensiveForcePool().Sum(strength => strength.GetStrength()) *.2f;
            }
            foreach (KeyValuePair<Empire, Relationship> entry in this.empire.GetRelations())
            {
                if (!entry.Value.AtWar || entry.Key.isFaction || this.MinimumTaskForceStrength <=  ClosestAO.GetOffensiveForcePool().Sum(strength => strength.GetStrength())* .5f)
                {
                    continue;
                }
                this.EndTask();
                return;
            }
            List<Ship> PotentialAssaultShips = new List<Ship>();
            List<Troop> PotentialTroops = new List<Troop>();
            foreach (Ship ship in ClosestAO.GetOffensiveForcePool())
            {
                if (ship.fleet != null || (!ship.HasTroopBay && !ship.hasTransporter && ship.Role != "troop") ||  ship.TroopList.Count ==0)    
                {
                    continue;
                }
                PotentialAssaultShips.Add(ship);
            }
            List<Planet> shipyards = new List<Planet>();
            foreach (Planet planet1 in ClosestAO.GetPlanets())
            {
                if (!planet1.HasShipyard)
                {
                    continue;
                }
                shipyards.Add(planet1);
            }
            IOrderedEnumerable<Planet> planets =
                from p in shipyards
                orderby Vector2.Distance(p.Position, this.TargetPlanet.Position)
                select p;
            if (planets.Count<Planet>() != 0)
            {
                IOrderedEnumerable<Planet> sortedList =
                    from planet in ClosestAO.GetPlanets()
                    orderby Vector2.Distance(planet.Position, planets.First<Planet>().Position)
                    select planet;
                foreach (Planet planet2 in sortedList)
                {
                    foreach (Troop t in planet2.TroopsHere)
                    {
                        if (t.GetOwner() != this.empire)
                        {
                            continue;
                        }
                        t.SetPlanet(planet2);
                        PotentialTroops.Add(t);
                    }
                }
                float ourAvailableStrength = 0f;
                foreach (Ship ship in PotentialAssaultShips)
                {
                    if (ship.fleet != null)
                    {
                        continue;
                    }
                    foreach (Troop t in ship.TroopList)
                    {
                        ourAvailableStrength = ourAvailableStrength + (float)t.Strength;
                    }
                }
                foreach (Troop t in PotentialTroops)
                {
                    ourAvailableStrength = ourAvailableStrength + (float)t.Strength;
                }
                float tfstrength = 0f;
                BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();
                foreach (Ship ship in ClosestAO.GetOffensiveForcePool().OrderBy(strength=> strength.GetStrength()))
                {
                    if (ship.InCombat || ship.fleet != null || tfstrength >= this.MinimumTaskForceStrength + ship.GetStrength())
                    {
                        continue;
                    }
                    tfstrength = tfstrength + ship.GetStrength();
                    elTaskForce.Add(ship);
                }
                if (tfstrength >= this.MinimumTaskForceStrength && ourAvailableStrength >= 20f)
                {
                    this.TaskForce = elTaskForce;
                    this.StartingStrength = tfstrength;
                    int i = 1;
                    while (i < 10)
                    {
                        if (this.empire.GetGSAI().UsedFleets.Contains(i))
                        {
                            i++;
                        }
                        else
                        {
                            Fleet newFleet = new Fleet();
                            float ForceStrength = 0f;
                            List<Ship>.Enumerator enumerator = PotentialAssaultShips.GetEnumerator();
                            try
                            {
                                do
                                {
                                    if (!enumerator.MoveNext())
                                    {
                                        break;
                                    }
                                    Ship ship = enumerator.Current;
                                    newFleet.AddShip(ship);
                                    foreach (Troop t in ship.TroopList)
                                    {
                                        ForceStrength = ForceStrength + (float)t.Strength;
                                    }
                                }
                                while (ForceStrength < 20f);
                            }
                            finally
                            {
                                ((IDisposable)enumerator).Dispose();
                            }
                            List<Troop>.Enumerator enumerator1 = PotentialTroops.GetEnumerator();
                            try
                            {
                                do
                                {
                                Label1:
                                    if (!enumerator1.MoveNext())
                                    {
                                        break;
                                    }
                                    Troop t = enumerator1.Current;
                                    if (t.GetPlanet() != null)
                                    {
                                        (new List<Troop>()).Add(t);
                                        Ship launched = t.Launch();
                                        ForceStrength = ForceStrength + (float)t.Strength;
                                        newFleet.AddShip(launched);
                                    }
                                    else
                                    {
                                        goto Label1;
                                    }
                                }
                                while (ForceStrength < 20f);
                            }
                            finally
                            {
                                ((IDisposable)enumerator1).Dispose();
                            }
                            foreach (Ship ship in this.TaskForce)
                            {
                                ship.GetAI().OrderQueue.Clear();
                                ship.GetAI().State = AIState.AwaitingOrders;
                                newFleet.AddShip(ship);
                                ClosestAO.GetOffensiveForcePool().Remove(ship);
                                ClosestAO.GetWaitingShips().Remove(ship);
                            }
                            newFleet.Owner = this.empire;
                            newFleet.Name = "Exploration Force";
                            newFleet.AutoArrange();
                            this.empire.GetFleetsDict()[i] = newFleet;
                            this.empire.GetGSAI().UsedFleets.Add(i);
                            this.WhichFleet = i;
                            newFleet.Task = this;
                            break;
                        }
                    }
                    this.Step = 1;
                }
                return;
            }
            this.EndTask();
        }

		private void RequisitionForces()
		{
			IOrderedEnumerable<Ship_Game.Gameplay.AO> sorted = 
				from ao in this.empire.GetGSAI().AreasOfOperations
                orderby ao.GetOffensiveForcePool().Sum(strength => strength.GetStrength()) >= this.MinimumTaskForceStrength descending
                orderby Vector2.Distance(this.AO, ao.Position)
				select ao;
			if (sorted.Count<Ship_Game.Gameplay.AO>() == 0)
			{
				return;
			}
			Ship_Game.Gameplay.AO ClosestAO = sorted.First<Ship_Game.Gameplay.AO>();
			this.EnemyStrength = 0f;
			foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.empire.GetGSAI().ThreatMatrix.Pins)
			{
				if (Vector2.Distance(this.AO, pin.Value.Position) >= this.AORadius)
				{
					continue;
				}
				Empire Them = EmpireManager.GetEmpireByName(pin.Value.EmpireName);
				if (Them == this.empire || !Them.isFaction && !this.empire.GetRelations()[Them].AtWar)
				{
					continue;
				}
				MilitaryTask enemyStrength = this;
				enemyStrength.EnemyStrength = enemyStrength.EnemyStrength + pin.Value.Strength;
			}
			this.MinimumTaskForceStrength = this.EnemyStrength + 0.35f * this.EnemyStrength;
			if (this.MinimumTaskForceStrength == 0f)
			{
				this.EndTask();
				return;
			}
			if (ClosestAO.GetCoreFleet().Task == null && ClosestAO.GetCoreFleet().GetStrength() > this.MinimumTaskForceStrength)
			{
				this.WhichFleet = ClosestAO.WhichFleet;
				ClosestAO.GetCoreFleet().Task = this;
				ClosestAO.GetCoreFleet().TaskStep = 1;
				this.IsCoreFleetTask = true;
				this.Step = 1;
			}
		}

		private void RequisitionSupplyFleet()
		{
		}

		public void SetEmpire(Empire e)
		{
			this.empire = e;
		}

		public void SetTargetPlanet(Planet p)
		{
			this.TargetPlanet = p;
			this.TargetPlanetGuid = p.guid;
		}

		public enum TaskType
		{
			ClearAreaOfEnemies,
			Resupply,
			AssaultPlanet,
			CorsairRaid,
			CohesiveClearAreaOfEnemies,
			Exploration,
			DefendSystem,
			DefendClaim,
			DefendPostInvasion,
			GlassPlanet
		}
	}
}