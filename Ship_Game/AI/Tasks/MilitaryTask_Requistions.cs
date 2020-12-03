using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Debug;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Ship_Game.Commands.Goals;

namespace Ship_Game.AI.Tasks
{
    public partial class MilitaryTask
    {
        public int FleetCount = 1;
        public float Completeness = 0.5f;

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
                SolarSystem system     = troopSystems[x];
                SystemCommander sysCom = defenseDict[system];
                if (!sysCom.IsEnoughTroopStrength && !troopPriorityHigh) 
                    continue;
                
                for (int i = 0; i < system.PlanetList.Count; i++)
                {
                    Planet planet = system.PlanetList[i];

                    if (planet.Owner != Owner) continue;
                    if (planet.RecentCombat)   continue;

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

        private void CreateFleet(Array<Ship> ships, string Name)
        {
            var newFleet = new Fleet
            {
                Name = Name,
                Owner = Owner
            };

            int fleetNum = FindUnusedFleetNumber();
            Owner.GetFleetsDict()[fleetNum] = newFleet;
            Owner.GetEmpireAI().UsedFleets.Add(fleetNum);
            WhichFleet = fleetNum;
            newFleet.FleetTask = this;
            foreach (Ship ship in ships)
            {
                ship.AI.ClearOrders();
                Owner.Pool.RemoveShipFromFleetAndPools(ship);
                newFleet.AddShip(ship);
            }

            newFleet.AutoArrange();
        }

        public void CreateRemnantFleet(Empire owner, Ship ship, string name, out Fleet newFleet)
        {
            newFleet = new Fleet
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
            int bombTime =  TargetPlanet.TotalDefensiveStrength / 10  ;

            //shields are a real pain. this may need a lot more code to deal with. 
            bombTime    += (int)TargetPlanet.ShieldStrengthMax / 50;
            return bombTime;
        }

        AO FindClosestAO(float strWanted = 100)
        {
            if (Fleet != null)
            {
                RallyAO = null;
                foreach (var theater in Owner.AllActiveWarTheaters.Sorted(t=> t?.TheaterAO.Center.SqDist(Fleet.AveragePosition()) ?? float.MaxValue ))
                {
                    RallyAO = theater?.RallyAO;
                }
            }


            if (RallyAO != null) 
                return RallyAO;
            var aos =  Owner.GetEmpireAI().AreasOfOperations;
            if (aos.Count == 0)
            {
                Log.Info($"{Owner.Name} has no areas of operation");
                return null;
            }

            AO closestAO = aos.FindMaxFiltered(ao => ao.GetPoolStrength() > strWanted,
                               ao => -ao.Center.SqDist(AO))
                           ?? aos.FindMin(ao => ao.Center.SqDist(AO));
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
            AO[] sorted = Owner.GetEmpireAI().AreasOfOperations
                .OrderByDescending(ao => ao.OffensiveForcePoolStrength >= MinimumTaskForceStrength)
                .ThenBy(ao => AO.Distance(ao.Center)).ToArray();

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
                Step = 1;
            }
        }

        void RequisitionDefenseForce()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            AO = TargetPlanet?.Center ?? AO;

            InitFleetRequirements(minFleetStrength: 100, minTroopStrength: 0, minBombMinutes: 0);

            if (CreateTaskFleet("Defensive Fleet", Completeness) == RequisitionStatus.Complete)
            {
                Step = 1;
            }
        }

        void RequisitionClaimForce()
        {
            if (AO.AlmostZero())
                throw new Exception();
            if (Owner.Pool.CurrentUseableFleets < 0) return;

            AO closestAO = FindClosestAO();
            if (closestAO == null || closestAO.GetNumOffensiveForcePoolShips() < 1)
                return;

            int requiredTroopStrength = 0;
            if (TargetPlanet != null)
            {
                AO                    = TargetPlanet.Center;
                requiredTroopStrength = (int)TargetPlanet.GetGroundStrengthOther(Owner) - (int)TargetPlanet.GetGroundStrength(Owner);
                UpdateMinimumTaskForceStrength(TargetPlanet.ParentSystem, TargetPlanet.BuildingGeodeticOffense);
            }

            if (requiredTroopStrength > 0) // If we need troops, we must have a minimum
                requiredTroopStrength = requiredTroopStrength.LowerBound(40);

            InitFleetRequirements(minFleetStrength: MinimumTaskForceStrength, minTroopStrength: requiredTroopStrength, minBombMinutes: 0);
            float battleFleetSize = MinimumTaskForceStrength < 100 ? 0 : 1f;
            if (CreateTaskFleet("Scout Fleet", Completeness * battleFleetSize, true) == RequisitionStatus.Complete)
                Step = 1;
        }

        void RequisitionGuardBeforeColonize()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            AO closestAO = FindClosestAO(EnemyStrength);
            if (closestAO == null || closestAO.GetNumOffensiveForcePoolShips() < 1)
                return;
            float battleFleetSize = MinimumTaskForceStrength < 100 ? 0 : 0.5f;

            InitFleetRequirements(MinimumTaskForceStrength, minTroopStrength: 0, minBombMinutes: 0);
            if (CreateTaskFleet("Pre-Colonization Force", battleFleetSize, false) == RequisitionStatus.Complete)
                Step = 1;
        }

        void RequisitionAssaultPirateBase()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            if (TargetShip == null || !TargetShip.Active)
            {
                EndTask();
                return;
            }

            AO closestAO = FindClosestAO(EnemyStrength);
            if (closestAO == null || closestAO.GetNumOffensiveForcePoolShips() < 1)
                return;

            EnemyStrength = Owner.GetEmpireAI().ThreatMatrix.PingRadarStr(TargetShip.Center,
                   40000, Owner, true).LowerBound(100);

            UpdateMinimumTaskForceStrength();
            InitFleetRequirements(MinimumTaskForceStrength, minTroopStrength: 0, minBombMinutes: 0);
            if (CreateTaskFleet("Assault Fleet", Completeness, false) == RequisitionStatus.Complete)
                Step = 1;
        }

        void RequisitionDefendVsRemnants()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            if (TargetPlanet.Owner != Owner)
            {
                EndTask();
                return;
            }

            InitFleetRequirements(MinimumTaskForceStrength, minTroopStrength: 0, minBombMinutes: 0);
            EnemyStrength = MinimumTaskForceStrength;
            if (CreateTaskFleet("Defense Task Force", Completeness) == RequisitionStatus.Complete)
            {
                Owner.GetEmpireAI().Goals.Add(new DefendVsRemnants(TargetPlanet, TargetPlanet.Owner, Fleet));
                Step = 1;
            }
        }

        void RequisitionExplorationForce()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            if (TargetPlanet.Owner != null && TargetPlanet.Owner != Owner && !Owner.IsEmpireAttackable(TargetPlanet.Owner))
            {
                EndTask();
                return;
            }

            AO = TargetPlanet.Center;

            float buildingGeodeticOffense = TargetPlanet.Owner != Owner ? TargetPlanet.BuildingGeodeticOffense : 0;
            EnemyStrength = Owner.GetEmpireAI().ThreatMatrix.PingRadarStr(TargetPlanet.Center,
                               TargetPlanet.ParentSystem.Radius, Owner, true).LowerBound(100);

            UpdateMinimumTaskForceStrength(buildingGeodeticOffense);
            InitFleetRequirements(MinimumTaskForceStrength, minTroopStrength: 40, minBombMinutes: 0);
            float battleFleetSize = MinimumTaskForceStrength < 100 ? 0 : 1f;
            if (CreateTaskFleet("Exploration Force", Completeness * battleFleetSize, true) 
                                == RequisitionStatus.Complete)
            {
                Step = 1;
            }
        }

        void RequisitionAssaultForces()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            if (TargetPlanet.Owner == null || TargetPlanet.Owner == Owner ||
                Owner.IsPeaceTreaty(TargetPlanet.Owner))
            {
                EndTask();
                return;
            }

            AO            = TargetPlanet.Center;
            UpdateMinimumTaskForceStrength(TargetPlanet.ParentSystem, TargetPlanet.BuildingGeodeticOffense);

            EnemyStrength = GetEnemyShipStrengthInAO() + TargetPlanet.BuildingGeodeticCount;
            InitFleetRequirements(MinimumTaskForceStrength.LowerBound(EnemyStrength), minTroopStrength: 100 ,minBombMinutes: 3);

            if (CreateTaskFleet("Invasion Fleet", Completeness, true) == RequisitionStatus.Complete)
            {
                Step = 1;
            }
        }

        void RequisitionGlassForce()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            if (!Owner.canBuildBombers || TargetPlanet.Owner == null || TargetPlanet.Owner == Owner ||
                Owner.IsPeaceTreaty(TargetPlanet.Owner))
            {
                EndTask();
                return;
            }

            AO = TargetPlanet.Center;
            UpdateMinimumTaskForceStrength(TargetPlanet.ParentSystem, TargetPlanet.BuildingGeodeticOffense);

            int bombTimeNeeded = (TargetPlanet.TotalDefensiveStrength / 5).LowerBound(5) + (int)Math.Ceiling(TargetPlanet.PopulationBillion) * 2;
            InitFleetRequirements(minFleetStrength: MinimumTaskForceStrength, minTroopStrength: 0, minBombMinutes: bombTimeNeeded);

            if (CreateTaskFleet("Doom Fleet", Completeness) == RequisitionStatus.Complete)
            {
                Step = 1;
            }
        }

        void UpdateMinimumTaskForceStrength(SolarSystem targetSystem, float buildingsSpaceOffense = 0)
        {
            EnemyStrength = Owner.KnownEnemyStrengthIn(targetSystem);
            UpdateMinimumTaskForceStrength(buildingsSpaceOffense);
        }

        void UpdateMinimumTaskForceStrength(float buildingsSpaceOffense = 0)
        {
            if (EnemyStrength.AlmostEqual(100) && TargetShip != null)
                EnemyStrength += TargetShip.BaseStrength;

            MinimumTaskForceStrength = EnemyStrength + buildingsSpaceOffense;
            float multiplier         = Owner.GetFleetStrEmpireMultiplier(TargetEmpire);

            if (MinimumTaskForceStrength * multiplier < Owner.CurrentMilitaryStrength / 2)
                MinimumTaskForceStrength *= multiplier;
            else
                MinimumTaskForceStrength *= (multiplier / 2).LowerBound(1);
        }

        /// <summary>
        /// this creates a task fleet from task parameters.
        /// AO, EnemyStrength, NeededTroopStrength, TaskBombTimeNeeded
        /// </summary>
        /// <param name="fleetName">The name displayed for the fleet</param>
        /// <param name="battleFleetSize">The ratio of a full fleet required. Best to keep this low. 
        /// a full fleet is each role count in the fleet ratios being fulfilled.
        /// if a full fleet is not found but the needed strength is found it will create a fleet
        /// slightly larger than needed.
        /// technically the enemy strength requirement could be 0 and fleetSize set 1 and it would bring
        /// as close to a main battle fleet as it can.
        /// </param>
        /// <returns>The return is either complete for success or the type of failure in fleet creation.</returns>
        RequisitionStatus CreateTaskFleet(string fleetName, float battleFleetSize, bool highTroopPriority = false)
        {

            if (!RoomForMoreFleets()) return RequisitionStatus.NotEnoughAvailableFleets;

            // this determines what core fleet to send if the enemy is strong. 
            // its also an easy out for an empire in a bad state. 
            AO closestAO = FindClosestAO(MinimumTaskForceStrength);

            if (closestAO == null)
            {
                return RequisitionStatus.NoEmpireAreasOfOperation;
            }

            // where the fleet will gather after requisition before moving to target AO.
            Planet rallyPoint = TargetPlanet ?? GetRallyPlanet();
            if (rallyPoint == null)
                return RequisitionStatus.NoRallyPoint;


            FleetShips fleetShips                    = Owner.Pool.EmpireReadyFleets;
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
                    return RequisitionStatus.NotEnoughTroopStrength;
            }
            else if (TaskBombTimeNeeded > fleetShips.BombSecsAvailable)
                return RequisitionStatus.NotEnoughBomberStrength;

            int wantedNumberOfFleets = WantedNumberOfFleets();

            float strengthNeeded = MinimumTaskForceStrength.LowerBound(EnemyStrength);

            // All's Good... Make a fleet

            TaskForce = fleetShips.ExtractShipSet(strengthNeeded, TaskBombTimeNeeded,
                NeededTroopStrength, troopsOnPlanets, wantedNumberOfFleets, rallyCenter: rallyPoint.Center);

            if (TaskForce.IsEmpty)
                return RequisitionStatus.FailedToCreateAFleet;

            float totalStr = TaskForce.Sum(s => s.BaseStrength);
            if (type == TaskType.DefendClaim)
                Log.Info("lala");
            CreateFleet(TaskForce, fleetName);
            Owner.Pool.CurrentUseableFleets -= fleetShips.ShipSetsExtracted.LowerBound(1);
            return RequisitionStatus.Complete;
        }

        private int WantedNumberOfFleets()
        {
            int maxFleets = Owner.AllFleetsReady().CountFleets(out _);
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
                        NeededTroopStrength = (int)(GetTargetPlanetGroundStrength(minTroopStrength)); 
                    //* Owner.DifficultyModifiers.EnemyTroopStrength);

                    if (minBombMinutes > 0)
                        TaskBombTimeNeeded = BombTimeNeeded().LowerBound(minBombMinutes);
                }
            }

            EnemyStrength            = GetEnemyShipStrengthInAO();
            MinimumTaskForceStrength = minFleetStrength;
        }

        enum RequisitionStatus
        {
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