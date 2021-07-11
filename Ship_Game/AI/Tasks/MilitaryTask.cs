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
        [Serialize(4)] public TaskType Type;
        [Serialize(5)] public Vector2 AO;
        [Serialize(6)] public float AORadius;
        [Serialize(7)] public float EnemyStrength;
        [Serialize(8)] public float MinimumTaskForceStrength;
        [Serialize(9)] public int WhichFleet = -1;
        [Serialize(10)] public int NeededTroopStrength;
        [Serialize(11)] public int Priority = 5;
        [Serialize(12)] public int TaskBombTimeNeeded;
        [Serialize(13)] public Guid TargetShipGuid = Guid.Empty;
        [Serialize(14)] public Guid TaskGuid = Guid.NewGuid();
        [Serialize(15)] public Array<Vector2> PatrolPoints;
        [Serialize(16)] public int TargetEmpireId = -1;
        [Serialize(17)] public int TargetPlanetWarValue; // Used for doom fleets to affect colony lost value in war

        [XmlIgnore] [JsonIgnore] public bool QueuedForRemoval;

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
            if (Type != TaskType.ClearAreaOfEnemies) return false;
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
                Type                     = TaskType.DefendClaim,
                AORadius                 = targetPlanet.ParentSystem.Radius,
                MinimumTaskForceStrength = minStrength,
                Owner                    = owner,
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
                Type             = TaskType.Exploration,
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
                Type                     = TaskType.GuardBeforeColonize,
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
                TargetPlanet   = targetPlanet,
                AO             = targetPlanet.Center,
                Type           = TaskType.ReclaimPlanet,
                AORadius       = targetPlanet.ParentSystem.Radius,
                Owner          = owner,
                WhichFleet     = fleetId,
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
                Type                     = TaskType.AssaultPirateBase,
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
                AO             = planet.Center,
                AORadius       = 10000f,
                WhichFleet     = fleetId,
                TargetPlanet   = planet,
                Owner          = owner,
                Type           = TaskType.DefendPostInvasion,
                NeedEvaluation = false
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
            militaryTask.Type = TaskType.RemnantEngagement;
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
                Type                     = TaskType.DefendVsRemnants,
                TargetEmpire             = EmpireManager.Remnants,
            };

            return militaryTask;
        }

        public MilitaryTask(Empire owner, Vector2 center, float radius, SolarSystem system, float strengthWanted, TaskType taskType)
        {
            Empire dominant = owner.GetEmpireAI().ThreatMatrix.GetDominantEmpireInSystem(system);

            AO                       = center;
            AORadius                 = radius;
            Type                     = taskType;
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

            Type                     = TaskType.AssaultPlanet;
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

        public override string ToString() => $"{Type} {TargetPlanet} Priority {Priority}";

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
                DisbandTaskForce(Fleet);
                return;
            }

            if (Fleet != null && !Fleet.IsCoreFleet && !FleetNeededForNextTask)
                Owner.GetEmpireAI().UsedFleets.Remove(WhichFleet);

            if (FindClosestAO() == null)
            {
                if (Fleet.IsCoreFleet || Owner == Empire.Universe.player)
                    return;

                DisbandTaskForce(Fleet);
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
                DisbandTaskForce(Fleet);

            if (Type == TaskType.Exploration && TargetPlanet != null)
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

        /// <summary>
        /// Fleets will add back to the force pool when they are reset.
        /// Non fleet ships need to be manually sent back
        /// </summary>
        /// <param name="fleet"></param>
        public void DisbandTaskForce(Fleet fleet)
        {
            for (int i = 0; i < TaskForce.Count; i++)
            {
                var ship = TaskForce[i];
                if (ship.fleet == null)
                    Owner.AddShipToManagedPools(ship);
            }

            Fleet?.Reset();
            TaskForce.Clear();
        }

        bool RoomForMoreFleets()
        {
            float divisor = 0;
            if (Type == TaskType.ClearAreaOfEnemies)
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
                        Log.Warning($"MilitaryTask Evaluate found task with missing fleet {Type}");
                        EndTask();
                        return false;
                    }
                }
            }

            NeedEvaluation = Fleet == null;

            if (!NeedEvaluation)
                return false;

            switch (Type)
            {
                case TaskType.StrikeForce:
                case TaskType.StageFleet:          RequisitionAssaultForces(strike: true);                 break;
                case TaskType.AssaultPlanet:       RequisitionAssaultForces(Type == TaskType.StrikeForce); break;
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

                if (Type == TaskType.Exploration)
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

        public void IncreaseColonyLostValueByBombing()
        {
            if (!TargetEmpire.isFaction
                && TargetEmpire.IsAtWarWith(Owner)
                && TargetEmpire.TryGetActiveWars(out Array<War> wars))
            {
                var war = wars.Find(w => w.Them == Owner);
                if (war != null)
                    war.ColoniesValueLost += TargetPlanetWarValue;
            }
        }

        public void Prioritize(int numWars)
        {
            int priority;
            switch (Type)
            {
                default:                                  priority = 5;                               break;
                case TaskType.StageFleet:                 priority = 2 * (numWars * 2).LowerBound(1); break;
                case TaskType.GuardBeforeColonize:        priority = 3 + numWars;                     break;
                case TaskType.DefendVsRemnants:           priority = 0;                               break;
                case TaskType.CohesiveClearAreaOfEnemies:
                case TaskType.ClearAreaOfEnemies:         priority = 1;                               break;
                case TaskType.StrikeForce:                priority = 2;                               break;
                case TaskType.ReclaimPlanet:
                case TaskType.AssaultPlanet:              priority = 5;                               break;
                case TaskType.GlassPlanet:                priority = 5;                               break;
                case TaskType.Exploration:                priority = GetExplorationPriority();        break;
                case TaskType.DefendClaim:                priority = 5 + numWars * 2;                 break;
                case TaskType.AssaultPirateBase:          priority = GetAssaultPirateBasePriority();  break;
            }

            if (TargetEmpire == EmpireManager.Player)
                priority -= Owner.DifficultyModifiers.WarTaskPriorityMod;

            Priority = priority;

            // Local Function
            int GetAssaultPirateBasePriority()
            {
                Empire enemy = TargetEmpire;
                if (enemy?.WeArePirates == true && !enemy.Pirates.PaidBy(Owner))
                    return (Pirates.MaxLevel - enemy.Pirates.Level).LowerBound(3);

                return 10;
            }

            int GetExplorationPriority()
            {
                int initial = TargetPlanet.ParentSystem.HasPlanetsOwnedBy(Owner) ? 4 : 5;
                return initial + numWars + (MinimumTaskForceStrength > 100 ? 1 : 0);
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
            // If you add new task, make sure to have them added to the PrioritizeTask method in RunMilitaryPlanner
            // And to GetTaskCategory (to determine if it is a war task).
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
            StageFleet,
            ReclaimPlanet
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
            switch (Type)
            {
                case TaskType.StageFleet:
                case TaskType.StrikeForce:
                case TaskType.AssaultPlanet:
                case TaskType.ReclaimPlanet:
                case TaskType.DefendPostInvasion:
                case TaskType.GlassPlanet:
                case TaskType.CorsairRaid:
                case TaskType.ClearAreaOfEnemies:
                case TaskType.CohesiveClearAreaOfEnemies: taskCat |= TaskCategory.War; break;
                case TaskType.AssaultPirateBase:
                case TaskType.Patrol:
                case TaskType.DefendVsRemnants:
                case TaskType.RemnantEngagement:
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
            debug.AddLine($"({Priority}) -- {Type}, {target}, {fleet}", color);
            debug.AddLine($" Str Needed: ({MinimumTaskForceStrength})", color);
        }
    }
}