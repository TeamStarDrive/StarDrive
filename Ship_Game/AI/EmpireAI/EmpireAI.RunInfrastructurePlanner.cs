using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {
  
        private void RunInfrastructurePlanner()
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.AutoBuild)
                return;
            float sspBudget = OwnerEmpire.data.SSPBudget * .1f;
            float roadMaintenance = 0;
            float nodeMaintenance = ResourceManager.ShipsDict["Subspace Projector"].GetMaintCost(OwnerEmpire);
            foreach (SpaceRoad roadBudget in OwnerEmpire.SpaceRoadsList)
            {
                if (roadBudget.NumberOfProjectors == 0)
                    roadBudget.NumberOfProjectors = roadBudget.RoadNodesList.Count;
                roadMaintenance += roadBudget.NumberOfProjectors * nodeMaintenance;
            }

            sspBudget -= roadMaintenance;
            float underConstruction = 0f;
            foreach (Goal g in Goals)
            {
                if (!(g is BuildConstructionShip))
                    continue;
                underConstruction = underConstruction +
                                    ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(OwnerEmpire);
            }
            sspBudget -= underConstruction;
            if (sspBudget > nodeMaintenance * 2)
            {
                foreach (SolarSystem ownedSystem in OwnerEmpire.GetOwnedSystems())
                {
                    IOrderedEnumerable<SolarSystem> sortedList =
                        from otherSystem in OwnerEmpire.GetOwnedSystems()
                        orderby Vector2.Distance(otherSystem.Position, ownedSystem.Position)
                        select otherSystem;
                    int devLevelos = 0;
                    foreach (Planet p in ownedSystem.PlanetList)
                    {
                        if (p.Owner != OwnerEmpire)
                            continue;
                        devLevelos += p.Level;
                    }
                    if (devLevelos == 0)
                        continue;
                    foreach (SolarSystem origin in sortedList)
                    {
                        if (origin == ownedSystem)                        
                            continue;                        
                        int devLevel = devLevelos;
                        bool createRoad = true;
                        foreach (SpaceRoad road in OwnerEmpire.SpaceRoadsList)
                        {
                            if (road.GetOrigin() != ownedSystem && road.GetDestination() != ownedSystem)                            
                                continue;   
                            createRoad = false;
                        }                        
                        foreach (Planet p in origin.PlanetList)
                        {
                            if (p.Owner != OwnerEmpire)
                                continue;
                            devLevel += p.Level;
                        }
                        if (!createRoad)                        
                            continue;
                        
                        var newRoad = new SpaceRoad(origin, ownedSystem, OwnerEmpire, sspBudget, nodeMaintenance);

                        if (sspBudget <= 0 || newRoad.NumberOfProjectors == 0 || newRoad.NumberOfProjectors > devLevel)
                            continue;
                        
                        sspBudget -= newRoad.NumberOfProjectors * nodeMaintenance;
                        underConstruction += newRoad.NumberOfProjectors * nodeMaintenance;

                        OwnerEmpire.SpaceRoadsList.Add(newRoad);                        
                    }
                }
            }
            sspBudget = OwnerEmpire.data.SSPBudget - roadMaintenance - underConstruction;
            var toRemove = new Array<SpaceRoad>();
            foreach (SpaceRoad road in OwnerEmpire.SpaceRoadsList.OrderBy(ssps => ssps.NumberOfProjectors))
            {
                if (road.RoadNodesList.Count == 0 || sspBudget <= 0.0f)
                {                    
                    toRemove.Add(road);
                    sspBudget += road.NumberOfProjectors * nodeMaintenance;
                    continue;
                }

                RoadNode ssp = road.RoadNodesList.Find(notNull => notNull?.Platform != null);
                    
                if (ssp != null && (!road.GetOrigin().OwnerList.Contains(OwnerEmpire) ||
                                    !road.GetDestination().OwnerList.Contains(OwnerEmpire)))
                {
                    toRemove.Add(road);
                    sspBudget += road.NumberOfProjectors * nodeMaintenance;
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
                                sizecheck += !(bordernode.SourceObject is Ship) ? OwnerEmpire.ProjectorRadius : 0;
                                if (sizecheck >= bordernode.Radius)
                                    continue;
                                addNew = false;
                                break;
                            }
                        if (addNew)
                            Goals.Add(new BuildConstructionShip(node.Position, "Subspace Projector", OwnerEmpire));
                    }
                }
            }
            if (OwnerEmpire == Empire.Universe.player) return;
            {
                foreach (SpaceRoad road in toRemove)
                {
                    OwnerEmpire.SpaceRoadsList.Remove(road);
                    foreach (RoadNode node in road.RoadNodesList)
                    {
                        if (node.Platform != null && node.Platform.Active) 
                        {
                            node.Platform.Die(null, true); 
                            continue;
                        }
                        foreach (Goal g in Goals)
                        {
                            if (g.type != GoalType.DeepSpaceConstruction || !(g.BuildPosition == node.Position))                            
                                continue;
                            
                            Goals.QueuePendingRemoval(g);
                            foreach (Planet p in OwnerEmpire.GetPlanets())
                            {
                                foreach (QueueItem qi in p.ConstructionQueue)
                                {
                                    if (qi.Goal != g)                                    
                                        continue;
                                    
                                    Planet productionHere = p;
                                    productionHere.ProdHere =
                                        productionHere.ProdHere + qi.ProductionSpent;                                                                        
                                    p.ConstructionQueue.QueuePendingRemoval(qi);
                                }
                                p.ConstructionQueue.ApplyPendingRemovals();
                            }

                            using (OwnerEmpire.GetShips().AcquireReadLock())
                                foreach (Ship ship in OwnerEmpire.GetShips())
                                {
                                    ShipAI.ShipGoal goal = ship.AI.OrderQueue.PeekLast;

                                    if (goal?.Goal == null || goal.Goal.type != GoalType.DeepSpaceConstruction ||
                                        goal.Goal.BuildPosition != node.Position)                                    
                                        continue;
                                    
                                    ship.AI.OrderScrapShip();

                                    break;
                                }
                        }
                        Goals.ApplyPendingRemovals();
                    }
                    OwnerEmpire.SpaceRoadsList.Remove(road);
                }
            }
        }
    }
}