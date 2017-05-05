using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {
    public sealed partial class GSAI
    {



        private void RunMilitaryPlanner()
        {
            #region ShipBuilding

            this.nobuild = false;
            int ShipCountLimit = GlobalStats.ShipCountLimit;
            if (!this.empire.MinorRace)
                this.RunGroundPlanner();
            this.numberOfShipGoals = 0; // 6 + this.empire.data.EconomicPersonality.ShipGoalsPlus;
            foreach (Planet p in this.empire.GetPlanets())
            {
                // if (!p.HasShipyard || (p.GetMaxProductionPotential() <2f
                if
                ( //(p.GetMaxProductionPotential() < 2f //||( this.empire.data.Traits.Cybernetic !=0 && p.GetMaxProductionPotential()-p.consumption <2f)
                    //|| p.ps == Planet.GoodState.IMPORT
                    (p.WorkerPercentage) > .75 || p.GetMaxProductionPotential() < 2f
                ) //)   //p.GetNetProductionPerTurn() < .5f))
                {
                    continue;
                }

                this.numberOfShipGoals++; //(int)(p.ProductionHere /(1+ p.ConstructionQueue.Sum(q => q.Cost)));
            }

            // this.numberOfShipGoals = this.numberOfShipGoals / this.empire.GetPlanets().Count;
            //  this.numberOfShipGoals = (int)((float)this.numberOfShipGoals* (1 - this.empire.data.TaxRate));
            float numgoals = 0f;
            float offenseUnderConstruction = 0f;
            float UnderConstruction = 0f;
            float TroopStrengthUnderConstruction = 0f;
            foreach (Goal g in this.Goals)
                //Parallel.ForEach(this.Goals, g =>
            {
                if (g.GoalName == "BuildOffensiveShips" || g.GoalName == "BuildDefensiveShips")
                {
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        UnderConstruction = UnderConstruction +
                                            ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCostRealism();
                    }
                    else
                    {
                        UnderConstruction = UnderConstruction +
                                            ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(this.empire);
                    }
                    offenseUnderConstruction += ResourceManager.ShipsDict[g.ToBuildUID].BaseStrength;
                    foreach (Troop t in ResourceManager.ShipsDict[g.ToBuildUID].TroopList)
                    {
                        TroopStrengthUnderConstruction = TroopStrengthUnderConstruction + (float) t.Strength;
                    }
                    numgoals = numgoals + 1f;
                }
                if (g.GoalName != "BuildConstructionShip")
                {
                    continue;
                }
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                {
                    UnderConstruction = UnderConstruction +
                                        ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCostRealism();
                }
                else
                {
                    UnderConstruction = UnderConstruction +
                                        ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(this.empire);
                }
            }
            //this.GetAShip(0);
            //float offensiveStrength = offenseUnderConstruction + this.empire.GetForcePoolStrength();

            //int numWars = 0;
            float offenseNeeded = 0;
            //float FearTrust = 0;
            offenseNeeded += ThreatMatrix.StrengthOfAllThreats(empire);
            offenseNeeded /= empire.currentMilitaryStrength;

            if (offenseNeeded <= 0)
            {
                offenseNeeded = 0;
            }

            //offenseNeeded += FearTrust;
            if (offenseNeeded > 20)
                offenseNeeded = 20;
            numberOfShipGoals += (int) offenseNeeded;

            //float Capacity = this.empire.EstimateIncomeAtTaxRate(tax) + this.empire.Money * -.1f -UnderConstruction + this.empire.GetAverageNetIncome();
            float AtWarBonus = 0.05f;
            if (this.empire.Money > 500f)
                AtWarBonus += (offenseNeeded * (0.03f + this.empire.getResStrat().MilitaryPriority * .03f));
            float Capacity =
                this.empire.Grossincome() * (.25f + AtWarBonus) -
                UnderConstruction; // -UnderConstruction - this.empire.GetTotalShipMaintenance();// +this.empire.GetAverageNetIncome();
            float allowable_deficit =
                -(this.empire.Money * .05f) *
                AtWarBonus; //*(1.5f-this.empire.data.TaxRate))); //>0?(1 - (this.empire.Money * 10 / this.empire.Money)):0); //-Capacity;// +(this.empire.Money * -.1f);
            //-Capacity;

            if (Capacity > this.buildCapacity)
                this.buildCapacity = Capacity;
            this.empire.data.ShipBudget = this.buildCapacity;
            if (Capacity - this.empire.GetTotalShipMaintenance() - allowable_deficit <= 0f)
            {
                Capacity -= this.empire.GetTotalShipMaintenance() - allowable_deficit;
                float HowMuchWeAreScrapping = 0f;

                foreach (Ship ship1 in this.empire.GetShips())
                {
                    if (ship1.AI.State != AIState.Scrap)
                    {
                        continue;
                    }
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        HowMuchWeAreScrapping = HowMuchWeAreScrapping + ship1.GetMaintCostRealism();
                    }
                    else
                    {
                        HowMuchWeAreScrapping = HowMuchWeAreScrapping + ship1.GetMaintCost(this.empire);
                    }
                }
                if (HowMuchWeAreScrapping < Math.Abs(Capacity))
                {
                    float Added = 0f;

                    //added by gremlin clear out building ships before active ships.
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        foreach (Goal g in this.Goals
                            .Where(goal => goal.GoalName == "BuildOffensiveShips" ||
                                           goal.GoalName == "BuildDefensiveShips")
                            .OrderByDescending(goal => ResourceManager.ShipsDict[goal.ToBuildUID]
                                .GetMaintCostRealism()))
                        {
                            bool flag = false;
                            if (g.GetPlanetWhereBuilding() == null)
                                continue;
                            foreach (QueueItem shipToRemove in g.GetPlanetWhereBuilding().ConstructionQueue)
                            {
                                if (shipToRemove.Goal != g || shipToRemove.productionTowards > 0f)
                                {
                                    continue;
                                }
                                //g.GetPlanetWhereBuilding().ProductionHere += shipToRemove.productionTowards;
                                g.GetPlanetWhereBuilding().ConstructionQueue.QueuePendingRemoval(shipToRemove);
                                this.Goals.QueuePendingRemoval(g);
                                Added += ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCostRealism();
                                flag = true;
                                break;
                            }
                            if (flag)
                                g.GetPlanetWhereBuilding().ConstructionQueue.ApplyPendingRemovals();
                            if (HowMuchWeAreScrapping + Added >= Math.Abs(Capacity))
                                break;
                        }
                    }
                    else
                    {
                        foreach (Goal g in this.Goals
                            .Where(goal => goal.GoalName == "BuildOffensiveShips" ||
                                           goal.GoalName == "BuildDefensiveShips")
                            .OrderByDescending(goal => ResourceManager.ShipsDict[goal.ToBuildUID]
                                .GetMaintCost(this.empire)))
                        {
                            bool flag = false;
                            if (g.GetPlanetWhereBuilding() == null)
                                continue;
                            foreach (QueueItem shipToRemove in g.GetPlanetWhereBuilding().ConstructionQueue)
                            {
                                if (shipToRemove.Goal != g || shipToRemove.productionTowards > 0f)
                                {
                                    continue;
                                }
                                //g.GetPlanetWhereBuilding().ProductionHere += shipToRemove.productionTowards;
                                g.GetPlanetWhereBuilding().ConstructionQueue.QueuePendingRemoval(shipToRemove);
                                this.Goals.QueuePendingRemoval(g);
                                Added += ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(this.empire);
                                flag = true;
                                break;
                            }
                            if (flag)
                                g.GetPlanetWhereBuilding().ConstructionQueue.ApplyPendingRemovals();
                            if (HowMuchWeAreScrapping + Added >= Math.Abs(Capacity))
                                break;
                        }
                    }

                    this.Goals.ApplyPendingRemovals();
                    Capacity = Capacity + HowMuchWeAreScrapping + Added;
                }
                this.buildCapacity = Capacity;
            }
            //Capacity = this.empire.EstimateIncomeAtTaxRate(tax) - UnderConstruction;

            //if (allowable_deficit > 0f || noIncome > tax)
            //{
            //    allowable_deficit = Math.Abs(allowable_deficit);
            //}

            //this.buildCapacity = Capacity;
            if (this.buildCapacity < 0) //Scrap active ships
                this.GetAShip(this.buildCapacity); //- allowable_deficit

            //fbedard: Build Defensive ships
            bool Def = false;
            float HalfCapacity = this.buildCapacity / 2f;
            foreach (Planet planet2 in this.empire.GetPlanets())
                if (planet2.HasShipyard && planet2.ParentSystem.combatTimer > 0f)
                    Def = true;
            Capacity = this.buildCapacity;
            if (Def)
                while (Capacity - HalfCapacity > 0f
                       && numgoals < this.numberOfShipGoals / 2
                       && (Empire.Universe.globalshipCount < ShipCountLimit + recyclepool
                           ||
                           this.empire.empireShipTotal < this.empire.EmpireShipCountReserve)) //shipsize < SizeLimiter)
                {
                    string s = this.GetAShip(this.buildCapacity); //Capacity - allowable_deficit);
                    if (s == null || !this.empire.ShipsWeCanBuild.Contains(s))
                    {
                        break;
                    }
                    if (this.recyclepool > 0)
                    {
                        this.recyclepool--;
                    }

                    Goal g = new Goal(s, "BuildDefensiveShips", this.empire)
                    {
                        type = GoalType.BuildShips
                    };
                    this.Goals.Add(g);
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        Capacity = Capacity - ResourceManager.ShipsDict[s].GetMaintCostRealism();
                    }
                    else
                    {
                        Capacity = Capacity - ResourceManager.ShipsDict[s].GetMaintCost(this.empire);
                    }
                    numgoals = numgoals + 1f;
                }
            //if(numgoals >this.numberOfShipGoals)
            //    Log.Info("Offense Needed: " + this.numberOfShipGoals);
            //Build Offensive ships:
            while (Capacity > 0 //this.buildCapacity > 0 //Capacity > allowable_deficit 
                   && numgoals < this.numberOfShipGoals
                   && (Empire.Universe.globalshipCount < ShipCountLimit + recyclepool
                       || this.empire.empireShipTotal < this.empire.EmpireShipCountReserve)) //shipsize < SizeLimiter)
            {
                string s = this.GetAShip(this.buildCapacity); //Capacity - allowable_deficit);
                if (string.IsNullOrEmpty(s))
                {
                    break;
                }
                if (this.recyclepool > 0)
                {
                    this.recyclepool--;
                }

                Goal g = new Goal(s, "BuildOffensiveShips", this.empire)
                {
                    type = GoalType.BuildShips
                };
                this.Goals.Add(g);
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                {
                    Capacity = Capacity - ResourceManager.ShipsDict[s].GetMaintCostRealism();
                }
                else
                {
                    Capacity = Capacity - ResourceManager.ShipsDict[s].GetMaintCost(this.empire);
                }
                numgoals = numgoals + 1f;
            }

            foreach (Goal g in this.Goals)
            {
                if (g.type != GoalType.Colonize || g.Held)
                {
                    if (g.type != GoalType.Colonize || !g.Held || g.GetMarkedPlanet().Owner == null)
                    {
                        continue;
                    }
                    foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire
                        .AllRelations)
                    {
                        this.empire.GetGSAI().CheckClaim(Relationship, g.GetMarkedPlanet());
                    }
                    this.Goals.QueuePendingRemoval(g);

                    using (TaskList.AcquireReadLock())
                    {
                        foreach (MilitaryTask task in this.TaskList)
                        {
                            foreach (Guid held in task.HeldGoals)
                            {
                                if (held != g.guid)
                                {
                                    continue;
                                }
                                this.TaskList.QueuePendingRemoval(task);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (g.GetMarkedPlanet() != null)
                    {
                        foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in ThreatMatrix.Pins
                            .Where(pin => !((Vector2.Distance(g.GetMarkedPlanet().Position, pin.Value.Position) >=
                                             75000f)
                                            || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == empire ||
                                            pin.Value.Strength <= 0f
                                            || !this.empire
                                                .GetRelations(EmpireManager.GetEmpireByName(pin.Value.EmpireName))
                                                .AtWar)))
                        {
                            if (Vector2.Distance(g.GetMarkedPlanet().Position, pin.Value.Position) >= 75000f
                                || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == empire ||
                                pin.Value.Strength <= 0f
                                || !empire.GetRelations(EmpireManager.GetEmpireByName(pin.Value.EmpireName)).AtWar
                                && !EmpireManager.GetEmpireByName(pin.Value.EmpireName).isFaction)
                            {
                                continue;
                            }
                            Array<Goal> tohold = new Array<Goal>()
                            {
                                g
                            };
                            MilitaryTask task =
                                new MilitaryTask(g.GetMarkedPlanet().Position, 125000f, tohold, this.empire);
                            //lock (GlobalStats.TaskLocker)
                            {
                                this.TaskList.Add(task);
                                break;
                            }
                        }
                    }
                }
            }
            if (this.empire.data.DiplomaticPersonality.Territorialism < 50 &&
                this.empire.data.DiplomaticPersonality.Trustworthiness < 50
            ) //    Name == "Aggressive" || this.empire.data.DiplomaticPersonality.Name == "Ruthless" || this.empire.data.EconomicPersonality.Name == "Expansionist")
            {
                foreach (Goal g in this.Goals)
                {
                    if (g.type != GoalType.Colonize || g.Held)
                    {
                        continue;
                    }
                    bool OK = true;

                    using (TaskList.AcquireReadLock())
                    {
                        foreach (MilitaryTask mt in this.TaskList)
                            //Parallel.ForEach(this.TaskList, (mt,state) =>
                        {
                            if ((mt.type != MilitaryTask.TaskType.DefendClaim
                                 && mt.type != MilitaryTask.TaskType.ClearAreaOfEnemies)
                                || g.GetMarkedPlanet() != null
                                && !(mt.TargetPlanetGuid == g.GetMarkedPlanet().guid))
                            {
                                continue;
                            }
                            OK = false;
                            break;
                        }
                    }
                    if (!OK)
                    {
                        continue;
                    }
                    if (g.GetMarkedPlanet() == null)
                        continue;
                    MilitaryTask task = new MilitaryTask()
                    {
                        AO = g.GetMarkedPlanet().Position
                    };
                    task.SetEmpire(this.empire);
                    task.AORadius = 75000f;
                    task.SetTargetPlanet(g.GetMarkedPlanet());
                    task.TargetPlanetGuid = g.GetMarkedPlanet().guid;
                    task.type = MilitaryTask.TaskType.DefendClaim;
                    //lock (GlobalStats.TaskLocker)
                    {
                        this.TaskList.Add(task);
                    }
                }
            }
            this.Goals.ApplyPendingRemovals();

            #endregion

            //this where the global AI attack stuff happenes.
            using (TaskList.AcquireReadLock())
            {
                Array<MilitaryTask> ToughNuts = new Array<MilitaryTask>();
                Array<MilitaryTask> InOurSystems = new Array<MilitaryTask>();
                Array<MilitaryTask> InOurAOs = new Array<MilitaryTask>();
                Array<MilitaryTask> Remainder = new Array<MilitaryTask>();
                Vector2 EmpireCenter = this.empire.GetWeightedCenter();
                //var tasksort = from tasks in  this.TaskList
                //               where tasks.type == MilitaryTask.TaskType.AssaultPlanet
                //               orderby Vector2.Distance(EmpireCenter,tasks.GetTargetPlanet().Owner.GetWeightedCenter()),


                //               select tasks
                //               ;
                //float distance = 0;

                foreach (MilitaryTask task in this
                    .TaskList //.OrderBy(target => Vector2.Distance(target.AO, this.empire.GetWeightedCenter()) / 1500000)
                    .OrderByDescending(empire =>
                    {
                        if (empire.type != MilitaryTask.TaskType.AssaultPlanet)
                            return 0;
                        float weight = 0;
                        weight += (this.empire.currentMilitaryStrength - empire.MinimumTaskForceStrength) /
                                  this.empire.currentMilitaryStrength * 5;
                        //weight += ((Empire.Universe.UniverseRadius.X*.25f) - this.GetDistanceFromOurAO(empire.AO)) / (Empire.Universe.UniverseRadius.X * .25f) * 10;

                        if (empire.GetTargetPlanet() == null)
                        {
                            return weight * 2;
                        }
                        Empire emp = empire.GetTargetPlanet().Owner;
                        if (emp == null)
                            return 0;
                        if (emp.isFaction)
                            return 0;

                        Relationship test = null;
                        if (this.empire.TryGetRelations(emp, out test) && test != null)
                        {
                            if (test.Treaty_NAPact || test.Treaty_Alliance || test.Posture != Posture.Hostile)
                                return 0;
                            weight += ((test.TotalAnger * .25f) - (100 - test.Threat)) / (test.TotalAnger * .25f) * 5f;
                            if (test.AtWar)
                                weight += 5;
                        }
                        Planet target = empire.GetTargetPlanet();
                        if (target != null)
                        {
                            SystemCommander scom;
                            target.Owner.GetGSAI()
                                .DefensiveCoordinator.DefenseDict.TryGetValue(target.system, out scom);
                            if (scom != null)
                                weight += 11 - scom.RankImportance;
                            //weight += (target.MaxPopulation /1000) + (target.MineralRichness + (int)target.developmentLevel);
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
                        ToughNuts.Add(task);
                    }
                    else if (!this.empire.GetOwnedSystems().Contains(task.GetTargetPlanet().system))
                    {
                        bool dobreak = false;
                        foreach (KeyValuePair<Guid, Planet> entry in Empire.Universe.PlanetsDict)
                        {
                            if (task.GetTargetPlanet() == entry.Value)
                            {
                                foreach (AO area in AreasOfOperations)
                                {
                                    if (entry.Value.Position.OutsideRadius(area.Position, area.Radius))
                                        continue;
                                    InOurAOs.Add(task);
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
                        Remainder.Add(task);
                    }
                    else
                    {
                        InOurSystems.Add(task);
                    }
                }
                //this.TaskList.thisLock.ExitReadLock();
                Array<MilitaryTask> TNInOurSystems = new Array<MilitaryTask>();
                Array<MilitaryTask> TNInOurAOs = new Array<MilitaryTask>();
                Array<MilitaryTask> TNRemainder = new Array<MilitaryTask>();
                this.toughnuts = ToughNuts.Count;
                foreach (MilitaryTask task in ToughNuts)
                {
                    if (!this.empire.GetOwnedSystems().Contains(task.GetTargetPlanet().system))
                    {
                        bool dobreak = false;
                        foreach (KeyValuePair<Guid, Planet> entry in Empire.Universe.PlanetsDict)
                        {
                            if (task.GetTargetPlanet() != entry.Value)
                            {
                                continue;
                            }
                            foreach (AO area in AreasOfOperations)
                            {
                                if (entry.Value.Position.OutsideRadius(area.Position, area.Radius))
                                    continue;
                                TNInOurAOs.Add(task);
                                dobreak = true;
                                break;
                            }
                            break;
                        }
                        if (dobreak)
                        {
                            continue;
                        }
                        TNRemainder.Add(task);
                    }
                    else
                    {
                        TNInOurSystems.Add(task);
                    }
                }
                foreach (MilitaryTask task in TNInOurAOs)
                    //Parallel.ForEach(TNInOurAOs, task =>
                {
                    if (task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner == this.empire ||
                        this.empire.GetRelations(task.GetTargetPlanet().Owner).ActiveWar == null ||
                        (float) this.empire.TotalScore <= (float) task.GetTargetPlanet().Owner.TotalScore * 1.5f)
                    {
                        continue;
                        //return;
                    }
                    task.Evaluate(this.empire);
                } //);
                foreach (MilitaryTask task in TNInOurSystems)
                    //Parallel.ForEach(TNInOurSystems, task =>
                {
                    task.Evaluate(this.empire);
                } //);
                foreach (MilitaryTask task in TNRemainder)
                {
                    if (task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner == this.empire ||
                        this.empire.GetRelations(task.GetTargetPlanet().Owner).ActiveWar == null ||
                        (float) this.empire.TotalScore <= (float) task.GetTargetPlanet().Owner.TotalScore * 1.5f)
                    {
                        continue;
                    }
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in InOurAOs)
                {
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in InOurSystems)
                {
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in Remainder)
                {
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in this.TaskList)
                {
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet)
                    {
                        task.Evaluate(this.empire);
                    }
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet &&
                        task.type != MilitaryTask.TaskType.GlassPlanet || task.GetTargetPlanet().Owner != null &&
                        task.GetTargetPlanet().Owner != this.empire)
                    {
                        continue;
                    }
                    task.EndTask();
                }
            }
            this.TaskList.AddRange(this.TasksToAdd);
            this.TasksToAdd.Clear();
            this.TaskList.ApplyPendingRemovals();
        }


        //fbedard: Build a ship with a random role
        bool nobuild = false;
        private string GetAShip(float Capacity)
        {
            if (nobuild)
                return null;
            float ratio_Fighters = 1f;
            float ratio_Corvettes = 0f;
            float ratio_Frigates = 0f;
            float ratio_Cruisers = 0f;
            float ratio_Capitals = 0f;
            float ratio_Bombers = 0f;
            //float ratio_TroopShips = 0f;          //Not referenced in code, removing to save memory
            float ratio_Carriers = 0f;
            float ratio_Support = 0f;
            /*     
      float capFighters = 0f;
      float capCorvettes = 0f;
      float capFrigates = 0f;
      float capCruisers = 0f;
      float capCapitals = 0f;
      float capBombers = 0f;



              */
            float capBombers = 0f;
            float capCarriers = 0f;
            float capSupport = 0f;
            float capTroops = 0f;

            float numFighters = 0;
            float numCorvettes = 0;
            float numFrigates = 0;
            float numCruisers = 0;
            float numCarriers = 0f;
            float numBombers = 0f;
            float numCapitals = 0f;
            float numTroops = 0f;
            float numSupport = 0f;
            float capScrapping = 0;
            float TotalUpkeep = 0;
            /*
            float capFreighters = 0;
            float capPlatforms = 0;
            float capStations = 0;
            float nonMilitaryCap = 0;
            */
            float TotalMilShipCount = 0f;

            //Count the active ships
            for (int i = 0; i < this.empire.GetShips().Count(); i++)
            {
                Ship item = this.empire.GetShips()[i];
                if (item != null && item.Active && item.Mothership == null && item.AI.State != AIState.Scrap)
                {
                    ShipData.RoleName str = item.shipData.HullRole;
                    float upkeep = 0f;
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
                        TotalMilShipCount++;
                        capCarriers += upkeep;
                        TotalUpkeep += upkeep;
                    }
                    //troops ship
                    else if ((item.HasTroopBay || item.hasTransporter || item.hasAssaultTransporter) && str >= ShipData.RoleName.freighter
                             && item.GetHangars().Where(troopbay => troopbay.IsTroopBay).Sum(size => size.XSIZE * size.YSIZE)
                             + item.Transporters.Sum(troopbay => (troopbay.TransporterTroopAssault > 0 ? troopbay.YSIZE * troopbay.XSIZE : 0)) > item.Size * .10f
                    )
                    {
                        numTroops += upkeep;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
                        capTroops = +upkeep;

                    }

                    else if (item.hasOrdnanceTransporter || item.hasRepairBeam || item.HasSupplyBays || item.hasOrdnanceTransporter || item.InhibitionRadius > 0
                    )
                    {
                        numSupport++;
                        TotalUpkeep += upkeep;
                        TotalMilShipCount++;
                        capSupport += upkeep;
                    }
                    else if (item.BombBays.Count * 4 > item.Size * .20f && str >= ShipData.RoleName.freighter
                    )
                    {
                        numBombers += upkeep;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
                        capBombers += upkeep;
                        //capBombers += upkeep;
                    }

                    //capital and carrier without hangars
                    else if (str == ShipData.RoleName.capital || str == ShipData.RoleName.carrier)
                    {
                        numCapitals++;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
                    }
                    //bomber

                    else if (str == ShipData.RoleName.fighter || str == ShipData.RoleName.scout)
                    {
                        numFighters++;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
                        // capFighters += upkeep;
                    }
                    else if (str == ShipData.RoleName.corvette || str == ShipData.RoleName.gunboat)
                    {
                        numCorvettes++;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
                    }
                    else if (str == ShipData.RoleName.frigate || str == ShipData.RoleName.destroyer)
                    {
                        numFrigates++;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
                    }
                    else if (str == ShipData.RoleName.cruiser)
                    {
                        numCruisers++;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
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
            this.FreighterUpkeep = capFreighters - (nonMilitaryCap * .25f);
            this.PlatformUpkeep = capPlatforms - (nonMilitaryCap * .25f);
            this.StationUpkeep = capStations - (nonMilitaryCap * .5f);
            */
            //if (!this.empire.canBuildCapitals && Ship_Game.ResourceManager.TechTree.ContainsKey("Battleships"))
            //    this.empire.canBuildCapitals = this.empire.GetTDict()["Battleships"].Unlocked;
            //if (!this.empire.canBuildCruisers && Ship_Game.ResourceManager.TechTree.ContainsKey("Cruisers"))
            //    this.empire.canBuildCruisers = this.empire.GetTDict()["Cruisers"].Unlocked;
            //if (!this.empire.canBuildFrigates && Ship_Game.ResourceManager.TechTree.ContainsKey("FrigateConstruction"))
            //    this.empire.canBuildFrigates = this.empire.GetTDict()["FrigateConstruction"].Unlocked;
            //if (!this.empire.canBuildCorvettes && Ship_Game.ResourceManager.TechTree.ContainsKey("HeavyFighterHull"))
            //    this.empire.canBuildCorvettes = this.empire.GetTDict()["HeavyFighterHull"].Unlocked;

            //Set ratio by class
            numBombers = numBombers * capBombers;
            numCarriers = numCarriers * capCarriers;
            numSupport = numSupport * capSupport;
            numTroops = numTroops * capTroops;
            if (this.empire.canBuildCapitals) //&& TotalMilShipCount >10)
            {
                ratio_Fighters = 0f;
                ratio_Corvettes = .0f;
                ratio_Frigates = 10f;
                ratio_Cruisers = 5f;
                ratio_Capitals = 1f;
                if (this.empire.canBuildBombers)
                {
                    ratio_Bombers = 5f;
                    //numBombers = numBombers  //(float)Math.Ceiling((double)(numBombers / ResourceManager.ShipRoles[ShipData.RoleName.cruiser].Upkeep));
                }

                if (this.empire.canBuildCarriers)
                {
                    ratio_Carriers = 1f;
                    //numCarriers =(float)Math.Ceiling((double)(numCarriers / ResourceManager.ShipRoles[ShipData.RoleName.cruiser].Upkeep));
                }
                ratio_Support = 1f;
            }
            else if (this.empire.canBuildCruisers) // && TotalMilShipCount > 10)
            {
                ratio_Fighters = 10f;
                ratio_Corvettes = 10f;
                ratio_Frigates = 5f;
                ratio_Cruisers = 1f;
                ratio_Capitals = 0f;
                if (this.empire.canBuildBombers)
                {
                    ratio_Bombers = 5f;
                    // numBombers = (float)Math.Ceiling((double)(numBombers / ResourceManager.ShipRoles[ShipData.RoleName.frigate].Upkeep));
                }

                if (this.empire.canBuildCarriers)
                {
                    ratio_Carriers = 1f;
                    //numCarriers = (float)Math.Ceiling((double)(numCarriers / ResourceManager.ShipRoles[ShipData.RoleName.frigate].Upkeep));
                }
                ratio_Support = 1f;
            }
            else if (this.empire.canBuildFrigates)//&& TotalMilShipCount > 10)
            {
                ratio_Fighters = 10f;
                ratio_Corvettes = 5f;
                ratio_Frigates = 1f;
                ratio_Cruisers = 0f;
                ratio_Capitals = 0f;
                if (this.empire.canBuildBombers)
                {
                    ratio_Bombers = 5f;
                    //numBombers = (float)Math.Ceiling((double)(numBombers / ResourceManager.ShipRoles[ShipData.RoleName.corvette].Upkeep));
                }

                if (this.empire.canBuildCarriers)
                {
                    ratio_Carriers = 1f;
                    //numCarriers = (float)Math.Ceiling((double)(numCarriers / ResourceManager.ShipRoles[ShipData.RoleName.corvette].Upkeep));
                }
                ratio_Support = 1f;
            }
            else if (this.empire.canBuildCorvettes)//&& TotalMilShipCount > 10)
            {
                ratio_Fighters = 5f;
                ratio_Corvettes = 1f;
                ratio_Frigates = 0f;
                ratio_Cruisers = 0f;
                ratio_Capitals = 0f;
                ratio_Carriers = 0;
                if (this.empire.canBuildBombers)
                {
                    ratio_Bombers = 1f;
                    //numBombers = (float)Math.Ceiling((double)(numBombers / ResourceManager.ShipRoles[ShipData.RoleName.corvette].Upkeep));
                }
            }
            else
            {
                ratio_Bombers = 0f;
                ratio_Carriers = 0;
            }
            float totalRatio = ratio_Fighters + ratio_Corvettes + ratio_Frigates + ratio_Cruisers + ratio_Capitals + ratio_Bombers + ratio_Support + ratio_Carriers;
            bool atwar = (this.empire.AllRelations.Where(war => war.Value.AtWar).Count() > 0);

            if (TotalMilShipCount <= 0)
                totalRatio = 1;
            if (TotalUpkeep == 0)
                TotalUpkeep = 1;
            ratio_Bombers += this.toughnuts * .2f;
            float goal = Capacity / TotalUpkeep;// / TotalMilShipCount);
            float adjustedRatio = TotalMilShipCount / totalRatio;
            if (adjustedRatio == 0)
                adjustedRatio = 10;
            float DesiredFighters = (float)Math.Ceiling((double)(adjustedRatio * ratio_Fighters * goal));
            float DesiredCorvettes = (float)Math.Ceiling((double)(adjustedRatio * ratio_Corvettes * goal));
            float DesiredFrigates = (float)Math.Ceiling((double)(adjustedRatio * ratio_Frigates * goal));
            float DesiredCruisers = (float)Math.Ceiling((double)(adjustedRatio * ratio_Cruisers * goal));
            float DesiredCapitals = (float)Math.Ceiling((double)(adjustedRatio * ratio_Capitals * goal));
            float DesiredCarriers = (float)Math.Ceiling((double)(adjustedRatio * ratio_Carriers * goal));  //this.empire.canBuildCarriers ? (DesiredCapitals + DesiredCruisers) / 4f : 0;
            float DesiredBombers = (float)Math.Ceiling((double)(adjustedRatio * ratio_Bombers * goal));
            float DesiredSupport = (float)Math.Ceiling((double)(adjustedRatio * ratio_Support * goal));
            float DesiredTroops = 0;

            //if(this.empire.canBuildBombers)
            //{
            //    DesiredBombers = TotalMilShipCount / 15f;

            //}
            if (this.empire.canBuildTroopShips)
            {
                DesiredTroops = (float)Math.Ceiling((double)(atwar ? TotalMilShipCount / 10f : TotalMilShipCount / 30f));
            }
#if DEBUG
            //Log.Info("Build Ratios for: " + this.empire.data.PortraitName);
            //Log.Info("fighters: " + DesiredFighters + " / " + numFighters);
            //Log.Info("corvettes: " + DesiredCorvettes + " / " + numCorvettes);
            //Log.Info("Frigates: " + DesiredFrigates + " / " + numFrigates);
            //Log.Info("Cruisers: " + DesiredCruisers + " / " + numCruisers);
            //Log.Info("Capitals: " + DesiredCapitals + " / " + numCapitals);
            //Log.Info("Carriers: " + DesiredCarriers + " / " + numCarriers);
            //Log.Info("Bombers: " + DesiredBombers + " / " + numBombers);
            //Log.Info("TroopsHips: " + DesiredTroops + " / " + numTroops);
            //Log.Info("Capacity: " + Capacity);
            //Log.Info("ShipGoals: " + this.empire.GetGSAI().numberOfShipGoals);

#endif
            //Scrap ships when overspending by class
            if (this.buildCapacity / (TotalUpkeep * .90f + 1) < 1)  //capScrapping prevent from scrapping too much
                #region MyRegion
            {
                if (numFighters > DesiredFighters ||
                    numCorvettes > DesiredCorvettes ||
                    numFrigates > DesiredFrigates ||
                    numCruisers > DesiredCruisers ||
                    numCarriers > DesiredCarriers ||
                    numBombers > DesiredBombers ||
                    numCapitals > DesiredCapitals ||
                    numTroops > DesiredTroops)
                {
                    foreach (Ship ship in this.empire.GetShips()
                        .Where(ship => !ship.InCombat && ship.inborders && ship.fleet == null && ship.AI.State != AIState.Scrap && ship.Mothership == null && ship.Active && ship.shipData.HullRole >= ShipData.RoleName.fighter && ship.GetMaintCost(this.empire) > 0)
                        .OrderByDescending(defense => this.DefensiveCoordinator.DefensiveForcePool.Contains(defense))
                        .ThenBy(ship => ship.Level)
                        .ThenBy(ship => ship.BaseStrength)
                    )
                    {
                        if (numFighters > (DesiredFighters) && (ship.shipData.HullRole == ShipData.RoleName.fighter || ship.shipData.HullRole == ShipData.RoleName.scout))
                        {
                            numFighters--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCarriers > (DesiredCarriers) && (ship.GetHangars().Where(fighters => fighters.MaximumHangarShipSize > 0).Count() > 0 == true))
                        {
                            numCarriers--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numTroops > (DesiredTroops) && (ship.HasTroopBay || ship.hasTransporter))
                        {
                            numTroops--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numBombers > (DesiredBombers) && (ship.BombBays.Count > 0))
                        {
                            numBombers--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCorvettes > (DesiredCorvettes) && (ship.shipData.HullRole == ShipData.RoleName.corvette || ship.shipData.HullRole == ShipData.RoleName.gunboat))
                        {
                            numCorvettes--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numFrigates > (DesiredFrigates) && (ship.shipData.HullRole == ShipData.RoleName.frigate || ship.shipData.HullRole == ShipData.RoleName.destroyer))
                        {
                            numFrigates--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCruisers > (DesiredCruisers) && ship.shipData.HullRole == ShipData.RoleName.cruiser)
                        {
                            numCruisers--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCapitals > (DesiredCapitals) && (ship.shipData.HullRole == ShipData.RoleName.capital || ship.shipData.HullRole == ShipData.RoleName.carrier))
                        {
                            numCapitals--;
                            ship.AI.OrderScrapShip();
                        }

                        if (numFighters <= DesiredFighters
                            && numCorvettes <= DesiredCorvettes
                            && numFrigates <= DesiredFrigates
                            && numCruisers <= DesiredCruisers
                            && numCarriers <= DesiredCarriers
                            && numBombers <= DesiredBombers
                            && numCapitals <= DesiredCapitals
                            && numTroops <= DesiredTroops)
                        {
                            break;
                        }
                    }
                }
                if (Capacity <= 0)
                    return null;  //no money to build !
            }
            #endregion

            //Find ship to build
            bool ranA = false;

            if (RandomMath.RandomBetween(0f, 1f) < 0.5f)
                ranA = true;

            Array<Ship> PotentialShips = new Array<Ship>();
            Map<ShipData.RoleName, float> PickRoles = new Map<ShipData.RoleName, float>();
            this.empire.UpdateShipsWeCanBuild();
            string buildThis;

            bool destroyer = false;
            bool gunboats = false;
            bool carriers = false;
            foreach (KeyValuePair<string, bool> hull in this.empire.GetHDict())
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
            if (numTroops < DesiredTroops)
                PickRoles.Add(ShipData.RoleName.troop, numTroops / DesiredTroops);
            if (numFighters < DesiredFighters)
            {
                PickRoles.Add(ShipData.RoleName.fighter, numFighters / (DesiredFighters));
                //PickRoles.Add(ShipData.RoleName.scout, numFighters / (DesiredFighters + ranB)); //scouts are handeled somewhere else and generally are not good at anything
            }
            if (numCorvettes < DesiredCorvettes)
            {
                if (gunboats)
                {
                    if (ranA)
                        PickRoles.Add(ShipData.RoleName.gunboat, numCorvettes / (DesiredCorvettes));
                    else
                        PickRoles.Add(ShipData.RoleName.corvette, numCorvettes / (DesiredCorvettes));
                }
                else
                    PickRoles.Add(ShipData.RoleName.corvette, numCorvettes / (DesiredCorvettes));
            }
            if (numBombers < DesiredBombers)
                PickRoles.Add(ShipData.RoleName.drone, numBombers / DesiredBombers);
            if (numFrigates < DesiredFrigates)
            {
                if (destroyer)
                {
                    if (ranA)
                        PickRoles.Add(ShipData.RoleName.frigate, numFrigates / (DesiredFrigates));
                    else
                        PickRoles.Add(ShipData.RoleName.destroyer, numFrigates / (DesiredFrigates));
                }
                else
                    PickRoles.Add(ShipData.RoleName.frigate, numFrigates / (DesiredFrigates));
            }
            if (numCruisers < DesiredCruisers)
                PickRoles.Add(ShipData.RoleName.cruiser, numCruisers / DesiredCruisers);
            if (numCapitals < DesiredCapitals)
            {
                if (carriers)
                {
                    if (ranA)
                        PickRoles.Add(ShipData.RoleName.carrier, numCapitals / (DesiredCapitals));
                    else
                        PickRoles.Add(ShipData.RoleName.capital, numCapitals / (DesiredCapitals));
                }
                else
                {
                    PickRoles.Add(ShipData.RoleName.capital, numCapitals / (DesiredCapitals));
                }
            }
            if (numCarriers < DesiredCarriers)
                PickRoles.Add(ShipData.RoleName.prototype, numCarriers / DesiredCarriers);

            foreach (KeyValuePair<ShipData.RoleName, float> pick in PickRoles.OrderBy(val => val.Value))
            {
                buildThis = this.PickFromCandidates(pick.Key, Capacity, PotentialShips);
                if (!string.IsNullOrEmpty(buildThis))
                {
                    //Log.Info("Chosen: " + buildThis);
                    //Log.Info("TroopsHips: " + DesiredTroops);
                    return buildThis;
                }
            }
            //if(Empire.Universe.viewState == UniverseScreen.UnivScreenState.GalaxyView )
            //    Log.Info("Chosen: Nothing");
            this.nobuild = true;
            return null;  //Find nothing to build !
        }


        //fbedard: add TroopsShip(troop), Bomber(drone) and Carrier(prototype) roles
        public string PickFromCandidates(ShipData.RoleName role, float Capacity, Array<Ship> PotentialShips)
        {
            string name = "";
            Ship ship;
            int maxtech = 0;
            //float upkeep;          //Not referenced in code, removing to save memory
            foreach (string shipsWeCanBuild in this.empire.ShipsWeCanBuild)
            {
                if (!ResourceManager.ShipsDict.TryGetValue(shipsWeCanBuild, out ship))
                    continue;
                bool bombs = false;
                bool hangars = false;
                bool troops = false;
                int bombcount = 0;
                int hangarcount = 0;

                foreach (ShipModule slot in ship.ModuleSlotList)
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


                //    upkeep = ship.GetMaintCost(this.empire); //this automatically calls realistic maintenance cost if needed. 
                //Capacity < upkeep ||
                if (role == ShipData.RoleName.drone || role == ShipData.RoleName.troop)
                {
                    if (!this.NonCombatshipIsGoodForGoals(ship) || ship.shipData.HullRole < ShipData.RoleName.freighter)
                        continue;
                }
                else
                if (!shipIsGoodForGoals(ship) || ship.shipData.HullRole < ShipData.RoleName.freighter)
                    continue;
                if (role == ShipData.RoleName.troop && !troops)
                    continue;
                else if (role == ShipData.RoleName.drone && !bombs)
                    continue;
                else if (role == ShipData.RoleName.prototype && !hangars)
                    continue;
                else if (role != ship.shipData.HullRole && role == ShipData.RoleName.prototype && role != ShipData.RoleName.drone && role != ShipData.RoleName.troop)
                    continue;
                if (ship.shipData.techsNeeded.Count > maxtech)
                    maxtech = ship.shipData.techsNeeded.Count;
                PotentialShips.Add(ship);
            }
            float nearmax = maxtech * .5f;
            //Log.Info("number of candidates : " + PotentialShips.Count + " _ trying for : " + role);
            if (PotentialShips.Count > 0)
            {
                IOrderedEnumerable<Ship> sortedList =
                    from ship3 in PotentialShips
                    orderby ship3.shipData.techsNeeded.Count >= nearmax descending, ship3.BaseStrength descending
                    select ship3;
                maxtech++;
                int ran = (int)(sortedList.Count() * .5f);
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
                foreach (string known in this.empire.ShipsWeCanBuild)
                {
                    ships += known + " : ";
                }
                Log.Info(ships);
#endif
            }
            PotentialShips.Clear();
            return name;
        }

        public bool shipIsGoodForGoals(Ship ship)
        {
            if (ship.BaseStrength > 0f && ship.shipData.ShipStyle != "Platforms" && !ship.shipData.CarrierShip && ship.BaseCanWarp && ship.ModulePowerDraw * this.empire.data.FTLPowerDrainModifier <= ship.PowerFlowMax
                || (ship.ModulePowerDraw * this.empire.data.FTLPowerDrainModifier > ship.PowerFlowMax
                    && ship.PowerStoreMax / (ship.ModulePowerDraw * this.empire.data.FTLPowerDrainModifier - ship.PowerFlowMax) * ship.velocityMaximum > minimumWarpRange))
                return true;
            return false;
        }
        public bool NonCombatshipIsGoodForGoals(Ship ship)
        {
            if (ship.shipData.ShipStyle != "Platforms" && !ship.shipData.CarrierShip && ship.BaseCanWarp && ship.ModulePowerDraw * this.empire.data.FTLPowerDrainModifier <= ship.PowerFlowMax
                || (ship.ModulePowerDraw * this.empire.data.FTLPowerDrainModifier > ship.PowerFlowMax
                    && ship.PowerStoreMax / (ship.ModulePowerDraw * this.empire.data.FTLPowerDrainModifier - ship.PowerFlowMax) * ship.velocityMaximum > minimumWarpRange))
                return true;
            return false;
        }

    }
}