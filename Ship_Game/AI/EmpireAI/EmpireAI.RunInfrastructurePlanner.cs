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
        [StarData] public readonly Array<SpaceRoad> SpaceRoads = new();

        public void AddSpaceRoadHeat(SolarSystem origin, SolarSystem destination, float extraHeat)
        {
            if (origin == destination)
                return;

            string name = SpaceRoad.GetSpaceRoadName(origin, destination);
            SpaceRoad existingRoad = SpaceRoads.Find(r => r.Name == name);
            if (existingRoad != null)
            {
                existingRoad.AddHeat(extraHeat);
            }
            else
            {
                int numProjectors = SpaceRoad.GetNeededNumProjectors(origin, destination, OwnerEmpire);
                if (numProjectors > 0)
                    SpaceRoads.Add(new SpaceRoad(origin, destination, OwnerEmpire, numProjectors, name));
            }
        }

        public bool NodeAlreadyExistsAt(Vector2 pos)
        {
            for (int i = 0; i < GoalsList.Count; i++)
            {
                Goal g = GoalsList[i];
                if (g is BuildConstructionShip && g.BuildPosition.InRadius(pos, 1000))
                    return true;
            }

            return false;
        }

        void RunInfrastructurePlanner()
        {
            if (!OwnerEmpire.CanBuildPlatforms || OwnerEmpire.isPlayer && !OwnerEmpire.AutoBuild)
                return;

            if (OwnerEmpire.Universe.StarDate % 1 == 0)
                CoolDownRoads();

            float roadMaintenance = SpaceRoads.Sum(r => r.Maintenance);
            float availableRoadBudget = SSPBudget - roadMaintenance;


            if (!TryScrapSpaceRoad(lowBudgetMustScrap: availableRoadBudget  < 0))
                CreateNewRoad(availableRoadBudget);
        }

        void CoolDownRoads()
        {
            for (int i = 0; i < SpaceRoads.Count; i++)
                SpaceRoads[i].CoolDown();
        }

        void RemoveRoad(SpaceRoad road)
        {
            road.Scrap(GoalsList);
            SpaceRoads.Remove(road);
        }

        // Scrap one road per turn, if applicable
        bool TryScrapSpaceRoad(bool lowBudgetMustScrap)
        {
            SpaceRoad coldestRoad = SpaceRoads.FindMin(r => r.Heat);
            if (coldestRoad != null && (coldestRoad.IsCold || lowBudgetMustScrap))
            {
                RemoveRoad(coldestRoad);
                return true;
            }

            return false;
        }

        // Dealing with one road till its completed
        void CreateNewRoad(float roadBudget)
        {
            if (roadBudget <= 0)
                return;

            var hottestRoad = SpaceRoads.FindMaxFiltered(r => r.Status != SpaceRoad.SpaceRoadStatus.Online, r => r.Heat);
            if (hottestRoad != null) 
            {
                switch (hottestRoad.Status)
                {
                    case SpaceRoad.SpaceRoadStatus.Down when hottestRoad.IsHot && hottestRoad.OnlineMaintenance < roadBudget:
                        hottestRoad.DeployAllProjectors();
                        return;
                    case SpaceRoad.SpaceRoadStatus.InProgress:
                        hottestRoad.FillGaps();
                        return;
                }
            }
        }

        public void RemoveProjectorFromRoadList(Ship projector) => ManageProjectorInRoadsList(projector);
        public void AddProjectorToRoadList(Ship projector, Vector2 buildPosition) 
            => ManageProjectorInRoadsList(projector, buildPosition);

        // This is rarely called (mostly in destruction or when a new projector is placed
        void ManageProjectorInRoadsList(Ship projector, Vector2 buildPosition = default)
        {
            bool remove = buildPosition == default;
            for (int i = 0; i < SpaceRoads.Count; i++)
            {
                SpaceRoad road = SpaceRoads[i];
                for (int j = 0; j < road.RoadNodesList.Count; j++)
                {
                    RoadNode node = road.RoadNodesList[j];
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
            for (int i = 0; i < SpaceRoads.Count; i++)
            {
                SpaceRoad road = SpaceRoads[i];
                road.UpdateMaintenance();
            }
        }

        public void AbsorbSpaceRoadOwnershipFrom(Empire them, Array<SpaceRoad> theirRoads)
        {
            for (int i = 0; i < theirRoads.Count; i++)
            {
                SpaceRoad theirSpaceRoad = theirRoads[i];
                SpaceRoad existingRoad = SpaceRoads.Find(r => r.Name == theirSpaceRoad.Name);
                if (existingRoad != null)
                {
                    theirSpaceRoad.Scrap(them.AI.GoalsList); // we have that road as well
                }
                else
                {
                    theirSpaceRoad.TransferOwnerShipTo(OwnerEmpire);
                    SpaceRoads.Add(theirSpaceRoad);
                }
            }

            theirRoads.Clear();

            // Iterating all projectors since there might be projectors which are not in roads
            var projectors = them.OwnedProjectors;
            for (int i = projectors.Count - 1; i >= 0; i--)
            {
                Ship ship = projectors[i];
                ship.LoyaltyChangeByGift(OwnerEmpire, addNotification: false);
            }
        }
    }
}