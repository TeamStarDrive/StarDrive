using System.Collections.Generic;
using System.Linq;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        [StarData] public readonly Map<string, SpaceRoad> ProjectorHeatMap = new();


        public void AddSpaceRoadHeat(SolarSystem origin, SolarSystem destination)
        {
            if (origin == destination)
                return;

            string name = SpaceRoad.GetSpaceRoadName(origin, destination);
            if (ProjectorHeatMap.TryGetValue(name, out SpaceRoad spaceRoad))
            {
                spaceRoad.AddHeat();
            }
            else
            {
                int numProjectors = SpaceRoad.GetNeededNumProjectors(origin, destination, OwnerEmpire);
                if (numProjectors > 0)
                    ProjectorHeatMap.Add(name, new SpaceRoad(origin, destination, OwnerEmpire, numProjectors, name));
            }
        }

        public static bool InfluenceNodeExistsAt(Vector2 pos, Empire empire)
        {
            return empire.Universe.Influence.IsInInfluenceOf(empire, pos);
        }

        public bool NodeAlreadyExistsAt(Vector2 pos)
        {
            for (int gi = 0; gi < GoalsList.Count; gi++)
            {
                Goal g = GoalsList[gi];
                if (g is BuildConstructionShip && g.BuildPosition.InRadius(pos, 1000))
                    return true;
            }

            return false;
        }

        void RunInfrastructurePlanner()
        {
            if (!OwnerEmpire.CanBuildPlatforms || OwnerEmpire.isPlayer && !OwnerEmpire.AutoBuild)
                return;

            if (OwnerEmpire.Universe.StarDate % 10 == 0)
                CoolDownRoads();

            float roadMaintenance = ProjectorHeatMap.Values.Sum(r => r.Maintenance);
            float availableRoadBudget = SSPBudget - roadMaintenance;

            if (!TryScrapSpaceRoads(availableRoadBudget))
                CreateNewRoads(availableRoadBudget);
        }

        void CoolDownRoads()
        {
            var ships = OwnerEmpire.OwnedShips;
            if (ships.Any(s => s.IsIdleFreighter))
            {
                foreach (SpaceRoad road in ProjectorHeatMap.Values)
                    road.CoolDown();
            }
        }

        void RemoveRoad(SpaceRoad road)
        {
            road.Scrap(GoalsList);
            ProjectorHeatMap.Remove(road.Name);
        }

        bool TryScrapSpaceRoads(float availableBudget)
        {
            var projectorsByHeat = ProjectorHeatMap.Values.Sorted(r => r.Heat);
            bool mustRemove = availableBudget < 0;
            for (int i = 0; i < projectorsByHeat.Length; i++)
            {
                SpaceRoad road = projectorsByHeat[i];
                if (mustRemove || road.IsInvalid())
                {
                    RemoveRoad(road);
                    return true;
                }
            }

            return false;
        }

        void CreateNewRoads(float roadBudget)
        {
            if (roadBudget <= 0)
                return;

            var projectorsByHeat = ProjectorHeatMap.Values.SortedDescending(r => r.Heat);
            for (int i = 0; i < projectorsByHeat.Length; i++)
            {
                SpaceRoad road = projectorsByHeat[i];
                switch (road.Status)
                {
                    case SpaceRoad.SpaceRoadStatus.Down when road.IsHot && road.Maintenance < roadBudget:
                        road.DeployAllProjectors();
                        return;
                    case SpaceRoad.SpaceRoadStatus.InProgress: 
                        road.FillGaps();
                        return;
                }
            }
        }

        public void RemoveProjectorFromRoadList(Ship projector) => ManageProjectorInRoadsList(projector);
        public void AddProjectorToRoadList(Ship projector, Vector2 buildPosition) 
            => ManageProjectorInRoadsList(projector, buildPosition);

        void ManageProjectorInRoadsList(Ship projector, Vector2 buildPosition = default)
        {
            bool remove = buildPosition == default;
            foreach (SpaceRoad road in ProjectorHeatMap.Values)
            {
                for (int i = 0; i < road.RoadNodesList.Count; i++)
                {
                    RoadNode node = road.RoadNodesList[i];
                    if (remove && node.Projector == projector 
                        || !remove && node.Position.InRadius(buildPosition, 100))
                    {
                        road.SetProjectorInNode(node, projector); // will be set to null if remove is true
                        projector.Universe.Stats.StatAddRoad(projector.Universe.StarDate, node, OwnerEmpire);
                        return;
                    }
                }
            }
        }

        public void UpdateRoadMaintenance()
        {
            foreach (SpaceRoad road in ProjectorHeatMap.Values)
            {
                road.UpdateMaintenance();
            }
        }

        public void AbsorbSpaceRoadOwnershipFrom(Empire them, Map<string, SpaceRoad> theirRoads)
        {
            foreach (KeyValuePair<string,SpaceRoad> theirSpaceRoad in theirRoads)
            {
                if (ProjectorHeatMap.TryGetValue(theirSpaceRoad.Key, out _))
                {
                    theirSpaceRoad.Value.Scrap(them.AI.GoalsList); // we have that road as well
                }
                else
                {
                    theirSpaceRoad.Value.TransferOwnerShipTo(OwnerEmpire);
                    ProjectorHeatMap.Add(theirSpaceRoad.Key, theirSpaceRoad.Value);
                }
            }

            theirRoads.Clear();

            // Iterating all projectors since their might be projectors which are not in roads
            var projectors = them.OwnedProjectors;
            for (int i = projectors.Count - 1; i >= 0; i--)
            {
                Ship ship = projectors[i];
                ship.LoyaltyChangeByGift(OwnerEmpire, addNotification: false);
            }
        }
    }
}