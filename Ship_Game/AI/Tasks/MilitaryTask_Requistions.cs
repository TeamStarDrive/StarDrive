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
        //public Planet TargetPlanet => TargetPlanet;

        Array<Troop> GetTroopsOnPlanets(Vector2 rallyPoint, int needed = 0)
        {
            var potentialTroops = new Array<Troop>();
            var defenseDict = Owner.GetEmpireAI().DefensiveCoordinator.DefenseDict;
            var troopSystems = Owner.GetOwnedSystems().Sorted(dist => dist.Position.SqDist(rallyPoint));
            for (int x = 0; x < troopSystems.Length; x++)
            {
                SolarSystem system = troopSystems[x];
                int rank = (int) defenseDict[system].RankImportance;
                for (int i = 0; i < system.PlanetList.Count; i++)
                {
                    Planet planet = system.PlanetList[i];
                    if (planet.Owner != Owner) continue;
                    if (planet.RecentCombat) continue;
                    potentialTroops.AddRange(planet.GetOwnersLaunchReadyTroops(20 - rank));
                }

                if (potentialTroops.Count > 50)
                    break;
            }

            return potentialTroops;
        }

        int CountShipTroopAndStrength(Array<Ship> potentialAssaultShips, out float ourStrength)
        {
            ourStrength = 0;
            int troopCount = 0;
            foreach (Ship ship in potentialAssaultShips)
            {
                int hangars = 0;
                foreach (ShipModule hangar in ship.Carrier.AllActiveHangars)
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

        private Array<Ship> AddShipsLimited(Array<Ship> shipList, float strengthLimit, float tfStrength,
            out float currentStrength)
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

       private void CreateFleet(Array<Ship> elTaskForce, Array<Ship> potentialAssaultShips,
            Array<Troop> potentialTroops, float EnemyTroopStrength, AO closestAO, Array<Ship> potentialBombers = null,
            string fleetName = "Invasion Fleet")
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


            Fleet newFleet = new Fleet
            {
                Owner = Owner,
                Name = fleetName
            };

            
            float ForceStrength = 0f;

            foreach (Ship ship in potentialAssaultShips)
            {
                if (ForceStrength > EnemyTroopStrength * 1.5f)
                    break;

                newFleet.AddShip(ship);
                ForceStrength += ship.Carrier.PlanetAssaultStrength;
            }

            foreach (Troop t in potentialTroops.Where(planet => planet.HostPlanet != null)
                .OrderBy(troop => troop.HostPlanet.RecentCombat ? 1 :0)
                .ThenBy(troop => troop.HostPlanet.ParentSystem.HostileForcesPresent(Owner) ? 1 : 0)
                .ThenBy(troop => troop.HostPlanet.Center.SqDist(AO))
            )
            {
                if (ForceStrength > EnemyTroopStrength * 1.5f)
                    break;
                if (t.Loyalty == null || !t.CanMove)
                    continue;
                Ship launched = t.Launch();
                if (launched == null)
                {
                    Log.Warning($"CreateFleet: Troop launched from planet became null");
                    continue;
                }
                newFleet.AddShip(launched);
                ForceStrength += t.Strength;
            }

            int FleetNum = FindUnusedFleetNumber();
            Owner.GetFleetsDict()[FleetNum] = newFleet;
            Owner.GetEmpireAI().UsedFleets.Add(FleetNum);
            WhichFleet = FleetNum;
            newFleet.FleetTask = this;
            foreach (Ship ship in elTaskForce)
            {
                newFleet.AddShip(ship);
                ship.AI.ClearOrders();
                Owner.GetEmpireAI().RemoveShipFromForce(ship, closestAO);
            }
            newFleet.AutoArrange();
            Step = 1;
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

        private FleetShips GatherFleetReadyShips(AO ao)
        {
            FleetShips fleetShips = ao.GetFleetShips();
            Ship[] sorted = Owner.GetForcePool().Sorted(s => s.Center.SqDist(ao.Center));
            foreach (Ship ship in sorted)
            {
                if (!ship.InCombat && !ship.AI.BadGuysNear)
                    fleetShips.AddShip(ship);
            }
            return fleetShips;
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

        private FleetShips GetAvailableShips(AO area)
        {
            var fleetShips = area.GetFleetShips();
            var ships = Owner.GetForcePool();
            foreach (Ship ship in ships)
                fleetShips.AddShip(ship);

            return fleetShips;
        }

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

        private void RequisitionAssaultForces()
        {
            if (TargetPlanet.Owner == null || TargetPlanet.Owner == Owner ||
                Owner.GetRelations(TargetPlanet.Owner).Treaty_Peace)
            {
                EndTask();
                return;
            }

            AO closestAO = FindClosestAO(MinimumTaskForceStrength);

            if (closestAO == null)
            {
                EndTask();
                return;
            }

            Planet rallyPoint = Owner.FindNearestRallyPoint(TargetPlanet.Center);
            if (rallyPoint == null)
                return;

            FleetShips fleetShips = AllFleetReadyShipsNearestTarget(TargetPlanet.Center);
            AO = TargetPlanet.Center;
            EnemyStrength = Owner.GetEmpireAI().ThreatMatrix.PingNetRadarStr(TargetPlanet.Center, AORadius * 2, Owner);
            
            if (fleetShips.AccumulatedStrength < EnemyStrength)
            {
                SendSofteningFleet(closestAO);
                return; 
            }

            NeededTroopStrength = (int)TargetPlanet.GetGroundStrengthOther(Owner).ClampMin(100);

            //Bomb time is number of bombing minutes wanted. 
            int bombTimeNeeded = BombTimeNeeded();
            if (fleetShips.BombSecsAvailable < bombTimeNeeded)
                return;

            //See if we need to gather troops from planets. Bail if not enough
            var troopsOnPlanets = new Array<Troop>();
            if (fleetShips.InvasionTroopStrength < NeededTroopStrength)
            {
                troopsOnPlanets = GetTroopsOnPlanets(TargetPlanet.Center);
                float troopsOnPlanetsStrength = troopsOnPlanets.Sum(t => t.Strength);
                if (fleetShips.InvasionTroopStrength + troopsOnPlanetsStrength < NeededTroopStrength)
                    return;
            }

            //All's Good... Make a fleet
            var ships = fleetShips.CollectShipSet(EnemyStrength, bombTimeNeeded
                , NeededTroopStrength, troopsOnPlanets);
            if (ships.IsEmpty)
                return;

            CreateFleet(ships, "Invasion Fleet");

            if (Step > 0)
                DeclareWar();
            Step = 1;
        }

        private int BombTimeNeeded()
        {
            int bombersWanted = TargetPlanet.BuildingGeodeticCount * 5;
            bombersWanted += TargetPlanet.GetGroundLandingSpots() < 25 ? 1 : 0;
            bombersWanted += TargetPlanet.ShieldStrengthMax > 0 ? 10 : 0;
            return bombersWanted;
        }

        bool SendSofteningFleet(AO closestAO)
        {
            Fleet closestCoreFleet = FindClosestCoreFleet(MinimumTaskForceStrength);
            if (closestCoreFleet == null || closestCoreFleet.FleetTask != null)
                return false;
            var clearArea = new MilitaryTask(closestCoreFleet.Owner)
            {
                AO = TargetPlanet.Center,
                AORadius = 75000f,
                type = TaskType.ClearAreaOfEnemies,
                TargetPlanet = TargetPlanet,
                TargetPlanetGuid = TargetPlanet.guid
            };


            closestCoreFleet.Owner.GetEmpireAI().TasksToAdd.Add(clearArea);
            clearArea.WhichFleet = Owner.GetFleetsDict().FindFirstKeyForValue(closestCoreFleet);
            closestCoreFleet.FleetTask = clearArea;
            clearArea.IsCoreFleetTask = true;
            closestCoreFleet.TaskStep = 1;
            clearArea.Step = 1;

            return true;
        }

        void RequisitionDefenseForce()
        {
            float forcePoolStr = Owner.GetForcePoolStrength();
            float tfstrength = 0f;
            var elTaskForce = new Array<Ship>();

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
            int fleetId = FindUnusedFleetNumber();

            var newFleet = new Fleet();

            foreach (Ship ship in TaskForce)
            {
                newFleet.AddShip(ship);
            }

            newFleet.Owner = Owner;
            newFleet.Name = "Defensive Fleet";
            newFleet.AutoArrange();
            Owner.GetFleetsDict()[fleetId] = newFleet;
            Owner.GetEmpireAI().UsedFleets.Add(fleetId);
            WhichFleet = fleetId;
            newFleet.FleetTask = this;

            foreach (Ship ship in TaskForce)
            {
                Owner.ForcePoolRemove(ship);
            }
            Step = 1;
        }

        bool RequisitionClaimForce()
        {
            float strengthNeeded = EnemyStrength;

            if (strengthNeeded < 1)
                strengthNeeded = Owner.GetEmpireAI().ThreatMatrix.PingRadarStr(TargetPlanet.Center, 125000, Owner);

            AO ao = FindClosestAO();

            var forcePool = Owner.GetShipsFromOffensePools();

            // fix sentry bug: https://sentry.io/blackboxmod/blackbox/issues/626773068/
            if (!forcePool.IsEmpty && ao != null)
                forcePool.Sort(s => s.Center.Distance(ao.Center));

            FleetShips fleetShips = new FleetShips(Owner);
            foreach (var ship in forcePool)
                fleetShips.AddShip(ship);

            fleetShips.GatherSetsOfFleetShipsUpToStrength(strengthNeeded,0.2f , out Array<Ship> ships);

            if (ships.Count < 3 || fleetShips.AccumulatedStrength < strengthNeeded * 0.9f)
                return false;

            CreateFleet( ships, "Scout Fleet");
            StartingStrength = fleetShips.AccumulatedStrength;

            return true;
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

        void RequisitionExplorationForce()
        {
            AO closestAO = FindClosestAO();
            if (closestAO == null || closestAO.NumOffensiveForcePoolShips < 1)
            {
                //EndTask();
                return;
            }

            Planet rallyPoint = closestAO.GetPlanets().Intersect(Owner.RallyPoints)
                                .ToArrayList().FindMin(p => p.Center.SqDist(AO));
            if (rallyPoint == null)
            {
                //EndTask();
                return;
            }

            EnemyStrength = 0f;
            EnemyStrength = Owner.GetEmpireAI().ThreatMatrix.PingRadarStrengthLargestCluster(AO, AORadius, Owner);

            MinimumTaskForceStrength = EnemyStrength;

            Array<Troop> potentialTroops = GetTroopsOnPlanets(closestAO.GetPlanet().Center);
            if (potentialTroops.Count < 4)
            {
                NeededTroopStrength = 20;
                for (int i = 0; i < potentialTroops.Count; i++)
                {
                    Troop troop = potentialTroops[i];
                    NeededTroopStrength -= (int) troop.Strength;
                }
                NeededTroopStrength = 0;
            }

            FleetShips fleet = GetAvailableShips(closestAO);
            Array<Ship> potentialAssaultShips = fleet.GetTroops(4);
            fleet.GatherSetsOfFleetShipsUpToStrength(EnemyStrength, 0.25f, out Array<Ship> potentialCombatShips);

            float ourAvailableStrength = 0f;
            CountShipTroopAndStrength(potentialAssaultShips, out float troopStrength);
            ourAvailableStrength += troopStrength;

            foreach (Troop t in potentialTroops)
                ourAvailableStrength = ourAvailableStrength + t.Strength;

            float tfstrength = 0f;
            Array<Ship> elTaskForce = AddShipsLimited(potentialCombatShips,
                                                      MinimumTaskForceStrength, tfstrength, out tfstrength);

            if (tfstrength >= MinimumTaskForceStrength && ourAvailableStrength >= 20f)
            {
                StartingStrength = tfstrength;
                CreateFleet(elTaskForce, potentialAssaultShips, potentialTroops, 20, closestAO, null, "Exploration Force");
            }
        }

        private void RequisitionForces()
        {
            AO[] sorted = Owner.GetEmpireAI().AreasOfOperations
                .OrderByDescending(ao => ao.OffensiveForcePoolStrength >= MinimumTaskForceStrength)
                .ThenBy(ao => AO.Distance(ao.Center)).ToArray();

            if (sorted.Length == 0)
                return;

            AO closestAO = sorted[0];
            EnemyStrength = Owner.GetEmpireAI().ThreatMatrix.PingRadarStr(AO, 10000, Owner,factionOnly:false);

            MinimumTaskForceStrength = EnemyStrength;
            if (MinimumTaskForceStrength < 1f)
            {
                //EndTask();
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
    }
}