using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI.Tasks
{
    public partial class MilitaryTask : IDisposable
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
        [Serialize(16)] public int Priority;

        [XmlIgnore] [JsonIgnore] public Planet TargetPlanet { get; private set; }
        [XmlIgnore] [JsonIgnore] private Empire Owner;
        [XmlIgnore] [JsonIgnore] private Array<Ship> TaskForce = new Array<Ship>();
        [XmlIgnore] [JsonIgnore] private Fleet Fleet => Owner.GetFleet(WhichFleet);        
        //This file Refactored by Gretman

        public MilitaryTask()
        {
        }
        public static MilitaryTask CreatePostInvasion(Vector2 ao, int fleetID, Empire owner)
        {
            var militaryTask = new MilitaryTask
            {
                AO = ao,
                AORadius = 10000f,
                WhichFleet = fleetID
            };
            militaryTask.SetEmpire(owner);
            militaryTask.type = TaskType.DefendPostInvasion;
            return militaryTask;
        }
        public MilitaryTask(AO ao)
        {
            AO = ao.Center;
            AORadius = ao.Radius;
            type = TaskType.CohesiveClearAreaOfEnemies;
            WhichFleet = ao.WhichFleet;
            IsCoreFleetTask = true;
            SetEmpire(ao.GetCoreFleet().Owner);
        }

        public MilitaryTask(Vector2 location, float radius, Array<Goal> GoalsToHold, Empire Owner, float str = 0) 
        {
            type = TaskType.ClearAreaOfEnemies;
            AO = location;
            AORadius = radius;
            InitialEnemyStrength = str;
            foreach (Goal g in GoalsToHold)
            {
                g.Held = true;
                HeldGoals.Add(g.guid);
            }

            EnemyStrength = Owner.currentMilitaryStrength * .001f;
            if (InitialEnemyStrength < 1)
                InitialEnemyStrength = Owner.GetGSAI().ThreatMatrix.PingRadarStr(location, radius, Owner); ;

            MinimumTaskForceStrength = EnemyStrength;
            this.Owner = Owner;
        }         

        public MilitaryTask(Planet target, Empire Owner)
        {
            type = TaskType.AssaultPlanet;
            TargetPlanet = target;
            TargetPlanetGuid = target.guid;
            AO = target.Center;
            AORadius = 35000f;
            this.Owner = Owner;
            MinimumTaskForceStrength = Owner.currentMilitaryStrength *.05f;
        }

        public MilitaryTask(Empire Owner)
        {
            this.Owner = Owner;
        }

        private bool DeclareWar()
        {
            if (Owner.GetRelations(TargetPlanet.Owner).PreparingForWar)
            {
                Owner.GetGSAI().DeclareWarOn(TargetPlanet.Owner,
                    Owner.GetRelations(TargetPlanet.Owner).PreparingForWarType);
                return true;
            }
            return false;
        }

        public void EndTask()
        {
            DebugInfoScreen.CanceledMtasksCount++;
            Owner.GetGSAI().TaskList.QueuePendingRemoval(this);
            switch (type)
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
                    DebugInfoScreen.CanceledMTask4Name = type.ToString();
                    break;
                }
            }

            if (Owner.isFaction)
            {
                FactionEndTask();
                return;
            }

            foreach (Guid goalGuid in HeldGoals)
            {
                foreach (Goal g in Owner.GetGSAI().Goals)
                {
                    if (g.guid != goalGuid)
                        continue;

                    g.Held = false;
                }
            }

            AO closestAO = FindClosestAO();


            if (closestAO == null)
            {
                //something wrong here in the logic flow as sometimes the fleet is null. 
                if (WhichFleet == -1 || (Fleet?.IsCoreFleet ?? true) || Owner == Empire.Universe.player) return;
                Fleet fleet = Owner.GetFleet(WhichFleet);
                if (fleet == null) return;
                foreach (Ship ship in fleet.Ships)
                {
                    Fleet.RemoveShip(ship);
                    if (ship?.Active ?? false)
                        Owner.ForcePoolAdd(ship);
                }

                return;
            }

            if (WhichFleet == -1 || Fleet == null) return;
            if (IsCoreFleetTask)
            {
                foreach (Ship ship in Fleet.Ships)
                {
                    ship.AI.CombatState = ship.shipData.CombatState;
                }

                Fleet.FleetTask = null;
                return;
            }

            TaskForce.Clear();
            if (Fleet.IsCoreFleet || Owner.isPlayer)
                return;

            if (Fleet == null)
                return;

            for (int index = Fleet.Ships.Count - 1; index >= 0; index--)
            {
                Ship ship = Fleet.Ships[index];
                ship.AI.OrderQueue.Clear();
                ship.AI.State = AIState.AwaitingOrders;
                ship.AI.CombatState = ship.shipData.CombatState;
                Fleet.RemoveShip(ship);
                ship.HyperspaceReturn();
                ship.isSpooling = false;
                if (ship.shipData.Role == ShipData.RoleName.troop)
                    ship.AI.OrderRebaseToNearest();
                else
                {
                    Owner.ForcePoolAdd(ship);
                    ship.AI.GoOrbitNearestPlanetAndResupply(false);
                }
            }

            Owner.GetGSAI().UsedFleets.Remove(WhichFleet);
            Fleet.Reset();

            if (type == TaskType.Exploration)
            {
                Array<Troop> toLaunch = new Array<Troop>();
                for (int index = TargetPlanet.TroopsHere.Count - 1; index >= 0; index--)
                {
                    Troop t = TargetPlanet.TroopsHere[index];
                    if (t.GetOwner() != Owner
                        || TargetPlanet.ParentSystem.CombatInSystem
                        || t.AvailableAttackActions == 0
                        || t.MoveTimer > 0)
                        continue;

                    toLaunch.Add(t);
                }

                foreach (Troop t in toLaunch)
                {
                    Ship troopship = t.Launch();
                    if (troopship == null)
                        continue;

                    troopship.AI.OrderRebaseToNearest();
                }

                toLaunch.Clear();
            }
        }

        public void EndTaskWithMove()
        {
            Owner.GetGSAI().TaskList.QueuePendingRemoval(this);
            foreach (Guid goalGuid in HeldGoals)
            {
                foreach (Goal g in Owner.GetGSAI().Goals)
                {
                    if (g.guid != goalGuid)
                        continue;

                    g.Held = false;
                }
            }

            AO closestAO = Owner.GetGSAI().AreasOfOperations.FindMin(ao => AO.SqDist(ao.Center));
            if (closestAO == null)
            {
                if (  !IsCoreFleetTask && WhichFleet != -1 && Owner != EmpireManager.Player)
                {
                    foreach (Ship ship in Owner.GetFleetsDict()[WhichFleet].Ships)
                    {
                        Owner.ForcePoolAdd(ship);
                    }
                }
                return;
            }

            if (WhichFleet != -1)
            {
                if (IsCoreFleetTask)
                {
                    Owner.GetFleet(WhichFleet).FleetTask = null;
                    Owner.GetFleet(WhichFleet).MoveToDirectly(closestAO.Center, 0f, new Vector2(0f, -1f));
                }
                else
                {
                    foreach (Ship ship in Owner.GetFleetsDict()[WhichFleet].Ships)
                    {
                        Owner.GetFleet(WhichFleet).RemoveShip(ship);
                        closestAO.AddShip(ship);
                        closestAO.TurnsToRelax = 0;
                    }

                    TaskForce.Clear();
                    Owner.GetGSAI().UsedFleets.Remove(WhichFleet);
                    Owner.GetFleet(WhichFleet).Reset();
                }
            }
            
        }

        public void Evaluate(Empire e)
        {  
            Owner = e;
            if (WhichFleet >-1)
            {
                if (!e.GetFleetsDict().TryGetValue(WhichFleet, out Fleet fleet) || fleet == null )
                {
                    Log.Warning($"MilitaryTask Evaluate found task with missing fleet {type}");
                    EndTask();
                }
            }
            switch (type)
            {
                case TaskType.ClearAreaOfEnemies:
                    {
                        if      (Step == 0) RequisitionForces();
                        else if (Step == 1) ExecuteAndAssess();
                        break;
                    }
                case TaskType.AssaultPlanet:
                    {
                        if (Step == 0) RequisitionAssaultForces();
                        else
                        {
                            if (Owner.GetFleetsDict().TryGetValue(WhichFleet, out Fleet fleet))
                            {
                                if (fleet.Ships.Count != 0)
                                    break;
                            }

                            EndTask();
                        }
                        break;
                    }
                case TaskType.CorsairRaid:
                    {
                        if (Step != 0)
                            break;

                        Owner.GetFleetsDict()[1].Reset();
                        foreach (Ship shiptoadd in Owner.GetShips())
                        {
                            if (shiptoadd.shipData.Role != ShipData.RoleName.platform)
                                Owner.GetFleetsDict()[1].AddShip(shiptoadd);
                        }

                        if (Owner.GetFleetsDict()[1].Ships.Count <= 0)
                            break;

                        Owner.GetFleetsDict()[1].Name = "Corsair Raiders";
                        Owner.GetFleetsDict()[1].AutoArrange();
                        Owner.GetFleetsDict()[1].FleetTask = this;
                        WhichFleet = 1;
                        Step = 1;
                        Owner.GetFleetsDict()[1].FormationWarpTo(TargetPlanet.Center, 0.0f, Vector2.Zero);
                        break;
                    }
                case TaskType.CohesiveClearAreaOfEnemies:
                    {
                        if      (Step == 0) RequisitionForces();
                        else if (Step == 1) ExecuteAndAssess();
                        break;
                    }
                case TaskType.Exploration:
                    {
                        if (Step == 0) RequisitionExplorationForce();
                        break;
                    }
                case TaskType.DefendSystem:
                    {
                        if      (Step == 0) RequisitionDefenseForce();
                        else if (Step == 1)
                        {
                            if (Owner.GetFleetsDict().ContainsKey(WhichFleet))
                            {
                                if (Owner.GetFleetsDict()[WhichFleet].Ships.Count != 0)
                                    break;
                            }
                            EndTask();
                        }
                        break;
                    }
                case TaskType.DefendClaim:
                    {
                        switch (Step)
                        {
                            case 0:
                                {
                                    if (TargetPlanet.Owner != null)
                                    {
                                        Owner.TryGetRelations(TargetPlanet.Owner, out Relationship rel);

                                        if (rel != null && (!rel.AtWar && !rel.PreparingForWar))
                                            EndTask();
                                    }
                                    if (!RequisitionClaimForce())
                                        return;
                                    Step = 1;
                                    break;
                                }                                
                            case 1:
                                {
                                    if (Owner.GetFleetsDict().ContainsKey(WhichFleet))
                                    {
                                        if (Owner.GetFleetsDict()[WhichFleet].Ships.Count == 0)
                                        {
                                            EndTask();
                                            return;
                                        }

                                        if (TargetPlanet.Owner != null) // &&(empire.GetFleetsDict().ContainsKey(WhichFleet)))
                                        {
                                        Owner.TryGetRelations(TargetPlanet.Owner, out Relationship rel);
                                            if (rel != null && (rel.AtWar || rel.PreparingForWar))
                                            {
                                                if (Vector2.Distance(Owner.GetFleetsDict()[WhichFleet].FindAveragePosition(), TargetPlanet.Center) < AORadius)
                                                    Step = 2;

                                                return;
                                            }
                                        }
                                    }
                                    else
                                        EndTask();

                                    if (TargetPlanet.Owner == null)
                                        return;

                                    EndTask();
                                    return;
                                }

                            case 2:
                                {
                                    if (Owner.GetFleetsDict().ContainsKey(WhichFleet))
                                    {
                                        if (Owner.GetFleetsDict()[WhichFleet].Ships.Count == 0)
                                        {
                                            EndTask();
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        EndTask();
                                        return;
                                    }

                                    if (TargetPlanet.Owner == null)
                                    {
                                        EndTask();
                                        return;
                                    }

                                    Owner.TryGetRelations(TargetPlanet.Owner, out Relationship rel);
                                    if (rel != null && !(rel.AtWar || rel.PreparingForWar))
                                        EndTask();

                                    if (TargetPlanet.Owner == null || TargetPlanet.Owner == Owner)
                                        EndTask();

                                    return;
                                }
                            default:
                                return;
                        }
                        break;
                    }
            }
        }

        private void ExecuteAndAssess()
        {
            if (WhichFleet == -1)
            {
                Step = 0;
                return;
            }

            if (type == TaskType.Exploration ||type ==TaskType.AssaultPlanet)
            {
                
                float ourGroundStrength = TargetPlanet.GetGroundStrength(Owner);

                if (ourGroundStrength > 0)
                {
                    if (type == TaskType.Exploration)
                    {
                        Planet p = TargetPlanet;
                        if (p.BuildingList.Find(relic => !string.IsNullOrEmpty(relic.EventTriggerUID)) != null)
                            return;
                    }
                    else if (type == TaskType.AssaultPlanet)
                    {
                        float groundstrength = TargetPlanet.GetGroundStrengthOther(Owner);
                        if (groundstrength > 0)
                            return;
                    }
                }
            } 
            
            if (Owner.GetFleet(WhichFleet)?.FleetTask == null )
            {
                EndTask();
                return;
            }
            
            float currentStrength = 0f;
            foreach (Ship ship in Owner.GetFleetsDict()[WhichFleet].Ships)
            {
                if (!ship.Active || ship.InCombat && Step < 1 || ship.AI.State == AIState.Scrap)
                {
                    Owner.GetFleetsDict()[WhichFleet].Ships.QueuePendingRemoval(ship);
                    if (ship.Active && ship.AI.State != AIState.Scrap)
                    {
                        if (ship.fleet != null)
                            Owner.GetFleetsDict()[WhichFleet].Ships.QueuePendingRemoval(ship);
                        
                        Owner.ForcePoolAdd(ship);
                    }
                    else if (ship.AI.State == AIState.Scrap)
                    {
                        if (ship.fleet != null)
                            Owner.GetFleetsDict()[WhichFleet].Ships.QueuePendingRemoval(ship);
                    }
                }
                else
                {
                    currentStrength += ship.GetStrength();
                }
            }

            Owner.GetFleet(WhichFleet).Ships.ApplyPendingRemovals();
            float currentEnemyStrength = 0f;

            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in Owner.GetGSAI().ThreatMatrix.Pins)
            {
                if (Vector2.Distance(AO, pin.Value.Position) >= AORadius || pin.Value.Ship == null)
                    continue;

                Empire pinEmp = EmpireManager.GetEmpireByName(pin.Value.EmpireName);

                if (pinEmp == Owner || !pinEmp.isFaction && !Owner.GetRelations(pinEmp).AtWar )
                    continue;

                currentEnemyStrength += pin.Value.Strength;
            }

            if (currentStrength < 0.15f * StartingStrength && currentEnemyStrength > currentStrength)
            {
                EndTask();
                return;
            }

            if (currentEnemyStrength < 1 || currentStrength < 0f)
                EndTask();
        }

        public void FactionEndTask()
        {
            if (WhichFleet != -1)
            {
                if (!IsCoreFleetTask)
                {
                    if (!Owner.GetFleetsDict().ContainsKey(WhichFleet))
                        return;

                    foreach (Ship ship in Owner.GetFleet(WhichFleet).Ships)
                    {
                        ship.AI.OrderQueue.Clear();
                        ship.AI.State = AIState.AwaitingOrders;
                        Owner.GetFleetsDict()[WhichFleet].RemoveShip(ship);
                        ship.HyperspaceReturn();
                        ship.isSpooling = false;

                        if (ship.shipData.Role != ShipData.RoleName.troop)
                        {
                            ship.AI.GoOrbitNearestPlanetAndResupply(false);
                        }
                        else
                        {
                            ship.AI.OrderRebaseToNearest();
                        }
                    }
                    TaskForce.Clear();
                    Owner.GetGSAI().UsedFleets.Remove(WhichFleet);
                    Owner.GetFleetsDict()[WhichFleet].Reset();
                }

                if (type == TaskType.Exploration)
                {
                    Array<Troop> toLaunch = new Array<Troop>();
                    foreach (Troop t in TargetPlanet.TroopsHere)
                    {
                        if (t.GetOwner() != Owner)
                            continue;

                        toLaunch.Add(t);
                    }

                    foreach (Troop t in toLaunch)
                    {
                        Ship troopship = t.Launch();

                        if (troopship == null)
                            continue;

                        troopship.AI.OrderRebaseToNearest();
                    }
                    toLaunch.Clear();
                }
            }
            Owner.GetGSAI().TaskList.QueuePendingRemoval(this);
        }

        public void SetEmpire(Empire e)
        {
            Owner = e;
        }

        public void SetTargetPlanet(Planet p)
        {
            TargetPlanet = p;
            TargetPlanetGuid = p.guid;
        }


        private bool IsNull<T>(T item)
        {
            if (item == null)
            {
                Log.Error("Null Value");
                return false;
            }
            return true;
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
            TaskForce = null;  //Dispose(ref TaskForce);            
        }

        private int FindFleetNumber()
        {
            for (int i = 1; i < 10; i++)
            {
                if (Owner.GetGSAI().UsedFleets.Contains(i))
                    continue;

                return i;
            }
            return -1;
        }
    }
}