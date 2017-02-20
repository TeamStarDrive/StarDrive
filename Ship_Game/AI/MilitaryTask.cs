using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Debug;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    public class MilitaryTask : IDisposable
    {
        [Serialize(0)] public bool IsCoreFleetTask;
        [Serialize(1)] public bool WaitForCommand;
        [Serialize(2)] public Array<Guid> HeldGoals = new Array<Guid>();
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

        [XmlIgnore] [JsonIgnore] private Planet TargetPlanet;
        [XmlIgnore] [JsonIgnore] private Empire Empire;
        [XmlIgnore] [JsonIgnore] private BatchRemovalCollection<Ship> TaskForce = new BatchRemovalCollection<Ship>();
        [XmlIgnore] [JsonIgnore] private Fleet Fleet => Empire.GetFleetsDict()[WhichFleet];


        //This file Refactored by Gretman

        public MilitaryTask()
		{
		}
        public MilitaryTask(AO ao)
        {
            AO = ao.Position;
            AORadius = ao.Radius;
            type = TaskType.CohesiveClearAreaOfEnemies;
            WhichFleet = ao.WhichFleet;
            IsCoreFleetTask = true;
            SetEmpire(ao.GetCoreFleet().Owner);
        }
        public MilitaryTask(Vector2 location, float radius, Array<Goal> GoalsToHold, Empire Owner)
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
			this.Empire = Owner;
		}

		public MilitaryTask(Planet target, Empire Owner)
		{
			this.type = MilitaryTask.TaskType.AssaultPlanet;
			this.TargetPlanet = target;
			this.TargetPlanetGuid = target.guid;
			this.AO = target.Position;
			this.AORadius = 35000f;
			this.Empire = Owner;
		}

		public MilitaryTask(Empire Owner)
		{
			this.Empire = Owner;
		}

        private void GetAvailableShips(AO area, Array<Ship> Bombers, Array<Ship> Combat, Array<Ship> TroopShips, Array<Ship> Utility)
        {
            foreach (Ship ship in this.Empire.GetShips().OrderBy(str => str.BaseStrength).ThenBy(ship => Vector2.Distance(ship.Center, area.Position) >= area.Radius))
            {
                if ((ship.shipData.Role == ShipData.RoleName.station || ship.shipData.Role == ShipData.RoleName.platform)
                    || !ship.BaseCanWarp
                    || ship.InCombat
                    || ship.fleet != null
                    || ship.Mothership != null
                    || this.Empire.GetGSAI().DefensiveCoordinator.DefensiveForcePool.Contains(ship)
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
                from ao in this.Empire.GetGSAI().AreasOfOperations
                where ao.GetCoreFleet().Task == null || ao.GetCoreFleet().Task.type != TaskType.AssaultPlanet
                orderby ao.GetOffensiveForcePool().Where(combat=> !combat.InCombat).Sum(strength => strength.BaseStrength) >= this.MinimumTaskForceStrength descending
                orderby Vector2.Distance(this.AO, ao.Position)
                select ao;

            if (sorted.Count<AO>() == 0)
                return;

            Array<Ship> Bombers = new Array<Ship>();
            Array<Ship> EverythingElse = new Array<Ship>();
            Array<Ship> TroopShips = new Array<Ship>();
            Array<Troop> Troops = new Array<Troop>();
            
            foreach (AO area in sorted)
            {
                this.GetAvailableShips(area, Bombers, EverythingElse, TroopShips, EverythingElse);
                foreach (Planet p in area.GetPlanets())
                {
                    if (p.RecentCombat || p.ParentSystem.combatTimer>0)
                        continue;

                    foreach (Troop t in p.TroopsHere)
                    {
                        if (t.GetOwner() != this.Empire)
                            continue;

                        Troops.Add(t);
                    }
                }
            }

            EverythingElse.AddRange(TroopShips);
            Array<Ship> TaskForce = new Array<Ship>();
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

            Array<Ship> BombTaskForce = new Array<Ship>();
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

            Array<Troop> PotentialTroops = new Array<Troop>();
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
                if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.Empire.TryGetRelations(this.TargetPlanet.Owner, out Relationship rel))
                {
                    this.EndTask();
                    return;
                }

                if (this.Empire.GetRelations(this.TargetPlanet.Owner).PreparingForWar)
                {
                    this.Empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.Empire.GetRelations(this.TargetPlanet.Owner).PreparingForWarType);
                }

                AO ClosestAO = sorted.First<AO>();
                MilitaryTask assault = new MilitaryTask(this.Empire)
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
                    
                    ship.fleet?.RemoveShip(ship);

                    ship.GetAI().OrderQueue.Clear();
                    this.Empire.GetGSAI().DefensiveCoordinator.Remove(ship);
                    

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
                    MilitaryTask GlassPlanet = new MilitaryTask(this.Empire)
                    {
                        AO = this.TargetPlanet.Position,
                        AORadius = 75000f,
                        type = MilitaryTask.TaskType.GlassPlanet,
                        TargetPlanet = this.TargetPlanet,
                        WaitForCommand = true
                    };
                    
                    Fleet bomberFleet = new Fleet()
                    {
                        Owner = this.Empire
                    };

                    bomberFleet.Owner.GetGSAI().TasksToAdd.Add(GlassPlanet);
                    GlassPlanet.WhichFleet = this.Empire.GetUnusedKeyForFleet();
                    this.Empire.GetFleetsDict().Add(GlassPlanet.WhichFleet, bomberFleet);
                    bomberFleet.Task = GlassPlanet;
                    bomberFleet.Name = "Bomber Fleet";

                    foreach (Ship ship in BombTaskForce)
                    {
                        ship.GetAI().OrderQueue.Clear();
                        this.Empire.GetGSAI().DefensiveCoordinator.Remove(ship);
                        ship.fleet?.RemoveShip(ship);

                        bomberFleet.AddShip(ship);
                    }
                    bomberFleet.AutoArrange();
                }
                this.Step = 1;
                this.Empire.GetGSAI().TaskList.QueuePendingRemoval(this);
            }
        }

        public void EndTask()
		{
            DebugInfoScreen.CanceledMtasksCount++;

            switch (this.type)
            {
                case TaskType.Exploration:
                    {
                        DebugInfoScreen.CanceledMtask1Count++;
                        DebugInfoScreen.CanceledMTask1Name = TaskType.Exploration.ToString();
                        break;
                    }
                case TaskType.AssaultPlanet:
                    {
                        DebugInfoScreen.CanceledMtask2Count++;
                        DebugInfoScreen.CanceledMTask2Name = TaskType.AssaultPlanet.ToString();
                        break;
                    }
                case TaskType.CohesiveClearAreaOfEnemies:
                    {
                        DebugInfoScreen.CanceledMtask3Count++;
                        DebugInfoScreen.CanceledMTask3Name = TaskType.CohesiveClearAreaOfEnemies.ToString();
                        break;
                    }
                    default:
                    {
                        DebugInfoScreen.CanceledMtask4Count++;
                        DebugInfoScreen.CanceledMTask4Name = this.type.ToString();
                        break;
                    }
            }

            if (this.Empire.isFaction)
			{
				this.FactionEndTask();
				return;
			}

			foreach (Guid goalGuid in this.HeldGoals)
			{
				foreach (Goal g in this.Empire.GetGSAI().Goals)
				{
					if (g.guid != goalGuid)
						continue;

					g.Held = false;
				}
			}
            
            AO closestAO = Empire.GetGSAI().AreasOfOperations.FindMin(ao => AO.SqDist(ao.Position));
            
            if (closestAO == null)
            {
                if (!this.IsCoreFleetTask && this.WhichFleet != -1 && this.Empire != Ship.universeScreen.player)
                {
                    foreach (Ship ship in this.Empire.GetFleetsDict()[this.WhichFleet].Ships)
                    {
                        this.Empire.ForcePoolAdd(ship);
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
					if (!this.Empire.GetFleetsDict().ContainsKey(this.WhichFleet))
					{ //what the hell is this for? dictionary doesnt contain the key the foreach below would blow up. 
                        if (!this.IsCoreFleetTask && this.Empire != Ship.universeScreen.player)
                        {
                            foreach (Ship ship in this.Empire.GetFleetsDict()[this.WhichFleet].Ships)
                            {
                                this.Empire.ForcePoolAdd(ship);
                            }
                        }
                        return;
					}

				    for (int index = 0; index < Fleet.Ships.Count; index++)
				    {
				        Ship ship = Fleet.Ships[index];
				        ship.GetAI().OrderQueue.Clear();
				        ship.GetAI().State = AIState.AwaitingOrders;
				        Fleet.RemoveShip(ship);
				        ship.HyperspaceReturn();
				        ship.isSpooling = false;
				        if (ship.shipData.Role != ShipData.RoleName.troop)
				        {
				            closestAO.AddShip(ship);
				            ship.GetAI().OrderResupplyNearest(false);
				        }
				        else
				        {
				            ship.GetAI().OrderRebaseToNearest();
				        }
				    }
				    this.TaskForce.Clear();
					this.Empire.GetGSAI().UsedFleets.Remove(this.WhichFleet);
					Fleet.Reset();
				}

				if (this.type == TaskType.Exploration)
				{
					Array<Troop> toLaunch = new Array<Troop>();
					foreach (Troop t in this.TargetPlanet.TroopsHere)
					{
						if (   t.GetOwner() != this.Empire
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
			this.Empire.GetGSAI().TaskList.QueuePendingRemoval(this);
		}

        public void EndTaskWithMove()
		{
			foreach (Guid goalGuid in this.HeldGoals)
			{
				foreach (Goal g in this.Empire.GetGSAI().Goals)
				{
					if (g.guid != goalGuid)
						continue;

					g.Held = false;
				}
			}

            AO closestAO = Empire.GetGSAI().AreasOfOperations.FindMin(ao => AO.SqDist(ao.Position));
			if (closestAO == null)
			{
                if (  !this.IsCoreFleetTask && this.WhichFleet != -1 && this.Empire != EmpireManager.Player)
                {
                    foreach (Ship ship in this.Empire.GetFleetsDict()[this.WhichFleet].Ships)
                    {
                        this.Empire.ForcePoolAdd(ship);
                    }
                }
                return;
			}

			if (this.WhichFleet != -1)
			{
				if (this.IsCoreFleetTask)
				{
					this.Empire.GetFleetsDict()[this.WhichFleet].Task = null;
					this.Empire.GetFleetsDict()[this.WhichFleet].MoveToDirectly(closestAO.Position, 0f, new Vector2(0f, -1f));
				}
				else
				{
					foreach (Ship ship in this.Empire.GetFleetsDict()[this.WhichFleet].Ships)
					{
                        Empire.GetFleetsDict()[WhichFleet].RemoveShip(ship);
                        closestAO.AddShip(ship);
                        closestAO.TurnsToRelax = 0;
					}

					this.TaskForce.Clear();
					this.Empire.GetGSAI().UsedFleets.Remove(this.WhichFleet);
					this.Empire.GetFleetsDict()[this.WhichFleet].Reset();
				}
			}
			this.Empire.GetGSAI().TaskList.QueuePendingRemoval(this);
		}

        public void Evaluate(Empire e)
        {
            this.Empire = e;
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
                            if (this.Empire.GetFleetsDict().TryGetValue(this.WhichFleet, out Fleet fleet))
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

                        this.Empire.GetFleetsDict()[1].Reset();
                        foreach (Ship shiptoadd in (Array<Ship>)this.Empire.GetShips())
                        {
                            if (shiptoadd.shipData.Role != ShipData.RoleName.platform)
                                this.Empire.GetFleetsDict()[1].AddShip(shiptoadd);
                        }

                        if (this.Empire.GetFleetsDict()[1].Ships.Count <= 0)
                            break;

                        this.Empire.GetFleetsDict()[1].Name = "Corsair Raiders";
                        this.Empire.GetFleetsDict()[1].AutoArrange();
                        this.Empire.GetFleetsDict()[1].Task = this;
                        this.WhichFleet = 1;
                        this.Step = 1;
                        this.Empire.GetFleetsDict()[1].FormationWarpTo(this.TargetPlanet.Position, 0.0f, Vector2.Zero);
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
                            if (this.Empire.GetFleetsDict().ContainsKey(this.WhichFleet))
                            {
                                if (this.Empire.GetFleetsDict()[this.WhichFleet].Ships.Count != 0)
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
                                        Empire.TryGetRelations(TargetPlanet.Owner, out Relationship rel);

                                        if (rel != null && (!rel.AtWar && !rel.PreparingForWar))
                                            this.EndTask();
                                    }
                                    this.RequisitionClaimForce();
                                    return;
                                }

                            case 1:
                                {
                                    if (this.Empire.GetFleetsDict().ContainsKey(this.WhichFleet))
                                    {
                                        if (this.Empire.GetFleetsDict()[this.WhichFleet].Ships.Count == 0)
                                        {
                                            this.EndTask();
                                            return;
                                        }

                                        if (this.TargetPlanet.Owner != null) // &&(this.empire.GetFleetsDict().ContainsKey(this.WhichFleet)))
                                        {
                                        this.Empire.TryGetRelations(TargetPlanet.Owner, out Relationship rel);
                                            if (rel != null && (rel.AtWar || rel.PreparingForWar))
                                            {
                                                if (Vector2.Distance(this.Empire.GetFleetsDict()[this.WhichFleet].findAveragePosition(), this.TargetPlanet.Position) < this.AORadius)
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
                                    if (this.Empire.GetFleetsDict().ContainsKey(this.WhichFleet))
                                    {
                                        if (this.Empire.GetFleetsDict()[this.WhichFleet].Ships.Count == 0)
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

                                    Empire.TryGetRelations(TargetPlanet.Owner, out Relationship rel);
                                    if (rel != null && !(rel.AtWar || rel.PreparingForWar))
                                        this.EndTask();

                                    if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner == this.Empire)
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
                float groundstrength = this.GetTargetPlanet().GetGroundStrengthOther(this.Empire);
                float ourGroundStrength = this.GetTargetPlanet().GetGroundStrength(this.Empire);

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
            
			if (this.Empire.GetFleetsDict()[this.WhichFleet].Task == null )
			{
				this.EndTask();
				return;
			}
            
			float currentStrength = 0f;
			foreach (Ship ship in this.Empire.GetFleetsDict()[this.WhichFleet].Ships)
			{
				if (!ship.Active || ship.InCombat && this.Step < 1 || ship.GetAI().State == AIState.Scrap)
				{
					this.Empire.GetFleetsDict()[this.WhichFleet].Ships.QueuePendingRemoval(ship);
                    if (ship.Active && ship.GetAI().State != AIState.Scrap)
                    {
                        if (ship.fleet != null)
                            this.Empire.GetFleetsDict()[this.WhichFleet].Ships.QueuePendingRemoval(ship);
                        
                        this.Empire.ForcePoolAdd(ship);
                    }
                    else if (ship.GetAI().State == AIState.Scrap)
                    {
                        if (ship.fleet != null)
                            this.Empire.GetFleetsDict()[this.WhichFleet].Ships.QueuePendingRemoval(ship);
                    }
				}
				else
				{
					currentStrength += ship.GetStrength();
				}
			}

			this.Empire.GetFleetsDict()[this.WhichFleet].Ships.ApplyPendingRemovals();
			float currentEnemyStrength = 0f;

			foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Empire.GetGSAI().ThreatMatrix.Pins)
			{
				if (Vector2.Distance(this.AO, pin.Value.Position) >= this.AORadius || pin.Value.Ship == null)
					continue;

				Empire pinEmp = EmpireManager.GetEmpireByName(pin.Value.EmpireName);

				if (pinEmp == this.Empire || !pinEmp.isFaction && !this.Empire.GetRelations(pinEmp).AtWar )
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
					if (!this.Empire.GetFleetsDict().ContainsKey(this.WhichFleet))
					    return;

					foreach (Ship ship in this.Empire.GetFleetsDict()[this.WhichFleet].Ships)
					{
						ship.GetAI().OrderQueue.Clear();
						ship.GetAI().State = AIState.AwaitingOrders;
                        Empire.GetFleetsDict()[WhichFleet].RemoveShip(ship);
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
					this.Empire.GetGSAI().UsedFleets.Remove(this.WhichFleet);
					this.Empire.GetFleetsDict()[this.WhichFleet].Reset();
				}

				if (this.type == MilitaryTask.TaskType.Exploration)
				{
					Array<Troop> toLaunch = new Array<Troop>();
					foreach (Troop t in this.TargetPlanet.TroopsHere)
					{
						if (t.GetOwner() != this.Empire)
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
			this.Empire.GetGSAI().TaskList.QueuePendingRemoval(this);
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
                importance = 1 + scom.RankImportance * .01f;

            float distance = 250000 * importance;            
            MinimumEscortStrength = this.Empire.GetGSAI().ThreatMatrix.PingRadarStr(this.AO, distance,this.Empire);
            standardMinimum *= importance;
            if (MinimumEscortStrength < standardMinimum)
                MinimumEscortStrength = standardMinimum;

            return MinimumEscortStrength;
		}

		private float GetEnemyTroopStr()
		{
            return this.TargetPlanet.GetGroundStrengthOther(this.Empire);
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
            AO closestAO = Empire.GetGSAI().AreasOfOperations.FindMin(ao => AO.SqDist(ao.Position));

            if (closestAO == null)
                return;

            if (this.TargetPlanet.Owner == null || !this.Empire.ExistsRelation(TargetPlanet.Owner))
            {
                this.EndTask();
                return;
            }

            if (this.Empire.GetRelations(this.TargetPlanet.Owner).Treaty_Peace)
            {
                this.Empire.GetRelations(this.TargetPlanet.Owner).PreparingForWar = false;
                this.EndTask();
                return;
            }

            float EnemyTroopStrength = this.TargetPlanet.GetGroundStrengthOther(this.Empire) ;

            if (EnemyTroopStrength < 100f)
                EnemyTroopStrength = 100f;
            
            Array<Ship> PotentialAssaultShips = new Array<Ship>();
            Array<Troop> PotentialTroops = new Array<Troop>();
            Array<Ship> potentialCombatShips = new Array<Ship>();
            Array<Ship> PotentialBombers = new Array<Ship>();
            Array<Ship> PotentialUtilityShips = new Array<Ship>();
            this.GetAvailableShips(closestAO, PotentialBombers, potentialCombatShips, PotentialAssaultShips, PotentialUtilityShips);
            Array<Planet> shipyards = new Array<Planet>();

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
                from planet in Empire.GetPlanets()
                orderby Empire.GetGSAI().DefensiveCoordinator.DefenseDict[planet.ParentSystem].RankImportance,
                Vector2.Distance(planet.Position, planets.First<Planet>().Position)
                select planet;

            foreach (Planet planet2 in sortedList)
            {
                if (PotentialTroops.Count > 30)
                    break;

                int extra = (int)Empire.GetGSAI().DefensiveCoordinator.DefenseDict[planet2.ParentSystem].RankImportance;

                foreach (Troop t in planet2.TroopsHere)
                {
                    if (t.GetOwner() != this.Empire)
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
                if (ship.loyalty != this.Empire)
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

            if (!this.Empire.isFaction && this.Empire.data.DiplomaticPersonality.Territorialism < 50 && tfstrength < MinimumEscortStrength)
            {
                if (!this.IsCoreFleetTask)
                    foreach (var kv in this.Empire.GetGSAI().DefensiveCoordinator.DefenseDict
                        .OrderByDescending(system => system.Key.CombatInSystem ? 1:2 * system.Key.Position.SqDist(TargetPlanet.Position))
                        .ThenByDescending(ship => (ship.Value. GetOurStrength() - ship.Value.IdealShipStrength) < 1000)
                        

        )
                    {
                        var ships = kv.Value.GetShipList;

                        for (int index = 0; index < ships.Length; index++)
                        {
                            Ship ship = ships[index];
                            if (ship.GetAI().BadGuysNear || ship.fleet != null || tfstrength >= MinimumEscortStrength ||
                                ship.GetStrength() <= 0f
                                || ship.shipData.Role == ShipData.RoleName.troop || ship.hasAssaultTransporter ||
                                ship.HasTroopBay
                                || ship.Mothership != null
                            )
                                continue;

                            tfstrength = tfstrength + ship.GetStrength();
                            elTaskForce.Add(ship);
                            this.Empire.GetGSAI().DefensiveCoordinator.Remove(ship);
                        }
                    }
            }

            if (ourAvailableStrength >= EnemyTroopStrength &&  landingSpots >8 
                && troopCount >=10 && tfstrength >= this.MinimumTaskForceStrength)
            {
                if (this.TargetPlanet.Owner == null || !this.Empire.TryGetRelations(TargetPlanet.Owner, out Relationship rel))
                {
                    this.EndTask();
                    return;
                }

                if (this.Empire.GetRelations(this.TargetPlanet.Owner).PreparingForWar)
                {
                    this.Empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.Empire.GetRelations(this.TargetPlanet.Owner).PreparingForWarType);
                }

                GoodToGo = true;
                Fleet newFleet = new Fleet()
                {
                    Owner = this.Empire,
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
                    this.Empire.GetGSAI().DefensiveCoordinator.Remove(ship);
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

                this.Empire.GetFleetsDict()[FleetNum] = newFleet;
                this.Empire.GetGSAI().UsedFleets.Add(FleetNum);
                this.WhichFleet = FleetNum;
                newFleet.Task = this;
                foreach (Ship ship in elTaskForce)
                {
                    newFleet.AddShip(ship);                            
                    ship.GetAI().OrderQueue.Clear();
                    ship.GetAI().State = AIState.AwaitingOrders;
                    closestAO.RemoveShip(ship);
                    if(ship.GetAI().SystemToDefend != null)                    
                    this.Empire.GetGSAI().DefensiveCoordinator.Remove(ship);
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

                if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.Empire.ExistsRelation(TargetPlanet.Owner))
                {
                    this.EndTask();
                    return;
                }

                if (this.Empire.GetRelations(this.TargetPlanet.Owner).PreparingForWar)
                    this.Empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.Empire.GetRelations(this.TargetPlanet.Owner).PreparingForWarType);

                GoodToGo = true;
                Fleet newFleet = new Fleet()
                {
                    Owner = this.Empire,
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

                this.Empire.GetFleetsDict()[FleetNum] = newFleet;
                this.Empire.GetGSAI().UsedFleets.Add(FleetNum);
                this.WhichFleet = FleetNum;
                newFleet.Task = this;

                foreach (Ship ship in elTaskForce)
                {
                    newFleet.AddShip(ship);
                    ship.GetAI().OrderQueue.Clear();
                    ship.GetAI().State = AIState.AwaitingOrders;
                    closestAO.RemoveShip(ship);
                    if(ship.GetAI().SystemToDefend != null)
                        Empire.GetGSAI().DefensiveCoordinator.Remove(ship);
                }

                newFleet.AutoArrange();
                this.Step = 1;
            }
            else if (tfstrength <= this.MinimumTaskForceStrength)
            {
                if (this.TargetPlanet.Owner == null || this.TargetPlanet.Owner != null && !this.Empire.TryGetRelations(this.TargetPlanet.Owner, out Relationship rel2))
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

                    if (this.Empire.GetRelations(this.TargetPlanet.Owner).PreparingForWar)
                        this.Empire.GetGSAI().DeclareWarOn(this.TargetPlanet.Owner, this.Empire.GetRelations(this.TargetPlanet.Owner).PreparingForWarType);
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
                from ao in this.Empire.GetGSAI().AreasOfOperations
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
                strengthNeeded = this.Empire.GetGSAI().ThreatMatrix.PingRadarStr(this.TargetPlanet.Position, 125000, this.Empire);

            if (strengthNeeded < this.Empire.currentMilitaryStrength * .02f)
                strengthNeeded = this.Empire.currentMilitaryStrength * .02f;
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

            newFleet.Owner = this.Empire;
            newFleet.Name = "Scout Fleet";
            newFleet.AutoArrange();
            this.Empire.GetFleetsDict()[FleetNum] = newFleet;
            this.Empire.GetGSAI().UsedFleets.Add(FleetNum);
            this.WhichFleet = FleetNum;
            newFleet.Task = this;

            foreach (Ship ship in this.TaskForce)
            {
                ClosestAO.RemoveShip(ship);               
                this.Empire.GetGSAI().DefensiveCoordinator.Remove(ship);
            }
            this.Step = 1;
        }

		private void RequisitionDefenseForce()
		{
			float forcePoolStr = this.Empire.GetForcePoolStrength();
			float tfstrength = 0f;
			BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();
            
			foreach (Ship ship in this.Empire.GetForcePool().OrderBy(strength=> strength.GetStrength()))
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

			newFleet.Owner = this.Empire;
			newFleet.Name = "Defensive Fleet";
			newFleet.AutoArrange();
			this.Empire.GetFleetsDict()[FleetNum] = newFleet;
			this.Empire.GetGSAI().UsedFleets.Add(FleetNum);
			this.WhichFleet = FleetNum;
			newFleet.Task = this;

            foreach (Ship ship in this.TaskForce)
            {
                this.Empire.ForcePoolRemove(ship);
            }
			this.Step = 1;
		}

        //added by gremlin Req Exploration forces
        private void RequisitionExplorationForce()
        {
            AO closestAO = Empire.GetGSAI().AreasOfOperations.FindMin(ao => AO.SqDist(ao.Position));

            IOrderedEnumerable<AO> sorted =
                from ao in this.Empire.GetGSAI().AreasOfOperations
                orderby Vector2.Distance(this.AO, ao.Position)
                select ao;

            if (closestAO == null)
                return;

            this.EnemyStrength = 0f;
            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in this.Empire.GetGSAI().ThreatMatrix.Pins)
            {
                if (Vector2.Distance(this.AO, pin.Value.Position) >= this.AORadius || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == this.Empire)
                    continue;

                this.EnemyStrength += pin.Value.Strength;
            }

            this.MinimumTaskForceStrength = this.EnemyStrength + 0.35f * this.EnemyStrength;

            if (this.MinimumTaskForceStrength == 0f)
                this.MinimumTaskForceStrength = closestAO.GetOffensiveForcePool().Sum(strength => strength.GetStrength()) *.2f;

            foreach (var entry in this.Empire.AllRelations)
            {
                if (!entry.Value.AtWar || entry.Key.isFaction || this.MinimumTaskForceStrength <= closestAO.GetOffensiveForcePool().Sum(strength => strength.GetStrength())* .5f)
                    continue;

                this.EndTask();
                return;
            }

            Array<Ship> PotentialAssaultShips = new Array<Ship>();
            Array<Troop> PotentialTroops = new Array<Troop>();
            foreach (Ship ship in closestAO.GetOffensiveForcePool())
            {
                if (ship.fleet != null || (!ship.HasTroopBay && !ship.hasTransporter && ship.shipData.Role != ShipData.RoleName.troop) || ship.TroopList.Count == 0)    
                    continue;
                
                PotentialAssaultShips.Add(ship);
            }

            Array<Planet> shipyards = new Array<Planet>();
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
                        if (t.GetOwner() != this.Empire)
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
                        closestAO.RemoveShip(ship);
                    }

                    newFleet.Owner = this.Empire;
                    newFleet.Name = "Exploration Force";
                    newFleet.AutoArrange();
                    this.Empire.GetFleetsDict()[FleetNum] = newFleet;
                    this.Empire.GetGSAI().UsedFleets.Add(FleetNum);
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
            IOrderedEnumerable<AO> sorted = this.Empire.GetGSAI().AreasOfOperations
                .OrderByDescending(ao => ao.GetOffensiveForcePool().Sum(strength => strength.GetStrength()) >= this.MinimumTaskForceStrength)
                .ThenBy(ao => Vector2.Distance(this.AO, ao.Position));

			if (sorted.Count<AO>() == 0)
                return;

			AO ClosestAO = sorted.First<AO>();
            this.EnemyStrength = this.Empire.GetGSAI().ThreatMatrix.PingRadarStr(this.AO, this.AORadius,this.Empire);

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
			this.Empire = e;
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
            TaskForce?.Dispose(ref TaskForce);
        }

        private int FindFleetNumber()
        {
            for (int i = 1; i < 10; i++)
            {
                if (this.Empire.GetGSAI().UsedFleets.Contains(i))
                    continue;

                return i;
            }
            return -1;
        }
	}
}