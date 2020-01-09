using Microsoft.Xna.Framework;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.AI.Tasks
{
    public partial class MilitaryTask
    {
        private float GetEnemyShipStrengthInAO(int minimumStrength)
        {
            return Owner.GetEmpireAI().ThreatMatrix.PingNetRadarStr(AO, AORadius * 2, Owner).
                ClampMin(minimumStrength);
        }

        private int GetTargetPlanetGroundStrength(int minimumStrength)
        {
            return (int)TargetPlanet.GetGroundStrengthOther(Owner).ClampMin(minimumStrength);
        }

        Array<Troop> GetTroopsOnPlanets(Vector2 rallyPoint, float strengthNeeded, out float totalStrength)
        {
            var potentialTroops = new Array<Troop>();
            totalStrength = 0;
            if (strengthNeeded <= 0) return potentialTroops;

            var defenseDict     = Owner.GetEmpireAI().DefensiveCoordinator.DefenseDict;
            var troopSystems    = Owner.GetOwnedSystems().Sorted(dist => dist.Position.SqDist(rallyPoint));
            
            for (int x = 0; x < troopSystems.Length; x++)
            {
                SolarSystem system     = troopSystems[x];
                SystemCommander sysCom = defenseDict[system];
                if (!sysCom.IsEnoughTroopStrength) 
                    continue;
                
                for (int i = 0; i < system.PlanetList.Count; i++)
                {
                    Planet planet = system.PlanetList[i];

                    if (planet.Owner != Owner) continue;
                    if (planet.RecentCombat)   continue;

                    float planetMinStr                 = sysCom.TroopStrengthMin(planet);
                    float planetDefendingTroopStrength = planet.GetDefendingTroopStrength();
                    float maxCanTake                   = (planetDefendingTroopStrength - planetMinStr).ClampMin(0);

                    if (maxCanTake > 0)
                    {
                        potentialTroops.AddRange(planet.GetOwnersLaunchReadyTroops(maxCanTake));

                        totalStrength += potentialTroops.Sum(t => t.Strength);

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
            ///// asdaljksdjalsdkjal;sdkjla;sdkjasl;dkj i will rebuild this better.
            ///// this is acessing a lot of other classes stuff.
            int fleetNum = FindUnusedFleetNumber();
            Owner.GetFleetsDict()[fleetNum] = newFleet;
            Owner.GetEmpireAI().UsedFleets.Add(fleetNum);
            WhichFleet = fleetNum;
            newFleet.FleetTask = this;
            foreach (Ship ship in ships)
            {
                newFleet.AddShip(ship);
                ship.AI.ClearOrders();
                Owner.GetEmpireAI().RemoveShipFromForce(ship);
            }

            newFleet.AutoArrange();
        }

        private FleetShips AllFleetReadyShipsNearestTarget(Vector2 targetPosition)
        {
            //Get all available ships from AO's
            var ships = Owner.GetShipsFromOffensePools();
            //Get specialized ships
            ships.AddRange(Owner.GetForcePool());
            //Massive sort.
            ships.Sort(s => s.Center.SqDist(targetPosition));
            //return a fleet creator. 
            return new FleetShips(Owner, ships);
        }

        //not deleting yet. need to investigate usability
        private Array<Ship> GetShipsFromDefense(float tfstrength, float minimumEscortStrength)
        {
            Array<Ship> elTaskForce = new Array<Ship>();
            if (!Owner.isFaction && Owner.data.DiplomaticPersonality.Territorialism < 50 &&
                tfstrength < minimumEscortStrength)
            {
                if (!IsCoreFleetTask)
                    foreach (var kv in Owner.GetEmpireAI().DefensiveCoordinator.DefenseDict
                        .OrderByDescending(system => system.Key.HostileForcesPresent(Owner)
                            ? 1
                            : 2 * system.Key.Position.SqDist(TargetPlanet.Center))
                    )
                    {
                        Ship[] array = kv.Value.GetShipList.ToArray();
                        for (int i = 0; i < array.Length; i++)
                        {
                            Ship ship = array[i];
                            if (ship.AI.BadGuysNear || ship.fleet != null || tfstrength >= minimumEscortStrength ||
                                ship.GetStrength() <= 0f
                                || ship.shipData.Role == ShipData.RoleName.troop ||
                                ship.Carrier.HasAssaultTransporters ||
                                ship.Carrier.HasActiveTroopBays
                                || ship.Mothership != null
                            )
                                continue;

                            tfstrength = tfstrength + ship.GetStrength();
                            elTaskForce.Add(ship);
                            Owner.GetEmpireAI().DefensiveCoordinator.Remove(ship);
                        }
                    }
            }
            return elTaskForce;
        }

        private bool AreThereEnoughTroopsToInvade(FleetShips fleetShips, out Array<Troop> troopsOnPlanetNeeded,
                                                  Vector2 rallyPoint)
        {
           troopsOnPlanetNeeded = new Array<Troop>();

           if (NeededTroopStrength <= 0)
               return true;

            if (fleetShips.InvasionTroopStrength < NeededTroopStrength)
            {
                troopsOnPlanetNeeded = GetTroopsOnPlanets(rallyPoint, NeededTroopStrength, 
                                              out float planetsTroopStrength);

                if (fleetShips.InvasionTroopStrength + planetsTroopStrength >= NeededTroopStrength)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The concept here is to calculate how much bomb power is needed.
        /// this set into minutes of bombing. 
        /// </summary>
        /// <returns></returns>
        private int BombTimeNeeded()
        {
            //we cant easily say what the strength of the of the defensive systems yet. 
            //this just counts how many there are and want 5 minutes per building.
            int bombTime = TargetPlanet.BuildingGeodeticCount * 5;

            //ground landing spots. if we dont have a significant space to land troops. create them. 
            bombTime    += TargetPlanet.GetGroundLandingSpots() < 25 ? 1 : 0;

            //shields are a real pain. this may need a lot more code to deal with. 
            bombTime    += TargetPlanet.ShieldStrengthMax > 0 ? 10 : 0;
            return bombTime;
        }

        AO FindClosestAO(float strWanted = 100)
        {
            var aos = Owner.GetEmpireAI().AreasOfOperations;
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

            AO closestAo = aos.FindMaxFiltered(ao => ao.GetCoreFleet().GetStrength() > strWanted,
                ao => -ao.Center.SqDist(AO));
            if (closestAo == null)
            {
                Empire.Universe?.DebugWin?.DebugLogText($"Tasks ({Owner.Name}) Requistiions: No Core Fleets Stronger than ({strWanted}) found. CoreFleets#: {aos.Count} ", DebugModes.Normal);
                return null;
            }
            return closestAo.GetCoreFleet();
        }

        void SendSofteningFleet(AO closestAO)
        {
            Fleet closestCoreFleet = FindClosestCoreFleet(MinimumTaskForceStrength);
            if (closestCoreFleet == null || closestCoreFleet.FleetTask != null)
                return;
            var clearArea = new MilitaryTask(closestCoreFleet.Owner)
            {
                AO = TargetPlanet.Center,
                AORadius = 75000f,
                type = TaskType.ClearAreaOfEnemies,
                TargetPlanet = TargetPlanet,
                TargetPlanetGuid = TargetPlanet.guid
            };


            closestCoreFleet.Owner.GetEmpireAI().TasksToAdd.Add(clearArea);
            clearArea.WhichFleet       = Owner.GetFleetsDict().FindFirstKeyForValue(closestCoreFleet);
            closestCoreFleet.FleetTask = clearArea;
            clearArea.IsCoreFleetTask  = true;
            closestCoreFleet.TaskStep  = 1;
            clearArea.Step             = 1;
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

            MinimumTaskForceStrength = EnemyStrength;
            if (MinimumTaskForceStrength < 1f)
            {
                return;
            }

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

            float battleFleetSize = 0.5f;

            if (CreateTaskFleet("Defensive Fleet", battleFleetSize) == RequisitionStatus.Complete)
            {
                Step = 1;
            }
        }

        void RequisitionClaimForce()
        {
            if (AO.AlmostZero())
                throw new Exception();

            AO closestAO = FindClosestAO();
            if (closestAO == null || closestAO.NumOffensiveForcePoolShips < 1)
            {
                return;
            }

            AO = TargetPlanet?.Center ?? AO;

            InitFleetRequirements(minFleetStrength: 100, minTroopStrength: 0, minBombMinutes: 0);

            float battleFleetSize = 0.25f;

            if (CreateTaskFleet("Scout Fleet", battleFleetSize) == RequisitionStatus.Complete)
            {
                Step = 1;
            }
        }

        void RequisitionExplorationForce()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            if (TargetPlanet.Owner != null && !Owner.IsEmpireAttackable(TargetPlanet.Owner))
            {
                EndTask();
                return;
            }

            AO = TargetPlanet.Center;
            InitFleetRequirements(minFleetStrength: 100, minTroopStrength: 40, minBombMinutes: 1);

            float battleFleetSize = 0.25f;

            if (CreateTaskFleet("Exploration Force", battleFleetSize) == RequisitionStatus.Complete)
            {
                Step = 1;
            }
        }

        void RequisitionAssaultForces()
        {
            if (AO.AlmostZero())
                Log.Error($"no area of operation set for task: {type}");

            if (TargetPlanet.Owner == null || TargetPlanet.Owner == Owner ||
                Owner.GetRelations(TargetPlanet.Owner).Treaty_Peace)
            {
                EndTask();
                return;
            }

            AO = TargetPlanet.Center;
            InitFleetRequirements(minFleetStrength: 100, minTroopStrength: 100 ,minBombMinutes: 1);

            float battleFleetSize = 0.25f;

            if (CreateTaskFleet("Invasion Fleet", battleFleetSize) == RequisitionStatus.Complete)
            {
                Step = 1;
            }
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
        RequisitionStatus CreateTaskFleet(string fleetName, float battleFleetSize)
        {
            //this determines what core fleet to send if the enemy is strong. 
            // its also an easy out for an empire in a bad state. 
            AO closestAO = FindClosestAO(MinimumTaskForceStrength);

            if (closestAO == null)
            {
                return RequisitionStatus.NoEmpireAreasOfOperation;
            }

            //where the fleet will gather after requisition before moving to target AO.
            Planet rallyPoint = Owner.FindNearestRallyPoint(AO);
            if (rallyPoint == null)
                return RequisitionStatus.NoRallyPoint;


            FleetShips fleetShips = AllFleetReadyShipsNearestTarget(rallyPoint.Center);
            fleetShips.WantedFleetCompletePercentage = battleFleetSize;

            //if have bombers but not enough... wait for more.
            if (Owner.canBuildBombers && fleetShips.BombSecsAvailable < TaskBombTimeNeeded)
                return RequisitionStatus.NotEnoughBomberStrength;

            //if we cant build bombers then convert bombtime to troops. 
            //This assume a standard troop strength of 10 
            if (!Owner.canBuildBombers)
                NeededTroopStrength += TaskBombTimeNeeded * 10;

            if (fleetShips.AccumulatedStrength < EnemyStrength)
            {
                //send a core fleet and wait.
                SendSofteningFleet(closestAO);
                return  RequisitionStatus.NotEnoughShipStrength;
            }

            //See if we need to gather troops from planets. Bail if not enough
            if (!AreThereEnoughTroopsToInvade(fleetShips, out Array<Troop> troopsOnPlanets, rallyPoint.Center))
                return RequisitionStatus.NotEnoughTroopStrength;

            //All's Good... Make a fleet
            TaskForce = fleetShips.ExtractShipSet(EnemyStrength, TaskBombTimeNeeded
                , NeededTroopStrength, troopsOnPlanets);
            if (TaskForce.IsEmpty)
                return RequisitionStatus.FailedToCreateAFleet;

            CreateFleet(TaskForce, fleetName);
            return RequisitionStatus.Complete;
        }

        void InitFleetRequirements(int minFleetStrength, int minTroopStrength, int minBombMinutes)
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
                        NeededTroopStrength = GetTargetPlanetGroundStrength(minTroopStrength);
                    if (minBombMinutes > 0)
                        TaskBombTimeNeeded = BombTimeNeeded().ClampMin(minBombMinutes);
                }
            }
            EnemyStrength = GetEnemyShipStrengthInAO(minFleetStrength); ;
        }



        enum RequisitionStatus
        {
            NoRallyPoint,
            NoEmpireAreasOfOperation,
            NotEnoughShipStrength,
            NotEnoughTroopStrength,
            NotEnoughBomberStrength,
            FailedToCreateAFleet,
            Complete
        }
    }
}