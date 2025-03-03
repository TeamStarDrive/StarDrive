using Ship_Game.Fleets;
using Ship_Game.Ships;
using System;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI.Tasks
{
    public partial class MilitaryTask
    {
        [StarData] public int FleetCount = 1;
        [StarData] public float Completeness = 1f;
        RequisitionStatus ReqStatus = RequisitionStatus.None;

        float GetHostileStrengthAtAO()
        {
            // RedFox: I removed ClampMin(minimumStrength) because this was causing infinite
            //         Create-Destroy-Create loop of ClearAreaOfEnemies MilitaryTasks
            //         Lets just report what the actual strength is.
            return GetHostileStrengthAt(AO, AORadius);
        }
        float GetHostileStrengthAt(Vector2 pos, float radius)
        {
            return Owner.AI.ThreatMatrix.GetHostileStrengthAt(pos, radius);
        }

        private int GetTargetPlanetGroundStrength(int minimumStrength)
        {
            return (int)TargetPlanet.GetGroundStrengthOther(Owner).LowerBound(minimumStrength);
        }

        Array<Troop> GetTroopsOnPlanets(Vector2 rallyPoint, float strengthNeeded, out float totalStrength,
                                        bool troopPriorityHigh)
        {
            var potentialTroops = new Array<Troop>();
            totalStrength = 0;
            if (strengthNeeded <= 0) return potentialTroops;

            var defenseDict     = Owner.AI.DefensiveCoordinator.DefenseDict;
            var troopSystems    = Owner.GetOwnedSystems().Sorted(dist => dist.Position.SqDist(rallyPoint));
            
            for (int x = 0; x < troopSystems.Length; x++)
            {
                SolarSystem system = troopSystems[x];

                if (!defenseDict.TryGetValue(system, out SystemCommander sysCom)
                    || !sysCom.IsEnoughTroopStrength && !troopPriorityHigh)
                {
                    continue;
                }
                
                for (int i = 0; i < system.PlanetList.Count; i++)
                {
                    Planet planet = system.PlanetList[i];
                    if (planet.Owner != Owner || planet.RecentCombat || planet.SpaceCombatNearPlanet) 
                        continue;

                    float planetMinStr                 = sysCom.PlanetTroopMin(planet);
                    float planetDefendingTroopStrength = planet.GetDefendingTroopCount();
                    float maxCanTake                   = troopPriorityHigh && !planet.System.HostileForcesPresent(Owner)
                        ? 5 
                        : (planetDefendingTroopStrength - (planetMinStr - 3)).LowerBound(0);

                    if (maxCanTake > 0)
                    {
                        potentialTroops.AddRange(planet.GetEmpireTroops(Owner, (int)maxCanTake));
                        totalStrength = potentialTroops.Sum(t => t.ActualStrengthMax);
                        if (strengthNeeded <= totalStrength)
                            break;
                    }
                }

                if (potentialTroops.Count > 50)
                {
                    break;
                }
            }

            return potentialTroops;
        }

        void CreateFleet(Array<Ship> ships, string name)
        {
            Fleet = Owner.CreateFleet(Owner.CreateFleetKey(), name);
            Fleet.FleetTask = this;
            foreach (Ship ship in ships)
            {
                ship.RemoveFromPoolAndFleet(clearOrders: true);
                Fleet.AddShip(ship);
            }

            Fleet.AutoArrange();
        }

        public void CreateRemnantFleet(Empire remnants, Ship ship, string name, out Fleet newFleet)
        {
            Fleet = newFleet = remnants.CreateFleet(remnants.CreateFleetKey(), name);
            newFleet.FleetTask = this;
            ship.AI.ClearOrders();
            newFleet.AddShip(ship);
        }

        private bool AreThereEnoughTroopsToInvade(float invasionTroopStrength, out Array<Troop> troopsOnPlanetNeeded,
                                                  Vector2 rallyPoint, bool troopPriorityHigh = false)
        {
            troopsOnPlanetNeeded = new Array<Troop>();
            if (NeededTroopStrength <= 0)
                return true;

            if (invasionTroopStrength < NeededTroopStrength)
            {
                troopsOnPlanetNeeded   = GetTroopsOnPlanets(rallyPoint, NeededTroopStrength, out float planetsTroopStrength, troopPriorityHigh);
                invasionTroopStrength += planetsTroopStrength;
            }
            return invasionTroopStrength >= NeededTroopStrength;
        }

        /// <summary>
        /// The concept here is to calculate how much bomb power is needed.
        /// this set into minutes of bombing. 
        /// </summary>
        /// <returns></returns>
        private int BombTimeNeeded()
        {
            //ground landing spots. if we dont have a significant space to land troops. create them. 
            int bombTime =  TargetPlanet.TotalDefensiveStrength / 20  ;

            //shields are a real pain. this may need a lot more code to deal with. 
            bombTime += (int)TargetPlanet.ShieldStrengthMax / 50;
            return bombTime;
        }

        void RequisitionDefenseForce()
        {
            if (!Owner.IsSystemUnderThreatForUs(TargetSystem) 
                && !Owner.IsSystemUnderThreatForAllies(TargetSystem)
                && !TargetSystem.DangerousForcesPresent(Owner))
            {
                EndTask();
            }

            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {Type}");

            AO = TargetPlanet?.Position ?? AO;
            EnemyStrength = Owner.KnownEnemyStrengthIn(TargetSystem).LowerBound(EnemyStrength);
            UpdateMinimumTaskForceStrength();
            if (CreateTaskFleet(Completeness) == RequisitionStatus.Complete)
                NeedEvaluation = false;
        }

        void RequisitionClaimForce()
        {
            if (TargetPlanet.Owner != null
                && TargetPlanet.Owner != Owner.Universe.Unknown && !TargetPlanet.Owner.data.IsRebelFaction)
            {
                Owner.GetRelations(TargetPlanet.Owner, out Relationship rel);
                if (rel != null && !rel.AtWar)
                {
                    EndTask();
                    return;
                }
            }

            if (AO.AlmostZero())
                throw new Exception("AO cannot be empty");

            if (Owner.ShipsReadyForFleet.CurrentUseableFleets < 0)
                return;

            int requiredTroopStrength = 0;
            if (TargetPlanet != null)
            {
                AO = TargetPlanet.Position;
                requiredTroopStrength = (int)TargetPlanet.GetGroundStrengthOther(Owner) - (int)TargetPlanet.GetGroundStrength(Owner);
                EnemyStrength = (Owner.KnownEnemyStrengthIn(TargetPlanet.System) + TargetPlanet.BuildingGeodeticOffense).LowerBound(100);
                UpdateMinimumTaskForceStrength();
            }

            if (requiredTroopStrength > 0) // If we need troops, we must have a minimum
                requiredTroopStrength = requiredTroopStrength.LowerBound(40);

            InitFleetRequirements(minFleetStrength: MinimumTaskForceStrength, minTroopStrength: requiredTroopStrength, minBombMinutes: 0);
            float battleFleetSize = MinimumTaskForceStrength < 100 ? 0 : 1f;
            if (CreateTaskFleet(Completeness * battleFleetSize, true) == RequisitionStatus.Complete)
                NeedEvaluation = false;
        }

        void RequisitionGuardBeforeColonize()
        {
            if (TargetPlanet.Owner != null ||
                Owner.KnownEnemyStrengthIn(TargetPlanet.System)
                > MinimumTaskForceStrength / Owner.GetFleetStrEmpireMultiplier(TargetEmpire))
            {
                EndTask();
                return;
            }

            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {Type}");

            InitFleetRequirements(MinimumTaskForceStrength, minTroopStrength: 0, minBombMinutes: 0);
            if (CreateTaskFleet(0.1f) == RequisitionStatus.Complete)
                NeedEvaluation = false;
        }

        void RequisitionAssaultPirateBase()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {Type}");

            if (TargetShip == null || !TargetShip.Active)
            {
                EndTask();
                return;
            }

            EnemyStrength = GetHostileStrengthAt(TargetShip.Position, 40000).LowerBound(100);

            UpdateMinimumTaskForceStrength();
            InitFleetRequirements(MinimumTaskForceStrength, minTroopStrength: 0, minBombMinutes: 0);
            if (CreateTaskFleet(Completeness, false) == RequisitionStatus.Complete)
                NeedEvaluation = false;
        }

        void RequisitionDefendVsRemnants()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {Type}");

            if (TargetPlanet.Owner != Owner)
            {
                EndTask();
                return;
            }

            UpdateMinimumTaskForceStrength();
            float divider = (Owner.TotalPopBillion / 30).LowerBound(1);
            InitFleetRequirements(MinimumTaskForceStrength / divider, minTroopStrength: 0, minBombMinutes: 0);

            if (CreateTaskFleet(Completeness) == RequisitionStatus.Complete)
            {
                Owner.AI.AddGoal(new DefendVsRemnants(TargetPlanet, TargetPlanet.Owner, Fleet));
                NeedEvaluation = false;
            }
        }

        void RequisitionExplorationForce()
        {
            if (TargetPlanet.Owner != null 
                && TargetPlanet.Owner != Owner
                && TargetPlanet.Owner != Owner.Universe.Unknown
                && !TargetPlanet.Owner.data.IsRebelFaction || !TargetPlanet.EventsOnTiles())
            {
                EndTask();
                return;
            }

            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {Type}");

            AO = TargetPlanet.Position;
            AORadius = TargetPlanet.System.Radius;
            float buildingGeodeticOffense = TargetPlanet.Owner != Owner ? TargetPlanet.BuildingGeodeticOffense : 0;
            EnemyStrength = GetHostileStrengthAtAO().LowerBound(100);

            float minTroopStr = TargetPlanet.Owner == null ? 10 : TargetPlanet.GetGroundStrengthOther(Owner).LowerBound(40);
            UpdateMinimumTaskForceStrength(buildingGeodeticOffense);
            InitFleetRequirements(MinimumTaskForceStrength, (int)minTroopStr, minBombMinutes: 0);
            float battleFleetSize = MinimumTaskForceStrength < 100 ? 0.1f : 1f;
            if (CreateTaskFleet(Completeness * battleFleetSize, true) == RequisitionStatus.Complete)
            {
                NeedEvaluation = false;
            }
        }

        void RequisitionAssaultForces()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {Type}");

            Empire enemy = TargetPlanet.Owner;
            if (enemy == null 
                || enemy == Owner 
                || Owner.IsPeaceTreaty(enemy) 
                || !Owner.IsEmpireHostile(enemy) && !Owner.IsPreparingForWarWith(enemy) )
            {
                EndTask();
                return;
            }

            AO = TargetPlanet.Position;
            float geodeticOffense = TargetPlanet.BuildingGeodeticOffense;
            float lowerBound = GetMinimumStrLowerBound(geodeticOffense, enemy);
            EnemyStrength = (GetHostileStrengthAtAO() + geodeticOffense).LowerBound(EnemyStrength);

            UpdateMinimumTaskForceStrength(lowerBound);
            InitFleetRequirements(MinimumTaskForceStrength, minTroopStrength: 40 ,minBombMinutes: 3);
            if (CreateTaskFleet(Completeness, true) == RequisitionStatus.Complete)
                NeedEvaluation = false;
        }

        void RequisitionGlassForce()
        {
            Empire enemy = TargetPlanet.Owner;
            if (!Owner.CanBuildBombers 
                || enemy == null 
                || enemy == Owner 
                || Owner.IsPeaceTreaty(TargetPlanet.Owner) 
                || !Owner.IsEmpireHostile(TargetPlanet.Owner))
            {
                EndTask();
                return;
            }

            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {Type}");

            AO = TargetPlanet.Position;
            float geodeticOffense = TargetPlanet.BuildingGeodeticOffense;
            float lowerBound      = GetMinimumStrLowerBound(geodeticOffense, enemy);
            EnemyStrength         = (GetHostileStrengthAtAO() + geodeticOffense).LowerBound(EnemyStrength);

            UpdateMinimumTaskForceStrength(lowerBound);
            int bombTimeNeeded = (TargetPlanet.TotalDefensiveStrength / 5).LowerBound(5) + (int)Math.Ceiling(TargetPlanet.PopulationBillion) * 2;
            InitFleetRequirements(minFleetStrength: MinimumTaskForceStrength, minTroopStrength: 0, minBombMinutes: bombTimeNeeded);

            if (CreateTaskFleet(Completeness) == RequisitionStatus.Complete)
                NeedEvaluation = false;
        }

        void RequisitionInvestigation()
        {
            UpdateMinimumTaskForceStrength();
            InitFleetRequirements(minFleetStrength: MinimumTaskForceStrength, minTroopStrength: 0, minBombMinutes: 0);
            if (CreateTaskFleet(Completeness) == RequisitionStatus.Complete)
                NeedEvaluation = false;
        }

        float GetMinimumStrLowerBound(float geodeticOffense, Empire enemy)
        {
            float initialStr     = geodeticOffense + Owner.KnownEmpireStrength(enemy) / 10;
            float enemyStrNearby = geodeticOffense + GetKnownEnemyStrInClosestSystems(TargetPlanet.System, Owner, enemy);
            // Todo advanced tasks also get multiplier;
            float multiplier     = Type == TaskType.StrikeForce || Type == TaskType.StageFleet ? 2 : 1; 
            return initialStr.LowerBound(enemyStrNearby).LowerBound(Owner.OffensiveStrength/10) * multiplier;
        }

        void UpdateMinimumTaskForceStrength(float lowerBound = 0)
        {
            if (EnemyStrength.AlmostEqual(100) && TargetShip != null)
                EnemyStrength += TargetShip.BaseStrength;

            MinimumTaskForceStrength = EnemyStrength.LowerBound(lowerBound) *  Owner.GetFleetStrEmpireMultiplier(TargetEmpire);
            MinimumTaskForceStrength = MinimumTaskForceStrength.UpperBound(MinimumStrengthUpperBoundByImportance());

            float lifeTimeMax = IsWarTask ? Owner.PersonalityModifiers.WarTasksLifeTime : 10;
            float goalLifeTime = Goal?.LifeTime ?? 0;
            MinimumTaskForceStrength *= (lifeTimeMax - goalLifeTime).LowerBound(lifeTimeMax*0.5f) / lifeTimeMax;
        }

        float MinimumStrengthUpperBoundByImportance()
        {
            float minStr;
            switch (Importance)
            {
                default:
                case MilitaryTaskImportance.Normal:    minStr = MinimumTaskForceStrength;       break;
                case MilitaryTaskImportance.Important: minStr = Owner.OffensiveStrength * 0.5f; break;
            }

            return minStr * GetBuildCapacityMultiplier();
        }

        // If our build capacity is better, limit the minimum str needed so we will have some left for more fleets
        float GetBuildCapacityMultiplier()
        {
            if (TargetEmpire == null || TargetEmpire.IsFaction)
                return 1f;

            float ownerBuildCapacity = Owner.AI.BuildCapacity;
            float enemyBuildCapacity = TargetEmpire.AI.BuildCapacity;
            return (enemyBuildCapacity / ownerBuildCapacity).UpperBound(1);
        }

        string GetFleetName()
        {
            switch (Type)
            {
                default:                            return "General Fleet";
                case TaskType.StrikeForce:          return "Strike Fleet";
                case TaskType.AssaultPlanet:        return "Invasion Fleet";
                case TaskType.GlassPlanet:          return "Doom Fleet";
                case TaskType.Exploration:          return $"Exploration Force - {TargetPlanet.Name}";
                case TaskType.DefendVsRemnants:     return "Defense Task Force";
                case TaskType.AssaultPirateBase:    return "Assault Fleet";
                case TaskType.GuardBeforeColonize:  return "Pre-Colonization Force";
                case TaskType.DefendClaim:          return "Scout Fleet";
                case TaskType.ClearAreaOfEnemies:   return "Defensive Fleet";
                case TaskType.InhibitorInvestigate: return "Investigation Fleet";
                case TaskType.StageFleet when TargetEmpire.isPlayer && Owner.Universe.P.Difficulty > GameDifficulty.Normal: return "Investigation Fleet";
                case TaskType.StageFleet:           return "Stage Fleet";
            }
        }

        /// <summary>
        /// this creates a task fleet from task parameters.
        /// AO, EnemyStrength, NeededTroopStrength, TaskBombTimeNeeded
        /// </summary>
        /// <param name="battleFleetSize">The ratio of a full fleet required. Best to keep this low. 
        /// a full fleet is each role count in the fleet ratios being fulfilled.
        /// if a full fleet is not found but the needed strength is found it will create a fleet
        /// slightly larger than needed.
        /// technically the enemy strength requirement could be 0 and fleetSize set 1 and it would bring
        /// as close to a main battle fleet as it can.
        /// </param>
        /// <returns>The return is either complete for success or the type of failure in fleet creation.</returns>
        RequisitionStatus CreateTaskFleet(float battleFleetSize, bool highTroopPriority = false)
        {
            if (!RoomForMoreFleets())
            {
                ReqStatus = RequisitionStatus.NotEnoughAvailableFleets;
                return ReqStatus;
            }

            // where the fleet will gather after requisition before moving to target AO.
            Planet rallyPoint = TargetPlanet ?? Owner.FindNearestRallyPoint(AO); 
            if (rallyPoint == null)
            {
                ReqStatus = RequisitionStatus.NoRallyPoint;
                return ReqStatus;
            }

            var troopsOnPlanets = new Array<Troop>();

            FleetShips fleetShips = Owner.ShipsReadyForFleet;
            fleetShips.WantedFleetCompletePercentage = battleFleetSize;

            if (NeededTroopStrength > 0)
            {
                int bombDeficit = (TaskBombTimeNeeded - fleetShips.BombSecsAvailable).LowerBound(0);
                // if we cant build bombers then convert bombtime to troops. 
                // This assume a standard troop strength of 10 
                if (bombDeficit > 0)
                {
                    NeededTroopStrength += bombDeficit * 10;
                    TaskBombTimeNeeded = 0;
                }

                // See if we need to gather troops from planets. Bail if not enough
                if (!AreThereEnoughTroopsToInvade(fleetShips.InvasionTroopStrength, out troopsOnPlanets, rallyPoint.Position, highTroopPriority))
                {
                    ReqStatus = RequisitionStatus.NotEnoughTroopStrength;
                    return ReqStatus;
                }
            }
            else if (TaskBombTimeNeeded > fleetShips.BombSecsAvailable)
            {
                ReqStatus = RequisitionStatus.NotEnoughBomberStrength;
                return ReqStatus;
            }

            int wantedNumberOfFleets = WantedNumberOfFleets();
            // All's Good... Make a fleet
            TaskForce = fleetShips.ExtractShipSet(MinimumTaskForceStrength, troopsOnPlanets, wantedNumberOfFleets, this, out int fleetSetsGot);
            if (TaskForce.IsEmpty)
            {
                ReqStatus = RequisitionStatus.FailedToCreateAFleet;
                return ReqStatus;
            }

            Log.Info($"{Owner.Name} Formed TaskForce {GetFleetName()} NumShips={TaskForce.Count} Strength={GetTaskForceStrength()} MinStrength={MinimumTaskForceStrength}");
            CreateFleet(TaskForce, GetFleetName());
            Owner.ShipsReadyForFleet.RemoveUsableFleets(fleetSetsGot);
            ReqStatus = RequisitionStatus.Complete;
            return ReqStatus;
        }

        float GetTaskForceStrength()
        {
            return TaskForce.Sum(s => s.GetStrength());
        }
            
        public bool GetMoreTroops(Planet p, out Array<Ship> moreTroops)
        {
            moreTroops = new Array<Ship>();
            if (p == null)
                return false;

            TargetPlanet = p;
            NeededTroopStrength = (int)(GetTargetPlanetGroundStrength(40) * Owner.DifficultyModifiers.EnemyTroopStrength);
            if (!AreThereEnoughTroopsToInvade(Owner.ShipsReadyForFleet.InvasionTroopStrength, out _, TargetPlanet.Position, true))
                return false;

            moreTroops = Owner.ShipsReadyForFleet.ExtractTroops(NeededTroopStrength);
            float troopStr = moreTroops.Count == 0 ? 0 : moreTroops.Sum(s => s.GetOurTroopStrength(maxTroops: 500));

            while (troopStr < NeededTroopStrength)
            {
                Owner.GetTroopShipForRebase(out Ship troopShip, TargetPlanet.Position);
                if (troopShip == null)
                    break; // No more troops

                Vector2 dir = troopShip.Position.DirectionToTarget(TargetPlanet.System.Position);
                troopShip.AI.OrderMoveTo(TargetPlanet.System.Position, dir);
                troopStr += troopShip.GetOurTroopStrength(maxTroops: 500);
                moreTroops.Add(troopShip);
            }

            return moreTroops.Count > 0;
        }

        int WantedNumberOfFleets()
        {
            int maxFleets = Owner.ShipsReadyForFleet.CurrentUseableFleets;
            int wantedNumberOfFleets = FleetCount;
            if (TargetPlanet?.Owner != null)
            {
                wantedNumberOfFleets +=(int)Math.Floor(Owner.GetFleetStrEmpireMultiplier(TargetPlanet.Owner) + 0.5f);
                wantedNumberOfFleets += TargetPlanet.System.PlanetList.Max(p =>
                {
                    if (p.Owner == TargetPlanet.Owner)
                    {
                        int extraFleets = TargetPlanet.Level > 4 ? 1 : 0;
                        extraFleets += TargetPlanet.HasWinBuilding ? 1 : 0;
                        extraFleets += TargetPlanet.HasCapital ? 1 : 0;
                        extraFleets += TargetPlanet.Owner.CurrentMilitaryStrength > Owner.CurrentMilitaryStrength ? 1 : 0;

                        return extraFleets;
                    }

                    return 0;
                });
            }

            return wantedNumberOfFleets.Clamped(1, maxFleets);
        }

        void InitFleetRequirements(float minFleetStrength, int minTroopStrength, int minBombMinutes)
        {
            if (minTroopStrength > 0 || minBombMinutes > 0)
            {
                if (TargetPlanet == null)
                {
                    Log.Error($"Sending troops with no planet to assault");
                }
                else
                {
                    if (minTroopStrength > 0)
                        NeededTroopStrength = (int)(GetTargetPlanetGroundStrength(minTroopStrength) 
                                                    * Owner.DifficultyModifiers.EnemyTroopStrength); 

                    if (minBombMinutes > 0)
                        TaskBombTimeNeeded = BombTimeNeeded().LowerBound(minBombMinutes);
                }
            }

            EnemyStrength = GetHostileStrengthAtAO();
            MinimumTaskForceStrength = minFleetStrength;
        }

        public enum RequisitionStatus
        {
            None,
            NoRallyPoint,
            NoEmpireAreasOfOperation,
            NotEnoughAvailableFleets,
            NotEnoughShipStrength,
            NotEnoughTroopStrength,
            NotEnoughBomberStrength,
            FailedToCreateAFleet,
            Complete
        }
    }
}