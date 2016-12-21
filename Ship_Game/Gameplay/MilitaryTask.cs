using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Ship_Game.Gameplay
{
	public sealed class MilitaryTask : IDisposable
	{
        [Serialize(0)] public bool IsCoreFleetTask;
        [Serialize(1)] public bool WaitForCommand;
        [Serialize(2)] public List<Guid> HeldGoals = new List<Guid>();
        [Serialize(3)] public int Step;
        [Serialize(4)] public Guid TargetPlanetGuid = Guid.Empty;
        [Serialize(5)] public TaskType type;
        [Serialize(6)] public Vector2 AO;
        [Serialize(7)] public float AORadius;
        [Serialize(8)] public float InitialEnemyStrength;
        [Serialize(9)] public float EnemyStrength;
        [Serialize(10)] public float StartingStrength;
        [Serialize(11)] public float MinimumTaskForceStrength;
        [Serialize(12)] public float TaskTimer;
        [Serialize(13)] public int WhichFleet = -1;
        [Serialize(14)] public bool IsToughNut;
        [Serialize(15)] public int NeededTroopStrength;

        [XmlIgnore][JsonIgnore] private Planet TargetPlanet;
        [XmlIgnore][JsonIgnore] private Empire empire;
        [XmlIgnore][JsonIgnore] private BatchRemovalCollection<Ship> TaskForce = new BatchRemovalCollection<Ship>();
        [XmlIgnore][JsonIgnore] private bool disposed;      //adding for thread safe Dispose because class uses unmanaged resources 

        //This file Refactored by Gretman

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

            this.EnemyStrength = Owner.GetGSAI().ThreatMatrix.PingRadarStr(location, radius, Owner);
            if (InitialEnemyStrength == 0)
                this.InitialEnemyStrength = EnemyStrength;

            this.MinimumTaskForceStrength = this.EnemyStrength *.75f;
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

        private void GetAvailableShips(AO area, List<Ship> Bombers, List<Ship> Combat, List<Ship> TroopShips, List<Ship> Utility)
        {
            foreach (Ship ship in this.empire.GetShips().OrderBy(str => str.BaseStrength).ThenBy(ship => Vector2.Distance(ship.Center, area.Position) >= area.Radius))
            {
                if ((ship.shipData.Role == ShipData.RoleName.station || ship.shipData.Role == ShipData.RoleName.platform)
                    || !ship.BaseCanWarp
                    || ship.InCombat
                    || ship.fleet != null
                    || ship.Mothership != null
                    || this.empire.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Contains(ship)
                    || ship.GetAI().State !=  AIState.AwaitingOrders
                    || (ship.System!= null && ship.System.CombatInSystem)   )
                    continue;

                if(Utility != null && ship.InhibitionRadius > 0 || ship.hasOrdnanceTransporter || ship.hasRepairBeam || ship.HasRepairModule || ship.HasSupplyBays )
                {
                    Utility.Add(ship);
                }
                else if (Bombers != null && ship.BombBays.Count > 0)
                {
                    Bombers.Add(ship);
                }
                else if(TroopShips !=null && ship.TroopList.Count >0 && (ship.hasAssaultTransporter || ship.HasTroopBay || ship.GetShipData().Role == ShipData.RoleName.troop))
                {
                    TroopShips.Add(ship);
                }
                else if (Combat != null && ship.BombBays.Count <= 0 && ship.BaseStrength > 0)
                {
                    Combat.Add(ship);
                }
            }
        }

        private void DoToughNutRequisition()
        {
            float EnemyTroopStr = this.GetEnemyTroopStr();
            if (EnemyTroopStr < 100)
                EnemyTroopStr = 100;

            float EnemyShipStr = this.GetEnemyStrAtTarget();
            IOrderedEnumerable<AO> sorted =
                from ao in this.empire.GetGSAI().AreasOfOperations
                where ao.GetCoreFleet().Task == null || ao.GetCoreFleet().Task.type != TaskType.AssaultPlanet
                orderby ao.GetOffensiveForcePool().Where(combat=> !combat.InCombat).Sum(strength => strength.BaseStrength) >= this.MinimumTaskForceStrength descending
                orderby Vector2.Distance(this.AO, ao.Position)
                select ao;

            if (sorted.Count<AO>() == 0)
                return;

            List<Ship> Bombers = new List<Ship>();
            List<Ship> EverythingElse = new List<Ship>();
            List<Ship> TroopShips = new List<Ship>();
            List<Troop> Troops = new List<Troop>();
            
            foreach (AO area in sorted)
            {
                this.GetAvailableShips(area, Bombers, EverythingElse, TroopShips, EverythingElse);
                foreach (Planet p in area.GetPlanets())
                {
                    if (p.RecentCombat || p.ParentSystem.combatTimer>0)
                        continue;

                    foreach (Troop t in p.TroopsHere)
                    {
                        if (t.GetOwner() != this.empire)
                            continue;

                        Troops.Add(t);
                    }
                }
            }

            EverythingElse.AddRange(TroopShips);
            List<Ship> TaskForce = new List<Ship>();
            float strAdded = 0f;
            float troopStr = 0f;
            int numOfTroops = 0;

            foreach (Ship ship in EverythingElse)
            {
                if (strAdded < EnemyShipStr * 1.65f)
                    break;

                if (ship.HasTroopBay)
                {
                    foreach (ShipModule Hangar in ship.GetHangars())
                    {
                        troopStr += 10;
                        numOfTroops++;
                    }
                }
                TaskForce.Add(ship);
                strAdded += ship.GetStrength();
            }

            List<Ship> BombTaskForce = new List<Ship>();
            int numBombs = 0;
            foreach (Ship ship in Bombers)
            {
                if (numBombs >= 20 || BombTaskForce.Contains(ship))
                    continue;

                if (ship.HasTroopBay)
                {
                    foreach (ShipModule Hangar in ship.GetHangars())
                    {
                        troopStr += 10;
                        numOfTroops++;
                    }
                }
                BombTaskForce.Add(ship);
                numBombs += ship.BombBays.Count;
            }

            List<Troop> PotentialTroops = new List<Troop>();
            foreach (Troop t in Troops)
            {
                if (troopStr > EnemyTroopStr * 1.5f || numOfTroops > this.TargetPlanet.GetGroundLandingSpots() )
                    break;

                PotentialTroops.Add(t);
                troopStr += (float)t.Strength;
                numOfTroops++;
            }

            if (strAdded > EnemyShipStr * 1.65f)
            {
                if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.empire.TryGetRelations(this.TargetPlanet.Owner, out Relationship rel))
                {
                    this.EndTask();
                    return;
                }

                if (this.empire.GetRelations(this.TargetPlanet.Owner).PreparingForWar)
                {
                    this.empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.empire.GetRelations(this.TargetPlanet.Owner).PreparingForWarType);
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
                        ship.fleet.Ships.Remove(ship);

                    ship.GetAI().OrderQueue.Clear();
                    this.empire.GetGSAI().DefensiveCoordinator.remove(ship);
                    ship.fleet = null;

                    ClosestAO.GetCoreFleet().AddShip(ship);
                }

                foreach (Troop t in PotentialTroops)
                {
                    if (t.GetPlanet() == null)
                        continue;

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
                    this.empire.GetFleetsDict().TryAdd(GlassPlanet.WhichFleet, bomberFleet);
                    bomberFleet.Task = GlassPlanet;
                    bomberFleet.Name = "Bomber Fleet";

                    foreach (Ship ship in BombTaskForce)
                    {
                        if (ship.fleet != null)
                            ship.fleet.Ships.Remove(ship);

                        ship.GetAI().OrderQueue.Clear();
                        this.empire.GetGSAI().DefensiveCoordinator.remove(ship);
                        ship.fleet = null;

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
						continue;

					g.Held = false;
				}
			}
            
            AO closestAO = empire.GetGSAI().AreasOfOperations.FindMin(ao => AO.SqDist(ao.Position));
            
            if (closestAO == null)
            {
                if (!this.IsCoreFleetTask && this.WhichFleet != -1 && this.empire != Ship.universeScreen.player)
                {
                    foreach (Ship ship in this.empire.GetFleetsDict()[this.WhichFleet].Ships)
                    {
                        this.empire.ForcePoolAdd(ship);
                    }
                }
				return;
			}

			if (this.WhichFleet != -1)
			{
				if (this.IsCoreFleetTask)
				{
                    closestAO.GetCoreFleet().Task = null;
                    closestAO.GetCoreFleet().MoveToDirectly(closestAO.Position, 0f, new Vector2(0f, -1f));
                    closestAO.TurnsToRelax = 0;
				}
				else
				{
					if (!this.empire.GetFleetsDict().ContainsKey(this.WhichFleet))
					{ //what the hell is this for? dictionary doesnt contain the key the foreach below would blow up. 
                        if (!this.IsCoreFleetTask && this.empire != Ship.universeScreen.player)
                        {
                            foreach (Ship ship in this.empire.GetFleetsDict()[this.WhichFleet].Ships)
                            {
                                this.empire.ForcePoolAdd(ship);
                            }
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
                        if (ship.shipData.Role != ShipData.RoleName.troop)
						{
                            closestAO.GetOffensiveForcePool().Add(ship);
							ship.GetAI().OrderResupplyNearest(false);
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
						if (   t.GetOwner() != this.empire
                            || this.TargetPlanet.system.CombatInSystem
                            || t.AvailableAttackActions==0
                            || t.MoveTimer>0)
                            continue;

						toLaunch.Add(t);
					}

					foreach (Troop t in toLaunch)
					{
						Ship troopship = t.Launch();
						if (troopship == null)
							continue;

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
						continue;

					g.Held = false;
				}
			}

            AO closestAO = empire.GetGSAI().AreasOfOperations.FindMin(ao => AO.SqDist(ao.Position));
			if (closestAO == null)
			{
                if (  !this.IsCoreFleetTask && this.WhichFleet != -1 && this.empire != EmpireManager.Player)
                {
                    foreach (Ship ship in this.empire.GetFleetsDict()[this.WhichFleet].Ships)
                    {
                        this.empire.ForcePoolAdd(ship);
                    }
                }
                return;
			}

			if (this.WhichFleet != -1)
			{
				if (this.IsCoreFleetTask)
				{
					this.empire.GetFleetsDict()[this.WhichFleet].Task = null;
					this.empire.GetFleetsDict()[this.WhichFleet].MoveToDirectly(closestAO.Position, 0f, new Vector2(0f, -1f));
				}
				else
				{
					foreach (Ship ship in this.empire.GetFleetsDict()[this.WhichFleet].Ships)
					{
						ship.fleet = null;
                        closestAO.AddShip(ship);
                        closestAO.TurnsToRelax = 0;
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
                    {
                        if      (Step == 0) this.RequisitionForces();
                        else if (Step == 1) this.ExecuteAndAssess();
                        break;
                    }
                case MilitaryTask.TaskType.AssaultPlanet:
                    {
                        if (Step == 0) this.RequisitionAssaultForces();
                        else
                        {
                            if (this.empire.GetFleetsDict().TryGetValue(this.WhichFleet, out Fleet fleet))
                            {
                                if (fleet.Ships.Count != 0)
                                    break;
                            }

                            this.EndTask();
                        }
                        break;
                    }
                case MilitaryTask.TaskType.CorsairRaid:
                    {
                        if (this.Step != 0)
                            break;

                        this.empire.GetFleetsDict()[1].Reset();
                        foreach (Ship shiptoadd in (List<Ship>)this.empire.GetShips())
                        {
                            if (shiptoadd.shipData.Role != ShipData.RoleName.platform)
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
                    }
                case MilitaryTask.TaskType.CohesiveClearAreaOfEnemies:
                    {
                        if      (Step == 0) this.RequisitionForces();
                        else if (Step == 1) this.ExecuteAndAssess();
                        break;
                    }
                case MilitaryTask.TaskType.Exploration:
                    {
                        if (this.Step == 0) this.RequisitionExplorationForce();
                        break;
                    }
                case MilitaryTask.TaskType.DefendSystem:
                    {
                        if      (Step == 0) this.RequisitionDefenseForce();
                        else if (Step == 1)
                        {
                            if (this.empire.GetFleetsDict().ContainsKey(this.WhichFleet))
                            {
                                if (this.empire.GetFleetsDict()[this.WhichFleet].Ships.Count != 0)
                                    break;
                            }
                            this.EndTask();
                        }
                        break;
                    }
                case MilitaryTask.TaskType.DefendClaim:
                    {
                        switch (this.Step)
                        {
                            case 0:
                                {
                                    if (this.TargetPlanet.Owner != null)
                                    {
                                        empire.TryGetRelations(TargetPlanet.Owner, out Relationship rel);

                                        if (rel != null && (!rel.AtWar && !rel.PreparingForWar))
                                            this.EndTask();
                                    }
                                    this.RequisitionClaimForce();
                                    return;
                                }

                            case 1:
                                {
                                    if (this.empire.GetFleetsDict().ContainsKey(this.WhichFleet))
                                    {
                                        if (this.empire.GetFleetsDict()[this.WhichFleet].Ships.Count == 0)
                                        {
                                            this.EndTask();
                                            return;
                                        }

                                        if (this.TargetPlanet.Owner != null) // &&(this.empire.GetFleetsDict().ContainsKey(this.WhichFleet)))
                                        {
                                        this.empire.TryGetRelations(TargetPlanet.Owner, out Relationship rel);
                                            if (rel != null && (rel.AtWar || rel.PreparingForWar))
                                            {
                                                if (Vector2.Distance(this.empire.GetFleetsDict()[this.WhichFleet].findAveragePosition(), this.TargetPlanet.Position) < this.AORadius)
                                                    this.Step = 2;

                                                return;
                                            }
                                        }
                                    }
                                    else
                                        this.EndTask();

                                    if (this.TargetPlanet.Owner == null)
                                        return;

                                    this.EndTask();
                                    return;
                                }

                            case 2:
                                {
                                    if (this.empire.GetFleetsDict().ContainsKey(this.WhichFleet))
                                    {
                                        if (this.empire.GetFleetsDict()[this.WhichFleet].Ships.Count == 0)
                                        {
                                            this.EndTask();
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        this.EndTask();
                                        return;
                                    }

                                    if (this.TargetPlanet.Owner == null)
                                    {
                                        this.EndTask();
                                        return;
                                    }

                                    empire.TryGetRelations(TargetPlanet.Owner, out Relationship rel);
                                    if (rel != null && !(rel.AtWar || rel.PreparingForWar))
                                        this.EndTask();

                                    if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner == this.empire)
                                        this.EndTask();

                                    return;
                                }
                            default:
                                return;
                        }
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

            if (this.type == TaskType.Exploration ||this.type ==TaskType.AssaultPlanet)
            {
                float groundstrength = this.GetTargetPlanet().GetGroundStrengthOther(this.empire);
                float ourGroundStrength = this.GetTargetPlanet().GetGroundStrength(this.empire);

                if (ourGroundStrength > 0)
                {
                    if (this.type == TaskType.Exploration)
                    {
                        Planet p = this.GetTargetPlanet();
                        if (p.BuildingList.Where(relic => !string.IsNullOrEmpty(relic.EventTriggerUID)).Count() > 0)
                            return;
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
				if (!ship.Active || ship.InCombat && this.Step < 1 || ship.GetAI().State == AIState.Scrap)
				{
					this.empire.GetFleetsDict()[this.WhichFleet].Ships.QueuePendingRemoval(ship);
                    if (ship.Active && ship.GetAI().State != AIState.Scrap)
                    {
                        if (ship.fleet != null)
                            this.empire.GetFleetsDict()[this.WhichFleet].Ships.QueuePendingRemoval(ship);
                        
                        this.empire.ForcePoolAdd(ship);
                    }
                    else if (ship.GetAI().State == AIState.Scrap)
                    {
                        if (ship.fleet != null)
                            this.empire.GetFleetsDict()[this.WhichFleet].Ships.QueuePendingRemoval(ship);
                    }
				}
				else
				{
					currentStrength += ship.GetStrength();
				}
			}

			this.empire.GetFleetsDict()[this.WhichFleet].Ships.ApplyPendingRemovals();
			float currentEnemyStrength = 0f;

			foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.empire.GetGSAI().ThreatMatrix.Pins)
			{
				if (Vector2.Distance(this.AO, pin.Value.Position) >= this.AORadius || pin.Value.Ship == null)
					continue;

				Empire pinEmp = EmpireManager.GetEmpireByName(pin.Value.EmpireName);

				if (pinEmp == this.empire || !pinEmp.isFaction && !this.empire.GetRelations(pinEmp).AtWar )
					continue;

				currentEnemyStrength += pin.Value.Strength;
			}

			if (currentStrength < 0.15f * this.StartingStrength && currentEnemyStrength > currentStrength)
			{
				this.EndTask();
				return;
			}

			if (currentEnemyStrength == 0f || currentStrength == 0f)
				this.EndTask();
		}

		public void FactionEndTask()
		{
			if (this.WhichFleet != -1)
			{
				if (!this.IsCoreFleetTask)
				{
					if (!this.empire.GetFleetsDict().ContainsKey(this.WhichFleet))
					    return;

					foreach (Ship ship in this.empire.GetFleetsDict()[this.WhichFleet].Ships)
					{
						ship.GetAI().OrderQueue.Clear();
						ship.GetAI().State = AIState.AwaitingOrders;
						ship.fleet = null;
						ship.HyperspaceReturn();
						ship.isSpooling = false;

                        if (ship.shipData.Role != ShipData.RoleName.troop)
						{
							ship.GetAI().OrderResupplyNearest(false);
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
						    continue;

						toLaunch.Add(t);
					}

					foreach (Troop t in toLaunch)
					{
						Ship troopship = t.Launch();

						if (troopship == null)
						    continue;

						troopship.GetAI().OrderRebaseToNearest();
					}
					toLaunch.Clear();
				}
			}
			this.empire.GetGSAI().TaskList.QueuePendingRemoval(this);
		}

        private float GetEnemyStrAtTarget()
        {
            return GetEnemyStrAtTarget(1000);
        }

        private float GetEnemyStrAtTarget(float standardMinimum)
		{		                        
            float MinimumEscortStrength = 1000;
            if (this.TargetPlanet.Owner == null)
                return standardMinimum;

            this.TargetPlanet.Owner.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(this.TargetPlanet.ParentSystem, out SystemCommander scom);
            float importance = 1;

            if (scom != null)
                importance = 1 + (int)scom.RankImportance * .01f;

            float distance = 250000 * importance;            
            MinimumEscortStrength = this.empire.GetGSAI().ThreatMatrix.PingRadarStr(this.AO, distance,this.empire);
            standardMinimum *= importance;
            if (MinimumEscortStrength < standardMinimum)
                MinimumEscortStrength = standardMinimum;

            return MinimumEscortStrength;
		}

		private float GetEnemyTroopStr()
		{
            return this.TargetPlanet.GetGroundStrengthOther(this.empire);
		}

		public Planet GetTargetPlanet()
		{
			return this.TargetPlanet;
		}

		private void RequisitionAssaultForces()
        {
            if (this.IsToughNut)
            {
                this.DoToughNutRequisition();
                return;
            }

            int landingSpots = this.TargetPlanet.GetGroundLandingSpots();
            AO closestAO = empire.GetGSAI().AreasOfOperations.FindMin(ao => AO.SqDist(ao.Position));

            if (closestAO == null)
                return;

            if (this.TargetPlanet.Owner == null || !this.empire.ExistsRelation(TargetPlanet.Owner))
            {
                this.EndTask();
                return;
            }

            if (this.empire.GetRelations(this.TargetPlanet.Owner).Treaty_Peace)
            {
                this.empire.GetRelations(this.TargetPlanet.Owner).PreparingForWar = false;
                this.EndTask();
                return;
            }

            float EnemyTroopStrength = this.TargetPlanet.GetGroundStrengthOther(this.empire) ;

            if (EnemyTroopStrength < 100f)
                EnemyTroopStrength = 100f;
            
            List<Ship> PotentialAssaultShips = new List<Ship>();
            List<Troop> PotentialTroops = new List<Troop>();
            List<Ship> potentialCombatShips = new List<Ship>();
            List<Ship> PotentialBombers = new List<Ship>();
            List<Ship> PotentialUtilityShips = new List<Ship>();
            this.GetAvailableShips(closestAO, PotentialBombers, potentialCombatShips, PotentialAssaultShips, PotentialUtilityShips);
            List<Planet> shipyards = new List<Planet>();

            foreach (Planet planet1 in closestAO.GetPlanets())
            {
                if (!planet1.HasShipyard)
                    continue;

                shipyards.Add(planet1);
            }

            IOrderedEnumerable<Planet> planets = shipyards.OrderBy(p => p.ParentSystem.combatTimer <= -120)
                                                          .ThenBy(p => Vector2.Distance(p.Position, this.AO));

            if (planets.Count<Planet>() == 0)
                return;

            IOrderedEnumerable<Planet> sortedList =
                from planet in empire.GetPlanets()
                orderby empire.GetGSAI().DefensiveCoordinator.DefenseDict[planet.ParentSystem].RankImportance,
                Vector2.Distance(planet.Position, planets.First<Planet>().Position)
                select planet;

            foreach (Planet planet2 in sortedList)
            {
                if (PotentialTroops.Count > 30)
                    break;

                int extra = (int)empire.GetGSAI().DefensiveCoordinator.DefenseDict[planet2.ParentSystem].RankImportance;

                foreach (Troop t in planet2.TroopsHere)
                {
                    if (t.GetOwner() != this.empire)
                        continue;

                    t.SetPlanet(planet2);
                    extra--;

                    if(extra < 0)                    
                        PotentialTroops.Add(t);
                }
            }

            int troopCount = PotentialTroops.Count();
            float ourAvailableStrength = 0f;

            foreach (Ship ship in PotentialAssaultShips)
            {
                int hangars = 0;
                foreach(ShipModule hangar in  ship.GetHangars())
                {
                    if (hangar.IsTroopBay)
                        hangars++;
                }
                
                foreach (Troop t in ship.TroopList)
                {
                    ourAvailableStrength += (float)t.Strength;
                    troopCount++;
                    hangars--;
                    if (hangars <=0)
                        break;
                }
            }

            bool GoodToGo = false;
            foreach (Troop t in PotentialTroops)
            {
                ourAvailableStrength = ourAvailableStrength + (float)t.Strength;
            }

            float OurPresentStrength = 0f;
            foreach (Ship ship in this.TargetPlanet.system.ShipList)
            {
                if (ship.loyalty != this.empire)
                    continue;

                OurPresentStrength += ship.GetStrength();
            }

            float MinimumEscortStrength = this.GetEnemyStrAtTarget();

            // I'm unsure on ball-park figures for ship strengths. Given it used to build up to 1500, sticking flat +300 on seems a good start
            //updated. Now it will use 1/10th of the current military strength escort strength needed is under 1000
            //well thats too much. 1/10th can be huge. moved it into the getenemy strength logic with some adjustments. now it looks at the enemy empires importance of the planet. 
            //sort of cheating but as it would be much the same calculation as the attacking empire would use.... hrmm.
            // actually i think the raw importance value could be used to create an importance for that planet. interesting... that could be very useful in many areas. 

            this.MinimumTaskForceStrength = MinimumEscortStrength;
            BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();
            float tfstrength = 0f;

            foreach (Ship ship in potentialCombatShips)
            {
                if (tfstrength >= MinimumEscortStrength)
                    break;
  
                tfstrength += ship.GetStrength();
                elTaskForce.Add(ship);
            }

            foreach(Ship ship in PotentialUtilityShips)
            {
                if (tfstrength >= MinimumEscortStrength *1.5f)
                    break;

                tfstrength += ship.GetStrength();
                elTaskForce.Add(ship);
            }

            if (!this.empire.isFaction && this.empire.data.DiplomaticPersonality.Territorialism < 50 && tfstrength<MinimumEscortStrength)
            {
                if (!this.IsCoreFleetTask)
                foreach (KeyValuePair<SolarSystem, SystemCommander> entry in this.empire.GetGSAI().DefensiveCoordinator.DefenseDict
                    .OrderByDescending(system => system.Key.CombatInSystem)
                    .ThenByDescending(ship => (ship.Value.GetOurStrength() - ship.Value.IdealShipStrength) < 1000)
                    .ThenByDescending(system => Vector2.Distance(system.Key.Position, this.TargetPlanet.Position))

    )
                {
                    foreach (Ship ship in entry.Value.GetShipList())
                    {
                        if (ship.GetAI().BadGuysNear|| ship.fleet != null || tfstrength >= MinimumEscortStrength || ship.GetStrength() <= 0f
                            || ship.shipData.Role == ShipData.RoleName.troop || ship.hasAssaultTransporter || ship.HasTroopBay
                            ||  ship.Mothership != null
                            )
                        {
                            continue;
                        }
                        tfstrength = tfstrength + ship.GetStrength();
                        elTaskForce.Add(ship);
                        this.empire.GetGSAI().DefensiveCoordinator.remove(ship);
                    }
                }
            }

            if (ourAvailableStrength >= EnemyTroopStrength &&  landingSpots >8 && troopCount >=10   && tfstrength >= this.MinimumTaskForceStrength)
            {
                if (this.TargetPlanet.Owner == null || !this.empire.TryGetRelations(TargetPlanet.Owner, out Relationship rel))
                {
                    this.EndTask();
                    return;
                }

                if (this.empire.GetRelations(this.TargetPlanet.Owner).PreparingForWar)
                {
                    this.empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.empire.GetRelations(this.TargetPlanet.Owner).PreparingForWarType);
                }

                GoodToGo = true;
                Fleet newFleet = new Fleet()
                {
                    Owner = this.empire,
                    Name = "Invasion Fleet"
                };

                int FleetNum = FindFleetNumber();
                float ForceStrength = 0f;

                foreach (Ship ship in PotentialAssaultShips)
                {
                    if (ForceStrength > EnemyTroopStrength * 1.25f || landingSpots <= -5)
                        break;

                    newFleet.AddShip(ship);
                    ForceStrength += ship.PlanetAssaultStrength;
                    this.empire.GetGSAI().DefensiveCoordinator.remove(ship);
                    landingSpots -= ship.PlanetAssaultCount;
                }

                foreach (Troop t in PotentialTroops)
                {
                    if (ForceStrength > EnemyTroopStrength * 1.25f || landingSpots <= -5)
                        break;

                    if (t.GetPlanet() != null && t.GetPlanet().ParentSystem.combatTimer <= 0 && !t.GetPlanet().RecentCombat)
                    {
                        if (t.GetOwner() != null)
                        {
                            newFleet.AddShip(t.Launch());
                            ForceStrength += (float)t.Strength;
                            landingSpots--;
                        }
                    }
                }

                this.empire.GetFleetsDict()[FleetNum] = newFleet;
                this.empire.GetGSAI().UsedFleets.Add(FleetNum);
                this.WhichFleet = FleetNum;
                newFleet.Task = this;
                foreach (Ship ship in elTaskForce)
                {
                    newFleet.AddShip(ship);                            
                    ship.GetAI().OrderQueue.Clear();
                    ship.GetAI().State = AIState.AwaitingOrders;
                    closestAO.GetOffensiveForcePool().Remove(ship);
                    closestAO.GetWaitingShips().Remove(ship);
                    this.empire.GetGSAI().DefensiveCoordinator.remove(ship);
                }
                newFleet.AutoArrange();
                this.Step = 1;
            }
            else if (landingSpots < 10 && tfstrength >= this.MinimumTaskForceStrength && PotentialBombers.Count >0)
            {
                int bombs = 0;
                foreach (Ship ship in PotentialBombers)
                {
                    bombs += ship.BombBays.Count;

                    if (elTaskForce.Contains(ship))
                        continue;

                    elTaskForce.Add(ship);
                    if (bombs > 25 - landingSpots)
                        break;
                }

                if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.empire.ExistsRelation(TargetPlanet.Owner))
                {
                    this.EndTask();
                    return;
                }

                if (this.empire.GetRelations(this.TargetPlanet.Owner).PreparingForWar)
                    this.empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.empire.GetRelations(this.TargetPlanet.Owner).PreparingForWarType);

                GoodToGo = true;
                Fleet newFleet = new Fleet()
                {
                    Owner = this.empire,
                    Name = "Invasion Fleet"
                };

                int FleetNum = FindFleetNumber();
                float ForceStrength = 0f;

                foreach (Ship ship in PotentialAssaultShips)
                {
                    if (ForceStrength > EnemyTroopStrength || troopCount > 10)
                        break;

                    newFleet.AddShip(ship);
                    foreach (Troop t in ship.TroopList)
                    {
                        ForceStrength += (float)t.Strength;
                    }
                    troopCount++;
                }

                foreach (Troop t in PotentialTroops)
                {
                    if (ForceStrength > EnemyTroopStrength || troopCount > 10)
                        break;

                    if (t.GetPlanet() != null && t != null)
                    {
                        Ship launched = t.Launch();
                        ForceStrength += (float)t.Strength;
                        newFleet.AddShip(launched);
                        troopCount++;
                    }
                }

                this.empire.GetFleetsDict()[FleetNum] = newFleet;
                this.empire.GetGSAI().UsedFleets.Add(FleetNum);
                this.WhichFleet = FleetNum;
                newFleet.Task = this;

                foreach (Ship ship in elTaskForce)
                {
                    newFleet.AddShip(ship);
                    ship.GetAI().OrderQueue.Clear();
                    ship.GetAI().State = AIState.AwaitingOrders;
                    closestAO.GetOffensiveForcePool().Remove(ship);
                    closestAO.GetWaitingShips().Remove(ship);
                    this.empire.GetGSAI().DefensiveCoordinator.remove(ship);
                }

                newFleet.AutoArrange();
                this.Step = 1;
            }
            else if (tfstrength <= this.MinimumTaskForceStrength)
            {
                if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.empire.TryGetRelations(this.TargetPlanet.Owner, out Relationship rel2))
                {
                    this.EndTask();
                    return;
                }

                if (closestAO.GetCoreFleet().Task == null && closestAO.GetCoreFleet().GetStrength() > this.MinimumTaskForceStrength)
                {
                    MilitaryTask clearArea = new MilitaryTask(closestAO.GetCoreFleet().Owner)
                    {
                        AO = this.TargetPlanet.Position,
                        AORadius = 75000f,
                        type = MilitaryTask.TaskType.ClearAreaOfEnemies
                    };

                    closestAO.GetCoreFleet().Owner.GetGSAI().TasksToAdd.Add(clearArea);
                    clearArea.WhichFleet = closestAO.WhichFleet;
                    closestAO.GetCoreFleet().Task = clearArea;
                    clearArea.IsCoreFleetTask = true;
                    closestAO.GetCoreFleet().TaskStep = 1;
                    clearArea.Step = 1;

                    if (this.empire.GetRelations(this.TargetPlanet.Owner).PreparingForWar)
                        this.empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.empire.GetRelations(this.TargetPlanet.Owner).PreparingForWarType);
                }
            }
            else if ( landingSpots < 10) this.IsToughNut = true;

            if (!GoodToGo)
                this.NeededTroopStrength = (int)(EnemyTroopStrength  - ourAvailableStrength);
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
                return;

            AO ClosestAO = sorted.First<AO>();
            float tfstrength = 0f;
            BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();
            int shipCount = 0;
            float strengthNeeded = this.EnemyStrength;

            if (strengthNeeded == 0)
                strengthNeeded = this.empire.GetGSAI().ThreatMatrix.PingRadarStr(this.TargetPlanet.Position, 125000, this.empire);

            if (strengthNeeded < this.empire.currentMilitaryStrength * .02f)
                strengthNeeded = this.empire.currentMilitaryStrength * .02f;
            foreach (Ship ship in ClosestAO.GetOffensiveForcePool().OrderBy(str=>str.GetStrength()))
            {
                if (shipCount >= 3 && tfstrength >= strengthNeeded)
                    break;

                if (ship.GetStrength() <= 0f || ship.InCombat || ship.fleet != null)
                    continue;

                shipCount++;
                elTaskForce.Add(ship);
                tfstrength += ship.GetStrength();
            }

            if (shipCount < 3 && tfstrength < strengthNeeded)
                return;

            this.TaskForce = elTaskForce;
            this.StartingStrength = tfstrength;
            int FleetNum = FindFleetNumber();
            Fleet newFleet = new Fleet();
            foreach (Ship ship in this.TaskForce)
            {
                newFleet.AddShip(ship);
            }

            newFleet.Owner = this.empire;
            newFleet.Name = "Scout Fleet";
            newFleet.AutoArrange();
            this.empire.GetFleetsDict()[FleetNum] = newFleet;
            this.empire.GetGSAI().UsedFleets.Add(FleetNum);
            this.WhichFleet = FleetNum;
            newFleet.Task = this;

            foreach (Ship ship in this.TaskForce)
            {
                ClosestAO.GetOffensiveForcePool().Remove(ship);
                ClosestAO.GetWaitingShips().Remove(ship);
                this.empire.GetGSAI().DefensiveCoordinator.remove(ship);
            }
            this.Step = 1;
        }

		private void RequisitionDefenseForce()
		{
			float forcePoolStr = this.empire.GetForcePoolStrength();
			float tfstrength = 0f;
			BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();
            
			foreach (Ship ship in this.empire.GetForcePool().OrderBy(strength=> strength.GetStrength()))
			{
				if (ship.fleet != null)
                    continue;

				if (tfstrength >= forcePoolStr / 2f)
                    break;

				if (ship.GetStrength() <= 0f || ship.InCombat)
                    continue;

				elTaskForce.Add(ship);
				tfstrength = tfstrength + ship.GetStrength();
			}

			this.TaskForce = elTaskForce;
			this.StartingStrength = tfstrength;
            int FleetNum = FindFleetNumber();
            Fleet newFleet = new Fleet();

			foreach (Ship ship in this.TaskForce)
			{
				newFleet.AddShip(ship);
			}

			newFleet.Owner = this.empire;
			newFleet.Name = "Defensive Fleet";
			newFleet.AutoArrange();
			this.empire.GetFleetsDict()[FleetNum] = newFleet;
			this.empire.GetGSAI().UsedFleets.Add(FleetNum);
			this.WhichFleet = FleetNum;
			newFleet.Task = this;

            foreach (Ship ship in this.TaskForce)
            {
                this.empire.ForcePoolRemove(ship);
            }
			this.Step = 1;
		}

        //added by gremlin Req Exploration forces
        private void RequisitionExplorationForce()
        {
            AO closestAO = empire.GetGSAI().AreasOfOperations.FindMin(ao => AO.SqDist(ao.Position));

            IOrderedEnumerable<AO> sorted =
                from ao in this.empire.GetGSAI().AreasOfOperations
                orderby Vector2.Distance(this.AO, ao.Position)
                select ao;

            if (closestAO == null)
                return;

            this.EnemyStrength = 0f;
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.empire.GetGSAI().ThreatMatrix.Pins)
            {
                if (Vector2.Distance(this.AO, pin.Value.Position) >= this.AORadius || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == this.empire)
                    continue;

                this.EnemyStrength += pin.Value.Strength;
            }

            this.MinimumTaskForceStrength = this.EnemyStrength + 0.35f * this.EnemyStrength;

            if (this.MinimumTaskForceStrength == 0f)
                this.MinimumTaskForceStrength = closestAO.GetOffensiveForcePool().Sum(strength => strength.GetStrength()) *.2f;

            foreach (var entry in this.empire.AllRelations)
            {
                if (!entry.Value.AtWar || entry.Key.isFaction || this.MinimumTaskForceStrength <= closestAO.GetOffensiveForcePool().Sum(strength => strength.GetStrength())* .5f)
                    continue;

                this.EndTask();
                return;
            }

            List<Ship> PotentialAssaultShips = new List<Ship>();
            List<Troop> PotentialTroops = new List<Troop>();
            foreach (Ship ship in closestAO.GetOffensiveForcePool())
            {
                if (ship.fleet != null || (!ship.HasTroopBay && !ship.hasTransporter && ship.shipData.Role != ShipData.RoleName.troop) || ship.TroopList.Count == 0)    
                    continue;
                
                PotentialAssaultShips.Add(ship);
            }

            List<Planet> shipyards = new List<Planet>();
            foreach (Planet planet1 in closestAO.GetPlanets())
            {
                if (!planet1.CanBuildInfantry())
                    continue;
               
                shipyards.Add(planet1);
            }

            IOrderedEnumerable<Planet> planets =
                from p in shipyards
                orderby Vector2.Distance(p.Position, this.TargetPlanet.Position)
                select p;
            if (planets.Count<Planet>() != 0)
            {
                IOrderedEnumerable<Planet> sortedList =
                    from planet in closestAO.GetPlanets()
                    orderby Vector2.Distance(planet.Position, planets.First<Planet>().Position)
                    select planet;

                foreach (Planet planet2 in sortedList)
                {
                    foreach (Troop t in planet2.TroopsHere)
                    {
                        if (t.GetOwner() != this.empire)
                            continue;
                        
                        t.SetPlanet(planet2);
                        PotentialTroops.Add(t);
                    }
                }

                float ourAvailableStrength = 0f;
                foreach (Ship ship in PotentialAssaultShips)
                {
                    if (ship.fleet != null)
                        continue;
                    
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
                foreach (Ship ship in closestAO.GetOffensiveForcePool().OrderBy(strength=> strength.GetStrength()))
                {
                    if (ship.InCombat || ship.fleet != null || tfstrength >= this.MinimumTaskForceStrength + ship.GetStrength())
                        continue;
                    
                    tfstrength = tfstrength + ship.GetStrength();
                    elTaskForce.Add(ship);
                }

                if (tfstrength >= this.MinimumTaskForceStrength && ourAvailableStrength >= 20f)
                {
                    this.TaskForce = elTaskForce;
                    this.StartingStrength = tfstrength;
                    int FleetNum = FindFleetNumber();

                    Fleet newFleet = new Fleet();
                    float ForceStrength = 0f;

                    foreach (Ship ship in PotentialAssaultShips)
                    {
                        if (ForceStrength >= 20f)
                            break;

                        newFleet.AddShip(ship);

                        foreach (Troop t in ship.TroopList)
                        {
                            ForceStrength += (float)t.Strength;
                        }
                    }

                    foreach (Troop t in PotentialTroops)
                    {
                        if (ForceStrength >= 20f)
                            break;

                        if (t.GetPlanet() != null)
                        {
                            Ship launched = t.Launch();
                            ForceStrength += (float)t.Strength;
                            newFleet.AddShip(launched);
                        }
                    }

                    foreach (Ship ship in this.TaskForce)
                    {
                        ship.GetAI().OrderQueue.Clear();
                        ship.GetAI().State = AIState.AwaitingOrders;
                        newFleet.AddShip(ship);
                        closestAO.GetOffensiveForcePool().Remove(ship);
                        closestAO.GetWaitingShips().Remove(ship);
                    }

                    newFleet.Owner = this.empire;
                    newFleet.Name = "Exploration Force";
                    newFleet.AutoArrange();
                    this.empire.GetFleetsDict()[FleetNum] = newFleet;
                    this.empire.GetGSAI().UsedFleets.Add(FleetNum);
                    this.WhichFleet = FleetNum;
                    newFleet.Task = this;
                    this.Step = 1;
                }
                return;
            }
            this.EndTask();
        }

		private void RequisitionForces()
		{
            IOrderedEnumerable<Ship_Game.Gameplay.AO> sorted = this.empire.GetGSAI().AreasOfOperations
                .OrderByDescending(ao => ao.GetOffensiveForcePool().Sum(strength => strength.GetStrength()) >= this.MinimumTaskForceStrength)
                .ThenBy(ao => Vector2.Distance(this.AO, ao.Position));

			if (sorted.Count<Ship_Game.Gameplay.AO>() == 0)
                return;

			Ship_Game.Gameplay.AO ClosestAO = sorted.First<Ship_Game.Gameplay.AO>();
            this.EnemyStrength = this.empire.GetGSAI().ThreatMatrix.PingRadarStr(this.AO, this.AORadius,this.empire);

            this.MinimumTaskForceStrength = this.EnemyStrength;
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MilitaryTask() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.TaskForce != null)
                        this.TaskForce.Dispose();

                }
                this.TaskForce = null;
                this.disposed = true;
            }
        }

        private int FindFleetNumber()
        {
            for (int i = 1; i < 10; i++)
            {
                if (this.empire.GetGSAI().UsedFleets.Contains(i))
                    continue;

                return i;
            }
            return -1;
        }
	}
}