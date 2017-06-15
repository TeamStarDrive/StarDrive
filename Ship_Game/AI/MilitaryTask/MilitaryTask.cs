using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Debug;
using Ship_Game.Gameplay;

namespace Ship_Game.AI.Tasks
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
        [XmlIgnore] [JsonIgnore] private Empire Owner;
        [XmlIgnore] [JsonIgnore] private Array<Ship> TaskForce = new Array<Ship>();
        [XmlIgnore] [JsonIgnore] private Fleet Fleet => Owner.GetFleetsDict()[WhichFleet];

        //This file Refactored by Gretman

        public MilitaryTask()
        {
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
        public MilitaryTask(Vector2 location, float radius, Array<Goal> GoalsToHold, Empire Owner)
        {
            type = MilitaryTask.TaskType.ClearAreaOfEnemies;
            AO = location;
            AORadius = radius;

            foreach (Goal g in GoalsToHold)
            {
                g.Held = true;
                HeldGoals.Add(g.guid);
            }

            EnemyStrength = Owner.GetGSAI().ThreatMatrix.PingRadarStr(location, radius, Owner);
            if (InitialEnemyStrength == 0)
                InitialEnemyStrength = EnemyStrength;

            MinimumTaskForceStrength = EnemyStrength *.75f;
            this.Owner = Owner;
        }

        public MilitaryTask(Planet target, Empire Owner)
        {
            type = MilitaryTask.TaskType.AssaultPlanet;
            TargetPlanet = target;
            TargetPlanetGuid = target.guid;
            AO = target.Center;
            AORadius = 35000f;
            this.Owner = Owner;
        }

        public MilitaryTask(Empire Owner)
        {
            this.Owner = Owner;
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
                if (WhichFleet != -1 && !Fleet.IsCoreFleet && Owner != Empire.Universe.player)
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
                    Fleet.FleetTask = null;
                }
                else
                {
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
                        Fleet.RemoveShip(ship);
                        ship.HyperspaceReturn();
                        ship.isSpooling = false;
                        if (ship.shipData.Role == ShipData.RoleName.troop)
                            ship.AI.OrderRebaseToNearest();
                        else
                        {

                            Owner.ForcePoolAdd(ship);
                            ship.AI.OrderResupplyNearest(false);
                        }
                        
                    }
                    
                    Owner.GetGSAI().UsedFleets.Remove(WhichFleet);
                    Fleet.Reset();
                }

                if (type == TaskType.Exploration)
                {
                    Array<Troop> toLaunch = new Array<Troop>();
                    for (int index = TargetPlanet.TroopsHere.Count - 1; index >= 0; index--)
                    {
                        Troop t = TargetPlanet.TroopsHere[index];
                        if (t.GetOwner() != Owner
                            || TargetPlanet.system.CombatInSystem
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
                    Owner.GetFleetsDict()[WhichFleet].FleetTask = null;
                    Owner.GetFleetsDict()[WhichFleet].MoveToDirectly(closestAO.Center, 0f, new Vector2(0f, -1f));
                }
                else
                {
                    foreach (Ship ship in Owner.GetFleetsDict()[WhichFleet].Ships)
                    {
                        Owner.GetFleetsDict()[WhichFleet].RemoveShip(ship);
                        closestAO.AddShip(ship);
                        closestAO.TurnsToRelax = 0;
                    }

                    TaskForce.Clear();
                    Owner.GetGSAI().UsedFleets.Remove(WhichFleet);
                    Owner.GetFleetsDict()[WhichFleet].Reset();
                }
            }
            
        }

        public void Evaluate(Empire e)
        {  
            Owner = e;
            switch (type)
            {
                case MilitaryTask.TaskType.ClearAreaOfEnemies:
                    {
                        if      (Step == 0) RequisitionForces();
                        else if (Step == 1) ExecuteAndAssess();
                        break;
                    }
                case MilitaryTask.TaskType.AssaultPlanet:
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
                case MilitaryTask.TaskType.CorsairRaid:
                    {
                        if (Step != 0)
                            break;

                        Owner.GetFleetsDict()[1].Reset();
                        foreach (Ship shiptoadd in (Array<Ship>)Owner.GetShips())
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
                case MilitaryTask.TaskType.CohesiveClearAreaOfEnemies:
                    {
                        if      (Step == 0) RequisitionForces();
                        else if (Step == 1) ExecuteAndAssess();
                        break;
                    }
                case MilitaryTask.TaskType.Exploration:
                    {
                        if (Step == 0) RequisitionExplorationForce();
                        break;
                    }
                case MilitaryTask.TaskType.DefendSystem:
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
                case MilitaryTask.TaskType.DefendClaim:
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
                
                float ourGroundStrength = GetTargetPlanet().GetGroundStrength(Owner);

                if (ourGroundStrength > 0)
                {
                    if (type == TaskType.Exploration)
                    {
                        Planet p = GetTargetPlanet();
                        if (p.BuildingList.Find(relic => !string.IsNullOrEmpty(relic.EventTriggerUID)) != null)
                            return;
                    }
                    else if (type == TaskType.AssaultPlanet)
                    {
                        float groundstrength = GetTargetPlanet().GetGroundStrengthOther(Owner);
                        if (groundstrength > 0)
                            return;
                    }
                }
            } 
            
            if (Owner.GetFleetsDict()[WhichFleet].FleetTask == null )
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

            Owner.GetFleetsDict()[WhichFleet].Ships.ApplyPendingRemovals();
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

            if (currentEnemyStrength == 0f || currentStrength == 0f)
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

                    foreach (Ship ship in Owner.GetFleetsDict()[WhichFleet].Ships)
                    {
                        ship.AI.OrderQueue.Clear();
                        ship.AI.State = AIState.AwaitingOrders;
                        Owner.GetFleetsDict()[WhichFleet].RemoveShip(ship);
                        ship.HyperspaceReturn();
                        ship.isSpooling = false;

                        if (ship.shipData.Role != ShipData.RoleName.troop)
                        {
                            ship.AI.OrderResupplyNearest(false);
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

                if (type == MilitaryTask.TaskType.Exploration)
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

        private float GetEnemyStrAtTarget() => GetEnemyStrAtTarget(1000);
        
        private float GetEnemyStrAtTarget(float standardMinimum)
        {		                        
            float MinimumEscortStrength = 1000;
            if (TargetPlanet.Owner == null)
                return standardMinimum;

            TargetPlanet.Owner.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(TargetPlanet.ParentSystem, out SystemCommander scom);
            float importance = 1;

            if (scom != null)
                importance = 1 + scom.RankImportance * .01f;

            float distance = 250000 * importance;            
            MinimumEscortStrength = Owner.GetGSAI().ThreatMatrix.PingRadarStr(AO, distance,Owner);
            standardMinimum *= importance;
            if (MinimumEscortStrength < standardMinimum)
                MinimumEscortStrength = standardMinimum;

            return MinimumEscortStrength;
        }

        private float GetEnemyTroopStr() => TargetPlanet.GetGroundStrengthOther(Owner);        

        public Planet GetTargetPlanet() => TargetPlanet;
        
        private Array<Troop> GetTroopsOnPlanets(Array<Troop> potentialTroops, Vector2 rallyPoint)
        {            
            var defenseDict = Owner.GetGSAI().DefensiveCoordinator.DefenseDict;
            var troopSystems = Owner.GetOwnedSystems().OrderBy(troopSource => defenseDict[troopSource].RankImportance)
                .ThenBy(dist => dist.Position.SqDist(rallyPoint));
            foreach(SolarSystem system in troopSystems)
            {
                int rank = (int)defenseDict[system].RankImportance;
                foreach (Planet planet in system.PlanetList)
                {                    
                    if (planet.Owner != Owner) continue;
                    if (planet.RecentCombat) continue;
                    int extra = rank /2;
                    potentialTroops.AddRange(planet.GetEmpireTroops(Owner, extra));
                }
                if (potentialTroops.Count > 100)
                    break;
            }

            return potentialTroops;
        }
        private int CountShipTroopAndStrength(Array<Ship> potentialAssaultShips,  out float ourStrength)
        {
            ourStrength = 0;
            int troopCount = 0;
            foreach (Ship ship in potentialAssaultShips)
            {
                int hangars = 0;
                foreach (ShipModule hangar in ship.GetHangars())
                {
                    if (hangar.IsTroopBay)
                        hangars++;
                }

                foreach (Troop t in ship.TroopList)
                {
                    ourStrength += t.Strength;
                    troopCount++;
                    hangars--;
                    if (hangars <= 0)
                        break;
                }
            }
            return troopCount;
        }
        

        private Array<Ship> AddShipsLimited(Array<Ship> shipList, float strengthLimit, float tfStrength, out float currentStrength)
        {
            Array<Ship> added = new Array<Ship>();
            foreach (Ship ship in shipList)
            {               
                tfStrength += ship.GetStrength();
                added.Add(ship);
                if (tfStrength > strengthLimit)
                    break;
            }
            currentStrength = tfStrength;
            return added;
        }
        private bool DeclareWar()
        {
            if (Owner.GetRelations(TargetPlanet.Owner).PreparingForWar)
            {
                Owner.GetGSAI().DeclareWarOn(TargetPlanet.Owner, Owner.GetRelations(TargetPlanet.Owner).PreparingForWarType);
                return true;
            }
            return false;
        }
        private void CreateFleet(Array<Ship> elTaskForce, Array<Ship> potentialAssaultShips, 
            Array<Troop> potentialTroops,float EnemyTroopStrength, AO closestAO,  Array<Ship> potentialBombers = null, string fleetName = "Invasion Fleet")
        {
   
            
            int landingSpots = TargetPlanet.GetGroundLandingSpots();
            if (potentialBombers != null)
            {
                int bombs = 0;
                foreach (Ship ship in potentialBombers)
                {
                    bombs += ship.BombBays.Count;

                    if (elTaskForce.Contains(ship))
                        continue;

                    elTaskForce.Add(ship);
                    if (bombs > 25 - landingSpots)
                        break;
                }
            }
           
            
            Fleet newFleet = new Fleet()
            {
                Owner = Owner,
                Name = fleetName
            };

            int FleetNum = FindFleetNumber();
            float ForceStrength = 0f;

            foreach (Ship ship in potentialAssaultShips)
            {
                if (ForceStrength > EnemyTroopStrength * 1.25f )
                    break;

                newFleet.AddShip(ship);
                ForceStrength += ship.PlanetAssaultStrength;
                
            }

            foreach (Troop t in potentialTroops)
            {
                if (ForceStrength > EnemyTroopStrength * 1.25f )
                    break;

                if (t.GetPlanet() != null && t.GetPlanet().ParentSystem.combatTimer <= 0 && !t.GetPlanet().RecentCombat)
                {
                    if (t.GetOwner() != null)
                    {
                        newFleet.AddShip(t.Launch());
                        ForceStrength += t.Strength;
                        
                    }
                }
            }

            Owner.GetFleetsDict()[FleetNum] = newFleet;
            Owner.GetGSAI().UsedFleets.Add(FleetNum);
            WhichFleet = FleetNum;
            newFleet.FleetTask = this;
            foreach (Ship ship in elTaskForce)
            {
                newFleet.AddShip(ship);
                ship.AI.OrderQueue.Clear();
                ship.AI.State = AIState.AwaitingOrders;

                Owner.GetGSAI().RemoveShipFromForce(ship,closestAO);
            }
            newFleet.AutoArrange();
            Step = 1;


        }

        private void GetAvailableShips(AO area, Array<Ship> bombers, Array<Ship> combat, Array<Ship> troopShips, Array<Ship> utility)
        {
            var ships = area.GetOffensiveForcePool().Union(Owner.GetForcePool());
            foreach (Ship ship in ships)
            {
                if (ship.fleet != null)
                    Log.Error("GetAvailableShips: a ship is in fleet {0} and not available for {1}", ship.fleet.Name, type.ToString());
                if (area.GetWaitingShips().ContainsRef(ship))
                    Log.Error("ship is in waiting list and should not be");

                if (Empire.Universe.Debug) foreach (AO ao in Owner.GetGSAI().AreasOfOperations)
                {
                    if (ao == area) continue;
                    if (ao.GetOffensiveForcePool().Contains(ship))
                        Log.Error("Ship {0} in another AO {1}", ship.Name, ao.GetPlanet().Name);

                }
                if ((ship.shipData.Role == ShipData.RoleName.station || ship.shipData.Role == ShipData.RoleName.platform)
                    || !ship.BaseCanWarp
                    || ship.InCombat
                    || ship.fleet != null
                    || ship.Mothership != null
                    || ship.AI.State != AIState.AwaitingOrders
                    || (ship.System != null && ship.System.CombatInSystem))
                    continue;

                if (utility != null && (ship.InhibitionRadius > 0 || ship.hasOrdnanceTransporter || ship.hasRepairBeam || ship.HasRepairModule || ship.HasSupplyBays))
                {
                    utility.Add(ship);
                }
                else if (bombers != null && ship.BombBays.Count > 0)
                {
                    bombers.Add(ship);
                }
                else if (troopShips != null && (ship.TroopList.Count > 0 && (ship.hasAssaultTransporter || ship.HasTroopBay || ship.GetShipData().Role == ShipData.RoleName.troop)))
                {
                    troopShips.Add(ship);
                }
                else if (combat != null && ship.BombBays.Count <= 0 && ship.BaseStrength > 0)
                {
                    combat.Add(ship);
                }
            }
        }
        private Array<Ship> GetShipsFromDefense(float tfstrength, float MinimumEscortStrength)
        {
            Array<Ship> elTaskForce = new Array<Ship>();
            if (!Owner.isFaction && Owner.data.DiplomaticPersonality.Territorialism < 50 && tfstrength < MinimumEscortStrength)
            {
                if (!IsCoreFleetTask)
                    foreach (var kv in Owner.GetGSAI().DefensiveCoordinator.DefenseDict
                        .OrderByDescending(system => system.Key.CombatInSystem ? 1 : 2 * system.Key.Position.SqDist(TargetPlanet.Center))
                        .ThenByDescending(ship => (ship.Value.GetOurStrength() - ship.Value.IdealShipStrength) < 1000)


                    )
                    {
                        var ships = kv.Value.GetShipList;

                        for (int index = 0; index < ships.Length; index++)
                        {
                            Ship ship = ships[index];
                            if (ship.AI.BadGuysNear || ship.fleet != null || tfstrength >= MinimumEscortStrength ||
                                ship.GetStrength() <= 0f
                                || ship.shipData.Role == ShipData.RoleName.troop || ship.hasAssaultTransporter ||
                                ship.HasTroopBay
                                || ship.Mothership != null
                            )
                                continue;

                            tfstrength = tfstrength + ship.GetStrength();
                            elTaskForce.Add(ship);
                            Owner.GetGSAI().DefensiveCoordinator.Remove(ship);
                        }
                    }
            }
            return elTaskForce;
        }
        private void DoToughNutRequisition()
        {
            float EnemyTroopStr = GetEnemyTroopStr();
            if (EnemyTroopStr < 100)
                EnemyTroopStr = 100;

            float EnemyShipStr = GetEnemyStrAtTarget();
            IOrderedEnumerable<AO> sorted =
                from ao in Owner.GetGSAI().AreasOfOperations
                where ao.GetCoreFleet().FleetTask == null || ao.GetCoreFleet().FleetTask.type != TaskType.AssaultPlanet
                orderby ao.GetOffensiveForcePool().Where(combat => !combat.InCombat).Sum(strength => strength.BaseStrength) >= MinimumTaskForceStrength descending
                orderby Vector2.Distance(AO, ao.Center)
                select ao;

            if (sorted.Count<AO>() == 0)
                return;

            var Bombers = new Array<Ship>();
            var EverythingElse = new Array<Ship>();
            var TroopShips = new Array<Ship>();
            var Troops = new Array<Troop>();

            foreach (AO area in sorted)
            {
                GetAvailableShips(area, Bombers, EverythingElse, TroopShips, EverythingElse);
                foreach (Planet p in area.GetPlanets())
                {
                    if (p.RecentCombat || p.ParentSystem.combatTimer > 0)
                        continue;

                    foreach (Troop t in p.TroopsHere)
                    {
                        if (t.GetOwner() != Owner)
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
                if (troopStr > EnemyTroopStr * 1.5f || numOfTroops > TargetPlanet.GetGroundLandingSpots())
                    break;

                PotentialTroops.Add(t);
                troopStr += (float)t.Strength;
                numOfTroops++;
            }

            if (strAdded > EnemyShipStr * 1.65f)
            {
                if (TargetPlanet.Owner == null || TargetPlanet.Owner != null && !Owner.TryGetRelations(TargetPlanet.Owner, out Relationship rel))
                {
                    EndTask();
                    return;
                }

                if (Owner.GetRelations(TargetPlanet.Owner).PreparingForWar)
                {
                    Owner.GetGSAI().DeclareWarOn(TargetPlanet.Owner, Owner.GetRelations(TargetPlanet.Owner).PreparingForWarType);
                }

                AO ClosestAO = sorted.First<AO>();
                MilitaryTask assault = new MilitaryTask(Owner)
                {
                    AO = TargetPlanet.Center,
                    AORadius = 75000f,
                    type = MilitaryTask.TaskType.AssaultPlanet
                };

                ClosestAO.GetCoreFleet().Owner.GetGSAI().TasksToAdd.Add(assault);
                assault.WhichFleet = ClosestAO.WhichFleet;
                ClosestAO.GetCoreFleet().FleetTask = assault;
                assault.IsCoreFleetTask = true;
                assault.Step = 1;

                assault.TargetPlanet = TargetPlanet;
                ClosestAO.GetCoreFleet().TaskStep = 0;
                ClosestAO.GetCoreFleet().Name = "Doom Fleet";
                foreach (Ship ship in TaskForce)
                {

                    ship.fleet?.RemoveShip(ship);

                    ship.AI.OrderQueue.Clear();
                    Owner.GetGSAI().DefensiveCoordinator.Remove(ship);


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
                    MilitaryTask GlassPlanet = new MilitaryTask(Owner)
                    {
                        AO = TargetPlanet.Center,
                        AORadius = 75000f,
                        type = MilitaryTask.TaskType.GlassPlanet,
                        TargetPlanet = TargetPlanet,
                        WaitForCommand = true
                    };

                    Fleet bomberFleet = new Fleet()
                    {
                        Owner = Owner
                    };

                    bomberFleet.Owner.GetGSAI().TasksToAdd.Add(GlassPlanet);
                    GlassPlanet.WhichFleet = Owner.GetUnusedKeyForFleet();
                    Owner.GetFleetsDict().Add(GlassPlanet.WhichFleet, bomberFleet);
                    bomberFleet.FleetTask = GlassPlanet;
                    bomberFleet.Name = "Bomber Fleet";

                    foreach (Ship ship in BombTaskForce)
                    {
                        ship.AI.OrderQueue.Clear();
                        Owner.GetGSAI().DefensiveCoordinator.Remove(ship);
                        ship.fleet?.RemoveShip(ship);

                        bomberFleet.AddShip(ship);
                    }
                    bomberFleet.AutoArrange();
                }
                Step = 1;
                Owner.GetGSAI().TaskList.QueuePendingRemoval(this);
            }
        }

        private void RequisitionAssaultForces()
        {

            if (TargetPlanet.Owner == null || !Owner.IsEmpireAttackable(TargetPlanet.Owner))
            {
                EndTask();
                return;
            }
            if (IsToughNut)
            {
                DoToughNutRequisition();
                return;
            }
            int landingSpots = TargetPlanet.GetGroundLandingSpots();
            AO closestAO = FindClosestAO();

            if (closestAO == null || closestAO.GetOffensiveForcePool().Count < 5)
                return;

            if (Owner.GetRelations(TargetPlanet.Owner).Treaty_Peace)
            {
                Owner.GetRelations(TargetPlanet.Owner).PreparingForWar = false;
                EndTask();
                return;
            }

            float enemyTroopStrength = TargetPlanet.GetGroundStrengthOther(Owner) ;

            if (enemyTroopStrength < 100f)
                enemyTroopStrength = 100f;
            
            Array<Ship> potentialAssaultShips = new Array<Ship>();
            Array<Troop> potentialTroops = new Array<Troop>();
            Array<Ship> potentialCombatShips = new Array<Ship>();
            Array<Ship> potentialBombers = new Array<Ship>();
            Array<Ship> potentialUtilityShips = new Array<Ship>();
            GetAvailableShips(closestAO, potentialBombers, potentialCombatShips, potentialAssaultShips, potentialUtilityShips);
            Planet rallypoint = Owner.RallyPoints?.FindMin(p => p.Center.SqDist(AO));
            if (rallypoint == null)
                return;

            potentialTroops = GetTroopsOnPlanets(potentialTroops,rallypoint.Center);
            int troopCount = potentialTroops.Count();
            troopCount += CountShipTroopAndStrength(potentialAssaultShips, out float ourAvailableStrength);

            foreach (Troop t in potentialTroops)
            ourAvailableStrength = ourAvailableStrength + t.Strength;

            float MinimumEscortStrength = GetEnemyStrAtTarget();

            // I'm unsure on ball-park figures for ship strengths. Given it used to build up to 1500, sticking flat +300 on seems a good start
            //updated. Now it will use 1/10th of the current military strength escort strength needed is under 1000
            //well thats too much. 1/10th can be huge. moved it into the getenemy strength logic with some adjustments. now it looks at the enemy empires importance of the planet. 
            //sort of cheating but as it would be much the same calculation as the attacking empire would use.... hrmm.
            // actually i think the raw importance value could be used to create an importance for that planet. interesting... that could be very useful in many areas. 

            MinimumTaskForceStrength = MinimumEscortStrength;
            BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();
            float tfstrength = 0f;
            elTaskForce.AddRange(AddShipsLimited(potentialCombatShips, MinimumEscortStrength, tfstrength, out float tempStrength));
            tfstrength += tempStrength;

            elTaskForce.AddRange(AddShipsLimited(potentialUtilityShips, MinimumEscortStrength * 1.5f, tfstrength, out  tempStrength));
            tfstrength += tempStrength;

            elTaskForce.AddRange(GetShipsFromDefense(tfstrength, MinimumEscortStrength));
            if (tfstrength >= MinimumTaskForceStrength)
            {
                if (ourAvailableStrength >= enemyTroopStrength && landingSpots > 8 && troopCount >= 10 )
                {
                    DeclareWar();
                    CreateFleet(elTaskForce, potentialAssaultShips, potentialTroops, enemyTroopStrength, closestAO);
                    return;
                }
                if (landingSpots < 10 && potentialBombers.Count > 10 -landingSpots)
                {
                    DeclareWar();
                    CreateFleet(elTaskForce, potentialAssaultShips, potentialTroops, enemyTroopStrength, closestAO, potentialBombers);
                    return;
                }
                if (landingSpots >0 )
                {
                    DeclareWar();
                    CreateFleet(elTaskForce, potentialAssaultShips, potentialTroops, enemyTroopStrength * 2, closestAO);
                    return;
                }
            }
            else
            if (tfstrength <= MinimumTaskForceStrength)
            {
                if (TargetPlanet.Owner == null || TargetPlanet.Owner != null && !Owner.TryGetRelations(TargetPlanet.Owner, out Relationship rel2))
                {
                    EndTask();
                    return;
                }

                Fleet closestCoreFleet = closestAO.GetCoreFleet();
                if (closestCoreFleet.FleetTask == null && closestCoreFleet.GetStrength() > MinimumTaskForceStrength)
                {
                    var clearArea = new MilitaryTask(closestCoreFleet.Owner)
                    {
                        AO       = TargetPlanet.Center,
                        AORadius = 75000f,
                        type     = TaskType.ClearAreaOfEnemies
                    };

                    closestCoreFleet.Owner.GetGSAI().TasksToAdd.Add(clearArea);
                    clearArea.WhichFleet       = closestAO.WhichFleet;
                    closestCoreFleet.FleetTask = clearArea;
                    clearArea.IsCoreFleetTask  = true;
                    closestCoreFleet.TaskStep  = 1;
                    clearArea.Step             = 1;

                    if (Owner.GetRelations(TargetPlanet.Owner).PreparingForWar)
                        Owner.GetGSAI().DeclareWarOn(TargetPlanet.Owner, Owner.GetRelations(TargetPlanet.Owner).PreparingForWarType);
                }
                return;
            }
            if (landingSpots < 10) IsToughNut = true;

            NeededTroopStrength = (int)(enemyTroopStrength - ourAvailableStrength);
        }
        private void RequisitionDefenseForce()
        {
            float forcePoolStr = Owner.GetForcePoolStrength();
            float tfstrength = 0f;
            BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();

            foreach (Ship ship in Owner.GetForcePool().OrderBy(strength => strength.GetStrength()))
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

            TaskForce = elTaskForce;
            StartingStrength = tfstrength;
            int FleetNum = FindFleetNumber();
            Fleet newFleet = new Fleet();

            foreach (Ship ship in TaskForce)
            {
                newFleet.AddShip(ship);
            }

            newFleet.Owner = Owner;
            newFleet.Name = "Defensive Fleet";
            newFleet.AutoArrange();
            Owner.GetFleetsDict()[FleetNum] = newFleet;
            Owner.GetGSAI().UsedFleets.Add(FleetNum);
            WhichFleet = FleetNum;
            newFleet.FleetTask = this;

            foreach (Ship ship in TaskForce)
            {
                Owner.ForcePoolRemove(ship);
            }
            Step = 1;
        }
        //added by gremlin req claim forces
        private bool RequisitionClaimForce()
        {
  
            AO closestAO            = FindClosestAO();
            float tfstrength        = 0f;
            Array<Ship> elTaskForce = new Array<Ship>();
            int shipCount           = 0;
            float strengthNeeded    = EnemyStrength;

            if (strengthNeeded <1)
                strengthNeeded = Owner.GetGSAI().ThreatMatrix.PingRadarStr(TargetPlanet.Center, 125000, Owner);
            
            foreach (Ship ship in closestAO.GetOffensiveForcePool().OrderBy(str=>str.GetStrength()))
            {
                if (shipCount >= 3 && (strengthNeeded < Owner.currentMilitaryStrength * .02f && strengthNeeded < tfstrength))
                    break;

                if (ship.GetStrength() <= 0f || ship.InCombat || ship.fleet != null)
                    continue;

                shipCount++;
                if (elTaskForce.Contains(ship))
                     Log.Error("eltaskforce already contains ship");             
                elTaskForce.Add(ship);
                tfstrength += ship.GetStrength();
            }

            if (shipCount < 3 && tfstrength ==0 || tfstrength < strengthNeeded)
                return false;

            TaskForce        = elTaskForce;
            StartingStrength = tfstrength;
            int FleetNum     = FindFleetNumber();
            Fleet newFleet   = new Fleet();
            foreach (Ship ship in TaskForce)
            {
                Owner.GetGSAI().RemoveShipFromForce(ship,closestAO);
                newFleet.AddShip(ship);
            }          
                
            

            newFleet.Owner = Owner;
            newFleet.Name  = "Scout Fleet";
            newFleet.AutoArrange();
            Owner.GetFleetsDict()[FleetNum] = newFleet;
            Owner.GetGSAI().UsedFleets.Add(FleetNum);
            WhichFleet         = FleetNum;
            newFleet.FleetTask = this;


            return true;
        }
        private bool IsNull<T>(T item)
        {
            if(item == null)
            {
                Log.Error("Null Value");
                return false;
            }
            return true;
        }
        private AO FindClosestAO()
        {            
            var aos = Owner.GetGSAI().AreasOfOperations;
            if(aos == null)
            {
                Log.Error("{0} has no areas of operation", Owner.Name);
                return null;
            }
            AO closestAO = aos.FindMaxFiltered(ao =>ao.GetOffensiveForcePool().Count > 0 && ao.GetWaitingShips().Count ==0,ao => -ao.Center.SqDist(AO)) ??
                aos.FindMin(ao => ao.Center.SqDist(AO));
            if (closestAO == null)
            {
                Log.Error("{0} : no areas of operation found", Owner.Name);
                return null;
            }
            return closestAO;
        }
        

        //added by gremlin Req Exploration forces
        private void RequisitionExplorationForce()
        {
            AO closestAO = FindClosestAO();
            
            if (closestAO == null || closestAO.GetOffensiveForcePool().Count < 1)
            {
                EndTask();
                return;
            }
            
            Planet rallyPoint =  closestAO.GetPlanets().Intersect(Owner.RallyPoints).ToArrayList().FindMin(p => p.Center.SqDist(AO));

            if (rallyPoint == null)
            {
                EndTask();
                return;
            }
            EnemyStrength = 0f;
            EnemyStrength = Owner.GetGSAI().ThreatMatrix.PingRadarStrengthLargestCluster(AO, AORadius, Owner);            

            MinimumTaskForceStrength = EnemyStrength + 0.35f * EnemyStrength;

            if (MinimumTaskForceStrength < 1f)
                MinimumTaskForceStrength = 1;
            
            
            Array<Troop> potentialTroops = new Array<Troop>();
            potentialTroops = GetTroopsOnPlanets(potentialTroops, closestAO.GetPlanet().Center);
            if (potentialTroops.Count < 4)
            {
                NeededTroopStrength = 20;
                for (int i = 0; i < potentialTroops.Count; i++)
                {
                    Troop troop = potentialTroops[i];
                    NeededTroopStrength -= (int) troop.Strength;
                    if (NeededTroopStrength > 0)
                        continue;
                }

                NeededTroopStrength = 0;
            }

            Array<Ship> potentialAssaultShips = new Array<Ship>();
            Array<Ship> potentialCombatShips = new Array<Ship>();
            Array<Ship> potentialBombers = new Array<Ship>();
            Array<Ship> potentialUtilityShips = new Array<Ship>();
            GetAvailableShips(closestAO, potentialBombers, potentialCombatShips, potentialAssaultShips, potentialUtilityShips);



            float ourAvailableStrength = 0f;
            CountShipTroopAndStrength(potentialAssaultShips, out float troopStrength);
            ourAvailableStrength += troopStrength;
            

            foreach (Troop t in potentialTroops)
                ourAvailableStrength = ourAvailableStrength + (float)t.Strength;


            float tfstrength = 0f;
            Array<Ship> elTaskForce = AddShipsLimited(potentialCombatShips, MinimumTaskForceStrength, tfstrength, out tfstrength);

            if (tfstrength >= MinimumTaskForceStrength && ourAvailableStrength >= 20f)
            {
                StartingStrength = tfstrength;
                CreateFleet(elTaskForce, potentialAssaultShips, potentialTroops, EnemyStrength, closestAO,null, "Exploration Force");
                
            }
            

        }

        private void RequisitionForces()
        {
            IOrderedEnumerable<AO> sorted = Owner.GetGSAI().AreasOfOperations
                .OrderByDescending(ao => ao.GetOffensiveForcePool().Sum(strength => strength.GetStrength()) >= MinimumTaskForceStrength)
                .ThenBy(ao => Vector2.Distance(AO, ao.Center));

            if (sorted.Count<AO>() == 0)
                return;

            AO ClosestAO = sorted.First<AO>();
            EnemyStrength = Owner.GetGSAI().ThreatMatrix.PingRadarStr(AO, AORadius,Owner);

            MinimumTaskForceStrength = EnemyStrength;
            if (MinimumTaskForceStrength < 1f)
            {
                EndTask();
                return;
            }

            if (ClosestAO.GetCoreFleet().FleetTask == null && ClosestAO.GetCoreFleet().GetStrength() > MinimumTaskForceStrength)
            {
                WhichFleet = ClosestAO.WhichFleet;
                ClosestAO.GetCoreFleet().FleetTask = this;
                ClosestAO.GetCoreFleet().TaskStep = 1;
                IsCoreFleetTask = true;
                Step = 1;
            }
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