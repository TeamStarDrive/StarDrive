using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.Gameplay;

// ReSharper disable once CheckNamespace
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
            foreach (Planet p in OwnerEmpire.GetPlanets())
            {
                if(p.WorkerPercentage > .75 || p.GetMaxProductionPotential() < 2f)                
                    continue;
                NumberOfShipGoals++;
            }

            float numgoals = 0f;
            float underConstruction = 0f;
            float troopStrengthUnderConstruction = 0f;
            foreach (Goal g in Goals)
            {
                if (g.GoalName == "BuildOffensiveShips" || g.GoalName == "BuildDefensiveShips")
                {
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        underConstruction = underConstruction +
                                            ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCostRealism();
                    }                    
                    else                    
                        underConstruction = underConstruction +
                                            ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(OwnerEmpire);
                    
                    foreach (var t in ResourceManager.ShipsDict[g.ToBuildUID].TroopList)                    
                        troopStrengthUnderConstruction = troopStrengthUnderConstruction + t.Strength;
                    
                    numgoals = numgoals + 1f;
                }
                if (g.GoalName != "BuildConstructionShip")                
                    continue;
                
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                {
                    underConstruction = underConstruction +
                                        ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCostRealism();
                }
                else                
                    underConstruction = underConstruction +
                                        ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(OwnerEmpire);
                
            }
            float anger = 0;
            foreach (var rel in OwnerEmpire.AllRelations)
            {
                anger += rel.Value.TotalAnger;
            }
            anger *= .01f;
            float offenseNeeded = 0;
            offenseNeeded += ThreatMatrix.StrengthOfAllThreats(OwnerEmpire);
            offenseNeeded /= OwnerEmpire.currentMilitaryStrength;
            offenseNeeded = anger;
            if (offenseNeeded <= 0)            
                offenseNeeded = 0;
            
            if (offenseNeeded > 20)
                offenseNeeded = 20;
            NumberOfShipGoals += (int) offenseNeeded;
            var income = OwnerEmpire.Grossincome();
            float atWarBonus = 0f;
            if (offenseNeeded > 2)
                atWarBonus        += (offenseNeeded * (.05f + OwnerEmpire.getResStrat().MilitaryPriority * .03f));
            float capacity         = income * (.05f + atWarBonus) - underConstruction;
            float allowableDeficit = OwnerEmpire.Money * -(1f - OwnerEmpire.data.TaxRate);// - (OwnerEmpire.Money * .05f) * atWarBonus;

            if (capacity > BuildCapacity)
                BuildCapacity = capacity;
            var maintenance = OwnerEmpire.GetTotalShipMaintenance();
            capacity -= maintenance;
            OwnerEmpire.data.ShipBudget = BuildCapacity;
            
            if (capacity - maintenance - allowableDeficit <= 0f)
            {
                //capacity -= maintenance - allowableDeficit;
                float howMuchWeAreScrapping = 0f;

                foreach (Ship ship1 in OwnerEmpire.GetShips())
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
                    foreach (Goal g in Goals
                        .Where(goal => goal.GoalName == "BuildOffensiveShips" ||
                                       goal.GoalName == "BuildDefensiveShips")
                        .OrderByDescending(goal => ResourceManager.ShipsDict[goal.ToBuildUID]
                            .GetMaintCost(OwnerEmpire)))
                    {
                        bool flag = false;
                        if (g.GetPlanetWhereBuilding() == null)
                            continue;
                        foreach (QueueItem shipToRemove in g.GetPlanetWhereBuilding().ConstructionQueue)
                        {
                            if (shipToRemove.Goal != g || shipToRemove.productionTowards > 0f)
                                continue;

                            g.GetPlanetWhereBuilding().ConstructionQueue.QueuePendingRemoval(shipToRemove);
                            Goals.QueuePendingRemoval(g);                            
                             added += g.beingBuilt?.GetMaintCost(OwnerEmpire) 
                                ??  ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(OwnerEmpire);
                            flag = true;
                            break;
                        }
                        if (flag)
                            g.GetPlanetWhereBuilding().ConstructionQueue.ApplyPendingRemovals();
                        if (howMuchWeAreScrapping + added >= Math.Abs(capacity))
                            break;
                    }

                    Goals.ApplyPendingRemovals();
                    capacity = capacity + howMuchWeAreScrapping + added;
                }
                BuildCapacity = capacity;
            }

            if (BuildCapacity < 0)
                GetAShip(BuildCapacity);

            //bool def = false;
            //float halfCapacity = BuildCapacity / 2f;
            //foreach (var planet2 in OwnerEmpire.GetPlanets())
            //    if (planet2.HasShipyard && planet2.ParentSystem.combatTimer > 0f)
            //        def = true;
            //capacity = BuildCapacity;
            //if (def)
            //    while (capacity - halfCapacity > 0f
            //           && numgoals < NumberOfShipGoals / 2f
            //           && (Empire.Universe.globalshipCount < shipCountLimit + Recyclepool
            //               ||
            //               OwnerEmpire.empireShipTotal < OwnerEmpire.EmpireShipCountReserve))
            //    {
            //        string s = GetAShip(BuildCapacity);
            //        if (s == null || !OwnerEmpire.ShipsWeCanBuild.Contains(s))                    
            //            break;
                    
            //        if (Recyclepool > 0)                    
            //            Recyclepool--;
                    
            //        var g = new Goal(s, "BuildDefensiveShips", OwnerEmpire)
            //        {
            //            type = GoalType.BuildShips
            //        };
            //        Goals.Add(g);
            //        if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
            //        {
            //            capacity = capacity - ResourceManager.ShipsDict[s].GetMaintCostRealism();
            //        }
            //        else
            //        {
            //            capacity = capacity - ResourceManager.ShipsDict[s].GetMaintCost(OwnerEmpire);
            //        }
            //        numgoals = numgoals + 1f;
            //    }
            while (capacity > 0 && numgoals < NumberOfShipGoals
                   && (Empire.Universe.globalshipCount < shipCountLimit + Recyclepool
                       || OwnerEmpire.empireShipTotal < OwnerEmpire.EmpireShipCountReserve))
            {
                string s = GetAShip(BuildCapacity);
                if (string.IsNullOrEmpty(s))                
                    break;
                
                if (Recyclepool > 0)                
                    Recyclepool--;
                

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

            foreach (Goal g in Goals)
            {
                if (g.type != GoalType.Colonize || g.Held)
                {
                    if (g.type != GoalType.Colonize || !g.Held || g.GetMarkedPlanet().Owner == null)                    
                        continue;
                    
                    foreach (var relationship in OwnerEmpire.AllRelations)                    
                        OwnerEmpire.GetGSAI().CheckClaim(relationship, g.GetMarkedPlanet());
                    
                    Goals.QueuePendingRemoval(g);

                    using (TaskList.AcquireReadLock())
                    {
                        foreach (Tasks.MilitaryTask task in TaskList)
                        {
                            foreach (Guid held in task.HeldGoals)
                            {
                                if (held != g.guid)                                
                                    continue;
                                
                                TaskList.QueuePendingRemoval(task);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (g.GetMarkedPlanet() == null) continue;
                    foreach (var kv in ThreatMatrix.Pins
                        .Where(pin => !(g.GetMarkedPlanet().Center.OutsideRadius(pin.Value.Position,75000f)
                                        || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == OwnerEmpire ||
                                        pin.Value.Strength <= 0f
                                        || !OwnerEmpire
                                            .GetRelations(EmpireManager.GetEmpireByName(pin.Value.EmpireName))
                                            .AtWar)))
                    {
                        if (g.GetMarkedPlanet().Center.OutsideRadius(kv.Value.Position, 75000f)
                            || EmpireManager.GetEmpireByName(kv.Value.EmpireName) == OwnerEmpire ||
                            kv.Value.Strength <= 0f
                            || !OwnerEmpire.GetRelations(EmpireManager.GetEmpireByName(kv.Value.EmpireName)).AtWar
                            && !EmpireManager.GetEmpireByName(kv.Value.EmpireName).isFaction)
                        {
                            continue;
                        }
                        var tohold = new Array<Goal>
                        {
                            g
                        };
                        var task =
                            new Tasks.MilitaryTask(g.GetMarkedPlanet().Center, 125000f, tohold, OwnerEmpire);
                        {
                            TaskList.Add(task);
                            break;
                        }
                    }
                }
            }
            if (OwnerEmpire.data.DiplomaticPersonality.Territorialism < 50 &&
                OwnerEmpire.data.DiplomaticPersonality.Trustworthiness < 50
            )
            {
                foreach (Goal g in Goals)
                {
                    if (g.type != GoalType.Colonize || g.Held)                    
                        continue;
                    
                    bool ok = true;

                    using (TaskList.AcquireReadLock())
                    {
                        foreach (Tasks.MilitaryTask mt in TaskList)
                        {
                            if ((mt.type != Tasks.MilitaryTask.TaskType.DefendClaim
                                 && mt.type != Tasks.MilitaryTask.TaskType.ClearAreaOfEnemies)
                                || g.GetMarkedPlanet() != null
                                && !(mt.TargetPlanetGuid == g.GetMarkedPlanet().guid))                            
                                continue;
                            
                            ok = false;
                            break;
                        }
                    }
                    if (!ok)                    
                        continue;
                    
                    if (g.GetMarkedPlanet() == null)
                        continue;
                    var task = new Tasks.MilitaryTask
                    {
                        AO = g.GetMarkedPlanet().Center
                    };
                    task.SetEmpire(OwnerEmpire);
                    task.AORadius = 75000f;
                    task.SetTargetPlanet(g.GetMarkedPlanet());
                    task.TargetPlanetGuid = g.GetMarkedPlanet().guid;
                    task.type = Tasks.MilitaryTask.TaskType.DefendClaim;
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
                var toughNuts = new Array<Tasks.MilitaryTask>();
                var inOurSystems = new Array<Tasks.MilitaryTask>();
                var inOurAOs = new Array<Tasks.MilitaryTask>();
                var remainder = new Array<Tasks.MilitaryTask>();

                foreach (var task in this
                    .TaskList.OrderByDescending((Func<Tasks.MilitaryTask, float>)(task =>
                    {
                        if (task.type != Tasks.MilitaryTask.TaskType.AssaultPlanet)
                            return 0;
                        float weight = 0;
                        weight += (OwnerEmpire.currentMilitaryStrength - task.MinimumTaskForceStrength) /
                                  OwnerEmpire.currentMilitaryStrength * 5;

                        if (task.GetTargetPlanet() == null)                        
                            return weight * 2;

                        Empire emp = task.GetTargetPlanet().Owner;
                        if (emp == null)
                            return 0;
                        if (emp.isFaction)
                            return 0;

                        Relationship test;
                        if (OwnerEmpire.TryGetRelations(emp, out test) && test != null)
                        {
                            if (test.Treaty_NAPact || test.Treaty_Alliance || test.Posture != Posture.Hostile)
                                return 0;
                            weight += ((test.TotalAnger * .25f) - (100 - test.Threat)) / (test.TotalAnger * .25f) * 5f;
                            if (test.AtWar)
                                weight += 5;
                        }
                        Planet target = task.GetTargetPlanet();
                        if (target != null)
                        {
                            SystemCommander scom;
                            target.Owner.GetGSAI()
                                .DefensiveCoordinator.DefenseDict.TryGetValue((SolarSystem)target.ParentSystem, out scom);
                            if (scom != null)
                                weight += 11 - scom.RankImportance;
                        }

                        if (emp.isPlayer)
                            weight *= ((int)Empire.Universe.GameDifficulty > 0
                                ? (int)Empire.Universe.GameDifficulty: 1);
                        return weight;
                    }))
                )
                {
                    if (task.type != Tasks.MilitaryTask.TaskType.AssaultPlanet)                    
                        continue;
                    
                    if (task.IsToughNut)                    
                        toughNuts.Add(task);
                    
                    else if (!OwnerEmpire.GetOwnedSystems().Contains((SolarSystem)task.GetTargetPlanet().ParentSystem))
                    {
                        bool dobreak = false;
                        foreach (KeyValuePair<Guid, Planet> entry in Empire.Universe.PlanetsDict)
                        {
                            if (task.GetTargetPlanet() == entry.Value)
                            {
                                foreach (AO area in AreasOfOperations)
                                {
                                    if (entry.Value.Center.OutsideRadius(area.Center, area.Radius))
                                        continue;
                                    inOurAOs.Add(task);
                                    dobreak = true;
                                    break;
                                }
                            }
                            break;
                        }
                        if (dobreak)                        
                            continue;
                        
                        remainder.Add(task);
                    }
                    else                    
                        inOurSystems.Add(task);
                    
                }
                var tnInOurSystems = new Array<Tasks.MilitaryTask>();
                var tnInOurAOs     = new Array<Tasks.MilitaryTask>();
                var tnRemainder    = new Array<Tasks.MilitaryTask>();
                Toughnuts          = toughNuts.Count + remainder.Count + inOurSystems.Count + inOurAOs.Count;
                foreach (Tasks.MilitaryTask task in toughNuts)
                {
                    if (!OwnerEmpire.GetOwnedSystems().Contains(task.GetTargetPlanet().ParentSystem))
                    {
                        bool dobreak = false;
                        foreach (KeyValuePair<Guid, Planet> entry in Empire.Universe.PlanetsDict)
                        {
                            if (task.GetTargetPlanet() != entry.Value)                            
                                continue;
                            
                            foreach (AO area in AreasOfOperations)
                            {
                                if (entry.Value.Center.OutsideRadius(area.Center, area.Radius))
                                    continue;
                                tnInOurAOs.Add(task);
                                dobreak = true;
                                break;
                            }
                            break;
                        }
                        if (dobreak)                        
                            continue;
                        
                        tnRemainder.Add(task);
                    }
                    else                    
                        tnInOurSystems.Add(task);
                    
                }
                foreach (Tasks.MilitaryTask task in tnInOurAOs)
                {
                    if (task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner == OwnerEmpire ||
                        OwnerEmpire.GetRelations(task.GetTargetPlanet().Owner).ActiveWar == null ||
                        OwnerEmpire.TotalScore <= task.GetTargetPlanet().Owner.TotalScore * 1.5f)                    
                        continue;
                    
                    task.Evaluate(OwnerEmpire);
                }
                foreach (Tasks.MilitaryTask task in tnInOurSystems)                
                    task.Evaluate(OwnerEmpire);
                
                foreach (Tasks.MilitaryTask task in tnRemainder)
                {
                    if (task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner == OwnerEmpire ||
                        OwnerEmpire.GetRelations(task.GetTargetPlanet().Owner).ActiveWar == null ||
                        OwnerEmpire.TotalScore <= task.GetTargetPlanet().Owner.TotalScore * 1.5f)                    
                        continue;
                    
                    task.Evaluate(OwnerEmpire);
                }
                foreach (Tasks.MilitaryTask task in inOurAOs)                
                    task.Evaluate(OwnerEmpire);
                
                foreach (Tasks.MilitaryTask task in inOurSystems)                
                    task.Evaluate(OwnerEmpire);
                
                foreach (Tasks.MilitaryTask task in remainder)                
                    task.Evaluate(OwnerEmpire);
                
                foreach (Tasks.MilitaryTask task in TaskList)
                {
                    if (task.type != Tasks.MilitaryTask.TaskType.AssaultPlanet)                    
                        task.Evaluate(OwnerEmpire);
                    
                    if (task.type != Tasks.MilitaryTask.TaskType.AssaultPlanet &&
                        task.type != Tasks.MilitaryTask.TaskType.GlassPlanet || task.GetTargetPlanet().Owner != null &&
                        task.GetTargetPlanet().Owner != OwnerEmpire)                    
                        continue;
                    
                    task.EndTask();
                }
            }
            TaskList.AddRange(TasksToAdd);
            TasksToAdd.Clear();
            TaskList.ApplyPendingRemovals();
        }


        //fbedard: Build a ship with a random role
        private bool Nobuild;
        private string GetAShip(float capacity)
        {
            if (Nobuild)
                return null;
            float ratioFighters     = .5f;
            float ratioCorvettes    = 0f;
            float ratioFrigates     = 0f;
            float ratioCruisers     = 0f;
            float ratioCapitals     = 0f;
            float ratioBombers      = 0f;
            float ratioCarriers     = 0f;
            float ratioSupport      = 0f;
            float capBombers        = 0f;
            float capCarriers       = 0f;
            float capSupport        = 0f;
            float capTroops         = 0f;
            float numFighters       = 0;
            float numCorvettes      = 0;
            float numFrigates       = 0;
            float numCruisers       = 0;
            float numCarriers       = 0f;
            float numBombers        = 0f;
            float numCapitals       = 0f;
            float numTroops         = 0f;
            float numSupport        = 0f;
            float totalUpkeep       = 0;
            float totalMilShipCount = 0f;

            //Count the active ships
            for (int i = 0; i < OwnerEmpire.GetShips().Count; i++)
            {
                Ship item = OwnerEmpire.GetShips()[i];
                if (item == null || !item.Active || item.Mothership != null || item.AI.State == AIState.Scrap
                    || item.AI.State == AIState.Scrap) continue;

                ShipData.RoleName str = item.DesignRole;
                float upkeep;
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    upkeep = item.GetMaintCostRealism();
                else
                    upkeep = item.GetMaintCost();                

                //carrier
                if (str == ShipData.RoleName.carrier)
                {
                    numCarriers += upkeep;
                    totalMilShipCount++;
                    capCarriers += upkeep;
                    totalUpkeep += upkeep;
                }
                //troops ship
                else if (str == ShipData.RoleName.troopShip)                
                {
                    numTroops += upkeep;
                    totalMilShipCount++;
                    totalUpkeep += upkeep;
                    capTroops    = +upkeep;

                }

                else if (str == ShipData.RoleName.support)                
                {
                    numSupport++;
                    totalUpkeep += upkeep;
                    totalMilShipCount++;
                    capSupport += upkeep;
                }
                else if (str == ShipData.RoleName.bomber)
                {
                    numBombers += upkeep;
                    totalMilShipCount++;
                    totalUpkeep += upkeep;
                    capBombers += upkeep;
                }

                //capital and carrier without hangars
                else if(str == ShipData.RoleName.capital || str == ShipData.RoleName.carrier)
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
            }

            //Set ratio by class
            numBombers  = numBombers * capBombers;
            numCarriers = numCarriers * capCarriers;
            numSupport  = numSupport * capSupport;
            numTroops   = numTroops * capTroops;
            if (OwnerEmpire.canBuildCapitals)
            {
                ratioFighters  = 0f;
                ratioCorvettes = .0f;
                ratioFrigates  = 5f;
                ratioCruisers  = 5f;
                ratioCapitals  = 2f;
                if (OwnerEmpire.canBuildBombers)                
                    ratioBombers = 3f;
                
                if (OwnerEmpire.canBuildCarriers)                
                    ratioCarriers = 2f;
                
                ratioSupport = 1f;
            }
            else if (OwnerEmpire.canBuildCruisers)
            {
                ratioFighters  = 1f;
                ratioCorvettes = 2f;
                ratioFrigates  = 3f;
                ratioCruisers  = 4f;
                ratioCapitals  = 0f;
                if (OwnerEmpire.canBuildBombers)                
                    ratioBombers = 5f;
                

                if (OwnerEmpire.canBuildCarriers)                
                    ratioCarriers = 1f;
                
                ratioSupport = 1f;
            }
            else if (OwnerEmpire.canBuildFrigates)
            {
                ratioFighters  = 1f;
                ratioCorvettes = 2f;
                ratioFrigates  = 5f;
                ratioCruisers  = 0f;
                ratioCapitals  = 0f;
                if (OwnerEmpire.canBuildBombers)                
                    ratioBombers = 5f;
                
                if (OwnerEmpire.canBuildCarriers)                
                    ratioCarriers = 1f;
                
                ratioSupport = 1f;
            }
            else if (OwnerEmpire.canBuildCorvettes)
            {
                ratioFighters  = 1f;
                ratioCorvettes = 2f;
                ratioFrigates  = 0f;
                ratioCruisers  = 0f;
                ratioCapitals  = 0f;
                ratioCarriers  = 0;
                if (OwnerEmpire.canBuildBombers)                
                    ratioBombers = 1f;
                
            }
            else
            {
                ratioBombers = 0f;
                ratioCarriers = 0;
            }
            float totalRatio = ratioFighters + ratioCorvettes + ratioFrigates + ratioCruisers + ratioCapitals + ratioBombers + ratioSupport + ratioCarriers;
            bool atwar       = (OwnerEmpire.AllRelations.Any(war => war.Value.AtWar));

            if (totalMilShipCount <= 0)
                totalRatio = 1;
            if (totalUpkeep < 1)
                totalUpkeep = 1;
            ratioBombers += Toughnuts * .2f;
            float goal = capacity / totalUpkeep;
            float adjustedRatio = totalMilShipCount / totalRatio;
            if (adjustedRatio < .01)
                adjustedRatio      = 10;
            float desiredFighters  = (float)Math.Ceiling(adjustedRatio * ratioFighters * goal);
            float desiredCorvettes = (float)Math.Ceiling(adjustedRatio * ratioCorvettes * goal);
            float desiredFrigates  = (float)Math.Ceiling(adjustedRatio * ratioFrigates * goal);
            float desiredCruisers  = (float)Math.Ceiling(adjustedRatio * ratioCruisers * goal);
            float desiredCapitals  = (float)Math.Ceiling(adjustedRatio * ratioCapitals * goal);
            float desiredCarriers  = (float)Math.Ceiling(adjustedRatio * ratioCarriers * goal);
            float desiredBombers   = (float)Math.Ceiling(adjustedRatio * ratioBombers * goal);
            float desiredSupport   = (float)Math.Ceiling(adjustedRatio * ratioSupport * goal);
            float desiredTroops    = 0;

            if (OwnerEmpire.canBuildTroopShips)
            {
                desiredTroops = (float)Math.Ceiling(atwar ? totalMilShipCount / 10f : totalMilShipCount / 30f);
            }

            if (CullShipsForScrapping(capacity, totalUpkeep, desiredFighters, desiredCorvettes, 
                desiredFrigates, desiredCruisers, desiredCarriers, desiredBombers, desiredCapitals, 
                desiredTroops, desiredSupport, ref numFighters, ref numCorvettes, ref numFrigates, 
                ref numCruisers, ref numCarriers, ref numBombers, ref numCapitals, ref numTroops, ref numSupport)) return null;

          
            /* this looks bad. all bad.
             * the ideas are confused and interlocked. one the ship roles need to be set on the shipdata to work. need to check that. 
             * then it needs to use that as if it was there and not try and figure it out over and over what role a ship is. 
             * its actually a good idea to get these roles working right. So that a role is defined correctly and usefully.
             * but this uses both the idea that they are not and that they are. 
             * This may be a fundemtal flaw in the hull data. 
             * the hull data should set the size class of the ship and the role should set the type.
             * needs a rethink and remake. 
            bleh. 
            */
            //Find ship to build
            bool ranA = RandomMath.RandomBetween(0f, 1f) < 0.5f;
           
            var pickRoles = new Map<ShipData.RoleName, float>();
           // OwnerEmpire.UpdateShipsWeCanBuild();

            if (numTroops < desiredTroops)
                pickRoles.Add(ShipData.RoleName.troop, numTroops / desiredTroops);
            if (numFighters < desiredFighters)            
                pickRoles.Add(ShipData.RoleName.fighter, numFighters / (desiredFighters));
            
            if(numCorvettes < desiredCorvettes)            
                    pickRoles.Add(ShipData.RoleName.corvette, numCorvettes / (desiredCorvettes));
            
            if(numBombers < desiredBombers)
                pickRoles.Add(ShipData.RoleName.bomber, numBombers / desiredBombers);
            if(numFrigates < desiredFrigates)            
                    pickRoles.Add(ShipData.RoleName.frigate, numFrigates / (desiredFrigates));
            
            if (numCruisers < desiredCruisers)
                pickRoles.Add(ShipData.RoleName.cruiser, numCruisers / desiredCruisers);
            if (numCapitals < desiredCapitals)                          
                    pickRoles.Add(ShipData.RoleName.capital, numCapitals / (desiredCapitals));
                            
            if (numCarriers < desiredCarriers)
                pickRoles.Add(ShipData.RoleName.carrier, numCarriers / desiredCarriers);

            // to fix this it is needed to fix roles or at least the way roles are being used here. 
            if (numSupport < desiredSupport)
                pickRoles.Add(ShipData.RoleName.support, numSupport / desiredSupport);

            

            foreach (var kv in pickRoles.OrderBy(val => val.Value))
            {
                string buildThis = PickFromCandidates(kv.Key);
                if (!string.IsNullOrEmpty(buildThis))                
                    return buildThis;                
            }
            Nobuild = true;
            return null;  //Find nothing to build !
        }

        private bool CullShipsForScrapping(float capacity, float totalUpkeep, float desiredFighters, float desiredCorvettes,
            float desiredFrigates, float desiredCruisers, float desiredCarriers, float desiredBombers, float desiredCapitals,
            float desiredTroops, float desiredSupport, ref float numFighters, ref float numCorvettes, ref float numFrigates,
            ref float numCruisers, ref float numCarriers, ref float numBombers, ref float numCapitals, ref float numTroops,
            ref float numSupport)
        {
            //Scrap ships when overspending by class
            if (BuildCapacity / (totalUpkeep * .90f + 1) < 1) //capScrapping prevent from scrapping too much
            {
                if (numFighters > desiredFighters ||
                    numCorvettes > desiredCorvettes ||
                    numFrigates > desiredFrigates ||
                    numCruisers > desiredCruisers ||
                    numCarriers > desiredCarriers ||
                    numBombers > desiredBombers ||
                    numCapitals > desiredCapitals ||
                    numTroops > desiredTroops ||
                    numSupport > desiredSupport)
                {
                    foreach (var ship in OwnerEmpire.GetShips()
                        .Where(ship => !ship.InCombat && ship.inborders && ship.fleet == null
                                       && ship.AI.State != AIState.Scrap && ship.Mothership == null && ship.Active
                                       && ship.shipData.HullRole >= ShipData.RoleName.fighter &&
                                       ship.GetMaintCost(OwnerEmpire) > 0)
                        .OrderByDescending(defense => DefensiveCoordinator.DefensiveForcePool.Contains(defense))
                        .ThenBy(ship => ship.Level)
                        .ThenBy(ship => ship.BaseStrength)
                    )
                    {
                        if (numFighters > (desiredFighters)
                            && (ship.shipData.HullRole == ShipData.RoleName.fighter
                                || ship.shipData.HullRole == ShipData.RoleName.scout))
                        {
                            numFighters--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCarriers > (desiredCarriers)
                                 && ship.GetHangars().Any(fighters => fighters.MaximumHangarShipSize > 0))
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
                        else if (numCorvettes > (desiredCorvettes)
                                 && (ship.shipData.HullRole == ShipData.RoleName.corvette
                                     || ship.shipData.HullRole == ShipData.RoleName.gunboat))
                        {
                            numCorvettes--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numFrigates > (desiredFrigates)
                                 && (ship.shipData.HullRole == ShipData.RoleName.frigate
                                     || ship.shipData.HullRole == ShipData.RoleName.destroyer))
                        {
                            numFrigates--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCruisers > (desiredCruisers)
                                 && ship.shipData.HullRole == ShipData.RoleName.cruiser)
                        {
                            numCruisers--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCapitals > (desiredCapitals)
                                 && (ship.shipData.HullRole == ShipData.RoleName.capital
                                     || ship.shipData.HullRole == ShipData.RoleName.carrier))
                        {
                            numCapitals--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCapitals > (desiredCapitals)
                                 && (ship.hasOrdnanceTransporter || ship.hasRepairBeam || ship.HasSupplyBays
                                     || ship.hasOrdnanceTransporter || ship.InhibitionRadius > 0))
                        {
                            numSupport--;
                            ship.AI.OrderScrapShip();
                        }


                        if (numFighters <= desiredFighters
                            && numCorvettes <= desiredCorvettes
                            && numFrigates <= desiredFrigates
                            && numCruisers <= desiredCruisers
                            && numCarriers <= desiredCarriers
                            && numBombers <= desiredBombers
                            && numCapitals <= desiredCapitals
                            && numTroops <= desiredTroops
                            && numSupport <= desiredSupport)
                            break;
                    }
                }
                if (capacity <= 0)
                    return true;
            }
            return false;
        }


        //fbedard: add TroopsShip(troop), Bomber(drone) and Carrier(prototype) roles
        //This is broken
        public string PickFromCandidates(ShipData.RoleName role)
        {
            var potentialShips = new Array<Ship>();
            string name = "";
            Ship ship;
            int maxStr = 0;
            foreach (string shipsWeCanBuild in OwnerEmpire.ShipsWeCanBuild)
            {
                if (!ResourceManager.ShipsDict.TryGetValue(shipsWeCanBuild, out ship))
                    continue;

                if (role != ship.DesignRole)
                    continue;
                maxStr = Math.Max(maxStr, (int)ship.shipData.BaseStrength);
                
                potentialShips.Add(ship);
            }
            float nearmax = maxStr * .80f;
            //Log.Info("number of candidates : " + PotentialShips.Count + " _ trying for : " + role);
            if (potentialShips.Count > 0)
            {
                var sortedList = potentialShips.FilterBy(ships => ships.GetShipData().BaseStrength >= nearmax);
                //sortedList.OrderByDescending(ships => ships.BaseStrength);
                int newRand = (int)RandomMath.RandomBetween(0, sortedList.Length -1);                

                
                newRand = Math.Max(0, newRand);
                newRand = Math.Min(sortedList.Length -1, newRand);
                
                ship = sortedList[newRand];
                name    = ship.Name;
                if (Empire.Universe.showdebugwindow)
                    Log.Info("Chosen Role: {0}  Chosen Hull: {1}  Strength: {2}",
                        ship.GetShipData().Role, ship.GetShipData().Hull, ship.BaseStrength);
            }

            potentialShips.Clear();
            return name;
        }

    }
}