using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Debug;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI.StrategyAI.WarGoals;

namespace Ship_Game.AI.Tasks
{
    public partial class MilitaryTask
    {
        [Serialize(0)] public bool IsCoreFleetTask;
        [Serialize(1)] public Guid GoalGuid;
        [Serialize(2)] public bool NeedEvaluation = true;
        [Serialize(3)] public Guid TargetPlanetGuid = Guid.Empty;
        [Serialize(4)] public TaskType type;
        [Serialize(5)] public Vector2 AO;
        [Serialize(6)] public float AORadius;
        [Serialize(7)] public float EnemyStrength;
        [Serialize(8)] public float MinimumTaskForceStrength;
        [Serialize(9)] public int WhichFleet = -1;
        [Serialize(10)] public int NeededTroopStrength;
        [Serialize(11)] public int Priority;
        [Serialize(12)] public int TaskBombTimeNeeded;
        [Serialize(13)] public Guid TargetShipGuid = Guid.Empty;
        [Serialize(14)] public Guid TaskGuid = Guid.NewGuid();
        [Serialize(15)] public Array<Vector2> PatrolPoints;
        [Serialize(16)] public int TargetEmpireId = -1;

        [XmlIgnore] [JsonIgnore] public bool QueuedForRemoval;
        [XmlIgnore] [JsonIgnore] public Campaign WarCampaign = null;

        // FB - Do not disband the fleet, it is held for a new task - this is done at once and does not need save
        [XmlIgnore] [JsonIgnore] public bool FleetNeededForNextTask { get; private set; }

        [XmlIgnore] [JsonIgnore] public Planet TargetPlanet { get; private set; }
        [XmlIgnore] [JsonIgnore] public SolarSystem TargetSystem { get; private set; }
        [XmlIgnore] [JsonIgnore] public Ship TargetShip { get; private set; }
        [XmlIgnore] [JsonIgnore] Empire Owner;
        [XmlIgnore] [JsonIgnore] Array<Ship> TaskForce = new Array<Ship>();
        [XmlIgnore] [JsonIgnore] public Fleet Fleet => Owner?.GetFleetOrNull(WhichFleet);
        [XmlIgnore] [JsonIgnore] public Planet RallyPlanet => GetRallyPlanet();
        [XmlIgnore] [JsonIgnore] public AO RallyAO;
        [XmlIgnore] [JsonIgnore] public Goal Goal;

        [XmlIgnore] [JsonIgnore] public Empire TargetEmpire
        {
            get => TargetEmpireId < 0 ? null : EmpireManager.GetEmpireById(TargetEmpireId);
            set
            {
                if (value != null) 
                    TargetEmpireId = value.Id;
            }
        }

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

        private MilitaryTask()
        {
        }

        public static MilitaryTask CreateCoreSubTask(Vector2 ao, float aoRadius)
        {
            var task = new MilitaryTask
            {
                AO       = ao,
                AORadius = aoRadius
            };

            return task;
        }

        public static MilitaryTask CreateClaimTask(Empire owner, Planet targetPlanet, float minStrength)
        {
            Empire dominant  = owner.GetEmpireAI().ThreatMatrix.GetDominantEmpireInSystem(targetPlanet.ParentSystem);
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
                Priority                 = 5,
                TargetEmpire             = dominant
            };

            return militaryTask;
        }

        public static MilitaryTask CreateExploration(Planet targetPlanet, Empire owner)
        {
            Empire dominant  = owner.GetEmpireAI().ThreatMatrix.GetDominantEmpireInSystem(targetPlanet.ParentSystem);
            var militaryTask = new MilitaryTask
            {
                AO               = targetPlanet.Center,
                AORadius         = 50000f,
                type             = TaskType.Exploration,
                Priority         = 5,
                Owner            = owner,
                TargetPlanet     = targetPlanet,
                TargetPlanetGuid = targetPlanet.guid,
                TargetEmpire     = dominant
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
                MinimumTaskForceStrength = (owner.CurrentMilitaryStrength / 1000).LowerBound(50),
                Owner                    = owner,
                Priority                 = 0
            };

            return militaryTask;
        }

        public static MilitaryTask CreateReclaimTask(Empire owner, Planet targetPlanet, int fleetId)
        {
            var militaryTask = new MilitaryTask
            {
                TargetPlanet = targetPlanet,
                AO           = targetPlanet.Center,
                type         = TaskType.AssaultPlanet,
                AORadius     = targetPlanet.ParentSystem.Radius,
                Owner        = owner,
                WhichFleet   = fleetId,
                Priority     = 5,
                NeedEvaluation = false // We have ships
            };

            return militaryTask;
        }

        public static MilitaryTask CreateAssaultPirateBaseTask(Ship targetShip, Empire empire)
        {
            var threatMatrix = empire.GetEmpireAI().ThreatMatrix;
            float pingStr    = threatMatrix.PingRadarStr(targetShip.Center, 20000, empire);
            var militaryTask = new MilitaryTask
            {
                TargetShip               = targetShip,
                AO                       = targetShip.Center,
                type                     = TaskType.AssaultPirateBase,
                AORadius                 = 20000,
                Owner                    = empire,
                EnemyStrength            = targetShip.BaseStrength,
                TargetShipGuid           = targetShip.guid,
                MinimumTaskForceStrength = (targetShip.BaseStrength + pingStr) * empire.GetFleetStrEmpireMultiplier(targetShip.loyalty),
                TargetEmpire             = targetShip.loyalty
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
                TargetPlanet = planet,
                Owner        = owner,
                type         = TaskType.DefendPostInvasion
            };

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
            float strMulti   = owner.GetFleetStrEmpireMultiplier(EmpireManager.Remnants);
            var militaryTask = new MilitaryTask
            {
                AO                       = planet.Center,
                AORadius                 = 50000f,
                TargetPlanet             = planet,
                MinimumTaskForceStrength = str * strMulti,
                EnemyStrength            = str,
                Priority                 = 0,
                Owner                    = owner,
                type                     = TaskType.DefendVsRemnants,
                TargetEmpire             = EmpireManager.Remnants,
            };

            return militaryTask;
        }

        public MilitaryTask(Empire owner, Vector2 center, float radius, SolarSystem system, float strengthWanted, TaskType taskType)
        {
            Empire dominant = owner.GetEmpireAI().ThreatMatrix.GetDominantEmpireInSystem(system);

            AO                       = center;
            AORadius                 = radius;
            type                     = taskType;
            MinimumTaskForceStrength = strengthWanted.LowerBound(500) * owner.GetFleetStrEmpireMultiplier(dominant);
            EnemyStrength            = MinimumTaskForceStrength;
            TargetSystem             = system;
            Owner                    = owner;

            if (dominant != null)
                TargetEmpire = dominant;
        }

        public MilitaryTask(Planet target, Empire owner)
        {
            float radius     = 5000f;
            float strWanted  = GetKnownEnemyStrInClosestSystems(target.ParentSystem, owner, target.Owner)
                               + target.BuildingGeodeticOffense;

            type                     = TaskType.AssaultPlanet;
            TargetPlanet             = target;
            TargetPlanetGuid         = target.guid;
            AO                       = target.Center;
            AORadius                 = radius;
            Owner                    = owner;
            TargetEmpire             = target.Owner;
            MinimumTaskForceStrength = strWanted.LowerBound(owner.KnownEmpireOffensiveStrength(target.Owner) / 10) 
                                       * owner.GetFleetStrEmpireMultiplier(target.Owner);
        }

        float GetKnownEnemyStrInClosestSystems(SolarSystem system, Empire owner, Empire enemy)
        {
            var threatMatrix = owner.GetEmpireAI().ThreatMatrix;
            float strWanted  = threatMatrix.PingRadarStr(system.Position, system.Radius, owner);

            for (int i = 0; i < system.FiveClosestSystems.Count; i++)
            {
                SolarSystem closeSystem = system.FiveClosestSystems[i];
                strWanted += owner.KnownEnemyStrengthIn(closeSystem, 
                    p => p.GetEmpire() == enemy && !p.Ship?.IsPlatformOrStation == true);
            }

            return strWanted;
        }

        public void FlagFleetNeededForAnotherTask()
        {
            FleetNeededForNextTask = true;
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
            if (Owner == null)
                return;

            Owner.GetEmpireAI().QueueForRemoval(this);


            if (Owner.isFaction)
            {
                FactionEndTask();
                return;
            }

            if (WhichFleet == -1 || Fleet == null)
            {
                foreach (var ship in TaskForce)
                    Owner.AddShipToManagedPools(ship);
                
                TaskForce.Clear();
                return;
            }

            if (Fleet != null && !Fleet.IsCoreFleet && !FleetNeededForNextTask)
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

            if (!FleetNeededForNextTask)
                DisbandFleet(Fleet);

            if (type == TaskType.Exploration && TargetPlanet != null)
                RemoveTaskTroopsFromPlanet();
        }

        private void RemoveTaskTroopsFromPlanet()
        {
            Array<Troop> toLaunch = new Array<Troop>();
            if (TargetPlanet.ParentSystem.DangerousForcesPresent(Owner))
                return;

            for (int index = TargetPlanet.TroopsHere.Count - 1; index >= 0; index--)
            {
                Troop t = TargetPlanet.TroopsHere[index];
                if (t.Loyalty == Owner)
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

        bool RoomForMoreFleets()
        {
            float divisor = 0;
            if (type == TaskType.ClearAreaOfEnemies)
                divisor = 1;
            else if (GetTaskCategory() == TaskCategory.War)
                divisor = 5;
            else if (Owner.IsAtWarWithMajorEmpire)
                divisor = 10;
            float availableFleets = Owner.AIManagedShips.CurrentUseableFleets.LowerBound(1);
            float fleets = Owner.AIManagedShips.InitialReadyFleets.LowerBound(1);
            float usedFleets = fleets - availableFleets;
            return  fleets / divisor > usedFleets;
        }

        public bool Evaluate(Empire e)
        {
            Owner = e;
            if (WhichFleet > - 1)
            {
                if (!e.GetFleetsDict().TryGetValue(WhichFleet, out Fleet fleet) || fleet == null || fleet.Ships.Count == 0)
                {
                    if (fleet?.IsCoreFleet != true)
                    {
                        Log.Warning($"MilitaryTask Evaluate found task with missing fleet {type}");
                        EndTask();
                        return false;
                    }
                }
            }

            if (!NeedEvaluation)
                return false;

            switch (type)
            {
                case TaskType.StrikeForce:
                case TaskType.StageFleet:          RequisitionAssaultForces(strike: true);                 break;
                case TaskType.AssaultPlanet:       RequisitionAssaultForces(type == TaskType.StrikeForce); break;
                case TaskType.GuardBeforeColonize: RequisitionGuardBeforeColonize();                       break;
                case TaskType.AssaultPirateBase:   RequisitionAssaultPirateBase();                         break;
                case TaskType.DefendVsRemnants:    RequisitionDefendVsRemnants();                          break;
                case TaskType.ClearAreaOfEnemies:  RequisitionDefenseForce();                              break;
                case TaskType.Exploration:         RequisitionExplorationForce();                          break;
                case TaskType.DefendClaim:         RequisitionClaimForce();                                break;
                case TaskType.GlassPlanet:         RequisitionGlassForce();                                break;
            }

            return true;
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
            DefendClaim,
            DefendPostInvasion,
            GlassPlanet,
            AssaultPirateBase,
            Patrol,
            RemnantEngagement,
            DefendVsRemnants,
            GuardBeforeColonize,
            StrikeForce,
            StageFleet
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
            
            if (WarCampaign?.GetWarType() == WarType.EmpireDefense)
                taskCat |= TaskCategory.Domestic;
            else if (WarCampaign != null)
                taskCat |= TaskCategory.War;

            switch (type)
            {
                case TaskType.StrikeForce:
                case TaskType.AssaultPlanet:
                case TaskType.DefendPostInvasion:
                case TaskType.GlassPlanet:
                case TaskType.CorsairRaid:        taskCat |= TaskCategory.War; break;
                case TaskType.AssaultPirateBase:
                case TaskType.CohesiveClearAreaOfEnemies:
                case TaskType.Patrol:
                case TaskType.DefendVsRemnants:
                case TaskType.RemnantEngagement:
                case TaskType.ClearAreaOfEnemies:
                case TaskType.Resupply:           taskCat |= TaskCategory.Domestic; break;
                case TaskType.DefendClaim:
                case TaskType.GuardBeforeColonize:
                case TaskType.Exploration:        taskCat |= TaskCategory.Expansion; break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return taskCat;
        }

        public bool IsWarTask => GetTaskCategory().HasFlag(TaskCategory.War);

        public void RestoreFromSaveNoUniverse(Empire e, UniverseData data)
        {
            data.FindPlanet(TargetPlanetGuid, out Planet p);
            data.FindShip(TargetShipGuid, out Ship ship);
            RestoreFromSaveFromSave(e, ship, p);

            foreach (var system in data.SolarSystemsList)
            {
                if (IsTaskAOInSystem(system))
                    TargetSystem = system;
            }
        }

        public void RestoreFromSaveFromUniverse(Empire e)
        {
            Ship ship = Empire.Universe.Objects.FindShip(TargetShipGuid);
            var planet = Planet.GetPlanetFromGuid(TargetPlanetGuid);
            RestoreFromSaveFromSave(e, ship, planet);

            foreach (var system in Empire.Universe.SolarSystemDict.Values)
            {
                if (IsTaskAOInSystem(system))
                    TargetSystem = system;
            }
        }

        void RestoreFromSaveFromSave(Empire e, Ship ship, Planet p)
        {
            SetEmpire(e);

            if (PatrolPoints == null) PatrolPoints = new Array<Vector2>();

            if (p != null)
                SetTargetPlanet(p);
            
            if (ship != null)
                SetTargetShip(ship);

            foreach (Goal g in e.GetEmpireAI().Goals)
            {
                if (g.guid == GoalGuid)
                {
                    Goal = g;
                    break;
                }
            }

            if (WhichFleet != -1)
            {
                if (e.GetFleetsDict().TryGetValue(WhichFleet, out Fleet fleet))
                    fleet.FleetTask = this;
                else WhichFleet = 0;
            }
        }

        public void DebugDraw(ref DebugTextBlock debug)
        {
            Color color   = TargetEmpire?.EmpireColor ?? Owner.EmpireColor;
            string fleet  = Fleet != null ? $"Fleet Step: {Fleet.TaskStep}" : "No Fleet yet";
            string target = TargetPlanet?.Name ?? "";
            debug.AddLine($"({Priority}) -- {type}, {target}, {fleet}", color);
            debug.AddLine($" Str Needed: ({MinimumTaskForceStrength})", color);
        }
    }
}