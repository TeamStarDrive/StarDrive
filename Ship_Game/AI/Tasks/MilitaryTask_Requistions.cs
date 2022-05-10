using Newtonsoft.Json;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using System;
using System.Linq;
using System.Xml.Serialization;
using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI.Tasks
{
    public partial class MilitaryTask
    {
        public int FleetCount = 1;
        public float Completeness = 0.5f;
        [XmlIgnore] [JsonIgnore] RequisitionStatus ReqStatus = RequisitionStatus.None;
        public RequisitionStatus GetRequisitionStatus() => ReqStatus;
        float GetEnemyShipStrengthInAO()
        {
            // RedFox: I removed ClampMin(minimumStrength) because this was causing infinite
            //         Create-Destroy-Create loop of ClearAreaOfEnemies MilitaryTasks
            //         Lets just report what the actual strength is.
            return Owner.GetEmpireAI().ThreatMatrix.PingHostileStr(AO, AORadius, Owner);
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

            var defenseDict     = Owner.GetEmpireAI().DefensiveCoordinator.DefenseDict;
            var troopSystems    = Owner.GetOwnedSystems().Sorted(dist => -1 * dist.Position.SqDist(rallyPoint));
            
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
                    if (planet.Owner != Owner || planet.RecentCombat) 
                        continue;

                    float planetMinStr                 = sysCom.PlanetTroopMin(planet);
                    float planetDefendingTroopStrength = planet.GetDefendingTroopCount();
                    float maxCanTake                   = troopPriorityHigh && !planet.RecentCombat && !planet.ParentSystem.HostileForcesPresent(Owner)
                        ? 5 
                        : (planetDefendingTroopStrength - (planetMinStr - 3)).LowerBound(0);

                    if (maxCanTake > 0)
                    {
                        potentialTroops.AddRange(planet.GetEmpireTroops(Owner, (int)maxCanTake));

                        totalStrength += potentialTroops.Sum(t => t.ActualStrengthMax);

                        if (strengthNeeded <= totalStrength)
                        {
                            break;
                        }
                    }
                }

                if (potentialTroops.Count > 50)
                {
                    break;
                }
            }

            return potentialTroops;
        }

        private void CreateFleet(Array<Ship> ships, string name)
        {
            var newFleet = new Fleet(Owner.Universum.CreateId())
            {
                Name  = name,
                Owner = Owner
            };

            int fleetNum = FindUnusedFleetNumber();
            Owner.GetFleetsDict()[fleetNum] = newFleet;
            Owner.GetEmpireAI().UsedFleets.Add(fleetNum);
            WhichFleet = fleetNum;
            newFleet.FleetTask = this;
            foreach (Ship ship in ships)
            {
                ship.RemoveFromPoolAndFleet(clearOrders: true);
                newFleet.AddShip(ship);
            }

            newFleet.AutoArrange();
        }

        public void CreateRemnantFleet(Empire owner, Ship ship, string name, out Fleet newFleet)
        {
            newFleet = new Fleet(owner.Universum.CreateId())
            {
                Name = name,
                Owner = owner,
            };

            int fleetNum = FindUnusedFleetNumber();
            Owner.GetFleetsDict()[fleetNum] = newFleet;
            Owner.GetEmpireAI().UsedFleets.Add(fleetNum);
            WhichFleet = fleetNum;
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
            bombTime    += (int)TargetPlanet.ShieldStrengthMax / 50;
            return bombTime;
        }

        AO FindClosestAO(float strWanted = 100)
        {
            var aos =  Owner.GetEmpireAI().AreasOfOperations;
            if (aos.Count == 0)
            {
                Log.Info($"{Owner.Name} has no areas of operation");
                return null;
            }

            AO closestAO = aos.FindMin(ao => ao.Center.SqDist(AO));
            return closestAO;
        }

        Fleet FindClosestCoreFleet(float strWanted = 100)
        {
            Array<AO> aos = Owner.GetEmpireAI().AreasOfOperations;
            if (aos.Count == 0)
            {
                Log.Error($"{Owner.Name} has no areas of operation");
                return null;
            }

            AO closestAo = aos.FindMinFiltered(ao => ao.GetCoreFleet().GetStrength() > strWanted,
                                               ao => ao.Center.SqDist(AO));
            return closestAo?.GetCoreFleet();
        }

        void RequisitionCoreFleet()
        {
            AO[] sorted = CollectionExt.ToArray(Owner.GetEmpireAI().AreasOfOperations
                    .OrderByDescending(ao => ao.OffensiveForcePoolStrength >= MinimumTaskForceStrength)
                    .ThenBy(ao => AO.Distance(ao.Center)));

            if (sorted.Length == 0)
                return;

            AO closestAO = sorted[0];
            EnemyStrength = Owner.GetEmpireAI().ThreatMatrix.PingRadarStr(AO, 10000, Owner);
            if (EnemyStrength < 1f)
                return;

            MinimumTaskForceStrength = EnemyStrength;
            if (closestAO.GetCoreFleet().FleetTask == null &&
                closestAO.GetCoreFleet().GetStrength() > MinimumTaskForceStrength)
            {
                WhichFleet = closestAO.WhichFleet;
                closestAO.GetCoreFleet().FleetTask = this;
                closestAO.GetCoreFleet().TaskStep = 1;
                IsCoreFleetTask = true;
                NeedEvaluation = false;
            }
        }

        void RequisitionDefenseForce()
        {
            if (!Owner.SystemsWithThreat.Any(t => !t.ThreatTimedOut && t.TargetSystem == TargetSystem)
                && TargetSystem.DangerousForcesPresent(Owner))
            {
                EndTask();
            }

            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {Type}");

            AO = TargetPlanet?.Center ?? AO;
            EnemyStrength = Owner.KnownEnemyStrengthIn(TargetSystem).LowerBound(EnemyStrength);
            UpdateMinimumTaskForceStrength();
            if (CreateTaskFleet(Completeness) == RequisitionStatus.Complete)
                NeedEvaluation = false;
        }

        void RequisitionClaimForce()
        {
            if (TargetPlanet.Owner != null
                && TargetPlanet.Owner != EmpireManager.Unknown && !TargetPlanet.Owner.data.IsRebelFaction)
            {
                Owner.GetRelations(TargetPlanet.Owner, out Relationship rel);
                if (rel != null && (!rel.AtWar && !rel.PreparingForWar))
                {
                    EndTask();
                    return;
                }
            }

            if (AO.AlmostZero())
                throw new Exception("AO cannot be empty");

            if (Owner.AIManagedShips.CurrentUseableFleets < 0) 
                return;

            int requiredTroopStrength = 0;
            if (TargetPlanet != null)
            {
                AO                    = TargetPlanet.Center;
                requiredTroopStrength = (int)TargetPlanet.GetGroundStrengthOther(Owner) - (int)TargetPlanet.GetGroundStrength(Owner);
                EnemyStrength         = Owner.KnownEnemyStrengthIn(TargetPlanet.ParentSystem) + TargetPlanet.BuildingGeodeticOffense;
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
                Owner.KnownEnemyStrengthIn(TargetPlanet.ParentSystem)
                > MinimumTaskForceStrength / Owner.GetFleetStrEmpireMultiplier(TargetEmpire))
            {
                EndTask();
                return;
            }

            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {Type}");

            AO closestAO = FindClosestAO(EnemyStrength);
            if (closestAO == null || closestAO.GetNumOffensiveForcePoolShips() < 1)
                return;

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

            AO closestAO = FindClosestAO(EnemyStrength);
            if (closestAO == null || closestAO.GetNumOffensiveForcePoolShips() < 1)
                return;

            EnemyStrength = Owner.GetEmpireAI().ThreatMatrix.PingRadarStr(TargetShip.Position,
                   40000, Owner, true).LowerBound(100);

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
                Owner.GetEmpireAI().Goals.Add(new DefendVsRemnants(TargetPlanet, TargetPlanet.Owner, Fleet));
                NeedEvaluation = false;
            }
        }

        void RequisitionExplorationForce()
        {
            if (TargetPlanet.Owner != null 
                && TargetPlanet.Owner != Owner
                && TargetPlanet.Owner != EmpireManager.Unknown
                && !TargetPlanet.Owner.data.IsRebelFaction || !TargetPlanet.EventsOnTiles())
            {
                EndTask();
                return;
            }

            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {Type}");

            AO = TargetPlanet.Center;
            float buildingGeodeticOffense = TargetPlanet.Owner != Owner ? TargetPlanet.BuildingGeodeticOffense : 0;
            EnemyStrength = Owner.GetEmpireAI().ThreatMatrix.PingRadarStr(TargetPlanet.Center,
                               TargetPlanet.ParentSystem.Radius, Owner, true).LowerBound(100);

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

            AO = TargetPlanet.Center;
            float geodeticOffense = TargetPlanet.BuildingGeodeticOffense;
            float lowerBound      = GetMinimumStrLowerBound(geodeticOffense, enemy);
            EnemyStrength         = (GetEnemyShipStrengthInAO() + geodeticOffense).LowerBound(EnemyStrength);

            UpdateMinimumTaskForceStrength(lowerBound);
            InitFleetRequirements(MinimumTaskForceStrength, minTroopStrength: 40 ,minBombMinutes: 3);
            if (CreateTaskFleet(Completeness, true) == RequisitionStatus.Complete)
                NeedEvaluation = false;
        }

        void RequisitionGlassForce()
        {
            Empire enemy = TargetPlanet.Owner;
            if (!Owner.canBuildBombers 
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

            AO = TargetPlanet.Center;
            float geodeticOffense = TargetPlanet.BuildingGeodeticOffense;
            float lowerBound      = GetMinimumStrLowerBound(geodeticOffense, enemy);
            EnemyStrength         = (GetEnemyShipStrengthInAO() + geodeticOffense).LowerBound(EnemyStrength);

            UpdateMinimumTaskForceStrength(lowerBound);
            int bombTimeNeeded = (TargetPlanet.TotalDefensiveStrength / 5).LowerBound(5) + (int)Math.Ceiling(TargetPlanet.PopulationBillion) * 2;
            InitFleetRequirements(minFleetStrength: MinimumTaskForceStrength, minTroopStrength: 0, minBombMinutes: bombTimeNeeded);

            if (CreateTaskFleet(Completeness) == RequisitionStatus.Complete)
                NeedEvaluation = false;
        }

        float GetMinimumStrLowerBound(float geodeticOffense, Empire enemy)
        {
            float initialStr     = geodeticOffense + Owner.KnownEmpireOffensiveStrength(enemy) / 10;
            float enemyStrNearby = geodeticOffense + GetKnownEnemyStrInClosestSystems(TargetPlanet.ParentSystem, Owner, enemy);
            // Todo advanced tasks also get multiplier;
            float multiplier     = Type == TaskType.StrikeForce || Type == TaskType.StageFleet ? 2 : 1; 
            return initialStr.LowerBound(enemyStrNearby).LowerBound(Owner.OffensiveStrength/10) * multiplier;
        }

        void UpdateMinimumTaskForceStrength(float lowerBound = 0)
        {
            if (EnemyStrength.AlmostEqual(100) && TargetShip != null)
                EnemyStrength += TargetShip.BaseStrength;

            MinimumTaskForceStrength = EnemyStrength.LowerBound(lowerBound);
            float multiplier         = Owner.GetFleetStrEmpireMultiplier(TargetEmpire);
            MinimumTaskForceStrength = (MinimumTaskForceStrength * multiplier)
                .UpperBound(Owner.OffensiveStrength / GetBuildCapacityDivisor());

            float lifeTimeMax = IsWarTask ? Owner.PersonalityModifiers.WarTasksLifeTime : 10;
            float goalLifeTime = Goal?.LifeTime ?? 0;
                MinimumTaskForceStrength *= (lifeTimeMax - goalLifeTime).LowerBound(lifeTimeMax*0.5f) / lifeTimeMax;
        }
        
        float GetBuildCapacityDivisor()
        {
            if (TargetEmpire == null || TargetEmpire.isFaction)
                return 2;

            float ownerBuildCapacity = Owner.GetEmpireAI().BuildCapacity;
            float enemyBuildCapacity = TargetEmpire.GetEmpireAI().BuildCapacity;
            return (ownerBuildCapacity / enemyBuildCapacity).LowerBound(2);
        }

        string GetFleetName()
        {
            switch (Type)
            {
                default:                           return "General Fleet";
                case TaskType.StageFleet:          return "Stage Fleet";
                case TaskType.StrikeForce:         return "Strike Fleet";
                case TaskType.AssaultPlanet:       return "Invasion Fleet";
                case TaskType.GlassPlanet:         return "Doom Fleet";
                case TaskType.Exploration:         return $"Exploration Force - {TargetPlanet.Name}";
                case TaskType.DefendVsRemnants:    return "Defense Task Force";
                case TaskType.AssaultPirateBase:   return "Assault Fleet";
                case TaskType.GuardBeforeColonize: return "Pre-Colonization Force";
                case TaskType.DefendClaim:         return "Scout Fleet";
                case TaskType.ClearAreaOfEnemies:  return "Defensive Fleet";
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

            // this determines what core fleet to send if the enemy is strong. 
            // its also an easy out for an empire in a bad state. 
            AO closestAO = FindClosestAO(MinimumTaskForceStrength);

            if (closestAO == null)
            {
                ReqStatus = RequisitionStatus.NoEmpireAreasOfOperation;
                return ReqStatus;
            }

            // where the fleet will gather after requisition before moving to target AO.
            Planet rallyPoint = TargetPlanet ?? GetRallyPlanet();
            if (rallyPoint == null)
            {
                ReqStatus = RequisitionStatus.NoRallyPoint;
                return ReqStatus;
            }


            FleetShips fleetShips                    = Owner.AIManagedShips.EmpireReadyFleets;
            fleetShips.WantedFleetCompletePercentage = battleFleetSize;
            var troopsOnPlanets                      = new Array<Troop>();

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
                if (!AreThereEnoughTroopsToInvade(fleetShips.InvasionTroopStrength, out troopsOnPlanets, rallyPoint.Center, highTroopPriority))
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
            TaskForce = fleetShips.ExtractShipSet(MinimumTaskForceStrength, troopsOnPlanets, wantedNumberOfFleets, rallyPoint.Center, this);
            if (TaskForce.IsEmpty)
            {
                ReqStatus = RequisitionStatus.FailedToCreateAFleet;
                return ReqStatus;
            }

            CreateFleet(TaskForce, GetFleetName());
            Owner.AIManagedShips.CurrentUseableFleets -= fleetShips.ShipSetsExtracted.LowerBound(1);
            {
                ReqStatus = RequisitionStatus.Complete;
                return ReqStatus;
            }
        }

        public bool GetMoreTroops(Planet p, out Array<Ship> moreTroops)
        {
            moreTroops = new Array<Ship>();
            if (p == null)
                return false;

            SetTargetPlanet(p);
            FleetShips fleetShips = Owner.AIManagedShips.EmpireReadyFleets;
            NeededTroopStrength   = (int)(GetTargetPlanetGroundStrength(40) * Owner.DifficultyModifiers.EnemyTroopStrength);
            if (!AreThereEnoughTroopsToInvade(fleetShips.InvasionTroopStrength, out _, TargetPlanet.Center, true))
                return false;

            moreTroops     = fleetShips.ExtractTroops(NeededTroopStrength);
            float troopStr = moreTroops.Count == 0 ? 0 : moreTroops.Sum(s => s.GetOurTroopStrength(maxTroops: 500));

            while (troopStr < NeededTroopStrength)
            {
                Owner.GetTroopShipForRebase(out Ship troopShip, TargetPlanet.Center);
                if (troopShip == null)
                    break; // No more troops

                Vector2 dir = troopShip.Position.DirectionToTarget(TargetPlanet.ParentSystem.Position);
                troopShip.AI.OrderMoveTo(TargetPlanet.ParentSystem.Position, dir);
                troopStr += troopShip.GetOurTroopStrength(maxTroops: 500);
                moreTroops.Add(troopShip);
            }

            return moreTroops.Count > 0;
        }

        private int WantedNumberOfFleets()
        {
            int maxFleets = Owner.AIManagedShips.CurrentUseableFleets;
            int wantedNumberOfFleets = FleetCount;
            if (TargetPlanet?.Owner != null)
            {
                wantedNumberOfFleets +=(int)Math.Floor(Owner.GetFleetStrEmpireMultiplier(TargetPlanet.Owner) + 0.5f);
                wantedNumberOfFleets += TargetPlanet.ParentSystem.PlanetList.Max(p =>
                {
                    if (p.Owner == TargetPlanet.Owner)
                    {
                        int extraFleets = TargetPlanet.Level > 4 ? 1 : 0;
                        extraFleets += TargetPlanet.HasWinBuilding ? 1 : 0;
                        extraFleets += TargetPlanet.BuildingList.Any(b => b.IsCapital) ? 1 : 0;
                        extraFleets += TargetPlanet.Owner.CurrentMilitaryStrength > Owner.CurrentMilitaryStrength || TargetPlanet.Owner.TechScore > Owner.TechScore ? 1 : 0;

                        return extraFleets;
                    }

                    return 0;
                });
            }

            return wantedNumberOfFleets.UpperBound(maxFleets);
        }

        Planet GetRallyPlanet() => RallyAO?.GetPlanet() ?? Owner.FindNearestRallyPoint(AO);

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

            EnemyStrength            = GetEnemyShipStrengthInAO();
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