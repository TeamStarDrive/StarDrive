using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;
using static Ship_Game.AI.ShipAI;
using Vector2 = SDGraphics.Vector2;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    [StarDataType]
    public sealed class SpaceRoadsManager
    {
        // Fat Bastard
        /* This will maintain the space roads (Subspace Projector lanes) the empire needs
           When freighters set up a trade plan, a space road will be created between the the 2 planets.
           The more freighter follow that lane, the more heat this lane will get. If the heat is higher 
           then a certain threshold and there is budget, all the related projectors will be deployed.
           Roads also cool down if not used for quite some time and after a certain threshold, the road will
           be scrapped.
        */

        [StarData] public readonly Array<SpaceRoad> SpaceRoads = new();
        [StarData] public readonly Empire Owner;

        bool ShouldManageRoads => Owner.CanBuildPlatforms
                                  && (!Owner.isPlayer || Owner.isPlayer && Owner.AutoBuildSpaceRoads);

        [StarDataConstructor]
        SpaceRoadsManager() {}

        public SpaceRoadsManager(Empire owner)
        {
            Owner= owner;
        }

        // Adds heat to an existing road or set ups a new road is it does not exist
        public void AddSpaceRoadHeat(SolarSystem origin, SolarSystem destination, float extraHeat)
        {
            if (origin == destination || !ShouldManageRoads)
                return;

            string name = SpaceRoad.GetSpaceRoadName(origin, destination);
            SpaceRoad existingRoad = SpaceRoads.Find(r => r.Name == name);
            if (existingRoad != null)
            {
                existingRoad.AddHeat(extraHeat);
            }
            else
            {
                int numProjectors = SpaceRoad.GetNeededNumProjectors(origin, destination, Owner);
                if (numProjectors > 0)
                    SpaceRoads.Add(new SpaceRoad(origin, destination, Owner, numProjectors, name, GetNonDownEmpireRoadNodes()));
            }
        }

        public bool NodeGoalAlreadyExistsFor(Vector2 pos)
        {
            for (int i = 0; i < Owner.AI.Goals.Count; i++)
            {
                Goal g = Owner.AI.Goals[i];
                if (g is BuildConstructionShip && g.BuildPosition.InRadius(pos, 1000))
                    return true;
            }

            return false;
        }

        public bool InfluenceNodeExistsAt(Vector2 pos)
        {
            return Owner.Universe.Influence.IsInInfluenceOf(Owner, pos);
        }

        public void Update()
        {
            if (!ShouldManageRoads)
                return;

            if (Owner.Universe.StarDate % 1 == 0)
                CoolDownRoads();

            float roadMaintenance = SpaceRoads.Sum(r => r.Maintenance);
            float availableRoadBudget = Owner.AI.SSPBudget - roadMaintenance;
            bool skipRoadScrap = availableRoadBudget > Owner.AI.SSPBudget * 0.25f;
            if (skipRoadScrap || !TryScrapSpaceRoad(lowBudgetMustScrap: availableRoadBudget  < 0))
                CreateNewRoad(availableRoadBudget);
        }

        void CoolDownRoads()
        {
            for (int i = 0; i < SpaceRoads.Count; i++)
                SpaceRoads[i].CoolDown();
        }

        void RemoveRoad(SpaceRoad roadToRemove, bool fillGapsInOtherRoads)
        {
            if (fillGapsInOtherRoads)
            {
                var checkedNodes = roadToRemove.RoadNodesList.ToArray();
                var allOtherNodes = GetAllRoadNodes(excludedRoad: roadToRemove);
                foreach (SpaceRoad road in SpaceRoads.SortedDescending(r => r.Heat)
                             .Filter(r => r != roadToRemove && r.Status is SpaceRoad.SpaceRoadStatus.Online
                                                                        or SpaceRoad.SpaceRoadStatus.InProgress))
                {
                    road.FillNodeGaps(allOtherNodes, ref checkedNodes);
                }
            }

            roadToRemove.Scrap();
            SpaceRoads.Remove(roadToRemove);
        }

        RoadNode[] GetAllRoadNodes(SpaceRoad excludedRoad)
        {
            Array<RoadNode> allRoadNodes = new();
            SpaceRoad[] array = SpaceRoads.SortedDescending(r => r.Heat)
                         .Filter(r => r != excludedRoad && (r.Status == SpaceRoad.SpaceRoadStatus.Online
                                                            || r.Status == SpaceRoad.SpaceRoadStatus.InProgress));
            for (int i = 0; i < array.Length; i++)
            {
                SpaceRoad road = array[i];
                allRoadNodes.AddRange(road.RoadNodesList);
            }

            return allRoadNodes.ToArray();
        }

        // Scrap one road per turn, if applicable
        bool TryScrapSpaceRoad(bool lowBudgetMustScrap)
        {
            SpaceRoad coldestRoad = lowBudgetMustScrap 
                    ? SpaceRoads.FindMin(r => r.Heat)
                    : SpaceRoads.FindMinFiltered(r => r.IsCold, r => r.Heat);

            if (coldestRoad != null && (coldestRoad.IsCold || lowBudgetMustScrap))
            {
                RemoveRoad(coldestRoad, fillGapsInOtherRoads: true);
                return true;
            }

            return false;
        }

        public void RemoveRoadIfNeeded(SolarSystem system)
        {
            if (system.HasPlanetsOwnedBy(Owner)
                || system.IsResearchStationDeployedBy(Owner)
                || system.PlanetList.Any(p => p.Mining?.Owner == Owner))
            {
                return;
            }

            for (int i = SpaceRoads.Count - 1; i >= 0; i--)
            {
                SpaceRoad road = SpaceRoads[i];
                if (road.HasSystem(system))
                    RemoveRoad(road, fillGapsInOtherRoads: true);
            }
        }

        // Dealing with one road till its completed
        void CreateNewRoad(float roadBudget)
        {
            if (roadBudget <= 0)
                return;

            // Look for the hottest road which is not yet online and deal with it
            SpaceRoad[] hotRoads = SpaceRoads.Filter(r => r.Status != SpaceRoad.SpaceRoadStatus.Online && r.IsHot);
            if (hotRoads.Length > 0)
            {
                foreach (SpaceRoad road in hotRoads)
                {
                    switch (road.Status)
                    {
                        case SpaceRoad.SpaceRoadStatus.Down when road.OperationalMaintenance < roadBudget:
                            road.DeployAllProjectors(GetNonDownEmpireRoadNodes());
                            return;
                        case SpaceRoad.SpaceRoadStatus.InProgress: // This is tagged as some projectors are missing
                            if (road.FillProjectorGaps())
                                return;

                            break;
                    }
                }
            }
        }

        public void AddProjectorToRoadList(Ship projector, Vector2 buildPos)
        {
            for (int i = 0; i < SpaceRoads.Count; i++)
                if (SpaceRoads[i].AddProjector(projector, buildPos))
                    return; // added successfully
        }

        public void RemoveProjectorFromRoadList(Ship projector)
        {
            for (int i = 0; i < SpaceRoads.Count; i++)
                if (SpaceRoads[i].RemoveProjectorRef(projector))
                    return; // removed successfully
        }

        public void UpdateAllRoadsMaintenance()
        {
            for (int i = 0; i < SpaceRoads.Count; i++)
            {
                SpaceRoad road = SpaceRoads[i];
                road.UpdateMaintenance();
            }
        }

        public int NumOnlineSpaceRoads => SpaceRoads.Count(r => r.Status == SpaceRoad.SpaceRoadStatus.Online);

        // This removes all space roads of the empire and lest the absorbing empire 
        // space road manager to rebuild roads that are needed in the future
        public void RemoveSpaceRoadsByAbsorb()
        {
            for (int i = SpaceRoads.Count - 1; i >= 0; i--)
            {
                SpaceRoad spaceRoad = SpaceRoads[i];
                RemoveRoad(spaceRoad, fillGapsInOtherRoads: false);
            }

            SpaceRoads.Clear();

            // Iterating all projectors since there might be projectors which are not in roads
            var projectors = Owner.OwnedProjectors;
            for (int i = projectors.Count - 1; i >= 0; i--)
            {
                Ship projector = projectors[i];
                projector.AI.OrderScuttleShip();
            }
        }

        public IReadOnlyCollection<RoadNode> GetNonDownEmpireRoadNodes()
        {
            Array<RoadNode> nodePositions = new();
            foreach (SpaceRoad road in SpaceRoads.Filter(r => r.Status != SpaceRoad.SpaceRoadStatus.Down)) 
            {
                nodePositions.AddRange(road.RoadNodesList);
            }

            return nodePositions;
        }

        public void SetupProjectorBridgeIfNeeded(Ship ship, ProjectorBridgeEndCondition endCondition)
        {
            if (ship.System == null
                || Owner.IsFaction
                || Owner.isPlayer && !Owner.AutoBuildSpaceRoads)
            {
                return;
            }

            if (!CheckBridgeNeededColonyShip())
                CheckBridgeNeededTradeOrConstruction();

            bool CheckBridgeNeededColonyShip()
            {
                if (ship.ShipData.IsColonyShip && ship.AI.State == AIState.Colonize
                                          && !Owner.AI.SpaceRoadsManager.InfluenceNodeExistsAt(ship.Position))
                {
                    // find where the ship was coming from and setup a projector bridge
                    Goal colonizationGoal = Owner.AI.FindGoal(g => g.FinishedShip == ship);
                    if (colonizationGoal?.PlanetBuildingAt != null)
                    {
                        Owner.AI.AddGoal(new ProjectorBridge(ship.System,
                            colonizationGoal.PlanetBuildingAt.System.Position, Owner, endCondition));

                        return true;
                    }
                }

                return false;
            }

            void CheckBridgeNeededTradeOrConstruction()
            {
                if ((ship.IsFreighter || ship.IsConstructor) 
                    && !Owner.AI.SpaceRoadsManager.InfluenceNodeExistsAt(ship.Position))
                {
                    // find where the ship was coming from and setup a projector bridge
                    if (ship.AI.State == AIState.SystemTrader)
                    {
                        ship.AI.OrderQueue.TryPeekFirst(out ShipGoal goal);
                        if (goal?.Trade != null)
                        {
                            Owner.AI.AddGoalAndEvaluate(new ProjectorBridge(ship.System,
                                goal.Trade.ExportFrom.System.Position, Owner, endCondition));

                            return;
                        }
                    }
                    // fallback to ship position for bridge direction or if ship is a constructor
                    Owner.AI.AddGoalAndEvaluate(new ProjectorBridge(ship.System, Owner.WeightedCenter, Owner, endCondition));
                }
            }
        }
    }
}