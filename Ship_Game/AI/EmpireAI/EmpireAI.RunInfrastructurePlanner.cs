using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        float GetTotalConstructionGoalsMaintenance()
        {
            float maintenance = 0f;
            foreach (Goal g in Goals)
            {
                if (g is BuildConstructionShip)
                    maintenance += ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(OwnerEmpire);
            }
            return maintenance;
        }

        int GetCurrentProjectorCount()
        {
            int numProjectors = 0;
            for (int i = 0; i < OwnerEmpire.SpaceRoadsList.Count; ++i)
            {
                SpaceRoad road = OwnerEmpire.SpaceRoadsList[i]; // for i -- resilient to multi-threaded changes
                numProjectors += road.NumberOfProjectors == 0 ? road.RoadNodesList.Count : road.NumberOfProjectors;
            }
            return numProjectors;
        }

        int GetSystemDevelopmentLevel(SolarSystem system)
        {
            int level = 0;
            foreach (Planet p in system.PlanetList)
            {
                if (p.Owner == OwnerEmpire)
                    level += p.Level;
            }
            return level;
        }

        bool SpaceRoadExists(SolarSystem a, SolarSystem b)
        {
            if (a == b)
                return true;
            foreach (SpaceRoad road in OwnerEmpire.SpaceRoadsList)
                if ((road.Origin == a && road.Destination == b) ||
                    (road.Origin == b && road.Destination == a))
                    return true;
            return false;
        }

        public static bool InfluenceNodeExistsAt(Vector2 pos, Empire empire)
        {
            using (empire.BorderNodes.AcquireReadLock())
            {
                foreach (Empire.InfluenceNode border in empire.BorderNodes)
                {
                    float extra = border.SourceObject is Ship ? 0 : empire.ProjectorRadius;
                    if (pos.InRadius(border.Position, border.Radius - extra*0.75f))
                        return true;
                }
            }
            return false;
        }

        public bool NodeAlreadyExistsAt(Vector2 pos)
        {
            foreach (Goal g in Goals)
                if (g is BuildConstructionShip && g.BuildPosition.InRadius(pos, 1000f))
                    return true;

            // bugfix: make sure another construction ship isn't already deploying to pos
            var ships = OwnerEmpire.GetShips();
            foreach (Ship ship in ships)
            {
                if (ship.AI.FindGoal(ShipAI.Plan.DeployStructure, out ShipAI.ShipGoal goal)
                    && goal.MovePosition.InRadius(pos, 1000f))
                    return true;
            }

            return InfluenceNodeExistsAt(pos, OwnerEmpire);
        }

        void RunInfrastructurePlanner()
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.AutoBuild)
                return;

            float nodeMaintenance = ResourceManager.ShipsDict["Subspace Projector"].GetMaintCost(OwnerEmpire);
            float roadMaintenance = GetCurrentProjectorCount() * nodeMaintenance;
            float underConstruction = GetTotalConstructionGoalsMaintenance();

            float roadBudget = OwnerEmpire.data.SSPBudget * 0.1f - roadMaintenance - underConstruction;

            if (roadBudget > (nodeMaintenance * 2))
            {
                underConstruction = CreateNewRoads(roadBudget, nodeMaintenance, underConstruction);
            }

            roadBudget = OwnerEmpire.data.SSPBudget - roadMaintenance - underConstruction;
            var toRemove = new Array<SpaceRoad>();
            foreach (SpaceRoad road in OwnerEmpire.SpaceRoadsList.OrderBy(road => road.NumberOfProjectors))
            {
                if (road.RoadNodesList.Count == 0 || roadBudget <= 0.0f)
                {                    
                    toRemove.Add(road);
                    roadBudget += road.NumberOfProjectors * nodeMaintenance;
                    continue;
                }

                RoadNode ssp = road.RoadNodesList.Find(notNull => notNull?.Platform != null);
                    
                if (ssp != null && (!road.Origin.OwnerList.Contains(OwnerEmpire) ||
                                    !road.Destination.OwnerList.Contains(OwnerEmpire)))
                {
                    toRemove.Add(road);
                    roadBudget += road.NumberOfProjectors * nodeMaintenance;
                }
                else
                {
                    foreach (RoadNode node in road.RoadNodesList)
                    {
                        if (node.Platform == null || (node.Platform != null && !node.Platform.Active))
                        {
                            bool nodeExists = NodeAlreadyExistsAt(node.Position);
                            if (OwnerEmpire.isPlayer) // DEBUG
                                Log.Info($"NodeAlreadyExists? {node.Position}: {nodeExists}");

                            if (!nodeExists)
                            {
                                node.Platform = null;
                                Log.Info($"BuildProjector {node.Position}");
                                Goals.Add(new BuildConstructionShip(node.Position, "Subspace Projector", OwnerEmpire));
                            }
                        }
                    }
                }
            }

            if (OwnerEmpire != Empire.Universe.player)
                ScrapSpaceRoadsForAI(toRemove);
        }

        void ScrapSpaceRoadsForAI(Array<SpaceRoad> toRemove)
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
                                if (qi.Goal == g)
                                {
                                    p.ProdHere += qi.ProductionSpent;
                                    p.ConstructionQueue.QueuePendingRemoval(qi);
                                }
                            }

                            p.ConstructionQueue.ApplyPendingRemovals();
                        }

                        using (OwnerEmpire.GetShips().AcquireReadLock())
                        {
                            foreach (Ship ship in OwnerEmpire.GetShips())
                            {
                                ShipAI.ShipGoal goal = ship.AI.OrderQueue.PeekLast;
                                if (goal?.Goal != null &&
                                    goal.Goal.type == GoalType.DeepSpaceConstruction &&
                                    goal.Goal.BuildPosition == node.Position)
                                {
                                    ship.AI.OrderScrapShip();
                                    break;
                                }
                            }
                        }
                    }

                    Goals.ApplyPendingRemovals();
                }

                OwnerEmpire.SpaceRoadsList.Remove(road);
            }
        }

        float CreateNewRoads(float roadBudget, float nodeMaintenance, float underConstruction)
        {
            foreach (SolarSystem destination in OwnerEmpire.GetOwnedSystems())
            {
                int destSystemDevLevel = GetSystemDevelopmentLevel(destination);
                if (destSystemDevLevel == 0)
                    continue;

                SolarSystem[] systemsByDistance = OwnerEmpire.GetOwnedSystems()
                    .Sorted(s => s.Position.Distance(destination.Position));
                foreach (SolarSystem origin in systemsByDistance)
                {
                    if (!SpaceRoadExists(origin, destination))
                    {
                        int roadDevLevel = destSystemDevLevel + GetSystemDevelopmentLevel(origin);
                        var newRoad = new SpaceRoad(origin, destination, OwnerEmpire, roadBudget, nodeMaintenance);

                        if (newRoad.NumberOfProjectors != 0 && newRoad.NumberOfProjectors <= roadDevLevel)
                        {
                            roadBudget -= newRoad.NumberOfProjectors * nodeMaintenance;
                            underConstruction += newRoad.NumberOfProjectors * nodeMaintenance;
                            OwnerEmpire.SpaceRoadsList.Add(newRoad);
                        }
                    }
                }
            }
            return underConstruction;
        }
    }
}