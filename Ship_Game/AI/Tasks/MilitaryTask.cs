using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Ship_Game.AI.Tasks
{
    public partial class MilitaryTask
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
        [XmlIgnore] [JsonIgnore] Empire Owner;
        [XmlIgnore] [JsonIgnore] Array<Ship> TaskForce = new Array<Ship>();
        [XmlIgnore] [JsonIgnore] public Fleet Fleet => Owner.GetFleetOrNull(WhichFleet);

        public MilitaryTask()
        {
        }
        public static MilitaryTask CreatePostInvasion(Vector2 ao, int fleetId, Empire owner)
        {
            var militaryTask = new MilitaryTask
            {
                AO = ao,
                AORadius = 10000f,
                WhichFleet = fleetId
            };
            militaryTask.SetEmpire(owner);
            militaryTask.type = TaskType.DefendPostInvasion;
            return militaryTask;
        }

        public static MilitaryTask CreatePostInvasion(Planet planet, int fleetId, Empire owner)
        {
            var militaryTask = new MilitaryTask
            {
                AO           = planet.Center,
                AORadius     = 10000f,
                WhichFleet   = fleetId,
                TargetPlanet = planet
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

        public MilitaryTask(Vector2 location, float radius, Array<Goal> goalsToHold, Empire owner, float str = 0) 
        {
            type = TaskType.ClearAreaOfEnemies;
            AO = location;
            AORadius = radius;
            InitialEnemyStrength = str;

            foreach (Goal g in goalsToHold)
            {
                g.Held = true;
                HeldGoals.Add(g.guid);
            }

            EnemyStrength = owner.CurrentMilitaryStrength * .001f;
            if (InitialEnemyStrength < 1)
                InitialEnemyStrength = owner.GetEmpireAI().ThreatMatrix.PingRadarStr(location, radius, owner);

            MinimumTaskForceStrength = EnemyStrength;
            Owner = owner;
        }         

        public MilitaryTask(Planet target, Empire owner)
        {
            type = TaskType.AssaultPlanet;
            TargetPlanet = target;
            TargetPlanetGuid = target.guid;
            AO = target.Center;
            AORadius = 35000f;
            Owner = owner;
            MinimumTaskForceStrength = owner.CurrentMilitaryStrength *.05f;
        }

        public MilitaryTask(Planet target, Empire owner, float strWanted)
        {
            type = TaskType.AssaultPlanet;
            TargetPlanet = target;
            TargetPlanetGuid = target.guid;
            AO = target.Center;
            AORadius = 35000f;
            Owner = owner;

            strWanted = strWanted.ClampMin(owner.CurrentMilitaryStrength * .05f);
            MinimumTaskForceStrength = strWanted;
        }


        public MilitaryTask(Empire owner)
        {
            Owner = owner;
        }

        void DeclareWar()
        {
            Relationship r = Owner.GetRelations(TargetPlanet.Owner);
            if (r.PreparingForWar)
            {
                Owner.GetEmpireAI().DeclareWarOn(TargetPlanet.Owner, r.PreparingForWarType);
            }
        }

        public void EndTask()
        {
            Debug_TallyFailedTasks();

            if (Owner.isFaction)
            {
                FactionEndTask();
                return;
            }

            ClearHoldOnGoal();

            if (WhichFleet == -1) return;
            if (Fleet == null)    return;

            AO closestAo = FindClosestAO();
            if (closestAo == null)
            {
                if (Fleet.IsCoreFleet  || Owner == Empire.Universe.player) return;
                DisbandFleet(Fleet);
                return;
            }

            if (ClearCoreFleetTask()) return;

            TaskForce.Clear();
            if (Fleet.IsCoreFleet || Owner.isPlayer)
                return;

            if (Fleet == null)
                return;

            for (int i = Fleet.Ships.Count - 1; i >= 0; i--)
            {
                Ship ship = Fleet.Ships[i];
                ship.AI.ClearOrders();
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

            Owner.GetEmpireAI().UsedFleets.Remove(WhichFleet);
            Fleet.Reset();

            if (type == TaskType.Exploration)
            {
                Array<Troop> toLaunch = new Array<Troop>();
                for (int index = TargetPlanet.TroopsHere.Count - 1; index >= 0; index--)
                {
                    Troop t = TargetPlanet.TroopsHere[index];
                    if (t.Loyalty != Owner
                        || TargetPlanet.EnemyInRange()
                        || t.AvailableAttackActions == 0
                        || t.MoveTimer > 0)
                        continue;

                    toLaunch.Add(t);
                }

                foreach (Troop t in toLaunch)
                {
                    Ship troopship = t.Launch();
                    troopship?.AI.OrderRebaseToNearest();
                }

                toLaunch.Clear();
            }
        }

        private bool ClearCoreFleetTask()
        {
            if (IsCoreFleetTask)
            {
                for (int i = 0; i < Fleet.Ships.Count; i++)
                {
                    Ship ship = Fleet.Ships[i];
                    ship.AI.CombatState = ship.shipData.CombatState;
                }

                Fleet.FleetTask = null;
                return true;
            }

            return false;
        }

        private void DisbandFleet(Fleet fleet)
        {
            foreach (Ship ship in fleet.Ships)
            {
                Fleet.RemoveShip(ship);
                if (ship?.Active ?? false)
                    Owner.ForcePoolAdd(ship);
            }

            fleet.Reset();
        }

        private void Debug_TallyFailedTasks()
        {
            DebugInfoScreen.CanceledMtasksCount++;
            Owner.GetEmpireAI().TaskList.QueuePendingRemoval(this);
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
        }

        private void ClearHoldOnGoal()
        {
            foreach (Guid goalGuid in HeldGoals)
            {
                foreach (Goal g in Owner.GetEmpireAI().Goals)
                {
                    if (g.guid == goalGuid) g.Held = false;
                }
            }
        }

        public void EndTaskWithMove()
        {
            Owner.GetEmpireAI().TaskList.QueuePendingRemoval(this);
            
            ClearHoldOnGoal();

            AO closestAo = Owner.GetEmpireAI().AreasOfOperations.FindMin(ao => AO.SqDist(ao.Center));
            if (closestAo == null)
            {
                if (!IsCoreFleetTask && WhichFleet != -1 && Owner != EmpireManager.Player)
                {
                    foreach (Ship ship in Fleet.Ships)
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
                    Owner.GetFleetOrNull(WhichFleet).FleetTask = null;
                    Owner.GetFleetOrNull(WhichFleet).MoveToDirectly(closestAo.Center, Vectors.Up);
                }
                else
                {
                    foreach (Ship ship in Owner.GetFleetsDict()[WhichFleet].Ships)
                    {
                        Owner.GetFleetOrNull(WhichFleet).RemoveShip(ship);
                        closestAo.AddShip(ship);
                        closestAo.TurnsToRelax = 0;
                    }

                    TaskForce.Clear();
                    Owner.GetEmpireAI().UsedFleets.Remove(WhichFleet);
                    Owner.GetFleetOrNull(WhichFleet).Reset();
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
                                if (fleet.Ships.Count > 0)
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
                        foreach (Ship shipToAdd in Owner.GetShips())
                        {
                            if (shipToAdd.shipData.Role != ShipData.RoleName.platform)
                                Owner.GetFleetsDict()[1].AddShip(shipToAdd);
                        }

                        if (Owner.GetFleetsDict()[1].Ships.Count <= 0)
                            break;

                        Owner.GetFleetsDict()[1].Name = "Corsair Raiders";
                        Owner.GetFleetsDict()[1].AutoArrange();
                        Owner.GetFleetsDict()[1].FleetTask = this;
                        WhichFleet = 1;
                        Step = 1;
                        Owner.GetFleetsDict()[1].FormationWarpTo(TargetPlanet.Center, new Vector2(0f, -1));
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

                                        if (TargetPlanet.Owner != null)
                                        {
                                            Owner.TryGetRelations(TargetPlanet.Owner, out Relationship rel);
                                            if (rel != null && (rel.AtWar || rel.PreparingForWar))
                                            {
                                                if (Owner.GetFleetsDict()[WhichFleet].AveragePosition().Distance(TargetPlanet.Center) < AORadius)
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

        void ExecuteAndAssess()
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
                        if (p.BuildingList.Find(relic => relic.EventHere) != null)
                            return;
                    }
                    else if (type == TaskType.AssaultPlanet)
                    {
                        float groundStr = TargetPlanet.GetGroundStrengthOther(Owner);
                        if (groundStr > 0)
                            return;
                    }
                }
            }
            
            Fleet fleet = Owner.GetFleetOrNull(WhichFleet);
            if (fleet?.FleetTask == null)
            {
                EndTask();
                return;
            }
            
            float currentStrength = 0f;
            foreach (Ship ship in fleet.Ships)
            {
                // remove dead or scrapping ships
                if (!ship.Active || ship.InCombat && Step < 1 || ship.AI.State == AIState.Scrap)
                {
                    fleet.RemoveShip(ship);
                    if (ship.Active && ship.AI.State != AIState.Scrap)
                        Owner.ForcePoolAdd(ship);
                }
                else
                {
                    currentStrength += ship.GetStrength();
                }
            }

            float currentEnemyStrength = 0f;

            foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in Owner.GetEmpireAI().ThreatMatrix.Pins)
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

                    foreach (Ship ship in Owner.GetFleetOrNull(WhichFleet).Ships)
                    {
                        ship.AI.ClearOrders();
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
                    Owner.GetEmpireAI().UsedFleets.Remove(WhichFleet);
                    Owner.GetFleetsDict()[WhichFleet].Reset();
                }

                if (type == TaskType.Exploration)
                {
                    Array<Troop> toLaunch = new Array<Troop>();
                    foreach (Troop t in TargetPlanet.TroopsHere)
                    {
                        if (t.Loyalty != Owner)
                            continue;

                        toLaunch.Add(t);
                    }

                    foreach (Troop t in toLaunch)
                    {
                        Ship troopship = t.Launch();
                        troopship?.AI.OrderRebaseToNearest();
                    }
                    toLaunch.Clear();
                }
            }
            Owner.GetEmpireAI().TaskList.QueuePendingRemoval(this);
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

        //need to examine this fleet key thing. i believe there is a leak. 
        int FindUnusedFleetNumber()
        {
            var used = Owner.GetEmpireAI().UsedFleets;
            int key = 1;
            while (used.Contains(key))
                ++key;
            return key;
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