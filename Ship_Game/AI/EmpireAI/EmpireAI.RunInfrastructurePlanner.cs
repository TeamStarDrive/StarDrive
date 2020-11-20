using System;
using System.Collections.Generic;
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
            float nodeRadius = empire.GetProjectorRadius();
            using (empire.BorderNodes.AcquireReadLock())
            {
                for (int i = 0; i < empire.BorderNodes.Count; i++)
                {
                    Empire.InfluenceNode border = empire.BorderNodes[i];
                    nodeRadius = Math.Max(nodeRadius, border.Radius);
                    if (pos.InRadius(border.Position, nodeRadius * 0.75f))
                        return true;
                }
            }
            return false;
        }

        public bool NodeAlreadyExistsAt(Vector2 pos)
        {
            for (int gi = 0; gi < Goals.Count; gi++)
            {
                Goal g = Goals[gi];
                if (g is BuildConstructionShip && g.BuildPosition.InRadius(pos, OwnerEmpire.GetProjectorRadius()))
                    return true;
            }

            // bugfix: make sure another construction ship isn't already deploying to pos
            var ships = OwnerEmpire.GetShips();
            for (int si = 0; si < ships.Count; si++)
            {
                Ship ship = ships[si];
                if (ship.AI.FindGoal(ShipAI.Plan.DeployStructure, out ShipAI.ShipGoal goal)
                    && goal.MovePosition.InRadius(pos, OwnerEmpire.GetProjectorRadius()))
                    return true;
            }

            return InfluenceNodeExistsAt(pos, OwnerEmpire);
        }

        void RunInfrastructurePlanner()
        {
            if (!OwnerEmpire.CanBuildPlatforms || OwnerEmpire.isPlayer && !OwnerEmpire.AutoBuild)
                return;

            float perNodeMaintenance  = ResourceManager.ShipsDict["Subspace Projector"].GetMaintCost(OwnerEmpire);
            float roadMaintenance     = GetCurrentProjectorCount() * perNodeMaintenance;
            float underConstruction   = GetTotalConstructionGoalsMaintenance();
            float availableRoadBudget = OwnerEmpire.data.SSPBudget -roadMaintenance - underConstruction;
            float totalRoadBudget     = OwnerEmpire.data.SSPBudget;

            if (availableRoadBudget > perNodeMaintenance * 2)
            {
                CreateNewRoads(availableRoadBudget, perNodeMaintenance, underConstruction);
            }
            
            var toRemove = new Array<SpaceRoad>();

            // iterate spaceroads. remove invalid roads. remove roads that exceed budget. replace and build 
            // missing projectors in roads. 
            foreach (SpaceRoad road in OwnerEmpire.SpaceRoadsList.OrderBy(road => road.NumberOfProjectors))
            {
                // no nodes is invalid. remove. 
                if (road.RoadNodesList.Count == 0)
                {                    
                    toRemove.Add(road);
                    continue;
                }
                float roadCost = road.RoadNodesList.Count * perNodeMaintenance;
                totalRoadBudget -= roadCost;

                // no budget for road. remove. 
                if (totalRoadBudget < 0)
                {
                    toRemove.Add(road);
                    totalRoadBudget += roadCost;
                    continue;
                }

                // road end points are invalid remove. 
                if (!road.Origin.OwnerList.Contains(OwnerEmpire) ||
                                    !road.Destination.OwnerList.Contains(OwnerEmpire))
                {
                    toRemove.Add(road);
                    totalRoadBudget += road.NumberOfProjectors * perNodeMaintenance;
                }
                else
                {
                    // create missing road projectors this includes newly created roads above. 
                    CreateRoadProjectors(road);
                }
            }

            if (!OwnerEmpire.isPlayer)
                ScrapSpaceRoadsForAI(toRemove);
        }

        void CreateRoadProjectors(SpaceRoad road)
        {
            foreach (RoadNode node in road.RoadNodesList)
            {
                if (node.Platform?.Active != true)
                {
                    bool nodeExists = NodeAlreadyExistsAt(node.Position);
                    //if (OwnerEmpire.isPlayer) // DEBUG
                    //    Log.Info($"NodeAlreadyExists? {node.Position}: {nodeExists}");

                    if (!nodeExists)
                    {
                        node.Platform = null;
                        Log.Info($"BuildProjector {node.Position}");
                        Goals.Add(new BuildConstructionShip(node.Position, "Subspace Projector", OwnerEmpire));
                    }
                }
            }
        }

        void ScrapSpaceRoadsForAI(Array<SpaceRoad> toRemove)
        {
            for (int i = 0; i < toRemove.Count; i++)
            {
                SpaceRoad road = toRemove[i];
                OwnerEmpire.SpaceRoadsList.Remove(road);
                foreach (RoadNode node in road.RoadNodesList)
                {
                    if (node.Platform != null && node.Platform.Active)
                    {
                        node.Platform.Die(null, true);
                        continue;
                    }

                    for (int x = 0; x < Goals.Count; x++)
                    {
                        Goal g = Goals[x];
                        if (g.type != GoalType.DeepSpaceConstruction || !g.BuildPosition.AlmostEqual(node.Position))
                            continue;

                        Goals.QueuePendingRemoval(g);
                        IReadOnlyList<Planet> ps = OwnerEmpire.GetPlanets();
                        for (int pi = 0; pi < ps.Count; pi++)
                        { 
                            if (ps[pi].Construction.Cancel(g))
                                break;
                        }

                        Ship[] ships = OwnerEmpire.GetShips().AtomicCopy();
                        for (int si = 0; si < ships.Length; si++)
                        {
                            Ship ship = ships[si];
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

                    Goals.ApplyPendingRemovals();
                }

                OwnerEmpire.SpaceRoadsList.Remove(road);
            }
        }

        float CreateNewRoads(float roadBudget, float nodeMaintenance, float underConstruction)
        {
            IReadOnlyList<SolarSystem> list = OwnerEmpire.GetOwnedSystems();
            for (int i = 0; i < list.Count; i++)
            {
                SolarSystem destination = list[i];
                int destSystemDevLevel = GetSystemDevelopmentLevel(destination);
                if (destSystemDevLevel == 0)
                    continue;

                SolarSystem[] systemsByDistance = OwnerEmpire.GetOwnedSystems()
                    .Sorted(s => s.Position.Distance(destination.Position));
                for (int si = 0; si < systemsByDistance.Length; si++)
                {
                    SolarSystem origin = systemsByDistance[si];
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