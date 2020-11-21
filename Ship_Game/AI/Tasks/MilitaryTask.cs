using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Debug;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Threading.Tasks;
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
        [Serialize(17)] public int TaskBombTimeNeeded;
        [Serialize(18)] public Guid TargetShipGuid = Guid.Empty;
        [Serialize(19)] public Guid TaskGuid       = Guid.NewGuid();
        [Serialize(20)] public Array<Vector2> PatrolPoints;

        [XmlIgnore] [JsonIgnore] public bool QueuedForRemoval;

        [XmlIgnore] [JsonIgnore] public Planet TargetPlanet { get; private set; }
        [XmlIgnore] [JsonIgnore] public SolarSystem TargetSystem { get; private set; }
        [XmlIgnore] [JsonIgnore] public Ship TargetShip { get; private set; }
        [XmlIgnore] [JsonIgnore] Empire Owner;
        [XmlIgnore] [JsonIgnore] Array<Ship> TaskForce = new Array<Ship>();
        [XmlIgnore] [JsonIgnore] public Fleet Fleet    => Owner?.GetFleetOrNull(WhichFleet);
        [XmlIgnore] [JsonIgnore] public Planet RallyPlanet => GetRallyPlanet();
        [XmlIgnore] [JsonIgnore] public AO RallyAO;

        public bool IsTaskAOInSystem(SolarSystem system)
        {
            if (TargetSystem != null) return system == TargetSystem;
            if (!system.Position.InRadius(AO, AORadius)) return false;
            TargetSystem = system;
            return true;
        }

        public bool IsDefendingSystem(SolarSystem system)
        {
            if (type != TaskType.ClearAreaOfEnemies) return false;
            return IsTaskAOInSystem(system);
        }

        public MilitaryTask()
        {
        }

        public static MilitaryTask CreateClaimTask(Empire owner, Planet targetPlanet, float minStrength)
        {
            var militaryTask = new MilitaryTask
            {
                TargetPlanet             = targetPlanet,
                AO                       = targetPlanet.Center,
                type                     = TaskType.DefendClaim,
                AORadius                 = targetPlanet.ParentSystem.Radius,
                MinimumTaskForceStrength = minStrength,
                Owner                    = owner,
                // need to adjust this by personality.
                // this task will increase in priority as time goes by. 
                // this will generally only have an effect during war. 
                Priority                 = 20
            };

            return militaryTask;
        }

        public static MilitaryTask CreateGuardTask(Empire owner, Planet targetPlanet)
        {
            var militaryTask = new MilitaryTask
            {
                TargetPlanet             = targetPlanet,
                AO                       = targetPlanet.Center,
                type                     = TaskType.GuardBeforeColonize,
                AORadius                 = targetPlanet.ParentSystem.Radius,
                MinimumTaskForceStrength = (owner.CurrentMilitaryStrength / 1000).LowerBound(100),
                Owner                    = owner,
                Priority                 = 0
            };

            return militaryTask;
        }

            public static MilitaryTask CreateAssaultPirateBaseTask(Ship targetShip, float minStrength)
        {
            var militaryTask = new MilitaryTask
            {
                TargetShip               = targetShip,
                AO                       = targetShip.Center,
                type                     = TaskType.AssaultPirateBase,
                AORadius                 = 20000,
                EnemyStrength            = targetShip.BaseStrength,
                TargetShipGuid           = targetShip.guid,
                MinimumTaskForceStrength = minStrength
            };
            return militaryTask;
        }

        public static MilitaryTask CreateGlassPlanetTask(Planet targetPlanet, float minStrength)
        {
            var militaryTask = new MilitaryTask
            {
                TargetPlanet             = targetPlanet,
                AO                       = targetPlanet.Center,
                type                     = TaskType.GlassPlanet,
                AORadius                 = targetPlanet.GravityWellRadius,
                MinimumTaskForceStrength = minStrength
            };
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

        public static MilitaryTask CreateRemnantEngagement(Planet planet, Empire owner)
        {
            var militaryTask = new MilitaryTask
            {
                AO           = planet.Center,
                AORadius     = 50000f,
                TargetPlanet = planet
            };

            militaryTask.SetEmpire(owner);
            militaryTask.type = TaskType.RemnantEngagement;
            return militaryTask;
        }

        public static MilitaryTask CreateDefendVsRemnant(Planet planet, Empire owner, float str)
        {
            var militaryTask = new MilitaryTask
            {
                AO                       = planet.Center,
                AORadius                 = 50000f,
                TargetPlanet             = planet,
                MinimumTaskForceStrength = str,
                EnemyStrength            = str,
                Priority                 = 0
            };

            militaryTask.SetEmpire(owner);
            militaryTask.type = TaskType.DefendVsRemnants;
            return militaryTask;
        }

        public MilitaryTask(AO ao, Array<Vector2> patrolPoints)
        {
            AO              = ao.Center;
            AORadius        = ao.Radius;
            type            = TaskType.CohesiveClearAreaOfEnemies;
            PatrolPoints    = patrolPoints;
        }

        public MilitaryTask(Vector2 center, float radius, SolarSystem system, float strengthWanted, TaskType taskType)
        {
            AO                       = center;
            AORadius                 = radius;
            type                     = taskType;
            MinimumTaskForceStrength = strengthWanted;
            EnemyStrength            = strengthWanted;
            TargetSystem             = system;
        }

        public MilitaryTask(Planet target, Empire owner)
        {
            var threatMatrix = owner.GetEmpireAI().ThreatMatrix;
            float radius     = 3500f;
            float strWanted  = threatMatrix.PingRadarStr(target.Center, radius, owner);
            strWanted       += target.BuildingGeodeticCount;

            type                     = TaskType.AssaultPlanet;
            TargetPlanet             = target;
            TargetPlanetGuid         = target.guid;
            AO                       = target.Center;
            AORadius                 = radius;
            Owner                    = owner;
            MinimumTaskForceStrength = strWanted * owner.DifficultyModifiers.TaskForceStrength;
        }

        public MilitaryTask(Empire owner)
        {
            Owner = owner;
        }

        public void ChangeTargetPlanet(Planet planet)
        {
            TargetPlanet     = planet;
            TargetPlanetGuid = planet.guid;
            AO               = planet.Center;
        }

        public void ChangeAO(Vector2 position)
        {
            AO = position;
        }

        public override string ToString() => $"{type} {TargetPlanet} Priority {Priority}";

        public void EndTask()
        {
            Debug_TallyFailedTasks();
            if (Owner == null)
                return;

            Owner.GetEmpireAI().QueueForRemoval(this);


            if (Owner.isFaction)
            {
                FactionEndTask();
                return;
            }

            ClearHoldOnGoal();

            if (WhichFleet == -1 || Fleet == null)
            {
                Owner.Pool.ForcePoolAdd(TaskForce);
                TaskForce.Clear();
                return;
            }

            if (Fleet != null && !Fleet.IsCoreFleet)
                Owner.GetEmpireAI().UsedFleets.Remove(WhichFleet);

            if (FindClosestAO() == null)
            {
                if (Fleet.IsCoreFleet || Owner == Empire.Universe.player)
                    return;

                DisbandFleet(Fleet);
                return;
            }

            if (IsCoreFleetTask)
            {
                ClearCoreFleetTask();
                return;
            }

            if (Fleet.IsCoreFleet || Owner.isPlayer)
                return;

            DisbandFleet(Fleet);

            if (type == TaskType.Exploration && TargetPlanet != null)
                RemoveTaskTroopsFromPlanet();
        }

        private void RemoveTaskTroopsFromPlanet()
        {
            Array<Troop> toLaunch = new Array<Troop>();
            for (int index = TargetPlanet.TroopsHere.Count - 1; index >= 0; index--)
            {
                Troop t = TargetPlanet.TroopsHere[index];
                if (t.Loyalty != Owner
                    || TargetPlanet.SpaceCombatNearPlanet
                    || t.AvailableAttackActions == 0
                    || t.MoveTimer > 0)
                {
                    continue;
                }

                toLaunch.Add(t);
            }

            foreach (Troop t in toLaunch)
            {
                Ship troopship = t.Launch();
                troopship?.AI.OrderRebaseToNearest();
            }

            toLaunch.Clear();
        }

        private void ClearCoreFleetTask()
        {
            for (int i = 0; i < Fleet.Ships.Count; i++)
            {
                Ship ship = Fleet.Ships[i];
                ship.AI.CombatState = ship.shipData.CombatState;
                ship.AI.ClearOrders();
                ship.HyperspaceReturn();
            }

            Fleet.FleetTask = null;
        }

        public void DisbandFleet(Fleet fleet)
        {
            Fleet.Reset();
            TaskForce.Clear();
        }

        private void Debug_TallyFailedTasks()
        {
            DebugInfoScreen.CanceledMtasksCount++;
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
            for (int i = 0; i < HeldGoals.Count; i++)
            {
                Guid goalGuid = HeldGoals[i];
                var gs = Owner.GetEmpireAI().Goals;
                for (int x = 0; x < gs.Count; x++)
                {
                    Goal g = gs[x];
                    if (g.guid == goalGuid) g.Held = false;
                }
            }
        }

        public void EndTaskWithMove()
        {
            Owner.GetEmpireAI().QueueForRemoval(this);

            ClearHoldOnGoal();


            if (WhichFleet != -1)
            {
                if (IsCoreFleetTask)
                {
                    AO closestAo = Owner.GetEmpireAI().AreasOfOperations.FindMin(ao => AO.SqDist(ao.Center));
                    Fleet?.ClearFleetTask();
                    Fleet?.MoveToDirectly(closestAo.Center, Vectors.Up);
                }
                else
                {
                    Owner.Pool.ForcePoolAdd(Fleet.Ships);
                    TaskForce.Clear();
                    Owner.GetEmpireAI().UsedFleets.Remove(WhichFleet);
                    Fleet?.Reset();
                }
            }
        }

        public void Evaluate(Empire e)
        {
            Owner = e;
            if (WhichFleet >-1)
            {
                if (!e.GetFleetsDict().TryGetValue(WhichFleet, out Fleet fleet) || fleet == null || fleet.Ships.Count == 0)
                {
                    if (fleet?.IsCoreFleet != true)
                    {
                        Log.Warning($"MilitaryTask Evaluate found task with missing fleet {type}");
                        EndTask();
                        return;
                    }
                }
            }
            switch (type)
            {
                case TaskType.GuardBeforeColonize:
                    switch (Step)
                    {
                        case 0:
                            RequisitionGuardBeforeColonize();
                            break;
                    }

                    break;
                case TaskType.AssaultPirateBase:
                    switch (Step)
                    {
                        case 0:
                            RequisitionAssaultPirateBase();
                            break;
                    }

                    break;
                case TaskType.DefendVsRemnants:
                    switch (Step)
                    {
                        case 0:
                            RequisitionDefendVsRemnants();
                            break;
                    }

                    break;
                case TaskType.ClearAreaOfEnemies:
                    {
                        if (Step == 0)
                        {
                            if (EnemyStrength < 1)
                            {
                                EndTask();
                                break;
                            }
                            RequisitionDefenseForce();
                        }
                        break;
                    }
                case TaskType.AssaultPlanet:
                    {
                        if (TargetPlanet.Owner == null || !Owner.IsEmpireHostile(TargetPlanet.Owner))
                            EndTask();

                        if (Step < 0)
                        {
                            Step++;
                            break;
                        }
                        if (Step == 0)
                        {
                            RequisitionAssaultForces();
                            if (Step == 0) Step = -1;
                        }
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
                case TaskType.CohesiveClearAreaOfEnemies:
                    {
                        if      (Step == 0) RequisitionCoreFleet();
                        else if (Step == 1) ExecuteAndAssess();
                        break;
                    }
                case TaskType.Exploration:
                    {
                        if (Owner.GetEmpireAI().TroopShuttleCapacity > 0)
                            if (Step == 0)
                            {
                                RequisitionExplorationForce();
                                if (Step < 1)
                                {
                                    Priority += Priority > 1 ? -1 : 20;
                                }
                            }
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
                                if (Owner.GetEmpireAI().TroopShuttleCapacity > 0)
                                {
                                    if (TargetPlanet.Owner != null && TargetPlanet.Owner != EmpireManager.Unknown)
                                    {
                                        Owner.GetRelations(TargetPlanet.Owner, out Relationship rel);

                                        if (rel != null && (!rel.AtWar && !rel.PreparingForWar))
                                        {
                                            EndTask();
                                            break;
                                        }
                                    }
                                    RequisitionClaimForce();
                                    Priority += Priority < 1 ? 20 : -1;

                                }
                                break;
                            case 1:
                                {
                                    var fleetDictionary = Owner.GetFleetsDict();
                                    if (fleetDictionary.TryGetValue(WhichFleet, out Fleet fleet))
                                    {
                                        if (fleet.Ships.Count == 0)
                                        {
                                            EndTask();
                                            return;
                                        }

                                        if (TargetPlanet.Owner != null)
                                        {
                                            Owner.GetRelations(TargetPlanet.Owner, out Relationship rel);
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

                                    if (TargetPlanet.Owner == null || TargetPlanet.Owner == EmpireManager.Unknown)
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

                                    Owner.GetRelations(TargetPlanet.Owner, out Relationship rel);
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
                case TaskType.GlassPlanet:
                    {
                        if (TargetPlanet.Owner == null || !Owner.IsEmpireHostile(TargetPlanet.Owner))
                            EndTask();

                        if (Step == 0) RequisitionGlassForce();
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
                        if (TargetPlanet.Owner != null && !Owner.IsEmpireHostile(TargetPlanet.Owner))
                            EndTask();
                        Planet p = TargetPlanet;
                        if (p.BuildingList.Find(relic => relic.EventHere) != null)
                            return;
                    }
                    else if (type == TaskType.AssaultPlanet)
                    {
                        if (TargetPlanet.Owner == null || Owner.IsEmpireHostile(TargetPlanet.Owner))
                            EndTask();
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
            for (int i = fleet.Ships.Count-1; i >= 0; --i)
            {
                Ship ship = fleet.Ships[i];
                // remove dead or scrapping ships
                if (!ship.Active || ship.InCombat && Step < 1 || ship.AI.State == AIState.Scrap)
                {
                    ship.ClearFleet();
                    //if (ship.Active && ship.AI.State != AIState.Scrap)
                    //    Owner.Pool.ForcePoolAdd(ship);
                }
                else
                {
                    currentStrength += ship.GetStrength();
                }
            }

            float currentEnemyStrength = Owner.GetEmpireAI().ThreatMatrix
                                        .StrengthOfHostilesInRadius(Owner, AO, AORadius);

            if (!fleet.CanTakeThisFight(currentEnemyStrength))
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
                        ship.ClearFleet();
                        ship.HyperspaceReturn();

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
            Owner.GetEmpireAI().QueueForRemoval(this);
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

        public void SetTargetShip(Ship ship)
        {
            TargetShip     = ship;
            TargetShipGuid = ship.guid;
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
            // The order of these can not change without breaking save games. 
            ClearAreaOfEnemies,
            Resupply,
            AssaultPlanet,
            CorsairRaid,
            CohesiveClearAreaOfEnemies,
            Exploration,
            DefendSystem,
            DefendClaim,
            DefendPostInvasion,
            GlassPlanet,
            AssaultPirateBase,
            Patrol,
            RemnantEngagement,
            DefendVsRemnants,
            GuardBeforeColonize
        }

        [Flags]
        public enum TaskCategory 
        {
            None = 0,
            FleetNeeded = 1 << 0,
            War         = 1 << 1,
            Domestic    = 1 << 2,
            Expansion   = 1 << 3
        }

        public TaskCategory GetTaskCategory()
        {
            TaskCategory taskCat = MinimumTaskForceStrength > 0 ? TaskCategory.FleetNeeded : TaskCategory.None;

            switch (type)
            {
                case TaskType.AssaultPlanet:
                case TaskType.DefendPostInvasion:
                case TaskType.GlassPlanet:
                case TaskType.ClearAreaOfEnemies:
                case TaskType.CorsairRaid:        taskCat |= TaskCategory.War; break;
                case TaskType.DefendSystem:
                case TaskType.CohesiveClearAreaOfEnemies:
                case TaskType.Patrol:
                case TaskType.Resupply:           taskCat |= TaskCategory.Domestic; break;
                case TaskType.DefendClaim:
                case TaskType.Exploration:        taskCat |= TaskCategory.Expansion; break;
            }
            return taskCat;
        }

        public void RestoreFromSaveFromSaveNoUniverse(Empire e, UniverseData data)
        {
            data.FindPlanet(TargetPlanetGuid, out Planet p);
            data.FindShip(TargetShipGuid, out Ship ship);
            RestoreFromSaveFromSave(e, ship, p);
        }

        public void RestoreFromSaveFromUniverse(Empire e)
        {
            Ship ship = Empire.Universe.Objects.FindShip(TargetShipGuid);
            var planet = Planet.GetPlanetFromGuid(TargetPlanetGuid);
            RestoreFromSaveFromSave(e, ship, planet);
        }

        void RestoreFromSaveFromSave(Empire e, Ship ship, Planet p)
        {
            SetEmpire(e);

            if (PatrolPoints == null) PatrolPoints = new Array<Vector2>();

            if (p != null)
                SetTargetPlanet(p);
            
            if (ship != null)
                SetTargetShip(ship);

            foreach (Guid guid in HeldGoals)
            {
                foreach (Goal g in e.GetEmpireAI().Goals)
                {
                    if (g.guid == guid)
                    {
                        g.Held = true;
                        break;
                    }
                }
            }

            if (WhichFleet != -1)
            {
                if (e.GetFleetsDict().TryGetValue(WhichFleet, out Fleet fleet))
                    fleet.FleetTask = this;
                else WhichFleet = 0;
            }
        }
    }
}