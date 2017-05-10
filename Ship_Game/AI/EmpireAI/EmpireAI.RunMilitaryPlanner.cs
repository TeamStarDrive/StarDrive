using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {



        private void RunMilitaryPlanner()
        {
            #region ShipBuilding

            Nobuild = false;
            var shipCountLimit = GlobalStats.ShipCountLimit;
            if (!OwnerEmpire.MinorRace)
                RunGroundPlanner();
            NumberOfShipGoals = 0;
            foreach (var p in OwnerEmpire.GetPlanets())
            {
                if
                ((p.WorkerPercentage) > .75 || p.GetMaxProductionPotential() < 2f
                )
                {
                    continue;
                }

                NumberOfShipGoals++;
            }

            float numgoals = 0f;
            float underConstruction = 0f;
            float troopStrengthUnderConstruction = 0f;
            foreach (var g in Goals)
            {
                if (g.GoalName == "BuildOffensiveShips" || g.GoalName == "BuildDefensiveShips")
                {
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        underConstruction = underConstruction +
                                            ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCostRealism();
                    }
                    else
                    {
                        underConstruction = underConstruction +
                                            ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(OwnerEmpire);
                    }
                    foreach (var t in ResourceManager.ShipsDict[g.ToBuildUID].TroopList)
                    {
                        troopStrengthUnderConstruction = troopStrengthUnderConstruction + t.Strength;
                    }
                    numgoals = numgoals + 1f;
                }
                if (g.GoalName != "BuildConstructionShip")
                {
                    continue;
                }
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                {
                    underConstruction = underConstruction +
                                        ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCostRealism();
                }
                else
                {
                    underConstruction = underConstruction +
                                        ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(OwnerEmpire);
                }
            }

            float offenseNeeded = 0;
            offenseNeeded += ThreatMatrix.StrengthOfAllThreats(OwnerEmpire);
            offenseNeeded /= OwnerEmpire.currentMilitaryStrength;

            if (offenseNeeded <= 0)            
                offenseNeeded = 0;
            
            if (offenseNeeded > 20)
                offenseNeeded = 20;
            NumberOfShipGoals += (int) offenseNeeded;

            float atWarBonus = 0.05f;
            if (OwnerEmpire.Money > 500f)
                atWarBonus += (offenseNeeded * (0.03f + OwnerEmpire.getResStrat().MilitaryPriority * .03f));
            float capacity =
                OwnerEmpire.Grossincome() * (.25f + atWarBonus) -
                underConstruction;
            float allowableDeficit =
                -(OwnerEmpire.Money * .05f) *
                atWarBonus;

            if (capacity > BuildCapacity)
                BuildCapacity = capacity;
            OwnerEmpire.data.ShipBudget = BuildCapacity;
            if (capacity - OwnerEmpire.GetTotalShipMaintenance() - allowableDeficit <= 0f)
            {
                capacity -= OwnerEmpire.GetTotalShipMaintenance() - allowableDeficit;
                float howMuchWeAreScrapping = 0f;

                foreach (var ship1 in OwnerEmpire.GetShips())
                {
                    if (ship1.AI.State != AIState.Scrap)                    
                        continue;
                    
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        howMuchWeAreScrapping = howMuchWeAreScrapping + ship1.GetMaintCostRealism();
                    }
                    else
                    {
                        howMuchWeAreScrapping = howMuchWeAreScrapping + ship1.GetMaintCost(OwnerEmpire);
                    }
                }
                if (howMuchWeAreScrapping < Math.Abs(capacity))
                {
                    var added = 0f;

                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        foreach (var g in Goals
                            .Where(goal => goal.GoalName == "BuildOffensiveShips" ||
                                           goal.GoalName == "BuildDefensiveShips")
                            .OrderByDescending(goal => ResourceManager.ShipsDict[goal.ToBuildUID]
                                .GetMaintCostRealism()))
                        {
                            bool flag = false;
                            if (g.GetPlanetWhereBuilding() == null)
                                continue;
                            foreach (var shipToRemove in g.GetPlanetWhereBuilding().ConstructionQueue)
                            {
                                if (shipToRemove.Goal != g || shipToRemove.productionTowards > 0f)
                                {
                                    continue;
                                }
                                g.GetPlanetWhereBuilding().ConstructionQueue.QueuePendingRemoval(shipToRemove);
                                Goals.QueuePendingRemoval(g);
                                added += ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCostRealism();
                                flag = true;
                                break;
                            }
                            if (flag)
                                g.GetPlanetWhereBuilding().ConstructionQueue.ApplyPendingRemovals();
                            if (howMuchWeAreScrapping + added >= Math.Abs(capacity))
                                break;
                        }
                    }
                    else
                    {
                        foreach (var g in Goals
                            .Where(goal => goal.GoalName == "BuildOffensiveShips" ||
                                           goal.GoalName == "BuildDefensiveShips")
                            .OrderByDescending(goal => ResourceManager.ShipsDict[goal.ToBuildUID]
                                .GetMaintCost(OwnerEmpire)))
                        {
                            var flag = false;
                            if (g.GetPlanetWhereBuilding() == null)
                                continue;
                            foreach (var shipToRemove in g.GetPlanetWhereBuilding().ConstructionQueue)
                            {
                                if (shipToRemove.Goal != g || shipToRemove.productionTowards > 0f)
                                {
                                    continue;
                                }
                                g.GetPlanetWhereBuilding().ConstructionQueue.QueuePendingRemoval(shipToRemove);
                                Goals.QueuePendingRemoval(g);
                                added += ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(OwnerEmpire);
                                flag = true;
                                break;
                            }
                            if (flag)
                                g.GetPlanetWhereBuilding().ConstructionQueue.ApplyPendingRemovals();
                            if (howMuchWeAreScrapping + added >= Math.Abs(capacity))
                                break;
                        }
                    }

                    Goals.ApplyPendingRemovals();
                    capacity = capacity + howMuchWeAreScrapping + added;
                }
                BuildCapacity = capacity;
            }

            if (BuildCapacity < 0)
                GetAShip(BuildCapacity);

            bool def = false;
            float halfCapacity = BuildCapacity / 2f;
            foreach (var planet2 in OwnerEmpire.GetPlanets())
                if (planet2.HasShipyard && planet2.ParentSystem.combatTimer > 0f)
                    def = true;
            capacity = BuildCapacity;
            if (def)
                while (capacity - halfCapacity > 0f
                       && numgoals < NumberOfShipGoals / 2
                       && (Empire.Universe.globalshipCount < shipCountLimit + Recyclepool
                           ||
                           OwnerEmpire.empireShipTotal < OwnerEmpire.EmpireShipCountReserve))
                {
                    var s = GetAShip(BuildCapacity);
                    if (s == null || !OwnerEmpire.ShipsWeCanBuild.Contains(s))
                    {
                        break;
                    }
                    if (Recyclepool > 0)
                    {
                        Recyclepool--;
                    }

                    var g = new Goal(s, "BuildDefensiveShips", OwnerEmpire)
                    {
                        type = GoalType.BuildShips
                    };
                    Goals.Add(g);
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        capacity = capacity - ResourceManager.ShipsDict[s].GetMaintCostRealism();
                    }
                    else
                    {
                        capacity = capacity - ResourceManager.ShipsDict[s].GetMaintCost(OwnerEmpire);
                    }
                    numgoals = numgoals + 1f;
                }
            while (capacity > 0 && numgoals < NumberOfShipGoals
                   && (Empire.Universe.globalshipCount < shipCountLimit + Recyclepool
                       || OwnerEmpire.empireShipTotal < OwnerEmpire.EmpireShipCountReserve))
            {
                var s = GetAShip(BuildCapacity);
                if (string.IsNullOrEmpty(s))
                {
                    break;
                }
                if (Recyclepool > 0)
                {
                    Recyclepool--;
                }

                var g = new Goal(s, "BuildOffensiveShips", OwnerEmpire)
                {
                    type = GoalType.BuildShips
                };
                Goals.Add(g);
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                {
                    capacity = capacity - ResourceManager.ShipsDict[s].GetMaintCostRealism();
                }
                else
                {
                    capacity = capacity - ResourceManager.ShipsDict[s].GetMaintCost(OwnerEmpire);
                }
                numgoals = numgoals + 1f;
            }

            foreach (var g in Goals)
            {
                if (g.type != GoalType.Colonize || g.Held)
                {
                    if (g.type != GoalType.Colonize || !g.Held || g.GetMarkedPlanet().Owner == null)
                    {
                        continue;
                    }
                    foreach (var relationship in OwnerEmpire
                        .AllRelations)
                    {
                        OwnerEmpire.GetGSAI().CheckClaim(relationship, g.GetMarkedPlanet());
                    }
                    Goals.QueuePendingRemoval(g);

                    using (TaskList.AcquireReadLock())
                    {
                        foreach (var task in TaskList)
                        {
                            foreach (var held in task.HeldGoals)
                            {
                                if (held != g.guid)
                                {
                                    continue;
                                }
                                TaskList.QueuePendingRemoval(task);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (g.GetMarkedPlanet() != null)
                    {
                        foreach (var pin in ThreatMatrix.Pins
                            .Where(pin => !((Vector2.Distance(g.GetMarkedPlanet().Position, pin.Value.Position) >=
                                             75000f)
                                            || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == OwnerEmpire ||
                                            pin.Value.Strength <= 0f
                                            || !OwnerEmpire
                                                .GetRelations(EmpireManager.GetEmpireByName(pin.Value.EmpireName))
                                                .AtWar)))
                        {
                            if (Vector2.Distance(g.GetMarkedPlanet().Position, pin.Value.Position) >= 75000f
                                || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == OwnerEmpire ||
                                pin.Value.Strength <= 0f
                                || !OwnerEmpire.GetRelations(EmpireManager.GetEmpireByName(pin.Value.EmpireName)).AtWar
                                && !EmpireManager.GetEmpireByName(pin.Value.EmpireName).isFaction)
                            {
                                continue;
                            }
                            var tohold = new Array<Goal>
                            {
                                g
                            };
                            var task =
                                new MilitaryTask(g.GetMarkedPlanet().Position, 125000f, tohold, OwnerEmpire);
                            //lock (GlobalStats.TaskLocker)
                            {
                                TaskList.Add(task);
                                break;
                            }
                        }
                    }
                }
            }
            if (OwnerEmpire.data.DiplomaticPersonality.Territorialism < 50 &&
                OwnerEmpire.data.DiplomaticPersonality.Trustworthiness < 50
            ) //    Name == "Aggressive" || empire.data.DiplomaticPersonality.Name == "Ruthless" || empire.data.EconomicPersonality.Name == "Expansionist")
            {
                foreach (var g in Goals)
                {
                    if (g.type != GoalType.Colonize || g.Held)
                    {
                        continue;
                    }
                    var ok = true;

                    using (TaskList.AcquireReadLock())
                    {
                        foreach (var mt in TaskList)
                            //Parallel.ForEach(TaskList, (mt,state) =>
                        {
                            if ((mt.type != MilitaryTask.TaskType.DefendClaim
                                 && mt.type != MilitaryTask.TaskType.ClearAreaOfEnemies)
                                || g.GetMarkedPlanet() != null
                                && !(mt.TargetPlanetGuid == g.GetMarkedPlanet().guid))
                            {
                                continue;
                            }
                            ok = false;
                            break;
                        }
                    }
                    if (!ok)
                    {
                        continue;
                    }
                    if (g.GetMarkedPlanet() == null)
                        continue;
                    var task = new MilitaryTask
                    {
                        AO = g.GetMarkedPlanet().Position
                    };
                    task.SetEmpire(OwnerEmpire);
                    task.AORadius = 75000f;
                    task.SetTargetPlanet(g.GetMarkedPlanet());
                    task.TargetPlanetGuid = g.GetMarkedPlanet().guid;
                    task.type = MilitaryTask.TaskType.DefendClaim;
                    //lock (GlobalStats.TaskLocker)
                    {
                        TaskList.Add(task);
                    }
                }
            }
            Goals.ApplyPendingRemovals();

            #endregion

            //this where the global AI attack stuff happenes.
            using (TaskList.AcquireReadLock())
            {
                var toughNuts = new Array<MilitaryTask>();
                var inOurSystems = new Array<MilitaryTask>();
                var inOurAOs = new Array<MilitaryTask>();
                var remainder = new Array<MilitaryTask>();

                foreach (var task in this
                    .TaskList.OrderByDescending(task =>
                    {
                        if (task.type != MilitaryTask.TaskType.AssaultPlanet)
                            return 0;
                        float weight = 0;
                        weight += (OwnerEmpire.currentMilitaryStrength - task.MinimumTaskForceStrength) /
                                  OwnerEmpire.currentMilitaryStrength * 5;

                        if (task.GetTargetPlanet() == null)
                        {
                            return weight * 2;
                        }
                        var emp = task.GetTargetPlanet().Owner;
                        if (emp == null)
                            return 0;
                        if (emp.isFaction)
                            return 0;

                        Relationship test = null;
                        if (OwnerEmpire.TryGetRelations(emp, out test) && test != null)
                        {
                            if (test.Treaty_NAPact || test.Treaty_Alliance || test.Posture != Posture.Hostile)
                                return 0;
                            weight += ((test.TotalAnger * .25f) - (100 - test.Threat)) / (test.TotalAnger * .25f) * 5f;
                            if (test.AtWar)
                                weight += 5;
                        }
                        var target = task.GetTargetPlanet();
                        if (target != null)
                        {
                            SystemCommander scom;
                            target.Owner.GetGSAI()
                                .DefensiveCoordinator.DefenseDict.TryGetValue(target.system, out scom);
                            if (scom != null)
                                weight += 11 - scom.RankImportance;
                        }

                        if (emp.isPlayer)
                            weight *= ((int) Empire.Universe.GameDifficulty > 0
                                ? (int) Empire.Universe.GameDifficulty
                                : 1);
                        return weight;
                    })
                )
                {
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet)
                    {
                        continue;
                    }
                    if (task.IsToughNut)
                    {
                        toughNuts.Add(task);
                    }
                    else if (!OwnerEmpire.GetOwnedSystems().Contains(task.GetTargetPlanet().system))
                    {
                        var dobreak = false;
                        foreach (var entry in Empire.Universe.PlanetsDict)
                        {
                            if (task.GetTargetPlanet() == entry.Value)
                            {
                                foreach (var area in AreasOfOperations)
                                {
                                    if (entry.Value.Position.OutsideRadius(area.Position, area.Radius))
                                        continue;
                                    inOurAOs.Add(task);
                                    dobreak = true;
                                    break;
                                }
                            }
                            break;
                        }
                        if (dobreak)
                        {
                            continue;
                        }
                        remainder.Add(task);
                    }
                    else
                    {
                        inOurSystems.Add(task);
                    }
                }
                //TaskList.thisLock.ExitReadLock();
                var tnInOurSystems = new Array<MilitaryTask>();
                var tnInOurAOs = new Array<MilitaryTask>();
                var tnRemainder = new Array<MilitaryTask>();
                Toughnuts = toughNuts.Count;
                foreach (var task in toughNuts)
                {
                    if (!OwnerEmpire.GetOwnedSystems().Contains(task.GetTargetPlanet().system))
                    {
                        var dobreak = false;
                        foreach (var entry in Empire.Universe.PlanetsDict)
                        {
                            if (task.GetTargetPlanet() != entry.Value)
                            {
                                continue;
                            }
                            foreach (var area in AreasOfOperations)
                            {
                                if (entry.Value.Position.OutsideRadius(area.Position, area.Radius))
                                    continue;
                                tnInOurAOs.Add(task);
                                dobreak = true;
                                break;
                            }
                            break;
                        }
                        if (dobreak)
                        {
                            continue;
                        }
                        tnRemainder.Add(task);
                    }
                    else
                    {
                        tnInOurSystems.Add(task);
                    }
                }
                foreach (var task in tnInOurAOs)
                    //Parallel.ForEach(TNInOurAOs, task =>
                {
                    if (task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner == OwnerEmpire ||
                        OwnerEmpire.GetRelations(task.GetTargetPlanet().Owner).ActiveWar == null ||
                        OwnerEmpire.TotalScore <= task.GetTargetPlanet().Owner.TotalScore * 1.5f)
                    {
                        continue;
                        //return;
                    }
                    task.Evaluate(OwnerEmpire);
                } //);
                foreach (var task in tnInOurSystems)
                    //Parallel.ForEach(TNInOurSystems, task =>
                {
                    task.Evaluate(OwnerEmpire);
                } //);
                foreach (var task in tnRemainder)
                {
                    if (task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner == OwnerEmpire ||
                        OwnerEmpire.GetRelations(task.GetTargetPlanet().Owner).ActiveWar == null ||
                        OwnerEmpire.TotalScore <= task.GetTargetPlanet().Owner.TotalScore * 1.5f)
                    {
                        continue;
                    }
                    task.Evaluate(OwnerEmpire);
                }
                foreach (var task in inOurAOs)
                {
                    task.Evaluate(OwnerEmpire);
                }
                foreach (var task in inOurSystems)
                {
                    task.Evaluate(OwnerEmpire);
                }
                foreach (var task in remainder)
                {
                    task.Evaluate(OwnerEmpire);
                }
                foreach (var task in TaskList)
                {
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet)
                    {
                        task.Evaluate(OwnerEmpire);
                    }
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet &&
                        task.type != MilitaryTask.TaskType.GlassPlanet || task.GetTargetPlanet().Owner != null &&
                        task.GetTargetPlanet().Owner != OwnerEmpire)
                    {
                        continue;
                    }
                    task.EndTask();
                }
            }
            TaskList.AddRange(TasksToAdd);
            TasksToAdd.Clear();
            TaskList.ApplyPendingRemovals();
        }


        //fbedard: Build a ship with a random role
        bool Nobuild;
        private string GetAShip(float capacity)
        {
            if (Nobuild)
                return null;
            var ratioFighters = 1f;
            var ratioCorvettes = 0f;
            var ratioFrigates = 0f;
            var ratioCruisers = 0f;
            var ratioCapitals = 0f;
            var ratioBombers = 0f;
            var ratioCarriers = 0f;
            var ratioSupport = 0f;
            var capBombers = 0f;
            var capCarriers = 0f;
            var capSupport = 0f;
            var capTroops = 0f;

            float numFighters = 0;
            float numCorvettes = 0;
            float numFrigates = 0;
            float numCruisers = 0;
            var numCarriers = 0f;
            var numBombers = 0f;
            var numCapitals = 0f;
            var numTroops = 0f;
            var numSupport = 0f;
            float capScrapping = 0;
            float totalUpkeep = 0;
            var totalMilShipCount = 0f;

            //Count the active ships
            for (var i = 0; i < OwnerEmpire.GetShips().Count(); i++)
            {
                var item = OwnerEmpire.GetShips()[i];
                if (item != null && item.Active && item.Mothership == null && item.AI.State != AIState.Scrap)
                {
                    var str = item.shipData.HullRole;
                    var upkeep = 0f;
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                        upkeep = item.GetMaintCostRealism();
                    else
                        upkeep = item.GetMaintCost();

                    if (item.AI.State == AIState.Scrap)
                    {
                        capScrapping += upkeep;
                        continue;
                    }

                    //carrier
                    if (item.GetHangars().Sum(fighters => fighters.MaximumHangarShipSize > 0 ? fighters.XSIZE * fighters.YSIZE : 0) > item.Size * .20f && str >= ShipData.RoleName.freighter)
                    {
                        numCarriers += upkeep;
                        totalMilShipCount++;
                        capCarriers += upkeep;
                        totalUpkeep += upkeep;
                    }
                    //troops ship
                    else if ((item.HasTroopBay || item.hasTransporter || item.hasAssaultTransporter) && str >= ShipData.RoleName.freighter
                             && item.GetHangars().Where(troopbay => troopbay.IsTroopBay).Sum(size => size.XSIZE * size.YSIZE)
                             + item.Transporters.Sum(troopbay => (troopbay.TransporterTroopAssault > 0 ? troopbay.YSIZE * troopbay.XSIZE : 0)) > item.Size * .10f
                    )
                    {
                        numTroops += upkeep;
                        totalMilShipCount++;
                        totalUpkeep += upkeep;
                        capTroops = +upkeep;

                    }

                    else if (item.hasOrdnanceTransporter || item.hasRepairBeam || item.HasSupplyBays || item.hasOrdnanceTransporter || item.InhibitionRadius > 0
                    )
                    {
                        numSupport++;
                        totalUpkeep += upkeep;
                        totalMilShipCount++;
                        capSupport += upkeep;
                    }
                    else if (item.BombBays.Count * 4 > item.Size * .20f && str >= ShipData.RoleName.freighter
                    )
                    {
                        numBombers += upkeep;
                        totalMilShipCount++;
                        totalUpkeep += upkeep;
                        capBombers += upkeep;
                        //capBombers += upkeep;
                    }

                    //capital and carrier without hangars
                    else if (str == ShipData.RoleName.capital || str == ShipData.RoleName.carrier)
                    {
                        numCapitals++;
                        totalMilShipCount++;
                        totalUpkeep += upkeep;
                    }
                    //bomber

                    else if (str == ShipData.RoleName.fighter || str == ShipData.RoleName.scout)
                    {
                        numFighters++;
                        totalMilShipCount++;
                        totalUpkeep += upkeep;
                        // capFighters += upkeep;
                    }
                    else if (str == ShipData.RoleName.corvette || str == ShipData.RoleName.gunboat)
                    {
                        numCorvettes++;
                        totalMilShipCount++;
                        totalUpkeep += upkeep;
                    }
                    else if (str == ShipData.RoleName.frigate || str == ShipData.RoleName.destroyer)
                    {
                        numFrigates++;
                        totalMilShipCount++;
                        totalUpkeep += upkeep;
                    }
                    else if (str == ShipData.RoleName.cruiser)
                    {
                        numCruisers++;
                        totalMilShipCount++;
                        totalUpkeep += upkeep;
                    }


                    /*
                    else if (str == ShipData.RoleName.freighter)
                    {
                        capFreighters += upkeep;
                        nonMilitaryCap += upkeep;
                    }
                    else if (str == ShipData.RoleName.platform)
                    {
                        capPlatforms += upkeep;
                        nonMilitaryCap += upkeep;
                    }
                    else if (str == ShipData.RoleName.station)
                    {
                        capStations += upkeep;
                        nonMilitaryCap += upkeep;
                    }
                    */
                }
            }

            /*
            FreighterUpkeep = capFreighters - (nonMilitaryCap * .25f);
            PlatformUpkeep = capPlatforms - (nonMilitaryCap * .25f);
            StationUpkeep = capStations - (nonMilitaryCap * .5f);
            */
            //if (!empire.canBuildCapitals && Ship_Game.ResourceManager.TechTree.ContainsKey("Battleships"))
            //    empire.canBuildCapitals = empire.GetTDict()["Battleships"].Unlocked;
            //if (!empire.canBuildCruisers && Ship_Game.ResourceManager.TechTree.ContainsKey("Cruisers"))
            //    empire.canBuildCruisers = empire.GetTDict()["Cruisers"].Unlocked;
            //if (!empire.canBuildFrigates && Ship_Game.ResourceManager.TechTree.ContainsKey("FrigateConstruction"))
            //    empire.canBuildFrigates = empire.GetTDict()["FrigateConstruction"].Unlocked;
            //if (!empire.canBuildCorvettes && Ship_Game.ResourceManager.TechTree.ContainsKey("HeavyFighterHull"))
            //    empire.canBuildCorvettes = empire.GetTDict()["HeavyFighterHull"].Unlocked;

            //Set ratio by class
            numBombers = numBombers * capBombers;
            numCarriers = numCarriers * capCarriers;
            numSupport = numSupport * capSupport;
            numTroops = numTroops * capTroops;
            if (OwnerEmpire.canBuildCapitals) //&& TotalMilShipCount >10)
            {
                ratioFighters = 0f;
                ratioCorvettes = .0f;
                ratioFrigates = 10f;
                ratioCruisers = 5f;
                ratioCapitals = 1f;
                if (OwnerEmpire.canBuildBombers)
                {
                    ratioBombers = 5f;
                    //numBombers = numBombers  //(float)Math.Ceiling((double)(numBombers / ResourceManager.ShipRoles[ShipData.RoleName.cruiser].Upkeep));
                }

                if (OwnerEmpire.canBuildCarriers)
                {
                    ratioCarriers = 1f;
                    //numCarriers =(float)Math.Ceiling((double)(numCarriers / ResourceManager.ShipRoles[ShipData.RoleName.cruiser].Upkeep));
                }
                ratioSupport = 1f;
            }
            else if (OwnerEmpire.canBuildCruisers) // && TotalMilShipCount > 10)
            {
                ratioFighters = 10f;
                ratioCorvettes = 10f;
                ratioFrigates = 5f;
                ratioCruisers = 1f;
                ratioCapitals = 0f;
                if (OwnerEmpire.canBuildBombers)
                {
                    ratioBombers = 5f;
                    // numBombers = (float)Math.Ceiling((double)(numBombers / ResourceManager.ShipRoles[ShipData.RoleName.frigate].Upkeep));
                }

                if (OwnerEmpire.canBuildCarriers)
                {
                    ratioCarriers = 1f;
                    //numCarriers = (float)Math.Ceiling((double)(numCarriers / ResourceManager.ShipRoles[ShipData.RoleName.frigate].Upkeep));
                }
                ratioSupport = 1f;
            }
            else if (OwnerEmpire.canBuildFrigates)//&& TotalMilShipCount > 10)
            {
                ratioFighters = 10f;
                ratioCorvettes = 5f;
                ratioFrigates = 1f;
                ratioCruisers = 0f;
                ratioCapitals = 0f;
                if (OwnerEmpire.canBuildBombers)
                {
                    ratioBombers = 5f;
                    //numBombers = (float)Math.Ceiling((double)(numBombers / ResourceManager.ShipRoles[ShipData.RoleName.corvette].Upkeep));
                }

                if (OwnerEmpire.canBuildCarriers)
                {
                    ratioCarriers = 1f;
                    //numCarriers = (float)Math.Ceiling((double)(numCarriers / ResourceManager.ShipRoles[ShipData.RoleName.corvette].Upkeep));
                }
                ratioSupport = 1f;
            }
            else if (OwnerEmpire.canBuildCorvettes)//&& TotalMilShipCount > 10)
            {
                ratioFighters = 5f;
                ratioCorvettes = 1f;
                ratioFrigates = 0f;
                ratioCruisers = 0f;
                ratioCapitals = 0f;
                ratioCarriers = 0;
                if (OwnerEmpire.canBuildBombers)
                {
                    ratioBombers = 1f;
                    //numBombers = (float)Math.Ceiling((double)(numBombers / ResourceManager.ShipRoles[ShipData.RoleName.corvette].Upkeep));
                }
            }
            else
            {
                ratioBombers = 0f;
                ratioCarriers = 0;
            }
            var totalRatio = ratioFighters + ratioCorvettes + ratioFrigates + ratioCruisers + ratioCapitals + ratioBombers + ratioSupport + ratioCarriers;
            var atwar = (OwnerEmpire.AllRelations.Where(war => war.Value.AtWar).Count() > 0);

            if (totalMilShipCount <= 0)
                totalRatio = 1;
            if (totalUpkeep == 0)
                totalUpkeep = 1;
            ratioBombers += Toughnuts * .2f;
            var goal = capacity / totalUpkeep;// / TotalMilShipCount);
            var adjustedRatio = totalMilShipCount / totalRatio;
            if (adjustedRatio == 0)
                adjustedRatio = 10;
            var desiredFighters = (float)Math.Ceiling(adjustedRatio * ratioFighters * goal);
            var desiredCorvettes = (float)Math.Ceiling(adjustedRatio * ratioCorvettes * goal);
            var desiredFrigates = (float)Math.Ceiling(adjustedRatio * ratioFrigates * goal);
            var desiredCruisers = (float)Math.Ceiling(adjustedRatio * ratioCruisers * goal);
            var desiredCapitals = (float)Math.Ceiling(adjustedRatio * ratioCapitals * goal);
            var desiredCarriers = (float)Math.Ceiling(adjustedRatio * ratioCarriers * goal);  //empire.canBuildCarriers ? (DesiredCapitals + DesiredCruisers) / 4f : 0;
            var desiredBombers = (float)Math.Ceiling(adjustedRatio * ratioBombers * goal);
            var desiredSupport = (float)Math.Ceiling(adjustedRatio * ratioSupport * goal);
            float desiredTroops = 0;

            //if(empire.canBuildBombers)
            //{
            //    DesiredBombers = TotalMilShipCount / 15f;

            //}
            if (OwnerEmpire.canBuildTroopShips)
            {
                desiredTroops = (float)Math.Ceiling(atwar ? totalMilShipCount / 10f : totalMilShipCount / 30f);
            }
#if DEBUG
            //Log.Info("Build Ratios for: " + empire.data.PortraitName);
            //Log.Info("fighters: " + DesiredFighters + " / " + numFighters);
            //Log.Info("corvettes: " + DesiredCorvettes + " / " + numCorvettes);
            //Log.Info("Frigates: " + DesiredFrigates + " / " + numFrigates);
            //Log.Info("Cruisers: " + DesiredCruisers + " / " + numCruisers);
            //Log.Info("Capitals: " + DesiredCapitals + " / " + numCapitals);
            //Log.Info("Carriers: " + DesiredCarriers + " / " + numCarriers);
            //Log.Info("Bombers: " + DesiredBombers + " / " + numBombers);
            //Log.Info("TroopsHips: " + DesiredTroops + " / " + numTroops);
            //Log.Info("Capacity: " + Capacity);
            //Log.Info("ShipGoals: " + empire.GetGSAI().numberOfShipGoals);

#endif
            //Scrap ships when overspending by class
            if (BuildCapacity / (totalUpkeep * .90f + 1) < 1)  //capScrapping prevent from scrapping too much
                #region MyRegion
            {
                if (numFighters > desiredFighters ||
                    numCorvettes > desiredCorvettes ||
                    numFrigates > desiredFrigates ||
                    numCruisers > desiredCruisers ||
                    numCarriers > desiredCarriers ||
                    numBombers > desiredBombers ||
                    numCapitals > desiredCapitals ||
                    numTroops > desiredTroops)
                {
                    foreach (var ship in OwnerEmpire.GetShips()
                        .Where(ship => !ship.InCombat && ship.inborders && ship.fleet == null && ship.AI.State != AIState.Scrap && ship.Mothership == null && ship.Active && ship.shipData.HullRole >= ShipData.RoleName.fighter && ship.GetMaintCost(OwnerEmpire) > 0)
                        .OrderByDescending(defense => DefensiveCoordinator.DefensiveForcePool.Contains(defense))
                        .ThenBy(ship => ship.Level)
                        .ThenBy(ship => ship.BaseStrength)
                    )
                    {
                        if (numFighters > (desiredFighters) && (ship.shipData.HullRole == ShipData.RoleName.fighter || ship.shipData.HullRole == ShipData.RoleName.scout))
                        {
                            numFighters--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCarriers > (desiredCarriers) && ship.GetHangars().Where(fighters => fighters.MaximumHangarShipSize > 0).Count() > 0)
                        {
                            numCarriers--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numTroops > (desiredTroops) && (ship.HasTroopBay || ship.hasTransporter))
                        {
                            numTroops--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numBombers > (desiredBombers) && (ship.BombBays.Count > 0))
                        {
                            numBombers--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCorvettes > (desiredCorvettes) && (ship.shipData.HullRole == ShipData.RoleName.corvette || ship.shipData.HullRole == ShipData.RoleName.gunboat))
                        {
                            numCorvettes--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numFrigates > (desiredFrigates) && (ship.shipData.HullRole == ShipData.RoleName.frigate || ship.shipData.HullRole == ShipData.RoleName.destroyer))
                        {
                            numFrigates--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCruisers > (desiredCruisers) && ship.shipData.HullRole == ShipData.RoleName.cruiser)
                        {
                            numCruisers--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCapitals > (desiredCapitals) && (ship.shipData.HullRole == ShipData.RoleName.capital || ship.shipData.HullRole == ShipData.RoleName.carrier))
                        {
                            numCapitals--;
                            ship.AI.OrderScrapShip();
                        }

                        if (numFighters <= desiredFighters
                            && numCorvettes <= desiredCorvettes
                            && numFrigates <= desiredFrigates
                            && numCruisers <= desiredCruisers
                            && numCarriers <= desiredCarriers
                            && numBombers <= desiredBombers
                            && numCapitals <= desiredCapitals
                            && numTroops <= desiredTroops)
                        {
                            break;
                        }
                    }
                }
                if (capacity <= 0)
                    return null;  //no money to build !
            }
            #endregion

            //Find ship to build
            var ranA = false;

            if (RandomMath.RandomBetween(0f, 1f) < 0.5f)
                ranA = true;

            var potentialShips = new Array<Ship>();
            var pickRoles = new Map<ShipData.RoleName, float>();
            OwnerEmpire.UpdateShipsWeCanBuild();
            string buildThis;

            var destroyer = false;
            var gunboats = false;
            var carriers = false;
            foreach (var hull in OwnerEmpire.GetHDict())
            {
                if (!hull.Value)
                    continue;
                if (ResourceManager.HullsDict[hull.Key].Role == ShipData.RoleName.destroyer)
                    destroyer = true;
                if (ResourceManager.HullsDict[hull.Key].Role == ShipData.RoleName.gunboat)
                    gunboats = true;
                if (ResourceManager.HullsDict[hull.Key].Role == ShipData.RoleName.carrier)
                    carriers = true;
            }
            if (numTroops < desiredTroops)
                pickRoles.Add(ShipData.RoleName.troop, numTroops / desiredTroops);
            if (numFighters < desiredFighters)
            {
                pickRoles.Add(ShipData.RoleName.fighter, numFighters / (desiredFighters));
                //PickRoles.Add(ShipData.RoleName.scout, numFighters / (DesiredFighters + ranB)); //scouts are handeled somewhere else and generally are not good at anything
            }
            if (numCorvettes < desiredCorvettes)
            {
                if (gunboats)
                {
                    if (ranA)
                        pickRoles.Add(ShipData.RoleName.gunboat, numCorvettes / (desiredCorvettes));
                    else
                        pickRoles.Add(ShipData.RoleName.corvette, numCorvettes / (desiredCorvettes));
                }
                else
                    pickRoles.Add(ShipData.RoleName.corvette, numCorvettes / (desiredCorvettes));
            }
            if (numBombers < desiredBombers)
                pickRoles.Add(ShipData.RoleName.drone, numBombers / desiredBombers);
            if (numFrigates < desiredFrigates)
            {
                if (destroyer)
                {
                    if (ranA)
                        pickRoles.Add(ShipData.RoleName.frigate, numFrigates / (desiredFrigates));
                    else
                        pickRoles.Add(ShipData.RoleName.destroyer, numFrigates / (desiredFrigates));
                }
                else
                    pickRoles.Add(ShipData.RoleName.frigate, numFrigates / (desiredFrigates));
            }
            if (numCruisers < desiredCruisers)
                pickRoles.Add(ShipData.RoleName.cruiser, numCruisers / desiredCruisers);
            if (numCapitals < desiredCapitals)
            {
                if (carriers)
                {
                    if (ranA)
                        pickRoles.Add(ShipData.RoleName.carrier, numCapitals / (desiredCapitals));
                    else
                        pickRoles.Add(ShipData.RoleName.capital, numCapitals / (desiredCapitals));
                }
                else
                {
                    pickRoles.Add(ShipData.RoleName.capital, numCapitals / (desiredCapitals));
                }
            }
            if (numCarriers < desiredCarriers)
                pickRoles.Add(ShipData.RoleName.prototype, numCarriers / desiredCarriers);

            foreach (var pick in pickRoles.OrderBy(val => val.Value))
            {
                buildThis = PickFromCandidates(pick.Key, capacity, potentialShips);
                if (!string.IsNullOrEmpty(buildThis))
                {
                    //Log.Info("Chosen: " + buildThis);
                    //Log.Info("TroopsHips: " + DesiredTroops);
                    return buildThis;
                }
            }
            //if(Empire.Universe.viewState == UniverseScreen.UnivScreenState.GalaxyView )
            //    Log.Info("Chosen: Nothing");
            Nobuild = true;
            return null;  //Find nothing to build !
        }


        //fbedard: add TroopsShip(troop), Bomber(drone) and Carrier(prototype) roles
        public string PickFromCandidates(ShipData.RoleName role, float capacity, Array<Ship> potentialShips)
        {
            var name = "";
            Ship ship;
            var maxtech = 0;
            //float upkeep;          //Not referenced in code, removing to save memory
            foreach (var shipsWeCanBuild in OwnerEmpire.ShipsWeCanBuild)
            {
                if (!ResourceManager.ShipsDict.TryGetValue(shipsWeCanBuild, out ship))
                    continue;
                var bombs = false;
                var hangars = false;
                var troops = false;
                var bombcount = 0;
                var hangarcount = 0;

                foreach (var slot in ship.ModuleSlotList)
                {
                    if (slot.ModuleType == ShipModuleType.Bomb)
                    {
                        bombcount += slot.XSIZE * slot.YSIZE;
                        if (bombcount > ship.Size * .2)
                            bombs = true;
                    }
                    if (slot.MaximumHangarShipSize > 0)
                    {
                        hangarcount += slot.YSIZE * slot.XSIZE;
                        if (hangarcount > ship.Size * .2)
                            hangars = true;
                    }
                    if (slot.IsTroopBay || slot.TransporterRange > 0)
                        troops = true;

                }


                //    upkeep = ship.GetMaintCost(empire); //this automatically calls realistic maintenance cost if needed. 
                //Capacity < upkeep ||
                if (role == ShipData.RoleName.drone || role == ShipData.RoleName.troop)
                {
                    if (!NonCombatshipIsGoodForGoals(ship) || ship.shipData.HullRole < ShipData.RoleName.freighter)
                        continue;
                }
                else
                if (!ShipIsGoodForGoals(ship) || ship.shipData.HullRole < ShipData.RoleName.freighter)
                    continue;
                if (role == ShipData.RoleName.troop && !troops)
                    continue;
                if (role == ShipData.RoleName.drone && !bombs)
                    continue;
                if (role == ShipData.RoleName.prototype && !hangars)
                    continue;
                if (role != ship.shipData.HullRole && role == ShipData.RoleName.prototype && role != ShipData.RoleName.drone && role != ShipData.RoleName.troop)
                    continue;
                if (ship.shipData.techsNeeded.Count > maxtech)
                    maxtech = ship.shipData.techsNeeded.Count;
                potentialShips.Add(ship);
            }
            var nearmax = maxtech * .5f;
            //Log.Info("number of candidates : " + PotentialShips.Count + " _ trying for : " + role);
            if (potentialShips.Count > 0)
            {
                var sortedList =
                    from ship3 in potentialShips
                    orderby ship3.shipData.techsNeeded.Count >= nearmax descending, ship3.BaseStrength descending
                    select ship3;
                maxtech++;
                var ran = (int)(sortedList.Count() * .5f);
                ran = RandomMath.InRange(ran);
                if (ran > sortedList.Count())
                    ran = sortedList.Count();
                ship = sortedList.Skip(ran).First();
                name = ship.Name;
                if (Empire.Universe.showdebugwindow)
                    Log.Info("Chosen Role: {0}  Chosen Hull: {1}  Strength: {2}",
                        ship.GetShipData().Role, ship.GetShipData().Hull, ship.BaseStrength);
            }
            else
            {
#if false
                string ships = "Ships empire has: ";
                foreach (string known in empire.ShipsWeCanBuild)
                {
                    ships += known + " : ";
                }
                Log.Info(ships);
#endif
            }
            potentialShips.Clear();
            return name;
        }

        public bool ShipIsGoodForGoals(Ship ship)
        {
            if (ship.BaseStrength > 0f && ship.shipData.ShipStyle != "Platforms" && !ship.shipData.CarrierShip && ship.BaseCanWarp && ship.ModulePowerDraw * OwnerEmpire.data.FTLPowerDrainModifier <= ship.PowerFlowMax
                || (ship.ModulePowerDraw * OwnerEmpire.data.FTLPowerDrainModifier > ship.PowerFlowMax
                    && ship.PowerStoreMax / (ship.ModulePowerDraw * OwnerEmpire.data.FTLPowerDrainModifier - ship.PowerFlowMax) * ship.velocityMaximum > MinimumWarpRange))
                return true;
            return false;
        }
        public bool NonCombatshipIsGoodForGoals(Ship ship)
        {
            if (ship.shipData.ShipStyle != "Platforms" && !ship.shipData.CarrierShip && ship.BaseCanWarp && ship.ModulePowerDraw * OwnerEmpire.data.FTLPowerDrainModifier <= ship.PowerFlowMax
                || (ship.ModulePowerDraw * OwnerEmpire.data.FTLPowerDrainModifier > ship.PowerFlowMax
                    && ship.PowerStoreMax / (ship.ModulePowerDraw * OwnerEmpire.data.FTLPowerDrainModifier - ship.PowerFlowMax) * ship.velocityMaximum > MinimumWarpRange))
                return true;
            return false;
        }

    }
}