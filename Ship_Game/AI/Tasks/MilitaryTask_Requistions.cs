using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI.Tasks {
    public partial class MilitaryTask
    {
        public Planet GetTargetPlanet() => TargetPlanet;

        private Array<Troop> GetTroopsOnPlanets(Array<Troop> potentialTroops, Vector2 rallyPoint, int needed = 0)
        {
            var defenseDict = Owner.GetGSAI().DefensiveCoordinator.DefenseDict;
            var troopSystems = Owner.GetOwnedSystems().OrderBy(troopSource => defenseDict[troopSource].RankImportance)
                .ThenBy(dist => dist.Position.SqDist(rallyPoint));
            foreach (SolarSystem system in troopSystems)
            {
                int rank = (int) defenseDict[system].RankImportance;
                foreach (Planet planet in system.PlanetList)
                {
                    if (planet.Owner != Owner) continue;
                    if (planet.RecentCombat) continue;
                    int extra = needed == 0 ? rank / 3 : needed;
                    potentialTroops.AddRange(planet.GetEmpireTroops(Owner, extra));
                }
                if (potentialTroops.Count > 100)
                    break;
            }

            return potentialTroops;
        }

        private int CountShipTroopAndStrength(Array<Ship> potentialAssaultShips, out float ourStrength)
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

        private float GetEnemyStrAtTarget() => GetEnemyStrAtTarget(1000);

        private float GetEnemyStrAtTarget(float standardMinimum)
        {
            float MinimumEscortStrength = 1000;
            if (TargetPlanet.Owner == null)
                return standardMinimum;

            TargetPlanet.Owner.GetGSAI().DefensiveCoordinator.DefenseDict
                .TryGetValue(TargetPlanet.ParentSystem, out SystemCommander scom);
            float importance = 1;

            if (scom != null)
                importance = 1 + scom.RankImportance * .01f;

            float distance = 50000 * importance;
            MinimumEscortStrength = Owner.GetGSAI().ThreatMatrix.PingRadarStr(AO, distance, Owner);
            standardMinimum *= importance;
            if (MinimumEscortStrength < standardMinimum)
                MinimumEscortStrength = standardMinimum;

            return MinimumEscortStrength;
        }

        private float GetEnemyTroopStr() => TargetPlanet.GetGroundStrengthOther(Owner);

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


            Fleet newFleet = new Fleet()
            {
                Owner = Owner,
                Name = fleetName
            };

            int FleetNum = FindFleetNumber();
            float ForceStrength = 0f;

            foreach (Ship ship in potentialAssaultShips)
            {
                if (ForceStrength > EnemyTroopStrength * 1.5f)
                    break;

                newFleet.AddShip(ship);
                ForceStrength += ship.PlanetAssaultStrength;
            }

            foreach (Troop t in potentialTroops)
            {
                if (ForceStrength > EnemyTroopStrength * 1.5f)
                    break;

                if (t.GetPlanet() == null || !(t.GetPlanet().ParentSystem.combatTimer <= 0) ||
                    t.GetPlanet().RecentCombat) continue;
                if (t.GetOwner() == null) continue;
                newFleet.AddShip(t.Launch());
                ForceStrength += t.Strength;
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

                Owner.GetGSAI().RemoveShipFromForce(ship, closestAO);
            }
            newFleet.AutoArrange();
            Step = 1;
        }

        private void GetAvailableShips(AO area, Array<Ship> bombers, Array<Ship> combat, Array<Ship> troopShips,
            Array<Ship> utility)
        {
            var ships = area.GetOffensiveForcePool().Union(Owner.GetForcePool());
            foreach (Ship ship in ships)
            {
                if (ship.fleet != null)
                    Log.Info("GetAvailableShips: a ship is in fleet '{0}' and not available for {1}", ship.fleet.Name,
                        type.ToString());
                if (area.GetWaitingShips().ContainsRef(ship))
                    Log.Error("ship is in waiting list and should not be");

                if (Empire.Universe.Debug)
                    foreach (AO ao in Owner.GetGSAI().AreasOfOperations)
                    {
                        if (ao == area) continue;
                        if (ao.GetOffensiveForcePool().Contains(ship))
                            Log.Error("Ship {0} in another AO {1}", ship.Name, ao.GetPlanet().Name);
                    }
                if ((ship.shipData.Role == ShipData.RoleName.station ||
                     ship.shipData.Role == ShipData.RoleName.platform)
                    || !ship.BaseCanWarp
                    || ship.InCombat
                    || ship.fleet != null
                    || ship.Mothership != null
                    || ship.AI.State != AIState.AwaitingOrders
                    || (ship.System != null && ship.System.CombatInSystem))
                    continue;

                if (utility != null && (ship.DesignRole == ShipData.RoleName.support))                    
                {
                    utility.Add(ship);
                }
                else if (bombers != null && ship.DesignRole == ShipData.RoleName.bomber)
                {
                    bombers.Add(ship);
                }
                else if (troopShips != null && (ship.DesignRole == ShipData.RoleName.troop || ship.DesignRole == ShipData.RoleName.troopShip))
                {
                    troopShips.Add(ship);
                }
                else if (combat != null && ship.DesignRole >= ShipData.RoleName.fighter && ship.DesignRole < ShipData.RoleName.prototype
                    && ship.DesignRole != ShipData.RoleName.scout)
                {
                    combat.Add(ship);
                }
            }
        }

        private Array<Ship> GetShipsFromDefense(float tfstrength, float minimumEscortStrength)
        {
            Array<Ship> elTaskForce = new Array<Ship>();
            if (!Owner.isFaction && Owner.data.DiplomaticPersonality.Territorialism < 50 &&
                tfstrength < minimumEscortStrength)
            {
                if (!IsCoreFleetTask)
                    foreach (var kv in Owner.GetGSAI().DefensiveCoordinator.DefenseDict
                        .OrderByDescending(system => system.Key.CombatInSystem
                            ? 1
                            : 2 * system.Key.Position.SqDist(TargetPlanet.Center))
                        .ThenByDescending(ship => (ship.Value.GetOurStrength() - ship.Value.IdealShipStrength) < 1000)
                    )
                    {
                        var ships = kv.Value.GetShipList;

                        for (int index = 0; index < ships.Length; index++)
                        {
                            Ship ship = ships[index];
                            if (ship.AI.BadGuysNear || ship.fleet != null || tfstrength >= minimumEscortStrength ||
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
            float enemyTroopStr = GetEnemyTroopStr();
            if (enemyTroopStr < 100)
                enemyTroopStr = 100;

            float enemyShipStr = GetEnemyStrAtTarget();
            IOrderedEnumerable<AO> sorted =
                from ao in Owner.GetGSAI().AreasOfOperations
                where ao.GetCoreFleet().FleetTask == null || ao.GetCoreFleet().FleetTask.type != TaskType.AssaultPlanet
                orderby ao.GetOffensiveForcePool().Where(combat => !combat.InCombat)
                            .Sum(strength => strength.BaseStrength) >= MinimumTaskForceStrength descending, Vector2.Distance(AO, ao.Center)
                select ao;

            if (!sorted.Any())
                return;

            var bombers        = new Array<Ship>();
            var everythingElse = new Array<Ship>();
            var troopShips     = new Array<Ship>();
            var troops         = new Array<Troop>();

            foreach (AO area in sorted)
            {
                GetAvailableShips(area, bombers, everythingElse, troopShips, everythingElse);
                foreach (Planet p in area.GetPlanets())
                {
                    if (p.RecentCombat || p.ParentSystem.combatTimer > 0)
                        continue;

                    foreach (Troop t in p.TroopsHere)
                    {
                        if (t.GetOwner() != Owner)
                            continue;

                        troops.Add(t);
                    }
                }
            }

            everythingElse.AddRange(troopShips);
            var ships         = new Array<Ship>();
            float strAdded    = 0f;
            float troopStr    = 0f;
            int numOfTroops   = 0;

            foreach (Ship ship in everythingElse)
            {
                if (strAdded < enemyShipStr * 1.65f)
                    break;

                if (ship.HasTroopBay)
                {
                    troopStr    += ship.GetHangars().Count * 10;
                    numOfTroops += ship.GetHangars().Count;                    
                }
                ships.Add(ship);
                strAdded += ship.GetStrength();
            }

            var bombTaskForce = new Array<Ship>();
            int numBombs      = 0;
            foreach (Ship ship in bombers)
            {
                if (numBombs >= 20 || bombTaskForce.Contains(ship))
                    continue;

                if (ship.HasTroopBay)
                {
                    troopStr += ship.GetHangars().Count * 10;
                    numOfTroops += ship.GetHangars().Count;
                }
                bombTaskForce.Add(ship);
                numBombs += ship.BombBays.Count;
            }

            var potentialTroops = new Array<Troop>();
            foreach (Troop t in troops)
            {
                if (troopStr > enemyTroopStr * 1.5f || numOfTroops > TargetPlanet.GetGroundLandingSpots())
                    break;

                potentialTroops.Add(t);
                troopStr += (float) t.Strength;
                numOfTroops++;
            }

            if (strAdded > enemyShipStr * 1.65f)
            {
                if (TargetPlanet.Owner == null || TargetPlanet.Owner != null &&
                    !Owner.TryGetRelations(TargetPlanet.Owner, out Relationship rel))
                {
                    EndTask();
                    return;
                }

                if (Owner.GetRelations(TargetPlanet.Owner).PreparingForWar)
                {
                    Owner.GetGSAI().DeclareWarOn(TargetPlanet.Owner,
                        Owner.GetRelations(TargetPlanet.Owner).PreparingForWarType);
                }

                var closestAO = sorted.First<AO>();
                var assault = new MilitaryTask(Owner)
                {
                    AO = TargetPlanet.Center,
                    AORadius = 75000f,
                    type = MilitaryTask.TaskType.AssaultPlanet
                };

                closestAO.GetCoreFleet().Owner.GetGSAI().TasksToAdd.Add(assault);
                assault.WhichFleet = closestAO.WhichFleet;
                closestAO.GetCoreFleet().FleetTask = assault;
                assault.IsCoreFleetTask = true;
                assault.Step = 1;

                assault.TargetPlanet = TargetPlanet;
                closestAO.GetCoreFleet().TaskStep = 0;
                closestAO.GetCoreFleet().Name = "Doom Fleet";
                foreach (Ship ship in ships)
                {
                    ship.fleet?.RemoveShip(ship);

                    ship.AI.OrderQueue.Clear();
                    Owner.GetGSAI().DefensiveCoordinator.Remove(ship);


                    closestAO.GetCoreFleet().AddShip(ship);
                }

                foreach (Troop t in potentialTroops)
                {
                    if (t.GetPlanet() == null)
                        continue;

                    Ship launched = t.Launch();
                    closestAO.GetCoreFleet().AddShip(launched);
                }

                closestAO.GetCoreFleet().AutoArrange();
                if (bombers.Count > 0 && numBombs > 6)
                {
                    MilitaryTask GlassPlanet = new MilitaryTask(Owner)
                    {
                        AO = TargetPlanet.Center,
                        AORadius = 75000f,
                        type = MilitaryTask.TaskType.GlassPlanet,
                        TargetPlanet = TargetPlanet,
                        WaitForCommand = true
                    };

                    var bomberFleet = new Fleet()
                    {
                        Owner = Owner
                    };

                    bomberFleet.Owner.GetGSAI().TasksToAdd.Add(GlassPlanet);
                    GlassPlanet.WhichFleet = Owner.GetUnusedKeyForFleet();
                    Owner.GetFleetsDict().Add(GlassPlanet.WhichFleet, bomberFleet);
                    bomberFleet.FleetTask = GlassPlanet;
                    bomberFleet.Name = "Bomber Fleet";

                    foreach (Ship ship in bombTaskForce)
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
            

            if (closestAO == null || closestAO.GetOffensiveForcePool().Count == 0)
                return;

            if (Owner.GetRelations(TargetPlanet.Owner).Treaty_Peace)
            {
                Owner.GetRelations(TargetPlanet.Owner).PreparingForWar = false;
                EndTask();
                return;
            }

            float enemyTroopStrength = TargetPlanet.GetGroundStrengthOther(Owner);

            if (enemyTroopStrength < 100f)
                enemyTroopStrength = 100f;

            Array<Ship> potentialAssaultShips = new Array<Ship>();
            Array<Troop> potentialTroops = new Array<Troop>();
            Array<Ship> potentialCombatShips = new Array<Ship>();
            Array<Ship> potentialBombers = new Array<Ship>();
            Array<Ship> potentialUtilityShips = new Array<Ship>();
            GetAvailableShips(closestAO, potentialBombers, potentialCombatShips, potentialAssaultShips,
                potentialUtilityShips);
            Planet rallypoint = Owner.FindNearestRallyPoint(AO);
            if (rallypoint == null)
                return;

            potentialTroops = GetTroopsOnPlanets(potentialTroops, rallypoint.Center);
            int troopCount = potentialTroops.Count();
            troopCount += CountShipTroopAndStrength(potentialAssaultShips, out float ourAvailableStrength);

            foreach (Troop t in potentialTroops)
                ourAvailableStrength = ourAvailableStrength + t.Strength;
            if (ourAvailableStrength < enemyTroopStrength)
                return;

            float minimumEscortStrength = GetEnemyStrAtTarget(Owner.currentMilitaryStrength * .05f);

            // I'm unsure on ball-park figures for ship strengths. Given it used to build up to 1500, sticking flat +300 on seems a good start
            //updated. Now it will use 1/10th of the current military strength escort strength needed is under 1000
            //well thats too much. 1/10th can be huge. moved it into the getenemy strength logic with some adjustments. now it looks at the enemy empires importance of the planet. 
            //sort of cheating but as it would be much the same calculation as the attacking empire would use.... hrmm.
            // actually i think the raw importance value could be used to create an importance for that planet. interesting... that could be very useful in many areas. 

            MinimumTaskForceStrength = minimumEscortStrength;
            BatchRemovalCollection<Ship> elTaskForce = new BatchRemovalCollection<Ship>();
            float tfstrength = 0f;
            elTaskForce.AddRange(AddShipsLimited(potentialCombatShips, minimumEscortStrength, tfstrength,
                out float tempStrength));
            tfstrength += tempStrength;

            elTaskForce.AddRange(AddShipsLimited(potentialUtilityShips, minimumEscortStrength * 1.5f, tfstrength,
                out tempStrength));
            tfstrength += tempStrength;

            elTaskForce.AddRange(GetShipsFromDefense(tfstrength, minimumEscortStrength));
            if (tfstrength >= MinimumTaskForceStrength)
            {
                if (ourAvailableStrength >= enemyTroopStrength && landingSpots > 8 && troopCount >= 10)
                {
                    DeclareWar();
                    CreateFleet(elTaskForce, potentialAssaultShips, potentialTroops, enemyTroopStrength, closestAO);
                    return;
                }
                if (landingSpots < 10 && potentialBombers.Count > 10 - landingSpots)
                {
                    DeclareWar();
                    CreateFleet(elTaskForce, potentialAssaultShips, potentialTroops, enemyTroopStrength, closestAO,
                        potentialBombers);
                    return;
                }
                if (landingSpots > 0)
                {
                    DeclareWar();
                    CreateFleet(elTaskForce, potentialAssaultShips, potentialTroops, enemyTroopStrength * 2, closestAO);
                    return;
                }
            }
            else if (tfstrength <= MinimumTaskForceStrength)
            {
                if (TargetPlanet.Owner == null || TargetPlanet.Owner != null &&
                    !Owner.TryGetRelations(TargetPlanet.Owner, out Relationship rel2))
                {
                    EndTask();
                    return;
                }

                Fleet closestCoreFleet = closestAO.GetCoreFleet();
                if (closestCoreFleet.FleetTask == null && closestCoreFleet.GetStrength() > MinimumTaskForceStrength)
                {
                    var clearArea = new MilitaryTask(closestCoreFleet.Owner)
                    {
                        AO = TargetPlanet.Center,
                        AORadius = 75000f,
                        type = TaskType.ClearAreaOfEnemies
                    };

                    closestCoreFleet.Owner.GetGSAI().TasksToAdd.Add(clearArea);
                    clearArea.WhichFleet = closestAO.WhichFleet;
                    closestCoreFleet.FleetTask = clearArea;
                    clearArea.IsCoreFleetTask = true;
                    closestCoreFleet.TaskStep = 1;
                    clearArea.Step = 1;

                    if (Owner.GetRelations(TargetPlanet.Owner).PreparingForWar)
                        Owner.GetGSAI().DeclareWarOn(TargetPlanet.Owner,
                            Owner.GetRelations(TargetPlanet.Owner).PreparingForWarType);
                }
                return;
            }
            if (landingSpots < 10) IsToughNut = true;

            NeededTroopStrength = (int) (enemyTroopStrength - ourAvailableStrength);
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

        private bool RequisitionClaimForce()
        {
            AO closestAO = null;
            if ((closestAO = FindClosestAO()) == null) return false; 
            float tfstrength = 0f;
            Array<Ship> elTaskForce = new Array<Ship>();
            int shipCount = 0;
            float strengthNeeded = EnemyStrength;

            if (strengthNeeded < 1)
                strengthNeeded = Owner.GetGSAI().ThreatMatrix.PingRadarStr(TargetPlanet.Center, 125000, Owner);

            foreach (Ship ship in closestAO.GetOffensiveForcePool().OrderBy(str => str.GetStrength()))
            {
                if (shipCount >= 3 && (strengthNeeded < Owner.currentMilitaryStrength * .02f &&
                                       strengthNeeded < tfstrength))
                    break;

                if (ship.GetStrength() <= 0f || ship.InCombat || ship.fleet != null)
                    continue;

                shipCount++;
                if (elTaskForce.Contains(ship))
                    Log.Error("eltaskforce already contains ship");
                elTaskForce.Add(ship);
                tfstrength += ship.GetStrength();
            }

            if (shipCount < 3 && tfstrength == 0 || tfstrength < strengthNeeded)
                return false;

            TaskForce = elTaskForce;
            StartingStrength = tfstrength;
            int FleetNum = FindFleetNumber();
            Fleet newFleet = new Fleet();
            foreach (Ship ship in TaskForce)
            {
                Owner.GetGSAI().RemoveShipFromForce(ship, closestAO);
                newFleet.AddShip(ship);
            }


            newFleet.Owner = Owner;
            newFleet.Name = "Scout Fleet";
            newFleet.AutoArrange();
            Owner.GetFleetsDict()[FleetNum] = newFleet;
            Owner.GetGSAI().UsedFleets.Add(FleetNum);
            WhichFleet = FleetNum;
            newFleet.FleetTask = this;


            return true;
        }

        private AO FindClosestAO()
        {
            var aos = Owner.GetGSAI().AreasOfOperations;
            if (aos.Count == 0) return null;
            if (aos == null)
            {
                Log.Error("{0} has no areas of operation", Owner.Name);
                return null;
            }
            AO closestAO =
                aos.FindMaxFiltered(ao => ao.GetOffensiveForcePool().Count > 4 ,
                    ao => -ao.Center.SqDist(AO)) ??
                aos.FindMin(ao => ao.Center.SqDist(AO));
            if (closestAO == null)
            {               
                Log.Error("{0} : no areas of operation found", Owner.Name);
                return null;
            }
            return closestAO;
        }

        private void RequisitionExplorationForce()
        {
            AO closestAO = FindClosestAO();



            if (closestAO == null || closestAO.GetOffensiveForcePool().Count < 1)
            {
                EndTask();
                return;
            }

            Planet rallyPoint = closestAO.GetPlanets().Intersect(Owner.RallyPoints).ToArrayList()
                .FindMin(p => p.Center.SqDist(AO));

            if (rallyPoint == null)
            {
                EndTask();
                return;
            }
            EnemyStrength = 0f;
            EnemyStrength = Owner.GetGSAI().ThreatMatrix.PingRadarStrengthLargestCluster(AO, AORadius, Owner);
            
            MinimumTaskForceStrength = EnemyStrength + 0.35f * EnemyStrength;

            if (MinimumTaskForceStrength < 1f)
                MinimumTaskForceStrength = Owner.currentMilitaryStrength *.05f;


            Array<Troop> potentialTroops = new Array<Troop>();
            potentialTroops = GetTroopsOnPlanets(potentialTroops, closestAO. GetPlanet().Center);
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
            GetAvailableShips(closestAO, potentialBombers, potentialCombatShips, potentialAssaultShips,
                potentialUtilityShips);


            float ourAvailableStrength = 0f;
            CountShipTroopAndStrength(potentialAssaultShips, out float troopStrength);
            ourAvailableStrength += troopStrength;


            foreach (Troop t in potentialTroops)
                ourAvailableStrength = ourAvailableStrength + (float) t.Strength;


            float tfstrength = 0f;
            Array<Ship> elTaskForce = AddShipsLimited(potentialCombatShips, MinimumTaskForceStrength, tfstrength,
                out tfstrength);

            if (tfstrength >= MinimumTaskForceStrength && ourAvailableStrength >= 20f)
            {
                StartingStrength = tfstrength;
                CreateFleet(elTaskForce, potentialAssaultShips, potentialTroops, 20, closestAO, null,
                    "Exploration Force");
            }
        }

        private void RequisitionForces()
        {
            var sorted = Owner.GetGSAI().AreasOfOperations
                .OrderByDescending(ao => ao.GetOffensiveForcePool().Sum(strength => strength.GetStrength()) >=
                                         MinimumTaskForceStrength)
                .ThenBy(ao => Vector2.Distance(AO, ao.Center)).ToArray();

            if (sorted.Length == 0)
                return;

            AO closestAO = sorted[0];
            EnemyStrength = Owner.GetGSAI().ThreatMatrix.PingRadarStrengthLargestCluster(AO, AORadius, Owner,100000);

            MinimumTaskForceStrength = EnemyStrength;
            if (MinimumTaskForceStrength < 1f)
            {
                EndTask();
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