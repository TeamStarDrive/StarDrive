using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {
        private SolarSystem FindBestRoadOrigin(SolarSystem Origin, SolarSystem Destination)
        {
            SolarSystem Closest = Origin;
            Array<SolarSystem> ConnectedToOrigin = new Array<SolarSystem>();
            foreach (SpaceRoad road in this.OwnerEmpire.SpaceRoadsList)
            {
                if (road.GetOrigin() != Origin)
                {
                    continue;
                }
                ConnectedToOrigin.Add(road.GetDestination());
            }
            foreach (SolarSystem system in ConnectedToOrigin)
            {
                if (Vector2.Distance(system.Position, Destination.Position) + 25000f >=
                    Vector2.Distance(Closest.Position, Destination.Position))
                {
                    continue;
                }
                Closest = system;
            }
            if (Closest != Origin)
            {
                Closest = this.FindBestRoadOrigin(Closest, Destination);
            }
            return Closest;
        }

        private void RunInfrastructurePlanner()
        {
            //if (this.empire.SpaceRoadsList.Sum(node=> node.NumberOfProjectors) < ShipCountLimit * GlobalStats.spaceroadlimit)
            float sspBudget = this.OwnerEmpire.Money * (.01f * (1.025f - this.OwnerEmpire.data.TaxRate));
            if (sspBudget < 0 || this.OwnerEmpire.data.SSPBudget > this.OwnerEmpire.Money * .1)
            {
                sspBudget = 0;
            }
            else
            {
                this.OwnerEmpire.Money -= sspBudget;
                this.OwnerEmpire.data.SSPBudget += sspBudget;
            }
            sspBudget = this.OwnerEmpire.data.SSPBudget * .1f;
            float roadMaintenance = 0;
            float nodeMaintenance = ResourceManager.ShipsDict["Subspace Projector"].GetMaintCost(this.OwnerEmpire);
            foreach (SpaceRoad roadBudget in this.OwnerEmpire.SpaceRoadsList)
            {
                if (roadBudget.NumberOfProjectors == 0)
                    roadBudget.NumberOfProjectors = roadBudget.RoadNodesList.Count;
                roadMaintenance += roadBudget.NumberOfProjectors * nodeMaintenance;
            }

            sspBudget -= roadMaintenance;

            //this.empire.data.SSPBudget += sspBudget;
            //sspBudget = this.empire.data.SSPBudget;
            float UnderConstruction = 0f;
            foreach (Goal g in this.Goals)
            {
                //if (g.GoalName == "BuildOffensiveShips")
                //    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                //    {
                //        {
                //            UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCostRealism();
                //        }
                //    }
                //    else
                //    {
                //        {
                //            UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(this.empire);
                //        }
                //    }
                if (g.GoalName != "BuildConstructionShip")
                {
                    continue;
                }

                {
                    UnderConstruction = UnderConstruction +
                                        ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(this.OwnerEmpire);
                }
            }
            sspBudget -= UnderConstruction;
            if (sspBudget > nodeMaintenance * 2) //- nodeMaintenance * 5
            {
                foreach (SolarSystem ownedSystem in this.OwnerEmpire.GetOwnedSystems())
                    //ReaderWriterLockSlim roadlock = new ReaderWriterLockSlim();
                    //Parallel.ForEach(this.empire.GetOwnedSystems(), ownedSystem =>
                {
                    IOrderedEnumerable<SolarSystem> sortedList =
                        from otherSystem in this.OwnerEmpire.GetOwnedSystems()
                        orderby Vector2.Distance(otherSystem.Position, ownedSystem.Position)
                        select otherSystem;
                    int devLevelos = 0;
                    foreach (Planet p in ownedSystem.PlanetList)
                    {
                        if (p.Owner != this.OwnerEmpire)
                            continue;
                        //if (p.ps != Planet.GoodState.EXPORT && p.fs != Planet.GoodState.EXPORT)
                        //    continue;
                        devLevelos += p.developmentLevel;
                    }
                    if (devLevelos == 0)
                        //return;
                        continue;
                    foreach (SolarSystem Origin in sortedList)
                    {
                        if (Origin == ownedSystem)
                        {
                            continue;
                        }
                        int devLevel = devLevelos;
                        bool createRoad = true;
                        //roadlock.EnterReadLock();
                        foreach (SpaceRoad road in this.OwnerEmpire.SpaceRoadsList)
                        {
                            if (road.GetOrigin() != ownedSystem && road.GetDestination() != ownedSystem)
                            {
                                continue;
                            }
                            createRoad = false;
                        }
                        // roadlock.ExitReadLock();
                        foreach (Planet p in Origin.PlanetList)
                        {
                            if (p.Owner != this.OwnerEmpire)
                                continue;
                            devLevel += p.developmentLevel;
                        }
                        if (!createRoad)
                        {
                            continue;
                        }
                        SpaceRoad newRoad = new SpaceRoad(Origin, ownedSystem, this.OwnerEmpire, sspBudget, nodeMaintenance);

                        //roadlock.EnterWriteLock();
                        if (sspBudget <= 0 || newRoad.NumberOfProjectors == 0 || newRoad.NumberOfProjectors > devLevel)
                        {
                            // roadlock.ExitWriteLock();
                            continue;
                        }
                        sspBudget -= newRoad.NumberOfProjectors * nodeMaintenance;
                        UnderConstruction += newRoad.NumberOfProjectors * nodeMaintenance;

                        this.OwnerEmpire.SpaceRoadsList.Add(newRoad);
                        // roadlock.ExitWriteLock();
                    }
                } //);
            }
            sspBudget = this.OwnerEmpire.data.SSPBudget - roadMaintenance - UnderConstruction;
            Array<SpaceRoad> ToRemove = new Array<SpaceRoad>();
            //float income = this.empire.Money +this.empire.GrossTaxes; //this.empire.EstimateIncomeAtTaxRate(0.25f) +
            foreach (SpaceRoad road in this.OwnerEmpire.SpaceRoadsList.OrderBy(ssps => ssps.NumberOfProjectors))
            {
                if (road.RoadNodesList.Count == 0 || sspBudget <= 0.0f) // || road.NumberOfProjectors ==0)
                {
                    //if(road.NumberOfProjectors ==0)
                    //{
                    //int rnc=0;
                    //foreach(RoadNode rn in road.RoadNodesList)
                    //{
                    //    foreach(Goal G in this.Goals)                            
                    //    {                                
                    //            if (G.type != GoalType.DeepSpaceConstruction || !(g.BuildPosition == rn.Position))
                    //            {
                    //                continue;
                    //            }

                    //    }
                    //    if (rn.Platform == null)
                    //        continue;
                    //    rnc++;
                    //}
                    //if (rnc > 0)
                    //    road.NumberOfProjectors = rnc;
                    //else
                    //       ToRemove.Add(road);


                    //else
                    ToRemove.Add(road);
                    sspBudget += road.NumberOfProjectors * nodeMaintenance;
                    continue;
                }


                RoadNode ssp = road.RoadNodesList.Where(notNull => notNull != null && notNull.Platform != null)
                    .FirstOrDefault();
                if ((ssp != null && (!road.GetOrigin().OwnerList.Contains(this.OwnerEmpire) ||
                                     !road.GetDestination().OwnerList.Contains(this.OwnerEmpire))))
                {
                    ToRemove.Add(road);
                    sspBudget += road.NumberOfProjectors * nodeMaintenance;
                    //if(ssp!=null )
                    //{
                    //    this.SSPBudget += road.NumberOfProjectors * nodeMaintenance;
                    //}
                }
                else
                {
                    foreach (RoadNode node in road.RoadNodesList)
                    {
                        if (node.Platform != null && (node.Platform == null || node.Platform.Active))
                            continue;

                        bool addNew = true;
                        foreach (Goal g in Goals)
                        {
                            if (g.type != GoalType.DeepSpaceConstruction || !(g.BuildPosition == node.Position))
                                continue;
                            addNew = false;
                            break;
                        }
                        using (OwnerEmpire.BorderNodes.AcquireReadLock())
                            foreach (Empire.InfluenceNode bordernode in OwnerEmpire.BorderNodes)
                            {
                                float sizecheck = Vector2.Distance(node.Position, bordernode.Position);
                                sizecheck += !(bordernode.SourceObject is Ship) ? Empire.ProjectorRadius : 0;
                                if (sizecheck >= bordernode.Radius)
                                    continue;
                                addNew = false;
                                break;
                            }
                        if (addNew) Goals.Add(new Goal(node.Position, "Subspace Projector", OwnerEmpire));
                    }
                }
            }
            if (this.OwnerEmpire != Empire.Universe.player)
            {
                foreach (SpaceRoad road in ToRemove)
                {
                    this.OwnerEmpire.SpaceRoadsList.Remove(road);
                    foreach (RoadNode node in road.RoadNodesList)
                    {
                        if (node.Platform != null && node.Platform.Active
                        ) //(node.Platform == null || node.Platform.Active))
                        {
                            node.Platform.Die(null, true); //.GetAI().OrderScrapShip();

                            continue;
                        }


                        foreach (Goal g in this.Goals)
                        {
                            if (g.type != GoalType.DeepSpaceConstruction || !(g.BuildPosition == node.Position))
                            {
                                continue;
                            }
                            this.Goals.QueuePendingRemoval(g);
                            foreach (Planet p in this.OwnerEmpire.GetPlanets())
                            {
                                foreach (QueueItem qi in p.ConstructionQueue)
                                {
                                    if (qi.Goal != g)
                                    {
                                        continue;
                                    }
                                    Planet productionHere = p;
                                    productionHere.ProductionHere =
                                        productionHere.ProductionHere + qi.productionTowards;
                                    if (p.ProductionHere > p.MAX_STORAGE)
                                    {
                                        p.ProductionHere = p.MAX_STORAGE;
                                    }
                                    p.ConstructionQueue.QueuePendingRemoval(qi);
                                }
                                p.ConstructionQueue.ApplyPendingRemovals();
                            }

                            using (OwnerEmpire.GetShips().AcquireReadLock())
                                foreach (Ship ship in this.OwnerEmpire.GetShips())
                                {
                                    ShipAI.ShipGoal goal = ship.AI.OrderQueue.PeekLast;

                                    if (goal?.goal == null || goal.goal.type != GoalType.DeepSpaceConstruction ||
                                        goal.goal.BuildPosition != node.Position)
                                    {
                                        continue;
                                    }
                                    ship.AI.OrderScrapShip();

                                    break;
                                }
                        }
                        this.Goals.ApplyPendingRemovals();
                    }
                    this.OwnerEmpire.SpaceRoadsList.Remove(road);
                }
            }
        }
    }
}